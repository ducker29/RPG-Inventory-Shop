using UnityEngine;
using UnityEngine.UI;

namespace RpgInventoryShop.UI
{
    public sealed class MerchantRowView : MonoBehaviour
    {
        [SerializeField] private ProceduralButton button;
        [SerializeField] private RoundedGradientGraphic surface;
        [SerializeField] private Image icon;
        [SerializeField] private Text itemName;
        [SerializeField] private Text description;
        [SerializeField] private Text price;

        public ProceduralButton Button => button;
        public RoundedGradientGraphic Surface => surface;
        public Image Icon => icon;
        public Text ItemName => itemName;
        public Text Description => description;
        public Text Price => price;
    }
}
