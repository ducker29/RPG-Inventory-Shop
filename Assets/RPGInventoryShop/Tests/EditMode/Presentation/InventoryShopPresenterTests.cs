using System;
using System.Linq;
using NUnit.Framework;
using RpgInventoryShop.Application;
using RpgInventoryShop.Domain;

namespace RpgInventoryShop.Presentation.Tests
{
    public sealed class InventoryShopPresenterTests
    {
        private static readonly ItemDefinition Potion = new("potion", "Health Potion", 11, 5, 25);
        private Inventory inventory;
        private GoldWallet wallet;
        private PlayerHealth health;
        private PlayerMana mana;
        private View view;
        private Store store;
        private InventoryShopPresenter presenter;

        [SetUp]
        public void SetUp()
        {
            inventory = new Inventory();
            wallet = new GoldWallet(100);
            health = new PlayerHealth(100, 50);
            mana = new PlayerMana(100, 40);
            view = new View();
            store = new Store();
            var catalog = new Catalog();
            presenter = new InventoryShopPresenter(view, inventory, wallet, health, mana,
                new ShopService(inventory, wallet, catalog), new ConsumeItemService(inventory, health, mana),
                store, catalog, new[] { Potion.Id });
        }

        [TearDown]
        public void TearDown() => presenter.Dispose();

        [Test]
        public void BuyingUsingAndSellingRefreshSharedModelsAndView()
        {
            inventory.TryAdd(Potion);
            inventory.TryAdd(Potion);
            presenter.Initialize();
            view.Buy();
            Assert.That(view.Screen.Gold, Is.EqualTo(89));
            Assert.That(view.Screen.Slots[0].Quantity, Is.EqualTo(3));
            Assert.That(view.Screen.SelectedOffer, Is.EqualTo(Potion.Id));

            view.SelectSlot(0);
            view.Use();
            view.Use();
            Assert.That(view.Screen.Hp, Is.EqualTo(100));
            Assert.That(view.Screen.CanUse, Is.False);
            view.Use();
            Assert.That(view.Screen.Slots[0].Quantity, Is.EqualTo(1));
            Assert.That(view.Screen.Status, Does.Contain("already full"));

            view.Sell();
            Assert.That(view.Screen.Gold, Is.EqualTo(94), "Sale rounds half of 11 down to 5.");
            Assert.That(view.Screen.Slots[0], Is.Null);
            Assert.That(view.Screen.Details, Is.Null);
            Assert.That(view.Screen.CanSell, Is.False);
            Assert.That(view.Screen.SelectedSlot, Is.Zero);
        }

        [Test]
        public void SelectionSwitchesBetweenMerchantAndInventoryAndGuardsActions()
        {
            inventory.TryAdd(Potion);
            presenter.Initialize();
            Assert.That(view.Screen.SelectedOffer, Is.EqualTo(Potion.Id));
            Assert.That(view.Screen.SelectedSlot, Is.EqualTo(-1));
            Assert.That(view.Screen.CanBuy, Is.True);
            Assert.That(view.Screen.CanSell || view.Screen.CanUse, Is.False);

            view.SelectSlot(0);
            Assert.That(view.Screen.SelectedOffer, Is.Null);
            Assert.That(view.Screen.SelectedSlot, Is.Zero);
            Assert.That(view.Screen.Details, Is.SameAs(Potion));
            Assert.That(view.Screen.CanBuy, Is.False);
            Assert.That(view.Screen.CanSell && view.Screen.CanUse, Is.True);
            view.Buy();

            view.SelectOffer(Potion.Id);
            Assert.That(view.Screen.SelectedSlot, Is.EqualTo(-1));
            Assert.That(view.Screen.Details, Is.SameAs(Potion));
            Assert.That(view.Screen.CanSell || view.Screen.CanUse, Is.False);
            view.Sell();
            view.Use();
            Assert.That(wallet.Balance, Is.EqualTo(100));
            Assert.That(inventory.GetSlot(0).Quantity, Is.EqualTo(1));
            Assert.That(health.CurrentHp, Is.EqualTo(50));
        }

        [Test]
        public void EmptyMerchantStartsWithoutSelectingAnInventoryItem()
        {
            inventory.TryAdd(Potion);
            var catalog = new Catalog();
            using var emptyMerchant = new InventoryShopPresenter(view, inventory, wallet, health, mana,
                new ShopService(inventory, wallet, catalog), new ConsumeItemService(inventory, health, mana),
                store, catalog, Array.Empty<string>());
            emptyMerchant.Initialize();

            Assert.That(view.Screen.SelectedOffer, Is.Null);
            Assert.That(view.Screen.SelectedSlot, Is.EqualTo(-1));
            Assert.That(view.Screen.Details, Is.Null);
            Assert.That(view.Screen.CanBuy || view.Screen.CanSell || view.Screen.CanUse, Is.False);
        }

        [Test]
        public void EmptySlotAndMissingOfferDisableTheirActions()
        {
            presenter.Initialize();
            view.SelectSlot(7);
            Assert.That(view.Screen.SelectedOffer, Is.Null);
            Assert.That(view.Screen.CanBuy, Is.False);
            Assert.That(view.Screen.Details, Is.Null);
            Assert.That(view.Screen.CanSell, Is.False);
            Assert.That(view.Screen.CanUse, Is.False);

            view.SelectOffer("missing");
            Assert.That(view.Screen.CanBuy, Is.False);
            Assert.That(view.Screen.Details, Is.Null);
            view.Buy();
            Assert.That(wallet.Balance, Is.EqualTo(100));
        }

        [TestCase(false, "Not enough gold.")]
        [TestCase(true, "Inventory is full.")]
        public void PurchaseRefusalIsShownAndPreservesState(bool full, string status)
        {
            if (full)
                Assert.That(inventory.TryRestoreSlots(
                    Enumerable.Repeat((Potion, Potion.StackLimit), Inventory.SlotCount).ToArray()), Is.True);
            else
                wallet.RestoreBalance(10);

            presenter.Initialize();
            Assert.That(view.Screen.CanBuy, Is.False);
            Assert.That(view.Screen.Status, Is.EqualTo(status));
            view.Buy();
            Assert.That(view.Screen.Status, Is.EqualTo(status));
            Assert.That(view.Screen.Gold, Is.EqualTo(full ? 100 : 10));
            Assert.That(view.Screen.Slots.Sum(slot => slot?.Quantity ?? 0), Is.EqualTo(full ? 100 : 0));
        }

        [Test]
        public void InsufficientGoldMessageFollowsSelectionAndUpdatedBalance()
        {
            inventory.TryAdd(Potion);
            wallet.RestoreBalance(6);
            presenter.Initialize();
            Assert.That(view.Screen.Status, Is.EqualTo("Not enough gold."));
            view.SelectSlot(7);
            Assert.That(view.Screen.SelectedOffer, Is.Null);
            Assert.That(view.Screen.CanBuy, Is.False);
            Assert.That(view.Screen.Status, Is.Empty);

            view.SelectSlot(0);
            Assert.That(view.Screen.Status, Is.Empty);
            view.Buy();
            Assert.That(view.Screen.Status, Is.Empty);
            view.Sell();
            Assert.That(view.Screen.Status, Is.Empty);

            view.SelectOffer(Potion.Id);
            Assert.That(view.Screen.CanBuy, Is.True);
            Assert.That(view.Screen.Status, Is.Empty);
            view.Buy();
            Assert.That(view.Screen.CanBuy, Is.False);
            Assert.That(view.Screen.Status, Is.EqualTo("Not enough gold."));

            view.SelectOffer("missing");
            Assert.That(view.Screen.Status, Is.Empty);
        }

        [Test]
        public void ReinitializationDoesNotDuplicateSubscriptions()
        {
            presenter.Initialize();
            presenter.Initialize();
            view.Buy();
            Assert.That(wallet.Balance, Is.EqualTo(89));
            presenter.Dispose();
            view.Buy();
            Assert.That(wallet.Balance, Is.EqualTo(89));
            presenter.Initialize();
            view.Buy();
            Assert.That(wallet.Balance, Is.EqualTo(78));
        }

        [Test]
        public void SaveAndLoadRenderFeedbackAndOnlySuccessfulLoadResetsSelection()
        {
            inventory.TryAdd(Potion);
            presenter.Initialize();
            view.SelectSlot(0);

            view.Save();
            Assert.That(store.SaveCalls, Is.EqualTo(1));
            Assert.That(view.Screen.SaveStatusMessage, Is.EqualTo("Game saved."));
            Assert.That(view.Screen.SelectedSlot, Is.Zero);

            view.Load();
            Assert.That(view.Screen.SaveStatusMessage, Is.EqualTo("No save found."));
            Assert.That(view.Screen.SelectedSlot, Is.Zero);
            Assert.That(view.Screen.ResetMerchantScroll, Is.False);

            store.LoadResult = SaveStatus.RecoveredBackup;
            store.ApplySnapshot = () =>
            {
                wallet.RestoreBalance(7);
                health.RestoreHp(62);
                mana.RestoreMana(17);
                inventory.TryRemoveAt(0);
            };
            view.Load();
            Assert.That(view.Screen.SaveStatusMessage, Is.EqualTo("Game loaded from backup."));
            Assert.That(view.Screen.SelectedOffer, Is.EqualTo(Potion.Id));
            Assert.That(view.Screen.SelectedSlot, Is.EqualTo(-1));
            Assert.That(view.Screen.ResetMerchantScroll, Is.True);
            Assert.That(view.Screen.Gold, Is.EqualTo(7));
            Assert.That(view.Screen.Hp, Is.EqualTo(62));
            Assert.That(view.Screen.Mp, Is.EqualTo(17));
            Assert.That(view.Screen.Slots[0], Is.Null);
        }

        private sealed class Store : ISaveStore
        {
            public int SaveCalls { get; private set; }
            public SaveStatus LoadResult { get; set; } = SaveStatus.Missing;
            public Action ApplySnapshot { get; set; }

            public SaveStatus Save()
            {
                SaveCalls++;
                return SaveStatus.Success;
            }

            public SaveStatus Load()
            {
                ApplySnapshot?.Invoke();
                return LoadResult;
            }
        }

        private sealed class Catalog : IItemCatalog
        {
            public bool TryGet(string itemId, out ItemDefinition item)
            {
                item = itemId == Potion.Id ? Potion : null;
                return item != null;
            }
        }

        private sealed class View : IInventoryShopView
        {
            public event Action<int> SlotSelected;
            public event Action<string> OfferSelected;
            public event Action BuyRequested;
            public event Action SellRequested;
            public event Action UseRequested;
            public event Action SaveRequested;
            public event Action LoadRequested;
            public InventoryShopScreen Screen { get; private set; }

            public void Render(InventoryShopScreen screen) => Screen = screen;
            public void SelectSlot(int index) => SlotSelected?.Invoke(index);
            public void SelectOffer(string id) => OfferSelected?.Invoke(id);
            public void Buy() => BuyRequested?.Invoke();
            public void Sell() => SellRequested?.Invoke();
            public void Use() => UseRequested?.Invoke();
            public void Save() => SaveRequested?.Invoke();
            public void Load() => LoadRequested?.Invoke();
        }
    }
}
