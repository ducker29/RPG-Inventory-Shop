# RPG Inventory & Shop

A Unity portfolio demo of a 20-slot RPG inventory and merchant screen. The Obsidian & Ember interface uses a procedural uGUI shader for gradients, rounded corners, borders, and highlights.

![Inventory and merchant panels in the Obsidian & Ember demo](docs/screenshots/inventory-shop.png)

## Features

- Buy and sell Health Potions, Iron Swords, and Shields one unit at a time. Sale prices are half the purchase price, rounded down.
- Stack potions and store each sword or shield in its own slot.
- Use potions to restore HP. The Use action is unavailable for swords, shields, and potions at full HP.
- Track gold, HP, item quantities, and occupied slots as you trade.

## Run the demo

1. Open the project in Unity 6000.0.84f1.
2. Let Unity import assets and resolve packages.
3. Open `Assets/RPGInventoryShop/Scenes/InventoryShopDemo.unity` and enter Play Mode.
4. Select a merchant offer to buy it, or select an inventory slot to sell or use its item.

## Architecture

- **Domain** holds the inventory, wallet, health, and item rules in plain C#.
- **Application** coordinates trading, item use, and save-state validation through Unity-independent services.
- **MVP** keeps input and rendering in the Unity View. A plain C# Presenter coordinates selection, actions, and view updates.
- **Zenject (Extenject)** composes one session through the demo scene's SceneContext. Unity adapters provide the ScriptableObject catalog and JSON file storage. The demo uses URP 2D, uGUI, and scene-authored layouts with slot and merchant-row prefabs.

## Persistence

The JSON store writes a versioned snapshot of all 20 slots, gold, and current HP. Slots store stable item IDs and quantities. Loading resolves IDs through the catalog and validates the full snapshot before applying it. The demo screen has no save/load controls or automatic persistence.

## Verification

Final QA in Unity 6000.0.84f1 passed all 86 NUnit EditMode tests. Play Mode checks exercised trading all three items, potion use, insufficient gold, blocked actions, and a save/load round trip through the JSON store.
