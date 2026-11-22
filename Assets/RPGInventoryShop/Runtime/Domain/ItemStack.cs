namespace RpgInventoryShop.Domain
{
    public sealed class ItemStack
    {
        internal ItemStack(ItemDefinition item, int quantity)
        {
            Item = item;
            Quantity = quantity;
        }

        public ItemDefinition Item { get; }
        public int Quantity { get; }
    }
}
