using NUnit.Framework;
using RpgInventoryShop.Application;
using RpgInventoryShop.Domain;

namespace RpgInventoryShop.Core.Tests
{
    public sealed class ShopServiceTests
    {
        private static readonly ItemDefinition Potion = new ItemDefinition("potion", "Potion", 5, 3, 10);
        private static readonly ItemDefinition Sword = new ItemDefinition("sword", "Sword", 15, 1);

        [Test]
        public void BuysOneItemIntoExistingStackWithExactGold()
        {
            var inventory = new Inventory();
            var wallet = new GoldWallet(5);
            inventory.TryAdd(Potion);
            var shop = new ShopService(inventory, wallet, new TestCatalog(Potion));

            Assert.That(shop.Buy(Potion.Id), Is.EqualTo(ShopResult.Success));
            Assert.That(wallet.Balance, Is.Zero);
            Assert.That(inventory.GetSlot(0).Quantity, Is.EqualTo(2));
            Assert.That(inventory.GetSlot(1), Is.Null);
        }

        [Test]
        public void RefusesPurchaseWithConflictingDefinitionWithoutChargingGold()
        {
            var inventory = new Inventory();
            inventory.TryAdd(Potion);
            ItemStack original = inventory.GetSlot(0);
            var wallet = new GoldWallet(20);
            var conflicting = new ItemDefinition(Potion.Id, Potion.Name, 7, 1, 30);
            var shop = new ShopService(inventory, wallet, new TestCatalog(conflicting));

            Assert.That(shop.Buy(Potion.Id), Is.EqualTo(ShopResult.ConflictingItemDefinition));
            Assert.That(wallet.Balance, Is.EqualTo(20));
            Assert.That(inventory.GetSlot(0), Is.SameAs(original));
            Assert.That(inventory.GetSlot(1), Is.Null);
        }

        [TestCase("missing", 99, ShopResult.UnknownItem)]
        [TestCase("potion", 4, ShopResult.InsufficientGold)]
        public void RefusedPurchasePreservesWalletAndInventory(string id, int gold, ShopResult expected)
        {
            var inventory = new Inventory();
            var wallet = new GoldWallet(gold);
            inventory.TryAdd(Potion);
            ItemStack original = inventory.GetSlot(0);
            var shop = new ShopService(inventory, wallet, new TestCatalog(Potion));

            Assert.That(shop.Buy(id), Is.EqualTo(expected));
            Assert.That(inventory.GetSlot(0), Is.SameAs(original));
            Assert.That(inventory.GetSlot(1), Is.Null);
            Assert.That(wallet.Balance, Is.EqualTo(gold));
        }

        [Test]
        public void FullInventoryRefusesPurchaseButAllowsBuyingIntoPartialStack()
        {
            var inventory = new Inventory();
            var wallet = new GoldWallet(20);
            inventory.TryAdd(Potion);
            for (int i = 1; i < Inventory.SlotCount; i++)
                inventory.TryAdd(Sword);
            var shop = new ShopService(inventory, wallet, new TestCatalog(Potion));

            Assert.That(shop.Buy(Potion.Id), Is.EqualTo(ShopResult.Success));
            Assert.That(shop.Buy(Potion.Id), Is.EqualTo(ShopResult.Success));
            var original = new ItemStack[Inventory.SlotCount];
            for (int i = 0; i < original.Length; i++)
                original[i] = inventory.GetSlot(i);

            Assert.That(shop.Buy(Potion.Id), Is.EqualTo(ShopResult.InventoryFull));
            Assert.That(wallet.Balance, Is.EqualTo(10));
            Assert.That(inventory.GetSlot(0).Quantity, Is.EqualTo(3));
            for (int i = 0; i < original.Length; i++)
                Assert.That(inventory.GetSlot(i), Is.SameAs(original[i]));
        }

        [Test]
        public void SellsOneItemFromSelectedStackAtHalfPriceRoundedDown()
        {
            var inventory = new Inventory();
            var wallet = new GoldWallet(10);
            for (int i = 0; i < 5; i++)
                inventory.TryAdd(Potion);
            var shop = new ShopService(inventory, wallet, new TestCatalog(Potion));

            Assert.That(shop.Sell(1), Is.EqualTo(ShopResult.Success));
            Assert.That(wallet.Balance, Is.EqualTo(12));
            Assert.That(inventory.GetSlot(0).Quantity, Is.EqualTo(3));
            Assert.That(inventory.GetSlot(1).Quantity, Is.EqualTo(1));

            Assert.That(shop.Sell(1), Is.EqualTo(ShopResult.Success));
            Assert.That(wallet.Balance, Is.EqualTo(14));
            Assert.That(inventory.GetSlot(1), Is.Null);
        }

        [TestCase(-1, 0, ShopResult.InvalidSlot)]
        [TestCase(20, 0, ShopResult.InvalidSlot)]
        [TestCase(1, 0, ShopResult.EmptySlot)]
        [TestCase(0, int.MaxValue, ShopResult.WalletLimitReached)]
        public void RefusedSalePreservesWalletAndInventory(int slot, int gold, ShopResult expected)
        {
            var inventory = new Inventory();
            var wallet = new GoldWallet(gold);
            inventory.TryAdd(Potion);
            ItemStack original = inventory.GetSlot(0);
            var shop = new ShopService(inventory, wallet, new TestCatalog(Potion));

            Assert.That(shop.Sell(slot), Is.EqualTo(expected));
            Assert.That(inventory.GetSlot(0), Is.SameAs(original));
            Assert.That(inventory.GetSlot(1), Is.Null);
            Assert.That(wallet.Balance, Is.EqualTo(gold));
        }

        private sealed class TestCatalog : IItemCatalog
        {
            private readonly ItemDefinition item;

            public TestCatalog(ItemDefinition item) => this.item = item;

            public bool TryGet(string itemId, out ItemDefinition definition)
            {
                definition = item.Id == itemId ? item : null;
                return definition != null;
            }
        }
    }
}
