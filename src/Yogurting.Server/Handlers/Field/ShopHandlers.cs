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
                int points = player != null && player.StarPoints > 0 ? player.StarPoints : 10000;
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

                // 1. Locate product definition
                ShopProductDef? product = _gameDb?.StarProducts.Find(p => p.ProductId == productId);
                int price = product?.Price ?? 5000;
                int period = product?.Period ?? 0;

                // 2. Validate Player Star Points
                if (player.StarPoints < price)
                {
                    Logger.Warn($"[FieldServer] Star purchase rejected: '{player.CharacterName}' has {player.StarPoints} Star Points, but Product #{productId} costs {price}!");
                    byte[] failAns = YogurtingPackets.MakeByulProductBuyAns(10003, productId, 0L, player.StarPoints, period, price);
                    await state.Session.SendAsync(failAns);
                    return;
                }

                // 3. Deduct Currency (StarPoints)
                player.StarPoints -= price;

                // 4. Create and Deliver Purchased Items (Supports both single items and bundle packages from ProductList.xml)
                var deliveredItems = new List<Item>();
                var itemTypeIds = (product?.ItemIds != null && product.ItemIds.Count > 0)
                    ? product.ItemIds
                    : GetProductItemIds(productId);

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

                    // Also add to standard inventory for instant client sync
                    (player.Inventory ??= new List<Item>()).Add(new Item
                    {
                        Id = nextUid,
                        ItemId = itemTypeId,
                        TypeId = itemTypeId,
                        Name = itemName,
                        Quantity = quantity,
                        SerialId = itemSerialId,
                        SlotIndex = (ushort)(player.Inventory.Count + 1),
                        SlotType = ItemSlotType.Inventory,
                        IsEquipped = false,
                        SocketSlots = new int[5]
                    });

                    deliveredItems.Add(newItem);
                    Logger.Info($"[FieldServer] Star purchase granted item: '{player.CharacterName}' received '{itemName}' ({itemTypeId}) [FID: {itemSerialId}]");
                }

                Logger.Info($"[FieldServer] Star purchase SUCCESS: '{player.CharacterName}' bought Product #{productId} ({deliveredItems.Count} items) for {price} Stars (Remaining: {player.StarPoints} Stars)!");

                // 5. Persist to Save File
                if (_repository != null)
                {
                    await _repository.SaveAccountAsync(player);
                }

                // 6. Send Purchase Success Response (0x523F)
                byte[] successAns = YogurtingPackets.MakeByulProductBuyAns(0, productId, deliveredItems[0].SerialId, player.StarPoints, period, price, deliveredItems);
                await state.Session.SendAsync(successAns);
            }
            catch (Exception ex)
            {
                Logger.Error($"[FieldServer] HandleByulProductBuy failed: {ex.Message}");
            }
        }

        private static List<int> GetProductItemIds(int productId)
        {
            return productId switch
            {
                // Consumables & Utility Items
                23 => new List<int> { 23 },
                27 => new List<int> { 27 },
                31 => new List<int> { 31 },
                44 => new List<int> { 44 },
                48 => new List<int> { 48 },
                54 => new List<int> { 6103 }, // Memory Chip 30d
                70 => new List<int> { 70 },
                161 => new List<int> { 7004 }, // EXP Up 30d
                167 => new List<int> { 7103 }, // Damage Up 7d
                168 => new List<int> { 7104 }, // Damage Up 30d
                174 => new List<int> { 7203 }, // Defense Up 7d
                175 => new List<int> { 7204 }, // Defense Up 30d
                181 => new List<int> { 7303 }, // Hit Up 7d
                182 => new List<int> { 7304 }, // Hit Up 30d
                188 => new List<int> { 7403 }, // Free Up 7d
                189 => new List<int> { 7404 }, // Free Up 30d
                195 => new List<int> { 7503 }, // Critical Up 7d
                196 => new List<int> { 7504 }, // Critical Up 30d

                // Costume Sets
                105 => new List<int> { 2103111, 2103121, 2103131 }, // Badminton (Male)
                126 => new List<int> { 2203111, 2203121, 2203131 }, // Badminton (Female)
                133 => new List<int> { 1103211, 1103221, 1103231 }, // ROCK Stage (Male)
                140 => new List<int> { 1203211, 1203221, 1203231 }, // ROCK Stage (Female)
                147 => new List<int> { 2103211, 2103221, 2103231 }, // Folk Band (Male)
                154 => new List<int> { 2203211, 2203221, 2203231 }, // Folk Band (Female)

                // Tempo Buff & Utility Bundles
                205 => new List<int> { 6202, 27, 7003 },             // Mezzo Forte Set (Locker, Placard, EXP Up 7d)
                206 => new List<int> { 6202, 31, 27, 6103, 7003 },       // Forte Set (Locker, Name Tag, Placard, Chip, EXP Up 7d)
                207 => new List<int> { 6202, 31, 44, 27, 6103, 48, 23, 7004 }, // Fortissimo Set
                208 => new List<int> { 7301, 7401, 7001 },          // Presto / Beginner Adagio Set (Hit 1d, Free 1d, EXP 1d)
                209 => new List<int> { 7103, 7203, 7503 },          // Largo / Allegro Set (Damage 7d, Def 7d, Crit 7d)
                210 => new List<int> { 7303, 7403, 7003 },          // Moderato Set (Hit 7d, Free 7d, EXP 7d)

                // Club Uniform Sets
                211 => new List<int> { 1102211, 1102221, 1102231 }, // Robot Club Set (Male)
                212 => new List<int> { 1202211, 1202221, 1202231 }, // Robot Club Set (Female)
                213 => new List<int> { 2102911, 2102921, 2102931 }, // Archaeology Set (Male)
                214 => new List<int> { 2202911, 2202921, 2202931 }, // Archaeology Set (Female)
                215 => new List<int> { 1102411, 1102421, 1102431 }, // Tea Time Set (Male)
                216 => new List<int> { 1202411, 1202421, 1202431 }, // Tea Time Set (Female)
                217 => new List<int> { 2102411, 2102421, 2102431 }, // Dim Sum Set (Male)
                218 => new List<int> { 2202411, 2202421, 2202431 }, // Dim Sum Set (Female)

                // 30-Day Musical Tempo Buff Sets
                219 => new List<int> { 7104, 7204, 7504 },          // Allegrissimo Set (Damage 30d, Def 30d, Crit 30d)
                220 => new List<int> { 7304, 7404, 7004 },          // Vivace Set (Hit 30d, Free 30d, EXP 30d)

                // Club Uniforms (Extended)
                342 => new List<int> { 1102611, 1102621, 1102631 }, // Gambling Club (Male)
                343 => new List<int> { 1202611, 1202621, 1202631 }, // Gambling Club (Female)
                344 => new List<int> { 2101911, 2101921, 2101931 }, // Ninja Club (Male)
                345 => new List<int> { 2201911, 2201921, 2201931 }, // Ninja Club (Female)

                _ => new List<int> { productId }
            };
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
                else
                {
                    storeItems.Add((1, 200001)); // Beginner Bread
                    storeItems.Add((2, 110001)); // Starter Blade
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

                // Roll random capsule prize item from kuji / starter items
                int[] prizePool = { 30002, 1001, 1002, 110001, 120001, 130001, 140001 };
                int rolledItem = prizePool[Random.Shared.Next(prizePool.Length)];

                if (player.TaffPoints >= price)
                {
                    player.TaffPoints -= price;
                }

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
