using System;
using System.Threading.Tasks;
using Yogurting.Core.Logging;
using Yogurting.Core.Network;
using Yogurting.Data.Repositories;
using Yogurting.Server.Handlers.Auth;

namespace Yogurting.Server.Handlers
{
    /// <summary>
    /// Handles Login Authentication, Character Selection & World Routing on Port 10000.
    /// Bit-for-bit exact reproduction of Quartet.exe (Decompiled Delphi ULogin.pas / TLoginSession).
    /// </summary>
    public sealed class LoginServerHandler
    {
        private readonly IAccountRepository _repository;
        private readonly PacketDispatcher<ClientSession> _dispatcher = new();
        private readonly string _serverBindIp;
        private readonly int _schoolPort;
        private readonly int _commPort;

        public LoginServerHandler(IAccountRepository repository, string serverBindIp = "127.0.0.1", int schoolPort = 10002, int commPort = 10004)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _serverBindIp = serverBindIp;
            _schoolPort = schoolPort;
            _commPort = commPort;

            var authHandlers = new AuthHandlers(repository, _serverBindIp, _schoolPort, _commPort);
            _dispatcher.RegisterHandlers(authHandlers);
        }

        public async Task HandleClientConnectedAsync(ClientSession session)
        {
            Logger.Info($"[LoginServer] Client connected from {session.RemoteEndPoint}. Sending AuthTypeNtf (0x7595)...");
            await session.SendAsync(YogurtingPackets.MakeAuthTypeNtf(0));
        }

        public async Task HandlePacketAsync(ClientSession session, byte[] packetData)
        {
            if (packetData == null || packetData.Length < 4) return;

            ushort opcode = packetData.Length >= 6 ? BitConverter.ToUInt16(packetData, 4) : (ushort)0;

            // Handle World List & World Select standard queries
            if (opcode == (ushort)PacketOpcode.MsgLoginWorldListReq)
            {
                await session.SendAsync(YogurtingPackets.MakeWorldListAns(1));
                await session.SendAsync(YogurtingPackets.MakeWorldListNtf("Estiva", 91));
                return;
            }

            if (opcode == (ushort)PacketOpcode.MsgLoginSelectWorldReq)
            {
                int worldId = packetData.Length >= 10 ? BitConverter.ToInt32(packetData, 6) : 91;
                await session.SendAsync(YogurtingPackets.MakeLoginResumeNtf(1000));
                await session.SendAsync(YogurtingPackets.MakeSchoolListNtf(worldId));
                return;
            }

            bool handled = await _dispatcher.DispatchAsync(session, opcode, packetData);
            if (!handled)
            {
                Logger.Debug($"[LoginServer] Unhandled Opcode 0x{opcode:X4} ({opcode}) from {session.RemoteEndPoint}");
            }
        }

        public Task HandleClientDisconnectedAsync(ClientSession session)
        {
            Logger.Info($"[LoginServer] Client {session.RemoteEndPoint} disconnected.");
            return Task.CompletedTask;
        }
    }
}
