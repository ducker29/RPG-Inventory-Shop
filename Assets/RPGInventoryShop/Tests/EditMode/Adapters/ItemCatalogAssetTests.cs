using System;
using NUnit.Framework;
using RpgInventoryShop.Application;
using RpgInventoryShop.UnityAdapters;
using UnityEditor;
using UnityEngine;

namespace RpgInventoryShop.Adapters.Tests
{
    public sealed class ItemCatalogAssetTests
    {
        private ItemDefinitionAsset potion;
        private ItemDefinitionAsset other;
        private ItemCatalogAsset catalog;

        [SetUp]
        public void SetUp()
        {
            potion = ScriptableObject.CreateInstance<ItemDefinitionAsset>();
            other = ScriptableObject.CreateInstance<ItemDefinitionAsset>();
            catalog = ScriptableObject.CreateInstance<ItemCatalogAsset>();
            SetDefinition(potion, "potion");
            SetDefinition(other, "other");
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(catalog);
            UnityEngine.Object.DestroyImmediate(other);
            UnityEngine.Object.DestroyImmediate(potion);
        }

        [Test]
        public void MapsSerializedFieldsToPureDefinitionsUsingStableId()
        {
            SetCatalog(potion, other);
            potion.name = "Renamed Unity asset";
            IItemCatalog runtime = catalog.CreateCatalog();

            Assert.That(runtime.TryGet("potion", out var item), Is.True);
            Assert.That(item.Id, Is.EqualTo("potion"));
            Assert.That(item.Name, Is.EqualTo("Health Potion"));
            Assert.That(item.BasePrice, Is.EqualTo(5));
            Assert.That(item.StackLimit, Is.EqualTo(3));
            Assert.That(item.Healing, Is.EqualTo(10));
            Assert.That(runtime.TryGet("Renamed Unity asset", out _), Is.False);
            Assert.That(runtime.TryGet(null, out _), Is.False);
        }

        [Test]
        public void RejectsDuplicateIdsAcrossDistinctAssets()
        {
            SetDefinition(other, potion.ItemId);
            SetCatalog(potion, other);

            var error = Assert.Throws<InvalidOperationException>(() => catalog.CreateCatalog());
            Assert.That(error.Message, Does.Contain("Duplicate item ID 'potion'"));
        }

        [Test]
        public void RejectsInvalidDefinition()
        {
            SetDefinition(other, "other", 0);
            SetCatalog(potion, other);

            var error = Assert.Throws<InvalidOperationException>(() => catalog.CreateCatalog());
            Assert.That(error.InnerException, Is.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(error.Message, Does.Contain("stackLimit"));
        }

        [Test]
        public void RejectsMissingAsset()
        {
            SetCatalog(potion, null);
            Assert.Throws<InvalidOperationException>(() => catalog.CreateCatalog());
        }

        private static void SetDefinition(ItemDefinitionAsset asset, string id, int limit = 3)
        {
            using var serialized = new SerializedObject(asset);
            serialized.FindProperty("itemId").stringValue = id;
            serialized.FindProperty("displayName").stringValue = "Health Potion";
            serialized.FindProperty("basePrice").intValue = 5;
            serialized.FindProperty("stackLimit").intValue = limit;
            serialized.FindProperty("healing").intValue = 10;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private void SetCatalog(params ItemDefinitionAsset[] assets)
        {
            using var serialized = new SerializedObject(catalog);
            SerializedProperty items = serialized.FindProperty("items");
            items.arraySize = assets.Length;
            for (int index = 0; index < assets.Length; index++)
                items.GetArrayElementAtIndex(index).objectReferenceValue = assets[index];
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
