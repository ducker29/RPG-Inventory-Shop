using System;
using RpgInventoryShop.Domain;

namespace RpgInventoryShop.Application
{
    public enum ConsumeItemResult
    {
        Success,
        InvalidSlot,
        EmptySlot,
        NotConsumable,
        HealthFull
    }

    public sealed class ConsumeItemService
    {
        private readonly Inventory inventory;
        private readonly PlayerHealth health;

        public ConsumeItemService(Inventory inventory, PlayerHealth health)
        {
            this.inventory = inventory ?? throw new ArgumentNullException(nameof(inventory));
            this.health = health ?? throw new ArgumentNullException(nameof(health));
        }

        public ConsumeItemResult Use(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= Inventory.SlotCount)
                return ConsumeItemResult.InvalidSlot;

            ItemStack stack = inventory.GetSlot(slotIndex);
            if (stack == null)
                return ConsumeItemResult.EmptySlot;
            if (stack.Item.Healing == 0)
                return ConsumeItemResult.NotConsumable;
            if (health.IsFull)
                return ConsumeItemResult.HealthFull;

            inventory.TryRemoveAt(slotIndex);
            health.Heal(stack.Item.Healing);
            return ConsumeItemResult.Success;
        }
    }
}
