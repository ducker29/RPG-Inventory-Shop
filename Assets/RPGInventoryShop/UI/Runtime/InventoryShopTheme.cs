using UnityEngine;

namespace RpgInventoryShop.UI
{
    public static class InventoryShopTheme
    {
        public static readonly Color Void = ColorOf(0x101318);
        public static readonly Color Ivory = ColorOf(0xF0E6D6);
        public static readonly Color Muted = ColorOf(0xA6A9B0);
        public static readonly Color Ember = ColorOf(0xC58D59);
        public static readonly Color Blood = ColorOf(0xA84950);
        public static readonly Color Mist = ColorOf(0x8D9CB2);
        public static readonly Color Outline = ColorOf(0x596270);
        public static readonly Color Sage = ColorOf(0x789889);

        // Milestone 4B tuning for the composed screen. The documented prototype presets are unchanged.
        public static RoundedGradientStyle Panel => Surface(0x303845, 0x101318, 0.60f, 0.84f,
            WithAlpha(Mist, 0.24f), 1.5f, 24);
        public static RoundedGradientStyle Details => Surface(0x29313D, 0x101318, 0.65f, 0.86f,
            WithAlpha(Ember, 0.23f), 1.5f, 22);
        public static RoundedGradientStyle Header => Surface(0x101318, 0x101318, 0.33f, 0f, Color.clear, 0, 0);
        public static RoundedGradientStyle HpTrack => Surface(0x34303A, 0x171920, 1, 1,
            WithAlpha(Outline, 0.50f), 2, 10);
        public static RoundedGradientStyle HpFill => Effect(Surface(0xE16C6D, 0x8D293A, 1, 1,
            Color.clear, 0, 8), RoundedGradientEffect.TopHighlight, WithAlpha(Ivory, 0.15f), 8);
        public static RoundedGradientStyle Row => Surface(0x2A323D, 0x1B2129, 0.96f, 0.96f,
            WithAlpha(Outline, 0.42f), 2, 16);
        public static RoundedGradientStyle RowHover => Effect(Surface(0x39404A, 0x242A32, 0.98f, 0.98f,
            WithAlpha(Mist, 0.65f), 2, 16), RoundedGradientEffect.OuterGlow, WithAlpha(Mist, 0.10f), 8);
        public static RoundedGradientStyle SlotSelected => Selection(InventorySlotTheme.Selected, 0.14f, 7);
        public static RoundedGradientStyle RowSelected => Selection(
            Surface(0x69412D, 0x282329, 0.98f, 0.98f, Ember, 2.5f, 16), 0.11f, 7);
        public static RoundedGradientStyle Unaffordable => Surface(0x252A32, 0x1B2029, 0.75f, 0.75f,
            WithAlpha(Outline, 0.25f), 1, 16);
        public static RoundedGradientStyle IconBackdrop => Surface(0x252B34, 0x171B23, 0.95f, 0.95f,
            WithAlpha(Outline, 0.35f), 1, 12);
        public static RoundedGradientStyle Use => ActionStyle(0x47664B, 0x253D2D, Sage, 0.75f, 0.10f);
        public static RoundedGradientStyle Buy => ActionStyle(0x985C39, 0x4B2C22, Ember, 0.90f, 0.10f);
        public static RoundedGradientStyle Sell => ActionStyle(0x405165, 0x222B36, Mist, 0.65f, 0.08f);
        public static RoundedGradientStyle Disabled => Surface(0x30343B, 0x20242B, 0.82f, 0.82f,
            WithAlpha(Outline, 0.35f), 1, 16);

        public static RoundedGradientStyle ActionHover(RoundedGradientStyle style)
        {
            style.FillTop = ScaleRgb(style.FillTop, 1.08f);
            style.FillBottom = ScaleRgb(style.FillBottom, 1.08f);
            style.BorderColor = WithAlpha(style.BorderColor, 1);
            return Effect(style, RoundedGradientEffect.OuterGlow, WithAlpha(style.BorderColor, 0.16f), 12);
        }

        public static RoundedGradientStyle ActionPressed(RoundedGradientStyle style)
        {
            style.FillTop = ScaleRgb(style.FillTop, 0.88f);
            style.FillBottom = ScaleRgb(style.FillBottom, 0.88f);
            style.Effect = RoundedGradientEffect.None;
            return style;
        }

        public static RoundedGradientStyle Solid(Color color) => new()
        {
            FillTop = color, FillBottom = color, EdgeSmoothing = 1
        };

        public static RoundedGradientStyle Effect(RoundedGradientStyle style, RoundedGradientEffect effect,
            Color color, float width)
        {
            style.Effect = effect;
            style.EffectColor = color;
            style.EffectWidth = width;
            return style;
        }

        public static Color WithAlpha(Color color, float alpha)
        {
            color.a = alpha;
            return color;
        }

        private static RoundedGradientStyle ActionStyle(int top, int bottom, Color border, float alpha, float highlight) =>
            Effect(Surface(top, bottom, 1, 1, WithAlpha(border, alpha), 2, 16),
                RoundedGradientEffect.BevelHighlight, WithAlpha(Ivory, highlight), 24);

        private static RoundedGradientStyle Selection(RoundedGradientStyle style, float alpha, float width) =>
            Effect(style, RoundedGradientEffect.OuterGlowWithInnerRim, WithAlpha(Ember, alpha), width);

        private static RoundedGradientStyle Surface(int top, int bottom, float topAlpha, float bottomAlpha,
            Color border, float width, float radius) => new()
        {
            FillTop = ColorOf(top, topAlpha), FillBottom = ColorOf(bottom, bottomAlpha),
            BorderColor = border, BorderWidth = width, Radius = radius, EdgeSmoothing = 1
        };

        private static Color ScaleRgb(Color c, float scale) => new(
            Mathf.Clamp01(c.r * scale), Mathf.Clamp01(c.g * scale), Mathf.Clamp01(c.b * scale), c.a);

        private static Color ColorOf(int rgb, float alpha = 1) =>
            new((rgb >> 16) / 255f, ((rgb >> 8) & 255) / 255f, (rgb & 255) / 255f, alpha);
    }
}
