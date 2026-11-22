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
        private View view;
        private InventoryShopPresenter presenter;

        [SetUp]
        public void SetUp()
        {
            inventory = new Inventory();
            wallet = new GoldWallet(100);
            health = new PlayerHealth(100, 50);
            view = new View();
            var catalog = new Catalog();
            presenter = new InventoryShopPresenter(view, inventory, wallet, health,
                new ShopService(inventory, wallet, catalog), new ConsumeItemService(inventory, health),
                catalog, new[] { Potion.Id });
        }

        [TearDown]
        public void TearDown() => presenter.Dispose();

        [Test]
        public void BuyingUsingAndSellingRefreshSharedModelsAndView()
        {
            inventory.TryAdd(Potion);
            inventory.TryAdd(Potion);
            presenter.Initialize();
            Assert.That(view.Screen.Details, Is.SameAs(Potion));
            Assert.That(view.Screen.SelectedSlot, Is.Zero);

            view.Buy();
            Assert.That(view.Screen.Gold, Is.EqualTo(89));
            Assert.That(view.Screen.Slots[0].Quantity, Is.EqualTo(3));

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
        }

        [Test]
        public void EmptySlotAndMissingOfferDisableTheirActions()
        {
            presenter.Initialize();
            view.SelectSlot(7);
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
            public InventoryShopScreen Screen { get; private set; }

            public void Render(InventoryShopScreen screen) => Screen = screen;
            public void SelectSlot(int index) => SlotSelected?.Invoke(index);
            public void SelectOffer(string id) => OfferSelected?.Invoke(id);
            public void Buy() => BuyRequested?.Invoke();
            public void Sell() => SellRequested?.Invoke();
            public void Use() => UseRequested?.Invoke();
        }
    }
}
