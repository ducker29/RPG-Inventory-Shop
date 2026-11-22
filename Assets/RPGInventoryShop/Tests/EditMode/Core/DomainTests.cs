using System;
using NUnit.Framework;
using RpgInventoryShop.Domain;

namespace RpgInventoryShop.Core.Tests
{
    public sealed class ItemDefinitionTests
    {
        [TestCase("", "Potion", "id")]
        [TestCase("  ", "Potion", "id")]
        [TestCase("potion", "", "name")]
        public void RejectsBlankText(string id, string name, string parameter)
        {
            var error = Assert.Throws<ArgumentException>(() => new ItemDefinition(id, name, 5, 2));
            Assert.That(error.ParamName, Is.EqualTo(parameter));
        }

        [TestCase(-1, 2, 0, "basePrice")]
        [TestCase(5, 0, 0, "stackLimit")]
        [TestCase(5, -1, 0, "stackLimit")]
        [TestCase(5, 2, -1, "healing")]
        public void RejectsOutOfRangeNumbers(int price, int stackLimit, int healing, string parameter)
        {
            var error = Assert.Throws<ArgumentOutOfRangeException>(
                () => new ItemDefinition("potion", "Potion", price, stackLimit, healing));
            Assert.That(error.ParamName, Is.EqualTo(parameter));
        }
    }

    public sealed class InventoryTests
    {
        [TestCase("Other name", 5, 3, 10)]
        [TestCase("Potion", 6, 3, 10)]
        [TestCase("Potion", 5, 1, 10)]
        [TestCase("Potion", 5, 3, 20)]
        public void RejectsConflictingDefinitionEvenAfterLastUnitIsRemoved(
            string name, int price, int limit, int healing)
        {
            var inventory = new Inventory();
            var original = new ItemDefinition("potion", "Potion", 5, 3, 10);
            var conflicting = new ItemDefinition(original.Id, name, price, limit, healing);
            inventory.TryAdd(original);
            ItemStack stack = inventory.GetSlot(0);

            Assert.That(inventory.Add(conflicting), Is.EqualTo(InventoryAddResult.ConflictingItemDefinition));
            Assert.That(inventory.GetSlot(0), Is.SameAs(stack));
            Assert.That(inventory.GetSlot(1), Is.Null);
            Assert.That(inventory.TryRemoveAt(0), Is.True);
            Assert.That(inventory.Add(conflicting), Is.EqualTo(InventoryAddResult.ConflictingItemDefinition));
            Assert.That(inventory.GetSlot(0), Is.Null);

            var equivalent = new ItemDefinition(original.Id, "Potion", 5, 3, 10);
            Assert.That(inventory.TryAdd(equivalent), Is.True);
            Assert.That(inventory.TryAdd(equivalent), Is.True);
            Assert.That(inventory.GetSlot(0).Item, Is.SameAs(original));
            Assert.That(inventory.GetSlot(0).Quantity, Is.EqualTo(2));
        }

        [Test]
        public void HoldsExactlyTwentyNonStackableItems()
        {
            var inventory = new Inventory();
            var sword = new ItemDefinition("sword", "Sword", 15, 1);

            Assert.That(Inventory.SlotCount, Is.EqualTo(20));
            for (int i = 0; i < 20; i++)
                Assert.That(inventory.TryAdd(sword), Is.True);

            Assert.That(inventory.TryAdd(sword), Is.False);
            for (int i = 0; i < 20; i++)
            {
                Assert.That(inventory.GetSlot(i).Item, Is.SameAs(sword));
                Assert.That(inventory.GetSlot(i).Quantity, Is.EqualTo(1));
            }
        }

        [Test]
        public void StackingPrefersExistingStackEvenWhenEarlierSlotIsEmpty()
        {
            var inventory = new Inventory();
            var blocker = new ItemDefinition("sword", "Sword", 15, 1);
            var potion = new ItemDefinition("potion", "Potion", 5, 2, 10);
            inventory.TryAdd(blocker);
            inventory.TryAdd(potion);
            inventory.TryRemoveAt(0);

            Assert.That(inventory.TryAdd(potion), Is.True);
            Assert.That(inventory.GetSlot(0), Is.Null);
            Assert.That(inventory.GetSlot(1).Quantity, Is.EqualTo(2));
            Assert.That(inventory.TryAdd(potion), Is.True);
            Assert.That(inventory.GetSlot(0).Quantity, Is.EqualTo(1));

            Assert.That(inventory.TryRemoveAt(1), Is.True);
            Assert.That(inventory.GetSlot(1).Quantity, Is.EqualTo(1));
            Assert.That(inventory.TryRemoveAt(1), Is.True);
            Assert.That(inventory.GetSlot(1), Is.Null);
            Assert.That(inventory.GetSlot(0).Quantity, Is.EqualTo(1));
        }
    }

    public sealed class GoldWalletTests
    {
        [Test]
        public void SpendsAndCreditsGold()
        {
            var wallet = new GoldWallet(10);

            Assert.That(wallet.TrySpend(10), Is.True);
            Assert.That(wallet.Balance, Is.Zero);
            Assert.That(wallet.TryCredit(7), Is.True);
            Assert.That(wallet.Balance, Is.EqualTo(7));
        }

        [Test]
        public void RefusesInvalidAmountsWithoutChangingBalance()
        {
            var wallet = new GoldWallet(10);

            Assert.That(wallet.TrySpend(-1), Is.False);
            Assert.That(wallet.TrySpend(11), Is.False);
            Assert.That(wallet.TryCredit(-1), Is.False);
            Assert.That(wallet.TryCredit(int.MaxValue), Is.False);
            Assert.That(wallet.Balance, Is.EqualTo(10));
        }

        [Test]
        public void RefusesNegativeStartingBalance()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new GoldWallet(-1));
        }
    }

    public sealed class PlayerHealthTests
    {
        [Test]
        public void HealingIsCappedAndReturnsAmountRestored()
        {
            var health = new PlayerHealth(100, 50);

            Assert.That(health.Heal(10), Is.EqualTo(10));
            Assert.That(health.CurrentHp, Is.EqualTo(60));
            Assert.That(health.Heal(int.MaxValue), Is.EqualTo(40));
            Assert.That(health.IsFull, Is.True);
            Assert.That(health.Heal(1), Is.Zero);
        }

        [TestCase(0, 0)]
        [TestCase(100, -1)]
        [TestCase(100, 101)]
        public void RefusesInvalidStartingHealth(int maxHp, int currentHp)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new PlayerHealth(maxHp, currentHp));
        }
    }
}
