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
        [SerializeField] private GameConfigAsset gameConfig;
        [SerializeField] private InventoryShopView screen;

        public override void InstallBindings()
        {
            if (gameConfig == null)
                throw new InvalidOperationException("InventoryShopInstaller requires a Game Config.");

            var itemCatalog = gameConfig.ItemCatalog;
            if (itemCatalog == null)
                throw new InvalidOperationException($"Game Config '{gameConfig.name}' requires an Item Catalog.");
            if (gameConfig.StartingInventory == null)
                throw new InvalidOperationException($"Game Config '{gameConfig.name}' requires a Starting Inventory.");

            IItemCatalog catalog = itemCatalog.CreateCatalog();
            var wallet = new GoldWallet(gameConfig.StartingGold);
            var health = new PlayerHealth(gameConfig.MaxHp, gameConfig.StartingHp);
            var mana = new PlayerMana(gameConfig.MaxMana, gameConfig.StartingMana);
            var inventory = gameConfig.StartingInventory.CreateInventory(itemCatalog, catalog);

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
