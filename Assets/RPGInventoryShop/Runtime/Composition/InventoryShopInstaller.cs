using System;
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
        [SerializeField] private InventoryShopView screen;
        [SerializeField] private string startingItemId;
        [SerializeField, Min(0)] private int startingItemQuantity;

        public override void InstallBindings()
        {
            if (itemCatalog == null)
                throw new InvalidOperationException("InventoryShopInstaller requires an item catalog.");

            IItemCatalog catalog = itemCatalog.CreateCatalog();
            var wallet = new GoldWallet(startingGold);
            var health = new PlayerHealth(maxHp, startingHp);
            var inventory = new Inventory();

            if (startingItemQuantity > 0)
            {
                if (!catalog.TryGet(startingItemId, out var startingItem))
                    throw new InvalidOperationException($"Unknown starting item '{startingItemId}'.");
                for (int index = 0; index < startingItemQuantity; index++)
                {
                    if (!inventory.TryAdd(startingItem))
                        throw new InvalidOperationException("The starting items exceed inventory capacity.");
                }
            }

            Container.BindInstance(inventory);
            Container.BindInstance(wallet);
            Container.BindInstance(health);
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
