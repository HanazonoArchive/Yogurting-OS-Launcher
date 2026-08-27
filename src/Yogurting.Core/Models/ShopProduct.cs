namespace Yogurting.Core.Models
{
    /// <summary>
    /// Star Cash Shop Product Definition matching ShopItemList.txt.
    /// </summary>
    public sealed class ShopProductDef
    {
        public int ProductId { get; set; }
        public int Price { get; set; }
        public int DisplayOption { get; set; }
        public int PriceType { get; set; }
        public int Period { get => DisplayOption; set => DisplayOption = value; }
        public int Flag { get => PriceType; set => PriceType = value; }
        public System.Collections.Generic.List<int> ItemIds { get; set; } = new();
    }
}
