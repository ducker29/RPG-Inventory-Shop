using System;
using RpgInventoryShop.Application;
using RpgInventoryShop.Domain;
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

        public override void InstallBindings()
        {
            if (itemCatalog == null)
                throw new InvalidOperationException("InventoryShopInstaller requires an item catalog.");

            IItemCatalog catalog = itemCatalog.CreateCatalog();
            var wallet = new GoldWallet(startingGold);
            var health = new PlayerHealth(maxHp, startingHp);

            Container.Bind<Inventory>().AsSingle();
            Container.BindInstance(wallet);
            Container.BindInstance(health);
            Container.BindInstance(catalog);
            Container.Bind<ShopService>().AsSingle();
            Container.Bind<ConsumeItemService>().AsSingle();
            Container.Bind<SaveState>().AsSingle();
            Container.Bind<JsonSaveStore>()
                .FromMethod(context => new JsonSaveStore(context.Container.Resolve<SaveState>()))
                .AsSingle();
        }
    }
}
