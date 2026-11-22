using NUnit.Framework;
using RpgInventoryShop.Application;
using RpgInventoryShop.Domain;

namespace RpgInventoryShop.Core.Tests
{
    public sealed class ConsumeItemServiceTests
    {
        private static readonly ItemDefinition Potion = new ItemDefinition("potion", "Potion", 5, 1, 10);
        private static readonly ItemDefinition Sword = new ItemDefinition("sword", "Sword", 15, 1);

        [Test]
        public void UsesOnePotionFromSelectedSlotAndCapsHealing()
        {
            var inventory = new Inventory();
            var health = new PlayerHealth(100, 95);
            inventory.TryAdd(Potion);
            inventory.TryAdd(Potion);
            var service = new ConsumeItemService(inventory, health);

            Assert.That(service.Use(1), Is.EqualTo(ConsumeItemResult.Success));
            Assert.That(health.CurrentHp, Is.EqualTo(100));
            Assert.That(inventory.GetSlot(0).Item, Is.SameAs(Potion));
            Assert.That(inventory.GetSlot(1), Is.Null);
        }

        [Test]
        public void UsesExactlyOnePotionFromSelectedStack()
        {
            var inventory = new Inventory();
            var potion = new ItemDefinition("potion", "Potion", 5, 3, 10);
            for (int i = 0; i < 5; i++)
                inventory.TryAdd(potion);
            inventory.TryAdd(Sword);
            var original = new ItemStack[Inventory.SlotCount];
            for (int i = 0; i < original.Length; i++)
                original[i] = inventory.GetSlot(i);
            var health = new PlayerHealth(100, 50);
            var service = new ConsumeItemService(inventory, health);

            Assert.That(original[1].Quantity, Is.EqualTo(2));
            Assert.That(service.Use(1), Is.EqualTo(ConsumeItemResult.Success));
            Assert.That(health.CurrentHp, Is.EqualTo(60));
            Assert.That(inventory.GetSlot(1).Item, Is.SameAs(potion));
            Assert.That(inventory.GetSlot(1).Quantity, Is.EqualTo(original[1].Quantity - 1));
            for (int i = 0; i < original.Length; i++)
            {
                if (i != 1)
                    Assert.That(inventory.GetSlot(i), Is.SameAs(original[i]), $"Slot {i}");
            }
        }

        [TestCase(0, 100, ConsumeItemResult.HealthFull)]
        [TestCase(1, 50, ConsumeItemResult.NotConsumable)]
        [TestCase(2, 50, ConsumeItemResult.EmptySlot)]
        [TestCase(-1, 50, ConsumeItemResult.InvalidSlot)]
        [TestCase(20, 50, ConsumeItemResult.InvalidSlot)]
        public void RefusedUsePreservesItemsAndHealth(int slot, int hp, ConsumeItemResult expected)
        {
            var inventory = new Inventory();
            var health = new PlayerHealth(100, hp);
            inventory.TryAdd(Potion);
            inventory.TryAdd(Sword);
            ItemStack potion = inventory.GetSlot(0);
            ItemStack sword = inventory.GetSlot(1);
            var service = new ConsumeItemService(inventory, health);

            Assert.That(service.Use(slot), Is.EqualTo(expected));
            Assert.That(health.CurrentHp, Is.EqualTo(hp));
            Assert.That(inventory.GetSlot(0), Is.SameAs(potion));
            Assert.That(inventory.GetSlot(1), Is.SameAs(sword));
        }
    }
}
