using System;

namespace RpgInventoryShop.Domain
{
    public sealed class ItemDefinition
    {
        public ItemDefinition(string id, string name, int basePrice, int stackLimit, int healing = 0)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("An item ID is required.", nameof(id));
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("An item name is required.", nameof(name));
            if (basePrice < 0)
                throw new ArgumentOutOfRangeException(nameof(basePrice));
            if (stackLimit < 1)
                throw new ArgumentOutOfRangeException(nameof(stackLimit));
            if (healing < 0)
                throw new ArgumentOutOfRangeException(nameof(healing));

            Id = id;
            Name = name;
            BasePrice = basePrice;
            StackLimit = stackLimit;
            Healing = healing;
        }

        public string Id { get; }
        public string Name { get; }
        public int BasePrice { get; }
        public int StackLimit { get; }
        public int Healing { get; }
    }
}
