using System;
using Yogurting.Core.Network;

namespace Yogurting.Core.Models
{
    /// <summary>
    /// Represents the live session and in-memory state of an active player connection.
    /// Bridges the low-level <see cref="ClientSession"/> TCP socket with high-level game entity logic.
    /// </summary>
    public sealed class PlayerSessionState
    {
        public ClientSession Session { get; }
        public Player Player { get; }
        public int EntityId { get; }
        public DateTime ConnectedAt { get; }
        public DateTime LastPacketAt { get; set; }
        public int PendingWarpFieldId { get; set; }
        public Position PendingWarpPosition { get; set; }
        public DateTime LastWarpAt { get; set; } = DateTime.MinValue;
        public int ActiveNpcId { get; set; }
        public int ActiveDialogId { get; set; }
        public string CurrentNpcDialogNode { get; set; } = string.Empty;

        public PlayerSessionState(ClientSession session, Player player, int entityId)
        {
            Session = session ?? throw new ArgumentNullException(nameof(session));
            Player = player ?? throw new ArgumentNullException(nameof(player));
            EntityId = entityId;
            ConnectedAt = DateTime.UtcNow;
            LastPacketAt = DateTime.UtcNow;
        }

        public override string ToString() => $"[Entity #{EntityId}] {Player.CharacterName} ({Player.School}, Field {Player.FieldId})";
    }
}
