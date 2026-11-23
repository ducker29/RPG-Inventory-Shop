using System;
using System.Collections.Generic;
using System.Linq;
using RpgInventoryShop.Application;
using RpgInventoryShop.Domain;
using RpgInventoryShop.UnityAdapters;
using UnityEngine;

namespace RpgInventoryShop.Composition
{
    [CreateAssetMenu(menuName = "RPG Inventory Shop/Starting Inventory")]
    public sealed class StartingInventoryAsset : ScriptableObject
    {
        [Serializable]
        public sealed class Entry
        {
            [SerializeField] private ItemDefinitionAsset item;
            [SerializeField, Min(1)] private int quantity = 1;

            public ItemDefinitionAsset Item => item;
            public int Quantity => quantity;
        }

        [SerializeField] private List<Entry> items = new();

        public IReadOnlyList<Entry> Items => items;

        public Inventory CreateInventory(ItemCatalogAsset itemCatalog, IItemCatalog catalog)
        {
            var inventory = new Inventory();

            for (int index = 0; index < items.Count; index++)
            {
                var entry = items[index];
                string label = $"Starting Inventory '{name}' entry {index + 1}";
                if (entry?.Item == null)
                    throw new InvalidOperationException($"{label} requires an item asset.");

                string itemId = entry.Item.ItemId;
                if (!itemCatalog.Items.Contains(entry.Item) || !catalog.TryGet(itemId, out var definition))
                    throw new InvalidOperationException($"{label} ('{itemId}') must reference an asset in '{itemCatalog.name}'.");
                if (entry.Quantity <= 0)
                    throw new InvalidOperationException($"{label} ('{itemId}') requires a positive quantity (got {entry.Quantity}).");

                for (int quantity = 0; quantity < entry.Quantity; quantity++)
                {
                    if (!inventory.TryAdd(definition))
                        throw new InvalidOperationException($"{label} ('{itemId}') exceeds inventory capacity.");
                }
            }

            return inventory;
        }
    }
}
