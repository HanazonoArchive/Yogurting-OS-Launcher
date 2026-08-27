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
                    if (invItem == null && player.StarBeItems != null && uniqueId >= 0x100 && (uniqueId - 0x101) < player.StarBeItems.Count)
                    {
                        invItem = player.StarBeItems[uniqueId - 0x101];
                    }
                    typeId = invItem != null ? (invItem.TypeId > 0 ? invItem.TypeId : invItem.ItemId) : uniqueId;
                }
                else
                {
                    invItem = player.Inventory?.Find(i => i.Id == uniqueId || i.SerialId == rawUid)
                           ?? player.StarBeItems?.Find(i => i.Id == uniqueId || i.SerialId == rawUid);
                    if (invItem == null && player.Inventory != null && uniqueId >= 0x100 && (uniqueId - 0x101) < player.Inventory.Count)
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

                // 5. Dynamic Slot Resolution
                ushort targetSlot = itemDef?.GetTargetEquipSlot() ?? (uniqueId switch
                {
                    1 => 4, // Weapon
                    2 or 5 => 7, // Top
                    3 or 6 => 8, // Bottom
                    4 or 7 => 9, // Shoes
                    _ => (ushort)0
                });

                if (typeId == 0)
                {
                    typeId = YogurtingPackets.GetPlayerItemTypeId(player, uniqueId, targetSlot);
                }

                // 6. Handle previously equipped item in target slot (only for real equipment slots 1..9)
                if (targetSlot > 0 && targetSlot < player.EquippedSlotUids.Length && player.EquippedSlotUids[targetSlot] > 0)
                {
                    int prevUid = player.EquippedSlotUids[targetSlot];
                    var prevItem = player.Inventory?.Find(i => i.Id == prevUid);
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
                int baseModelTypeId = (itemDef != null && itemDef.BaseItemType > 0) ? itemDef.BaseItemType : (typeId > 1000000 && typeId % 10 == 4 ? typeId / 10 : typeId);

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
                await state.Session.SendAsync(YogurtingPackets.MakeGameStatDeltaNtf());
                await state.Session.SendAsync(YogurtingPackets.MakeGameSetStateNtf(player));
                await state.Session.SendAsync(YogurtingPackets.MakeGameChargePointUpdateNtf());
                await state.Session.SendAsync(YogurtingPackets.MakeGameAtkMovChangeNtf(player.CharaId, 1.0f, 1.0f));

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
                await state.Session.SendAsync(YogurtingPackets.MakeGameStatDeltaNtf());
                await state.Session.SendAsync(YogurtingPackets.MakeGameSetStateNtf(player));
                await state.Session.SendAsync(YogurtingPackets.MakeGameChargePointUpdateNtf());
                await state.Session.SendAsync(YogurtingPackets.MakeGameAtkMovChangeNtf(player.CharaId, 1.0f, 1.0f));

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
        /// 0x793B (31035): MsgGameItemUseReq - Use Consumable Item
        /// </summary>
        [PacketHandler(PacketOpcode.MsgGameItemUseReq)]
        public async Task HandleItemUseAsync(PlayerSessionState state, byte[] packetData)
        {
            try
            {
                state.Player.Hp = Math.Min(state.Player.MaxHp, state.Player.Hp + 50);
                await state.Session.SendAsync(YogurtingPackets.MakeGameStatDeltaNtf());
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
                await state.Session.SendAsync(YogurtingPackets.MakeGameStatDeltaNtf());
                await state.Session.SendAsync(YogurtingPackets.MakeGameSetStateNtf(player));
                await state.Session.SendAsync(YogurtingPackets.MakeGameChargePointUpdateNtf());
                await state.Session.SendAsync(YogurtingPackets.MakeGameAtkMovChangeNtf(player.CharaId, 1.0f, 1.0f));

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
                await state.Session.SendAsync(YogurtingPackets.MakeGameAtkMovChangeNtf(player.CharaId, 1.0f, 1.0f));
            }
            catch (Exception ex)
            {
                Logger.Error($"[FieldServer] ActionAttack error: {ex.Message}");
            }
        }

        /// <summary>
        /// 0x7928 (31016): MsgGameUseCoItemReq - Consumable Item Use Request (31016.dms: 消費アイテム使用要求)
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

                // Apply Consumable Benefits (Heal HP / SP) - 100% DB-driven from ByulItemType.txt / CoItemType.txt
                int healHp = 50;
                if (_gameDb != null && _gameDb.Items.TryGetValue(coItemType, out var itemDef))
                {
                    healHp = itemDef.RecoveryAmount > 0 
                        ? itemDef.RecoveryAmount 
                        : (itemDef.Price > 0 ? Math.Max(50, itemDef.Price / 5) : 70);
                }

                player.CurrentHp = Math.Min(player.MaxHp, player.CurrentHp + healHp);

                // 1. Reply with 0x7929 (31017: COITEM使用返答) - Result 1 = Success
                await state.Session.SendAsync(YogurtingPackets.MakeGameUseCoItemAns(player.CharaId, 1, coItemType, remainingCount));

                // 2. Sync updated HP / SP state (0x520F)
                await state.Session.SendAsync(YogurtingPackets.MakeGameSetStateNtf(player));

                // 3. Persist character
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
        /// 0x5273 (21107): MsgGameWeaponFrameReq - Weapon Socket Frame Info Request
        /// 0x5274 (21108): MsgGameWeaponFrameAns - Weapon Socket Frame Info Answer
        /// </summary>
        [PacketHandler(PacketOpcode.MsgGameWeaponFrameReq)]
        public async Task HandleWeaponFrameReqAsync(PlayerSessionState state, byte[] packetData)
        {
            try
            {
                var player = state.Player;
                if (player == null || packetData.Length < 10) return;

                int weaponId = BitConverter.ToInt32(packetData, 6);
                ushort serialLow = packetData.Length >= 12 ? BitConverter.ToUInt16(packetData, 10) : (ushort)0;
                ushort serialHigh = packetData.Length >= 14 ? BitConverter.ToUInt16(packetData, 12) : (ushort)0;
                int serialType = packetData.Length >= 18 ? BitConverter.ToInt32(packetData, 14) : 0;

                await state.Session.SendAsync(YogurtingPackets.MakeGameWeaponFrameAns(weaponId, serialLow, serialHigh, serialType));
            }
            catch (Exception ex)
            {
                Logger.Error($"[FieldServer] WeaponFrameReq error: {ex.Message}");
            }
        }

        public static int GetItemTypeId(Player player, int uniqueId, ushort slot) =>
            YogurtingPackets.GetPlayerItemTypeId(player, uniqueId, slot);
    }
}
