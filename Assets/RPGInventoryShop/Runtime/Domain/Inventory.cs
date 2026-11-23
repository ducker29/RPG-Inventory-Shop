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
        private Dictionary<string, ItemDefinition> definitions = new(StringComparer.Ordinal);

        public ItemStack GetSlot(int slotIndex) => slots[slotIndex];

        public bool TryAdd(ItemDefinition item) => Add(item) == InventoryAddResult.Success;

        public InventoryAddResult Add(ItemDefinition item)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));

            if (definitions.TryGetValue(item.Id, out ItemDefinition registered) &&
                !Matches(registered, item))
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

        public bool CanRestoreSlots(IReadOnlyList<(ItemDefinition Item, int Quantity)> restored) =>
            TryPrepareSlots(restored, out _, out _);

        public bool TryRestoreSlots(IReadOnlyList<(ItemDefinition Item, int Quantity)> restored)
        {
            if (!TryPrepareSlots(restored, out var nextSlots, out var nextDefinitions))
                return false;

            Array.Copy(nextSlots, slots, SlotCount);
            definitions = nextDefinitions;
            return true;
        }

        private bool TryPrepareSlots(
            IReadOnlyList<(ItemDefinition Item, int Quantity)> restored,
            out ItemStack[] nextSlots,
            out Dictionary<string, ItemDefinition> nextDefinitions)
        {
            nextSlots = null;
            nextDefinitions = null;
            if (restored == null || restored.Count != SlotCount)
                return false;

            nextSlots = new ItemStack[SlotCount];
            nextDefinitions = new Dictionary<string, ItemDefinition>(definitions, StringComparer.Ordinal);
            for (int index = 0; index < SlotCount; index++)
            {
                var (item, quantity) = restored[index];
                if (item == null && quantity == 0)
                    continue;
                if (item == null || quantity < 1 || quantity > item.StackLimit)
                    return false;

                if (nextDefinitions.TryGetValue(item.Id, out ItemDefinition registered))
                {
                    if (!Matches(registered, item))
                        return false;
                }
                else
                {
                    registered = item;
                    nextDefinitions.Add(item.Id, item);
                }

                nextSlots[index] = new ItemStack(registered, quantity);
            }

            return true;
        }

        private static bool Matches(ItemDefinition left, ItemDefinition right) =>
            left.Name == right.Name && left.BasePrice == right.BasePrice &&
            left.StackLimit == right.StackLimit && left.Healing == right.Healing &&
            left.ManaRestoration == right.ManaRestoration;
    }
}
