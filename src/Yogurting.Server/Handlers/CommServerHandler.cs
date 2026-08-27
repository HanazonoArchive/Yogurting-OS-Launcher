using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Yogurting.Core.Network;
using Yogurting.Data.Repositories;

namespace Yogurting.Server.Handlers
{
    /// <summary>
    /// Handles Friends, Instant Messaging, Presence, and Heartbeats on Port 10004.
    /// Direct 1-to-1 C# port of Quartet's UComm.pas (TCommSession).
    /// Comm handshake (0x5211) uses same packet format as School handshake (0x5213):
    ///   ReadInt32 → charaID, ReadInt32, ReadInt32, ReadSeek(16), ReadInt32 → authToken
    /// </summary>
    public sealed class CommServerHandler
    {
        private readonly IAccountRepository _repository;
        private readonly ConcurrentDictionary<Guid, ClientSession> _activeSessions = new();

        public CommServerHandler(IAccountRepository repository)
        {
            _repository = repository;
        }

        public Task HandleClientConnectedAsync(ClientSession session)
        {
            _activeSessions[session.Id] = session;
            Console.WriteLine($"[CommServer] Client connected from {session.RemoteEndPoint} to Comm/Messenger Server (Port 10004)!");
            return Task.CompletedTask;
        }

        public async Task HandlePacketAsync(ClientSession session, byte[] packetData)
        {
            if (packetData == null || packetData.Length < 6) return;

            ushort opcode = BitConverter.ToUInt16(packetData, 4);

            switch ((PacketOpcode)opcode)
            {
                // 1. Comm Server Join / Handshake (Opcode 21009 / 0x5211) -> TCommSession.sub_006C620C
                // Same packet format as school 0x5213: charaID + skip + authToken
                case PacketOpcode.MsgPingTimeReq:
                {
                    int charaId = 0;
                    int authToken = 0;
                    if (packetData.Length >= 10)
                        charaId = BitConverter.ToInt32(packetData, 6);
                    if (packetData.Length >= 38)
                        authToken = BitConverter.ToInt32(packetData, 34);

                    Console.WriteLine($"[CommServer] Comm Handshake (0x5211) from {session.RemoteEndPoint} - CharaId={charaId}, AuthToken={authToken}");

                    var player = await _repository.GetByUsernameAsync(session.AccountId ?? "test");
                    // Respond with TMsgTransJoinCmsAns (0x7604) - exact 254-byte ground truth
                    await session.SendAsync(YogurtingPackets.MakeTransJoinCmsAns(player));
                    break;
                }

                // 2. Comm Echo Heartbeat (Opcode 30553 / 0x7759) -> TCommSession.sub_006C6344
                case PacketOpcode.MsgCommEchoNtf:
                {
                    int seqNum = packetData.Length >= 10 ? BitConverter.ToInt32(packetData, 6) : 0;
                    await session.SendAsync(YogurtingPackets.MakeCommEchoNtf(seqNum));
                    break;
                }

                // 3. Friend Request (Opcode 30504 / 0x7728) -> TCommSession.sub_006C62F8
                case PacketOpcode.MsgCommFriendProposeReq:
                    Console.WriteLine($"[CommServer] Friend Request received from {session.RemoteEndPoint}");
                    break;

                default:
                    Console.WriteLine($"[CommServer] Unknown opcode 0x{opcode:X4} ({opcode}) from {session.RemoteEndPoint}, echoing...");
                    await session.SendAsync(YogurtingPackets.MakeCommEchoNtf());
                    break;
            }
        }

        public Task HandleClientDisconnectedAsync(ClientSession session)
        {
            _activeSessions.TryRemove(session.Id, out _);
            Console.WriteLine($"[CommServer] Client disconnected from {session.RemoteEndPoint}.");
            return Task.CompletedTask;
        }
    }
}
