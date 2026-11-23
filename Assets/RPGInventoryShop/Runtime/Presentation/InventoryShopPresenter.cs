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
        event Action SaveRequested;
        event Action LoadRequested;

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
        public int Mp { get; set; }
        public int MaxMp { get; set; }
        public bool CanBuy { get; set; }
        public bool CanSell { get; set; }
        public bool CanUse { get; set; }
        public string Status { get; set; }
        public string SaveStatusMessage { get; set; }
        public bool ResetMerchantScroll { get; set; }
    }

    public sealed class InventoryShopPresenter : IDisposable
    {
        private readonly IInventoryShopView view;
        private readonly Inventory inventory;
        private readonly GoldWallet wallet;
        private readonly PlayerHealth health;
        private readonly PlayerMana mana;
        private readonly ShopService shop;
        private readonly ConsumeItemService consume;
        private readonly ISaveStore saveStore;
        private readonly ItemDefinition[] offers;
        private (int Slot, ItemDefinition Offer) selection;
        private bool initialized;

        public InventoryShopPresenter(IInventoryShopView view, Inventory inventory, GoldWallet wallet,
            PlayerHealth health, PlayerMana mana, ShopService shop, ConsumeItemService consume,
            ISaveStore saveStore, IItemCatalog catalog, string[] offerIds)
        {
            this.view = view;
            this.inventory = inventory;
            this.wallet = wallet;
            this.health = health;
            this.mana = mana;
            this.shop = shop;
            this.consume = consume;
            this.saveStore = saveStore ?? throw new ArgumentNullException(nameof(saveStore));
            offers = offerIds.Select(id => catalog.TryGet(id, out var item) ? item :
                throw new ArgumentException($"Unknown merchant item '{id}'.", nameof(offerIds))).ToArray();
            selection = (-1, offers.FirstOrDefault());
        }

        public void Initialize()
        {
            if (initialized)
                return;

            initialized = true;

            view.SlotSelected += SelectSlot;
            view.OfferSelected += SelectOffer;
            view.BuyRequested += Buy;
            view.SellRequested += Sell;
            view.UseRequested += Use;
            view.SaveRequested += Save;
            view.LoadRequested += Load;
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
            view.SaveRequested -= Save;
            view.LoadRequested -= Load;
        }

        private void SelectSlot(int index)
        {
            if (index < 0 || index >= Inventory.SlotCount)
                return;
            selection = (index, null);
            Refresh();
        }

        private void SelectOffer(string id)
        {
            selection = (-1, offers.FirstOrDefault(item => item.Id == id));
            Refresh();
        }

        private void Buy()
        {
            if (selection.Offer == null)
                return;

            ShopResult result = shop.Buy(selection.Offer.Id);
            Refresh(result switch
            {
                ShopResult.Success => "",
                ShopResult.InsufficientGold => "",
                ShopResult.InventoryFull => "Inventory is full.",
                _ => "Purchase unavailable."
            });
        }

        private void Sell()
        {
            if (selection.Slot < 0 || inventory.GetSlot(selection.Slot) == null)
                return;

            ShopResult result = shop.Sell(selection.Slot);
            Refresh(result == ShopResult.Success ? "" : "Sale unavailable.");
        }

        private void Use()
        {
            if (selection.Slot < 0 || inventory.GetSlot(selection.Slot) == null)
                return;

            ConsumeItemResult result = consume.Use(selection.Slot);
            Refresh(result switch
            {
                ConsumeItemResult.Success => "",
                ConsumeItemResult.HealthFull => "Health is already full.",
                ConsumeItemResult.ManaFull => "Mana is already full.",
                _ => "Item cannot be used."
            });
        }

        private void Save()
        {
            SaveStatus result = saveStore.Save();
            Refresh(saveMessage: result switch
            {
                SaveStatus.Success => "Game saved.",
                SaveStatus.NewerVersion => "Save version is newer.",
                SaveStatus.IndeterminateVersion => "Cannot verify save version.",
                _ => "Could not save game."
            });
        }

        private void Load()
        {
            SaveStatus result = saveStore.Load();
            bool loaded = result == SaveStatus.Success || result == SaveStatus.RecoveredBackup;
            if (loaded)
                selection = (-1, offers.FirstOrDefault());

            Refresh(saveMessage: result switch
            {
                SaveStatus.Success => "Game loaded.",
                SaveStatus.RecoveredBackup => "Game loaded from backup.",
                SaveStatus.Missing => "No save found.",
                SaveStatus.NewerVersion => "Save version is newer.",
                SaveStatus.UnsupportedVersion => "Unsupported save version.",
                SaveStatus.IoError => "Could not read save.",
                _ => "Save data is invalid."
            }, resetMerchantScroll: loaded);
        }

        private void Refresh(string status = "", string saveMessage = "", bool resetMerchantScroll = false)
        {
            var slots = new ItemStack[Inventory.SlotCount];
            for (int index = 0; index < slots.Length; index++)
                slots[index] = inventory.GetSlot(index);

            var (selectedSlot, selectedOffer) = selection;
            ItemDefinition item = selectedSlot >= 0 ? slots[selectedSlot]?.Item : null;
            bool hasSpace = selectedOffer != null && slots.Any(stack => stack == null ||
                (stack.Item.Id == selectedOffer.Id && stack.Quantity < selectedOffer.StackLimit));
            bool canAffordOffer = selectedOffer != null && wallet.CanSpend(selectedOffer.BasePrice);
            bool canBuy = canAffordOffer && hasSpace;
            if (selectedOffer != null && !canAffordOffer)
                status = "Not enough gold.";
            else if (status.Length == 0 && canAffordOffer && !hasSpace)
                status = "Inventory is full.";

            view.Render(new InventoryShopScreen
            {
                Slots = slots, Offers = offers, Details = item ?? selectedOffer,
                SelectedSlot = selectedSlot,
                SelectedOffer = selectedOffer?.Id, Gold = wallet.Balance, Hp = health.CurrentHp,
                MaxHp = health.MaxHp, Mp = mana.CurrentMana, MaxMp = mana.MaxMana, CanBuy = canBuy,
                CanSell = item != null && wallet.CanCredit(item.BasePrice / 2),
                CanUse = consume.CanUse(selectedSlot), Status = status,
                SaveStatusMessage = saveMessage, ResetMerchantScroll = resetMerchantScroll
            });
        }
    }
}
