using System;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace RpgInventoryShop.UI.Tests
{
    public sealed class InventorySlotThemeTests
    {
        private const string UiRoot = "Assets/RPGInventoryShop/UI/";

        [Test]
        public void DemoSceneMatchesSlotPresetsAndImportedPalette()
        {
            var asset = AssetDatabase.LoadAssetAtPath<TextAsset>(UiRoot + "Docs/palette.json");
            Assert.That(asset, Is.Not.Null);
            var palette = JsonUtility.FromJson<Palette>(asset.text);
            Assert.That(palette.name, Is.EqualTo("Obsidian & Ember"));
            Assert.That(palette.version, Is.EqualTo("1.0.0"));
            Assert.That(palette.unity_reference_resolution, Is.EqualTo(new[] { 1920, 1080 }));
            AssertRgb(InventorySlotTheme.Normal.BorderColor, palette.colors.outline);
            AssertRgb(InventorySlotTheme.Normal.EffectColor, palette.colors.ivory);
            AssertRgb(InventorySlotTheme.Hover.BorderColor, palette.colors.mist);
            AssertRgb(InventorySlotTheme.Hover.EffectColor, palette.colors.mist);
            AssertRgb(InventorySlotTheme.Selected.BorderColor, palette.colors.ember);
            AssertRgb(InventorySlotTheme.Selected.EffectColor, palette.colors.ember);

            var scene = EditorSceneManager.OpenPreviewScene(UiRoot + "Scenes/ProceduralUiTest.unity");
            try
            {
                var roots = scene.GetRootGameObjects();
                var scaler = roots.SelectMany(root => root.GetComponentsInChildren<CanvasScaler>()).Single();
                Assert.That(scaler.uiScaleMode, Is.EqualTo(CanvasScaler.ScaleMode.ScaleWithScreenSize));
                Assert.That(scaler.screenMatchMode, Is.EqualTo(CanvasScaler.ScreenMatchMode.MatchWidthOrHeight));
                Assert.That(scaler.matchWidthOrHeight, Is.EqualTo(0.5f));
                Assert.That(scaler.referenceResolution, Is.EqualTo(new Vector2(
                    palette.unity_reference_resolution[0], palette.unity_reference_resolution[1])));
                var camera = roots.SelectMany(root => root.GetComponentsInChildren<Camera>()).Single();
                AssertRgb(camera.backgroundColor, palette.colors.@void);

                var slots = roots.SelectMany(root => root.GetComponentsInChildren<RoundedGradientGraphic>())
                    .ToDictionary(slot => slot.name);
                Assert.That(slots.Count, Is.EqualTo(3));
                Assert.That(slots["Normal"].Style, Is.EqualTo(InventorySlotTheme.Normal));
                Assert.That(slots["Hover"].Style, Is.EqualTo(InventorySlotTheme.Hover));
                Assert.That(slots["Selected"].Style, Is.EqualTo(InventorySlotTheme.Selected));
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(scene);
            }
        }

        private static void AssertRgb(Color value, string hex) =>
            Assert.That("#" + ColorUtility.ToHtmlStringRGB(value), Is.EqualTo(hex));

        [Serializable]
        private sealed class Palette
        {
            public string name = "";
            public string version = "";
            public Tokens colors = new();
            public int[] unity_reference_resolution = Array.Empty<int>();
        }

        [Serializable]
        private sealed class Tokens
        {
            public string @void = "";
            public string outline = "";
            public string ivory = "";
            public string mist = "";
            public string ember = "";
        }
    }
}
