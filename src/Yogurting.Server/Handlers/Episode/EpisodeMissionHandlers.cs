using System;
using System.Threading.Tasks;
using Yogurting.Core.Logging;
using Yogurting.Core.Network;
using Yogurting.Data.Loaders;
using Yogurting.Data.Repositories;

namespace Yogurting.Server.Handlers.Episode
{
    /// <summary>
    /// Handles Episode Combat, Missions, Booty Box, and School Redirects on Port 10003.
    /// Bit-for-bit match with Quartet's UEpisode.pas / TAttractionSession.
    /// </summary>
    public sealed class EpisodeMissionHandlers
    {
        private readonly IAccountRepository _repository;
        private readonly GameDatabase _gameDatabase;
        private readonly string _host;
        private readonly int _fieldPort;

        public EpisodeMissionHandlers(IAccountRepository repository, GameDatabase gameDatabase, string host = "127.0.0.1", int fieldPort = 10002)
        {
            _repository = repository;
            _gameDatabase = gameDatabase;
            _host = host;
            _fieldPort = fieldPort;
        }

        /// <summary>
        /// 0x5211 (21009): MsgPingTimeReq - Attraction Handshake
        /// SRC: server_legacy/DELPHI PROJECT/UEpisode.pas & _Unit67.pas:006C4F88
        /// </summary>
        [PacketHandler(PacketOpcode.MsgPingTimeReq)]
        public async Task HandlePingTimeReqAsync(ClientSession session, PacketReader reader)
        {
            Logger.Info($"[EpisodeServer] Attraction Handshake received from {session.RemoteEndPoint}. Dispatching Episode Start info...");
            await session.SendAsync(YogurtingPackets.MakeTimeNtf());
            await session.SendAsync(YogurtingPackets.MakeWorldTimeNtf(0, 0));
            await session.SendAsync(YogurtingPackets.MakeGameAtkMovChangeNtf(1, 1.0f, 1.0f));
            await session.SendAsync(YogurtingPackets.MakeGameFieldInfoDoneNtf());
        }

        /// <summary>
        /// 0x5215 (21013): MsgLeaveAtsNtf - Client requested return to School Campus
        /// SRC: server_legacy/DELPHI PROJECT/_Unit67.pas:006C52D8
        /// </summary>
        [PacketHandler(PacketOpcode.MsgLeaveAtsNtf)]
        public async Task HandleLeaveAtsNtfAsync(ClientSession session, PacketReader reader)
        {
            Logger.Info($"[EpisodeServer] Client {session.RemoteEndPoint} requested return to School Campus.");
            await session.SendAsync(YogurtingPackets.MakeGotoSvrNtf(_host, _fieldPort));
        }

        /// <summary>
        /// 0x7974 (31092): MsgGameBootyBoxDoneReq - Booty Box Opened
        /// SRC: server_legacy/DELPHI PROJECT/_Unit67.pas:006C5DCC
        /// </summary>
        [PacketHandler(PacketOpcode.MsgGameBootyBoxDoneReq)]
        public async Task HandleBootyBoxDoneReqAsync(ClientSession session, PacketReader reader)
        {
            int selectedBoxIndex = reader.Remaining >= 4 ? reader.ReadInt32() : 0;
            Logger.Info($"[EpisodeServer] Booty Box #{selectedBoxIndex} Opened! Unboxing reward...");

            // 1. Confirm Booty Box Unbox with particle effect trigger (0x7975)
            await session.SendAsync(YogurtingPackets.MakeGameBootyBoxDoneAns(1));

            // 2. Return to School Campus
            await session.SendAsync(YogurtingPackets.MakeGotoSvrNtf(_host, _fieldPort));
        }

        /// <summary>
        /// 0x79BE (31166): MsgGameRaceEpisodeResultReq - Episode Completed
        /// SRC: server_legacy/DELPHI PROJECT/_Unit67.pas:006C5FE0
        /// </summary>
        [PacketHandler(PacketOpcode.MsgGameRaceEpisodeResultReq)]
        public async Task HandleRaceEpisodeResultReqAsync(ClientSession session, PacketReader reader)
        {
            Logger.Info($"[EpisodeServer] Episode Completed! Sending Result & Rank Screen (0x7972)...");
            await session.SendAsync(YogurtingPackets.MakeGameEpisodeResultNtf(1, "Student", 1, 12500, 750));
            await Task.Delay(100);
            await session.SendAsync(YogurtingPackets.MakeGotoSvrNtf(_host, _fieldPort));
        }

        /// <summary>
        /// 0x794D (31053): MsgGameRevival119Req - Emergency 119 Respawn
        /// SRC: server_legacy/DELPHI PROJECT/_Unit67.pas:006C59A8
        /// </summary>
        [PacketHandler(PacketOpcode.MsgGameRevival119Req)]
        public Task HandleRevival119ReqAsync(ClientSession session, PacketReader reader)
        {
            Logger.Info($"[EpisodeServer] 119 Emergency Respawn requested on Episode Server.");
            return Task.CompletedTask;
        }

        /// <summary>
        /// 0x794F (31055): MsgGameRevivalSchoolReq - School Respawn
        /// SRC: server_legacy/DELPHI PROJECT/_Unit67.pas:006C5AAC
        /// </summary>
        [PacketHandler(PacketOpcode.MsgGameRevivalSchoolReq)]
        public async Task HandleRevivalSchoolReqAsync(ClientSession session, PacketReader reader)
        {
            Logger.Info($"[EpisodeServer] Revival at School requested. Redirecting to Campus...");
            await session.SendAsync(YogurtingPackets.MakeGotoSvrNtf(_host, _fieldPort));
        }

        /// <summary>
        /// 0x798F (31119): MsgGameCharDirectNtf - Character Facing Direction Broadcast
        /// SRC: server_legacy/DELPHI PROJECT/_Unit67.pas:006C5F30 (TAttractionSession.sub_006C5F30)
        /// </summary>
        [PacketHandler(PacketOpcode.MsgGameCharDirectNtf)]
        public Task HandleCharDirectAsync(ClientSession session, PacketReader reader)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// 0x794B (31051): MsgGameRevivalCampusReq - Return to Campus from Episode
        /// SRC: server_legacy/DELPHI PROJECT/_Unit67.pas:006C594C (sub_006C594C)
        /// </summary>
        [PacketHandler(PacketOpcode.MsgGameRevivalCampusReq)]
        public async Task HandleRevivalCampusReqAsync(ClientSession session, PacketReader reader)
        {
            Logger.Info($"[EpisodeServer] Revival at Campus requested. Redirecting to Campus Server...");
            await session.SendAsync(YogurtingPackets.MakeGotoSvrNtf(_host, _fieldPort));
        }
    }
}
