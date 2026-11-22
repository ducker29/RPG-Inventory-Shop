using UnityEngine;
using UnityEngine.UI;

namespace RpgInventoryShop.UI
{
    public sealed class ProceduralButton : Button
    {
        private RoundedGradientGraphic surface;
        private RoundedGradientStyle normal, hover, pressed, disabled;
        private RectTransform content;
        private Vector2 contentPosition;

        public void SetStyles(RoundedGradientGraphic graphic, RoundedGradientStyle idle,
            RoundedGradientStyle focused, RoundedGradientStyle down, RoundedGradientStyle unavailable,
            RectTransform pressedContent = null)
        {
            surface = graphic;
            normal = idle;
            hover = focused;
            pressed = down;
            disabled = unavailable;
            content = pressedContent;
            if (content)
                contentPosition = content.anchoredPosition;
            transition = Transition.None;
            DoStateTransition(currentSelectionState, true);
        }

        protected override void DoStateTransition(SelectionState state, bool instant)
        {
            if (!surface)
                return;

            if (content)
                content.anchoredPosition = contentPosition -
                    (state == SelectionState.Pressed ? Vector2.up : Vector2.zero);

            surface.Style = state switch
            {
                SelectionState.Highlighted or SelectionState.Selected => hover,
                SelectionState.Pressed => pressed,
                SelectionState.Disabled => disabled,
                _ => normal
            };
        }
    }
}
