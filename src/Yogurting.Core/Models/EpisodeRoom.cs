using System;
using System.Collections.Generic;

namespace Yogurting.Core.Models
{
    /// <summary>
    /// Represents a player participating in an Episode Waiting Room.
    /// Byte-exact layout matches Delphi pc_info (struct.dms).
    /// </summary>
    public class WaitRoomMember
    {
        public int CharacterId { get; set; }
        public string CharacterName { get; set; } = string.Empty;
        public byte Gender { get; set; } = 1;
        public byte Grade { get; set; } = 1;
        public int WeaponTypeId { get; set; } = 140001;
        public ushort Weapon => (ushort)(WeaponTypeId % 10000);
        public ushort TeamId { get; set; } = 0;
        public int PhoneNumber { get; set; } = 3456;
        public int PromotionId { get; set; } = 0;
        public bool IsReady { get; set; } = false;
        public bool IsHost { get; set; } = false;
    }

    /// <summary>
    /// Represents an Episode Lobby / Matchmaking Room.
    /// Byte-exact layout matches Delphi room_info (struct.dms / 30329.dms).
    /// </summary>
    public class EpisodeRoom
    {
        public ushort RoomId { get; set; } = 1;
        public ushort LobbyId { get; set; } = 1;
        public byte Status { get; set; } = 0; // 0 = Open/Waiting, 1 = Full, 2 = InProgress
        public string Title { get; set; } = "Episode Mission";
        public uint EpisodeTypeId { get; set; } = 101;
        public byte MaxUsers { get; set; } = 4;
        public byte CurrentUsers => (byte)Members.Count;
        public byte TeamCount { get; set; } = 1;
        public byte HasPassword { get; set; } = 0;
        public byte PkMode { get; set; } = 0;
        public byte LimitMilk { get; set; } = 0;
        public byte IsWaiting { get; set; } = 1;
        public byte ClearRate { get; set; } = 0;
        public float CalorieEnter { get; set; } = 0f;
        public float CalorieConsume { get; set; } = 0f;
        public string Password { get; set; } = string.Empty;

        public List<WaitRoomMember> Members { get; set; } = new();
    }
}
