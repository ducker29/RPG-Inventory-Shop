using System;
using UnityEngine;
using UnityEngine.UI;

namespace RpgInventoryShop.UI
{
    public enum RoundedGradientEffect
    {
        None,
        OuterGlow,
        TopHighlight,
        OuterGlowWithInnerRim,
        BevelHighlight
    }

    [Serializable]
    public struct RoundedGradientStyle
    {
        [ColorUsage(true)] public Color FillTop;
        [ColorUsage(true)] public Color FillBottom;
        [Min(0)] public float Radius;
        [Min(0)] public float BorderWidth;
        [ColorUsage(true)] public Color BorderColor;
        [Min(0)] public float EdgeSmoothing;
        public RoundedGradientEffect Effect;
        [ColorUsage(true)] public Color EffectColor;
        [Min(0)] public float EffectWidth;
    }

    [AddComponentMenu("UI/Rounded Gradient Graphic")]
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class RoundedGradientGraphic : MaskableGraphic
    {
        private const string ShaderName = "RpgInventoryShop/UI/Rounded Gradient";

        private const AdditionalCanvasShaderChannels StyleChannels =
            AdditionalCanvasShaderChannels.TexCoord1 |
            AdditionalCanvasShaderChannels.TexCoord2 |
            AdditionalCanvasShaderChannels.TexCoord3;

        [SerializeField] private RoundedGradientStyle style = InventorySlotTheme.Normal;

        public override Material material
        {
            get => base.material;
            set
            {
                base.material = value;
                SetVerticesDirty();
            }
        }

        public RoundedGradientStyle Style
        {
            get => style;
            set
            {
                style = value;
                SetVerticesDirty();
            }
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            EnableStyleChannels();
        }

        protected override void OnCanvasHierarchyChanged()
        {
            base.OnCanvasHierarchyChanged();
            if (isActiveAndEnabled)
                EnableStyleChannels();
        }

        private void EnableStyleChannels()
        {
            Canvas parentCanvas = canvas;
            if (parentCanvas != null && (parentCanvas.additionalShaderChannels & StyleChannels) != StyleChannels)
                parentCanvas.additionalShaderChannels |= StyleChannels;
        }

        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            // Graphic falls back to UI/Default when its material asset is missing.
            // That shader cannot interpret the SDF vertex data.
            Shader shader = material.shader;
            if (!shader || shader.name != ShaderName)
                return;

            Rect rect = rectTransform.rect;
            Vector2 halfSize = rect.size * 0.5f;
            if (halfSize.x <= 0f || halfSize.y <= 0f)
                return;

            float maxRadius = Mathf.Min(halfSize.x, halfSize.y);
            float effectWidth = style.EffectColor.a > 0f ? Mathf.Max(0f, style.EffectWidth) : 0f;
            bool outerGlow = style.Effect is RoundedGradientEffect.OuterGlow or RoundedGradientEffect.OuterGlowWithInnerRim;
            float glowPadding = outerGlow ? effectWidth : 0f;
            float signedEffectWidth = style.Effect is RoundedGradientEffect.TopHighlight or RoundedGradientEffect.BevelHighlight
                ? -effectWidth
                : glowPadding;
            Vector2 meshHalfSize = halfSize + Vector2.one * glowPadding;

            var vertex = UIVertex.simpleVert;
            vertex.color = color;
            vertex.uv1 = new Vector4(Mathf.Clamp(style.Radius, 0f, maxRadius),
                Mathf.Clamp(style.BorderWidth, 0f, maxRadius), Mathf.Max(0f, style.EdgeSmoothing),
                signedEffectWidth);
            vertex.uv2 = ColorPair(style.FillTop, style.FillBottom);
            vertex.uv3 = ColorPair(style.BorderColor, style.EffectColor);
            // Signed effect alpha opts into an extra inner highlight without another vertex channel.
            if (style.Effect is RoundedGradientEffect.OuterGlowWithInnerRim or RoundedGradientEffect.BevelHighlight)
                vertex.uv3.w = -vertex.uv3.w;

            AddCorner(mesh, vertex, rect.center, halfSize, -meshHalfSize.x, -meshHalfSize.y);
            AddCorner(mesh, vertex, rect.center, halfSize, -meshHalfSize.x, meshHalfSize.y);
            AddCorner(mesh, vertex, rect.center, halfSize, meshHalfSize.x, meshHalfSize.y);
            AddCorner(mesh, vertex, rect.center, halfSize, meshHalfSize.x, -meshHalfSize.y);
            mesh.AddTriangle(0, 1, 2);
            mesh.AddTriangle(2, 3, 0);
        }

        private static void AddCorner(
            VertexHelper mesh, UIVertex vertex, Vector2 center, Vector2 halfSize, float x, float y)
        {
            vertex.position = new Vector3(center.x + x, center.y + y);
            vertex.uv0 = new Vector4(x, y, halfSize.x, halfSize.y);
            mesh.AddVert(vertex);
        }

        // Each RGB triplet fits into an exact 24-bit float integer. Alpha keeps float precision.
        private static Vector4 ColorPair(Color first, Color second) =>
            new(PackRgb(first), Mathf.Clamp01(first.a), PackRgb(second), Mathf.Clamp01(second.a));

        private static float PackRgb(Color color)
        {
            Color32 bytes = color;
            return (bytes.r << 16) | (bytes.g << 8) | bytes.b;
        }
    }
}
