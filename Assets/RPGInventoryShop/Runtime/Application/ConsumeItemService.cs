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
        HealthFull,
        ManaFull
    }

    public sealed class ConsumeItemService
    {
        private readonly Inventory inventory;
        private readonly PlayerHealth health;
        private readonly PlayerMana mana;

        public ConsumeItemService(Inventory inventory, PlayerHealth health, PlayerMana mana)
        {
            this.inventory = inventory ?? throw new ArgumentNullException(nameof(inventory));
            this.health = health ?? throw new ArgumentNullException(nameof(health));
            this.mana = mana ?? throw new ArgumentNullException(nameof(mana));
        }

        public bool CanUse(int slotIndex) => CheckUse(slotIndex) == ConsumeItemResult.Success;

        public ConsumeItemResult Use(int slotIndex)
        {
            ConsumeItemResult result = CheckUse(slotIndex);
            if (result != ConsumeItemResult.Success)
                return result;

            ItemDefinition item = inventory.GetSlot(slotIndex).Item;
            inventory.TryRemoveAt(slotIndex);
            health.Heal(item.Healing);
            mana.Recover(item.ManaRestoration);
            return ConsumeItemResult.Success;
        }

        private ConsumeItemResult CheckUse(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= Inventory.SlotCount)
                return ConsumeItemResult.InvalidSlot;

            ItemStack stack = inventory.GetSlot(slotIndex);
            if (stack == null)
                return ConsumeItemResult.EmptySlot;
            ItemDefinition item = stack.Item;
            if ((item.Healing > 0 && !health.IsFull) || (item.ManaRestoration > 0 && !mana.IsFull))
                return ConsumeItemResult.Success;
            if (item.Healing > 0)
                return ConsumeItemResult.HealthFull;
            return item.ManaRestoration > 0 ? ConsumeItemResult.ManaFull : ConsumeItemResult.NotConsumable;
        }
    }
}
