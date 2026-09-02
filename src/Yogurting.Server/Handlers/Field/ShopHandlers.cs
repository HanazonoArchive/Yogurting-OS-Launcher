using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Yogurting.Core.Logging;
using Yogurting.Core.Models;
using Yogurting.Core.Network;
using Yogurting.Data.Loaders;
using Yogurting.Data.Repositories;

namespace Yogurting.Server.Handlers.Field
{
    /// <summary>
    /// Handles Star Cash Shop (Byul Shop) and Standard NPC Shop actions in the Field Server.
    /// Matches Delphi UYgItem, UYgDB, and _Unit47.pas / _Unit67.pas.
    /// </summary>
    public sealed class ShopHandlers
    {
        private readonly Func<PlayerSessionState, byte[], Task> _broadcastDelegate;
        private readonly IAccountRepository? _repository;
        private readonly GameDatabase? _gameDb;

        public ShopHandlers(Func<PlayerSessionState, byte[], Task> broadcastDelegate, IAccountRepository? repository = null, GameDatabase? gameDb = null)
        {
            _broadcastDelegate = broadcastDelegate ?? throw new ArgumentNullException(nameof(broadcastDelegate));
            _repository = repository;
            _gameDb = gameDb;
        }

        /// <summary>
        /// 0x5233 (21043): MsgGameByulShopBeginReq - Open Star Cash Shop
        /// Matches Quartet behavior: Star Shop is disabled in Mob / Combat Fields.
        /// </summary>
        [PacketHandler(PacketOpcode.MsgGameByulShopBeginReq)]
        public async Task HandleByulShopBeginAsync(PlayerSessionState state, byte[] packetData)
        {
            try
            {
                var player = state.Player;
                Logger.Info($"[FieldServer] '{player?.CharacterName ?? "Player"}' opened Star Cash Shop terminal.");
                byte[] ans = YogurtingPackets.MakeByulShopBeginAns(0);
                await state.Session.SendAsync(ans);
            }
            catch (Exception ex)
            {
                Logger.Error($"[FieldServer] HandleByulShopBegin failed: {ex.Message}");
            }
        }

        /// <summary>
        /// 0x523A (21050): MsgGameByulChargeReq - Query current Star Cash Coin balance
        /// </summary>
        [PacketHandler(PacketOpcode.MsgGameByulChargeReq)]
        public async Task HandleByulChargeReqAsync(PlayerSessionState state, byte[] packetData)
        {
            try
            {
                var player = state.Player;
                int points = player?.StarPoints ?? 0;
                Logger.Info($"[FieldServer] '{player?.CharacterName ?? "Player"}' requested Star Coin balance -> {points} Points");

                byte[] ans = YogurtingPackets.MakeGameByulChargeAns(0, points);
                await state.Session.SendAsync(ans);
            }
            catch (Exception ex)
            {
                Logger.Error($"[FieldServer] HandleByulChargeReq failed: {ex.Message}");
            }
        }

        /// <summary>
        /// 0x5235 (21045): MsgGameByulShopEndReq - Close Star Cash Shop
        /// </summary>
        [PacketHandler(PacketOpcode.MsgGameByulShopEndReq)]
        public async Task HandleByulShopEndAsync(PlayerSessionState state, byte[] packetData)
        {
            try
            {
                var player = state.Player;
                Logger.Info($"[FieldServer] '{player?.CharacterName ?? "Player"}' closed Star Cash Shop terminal.");
                byte[] ans = YogurtingPackets.MakeByulShopEndAns(0);
                await state.Session.SendAsync(ans);
            }
            catch (Exception ex)
            {
                Logger.Error($"[FieldServer] HandleByulShopEnd failed: {ex.Message}");
            }
        }

        /// <summary>
        /// 0x523C (21052): MsgGameByulProductListReq - Request Star Cash Shop catalog
        /// </summary>
        [PacketHandler(PacketOpcode.MsgGameByulProductListReq)]
        public async Task HandleByulProductListAsync(PlayerSessionState state, byte[] packetData)
        {
            try
            {
                var products = _gameDb?.StarProducts ?? new List<ShopProductDef>();
                Logger.Info($"[FieldServer] Dispatched Star Shop product catalog ({products.Count} items) to '{state.Player?.CharacterName}'.");
                byte[] ans = YogurtingPackets.MakeByulProductListAns(0, products);
                await state.Session.SendAsync(ans);
            }
            catch (Exception ex)
            {
                Logger.Error($"[FieldServer] HandleByulProductList failed: {ex.Message}");
            }
        }

        /// <summary>
        /// 0x523E (21054): MsgGameByulProductBuyReq - Purchase item with Star Points
        /// Packet layout: [Header 6B] [Int32 ProductID 4B] [Int32 Quantity 4B]
        /// </summary>
        [PacketHandler(PacketOpcode.MsgGameByulProductBuyReq)]
        public async Task HandleByulProductBuyAsync(PlayerSessionState state, byte[] packetData)
        {
            try
            {
                if (packetData.Length < 10) return;
                int productId = BitConverter.ToInt32(packetData, 6);
                int quantity = packetData.Length >= 14 ? BitConverter.ToInt32(packetData, 10) : 1;
                if (quantity <= 0) quantity = 1;

                var player = state.Player;
                if (player == null) return;

                // 1. Locate product definition from database
                ShopProductDef? product = _gameDb?.StarProducts.Find(p => p.ProductId == productId);
                int price = product?.Price ?? 0;
                int period = product?.Period ?? 0;

                var deliveredItems = new List<Item>();

                // 2. Validate Player Star Points & Deliver Items under lock
                lock (player)
                {
                    if (player.StarPoints < price)
                    {
                        Logger.Warn($"[FieldServer] Star purchase rejected: '{player.CharacterName}' has {player.StarPoints} Star Points, but Product #{productId} costs {price}!");
                        byte[] failAns = YogurtingPackets.MakeByulProductBuyAns(10003, productId, 0L, player.StarPoints, period, price);
                        _ = state.Session.SendAsync(failAns);
                        return;
                    }

                    // 3. Deduct Currency (StarPoints)
                    player.StarPoints -= price;

                    // 4. Create and Deliver Purchased Items
                    var itemTypeIds = (product?.ItemIds != null && product.ItemIds.Count > 0)
                        ? product.ItemIds
                        : (product != null ? new List<int> { product.ProductId } : new List<int> { productId });

                    int maxInv = (player.Inventory != null && player.Inventory.Count > 0) ? player.Inventory.Max(i => i.Id) : 0;
                    int maxStar = (player.StarBeItems != null && player.StarBeItems.Count > 0) ? player.StarBeItems.Max(i => i.Id) : 0;
                    int runningUid = Math.Max(Math.Max(maxInv, maxStar), 100);

                    foreach (int itemTypeId in itemTypeIds)
                    {
                        runningUid += 2;
                        int nextUid = runningUid;
                        long itemSerialId = nextUid;
                        string itemName = _gameDb?.Items.TryGetValue(itemTypeId, out var def) == true ? def.Name : $"Product {itemTypeId}";

                        var newItem = new Item
                        {
                            Id = nextUid,
                            ItemId = itemTypeId,
                            TypeId = itemTypeId,
                            Name = itemName,
                            Quantity = quantity,
                            SerialId = itemSerialId,
                            SlotIndex = (ushort)((player.StarBeItems?.Count ?? 0) + 1),
                            SlotType = ItemSlotType.Inventory,
                            IsEquipped = false,
                            SocketSlots = new int[5]
                        };

                        (player.StarBeItems ??= new List<Item>()).Add(newItem);
                        deliveredItems.Add(newItem);
                        Logger.Info($"[FieldServer] Star purchase granted item: '{player.CharacterName}' received '{itemName}' ({itemTypeId}) [FID: {itemSerialId}]");
                    }
                }

                Logger.Info($"[FieldServer] Star purchase SUCCESS: '{player.CharacterName}' bought Product #{productId} ({deliveredItems.Count} items) for {price} Stars (Remaining: {player.StarPoints} Stars)!");

                // 5. Persist to Save File
                if (_repository != null)
                {
                    await _repository.SaveAccountAsync(player);
                }

                // 6. Send Purchase Success Response (0x523F)
                byte[] successAns = YogurtingPackets.MakeByulProductBuyAns(0, productId, deliveredItems.Count > 0 ? deliveredItems[0].SerialId : 0L, player.StarPoints, period, price, deliveredItems);
                await state.Session.SendAsync(successAns);
            }
            catch (Exception ex)
            {
                Logger.Error($"[FieldServer] HandleByulProductBuy failed: {ex.Message}");
            }
        }

        /// <summary>
        /// 0x5221 (21025): MsgGameShopEnterReq - Open Standard NPC Store
        /// </summary>
        [PacketHandler(PacketOpcode.MsgGameShopEnterReq)]
        public async Task HandleShopEnterAsync(PlayerSessionState state, byte[] packetData)
        {
            try
            {
                int npcId = packetData.Length >= 10 ? BitConverter.ToInt32(packetData, 6) : 4;
                int shopId = packetData.Length >= 14 ? BitConverter.ToInt32(packetData, 10) : 1;
                string npcName = _gameDb?.Npcs.TryGetValue(npcId, out var name) == true ? name : $"NPC #{npcId}";

                Logger.Info($"[FieldServer] '{state.Player?.CharacterName}' opened store with {npcName} (NPC {npcId}, Shop {shopId}).");

                // Populate catalog dynamically from GameDatabase (ShopItemList.txt)
                var storeItems = new List<(byte category, int itemId)>();
                if (_gameDb != null && _gameDb.ShopItems.Count > 0)
                {
                    foreach (var item in _gameDb.ShopItems)
                    {
                        byte cat = (byte)(item.Category > 0 ? item.Category : 1);
                        storeItems.Add((cat, item.ItemId));
                    }
                }

                byte[] catalog = YogurtingPackets.MakeGameShopListNtf(1, storeItems.ToArray());
                await state.Session.SendAsync(catalog);
            }
            catch (Exception ex)
            {
                Logger.Error($"[FieldServer] HandleShopEnter failed: {ex.Message}");
            }
        }

        /// <summary>
        /// 0xA413 (42003): MsgGameCapsuleBuyReq - Gacha Capsule Machine Purchase Request
        /// 0xA415 (42005): MsgGameCapsuleExitNtf - Gacha Capsule Machine Exit
        /// </summary>
        [PacketHandler(PacketOpcode.MsgGameCapsuleBuyReq)]
        public async Task HandleCapsuleBuyReqAsync(PlayerSessionState state, byte[] packetData)
        {
            try
            {
                var player = state.Player;
                if (player == null) return;

                int machineSn = packetData.Length >= 10 ? BitConverter.ToInt32(packetData, 6) : 0;
                long price = 500;

                // Roll random capsule prize item dynamically from database ShopItems or Items table
                int rolledItem = 10001;
                if (_gameDb != null && _gameDb.ShopItems.Count > 0)
                {
                    rolledItem = _gameDb.ShopItems[Random.Shared.Next(_gameDb.ShopItems.Count)].ItemId;
                }
                else if (_gameDb != null && _gameDb.Items.Count > 0)
                {
                    var keyList = _gameDb.Items.Keys.ToList();
                    rolledItem = keyList[Random.Shared.Next(keyList.Count)];
                }

                lock (player)
                {
                    if (player.TaffPoints < price)
                    {
                        Logger.Warn($"[Shop] Capsule buy rejected: '{player.CharacterName}' has {player.TaffPoints} Taff, but machine requires {price}!");
                        _ = state.Session.SendAsync(YogurtingPackets.MakeGameCapsuleBuyAns(2, 0, 0, price, player.TaffPoints));
                        return;
                    }

                    player.TaffPoints -= price;

                    // Add rolled prize to inventory
                    var existing = player.Inventory?.Find(i => i.TypeId == rolledItem);
                    if (existing != null)
                    {
                        existing.Quantity++;
                    }
                    else
                    {
                        player.Inventory?.Add(new Item
                        {
                            Id = ((player.Inventory?.Count > 0) ? player.Inventory.Max(i => i.Id) : 0) + 1,
                            TypeId = rolledItem,
                            SlotIndex = player.Inventory?.Count ?? 0,
                            SlotType = ItemSlotType.Inventory,
                            Quantity = 1,
                            Name = _gameDb?.Items.TryGetValue(rolledItem, out var def) == true ? def.Name : "Capsule Prize"
                        });
                    }
                }

                Logger.Info($"[Shop] '{player.CharacterName}' bought capsule from Machine {machineSn}, received Item #{rolledItem}!");

                // Reply with 0xA414 (Capsule Buy Answer)
                await state.Session.SendAsync(YogurtingPackets.MakeGameCapsuleBuyAns(0, rolledItem, 1, price, player.TaffPoints));
                await state.Session.SendAsync(YogurtingPackets.MakeGameSetStateNtf(player));

                if (_repository != null)
                {
                    await _repository.SaveAccountAsync(player);
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"[Shop] CapsuleBuy error: {ex.Message}");
            }
        }

        [PacketHandler(PacketOpcode.MsgGameCapsuleExitNtf)]
        public Task HandleCapsuleExitNtfAsync(PlayerSessionState state, byte[] packetData)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// 0xA02B (41003): MsgLockerOpenReq - Open Player Storage Locker
        /// </summary>
        [PacketHandler(PacketOpcode.MsgLockerOpenReq)]
        public async Task HandleLockerOpenReqAsync(PlayerSessionState state, byte[] packetData)
        {
            try
            {
                var player = state.Player;
                if (player == null) return;

                int lockerId = packetData.Length >= 10 ? BitConverter.ToInt32(packetData, 6) : 1;
                Logger.Info($"[Locker] '{player.CharacterName}' opened Locker #{lockerId}.");

                // 1. Send Open Ans (0xA02C)
                await state.Session.SendAsync(YogurtingPackets.MakeLockerOpenAns(1, lockerId));

                // 2. Send Stored Items (0xA02D)
                await state.Session.SendAsync(YogurtingPackets.MakeLockerItemInfoNtf(lockerId, player.LockerItems));
            }
            catch (Exception ex)
            {
                Logger.Error($"[Locker] HandleLockerOpenReq error: {ex.Message}");
            }
        }

        /// <summary>
        /// 0xA02F (41007): MsgLockerMoveItemReq - Move Item between Backpack and Locker
        /// </summary>
        [PacketHandler(PacketOpcode.MsgLockerMoveItemReq)]
        public async Task HandleLockerMoveItemReqAsync(PlayerSessionState state, byte[] packetData)
        {
            try
            {
                var player = state.Player;
                if (player == null) return;

                int lockerId = packetData.Length >= 10 ? BitConverter.ToInt32(packetData, 6) : 1;
                byte direct = packetData.Length >= 11 ? packetData[10] : (byte)0; // 0 = Inven -> Locker, 1 = Locker -> Inven
                int rawType = packetData.Length >= 18 ? BitConverter.ToInt32(packetData, 14) : 0;
                int itemTypeMask = rawType & unchecked((int)0xFF000000);
                int typeId = rawType & 0x00FFFFFF;

                Logger.Info($"[Locker] '{player.CharacterName}' moving item Type #{typeId} Direct={direct} (0:Deposit, 1:Withdraw).");

                Item? movedItem = null;
                if (direct == 0) // Deposit
                {
                    movedItem = player.Inventory?.Find(i => i.TypeId == typeId);
                    if (movedItem != null)
                    {
                        player.Inventory?.Remove(movedItem);
                        player.LockerItems.Add(movedItem);
                    }
                }
                else // Withdraw
                {
                    movedItem = player.LockerItems.Find(i => i.TypeId == typeId);
                    if (movedItem != null)
                    {
                        player.LockerItems.Remove(movedItem);
                        player.Inventory?.Add(movedItem);
                    }
                }

                if (movedItem != null)
                {
                    await state.Session.SendAsync(YogurtingPackets.MakeLockerMoveItemCompleteNtf(0, lockerId, direct, movedItem));
                    if (_repository != null)
                    {
                        await _repository.SaveAccountAsync(player);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"[Locker] HandleLockerMoveItemReq error: {ex.Message}");
            }
        }

    }
}
