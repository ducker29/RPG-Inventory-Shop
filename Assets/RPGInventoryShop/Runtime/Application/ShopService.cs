using System;
using RpgInventoryShop.Domain;

namespace RpgInventoryShop.Application
{
    public enum ShopResult
    {
        Success,
        UnknownItem,
        InsufficientGold,
        InventoryFull,
        InvalidSlot,
        EmptySlot,
        WalletLimitReached,
        ConflictingItemDefinition
    }

    public sealed class ShopService
    {
        private readonly Inventory inventory;
        private readonly GoldWallet wallet;
        private readonly IItemCatalog catalog;

        public ShopService(Inventory inventory, GoldWallet wallet, IItemCatalog catalog)
        {
            this.inventory = inventory ?? throw new ArgumentNullException(nameof(inventory));
            this.wallet = wallet ?? throw new ArgumentNullException(nameof(wallet));
            this.catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
        }

        public ShopResult Buy(string itemId)
        {
            if (string.IsNullOrWhiteSpace(itemId) || !catalog.TryGet(itemId, out ItemDefinition item))
                return ShopResult.UnknownItem;

            if (!wallet.CanSpend(item.BasePrice))
                return ShopResult.InsufficientGold;

            InventoryAddResult added = inventory.Add(item);
            if (added == InventoryAddResult.ConflictingItemDefinition)
                return ShopResult.ConflictingItemDefinition;
            if (added == InventoryAddResult.Full)
                return ShopResult.InventoryFull;

            // Session operations run synchronously on one thread.
            wallet.TrySpend(item.BasePrice);
            return ShopResult.Success;
        }

        public ShopResult Sell(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= Inventory.SlotCount)
                return ShopResult.InvalidSlot;

            ItemStack stack = inventory.GetSlot(slotIndex);
            if (stack == null)
                return ShopResult.EmptySlot;

            int salePrice = stack.Item.BasePrice / 2;
            if (!wallet.TryCredit(salePrice))
                return ShopResult.WalletLimitReached;

            inventory.TryRemoveAt(slotIndex);
            return ShopResult.Success;
        }
    }
}
