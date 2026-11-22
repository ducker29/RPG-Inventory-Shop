using System;
using System.Collections.Generic;

namespace RpgInventoryShop.Domain
{
    public enum InventoryAddResult
    {
        Success,
        Full,
        ConflictingItemDefinition
    }

    public sealed class Inventory
    {
        public const int SlotCount = 20;

        private readonly ItemStack[] slots = new ItemStack[SlotCount];
        private readonly Dictionary<string, ItemDefinition> definitions = new(StringComparer.Ordinal);

        public ItemStack GetSlot(int slotIndex) => slots[slotIndex];

        public bool TryAdd(ItemDefinition item) => Add(item) == InventoryAddResult.Success;

        public InventoryAddResult Add(ItemDefinition item)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));

            if (definitions.TryGetValue(item.Id, out ItemDefinition registered) &&
                (registered.Name != item.Name || registered.BasePrice != item.BasePrice ||
                 registered.StackLimit != item.StackLimit || registered.Healing != item.Healing))
                return InventoryAddResult.ConflictingItemDefinition;

            int emptyIndex = -1;
            for (int index = 0; index < SlotCount; index++)
            {
                ItemStack stack = slots[index];
                if (stack == null)
                {
                    if (emptyIndex < 0)
                        emptyIndex = index;
                    continue;
                }

                if (stack.Item.Id == item.Id && stack.Quantity < stack.Item.StackLimit)
                {
                    slots[index] = new ItemStack(stack.Item, stack.Quantity + 1);
                    return InventoryAddResult.Success;
                }
            }

            if (emptyIndex < 0)
                return InventoryAddResult.Full;

            var newStack = new ItemStack(registered ?? item, 1);
            if (registered == null)
                definitions.Add(item.Id, item);
            slots[emptyIndex] = newStack;
            return InventoryAddResult.Success;
        }

        public bool TryRemoveAt(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= SlotCount)
                return false;

            ItemStack stack = slots[slotIndex];
            if (stack == null)
                return false;

            slots[slotIndex] = stack.Quantity == 1
                ? null
                : new ItemStack(stack.Item, stack.Quantity - 1);
            return true;
        }
    }
}
