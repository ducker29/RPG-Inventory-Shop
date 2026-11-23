using RpgInventoryShop.Domain;
using UnityEngine;

namespace RpgInventoryShop.UnityAdapters
{
    [CreateAssetMenu(menuName = "RPG Inventory Shop/Item Definition")]
    public sealed class ItemDefinitionAsset : ScriptableObject
    {
        [SerializeField, Tooltip("Unique save key. Keep it unchanged after publishing the item.")]
        private string itemId;
        [SerializeField] private string displayName;
        [SerializeField, TextArea] private string description;
        [SerializeField] private Sprite icon;
        [SerializeField] private int basePrice;
        [SerializeField] private int stackLimit = 1;
        [SerializeField] private int healing;
        [SerializeField] private int manaRestoration;

        public string ItemId => itemId;
        public string Description => description;
        public Sprite Icon => icon;

        public ItemDefinition ToDefinition() => new(itemId, displayName, basePrice, stackLimit, healing, manaRestoration);
    }
}
