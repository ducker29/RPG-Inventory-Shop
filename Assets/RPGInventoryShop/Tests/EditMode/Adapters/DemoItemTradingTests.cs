using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using RpgInventoryShop.Application;
using RpgInventoryShop.Domain;
using RpgInventoryShop.UnityAdapters;
using UnityEditor;

namespace RpgInventoryShop.Adapters.Tests
{
    public sealed class DemoItemTradingTests
    {
        [TestCase("iron-sword", "Iron Sword", 60)]
        [TestCase("shield", "Shield", 40)]
        public void DemoItemCanBeTradedAndSavedAsNonConsumable(string id, string name, int price)
        {
            var asset = AssetDatabase.LoadAssetAtPath<ItemCatalogAsset>(
                "Assets/RPGInventoryShop/Content/Items/DemoItemCatalog.asset");
            var entry = asset.Items.Single(item => item.ItemId == id);
            Assert.That(entry.Icon, Is.Not.Null);
            Assert.That(entry.Description, Is.Not.Empty);
            var catalog = asset.CreateCatalog();
            Assert.That(catalog.TryGet(id, out var definition), Is.True);
            Assert.That(definition.Name, Is.EqualTo(name));
            Assert.That(definition.BasePrice, Is.EqualTo(price));
            Assert.That(definition.StackLimit, Is.EqualTo(1));
            Assert.That(definition.Healing, Is.Zero);

            var inventory = new Inventory();
            var wallet = new GoldWallet(2 * price);
            var health = new PlayerHealth(100, 50);
            var shop = new ShopService(inventory, wallet, catalog);
            Assert.That(shop.Buy(id), Is.EqualTo(ShopResult.Success));
            Assert.That(shop.Buy(id), Is.EqualTo(ShopResult.Success));
            Assert.That(inventory.GetSlot(0).Quantity, Is.EqualTo(1));
            Assert.That(inventory.GetSlot(1).Quantity, Is.EqualTo(1));
            Assert.That(new ConsumeItemService(inventory, health).Use(0), Is.EqualTo(ConsumeItemResult.NotConsumable));
            Assert.That(health.CurrentHp, Is.EqualTo(50));
            Assert.That(shop.Sell(0), Is.EqualTo(ShopResult.Success));
            Assert.That(wallet.Balance, Is.EqualTo(price / 2));

            string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            try
            {
                string file = Path.Combine(path, "inventory.json");
                var save = new JsonSaveStore(file, new SaveState(inventory, wallet, health, catalog));
                Assert.That(save.Save(), Is.EqualTo(SaveStatus.Success));
                var restored = new Inventory();
                var restoredWallet = new GoldWallet(0);
                var restoredHealth = new PlayerHealth(100, 100);
                var load = new JsonSaveStore(file, new SaveState(restored, restoredWallet, restoredHealth, asset.CreateCatalog()));
                Assert.That(load.Load(), Is.EqualTo(SaveStatus.Success));
                Assert.That(restored.GetSlot(0), Is.Null);
                Assert.That(restored.GetSlot(1).Item.Id, Is.EqualTo(id));
                Assert.That(restored.GetSlot(1).Quantity, Is.EqualTo(1));
                Assert.That(restoredWallet.Balance, Is.EqualTo(price / 2));
                Assert.That(restoredHealth.CurrentHp, Is.EqualTo(50));
            }
            finally
            {
                if (Directory.Exists(path))
                    Directory.Delete(path, recursive: true);
            }
        }
    }
}
