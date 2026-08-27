using System;
using System.Text;
using System.Threading.Tasks;
using Yogurting.Core.Logging;
using Yogurting.Core.Models;
using Yogurting.Core.Network;

namespace Yogurting.Server.Handlers.Comm
{
    /// <summary>
    /// Handles Chat, Messenger, Friend lists, and Communication server packets (Port 10004).
    /// </summary>
    public sealed class CommHandlers
    {
        private readonly Func<PlayerSessionState, byte[], Task> _broadcastDelegate;

        public CommHandlers(Func<PlayerSessionState, byte[], Task> broadcastDelegate)
        {
            _broadcastDelegate = broadcastDelegate ?? throw new ArgumentNullException(nameof(broadcastDelegate));
        }

        /// <summary>
        /// 0x7963 (31075): MsgGameChatReq / MsgGameChatNtf - Character Campus / Room Chat
        /// </summary>
        [PacketHandler(PacketOpcode.MsgGameChatReq)]
        [PacketHandler(PacketOpcode.MsgGameChatNtf)]
        public async Task HandleChatAsync(PlayerSessionState state, byte[] packetData)
        {
            try
            {
                string message = string.Empty;
                if (packetData.Length >= 46)
                {
                    int len = BitConverter.ToUInt16(packetData, 42);
                    if (len > 0 && packetData.Length >= 44 + (len * 2))
                    {
                        message = Encoding.Unicode.GetString(packetData, 44, len * 2).TrimEnd('\0');
                    }
                }

                if (string.IsNullOrWhiteSpace(message)) return;

                Logger.Info($"[Chat] [{state.Player.CharacterName}]: {message}");

                byte[] broadcastPacket = YogurtingPackets.MakeGameChatNtf(
                    state.Player.CharaId,
                    state.Player.CharacterName,
                    message,
                    0
                );

                await state.Session.SendAsync(broadcastPacket);
                await _broadcastDelegate(state, broadcastPacket);
            }
            catch (Exception ex)
            {
                Logger.Error($"[Chat] Error: {ex.Message}");
            }
        }

        /// <summary>
        /// 0x7759 (30553): MsgCommEchoNtf - Comm Server Keep-Alive
        /// </summary>
        [PacketHandler(PacketOpcode.MsgCommEchoNtf)]
        public async Task HandleCommEchoAsync(PlayerSessionState state, byte[] packetData)
        {
            await state.Session.SendAsync(YogurtingPackets.MakeCommEchoNtf());
        }

        /// <summary>
        /// 0x7728 (30504): MsgCommFriendProposeReq - Friend Invite
        /// </summary>
        [PacketHandler(PacketOpcode.MsgCommFriendProposeReq)]
        public async Task HandleFriendProposeAsync(PlayerSessionState state, byte[] packetData)
        {
            await state.Session.SendAsync(YogurtingPackets.MakeTransJoinCmsAns());
        }
    }
}
