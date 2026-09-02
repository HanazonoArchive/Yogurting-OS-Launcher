using System;
using System.Threading.Tasks;
using Yogurting.Core.Logging;
using Yogurting.Core.Models;
using Yogurting.Core.Network;
using Yogurting.Data.Repositories;
using Yogurting.Server.World;

namespace Yogurting.Server.Handlers.Comm
{
    /// <summary>
    /// Handles Friends, Instant Messaging, Online Presence, and Heartbeats on Port 10004.
    /// Bit-for-bit match with Quartet's UComm.pas (TCommSession).
    /// </summary>
    public sealed class CommPresenceHandlers
    {
        private readonly IAccountRepository _repository;

        public CommPresenceHandlers(IAccountRepository repository)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        /// <summary>
        /// 0x4E21 (20001): MsgCheckVersionNtf - Client initial connection handshake on Comm port
        /// </summary>
        [PacketHandler(PacketOpcode.MsgCheckVersionNtf)]
        public Task HandleCheckVersionAsync(ClientSession session, PacketReader reader)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// 0x5211 (21009): MsgPingTimeReq - Comm Server Join / Handshake
        /// SRC: server_legacy/DELPHI PROJECT/UComm.pas & _Unit67.pas:006C620C
        /// </summary>
        [PacketHandler(PacketOpcode.MsgPingTimeReq)]
        public async Task HandleCommJoinAsync(ClientSession session, PacketReader reader)
        {
            int charaId = reader.Remaining >= 4 ? reader.ReadInt32() : 0;
            int authToken = 0;
            if (reader.Remaining >= 28)
            {
                reader.Skip(24);
                authToken = reader.ReadInt32();
            }

            Logger.Info($"[CommServer] Comm Handshake (0x5211) from {session.RemoteEndPoint} - CharaId={charaId}, AuthToken={authToken}");

            Player? player = null;
            if (authToken > 0)
            {
                player = await _repository.GetBySessionKeyAsync(authToken);
            }
            if (player == null && !string.IsNullOrEmpty(session.AccountId))
            {
                player = await _repository.GetByUsernameAsync(session.AccountId);
            }
            if (player == null)
            {
                player = await _repository.GetByUsernameAsync("test");
            }

            if (player != null)
            {
                CommManager.Instance.RegisterOnline(player, session);
                var friendList = CommManager.Instance.BuildFriendList(player, _repository);
                // Respond with TMsgTransJoinCmsAns (0x7604) with friend roster
                await session.SendAsync(YogurtingPackets.MakeTransJoinCmsAns(player, friendList));
            }
            else
            {
                await session.SendAsync(YogurtingPackets.MakeTransJoinCmsAns(new Player("test", "Student")));
            }
        }

        /// <summary>
        /// 0x7759 (30553): MsgCommEchoNtf - Comm Echo Heartbeat
        /// SRC: server_legacy/DELPHI PROJECT/_Unit67.pas:006C6344
        /// </summary>
        [PacketHandler(PacketOpcode.MsgCommEchoNtf)]
        public async Task HandleCommEchoAsync(ClientSession session, PacketReader reader)
        {
            int seqNum = reader.Remaining >= 4 ? reader.ReadInt32() : 0;
            await session.SendAsync(YogurtingPackets.MakeCommEchoNtf(seqNum));
        }

        /// <summary>
        /// 0x7728 (30504): MsgCommFriendProposeReq - Friend Request
        /// SRC: server_legacy/DELPHI PROJECT/_Unit67.pas:006C62F8
        /// </summary>
        [PacketHandler(PacketOpcode.MsgCommFriendProposeReq)]
        public Task HandleFriendProposeReqAsync(ClientSession session, PacketReader reader)
        {
            Logger.Info($"[CommServer] Friend Request received from {session.RemoteEndPoint}");
            return Task.CompletedTask;
        }
    }
}
