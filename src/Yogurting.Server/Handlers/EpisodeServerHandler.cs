using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Yogurting.Core.Models;
using Yogurting.Core.Network;
using Yogurting.Data.Loaders;
using Yogurting.Data.Repositories;

namespace Yogurting.Server.Handlers
{
    /// <summary>
    /// Handles Episode Combat, Mission Rooms, Monster Spawns, and Dungeon Instances on Port 10003.
    /// Direct 1-to-1 C# port of Quartet's UEpisode.pas / TAttractionSession.
    /// </summary>
    public sealed class EpisodeServerHandler
    {
        private readonly IAccountRepository _repository;
        private readonly GameDatabase _gameDatabase;
        private readonly string _host;
        private readonly int _fieldPort;
        private readonly ConcurrentDictionary<Guid, ClientSession> _activeSessions = new();

        public EpisodeServerHandler(IAccountRepository repository, GameDatabase gameDatabase, string host = "127.0.0.1", int fieldPort = 10002)
        {
            _repository = repository;
            _gameDatabase = gameDatabase;
            _host = host;
            _fieldPort = fieldPort;
        }

        public Task HandleClientConnectedAsync(ClientSession session)
        {
            _activeSessions[session.Id] = session;
            Console.WriteLine($"[EpisodeServer] Client connected from {session.RemoteEndPoint} to Episode/Attraction Server (Port 10003)!");
            return Task.CompletedTask;
        }

        public async Task HandlePacketAsync(ClientSession session, byte[] packetData)
        {
            if (packetData == null || packetData.Length < 4) return;

            ushort opcode = packetData.Length >= 6 ? BitConverter.ToUInt16(packetData, 4) : (ushort)0;

            switch ((PacketOpcode)opcode)
            {
                // 1. Attraction Handshake (Opcode 21009 / 0x5211) -> TAttractionSession.sub_006C4F88
                case PacketOpcode.MsgPingTimeReq:
                    Console.WriteLine($"[EpisodeServer] Attraction Handshake received from {session.RemoteEndPoint}. Dispatching Episode Start info...");
                    await session.SendAsync(YogurtingPackets.MakeTimeNtf());
                    await session.SendAsync(YogurtingPackets.MakeWorldTimeNtf(0, 0));
                    await session.SendAsync(YogurtingPackets.MakeGameAtkMovChangeNtf(1, 1.0f, 1.0f));
                    await session.SendAsync(YogurtingPackets.MakeGameFieldInfoDoneNtf());
                    break;

                // 2. Leave Attraction (Opcode 21013 / 0x5215) -> TAttractionSession.sub_006C52D8
                case PacketOpcode.MsgLeaveAtsNtf:
                    Console.WriteLine($"[EpisodeServer] Client {session.RemoteEndPoint} requested return to School Campus.");
                    // Redirect back to Field Server
                    await session.SendAsync(YogurtingPackets.MakeGotoSvrNtf(_host, _fieldPort));
                    break;

                // 3. Booty Box Opened (Opcode 31092 / 0x7974) -> TAttractionSession.sub_006C5DCC
                case PacketOpcode.MsgGameBootyBoxDoneReq:
                    Console.WriteLine($"[EpisodeServer] Booty Box Opened! Rewarding player...");
                    await session.SendAsync(YogurtingPackets.MakeGotoSvrNtf(_host, _fieldPort));
                    break;

                // 4. Episode Result Done (Opcode 31166 / 0x79BE) -> TAttractionSession.sub_006C5FE0
                case PacketOpcode.MsgGameRaceEpisodeResultReq:
                    Console.WriteLine($"[EpisodeServer] Episode Completed! Returning to School...");
                    await session.SendAsync(YogurtingPackets.MakeGotoSvrNtf(_host, _fieldPort));
                    break;

                // 5. Emergency 119 Respawn (Opcode 31053 / 0x794D) -> TAttractionSession.sub_006C59A8
                case PacketOpcode.MsgGameRevival119Req:
                    Console.WriteLine($"[EpisodeServer] 119 Emergency Respawn requested.");
                    break;

                // 6. School Respawn (Opcode 31055 / 0x794F) -> TAttractionSession.sub_006C5AAC
                case PacketOpcode.MsgGameRevivalSchoolReq:
                    Console.WriteLine($"[EpisodeServer] Revival at School requested. Redirecting to Campus...");
                    await session.SendAsync(YogurtingPackets.MakeGotoSvrNtf(_host, _fieldPort));
                    break;

                default:
                    break;
            }
        }

        public Task HandleClientDisconnectedAsync(ClientSession session)
        {
            _activeSessions.TryRemove(session.Id, out _);
            return Task.CompletedTask;
        }
    }
}
