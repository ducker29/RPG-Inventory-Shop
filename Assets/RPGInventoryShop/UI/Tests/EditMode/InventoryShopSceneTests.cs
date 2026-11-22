using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace RpgInventoryShop.UI.Tests
{
    public sealed class InventoryShopSceneTests
    {
        private const string Root = "Assets/RPGInventoryShop/";

        [Test]
        public void DemoSceneHasAuthoredLayoutAndPrefabInstances()
        {
            var slotPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "UI/Prefabs/InventorySlot.prefab");
            var rowPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "UI/Prefabs/MerchantRow.prefab");
            Assert.That(slotPrefab, Is.Not.Null);
            Assert.That(rowPrefab, Is.Not.Null);
            Assert.That(rowPrefab.GetComponent<MerchantRowView>().Price, Is.Not.Null);
            var scene = EditorSceneManager.OpenPreviewScene(Root + "Scenes/InventoryShopDemo.unity");
            try
            {
                var canvas = scene.GetRootGameObjects().Single(root => root.GetComponent<Canvas>());
                var frame = canvas.transform.Find("Screen Layout");
                Assert.That(canvas.transform.Find("Castle Background").GetComponent<RawImage>().texture, Is.Not.Null);
                Assert.That(frame.Find("Header"), Is.Not.Null);
                Assert.That(frame.Find("Item Details").GetComponentsInChildren<ProceduralButton>(), Has.Length.EqualTo(3));

                var slots = frame.Find("Inventory/Slots · 5 × 4").GetComponentsInChildren<InventorySlotView>();
                Assert.That(slots, Has.Length.EqualTo(20));
                var first = (RectTransform)slots[0].transform;
                float edge = first.rect.width;
                for (int index = 0; index < slots.Length; index++)
                {
                    var slot = slots[index];
                    var rect = (RectTransform)slot.transform;
                    Assert.That(PrefabUtility.GetCorrespondingObjectFromSource(slot.gameObject), Is.SameAs(slotPrefab));
                    Assert.That(slot.Button.targetGraphic, Is.SameAs(slot.Surface));
                    Assert.That(slot.Icon, Is.Not.Null);
                    Assert.That(slot.Count, Is.Not.Null);
                    Assert.That(rect.rect.size, Is.EqualTo(new Vector2(edge, edge)));
                    Vector2 expected = first.anchoredPosition + new Vector2(index % 5, -(index / 5)) * (edge + 12);
                    Assert.That(rect.anchoredPosition, Is.EqualTo(expected));
                }

                var offers = frame.Find("Merchant/Merchant Scroll").GetComponent<ScrollRect>().content;
                Assert.That(offers.childCount, Is.EqualTo(1));
                var preview = offers.GetChild(0);
                Assert.That(preview.CompareTag("EditorOnly"), Is.True);
                var rows = preview.GetComponentsInChildren<MerchantRowView>();
                Assert.That(rows.Select(row => row.ItemName.text), Is.EqualTo(new[] { "Health Potion", "Iron Sword", "Shield" }));
                foreach (var row in rows)
                    Assert.That(PrefabUtility.GetCorrespondingObjectFromSource(row.gameObject), Is.SameAs(rowPrefab));
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(scene);
            }
        }
    }
}
