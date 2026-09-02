using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Yogurting.Core.Logging;
using Yogurting.Core.Models;
using Yogurting.Core.Network;
using Yogurting.Data.Loaders;
using Yogurting.Data.Repositories;

namespace Yogurting.Server.Handlers.Field
{
    /// <summary>
    /// Storage Locker, Item Reinforcement, Crystal Enchanting, and Respawn Handlers.
    /// Reverse-engineered from Delphi Quartet server logic
    /// (server_legacy/DELPHI PROJECT/_Unit49.pas:20800-21800, _Unit67.pas:006C2610-006C5300 & _Unit47.pas).
    /// </summary>
    public sealed class StorageAndRefinementHandlers
    {
        private readonly Func<PlayerSessionState, byte[], Task> _broadcastDelegate;
        private readonly IAccountRepository? _repository;
        private readonly GameDatabase? _gameDb;
        private readonly object _storageLock = new();

        public StorageAndRefinementHandlers(
            Func<PlayerSessionState, byte[], Task> broadcastDelegate,
            IAccountRepository? repository = null,
            GameDatabase? gameDb = null)
        {
            _broadcastDelegate = broadcastDelegate;
            _repository = repository;
            _gameDb = gameDb;
        }

        /// <summary>
        /// 0xA02B (41003): MsgGameLockerOpenReq - Open Storage Locker
        /// SRC: server_legacy/DELPHI PROJECT/_Unit67.pas:006C5200 & 41003.dms
        /// Payload: Int32 LockerID
        /// </summary>
        [PacketHandler(PacketOpcode.MsgGameLockerOpenReq)]
        public async Task HandleLockerOpenReqAsync(PlayerSessionState state, PacketReader reader)
        {
            int lockerId = reader.Remaining >= 4 ? reader.ReadInt32() : 1;
            var player = state.Player;

            Logger.Info($"[Locker] '{player.CharacterName}' opened Storage Locker #{lockerId}.");

            // 1. Send Open Confirmation (0xA02C)
            await state.Session.SendAsync(YogurtingPackets.MakeGameLockerOpenAns(lockerId, 1));

            // 2. Synchronize Locker contents (0xA02D)
            await state.Session.SendAsync(YogurtingPackets.MakeGameLockerItemInfoNtf(lockerId, player.LockerItems));
        }

        /// <summary>
        /// 0x794A (31050): MsgGameReinforceItemReq - Reinforce / Socket item with enhancement stone
        /// SRC: server_legacy/DELPHI PROJECT/_Unit49.pas:21500-21800 & _Unit67.pas:006C2610
        /// </summary>
        [PacketHandler(PacketOpcode.MsgGameReinforceItemReq)]
        public async Task HandleReinforceItemReqAsync(PlayerSessionState state, PacketReader reader)
        {
            if (reader.Remaining < 8) return;

            int targetItemTypeId = reader.ReadInt32();
            int stoneItemTypeId = reader.ReadInt32();
            int socketIdx = reader.Remaining >= 4 ? reader.ReadInt32() : 0;

            var player = state.Player;
            Item? targetItem;

            lock (_storageLock)
            {
                targetItem = player.Inventory.FirstOrDefault(i => i.TypeId == targetItemTypeId)
                             ?? player.Equips.FirstOrDefault(i => i.TypeId == targetItemTypeId);

                var stoneItem = player.Inventory.FirstOrDefault(i => i.TypeId == stoneItemTypeId);

                if (targetItem == null || stoneItem == null)
                {
                    return;
                }

                // Consume 1 stone
                if (stoneItem.Quantity > 1)
                {
                    stoneItem.Quantity--;
                }
                else
                {
                    player.Inventory.Remove(stoneItem);
                }

                // Attach to socket (0..4)
                if (socketIdx >= 0 && socketIdx < 5)
                {
                    targetItem.SocketSlots[socketIdx] = stoneItemTypeId;
                }
                else
                {
                    for (int i = 0; i < 5; i++)
                    {
                        if (targetItem.SocketSlots[i] == 0)
                        {
                            targetItem.SocketSlots[i] = stoneItemTypeId;
                            socketIdx = i;
                            break;
                        }
                    }
                }
            }

            Logger.Info($"[Reinforce] '{player.CharacterName}' socketed Stone #{stoneItemTypeId} into Item #{targetItemTypeId} (Slot {socketIdx}).");

            // Dispatch 0x79F0 (MsgGameReinforceBeItemAttachStoneAns)
            await state.Session.SendAsync(
                YogurtingPackets.MakeGameReinforceBeItemAttachStoneAns(player.CharacterId, targetItem, stoneItemTypeId, socketIdx));

            // Sync updated inventory
            await state.Session.SendAsync(YogurtingPackets.MakeGameUpdateItemNtf(player));

            if (_repository != null)
            {
                _ = Task.Run(() => _repository.SaveAccountAsync(player));
            }
        }

        /// <summary>
        /// 0x79A8 (31144): MsgGameEnchantCrystalReq - Crystal Enchant Level upgrade
        /// SRC: server_legacy/DELPHI PROJECT/_Unit67.pas:006C4350 & 31144.dms
        /// </summary>
        [PacketHandler(PacketOpcode.MsgGameEnchantCrystalReq)]
        public async Task HandleEnchantCrystalReqAsync(PlayerSessionState state, PacketReader reader)
        {
            if (reader.Remaining < 4) return;
            int type = reader.ReadInt32();
            ushort itemCount = reader.Remaining >= 2 ? reader.ReadUInt16() : (ushort)0;

            var player = state.Player;
            int newLevel = 1;

            lock (_storageLock)
            {
                for (int i = 0; i < itemCount && reader.Remaining >= 12; i++)
                {
                    int consumeTypeId = reader.ReadInt32();
                    int count = reader.ReadInt32();
                    reader.ReadInt32(); // skip invalid

                    var consumeItem = player.Inventory.FirstOrDefault(it => it.TypeId == consumeTypeId);
                    if (consumeItem != null)
                    {
                        if (consumeItem.Quantity > count)
                            consumeItem.Quantity -= count;
                        else
                            player.Inventory.Remove(consumeItem);
                    }
                }
            }

            Logger.Info($"[Crystal] '{player.CharacterName}' upgraded Crystal Type {type} to Level {newLevel}.");

            // Dispatch 0x79A9 (MsgGameEnchantCrystalAns)
            await state.Session.SendAsync(YogurtingPackets.MakeGameEnchantCrystalAns(type, newLevel));
            await state.Session.SendAsync(YogurtingPackets.MakeGameUpdateItemNtf(player));

            if (_repository != null)
            {
                _ = Task.Run(() => _repository.SaveAccountAsync(player));
            }
        }

        /// <summary>
        /// 0x79AA (31146): MsgGameCrystallizeReq - Crystallize items into pure crystals
        /// SRC: server_legacy/DELPHI PROJECT/_Unit67.pas:006C4420 & 31146.dms
        /// </summary>
        [PacketHandler(PacketOpcode.MsgGameCrystallizeReq)]
        public async Task HandleCrystallizeReqAsync(PlayerSessionState state, PacketReader reader)
        {
            if (reader.Remaining < 8) return;
            int type = reader.ReadInt32();
            int level = reader.ReadInt32();
            ushort itemCount = reader.Remaining >= 2 ? reader.ReadUInt16() : (ushort)0;

            var player = state.Player;
            var consumed = new List<Item>();
            int crystalTypeId = 300001; // Default Crystal item type
            int crystalCount = Math.Max(1, (int)itemCount);

            lock (_storageLock)
            {
                for (int i = 0; i < itemCount && reader.Remaining >= 12; i++)
                {
                    int consumeTypeId = reader.ReadInt32();
                    int count = reader.ReadInt32();
                    reader.ReadInt32(); // skip invalid

                    var consumeItem = player.Inventory.FirstOrDefault(it => it.TypeId == consumeTypeId);
                    if (consumeItem != null)
                    {
                        consumed.Add(new Item { TypeId = consumeTypeId, Quantity = count });
                        if (consumeItem.Quantity > count)
                            consumeItem.Quantity -= count;
                        else
                            player.Inventory.Remove(consumeItem);
                    }
                }

                // Add crystal reward to inventory
                var existingCrystal = player.Inventory.FirstOrDefault(it => it.TypeId == crystalTypeId);
                if (existingCrystal != null)
                {
                    existingCrystal.Quantity += crystalCount;
                }
                else
                {
                    player.Inventory.Add(new Item
                    {
                        TypeId = crystalTypeId,
                        Quantity = crystalCount,
                        SlotIndex = player.Inventory.Count + 1
                    });
                }
            }

            Logger.Info($"[Crystal] '{player.CharacterName}' crystallized {consumed.Count} items into {crystalCount}x Crystal #{crystalTypeId}.");

            // Dispatch 0x79AB (MsgGameCrystallizeAns)
            await state.Session.SendAsync(YogurtingPackets.MakeGameCrystallizeAns(crystalTypeId, crystalCount, consumed));
            await state.Session.SendAsync(YogurtingPackets.MakeGameUpdateItemNtf(player));

            if (_repository != null)
            {
                _ = Task.Run(() => _repository.SaveAccountAsync(player));
            }
        }

        /// <summary>
        /// 0x794D (31053): MsgGameRevival119Req - 119 Emergency Respawn
        /// SRC: server_legacy/DELPHI PROJECT/_Unit67.pas:006C2800 & 31053.dms
        /// </summary>
        [PacketHandler(PacketOpcode.MsgGameRevival119Req)]
        public async Task HandleRevival119ReqAsync(PlayerSessionState state, PacketReader reader)
        {
            var player = state.Player;
            long fee = 500; // 500 Taff emergency medical fee

            lock (_storageLock)
            {
                player.TaffPoints = Math.Max(0, player.TaffPoints - fee);
                player.CurrentHp = player.MaxHp; // Restored by 119 medics
            }

            Logger.Info($"[Revival] 119 Emergency Respawn for '{player.CharacterName}' (HP: {player.CurrentHp}/{player.MaxHp}, Fee: {fee} Taff).");

            // 1. Dispatch 0x794E (MsgGameRevival119Ans)
            await state.Session.SendAsync(YogurtingPackets.MakeGameRevival119Ans(player.CharacterId, player.CurrentHp, player.TaffPoints));

            // 2. Sync HP and state to player
            await state.Session.SendAsync(YogurtingPackets.MakeGameSetHpNtf((ushort)player.CurrentHp));
            await state.Session.SendAsync(YogurtingPackets.MakeGameSetStateNtf(player));

            if (_repository != null)
            {
                _ = Task.Run(() => _repository.SaveAccountAsync(player));
            }
        }

        /// <summary>
        /// 0x794F (31055): MsgGameRevivalSchoolReq - School Respawn
        /// SRC: server_legacy/DELPHI PROJECT/_Unit67.pas:006C2950 & 31055.dms
        /// </summary>
        [PacketHandler(PacketOpcode.MsgGameRevivalSchoolReq)]
        public async Task HandleRevivalSchoolReqAsync(PlayerSessionState state, PacketReader reader)
        {
            var player = state.Player;

            lock (_storageLock)
            {
                player.CurrentHp = player.MaxHp;
                // Warp back to campus spawn anchor (Estiva Central Courtyard)
                player.FieldId = player.SaveFieldId > 0 ? player.SaveFieldId : 1;
                player.Position = new Position(player.SavePosition.X > 0 ? player.SavePosition.X : 76f,
                                               player.SavePosition.Y > 0 ? player.SavePosition.Y : 104f, 0f);
            }

            Logger.Info($"[Revival] School Infirmary Respawn for '{player.CharacterName}' at Field #{player.FieldId} {player.Position}.");

            // 1. Dispatch 0x7950 (MsgGameRevivalSchoolAns)
            await state.Session.SendAsync(YogurtingPackets.MakeGameRevivalSchoolAns(1));

            // 2. Sync HP and state
            await state.Session.SendAsync(YogurtingPackets.MakeGameSetHpNtf((ushort)player.CurrentHp));
            await state.Session.SendAsync(YogurtingPackets.MakeGameSetStateNtf(player));

            // 3. Broadcast position sync
            byte[] movePkt = YogurtingPackets.MakeGameMoveExNtf(player.CharacterId, (ushort)player.Position.X, (ushort)player.Position.Y, 0, 0);
            await state.Session.SendAsync(movePkt);
            await _broadcastDelegate(state, movePkt);

            if (_repository != null)
            {
                _ = Task.Run(() => _repository.SaveAccountAsync(player));
            }
        }
    }
}
