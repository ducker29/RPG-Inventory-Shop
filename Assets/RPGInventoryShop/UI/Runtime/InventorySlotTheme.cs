using UnityEngine;

namespace RpgInventoryShop.UI
{
    public static class InventorySlotTheme
    {
        // Normal corresponds to the Idle row of the procedural UI specification.
        public static RoundedGradientStyle Normal => Slot(
            0x242B35, 0x151A20, InventoryShopTheme.Outline, 0.55f, 2f,
            RoundedGradientEffect.TopHighlight, InventoryShopTheme.Ivory, 0.04f, 8f);

        public static RoundedGradientStyle Hover => Slot(
            0x37404B, 0x222A33, InventoryShopTheme.Mist, 0.75f, 2f,
            RoundedGradientEffect.OuterGlow, InventoryShopTheme.Mist, 0.12f, 8f);

        public static RoundedGradientStyle Selected => Slot(
            0x65402B, 0x241C1B, InventoryShopTheme.Ember, 1f, 3f,
            RoundedGradientEffect.OuterGlow, InventoryShopTheme.Ember, 0.32f, 14f);

        private static RoundedGradientStyle Slot(
            int top, int bottom, Color border, float borderAlpha, float borderWidth,
            RoundedGradientEffect effect, Color effectColor, float effectAlpha, float effectWidth) => new()
        {
            FillTop = Rgb(top, 0.98f),
            FillBottom = Rgb(bottom, 0.98f),
            Radius = 14f,
            BorderWidth = borderWidth,
            BorderColor = InventoryShopTheme.WithAlpha(border, borderAlpha),
            EdgeSmoothing = 1f,
            Effect = effect,
            EffectColor = InventoryShopTheme.WithAlpha(effectColor, effectAlpha),
            EffectWidth = effectWidth
        };

        private static Color Rgb(int rgb, float alpha) =>
            new((rgb >> 16) / 255f, ((rgb >> 8) & 255) / 255f, (rgb & 255) / 255f, alpha);
    }
}
