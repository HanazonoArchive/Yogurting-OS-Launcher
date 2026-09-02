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
        private readonly PacketDispatcher<ClientSession> _dispatcher = new();

        public EpisodeServerHandler(IAccountRepository repository, GameDatabase gameDatabase, string host = "127.0.0.1", int fieldPort = 10002)
        {
            _repository = repository;
            _gameDatabase = gameDatabase;
            _host = host;
            _fieldPort = fieldPort;

            var missionHandlers = new Episode.EpisodeMissionHandlers(repository, gameDatabase, host, fieldPort);
            _dispatcher.RegisterHandlers(missionHandlers);
        }

        public Task HandleClientConnectedAsync(ClientSession session)
        {
            _activeSessions[session.Id] = session;
            Console.WriteLine($"[EpisodeServer] Client connected from {session.RemoteEndPoint} to Episode/Attraction Server (Port 10003)!");
            return Task.CompletedTask;
        }

        public async Task HandlePacketAsync(ClientSession session, byte[] packetData)
        {
            if (packetData == null || packetData.Length < 6) return;

            ushort opcode = System.Buffers.Binary.BinaryPrimitives.ReadUInt16LittleEndian(packetData.AsSpan(4, 2));

            bool handled = await _dispatcher.DispatchAsync(session, opcode, packetData);
            if (!handled)
            {
                Console.WriteLine($"[EpisodeServer] Unhandled Opcode 0x{opcode:X4} ({opcode}) from {session.RemoteEndPoint}");
            }
        }

        public Task HandleClientDisconnectedAsync(ClientSession session)
        {
            _activeSessions.TryRemove(session.Id, out _);
            return Task.CompletedTask;
        }
    }
}
