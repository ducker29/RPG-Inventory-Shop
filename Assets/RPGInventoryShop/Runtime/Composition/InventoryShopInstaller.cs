using System;
using System.Collections.Generic;
using System.Linq;
using RpgInventoryShop.Application;
using RpgInventoryShop.Domain;
using RpgInventoryShop.Presentation;
using RpgInventoryShop.UnityAdapters;
using UnityEngine;
using Zenject;

namespace RpgInventoryShop.Composition
{
    public sealed class InventoryShopInstaller : MonoInstaller
    {
        [SerializeField] private ItemCatalogAsset itemCatalog;
        [SerializeField, Min(0)] private int startingGold = 100;
        [SerializeField, Min(1)] private int maxHp = 100;
        [SerializeField, Min(0)] private int startingHp = 50;
        [SerializeField, Min(1)] private int maxMana = 100;
        [SerializeField, Min(0)] private int startingMana = 40;
        [SerializeField] private InventoryShopView screen;
        [SerializeField] private List<StartingItem> startingItems = new();

        [Serializable]
        private sealed class StartingItem
        {
            public ItemDefinitionAsset Item;
            [Min(1)] public int Quantity = 1;
        }

        public override void InstallBindings()
        {
            if (itemCatalog == null)
                throw new InvalidOperationException("InventoryShopInstaller requires an item catalog.");

            IItemCatalog catalog = itemCatalog.CreateCatalog();
            var wallet = new GoldWallet(startingGold);
            var health = new PlayerHealth(maxHp, startingHp);
            var mana = new PlayerMana(maxMana, startingMana);
            var inventory = new Inventory();

            for (int index = 0; index < startingItems.Count; index++)
            {
                StartingItem entry = startingItems[index];
                string label = $"Starting Items entry {index + 1}";
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

            Container.BindInstance(inventory);
            Container.BindInstance(wallet);
            Container.BindInstance(health);
            Container.BindInstance(mana);
            Container.BindInstance(catalog);
            Container.Bind<ShopService>().AsSingle();
            Container.Bind<ConsumeItemService>().AsSingle();
            Container.Bind<SaveState>().AsSingle();
            Container.Bind<JsonSaveStore>()
                .FromMethod(context => new JsonSaveStore(context.Container.Resolve<SaveState>()))
                .AsSingle();

            if (screen)
            {
                screen.ConfigureItems(itemCatalog.Items);
                Container.BindInstance<IInventoryShopView>(screen);
                Container.Bind<InventoryShopPresenter>().AsSingle()
                    .WithArguments((object)itemCatalog.Items.Select(item => item.ItemId).ToArray())
                    .OnInstantiated<InventoryShopPresenter>((_, presenter) => screen.Connect(presenter))
                    .NonLazy();
            }
        }
    }
}
