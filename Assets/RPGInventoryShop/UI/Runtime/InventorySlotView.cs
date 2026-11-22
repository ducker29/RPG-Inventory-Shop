using UnityEngine;
using UnityEngine.UI;

namespace RpgInventoryShop.UI
{
    public sealed class InventorySlotView : MonoBehaviour
    {
        [SerializeField] private ProceduralButton button;
        [SerializeField] private RoundedGradientGraphic surface;
        [SerializeField] private Image icon;
        [SerializeField] private Text count;

        public ProceduralButton Button => button;
        public RoundedGradientGraphic Surface => surface;
        public Image Icon => icon;
        public Text Count => count;
    }
}
