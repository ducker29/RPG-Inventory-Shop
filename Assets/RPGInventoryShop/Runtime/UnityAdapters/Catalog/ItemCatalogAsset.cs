using System;
using System.Collections.Generic;
using RpgInventoryShop.Application;
using RpgInventoryShop.Domain;
using UnityEngine;

namespace RpgInventoryShop.UnityAdapters
{
    [CreateAssetMenu(menuName = "RPG Inventory Shop/Item Catalog")]
    public sealed class ItemCatalogAsset : ScriptableObject
    {
        [SerializeField] private ItemDefinitionAsset[] items = Array.Empty<ItemDefinitionAsset>();

        public IReadOnlyList<ItemDefinitionAsset> Items => items;

        public IItemCatalog CreateCatalog()
        {
            var definitions = new Dictionary<string, ItemDefinition>(StringComparer.Ordinal);
            for (int index = 0; index < items.Length; index++)
            {
                ItemDefinitionAsset asset = items[index];
                if (asset == null)
                    throw new InvalidOperationException($"Item catalog entry {index} has no asset.");

                ItemDefinition definition;
                try
                {
                    definition = asset.ToDefinition();
                }
                catch (ArgumentException exception)
                {
                    throw new InvalidOperationException(
                        $"Item catalog entry {index} ('{asset.name}') is invalid: {exception.ParamName}.", exception);
                }

                if (!definitions.TryAdd(definition.Id, definition))
                    throw new InvalidOperationException($"Duplicate item ID '{definition.Id}' at entry {index}.");
            }

            return new Catalog(definitions);
        }

        private sealed class Catalog : IItemCatalog
        {
            private readonly Dictionary<string, ItemDefinition> definitions;

            public Catalog(Dictionary<string, ItemDefinition> definitions) => this.definitions = definitions;

            public bool TryGet(string itemId, out ItemDefinition item) =>
                definitions.TryGetValue(itemId ?? string.Empty, out item);
        }
    }
}
