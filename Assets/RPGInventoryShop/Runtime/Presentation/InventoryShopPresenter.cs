using System;
using System.Linq;
using RpgInventoryShop.Application;
using RpgInventoryShop.Domain;

namespace RpgInventoryShop.Presentation
{
    public interface IInventoryShopView
    {
        event Action<int> SlotSelected;
        event Action<string> OfferSelected;
        event Action BuyRequested;
        event Action SellRequested;
        event Action UseRequested;

        void Render(InventoryShopScreen screen);
    }

    public sealed class InventoryShopScreen
    {
        public ItemStack[] Slots { get; set; }
        public ItemDefinition[] Offers { get; set; }
        public ItemDefinition Details { get; set; }
        public int SelectedSlot { get; set; }
        public string SelectedOffer { get; set; }
        public int Gold { get; set; }
        public int Hp { get; set; }
        public int MaxHp { get; set; }
        public bool CanBuy { get; set; }
        public bool CanSell { get; set; }
        public bool CanUse { get; set; }
        public string Status { get; set; }
    }

    public sealed class InventoryShopPresenter : IDisposable
    {
        private readonly IInventoryShopView view;
        private readonly Inventory inventory;
        private readonly GoldWallet wallet;
        private readonly PlayerHealth health;
        private readonly ShopService shop;
        private readonly ConsumeItemService consume;
        private readonly ItemDefinition[] offers;
        private int selectedSlot = -1;
        private ItemDefinition selectedOffer;
        private bool inventoryDetails;
        private bool initialized;

        public InventoryShopPresenter(IInventoryShopView view, Inventory inventory, GoldWallet wallet,
            PlayerHealth health, ShopService shop, ConsumeItemService consume, IItemCatalog catalog, string[] offerIds)
        {
            this.view = view;
            this.inventory = inventory;
            this.wallet = wallet;
            this.health = health;
            this.shop = shop;
            this.consume = consume;
            offers = offerIds.Select(id => catalog.TryGet(id, out var item) ? item :
                throw new ArgumentException($"Unknown merchant item '{id}'.", nameof(offerIds))).ToArray();
            selectedOffer = offers.FirstOrDefault();
        }

        public void Initialize()
        {
            if (initialized)
                return;

            initialized = true;
            if (selectedSlot < 0 && inventory.GetSlot(0) != null)
            {
                selectedSlot = 0;
                inventoryDetails = true;
            }

            view.SlotSelected += SelectSlot;
            view.OfferSelected += SelectOffer;
            view.BuyRequested += Buy;
            view.SellRequested += Sell;
            view.UseRequested += Use;
            Refresh();
        }

        public void Dispose()
        {
            if (!initialized)
                return;

            initialized = false;
            view.SlotSelected -= SelectSlot;
            view.OfferSelected -= SelectOffer;
            view.BuyRequested -= Buy;
            view.SellRequested -= Sell;
            view.UseRequested -= Use;
        }

        private void SelectSlot(int index)
        {
            if (index < 0 || index >= Inventory.SlotCount)
                return;
            selectedSlot = index;
            inventoryDetails = true;
            Refresh();
        }

        private void SelectOffer(string id)
        {
            selectedOffer = offers.FirstOrDefault(item => item.Id == id);
            inventoryDetails = false;
            Refresh();
        }

        private void Buy()
        {
            if (selectedOffer == null)
                return;

            ShopResult result = shop.Buy(selectedOffer.Id);
            Refresh(result switch
            {
                ShopResult.Success => "",
                ShopResult.InsufficientGold => "Not enough gold.",
                ShopResult.InventoryFull => "Inventory is full.",
                _ => "Purchase unavailable."
            });
        }

        private void Sell()
        {
            if (!inventoryDetails || selectedSlot < 0)
                return;

            ShopResult result = shop.Sell(selectedSlot);
            Refresh(result == ShopResult.Success ? "" : "Sale unavailable.");
        }

        private void Use()
        {
            if (!inventoryDetails || selectedSlot < 0)
                return;

            ConsumeItemResult result = consume.Use(selectedSlot);
            Refresh(result switch
            {
                ConsumeItemResult.Success => "",
                ConsumeItemResult.HealthFull => "Health is already full.",
                _ => "Item cannot be used."
            });
        }

        private void Refresh(string status = "")
        {
            var slots = new ItemStack[Inventory.SlotCount];
            for (int index = 0; index < slots.Length; index++)
                slots[index] = inventory.GetSlot(index);

            ItemDefinition item = inventoryDetails && selectedSlot >= 0 ? slots[selectedSlot]?.Item : null;
            bool hasSpace = selectedOffer != null && slots.Any(stack => stack == null ||
                (stack.Item.Id == selectedOffer.Id && stack.Quantity < selectedOffer.StackLimit));
            bool canBuy = selectedOffer != null && wallet.CanSpend(selectedOffer.BasePrice) && hasSpace;
            if (status.Length == 0 && selectedOffer != null && !canBuy)
                status = wallet.CanSpend(selectedOffer.BasePrice) ? "Inventory is full." : "Not enough gold.";

            view.Render(new InventoryShopScreen
            {
                Slots = slots, Offers = offers, Details = inventoryDetails ? item : selectedOffer,
                SelectedSlot = inventoryDetails ? selectedSlot : -1,
                SelectedOffer = selectedOffer?.Id, Gold = wallet.Balance, Hp = health.CurrentHp,
                MaxHp = health.MaxHp, CanBuy = canBuy,
                CanSell = item != null && wallet.CanCredit(item.BasePrice / 2),
                CanUse = item != null && item.Healing > 0 && !health.IsFull, Status = status
            });
        }
    }
}
