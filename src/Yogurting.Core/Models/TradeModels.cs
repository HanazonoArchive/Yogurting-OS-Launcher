using System;

namespace Yogurting.Core.Models
{
    /// <summary>
    /// Represents a single item slot in the 5-slot trade basket.
    /// Byte-exact layout matches Delphi Quartet TMsgGameTradeOtherSideBasketInfoNtf (_Unit47.pas:52062).
    /// </summary>
    public class TradeSlot
    {
        public int ItemType { get; set; } = 0;
        public ushort Count { get; set; } = 0;
        public ushort Dim2Index { get; set; } = 0;
        public int ItemId { get; set; } = 0;
        public int[] ReinforceSlots { get; set; } = new int[5];

        public bool IsEmpty => ItemType == 0 || Count == 0;

        public void Clear()
        {
            ItemType = 0;
            Count = 0;
            Dim2Index = 0;
            ItemId = 0;
            Array.Clear(ReinforceSlots, 0, ReinforceSlots.Length);
        }

        public TradeSlot Clone()
        {
            return new TradeSlot
            {
                ItemType = ItemType,
                Count = Count,
                Dim2Index = Dim2Index,
                ItemId = ItemId,
                ReinforceSlots = (int[])ReinforceSlots.Clone()
            };
        }
    }

    /// <summary>
    /// Lifecycle state of an active trade session between two players.
    /// Matches Delphi Quartet TChara.TradeState (_Unit49.pas:22030-22250).
    /// </summary>
    public enum TradeState
    {
        None = 0,
        Proposed = 1,
        Trading = 2,
        Locked = 3,
        FinalConfirmed = 4,
        Completed = 5
    }

    /// <summary>
    /// Friend presence and information entry for CommServer.
    /// </summary>
    public class FriendEntry
    {
        public int CharacterId { get; set; }
        public int PhoneNumber { get; set; }
        public string CharacterName { get; set; } = string.Empty;
        public bool IsOnline { get; set; }
        public int FieldId { get; set; }
    }
}
