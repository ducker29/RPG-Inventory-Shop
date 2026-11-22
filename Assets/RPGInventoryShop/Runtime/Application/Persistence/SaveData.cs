namespace RpgInventoryShop.Application
{
    public sealed class SaveData
    {
        public const int CurrentVersion = 1;

        public int Version { get; set; }
        public int Gold { get; set; }
        public int Hp { get; set; }
        public SavedSlot[] Slots { get; set; }
    }

    public sealed class SavedSlot
    {
        public string ItemId { get; set; }
        public int Quantity { get; set; }
    }
}
