using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Yogurting.Core.Models;
using Yogurting.Core.Network;
using Yogurting.Data.Repositories;

namespace Yogurting.Server.Handlers
{
    /// <summary>
    /// Handles Friends, Instant Messaging, Presence, and Heartbeats on Port 10004.
    /// Direct 1-to-1 C# port of Quartet's UComm.pas (TCommSession).
    /// </summary>
    public sealed class CommServerHandler
    {
        private readonly IAccountRepository _repository;
        private readonly ConcurrentDictionary<Guid, ClientSession> _activeSessions = new();
        private readonly PacketDispatcher<ClientSession> _dispatcher = new();

        public CommServerHandler(IAccountRepository repository)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));

            var presenceHandlers = new Comm.CommPresenceHandlers(repository);
            _dispatcher.RegisterHandlers(presenceHandlers);
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

            ushort opcode = System.Buffers.Binary.BinaryPrimitives.ReadUInt16LittleEndian(packetData.AsSpan(4, 2));

            bool handled = await _dispatcher.DispatchAsync(session, opcode, packetData);
            if (!handled)
            {
                Console.WriteLine($"[CommServer] Unhandled Opcode 0x{opcode:X4} ({opcode}) from {session.RemoteEndPoint}");
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
