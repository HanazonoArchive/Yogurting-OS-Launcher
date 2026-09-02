using System;
using System.Threading.Tasks;
using Yogurting.Core.Logging;
using Yogurting.Core.Models;
using Yogurting.Core.Network;
using Yogurting.Data.Loaders;
using Yogurting.Data.Repositories;

namespace Yogurting.Server.Handlers.Field
{
    /// <summary>
    /// Handles Equipment, Inventory, and Item consumption actions in the Field Server.
    /// Matches Delphi Unit49 TChara.EquipBeItem (0x006106B0) and TChara.StripBeItem (0x006107E0).
    /// Fully database-driven with gender, school, and grade requirements enforcement.
    /// </summary>
    public sealed class EquipmentHandlers
    {
        private readonly Func<PlayerSessionState, byte[], Task> _broadcastDelegate;
        private readonly IAccountRepository? _repository;
        private readonly GameDatabase? _gameDb;

        public EquipmentHandlers(Func<PlayerSessionState, byte[], Task> broadcastDelegate, IAccountRepository? repository = null, GameDatabase? gameDb = null)
        {
            _broadcastDelegate = broadcastDelegate ?? throw new ArgumentNullException(nameof(broadcastDelegate));
            _repository = repository;
            _gameDb = gameDb;
        }

        /// <summary>
        /// 0x7944 (31044): MsgGameEquipReq / 0x5265 (21093): MsgGameEquipByulBeItemReq - Equip Item
        /// Exact match for Quartet live packet flow (0x7945 -> 0x520D -> 0x520F -> 0x791C -> 0x799F).
        /// </summary>
        [PacketHandler(PacketOpcode.MsgGameEquipReq)]
        [PacketHandler(PacketOpcode.MsgGameEquipByulBeItemReq)]
        public async Task HandleEquipItemAsync(PlayerSessionState state, byte[] packetData)
        {
            try
            {
                // Packet: [Header 6 bytes] [Int64 UniqueId 8B]
                ushort reqOpcode = packetData.Length >= 6 ? BitConverter.ToUInt16(packetData, 4) : (ushort)0x7944;
                long rawUid = packetData.Length >= 14 ? BitConverter.ToInt64(packetData, 6) : (packetData.Length >= 10 ? BitConverter.ToInt32(packetData, 6) : 1);
                int uniqueId = (int)((rawUid & 0xFFFFFFFF) != 0 ? (rawUid & 0xFFFFFFFF) : (rawUid >> 32));
                if (uniqueId == 0) uniqueId = 1;

                var player = state.Player;
                Item? invItem = null;
                int typeId = 0;

                if (reqOpcode == (ushort)PacketOpcode.MsgGameEquipByulBeItemReq)
                {
                    invItem = player.StarBeItems?.Find(i => i.Id == uniqueId || i.SerialId == rawUid)
                           ?? player.Inventory?.Find(i => i.Id == uniqueId || i.SerialId == rawUid);
                    if (invItem == null && player.StarBeItems != null && uniqueId >= 0x101 && (uniqueId - 0x101) < player.StarBeItems.Count)
                    {
                        invItem = player.StarBeItems[uniqueId - 0x101];
                    }
                    typeId = invItem != null ? (invItem.TypeId > 0 ? invItem.TypeId : invItem.ItemId) : uniqueId;
                }
                else
                {
                    invItem = player.Inventory?.Find(i => i.Id == uniqueId || i.SerialId == rawUid)
                           ?? player.StarBeItems?.Find(i => i.Id == uniqueId || i.SerialId == rawUid);
                    if (invItem == null && player.Inventory != null && uniqueId >= 0x101 && (uniqueId - 0x101) < player.Inventory.Count)
                    {
                        invItem = player.Inventory[uniqueId - 0x101];
                    }
                    typeId = invItem != null ? (invItem.TypeId > 0 ? invItem.TypeId : invItem.ItemId) : uniqueId;
                }

                GameItemDef? itemDef = null;
                if (typeId > 0 && _gameDb != null)
                {
                    _gameDb.Items.TryGetValue(typeId, out itemDef);
                }

                // 1. Validation: Item must exist
                if (invItem == null && reqOpcode != (ushort)PacketOpcode.MsgGameEquipByulBeItemReq)
                {
                    Logger.Warn($"[FieldServer] Equip rejected: Item UniqueID {uniqueId} not found in '{player.CharacterName}' inventory!");
                    return;
                }

                // 2. Validation: Gender Restriction (0 = Unisex, 1 = Male only, 2 = Female only)
                if (itemDef != null && itemDef.Sex != 0 && itemDef.Sex != (int)player.Gender)
                {
                    string reqGender = itemDef.Sex == 1 ? "Male" : "Female";
                    Logger.Warn($"[FieldServer] Equip rejected: '{itemDef.Name}' ({typeId}) is restricted to {reqGender} students! Character '{player.CharacterName}' is {player.Gender}.");
                    return;
                }

                // 3. Validation: School Restriction (0 = All schools, 1 = Estiva Academy, 2 = So-il Academy)
                if (itemDef != null && itemDef.School != 0 && itemDef.School != (int)player.School)
                {
                    string reqSchool = itemDef.School == 1 ? "Estiva Academy" : "So-il Academy";
                    Logger.Warn($"[FieldServer] Equip rejected: '{itemDef.Name}' ({typeId}) is restricted to {reqSchool}! Character '{player.CharacterName}' is {player.School}.");
                    return;
                }

                // 4. Validation: Grade/Level Requirement
                if (itemDef != null && itemDef.GradeReq > 0 && player.Grade < itemDef.GradeReq)
                {
                    Logger.Warn($"[FieldServer] Equip rejected: '{itemDef.Name}' ({typeId}) requires Grade {itemDef.GradeReq}! Character '{player.CharacterName}' is Grade {player.Grade}.");
                    return;
                }

                // 5. Dynamic Slot Resolution from Database
                ushort targetSlot = itemDef?.GetTargetEquipSlot() ?? 0;
                if (targetSlot == 0)
                {
                    Logger.Warn($"[FieldServer] Cannot equip item '{itemDef?.Name ?? "Unknown"}' ({typeId}): No valid equipment slot mapped.");
                    return;
                }

                if (typeId == 0)
                {
                    typeId = YogurtingPackets.GetPlayerItemTypeId(player, uniqueId, targetSlot);
                }

                // 6. Handle previously equipped item in target slot (only for real equipment slots 1..9)
                if (targetSlot > 0 && targetSlot < player.EquippedSlotUids.Length && player.EquippedSlotUids[targetSlot] > 0)
                {
                    int prevUid = player.EquippedSlotUids[targetSlot];
                    var prevItem = player.Inventory?.Find(i => i.Id == prevUid)
                                ?? player.StarBeItems?.Find(i => i.Id == prevUid);
                    if (prevItem != null)
                    {
                        prevItem.IsEquipped = false;
                        prevItem.SlotType = ItemSlotType.Inventory;
                    }
                }

                // If equipping a full-body dress (EquipPos 192), unequip lower pants/skirt (Slot 8) as well
                if (itemDef != null && (itemDef.EquipPos == 192 || (itemDef.EquipPos & 192) == 192))
                {
                    if (player.EquippedSlotUids.Length > 8 && player.EquippedSlotUids[8] > 0)
                    {
                        int prevLowerUid = player.EquippedSlotUids[8];
                        var prevLower = player.Inventory?.Find(i => i.Id == prevLowerUid);
                        if (prevLower != null)
                        {
                            prevLower.IsEquipped = false;
                            prevLower.SlotType = ItemSlotType.Inventory;
                        }
                        player.EquippedSlotUids[8] = 0;
                    }
                }

                // 7. Equip new item (only update player.EquippedSlotUids if it is a clothing/weapon slot 1..9)
                if (targetSlot > 0 && targetSlot < player.EquippedSlotUids.Length)
                {
                    player.EquippedSlotUids[targetSlot] = uniqueId;
                    if (targetSlot < player.EquippedSlotIsStar.Length)
                    {
                        player.EquippedSlotIsStar[targetSlot] = (reqOpcode == (ushort)PacketOpcode.MsgGameEquipByulBeItemReq) || (typeId > 1000000);
                    }
                }
                if (invItem != null && targetSlot > 0)
                {
                    invItem.IsEquipped = true;
                    invItem.SlotType = ItemSlotType.Equipment;
                    invItem.SlotIndex = targetSlot;
                }
                else if (reqOpcode == (ushort)PacketOpcode.MsgGameEquipByulBeItemReq && targetSlot > 0)
                {
                    invItem = new Item
                    {
                        Id = uniqueId,
                        ItemId = typeId,
                        TypeId = typeId,
                        Name = itemDef?.Name ?? $"Star Item {typeId}",
                        Quantity = 1,
                        SerialId = rawUid,
                        SlotIndex = targetSlot,
                        SlotType = ItemSlotType.Equipment,
                        IsEquipped = true
                    };
                    (player.StarBeItems ??= new List<Item>()).Add(invItem);
                }

                Logger.Info($"[FieldServer] '{player.CharacterName}' equipped item (UID {uniqueId} [Raw {rawUid}], Type {typeId} '{itemDef?.Name ?? "Item"}', Slot {targetSlot})");

                // 8. Send equip answer (Client 3D mesh update)
                PacketOpcode ansOpcode = reqOpcode == (ushort)PacketOpcode.MsgGameEquipByulBeItemReq 
                    ? PacketOpcode.MsgGameEquipByulBeItemAns 
                    : PacketOpcode.MsgGameEquipAns;

                int equipAnsTypeId = reqOpcode == (ushort)PacketOpcode.MsgGameEquipByulBeItemReq ? typeId : ((itemDef != null && itemDef.BaseItemType > 0) ? itemDef.BaseItemType : typeId);
                int baseModelTypeId = (itemDef != null && itemDef.BaseItemType > 0) ? itemDef.BaseItemType : typeId;

                byte[] ans = YogurtingPackets.MakeGameEquipAns(player.CharaId, rawUid, equipAnsTypeId, ansOpcode);
                await state.Session.SendAsync(ans);
                await _broadcastDelegate(state, ans);

                // If weapon equipped (Slot 4), sync weapon frame (0x5274) with valid model type
                if (targetSlot == 4)
                {
                    byte[] wpnFrame = YogurtingPackets.MakeGameWeaponFrameAns(baseModelTypeId, (ushort)rawUid, 0, 1);
                    await state.Session.SendAsync(wpnFrame);
                    await _broadcastDelegate(state, wpnFrame);
                }

                // 9. Update Stats & State (Exact sequence matching Quartet live packet capture)
                await state.Session.SendAsync(YogurtingPackets.MakeGameSetHpNtf((ushort)player.CurrentHp));
                await state.Session.SendAsync(YogurtingPackets.MakeGameSetStateNtf(player));
                await state.Session.SendAsync(YogurtingPackets.MakeGameChargePointUpdateNtf(player.ChargePoint, player.GaugeMax, player.GaugeCurrent));
                await state.Session.SendAsync(YogurtingPackets.MakeGameAtkMovChangeNtf(player.CharaId, player.AtkSpeedF, player.MoveSpeedF));

                if (ansOpcode == PacketOpcode.MsgGameEquipByulBeItemAns)
                {
                    await state.Session.SendAsync(YogurtingPackets.MakeGameUseByulBeItemStartNtf(rawUid, 0));
                }

                // 10. Persist Character Equipment State
                if (_repository != null)
                {
                    await _repository.SaveAsync(player);
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"[FieldServer] Equip error: {ex.Message}");
            }
        }

        /// <summary>
        /// 0x7946 (31046): MsgGameUnequipReq / 0x5267 (21095): MsgGameStripByulBeItemReq - Unequip Item
        /// Exact match for Quartet live packet flow (0x520D -> 0x520F -> 0x791C -> 0x799F -> 0x7947).
        /// </summary>
        [PacketHandler(PacketOpcode.MsgGameUnequipReq)]
        [PacketHandler(PacketOpcode.MsgGameStripByulBeItemReq)]
        public async Task HandleUnequipItemAsync(PlayerSessionState state, byte[] packetData)
        {
            try
            {
                // Packet: [Header 6 bytes] [Int64 UniqueId 8B]
                ushort reqOpcode = packetData.Length >= 6 ? BitConverter.ToUInt16(packetData, 4) : (ushort)0x7946;
                long rawUid = packetData.Length >= 14 ? BitConverter.ToInt64(packetData, 6) : (packetData.Length >= 10 ? BitConverter.ToInt32(packetData, 6) : 1);
                int uniqueId = (int)((rawUid & 0xFFFFFFFF) != 0 ? (rawUid & 0xFFFFFFFF) : (rawUid >> 32));
                if (uniqueId == 0) uniqueId = 1;

                var player = state.Player;
                Item? invItem = null;
                int typeId = 0;

                invItem = player.StarBeItems?.Find(i => i.Id == uniqueId || i.SerialId == rawUid)
                       ?? player.Inventory?.Find(i => i.Id == uniqueId || i.SerialId == rawUid);
                typeId = invItem != null ? (invItem.TypeId > 0 ? invItem.TypeId : invItem.ItemId) : 0;

                // Locate which equip slot held this uniqueId
                ushort targetSlot = 0;
                for (ushort s = 1; s <= 9; s++)
                {
                    if (s < player.EquippedSlotUids.Length && player.EquippedSlotUids[s] == uniqueId)
                    {
                        targetSlot = s;
                        break;
                    }
                }

                if (targetSlot == 0 && invItem != null && invItem.SlotIndex > 0)
                {
                    targetSlot = (ushort)invItem.SlotIndex;
                }

                if (typeId == 0)
                {
                    typeId = YogurtingPackets.GetPlayerItemTypeId(player, uniqueId, targetSlot);
                }

                // Clear equipped slot in player state
                if (targetSlot > 0 && targetSlot < player.EquippedSlotUids.Length)
                {
                    player.EquippedSlotUids[targetSlot] = 0;
                    if (targetSlot < player.EquippedSlotIsStar.Length)
                    {
                        player.EquippedSlotIsStar[targetSlot] = false;
                    }
                }
                if (invItem != null)
                {
                    invItem.IsEquipped = false;
                    invItem.SlotType = ItemSlotType.Inventory;
                }

                Logger.Info($"[FieldServer] '{player.CharacterName}' unequipped item (UID {uniqueId}, Slot {targetSlot}, Type {typeId})");

                // 1. Update Stats & State FIRST (Exact sequence matching Quartet live capture)
                await state.Session.SendAsync(YogurtingPackets.MakeGameSetHpNtf((ushort)player.CurrentHp));
                await state.Session.SendAsync(YogurtingPackets.MakeGameSetStateNtf(player));
                await state.Session.SendAsync(YogurtingPackets.MakeGameChargePointUpdateNtf(player.ChargePoint, player.GaugeMax, player.GaugeCurrent));
                await state.Session.SendAsync(YogurtingPackets.MakeGameAtkMovChangeNtf(player.CharaId, player.AtkSpeedF, player.MoveSpeedF));

                // 2. Send unequip answer (Client 3D mesh update)
                PacketOpcode ansOpcode = reqOpcode == (ushort)PacketOpcode.MsgGameStripByulBeItemReq 
                    ? PacketOpcode.MsgGameStripByulBeItemAns 
                    : PacketOpcode.MsgGameUnequipAns;

                byte[] ans = YogurtingPackets.MakeGameUnequipAns(player.CharaId, rawUid, typeId, ansOpcode);
                await state.Session.SendAsync(ans);
                await _broadcastDelegate(state, ans);

                // 3. Persist Character Equipment State
                if (_repository != null)
                {
                    await _repository.SaveAsync(player);
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"[FieldServer] Unequip error: {ex.Message}");
            }
        }

        /// <summary>
        /// 0x793B (31035): MsgGameItemUseReq - Use Consumable Item or Interact with Field Object (Book of Knowledge)
        /// </summary>
        [PacketHandler(PacketOpcode.MsgGameItemUseReq)]
        public async Task HandleItemUseAsync(PlayerSessionState state, byte[] packetData)
        {
            try
            {
                int itemId = packetData.Length >= 10 ? BitConverter.ToInt32(packetData, 6) : 0;

                // If itemId has an NPC/object dialogue script (e.g. Book of Knowledge #600 / #486)
                if (_gameDb != null && (_gameDb.NpcScripts.ContainsKey(itemId) || itemId == 600 || itemId == 486))
                {
                    if (_repository != null)
                    {
                        var npcHandler = new NpcAndDialogueHandlers(_gameDb, _repository, _broadcastDelegate);
                        await npcHandler.HandleNpcDialogReqAsync(state, packetData);
                    }
                    return;
                }

                state.Player.Hp = Math.Min(state.Player.MaxHp, state.Player.Hp + 50);
                await state.Session.SendAsync(YogurtingPackets.MakeGameSetHpNtf((ushort)state.Player.CurrentHp));
                await state.Session.SendAsync(YogurtingPackets.MakeGameSetStateNtf(state.Player));
            }
            catch (Exception ex)
            {
                Logger.Error($"[FieldServer] Item use error: {ex.Message}");
            }
        }

        /// <summary>
        /// 0x524B (21067): MsgGameUseByulBeItemReq - Activate / Consume Star Cash Item or Buff
        /// Delphi 0x006C0614 -> 0x005A9E71 (0x524C: MsgGameUseByulBeItemAns)
        /// </summary>
        [PacketHandler(PacketOpcode.MsgGameUseByulBeItemReq)]
        public async Task HandleUseByulBeItemAsync(PlayerSessionState state, byte[] packetData)
        {
            try
            {
                long rawUid = packetData.Length >= 14 ? BitConverter.ToInt64(packetData, 6) : (packetData.Length >= 10 ? BitConverter.ToInt32(packetData, 6) : 0);
                int uniqueId = (int)((rawUid & 0xFFFFFFFF) != 0 ? (rawUid & 0xFFFFFFFF) : (rawUid >> 32));
                var player = state.Player;
                if (player == null) return;

                var invItem = player.StarBeItems?.Find(i => i.Id == uniqueId || i.SerialId == rawUid || i.TypeId == uniqueId)
                           ?? player.Inventory?.Find(i => i.Id == uniqueId || i.SerialId == rawUid || i.TypeId == uniqueId);

                int typeId = invItem != null ? (invItem.TypeId > 0 ? invItem.TypeId : invItem.ItemId) : uniqueId;
                GameItemDef? itemDef = null;
                if (typeId > 0 && _gameDb != null)
                {
                    _gameDb.Items.TryGetValue(typeId, out itemDef);
                }

                Logger.Info($"[FieldServer] '{player.CharacterName}' activated Star Item / Buff (UID {uniqueId} [Raw {rawUid}], Type {typeId} '{itemDef?.Name ?? "Star Item"}')");

                bool isTimedBuff = itemDef != null && (itemDef.DurationDays > 0 || itemDef.EffectId > 0);
                int durationMinutes = itemDef?.DurationDays > 0 ? itemDef.DurationDays * 1440 : 30 * 1440;
                int durationSeconds = durationMinutes * 60;
                int effectType = itemDef?.EffectId > 0 ? itemDef.EffectId : (itemDef?.SkillId > 0 ? itemDef.SkillId : typeId);

                // 1. Send Use Answer (0x524C)
                await state.Session.SendAsync(YogurtingPackets.MakeGameUseByulBeItemAns(player.CharaId, rawUid, typeId, 0));

                // 2. Consume Item from Inventory (Both consumables and buff tickets are consumed on activation)
                if (invItem != null)
                {
                    if (invItem.Quantity > 1)
                    {
                        invItem.Quantity--;
                    }
                    else
                    {
                        player.StarBeItems?.Remove(invItem);
                        player.Inventory?.RemoveAll(i => i.Id == invItem.Id || i.SerialId == invItem.SerialId);
                    }
                }

                // 3. Register Active Buff on Player
                if (isTimedBuff)
                {
                    var existing = player.ActiveBuffs.Find(b => b.EffectType == effectType);
                    if (existing != null)
                    {
                        existing.DurationSeconds += durationSeconds;
                    }
                    else
                    {
                        player.ActiveBuffs.Add(new ActiveBuff
                        {
                            EffectType = effectType,
                            DurationSeconds = durationSeconds,
                            ActivatedAt = DateTime.UtcNow
                        });
                    }
                    Logger.Info($"[FieldServer] '{player.CharacterName}' activated Buff #{effectType} for {durationMinutes / 1440} days (Remaining: {player.ActiveBuffs.Find(b => b.EffectType == effectType)?.RemainingSeconds}s)");
                }

                // 4. Apply Consumable Benefits (100% DB-driven from ByulItemType.txt Column 8)
                if (itemDef != null && itemDef.RecoveryAmount > 0)
                {
                    player.Hp = Math.Min(player.MaxHp, player.Hp + itemDef.RecoveryAmount);
                }

                // 5. Stat & State Update
                await state.Session.SendAsync(YogurtingPackets.MakeGameSetHpNtf((ushort)player.CurrentHp));
                await state.Session.SendAsync(YogurtingPackets.MakeGameSetStateNtf(player));
                await state.Session.SendAsync(YogurtingPackets.MakeGameChargePointUpdateNtf(player.ChargePoint, player.GaugeMax, player.GaugeCurrent));
                await state.Session.SendAsync(YogurtingPackets.MakeGameAtkMovChangeNtf(player.CharaId, player.AtkSpeedF, player.MoveSpeedF));

                // 6. Duration active notice (0x5269)
                if (isTimedBuff)
                {
                    await state.Session.SendAsync(YogurtingPackets.MakeGameUseByulBeItemStartNtf(rawUid, durationMinutes));
                }

                // 7. Persist character state
                if (_repository != null)
                {
                    await _repository.SaveAsync(player);
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"[FieldServer] UseByulBeItem error: {ex.Message}");
            }
        }

        /// <summary>
        /// 0xA031 (41009): MsgGameActionAttackReq - Basic Attack / Action Request
        /// </summary>
        [PacketHandler(PacketOpcode.MsgGameActionAttackReq)]
        public async Task HandleActionAttackAsync(PlayerSessionState state, byte[] packetData)
        {
            try
            {
                var player = state.Player;
                if (player == null) return;
                // Acknowledge action and maintain player combat state
                await state.Session.SendAsync(YogurtingPackets.MakeGameAtkMovChangeNtf(player.CharaId, player.AtkSpeedF, player.MoveSpeedF));
            }
            catch (Exception ex)
            {
                Logger.Error($"[FieldServer] ActionAttack error: {ex.Message}");
            }
        }

        /// <summary>
        /// 0x7928 (31016): MsgGameUseCoItemReq - Consumable Item Use Request (31016.dms: 消費アイテム使用要求)
        /// Exact 1-to-1 match with Delphi TChara.UseCoItem (_Unit49.pas:0061019C)
        /// </summary>
        [PacketHandler(PacketOpcode.MsgGameUseCoItemReq)]
        public async Task HandleUseCoItemAsync(PlayerSessionState state, byte[] packetData)
        {
            try
            {
                var player = state.Player;
                if (player == null || packetData.Length < 10) return;

                int coItemType = BitConverter.ToInt32(packetData, 6);
                Logger.Info($"[FieldServer] '{player.CharacterName}' used Consumable CoItem Type {coItemType}");

                int remainingCount = 0;
                var invItem = player.Inventory?.Find(i => i.TypeId == coItemType || i.Id == coItemType);
                if (invItem != null)
                {
                    if (invItem.Quantity > 1)
                    {
                        invItem.Quantity--;
                        remainingCount = invItem.Quantity;
                    }
                    else
                    {
                        player.Inventory?.Remove(invItem);
                        remainingCount = 0;
                    }
                }

                // 1. Determine Consumable Effect & Healing Amount dynamically from GameDatabase
                GameItemDef? coDef = null;
                _gameDb?.Items.TryGetValue(coItemType, out coDef);

                int healHp = 150;
                if (coDef != null && coDef.RecoveryAmount > 0)
                {
                    healHp = coDef.RecoveryAmount;
                }
                else if (coDef != null && coDef.Description.Contains("全回復"))
                {
                    healHp = player.MaxHp;
                }

                bool isInstant = coDef?.QuickUsable == true || (coItemType >= 20000 && coItemType < 30000);
                bool isBox = coDef?.UseType == 2 || (coItemType >= 40000 && coItemType < 50000);

                // Handle Loot Boxes (UseType 2: バトルボックス / 源石ボックス)
                if (isBox)
                {
                    int rewardItemId = 101100; // 翡翠・刀源石 1-C default
                    if (_gameDb != null && _gameDb.ReinforceStones.Count > 0)
                    {
                        var stoneKeys = _gameDb.ReinforceStones.Keys.ToList();
                        rewardItemId = stoneKeys[Random.Shared.Next(stoneKeys.Count)];
                    }
                    else if (_gameDb != null && _gameDb.ShopItems.Count > 0)
                    {
                        rewardItemId = _gameDb.ShopItems[Random.Shared.Next(_gameDb.ShopItems.Count)].ItemId;
                    }

                    var existing = player.Inventory?.Find(i => i.TypeId == rewardItemId);
                    if (existing != null)
                    {
                        existing.Quantity += 1;
                    }
                    else
                    {
                        player.Inventory?.Add(new Item
                        {
                            Id = ((player.Inventory?.Count > 0) ? player.Inventory.Max(i => i.Id) : 0) + 1,
                            TypeId = rewardItemId,
                            SlotIndex = player.Inventory?.Count ?? 0,
                            SlotType = ItemSlotType.Inventory,
                            Quantity = 1,
                            Name = _gameDb != null && _gameDb.Items.TryGetValue(rewardItemId, out var rDef) ? rDef.Name : "Box Reward"
                        });
                    }
                    Logger.Info($"[FieldServer] '{player.CharacterName}' unboxed #{coItemType} and obtained Item #{rewardItemId}!");
                }
                else if (isInstant)
                {
                    // Instant Heal (0x520D + 0x520F)
                    player.CurrentHp = Math.Min(player.MaxHp, player.CurrentHp + healHp);
                    await state.Session.SendAsync(YogurtingPackets.MakeGameSetStateNtf(player));
                    await state.Session.SendAsync(YogurtingPackets.MakeGameSetHpNtf((ushort)player.CurrentHp));
                }
                else
                {
                    // Gradual Potion Regain over 5s (0x520E: MsgGameGeneralPotionNtf)
                    player.CurrentHp = Math.Min(player.MaxHp, player.CurrentHp + healHp);
                    await state.Session.SendAsync(YogurtingPackets.MakeGameGeneralPotionNtf((ushort)healHp, healHp / 5.0f));
                    await state.Session.SendAsync(YogurtingPackets.MakeGameSetStateNtf(player));
                    await state.Session.SendAsync(YogurtingPackets.MakeGameSetHpNtf((ushort)player.CurrentHp));
                }

                // 2. Broadcast 0x7929 (31017: COITEM使用返答) to area for player consuming animation
                byte[] ansPkt = YogurtingPackets.MakeGameUseCoItemAns(player.CharaId, 1, coItemType, remainingCount);
                await state.Session.SendAsync(ansPkt);
                await _broadcastDelegate(state, ansPkt);

                // 3. Persist character progression
                if (_repository != null)
                {
                    await _repository.SaveAccountAsync(player);
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"[FieldServer] UseCoItem error: {ex.Message}");
            }
        }

        /// <summary>
        /// 0x5274 (21108): MsgGameWeaponFrameAns - Client Weapon Socket & Skill Frames Response
        /// Server acknowledges weapon combat stance and updates combat stats (0x7945 -> 0x520D -> 0x520F -> 0x791C -> 0x799F).
        /// </summary>
        [PacketHandler(PacketOpcode.MsgGameWeaponFrameAns)]
        public async Task HandleWeaponFrameAnsAsync(PlayerSessionState state, byte[] packetData)
        {
            try
            {
                var player = state.Player;
                if (player == null) return;

                int rawType = packetData.Length >= 10 ? BitConverter.ToInt32(packetData, 6) : 0;
                int weaponTypeId = rawType & 0x00FFFFFF;
                long weaponUid = packetData.Length >= 18 ? BitConverter.ToInt64(packetData, 10) : 1;
                if (weaponUid == 0) weaponUid = 1;

                if (weaponTypeId == 0)
                {
                    weaponTypeId = YogurtingPackets.GetPlayerItemTypeId(player, (int)weaponUid, 4);
                    // HARDCODED: Fallback default weapon type ID (Wooden Practice Blade 140001) when client passes 0
                    if (weaponTypeId == 0) weaponTypeId = 140001;
                }

                Logger.Info($"[FieldServer] '{player.CharacterName}' synchronized weapon frame for Weapon #{weaponTypeId} (UID {weaponUid})");

                // 1. Confirm combat weapon equip (0x7945)
                byte[] equipAns = YogurtingPackets.MakeGameEquipAns(player.CharaId, weaponUid, weaponTypeId, PacketOpcode.MsgGameEquipAns);
                await state.Session.SendAsync(equipAns);
                await _broadcastDelegate(state, equipAns);

                // 2. Stat & Combat State Synchronization
                await state.Session.SendAsync(YogurtingPackets.MakeGameSetHpNtf((ushort)player.CurrentHp));
                await state.Session.SendAsync(YogurtingPackets.MakeGameSetStateNtf(player));
                await state.Session.SendAsync(YogurtingPackets.MakeGameChargePointUpdateNtf(player.ChargePoint, player.GaugeMax, player.GaugeCurrent));
                await state.Session.SendAsync(YogurtingPackets.MakeGameAtkMovChangeNtf(player.CharaId, player.AtkSpeedF, player.MoveSpeedF));

                // 3. Exact Quartet response to 0x5274: Weapon Frame Request (0x5273)
                await state.Session.SendAsync(YogurtingPackets.MakeGameWeaponFrameInfoReq(rawType > 0 ? rawType : weaponTypeId, (int)weaponUid));
            }
            catch (Exception ex)
            {
                Logger.Error($"[FieldServer] WeaponFrameAns error: {ex.Message}");
            }
        }

        /// <summary>
        /// 0x79A2 (31138): MsgGameEquipTitleReq - Player equips a title
        /// SRC: server_legacy/DELPHI PROJECT/_Unit67.pas:006C4290 & _Unit47.pas:005AED10
        /// Payload: Int32 TitleId
        /// </summary>
        [PacketHandler(PacketOpcode.MsgGameEquipTitleReq)]
        public async Task HandleEquipTitleAsync(PlayerSessionState state, PacketReader reader)
        {
            if (reader.Remaining < 4) return;
            int titleId = reader.ReadInt32();

            var player = state.Player;
            player.TitleId = titleId;
            if (!player.UnlockedTitles.Contains(titleId))
            {
                player.UnlockedTitles.Add(titleId);
            }

            Logger.Info($"[Title] '{player.CharacterName}' equipped Title #{titleId}.");

            // Dispatch 0x79A3 (MsgGameEquipTitleAns) to player and surrounding area
            byte[] titleAns = YogurtingPackets.MakeGameEquipTitleAns(player.CharacterId, titleId);
            await state.Session.SendAsync(titleAns);
            await _broadcastDelegate(state, titleAns);

            // Persist
            if (_repository != null)
            {
                _ = Task.Run(() => _repository.SaveAccountAsync(player));
            }
        }

        /// <summary>
        /// 0x79A4 (31140): MsgGameStripTitleReq - Player unequips active title
        /// SRC: server_legacy/DELPHI PROJECT/_Unit67.pas:006C4320 & _Unit47.pas:005AED84
        /// Payload: Int32 TitleId
        /// </summary>
        [PacketHandler(PacketOpcode.MsgGameStripTitleReq)]
        public async Task HandleStripTitleAsync(PlayerSessionState state, PacketReader reader)
        {
            var player = state.Player;
            int oldTitleId = player.TitleId;
            player.TitleId = 0;

            Logger.Info($"[Title] '{player.CharacterName}' unequipped Title #{oldTitleId}.");

            // Dispatch 0x79A5 (MsgGameStripTitleAns) to player and surrounding area
            byte[] stripAns = YogurtingPackets.MakeGameStripTitleAns(player.CharacterId, 0);
            await state.Session.SendAsync(stripAns);
            await _broadcastDelegate(state, stripAns);

            // Persist
            if (_repository != null)
            {
                _ = Task.Run(() => _repository.SaveAccountAsync(player));
            }
        }

        /// <summary>
        /// 0x7A04 (31236): MsgGameItemDiscardReq - Player discards / trashes an item from inventory
        /// Exact Delphi layout (_Unit67.pas:23418-23500 TSchoolSession.sub_006C427C / 31236.dms):
        ///   ReadInt32(typeItem_typeNum)
        ///   ReadWord(dim1Index)
        ///   ReadWord(dim2Index)
        ///   ReadInt32(itemId)
        /// </summary>
        [PacketHandler(PacketOpcode.MsgGameItemDiscardReq)]
        public async Task HandleItemDiscardAsync(PlayerSessionState state, PacketReader reader)
        {
            try
            {
                if (reader.Remaining < 12) return;

                int rawType = reader.ReadInt32();
                ushort dim1 = reader.ReadUInt16();
                ushort dim2 = reader.ReadUInt16();
                int itemId = reader.ReadInt32();

                int itemTypeId = rawType & 0x00FFFFFF;
                var player = state.Player;

                Logger.Info($"[FieldServer] '{player.CharacterName}' discarding Item (RawType=0x{rawType:X8}, TypeId={itemTypeId}, Dim1={dim1}, Dim2={dim2}, ItemId={itemId})");

                Item? targetItem = null;
                // 1. Try finding by slot/dim1 index
                if (dim1 < player.Inventory.Count)
                {
                    targetItem = player.Inventory[dim1];
                }

                // 2. Fallback: match by ItemId or TypeId
                if (targetItem == null && itemId > 0)
                {
                    targetItem = player.Inventory.FirstOrDefault(i => i.Id == itemId || i.SerialId == itemId);
                }
                if (targetItem == null && itemTypeId > 0)
                {
                    targetItem = player.Inventory.FirstOrDefault(i => i.TypeId == itemTypeId);
                }

                if (targetItem != null)
                {
                    if (targetItem.Quantity > 1)
                    {
                        targetItem.Quantity--;
                        Logger.Info($"[FieldServer] Decremented '{targetItem.Name}' (#{targetItem.TypeId}) quantity to {targetItem.Quantity} for '{player.CharacterName}'.");
                    }
                    else
                    {
                        player.Inventory.Remove(targetItem);
                        Logger.Info($"[FieldServer] Removed '{targetItem.Name}' (#{targetItem.TypeId}) from inventory for '{player.CharacterName}'.");
                    }

                    // Save account state
                    if (_repository != null)
                    {
                        await _repository.SaveAccountAsync(player);
                    }
                }
                else
                {
                    Logger.Warn($"[FieldServer] Discard failed: Item not found in inventory for '{player.CharacterName}'.");
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"[FieldServer] ItemDiscard error: {ex.Message}");
            }
        }

        /// <summary>
        /// 0x7937 (31031): MsgGameItemDropReq - Drop Item to Floor
        /// SRC: server_legacy/DELPHI PROJECT/_Unit67.pas:006C21FC (TSchoolSession.sub_006C21FC)
        /// Payload: ReadInt32(typeId), ReadInt32(count)
        /// </summary>
        [PacketHandler(PacketOpcode.MsgGameItemDropReq)]
        public async Task HandleItemDropAsync(PlayerSessionState state, PacketReader reader)
        {
            try
            {
                if (reader.Remaining < 8) return;
                int typeId = reader.ReadInt32();
                int count = Math.Max(1, reader.ReadInt32());
                var player = state.Player;

                var targetItem = player.Inventory.FirstOrDefault(i => i.TypeId == typeId);
                if (targetItem != null)
                {
                    if (targetItem.Quantity > count)
                    {
                        targetItem.Quantity -= count;
                        Logger.Info($"[FieldServer] '{player.CharacterName}' dropped {count}x '{targetItem.Name}' (Remaining: {targetItem.Quantity}).");
                    }
                    else
                    {
                        player.Inventory.Remove(targetItem);
                        Logger.Info($"[FieldServer] '{player.CharacterName}' dropped all '{targetItem.Name}' to floor.");
                    }

                    if (_repository != null)
                    {
                        await _repository.SaveAccountAsync(player);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"[FieldServer] ItemDrop error: {ex.Message}");
            }
        }

        public static int GetItemTypeId(Player player, int uniqueId, ushort slot) =>
            YogurtingPackets.GetPlayerItemTypeId(player, uniqueId, slot);
    }
}
