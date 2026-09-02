using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Yogurting.Core.Logging;
using Yogurting.Core.Models;
using Yogurting.Core.Network;
using Yogurting.Data.Repositories;

namespace Yogurting.Server.Handlers.Field
{
    /// <summary>
    /// Peer-to-Peer Direct Trade Handlers (Port 10002 - Opcodes 0x792B to 0x7935).
    /// Reverse-engineered from Delphi Quartet TSchoolSession Trade Pipeline
    /// (server_legacy/DELPHI PROJECT/_Unit67.pas:006C1B14-006C1EE9 & _Unit49.pas:22030-22250).
    /// </summary>
    public sealed class TradeHandlers
    {
        private readonly Func<int, PlayerSessionState?> _findPlayerById;
        private readonly Func<int, List<PlayerSessionState>> _getPlayersInField;
        private readonly IAccountRepository? _repository;
        private readonly object _tradeLock = new();

        public TradeHandlers(
            Func<int, PlayerSessionState?> findPlayerById,
            Func<int, List<PlayerSessionState>> getPlayersInField,
            IAccountRepository? repository = null)
        {
            _findPlayerById = findPlayerById;
            _getPlayersInField = getPlayersInField;
            _repository = repository;
        }

        /// <summary>
        /// 0x792B (31019): MsgGameTradeReq - Player proposes a direct trade with target.
        /// SRC: server_legacy/DELPHI PROJECT/_Unit67.pas:006C1B14 (TSchoolSession.sub_006C1B14)
        /// Payload: Int32 TargetCharacterId
        /// </summary>
        [PacketHandler(PacketOpcode.MsgGameTradeReq)]
        public async Task HandleTradeReqAsync(PlayerSessionState state, PacketReader reader)
        {
            if (reader.Remaining < 4) return;
            int targetId = reader.ReadInt32();

            var player = state.Player;
            var targetSession = _findPlayerById(targetId);

            if (targetSession == null || targetSession.Player.FieldId != player.FieldId)
            {
                // Target not found or in different field
                await state.Session.SendAsync(YogurtingPackets.MakeGameTradeFailedNtf(6));
                return;
            }

            var targetPlayer = targetSession.Player;

            lock (_tradeLock)
            {
                // Check if either player is already in a trade
                if (player.CurrentTradeState != TradeState.None || targetPlayer.CurrentTradeState != TradeState.None)
                {
                    _ = state.Session.SendAsync(YogurtingPackets.MakeGameTradeFailedNtf(6));
                    return;
                }

                player.ClearTrade();
                targetPlayer.ClearTrade();

                player.TradeTargetPlayerId = targetPlayer.CharacterId;
                player.CurrentTradeState = TradeState.Proposed;

                targetPlayer.TradeTargetPlayerId = player.CharacterId;
                targetPlayer.CurrentTradeState = TradeState.Proposed;
            }

            Logger.Info($"[Trade] '{player.CharacterName}' (ID: {player.CharacterId}) proposed trade to '{targetPlayer.CharacterName}' (ID: {targetPlayer.CharacterId}).");

            // Dispatch 0x792C (MsgGameTradeResponseReq) to target client showing trade proposal modal
            await targetSession.Session.SendAsync(YogurtingPackets.MakeGameTradeResponseReq(player.CharacterName));
        }

        /// <summary>
        /// 0x792D (31021): MsgGameTradeResponseAns - Target responds to trade proposal (Accept / Decline).
        /// SRC: server_legacy/DELPHI PROJECT/_Unit67.pas:006C1BA8 (TSchoolSession.sub_006C1BA8)
        /// Payload: Int32 bAttend (1 = Accept, 0 = Decline)
        /// </summary>
        [PacketHandler(PacketOpcode.MsgGameTradeResponseAns)]
        public async Task HandleTradeResponseAnsAsync(PlayerSessionState state, PacketReader reader)
        {
            if (reader.Remaining < 4) return;
            int bAttend = reader.ReadInt32();

            var player = state.Player;
            PlayerSessionState? partnerSession;

            lock (_tradeLock)
            {
                if (player.CurrentTradeState != TradeState.Proposed || player.TradeTargetPlayerId == 0)
                {
                    return;
                }

                partnerSession = _findPlayerById(player.TradeTargetPlayerId);
                if (partnerSession == null || partnerSession.Player.TradeTargetPlayerId != player.CharacterId)
                {
                    player.ClearTrade();
                    _ = state.Session.SendAsync(YogurtingPackets.MakeGameTradeFailedNtf(6));
                    return;
                }

                if (bAttend != 1)
                {
                    // Declined
                    player.ClearTrade();
                    partnerSession.Player.ClearTrade();
                    _ = partnerSession.Session.SendAsync(YogurtingPackets.MakeGameTradeFailedNtf(6));
                    return;
                }

                player.CurrentTradeState = TradeState.Trading;
                partnerSession.Player.CurrentTradeState = TradeState.Trading;
            }

            Logger.Info($"[Trade] Trade accepted between '{partnerSession.Player.CharacterName}' and '{player.CharacterName}'. Opening trade windows.");

            // Dispatch 0x792E (MsgGameTradeOtherSideAttendNtf) to both sessions to open trade UI
            await state.Session.SendAsync(YogurtingPackets.MakeGameTradeOtherSideAttendNtf(partnerSession.Player.CharacterId));
            await partnerSession.Session.SendAsync(YogurtingPackets.MakeGameTradeOtherSideAttendNtf(player.CharacterId));
        }

        /// <summary>
        /// 0x7930 (31024): MsgGameTradeBasketUpdateReq - Adds or updates an item or money in trade basket.
        /// SRC: server_legacy/DELPHI PROJECT/_Unit67.pas:006C1C70 (TSchoolSession.sub_006C1C70)
        /// Payload:
        ///   Int32 typeItem (or 1 for Money update)
        ///   Word count
        ///   Word dim2
        ///   Int32 itemId
        ///   5 x Int32 reinforceSlots
        /// </summary>
        [PacketHandler(PacketOpcode.MsgGameTradeBasketUpdateReq)]
        public async Task HandleTradeBasketUpdateAsync(PlayerSessionState state, PacketReader reader)
        {
            if (reader.Remaining < 32) return;

            int itemType = reader.ReadInt32();
            ushort count = reader.ReadUInt16();
            ushort dim2 = reader.ReadUInt16();
            int itemId = reader.ReadInt32();
            int[] sockets = new int[5];
            for (int i = 0; i < 5; i++)
            {
                sockets[i] = reader.ReadInt32();
            }

            var player = state.Player;
            PlayerSessionState? partnerSession;

            lock (_tradeLock)
            {
                if (player.CurrentTradeState != TradeState.Trading || player.TradeTargetPlayerId == 0)
                {
                    return;
                }

                partnerSession = _findPlayerById(player.TradeTargetPlayerId);
                if (partnerSession == null)
                {
                    player.ClearTrade();
                    _ = state.Session.SendAsync(YogurtingPackets.MakeGameTradeFailedNtf(6));
                    return;
                }

                // If typeItem == 1: Currency update (count + dim2 + itemId represent money amount in Delphi)
                if (itemType == 1)
                {
                    long moneyOffer = ((long)itemId << 32) | (uint)((count << 16) | dim2);
                    if (moneyOffer < 0 || moneyOffer > player.TaffPoints)
                    {
                        moneyOffer = Math.Clamp(moneyOffer, 0, player.TaffPoints);
                    }
                    player.TradeMoney = moneyOffer;
                }
                else
                {
                    // Find an empty slot or update existing slot in 5-slot basket
                    int targetSlotIdx = -1;
                    for (int i = 0; i < 5; i++)
                    {
                        if (player.TradeBasket[i].IsEmpty || (player.TradeBasket[i].ItemType == itemType && player.TradeBasket[i].ItemId == itemId))
                        {
                            targetSlotIdx = i;
                            break;
                        }
                    }

                    if (targetSlotIdx >= 0)
                    {
                        player.TradeBasket[targetSlotIdx].ItemType = itemType;
                        player.TradeBasket[targetSlotIdx].Count = count;
                        player.TradeBasket[targetSlotIdx].Dim2Index = dim2;
                        player.TradeBasket[targetSlotIdx].ItemId = itemId;
                        player.TradeBasket[targetSlotIdx].ReinforceSlots = sockets;
                    }
                }
            }

            // Broadcast 0x792F (MsgGameTradeOtherSideBasketInfoNtf) to partner
            await partnerSession.Session.SendAsync(
                YogurtingPackets.MakeGameTradeOtherSideBasketInfoNtf(player.TradeBasket, player.TradeMoney));
        }

        /// <summary>
        /// 0x7931 (31025): MsgGameTradeCancelReq - Aborts trade and clears state on both sides.
        /// SRC: server_legacy/DELPHI PROJECT/_Unit67.pas:006C1DE4 (TSchoolSession.sub_006C1DE4)
        /// </summary>
        [PacketHandler(PacketOpcode.MsgGameTradeCancelReq)]
        public async Task HandleTradeCancelAsync(PlayerSessionState state, PacketReader reader)
        {
            var player = state.Player;
            PlayerSessionState? partnerSession;

            lock (_tradeLock)
            {
                int targetId = player.TradeTargetPlayerId;
                player.ClearTrade();

                partnerSession = _findPlayerById(targetId);
                if (partnerSession != null)
                {
                    partnerSession.Player.ClearTrade();
                }
            }

            await state.Session.SendAsync(YogurtingPackets.MakeGameTradeFailedNtf(6));
            if (partnerSession != null)
            {
                await partnerSession.Session.SendAsync(YogurtingPackets.MakeGameTradeFailedNtf(6));
            }
            Logger.Info($"[Trade] Trade cancelled by '{player.CharacterName}'.");
        }

        /// <summary>
        /// 0x7933 (31027): MsgGameTradeOkReq - Locks basket and marks trade as ready/locked.
        /// SRC: server_legacy/DELPHI PROJECT/_Unit67.pas:006C1E2C (TSchoolSession.sub_006C1E2C)
        /// </summary>
        [PacketHandler(PacketOpcode.MsgGameTradeOkReq)]
        [PacketHandler(PacketOpcode.MsgGameTradeOkNtf)]
        public async Task HandleTradeOkAsync(PlayerSessionState state, PacketReader reader)
        {
            var player = state.Player;
            PlayerSessionState? partnerSession;

            lock (_tradeLock)
            {
                if (player.CurrentTradeState != TradeState.Trading || player.TradeTargetPlayerId == 0)
                {
                    return;
                }

                player.CurrentTradeState = TradeState.Locked;
                partnerSession = _findPlayerById(player.TradeTargetPlayerId);
            }

            if (partnerSession != null)
            {
                // Send 0x7932 (MsgGameTradeOkNtf) to partner to display green Lock status
                await partnerSession.Session.SendAsync(YogurtingPackets.MakeGameTradeOkNtf());
            }
        }

        /// <summary>
        /// 0x7935 (31029): MsgGameTradeFinalConfirmReq - Final confirmation.
        /// If both players confirmed, atomically swaps items and currency.
        /// SRC: server_legacy/DELPHI PROJECT/_Unit67.pas:006C1E64 (TSchoolSession.sub_006C1E64)
        /// </summary>
        [PacketHandler(PacketOpcode.MsgGameTradeFinalConfirmReq)]
        public async Task HandleTradeFinalConfirmAsync(PlayerSessionState state, PacketReader reader)
        {
            var player = state.Player;
            PlayerSessionState? partnerSession;
            bool executeSwap = false;

            lock (_tradeLock)
            {
                if (player.CurrentTradeState != TradeState.Locked && player.CurrentTradeState != TradeState.Trading)
                {
                    return;
                }

                player.CurrentTradeState = TradeState.FinalConfirmed;
                partnerSession = _findPlayerById(player.TradeTargetPlayerId);

                if (partnerSession == null || partnerSession.Player.TradeTargetPlayerId != player.CharacterId)
                {
                    player.ClearTrade();
                    _ = state.Session.SendAsync(YogurtingPackets.MakeGameTradeFailedNtf(6));
                    return;
                }

                // If both players have reached FinalConfirmed state, execute the atomic swap!
                if (partnerSession.Player.CurrentTradeState == TradeState.FinalConfirmed)
                {
                    executeSwap = true;
                }
            }

            if (!executeSwap || partnerSession == null)
            {
                return;
            }

            var p1 = player;
            var p2 = partnerSession.Player;

            // Snapshot trade offers
            var p1OutSlots = p1.TradeBasket.Select(s => s.Clone()).ToArray();
            long p1OutMoney = p1.TradeMoney;

            var p2OutSlots = p2.TradeBasket.Select(s => s.Clone()).ToArray();
            long p2OutMoney = p2.TradeMoney;

            // Execute inventory and currency swap
            lock (_tradeLock)
            {
                // 1. Swap Money
                if (p1.TaffPoints >= p1OutMoney && p2.TaffPoints >= p2OutMoney)
                {
                    p1.TaffPoints = p1.TaffPoints - p1OutMoney + p2OutMoney;
                    p2.TaffPoints = p2.TaffPoints - p2OutMoney + p1OutMoney;
                }

                // 2. Transfer items from P1 -> P2
                foreach (var slot in p1OutSlots.Where(s => !s.IsEmpty))
                {
                    var itemToMove = p1.Inventory.FirstOrDefault(i => i.TypeId == slot.ItemType || i.Id == slot.ItemId || (int)i.SerialId == slot.ItemId);
                    if (itemToMove != null)
                    {
                        p1.Inventory.Remove(itemToMove);
                        p2.Inventory.Add(itemToMove);
                    }
                }

                // 3. Transfer items from P2 -> P1
                foreach (var slot in p2OutSlots.Where(s => !s.IsEmpty))
                {
                    var itemToMove = p2.Inventory.FirstOrDefault(i => i.TypeId == slot.ItemType || i.Id == slot.ItemId || (int)i.SerialId == slot.ItemId);
                    if (itemToMove != null)
                    {
                        p2.Inventory.Remove(itemToMove);
                        p1.Inventory.Add(itemToMove);
                    }
                }

                p1.ClearTrade();
                p2.ClearTrade();
            }

            Logger.Info($"[Trade] Atomic trade complete between '{p1.CharacterName}' and '{p2.CharacterName}'.");

            // Dispatch 0x7934 (MsgGameTradeCompleteNtf) to both sessions
            await state.Session.SendAsync(YogurtingPackets.MakeGameTradeCompleteNtf(p1OutSlots, p1OutMoney, p2OutSlots, p2OutMoney));
            await partnerSession.Session.SendAsync(YogurtingPackets.MakeGameTradeCompleteNtf(p2OutSlots, p2OutMoney, p1OutSlots, p1OutMoney));

            // Sync updated inventory to both players
            await state.Session.SendAsync(YogurtingPackets.MakeGameUpdateItemNtf(p1));
            await partnerSession.Session.SendAsync(YogurtingPackets.MakeGameUpdateItemNtf(p2));

            // Persist accounts
            if (_repository != null)
            {
                _ = Task.Run(async () =>
                {
                    await _repository.SaveAccountAsync(p1);
                    await _repository.SaveAccountAsync(p2);
                });
            }
        }
    }
}
