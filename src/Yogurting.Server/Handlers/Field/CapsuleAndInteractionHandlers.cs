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
    /// Handles Capsule Vending Machine (Gacha), Interactive Field Objects, and Special Hotline Actions.
    /// Reverse-engineered from Delphi Quartet server logic
    /// (server_legacy/DELPHI PROJECT/_Unit47.pas:005B03F8-005B0554, 005AE6CC-005AEB70, _Unit67.pas:006C4550 & 42000 series).
    /// </summary>
    public sealed class CapsuleAndInteractionHandlers
    {
        private readonly Func<PlayerSessionState, byte[], Task> _broadcastDelegate;
        private readonly IAccountRepository? _repository;
        private readonly GameDatabase? _gameDb;

        public CapsuleAndInteractionHandlers(
            Func<PlayerSessionState, byte[], Task> broadcastDelegate,
            IAccountRepository? repository = null,
            GameDatabase? gameDb = null)
        {
            _broadcastDelegate = broadcastDelegate;
            _repository = repository;
            _gameDb = gameDb;
        }

        /// <summary>
        /// 0xA411 (42001): MsgGameCapsuleEnterNtf - Player enters capsule vending machine UI
        /// SRC: server_legacy/DELPHI PROJECT/_Unit47.pas:005B03F8 & 42001.dms
        /// </summary>
        [PacketHandler(PacketOpcode.MsgGameCapsuleEnterNtf)]
        public async Task HandleCapsuleEnterNtfAsync(PlayerSessionState state, PacketReader reader)
        {
            ushort machineId = reader.Remaining >= 2 ? reader.ReadUInt16() : (ushort)1;
            var player = state.Player;

            Logger.Info($"[Capsule] '{player.CharacterName}' entered Capsule Vending Machine #{machineId}.");

            // Dispatch Product Info (0xA412)
            var products = new List<(int bSecret, int typeItem, long amount)>();
            if (_gameDb?.StarProducts != null && _gameDb.StarProducts.Count > 0)
            {
                foreach (var prod in _gameDb.StarProducts.Take(10))
                {
                    products.Add((0, prod.ProductId, 99));
                }
            }
            else
            {
                products.Add((0, 140001, 99));
            }

            byte[] productInfoPkt = YogurtingPackets.MakeGameCapsuleProductInfoNtf(machineId, 500, products, 999);
            await state.Session.SendAsync(productInfoPkt);
        }

        /// <summary>
        /// 0x7984 (31108): MsgGameTakeUpObjectReq - Lift interactive box/object
        /// SRC: server_legacy/DELPHI PROJECT/_Unit47.pas:005AE6CC & 31108
        /// </summary>
        [PacketHandler(PacketOpcode.MsgGameTakeUpObjectReq)]
        public async Task HandleTakeUpObjectReqAsync(PlayerSessionState state, PacketReader reader)
        {
            int objectId = reader.Remaining >= 4 ? reader.ReadInt32() : 0;
            var player = state.Player;

            Logger.Info($"[Interaction] '{player.CharacterName}' lifted object #{objectId}.");

            byte[] ansPkt = YogurtingPackets.MakeGameTakeUpObjectAns(player.CharacterId, objectId, result: 1);
            await state.Session.SendAsync(ansPkt);
            await _broadcastDelegate(state, ansPkt);
        }

        /// <summary>
        /// 0x7986 (31110): MsgGameTakeDownObjectReq - Put down interactive box/object
        /// SRC: server_legacy/DELPHI PROJECT/_Unit47.pas:005AE718 & 31110
        /// </summary>
        [PacketHandler(PacketOpcode.MsgGameTakeDownObjectReq)]
        public async Task HandleTakeDownObjectReqAsync(PlayerSessionState state, PacketReader reader)
        {
            int objectId = reader.Remaining >= 4 ? reader.ReadInt32() : 0;
            var player = state.Player;

            Logger.Info($"[Interaction] '{player.CharacterName}' put down object #{objectId}.");

            byte[] ansPkt = YogurtingPackets.MakeGameTakeDownObjectAns(player.CharacterId, objectId, result: 1);
            await state.Session.SendAsync(ansPkt);
            await _broadcastDelegate(state, ansPkt);
        }

        /// <summary>
        /// 0x7996 (31126): MsgGamePushObjectReq - Push interactive obstacle/box
        /// SRC: server_legacy/DELPHI PROJECT/_Unit47.pas:005AEB70 & 31126.dms
        /// </summary>
        [PacketHandler(PacketOpcode.MsgGamePushObjectReq)]
        public async Task HandlePushObjectReqAsync(PlayerSessionState state, PacketReader reader)
        {
            int objectId = reader.Remaining >= 4 ? reader.ReadInt32() : 0;
            var player = state.Player;

            ushort newX = (ushort)(player.Position.X + 2);
            ushort newY = (ushort)(player.Position.Y + 2);

            Logger.Info($"[Interaction] '{player.CharacterName}' pushed object #{objectId} to ({newX}, {newY}).");

            byte[] ansPkt = YogurtingPackets.MakeGamePushObjectAns(player.CharacterId, objectId, result: 1, newX, newY);
            await state.Session.SendAsync(ansPkt);
            await _broadcastDelegate(state, ansPkt);
        }

        /// <summary>
        /// 0x79C2 (31170): MsgGameSpecialPhoneCallReq - Special Hotline Action
        /// SRC: server_legacy/DELPHI PROJECT/_Unit67.pas:006C4550 & 31170.dms
        /// </summary>
        [PacketHandler(PacketOpcode.MsgGameSpecialPhoneCallReq)]
        public async Task HandleSpecialPhoneCallReqAsync(PlayerSessionState state, PacketReader reader)
        {
            int phone = reader.Remaining >= 4 ? reader.ReadInt32() : 0;
            var player = state.Player;

            Logger.Info($"[Phone] '{player.CharacterName}' dialed special hotline #{phone}.");

            await state.Session.SendAsync(YogurtingPackets.MakeGameSpecialPhoneCallAns(1));
        }
    }
}
