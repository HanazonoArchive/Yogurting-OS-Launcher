using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Yogurting.Core.Logging;
using Yogurting.Core.Models;
using Yogurting.Core.Network;
using Yogurting.Data.Repositories;

namespace Yogurting.Server.World
{
    /// <summary>
    /// Global Coordinator for Community Server (Port 10004).
    /// Tracks online presence, routes cross-server whispers, and synchronizes friend rosters.
    /// Reverse-engineered from Delphi Quartet TCommSession (_Unit67.pas:006C620C-006C6450).
    /// </summary>
    public sealed class CommManager
    {
        private static readonly Lazy<CommManager> _instance = new(() => new CommManager());
        public static CommManager Instance => _instance.Value;

        private readonly ConcurrentDictionary<int, (Player Player, ClientSession Session)> _onlinePlayers = new();

        public void RegisterOnline(Player player, ClientSession session)
        {
            _onlinePlayers[player.CharacterId] = (player, session);
            Logger.Info($"[CommManager] Player '{player.CharacterName}' (ID: {player.CharacterId}) registered online in CommServer.");
        }

        public void Unregister(int characterId)
        {
            _onlinePlayers.TryRemove(characterId, out _);
        }

        public bool IsOnline(int characterId) => _onlinePlayers.ContainsKey(characterId);

        public List<FriendEntry> BuildFriendList(Player player, IAccountRepository? repository = null)
        {
            var result = new List<FriendEntry>();
            if (player.FriendIds == null) return result;

            foreach (var friendId in player.FriendIds)
            {
                if (_onlinePlayers.TryGetValue(friendId, out var online))
                {
                    result.Add(new FriendEntry
                    {
                        CharacterId = online.Player.CharacterId,
                        PhoneNumber = int.TryParse(online.Player.TelNumber, out var tel) ? tel : 3456,
                        CharacterName = online.Player.CharacterName,
                        IsOnline = true,
                        FieldId = online.Player.FieldId
                    });
                }
                else
                {
                    result.Add(new FriendEntry
                    {
                        CharacterId = friendId,
                        PhoneNumber = 3456,
                        CharacterName = $"Student #{friendId}",
                        IsOnline = false,
                        FieldId = 0
                    });
                }
            }

            return result;
        }

        public async Task<bool> SendWhisperAsync(int senderId, string targetName, string message)
        {
            var target = _onlinePlayers.Values.FirstOrDefault(p =>
                string.Equals(p.Player.CharacterName, targetName, StringComparison.OrdinalIgnoreCase));

            if (target.Session == null) return false;

            // In Yogurting, whisper format is sent via chat packet to target
            var sender = _onlinePlayers.TryGetValue(senderId, out var s) ? s.Player.CharacterName : "System";
            byte[] chatPkt = YogurtingPackets.MakeGameChatNtf(senderId, sender, $"[Whisper] {message}", 0);
            await target.Session.SendAsync(chatPkt);
            return true;
        }
    }
}
