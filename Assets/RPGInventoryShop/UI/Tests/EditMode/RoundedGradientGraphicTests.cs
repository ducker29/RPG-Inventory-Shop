using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace RpgInventoryShop.UI.Tests
{
    public sealed class RoundedGradientGraphicTests
    {
        private GameObject canvasObject;
        private Material material;

        [SetUp]
        public void SetUp()
        {
            canvasObject = new GameObject("Test Canvas", typeof(Canvas));
            material = AssetDatabase.LoadAssetAtPath<Material>(
                "Assets/RPGInventoryShop/UI/Shaders/RoundedGradient.mat");
            Assert.That(material, Is.Not.Null);
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(canvasObject);

        [Test]
        public void StyleChangesRebuildOnlyThisGraphicAndKeepSharedMaterial()
        {
            var normal = CreateGraphic(InventorySlotTheme.Normal);
            var selected = CreateGraphic(InventorySlotTheme.Selected);
            Vector4 selectedFill = ReadUv(selected, 2);
            Vector4 selectedShape = ReadUv(selected, 1);
            var edited = normal.Style;
            edited.Radius = 23f;
            edited.BorderWidth = 7f;
            edited.EdgeSmoothing = 0.6f;
            edited.FillTop.a = 0.31f;
            edited.FillBottom.a = 0.73f;
            edited.BorderColor.a = 0.42f;
            normal.Style = edited;

            Assert.That(ReadUv(normal, 1), Is.EqualTo(new Vector4(23f, 7f, 0.6f, -8f)));
            Assert.That(ReadUv(normal, 2).y, Is.EqualTo(0.31f));
            Assert.That(ReadUv(normal, 2).w, Is.EqualTo(0.73f));
            Assert.That(ReadUv(normal, 3).y, Is.EqualTo(0.42f));
            Assert.That(ReadUv(selected, 2), Is.EqualTo(selectedFill));
            Assert.That(ReadUv(selected, 1), Is.EqualTo(selectedShape));
            Assert.That(normal.materialForRendering, Is.SameAs(material));
            Assert.That(selected.materialForRendering, Is.SameAs(material));
        }

        [Test]
        public void MissingMaterialSuppressesGeometryUntilCompatibleMaterialIsAssigned()
        {
            var graphic = CreateGraphic(InventorySlotTheme.Selected);
            ReadUv(graphic, 0);

            graphic.material = null;
            Canvas.ForceUpdateCanvases();
            Assert.That(graphic.canvasRenderer.GetMesh().vertexCount, Is.Zero);

            graphic.material = material;
            Assert.That(ReadUv(graphic, 1).w, Is.EqualTo(14f));
        }

        [Test]
        public void ResizeRebuildsGeometryAndClampsRadiusAndBorderToShortSide()
        {
            var graphic = CreateGraphic(InventorySlotTheme.Selected);
            graphic.rectTransform.pivot = Vector2.zero;
            var style = graphic.Style;
            style.Radius = 40f;
            style.BorderWidth = 16f;
            graphic.Style = style;

            graphic.rectTransform.sizeDelta = new Vector2(140f, 24f);
            Assert.That(ReadUv(graphic, 0), Is.EqualTo(new Vector4(-84f, -26f, 70f, 12f)));
            Assert.That(ReadUv(graphic, 1), Is.EqualTo(new Vector4(12f, 12f, 1f, 14f)));
            graphic.rectTransform.sizeDelta = new Vector2(90f, 64f);
            Assert.That(ReadUv(graphic, 0), Is.EqualTo(new Vector4(-59f, -46f, 45f, 32f)));
            Assert.That(ReadUv(graphic, 1), Is.EqualTo(new Vector4(32f, 16f, 1f, 14f)));
        }

        [Test]
        public void MaskedGraphicUsesStencilMaterialWithSameShader()
        {
            var parent = CreateGraphic(InventorySlotTheme.Normal);
            parent.gameObject.AddComponent<Mask>().showMaskGraphic = false;
            var child = CreateGraphic(InventorySlotTheme.Hover);
            child.transform.SetParent(parent.transform, false);
            Canvas.ForceUpdateCanvases();

            Assert.That(child.materialForRendering, Is.Not.SameAs(material));
            Assert.That(child.materialForRendering.shader, Is.SameAs(material.shader));
            Assert.That(child.materialForRendering.GetInt("_Stencil"), Is.EqualTo(1));
            Assert.That(material.GetInt("_Stencil"), Is.Zero);
        }

        private RoundedGradientGraphic CreateGraphic(RoundedGradientStyle style)
        {
            var target = new GameObject("Rounded", typeof(RectTransform));
            var rect = (RectTransform)target.transform;
            rect.SetParent(canvasObject.transform, false);
            rect.sizeDelta = new Vector2(108f, 108f);
            var graphic = target.AddComponent<RoundedGradientGraphic>();
            graphic.material = material;
            graphic.Style = style;
            return graphic;
        }

        private static Vector4 ReadUv(RoundedGradientGraphic graphic, int channel)
        {
            Canvas.ForceUpdateCanvases();
            var mesh = graphic.canvasRenderer.GetMesh();
            var data = new List<Vector4>();
            mesh.GetUVs(channel, data);
            Assert.That(mesh.vertexCount, Is.EqualTo(4));
            Assert.That(data.Count, Is.EqualTo(4));
            return data[0];
        }
    }
}
