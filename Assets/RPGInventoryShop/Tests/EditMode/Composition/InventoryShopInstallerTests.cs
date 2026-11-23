using NUnit.Framework;
using RpgInventoryShop.Application;
using RpgInventoryShop.Domain;
using RpgInventoryShop.UnityAdapters;
using UnityEditor;
using UnityEngine;
using Zenject;

namespace RpgInventoryShop.Composition.Tests
{
    public sealed class InventoryShopInstallerTests
    {
        private ItemDefinitionAsset potion;
        private ItemCatalogAsset catalog;
        private StartingInventoryAsset startingInventory;
        private GameConfigAsset gameConfig;
        private GameObject installerObject;

        [SetUp]
        public void SetUp()
        {
            potion = ScriptableObject.CreateInstance<ItemDefinitionAsset>();
            using (var serialized = new SerializedObject(potion))
            {
                serialized.FindProperty("itemId").stringValue = "test-potion";
                serialized.FindProperty("displayName").stringValue = "Test Potion";
                serialized.FindProperty("basePrice").intValue = 5;
                serialized.FindProperty("stackLimit").intValue = 3;
                serialized.FindProperty("healing").intValue = 20;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }

            catalog = ScriptableObject.CreateInstance<ItemCatalogAsset>();
            using (var serialized = new SerializedObject(catalog))
            {
                SerializedProperty items = serialized.FindProperty("items");
                items.arraySize = 1;
                items.GetArrayElementAtIndex(0).objectReferenceValue = potion;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }

            startingInventory = ScriptableObject.CreateInstance<StartingInventoryAsset>();
            gameConfig = ScriptableObject.CreateInstance<GameConfigAsset>();
            using (var serialized = new SerializedObject(gameConfig))
            {
                serialized.FindProperty("itemCatalog").objectReferenceValue = catalog;
                serialized.FindProperty("startingInventory").objectReferenceValue = startingInventory;
                serialized.FindProperty("maxMana").intValue = 80;
                serialized.FindProperty("startingMana").intValue = 25;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }

            installerObject = new GameObject();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(installerObject);
            Object.DestroyImmediate(gameConfig);
            Object.DestroyImmediate(startingInventory);
            Object.DestroyImmediate(catalog);
            Object.DestroyImmediate(potion);
        }

        [Test]
        public void ResolvesRegisteredServicesAndCatalog()
        {
            DiContainer container = CreateSession();

            Assert.That(container.Resolve<Inventory>(), Is.SameAs(container.Resolve<Inventory>()));
            Assert.That(container.Resolve<GoldWallet>(), Is.SameAs(container.Resolve<GoldWallet>()));
            Assert.That(container.Resolve<PlayerHealth>(), Is.SameAs(container.Resolve<PlayerHealth>()));
            Assert.That(container.Resolve<ShopService>(), Is.SameAs(container.Resolve<ShopService>()));
            Assert.That(container.Resolve<ConsumeItemService>(), Is.SameAs(container.Resolve<ConsumeItemService>()));
            Assert.That(container.Resolve<SaveState>(), Is.SameAs(container.Resolve<SaveState>()));
            Assert.That(container.Resolve<JsonSaveStore>(), Is.SameAs(container.Resolve<JsonSaveStore>()));
            Assert.That(container.Resolve<PlayerMana>().MaxMana, Is.EqualTo(80));
            Assert.That(container.Resolve<PlayerMana>().CurrentMana, Is.EqualTo(25));
            var resolvedCatalog = container.Resolve<IItemCatalog>();
            Assert.That(resolvedCatalog.TryGet("test-potion", out ItemDefinition definition), Is.True);
            Assert.That(definition.Name, Is.EqualTo("Test Potion"));
            Assert.That(definition.BasePrice, Is.EqualTo(5));
            Assert.That(definition.StackLimit, Is.EqualTo(3));
            Assert.That(definition.Healing, Is.EqualTo(20));
        }

        [Test]
        public void ShopConsumptionAndSaveStateShareSessionModels()
        {
            DiContainer container = CreateSession();
            var inventory = container.Resolve<Inventory>();
            var wallet = container.Resolve<GoldWallet>();
            var health = container.Resolve<PlayerHealth>();
            var shop = container.Resolve<ShopService>();
            var consume = container.Resolve<ConsumeItemService>();
            var saveState = container.Resolve<SaveState>();

            Assert.That(shop.Buy("test-potion"), Is.EqualTo(ShopResult.Success));
            Assert.That(shop.Buy("test-potion"), Is.EqualTo(ShopResult.Success));
            Assert.That(wallet.Balance, Is.EqualTo(90));
            Assert.That(inventory.GetSlot(0).Quantity, Is.EqualTo(2));
            Assert.That(consume.Use(0), Is.EqualTo(ConsumeItemResult.Success));
            Assert.That(health.CurrentHp, Is.EqualTo(70));

            SaveData snapshot = saveState.Capture();
            Assert.That(snapshot.Slots, Has.Length.EqualTo(20));
            Assert.That(snapshot.Slots[0].ItemId, Is.EqualTo("test-potion"));
            Assert.That(snapshot.Slots[0].Quantity, Is.EqualTo(1));
            Assert.That(snapshot.Gold, Is.EqualTo(90));
            Assert.That(snapshot.Hp, Is.EqualTo(70));

            Assert.That(shop.Sell(0), Is.EqualTo(ShopResult.Success));
            Assert.That(inventory.GetSlot(0), Is.Null);
            Assert.That(wallet.Balance, Is.EqualTo(92));
        }

        [Test]
        public void SeparateContainersKeepIndependentSessions()
        {
            DiContainer first = CreateSession();
            DiContainer second = CreateSession();

            Assert.That(first.Resolve<ShopService>().Buy("test-potion"), Is.EqualTo(ShopResult.Success));
            Assert.That(first.Resolve<ConsumeItemService>().Use(0), Is.EqualTo(ConsumeItemResult.Success));
            Assert.That(first.Resolve<ShopService>().Buy("test-potion"), Is.EqualTo(ShopResult.Success));

            Assert.That(second.Resolve<Inventory>(), Is.Not.SameAs(first.Resolve<Inventory>()));
            Assert.That(second.Resolve<GoldWallet>(), Is.Not.SameAs(first.Resolve<GoldWallet>()));
            Assert.That(second.Resolve<PlayerHealth>(), Is.Not.SameAs(first.Resolve<PlayerHealth>()));
            Assert.That(second.Resolve<JsonSaveStore>(), Is.Not.SameAs(first.Resolve<JsonSaveStore>()));
            Assert.That(second.Resolve<Inventory>().GetSlot(0), Is.Null);
            Assert.That(second.Resolve<GoldWallet>().Balance, Is.EqualTo(100));
            Assert.That(second.Resolve<PlayerHealth>().CurrentHp, Is.EqualTo(50));
        }

        [Test]
        public void StartingItemsCombineRepeatedEntriesAndRespectStackLimits()
        {
            var other = Object.Instantiate(potion);
            try
            {
                using (var serialized = new SerializedObject(other))
                {
                    serialized.FindProperty("itemId").stringValue = "other-potion";
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                }

                using (var serialized = new SerializedObject(catalog))
                {
                    var items = serialized.FindProperty("items");
                    items.arraySize = 2;
                    items.GetArrayElementAtIndex(1).objectReferenceValue = other;
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                }

                var inventory = CreateSession((potion, 2), (other, 1), (potion, 2)).Resolve<Inventory>();
                Assert.That(inventory.GetSlot(0).Item.Id, Is.EqualTo("test-potion"));
                Assert.That(inventory.GetSlot(0).Quantity, Is.EqualTo(3));
                Assert.That(inventory.GetSlot(1).Item.Id, Is.EqualTo("other-potion"));
                Assert.That(inventory.GetSlot(1).Quantity, Is.EqualTo(1));
                Assert.That(inventory.GetSlot(2).Item.Id, Is.EqualTo("test-potion"));
                Assert.That(inventory.GetSlot(2).Quantity, Is.EqualTo(1));
                Assert.That(inventory.GetSlot(3), Is.Null);
            }
            finally
            {
                Object.DestroyImmediate(other);
            }
        }

        [TestCase(0, "positive quantity")]
        [TestCase(61, "inventory capacity")]
        public void InvalidStartingItemsDoNotBindSessionState(int quantity, string message)
        {
            var container = new DiContainer();
            var installer = ConfigureInstaller((potion, 1), (potion, quantity));
            container.Inject(installer);

            var error = Assert.Throws<System.InvalidOperationException>(installer.InstallBindings);
            Assert.That(error.Message, Does.Contain("entry 2").And.Contain(message));
            Assert.That(container.HasBinding<Inventory>(), Is.False);
            Assert.That(container.HasBinding<GoldWallet>(), Is.False);
        }

        private DiContainer CreateSession(params (ItemDefinitionAsset Item, int Quantity)[] entries)
        {
            var container = new DiContainer();
            var installer = ConfigureInstaller(entries);
            container.Inject(installer);
            installer.InstallBindings();
            return container;
        }

        private InventoryShopInstaller ConfigureInstaller(params (ItemDefinitionAsset Item, int Quantity)[] entries)
        {
            var installer = installerObject.AddComponent<InventoryShopInstaller>();
            using (var serializedInstaller = new SerializedObject(installer))
            {
                serializedInstaller.FindProperty("gameConfig").objectReferenceValue = gameConfig;
                serializedInstaller.ApplyModifiedPropertiesWithoutUndo();
            }

            using var serialized = new SerializedObject(startingInventory);
            var startingItems = serialized.FindProperty("items");
            startingItems.arraySize = entries.Length;
            for (int index = 0; index < entries.Length; index++)
            {
                var entry = startingItems.GetArrayElementAtIndex(index);
                entry.FindPropertyRelative("item").objectReferenceValue = entries[index].Item;
                entry.FindPropertyRelative("quantity").intValue = entries[index].Quantity;
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return installer;
        }
    }
}
