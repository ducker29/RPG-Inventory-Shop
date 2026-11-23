using RpgInventoryShop.UnityAdapters;
using UnityEngine;

namespace RpgInventoryShop.Composition
{
    [CreateAssetMenu(menuName = "RPG Inventory Shop/Game Config")]
    public sealed class GameConfigAsset : ScriptableObject
    {
        [SerializeField] private ItemCatalogAsset itemCatalog;
        [SerializeField] private StartingInventoryAsset startingInventory;
        [SerializeField, Min(0)] private int startingGold = 100;
        [SerializeField, Min(1)] private int maxHp = 100;
        [SerializeField, Min(0)] private int startingHp = 50;
        [SerializeField, Min(1)] private int maxMana = 100;
        [SerializeField, Min(0)] private int startingMana = 40;

        public ItemCatalogAsset ItemCatalog => itemCatalog;
        public StartingInventoryAsset StartingInventory => startingInventory;
        public int StartingGold => startingGold;
        public int MaxHp => maxHp;
        public int StartingHp => startingHp;
        public int MaxMana => maxMana;
        public int StartingMana => startingMana;
    }
}
