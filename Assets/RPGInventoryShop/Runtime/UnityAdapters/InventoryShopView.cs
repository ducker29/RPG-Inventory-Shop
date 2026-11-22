using System;
using System.Collections.Generic;
using System.Linq;
using RpgInventoryShop.Domain;
using RpgInventoryShop.Presentation;
using RpgInventoryShop.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Theme = RpgInventoryShop.UI.InventoryShopTheme;

namespace RpgInventoryShop.UnityAdapters
{
    [RequireComponent(typeof(Canvas))]
    public sealed class InventoryShopView : MonoBehaviour, IInventoryShopView
    {
        [SerializeField] private Material surfaceMaterial;
        [SerializeField] private Texture2D background;
        [SerializeField] private Sprite banner, heart, coins, backpack, scales;
        [SerializeField] private Font regularFont, boldFont;

        private readonly Dictionary<string, ItemDefinitionAsset> itemContent = new(StringComparer.Ordinal);
        private readonly List<(ProceduralButton Button, RoundedGradientGraphic Surface, Image Icon, Text Count)> slots = new();
        private readonly List<(ItemDefinition Item, ProceduralButton Button, RoundedGradientGraphic Surface, Text Price)> offers = new();
        private InventoryShopPresenter presenter;
        private bool started, built;
        private Text hpText, goldText, slotCount, detailTitle, detailDescription, statusText;
        private RectTransform hpFill, frame;
        private Image detailIcon;
        private ActionControls use, buy, sell;

        public event Action<int> SlotSelected;
        public event Action<string> OfferSelected;
        public event Action BuyRequested;
        public event Action SellRequested;
        public event Action UseRequested;

        public void ConfigureItems(IEnumerable<ItemDefinitionAsset> items)
        {
            itemContent.Clear();
            foreach (var item in items)
                itemContent.Add(item.ItemId, item);
        }

        public void Connect(InventoryShopPresenter presenter) => this.presenter = presenter;

        private void Start()
        {
            started = true;
            presenter?.Initialize();
            if (slots.Count > 0 && EventSystem.current)
                EventSystem.current.SetSelectedGameObject(slots[0].Button.gameObject);
        }

        private void OnEnable()
        {
            if (started)
                presenter?.Initialize();
        }

        private void OnDisable() => presenter?.Dispose();

        private void OnRectTransformDimensionsChange()
        {
            if (!frame)
                return;

            Vector2 size = ((RectTransform)transform).rect.size;
            frame.localScale = Vector3.one * Mathf.Min(size.x / 1920f, size.y / 1080f, 1f);
        }

        public void Render(InventoryShopScreen screen)
        {
            if (!built)
                Build(screen.Offers);

            hpText.text = $"HP  {screen.Hp} / {screen.MaxHp}";
            float ratio = screen.Hp / (float)screen.MaxHp;
            hpFill.gameObject.SetActive(ratio > 0);
            hpFill.anchorMax = new Vector2(ratio, 1);
            goldText.text = $"{screen.Gold} Gold";
            slotCount.text = $"{screen.Slots.Count(stack => stack != null)} / {screen.Slots.Length}";

            for (int index = 0; index < slots.Count; index++)
            {
                var stack = screen.Slots[index];
                var slot = slots[index];
                SetIcon(slot.Icon, stack?.Item.Id);
                slot.Count.text = stack?.Quantity > 1 ? $"×{stack.Quantity}" : "";
                var normal = index == screen.SelectedSlot ? Theme.SlotSelected : InventorySlotTheme.Normal;
                var hover = index == screen.SelectedSlot ? normal : InventorySlotTheme.Hover;
                slot.Button.SetStyles(slot.Surface, normal, hover, hover, normal);
            }

            foreach (var row in offers)
            {
                bool affordable = screen.Gold >= row.Item.BasePrice;
                bool selected = screen.SelectedOffer == row.Item.Id;
                var normal = selected ? Theme.RowSelected : affordable ? Theme.Row : Theme.Unaffordable;
                var hover = selected ? normal : Theme.RowHover;
                row.Button.SetStyles(row.Surface, normal, hover, hover, normal);
                row.Price.color = affordable ? Theme.Ivory : Theme.Blood;
            }

            SetIcon(detailIcon, screen.Details?.Id);
            detailTitle.text = screen.Details?.Name ?? "No item selected";
            detailDescription.text = screen.Details == null ? "" : Description(screen.Details);
            statusText.text = screen.Status;
            use.SetEnabled(screen.CanUse);
            buy.SetEnabled(screen.CanBuy);
            sell.SetEnabled(screen.CanSell);
        }

        private void Build(ItemDefinition[] items)
        {
            built = true;
            var root = (RectTransform)transform;
            var backdrop = Stretch("Castle Background", root).gameObject.AddComponent<RawImage>();
            backdrop.texture = background;
            backdrop.raycastTarget = false;
            var crop = backdrop.gameObject.AddComponent<AspectRatioFitter>();
            crop.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            crop.aspectRatio = (float)background.width / background.height;
            var dim = Stretch("Backdrop Dim", root);
            Graphic(dim, Theme.Solid(Theme.WithAlpha(Theme.Void, 0.72f)));

            frame = Box("Screen Layout", root, 0, 0, 1920, 1080);
            frame.anchorMin = frame.anchorMax = frame.pivot = new Vector2(0.5f, 0.5f);
            frame.anchoredPosition = Vector2.zero;
            OnRectTransformDimensionsChange();

            Surface("Header", frame, 0, 0, 1920, 116, Theme.Header);
            Icon("Sword Banner", frame, banner, 59, 0, 104, 132);
            Label(frame, "Inventory & Shop", 195, 21, 760, 82, 65, Theme.Ivory, bold: true);
            Icon("Heart", frame, heart, 1155, 29, 38, 38);
            hpText = Label(frame, "", 1208, 25, 325, 48, 32, Theme.Ivory);
            var track = Surface("HP Track", frame, 1157, 78, 408, 24, Theme.HpTrack).rectTransform;
            var fillBounds = Stretch("HP Insets", track);
            fillBounds.offsetMin = new Vector2(2, 2);
            fillBounds.offsetMax = new Vector2(-2, -2);
            hpFill = Stretch("HP Fill", fillBounds);
            Graphic(hpFill, Theme.HpFill);
            Line(frame, 1602, 37, 1, 66, Theme.WithAlpha(Theme.Outline, 0.42f));
            Icon("Gold Coins", frame, coins, 1651, 40, 49, 52);
            goldText = Label(frame, "", 1713, 33, 170, 60, 36, Theme.Ember, bold: true);

            var inventory = Panel("Inventory", frame, 70, 163, 836, 648, Theme.Panel, 28, 12, 0.38f);
            Icon("Backpack", inventory, backpack, 61, 25, 54, 52);
            Label(inventory, "Inventory", 138, 17, 500, 65, 47, Theme.Ivory, bold: true);
            slotCount = Label(inventory, "", 626, 24, 169, 55, 29, Theme.Muted, TextAnchor.MiddleRight);
            PanelDivider(inventory, 42, 95, 752);
            BuildSlots(inventory);

            var merchant = Panel("Merchant", frame, 944, 163, 927, 648, Theme.Panel, 28, 12, 0.38f);
            Icon("Scales", merchant, scales, 55, 25, 64, 52);
            Label(merchant, "Merchant", 151, 17, 620, 65, 47, Theme.Ivory, bold: true);
            PanelDivider(merchant, 42, 95, 839);
            BuildMerchant(merchant, items);

            var details = Panel("Item Details", frame, 70, 844, 1801, 198, Theme.Details, 20, 8, 0.30f);
            detailIcon = Icon("Selected Item", details, null, 79, 22, 130, 150);
            Line(details, 257, 34, 1, 132, Theme.WithAlpha(Theme.Ember, 0.38f));
            detailTitle = Label(details, "", 294, 29, 590, 62, 44, Theme.Ivory, bold: true);
            detailDescription = Label(details, "", 296, 89, 590, 48, 29, Theme.Muted);
            statusText = Label(details, "", 296, 137, 600, 37, 23, Theme.Blood);
            use = Action(details, "Use", 932, Theme.Use, () => UseRequested?.Invoke());
            buy = Action(details, "Buy", 1221, Theme.Buy, () => BuyRequested?.Invoke());
            sell = Action(details, "Sell", 1510, Theme.Sell, () => SellRequested?.Invoke());
        }

        private void BuildSlots(RectTransform panel)
        {
            const int columns = 5;
            const int rows = 4;
            const float gap = 12;
            var region = Box("Slots · 5 × 4", panel, 55, 120, panel.rect.width - 110, panel.rect.height - 154);
            float cellSize = Mathf.Min((region.rect.width - (columns - 1) * gap) / columns,
                (region.rect.height - (rows - 1) * gap) / rows);
            float leftInset = (region.rect.width - (columns * cellSize + (columns - 1) * gap)) * 0.5f;
            for (int index = 0; index < columns * rows; index++)
            {
                int selectedIndex = index;
                var surface = Surface($"Inventory Slot {index + 1}", region,
                    leftInset + (index % columns) * (cellSize + gap), (index / columns) * (cellSize + gap),
                    cellSize, cellSize, InventorySlotTheme.Normal);
                var button = MakeButton(surface, () => SlotSelected?.Invoke(selectedIndex));
                var rect = surface.rectTransform;
                var icon = Icon("Item Icon", rect, null, 13, 7, cellSize - 26, cellSize - 16);
                var count = Label(rect, "", 6, cellSize - 40, cellSize - 16, 36, 26,
                    Theme.Ivory, TextAnchor.MiddleRight);
                count.verticalOverflow = VerticalWrapMode.Overflow;
                slots.Add((button, surface, icon, count));
            }
        }

        private void BuildMerchant(RectTransform panel, ItemDefinition[] items)
        {
            var window = Box("Merchant Scroll", panel, 32, 111, 866, 490);
            var viewport = Stretch("Merchant Viewport", window);
            Graphic(viewport, Theme.Solid(Color.clear)).raycastTarget = true;
            viewport.gameObject.AddComponent<RectMask2D>();
            var merchantContent = Box("Offers", viewport, 0, 0, 866, Mathf.Max(490, 24 + items.Length * 153));
            var scroll = window.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = viewport;
            scroll.content = merchantContent;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 36;

            for (int index = 0; index < items.Length; index++)
            {
                var item = items[index];
                var surface = Surface($"Offer · {item.Name}", merchantContent, 14, 10 + index * 153,
                    826, 137, Theme.Row);
                var button = MakeButton(surface, () => OfferSelected?.Invoke(item.Id));
                var rect = surface.rectTransform;
                Surface("Item Backdrop", rect, 23, 12, 126, 112, Theme.IconBackdrop);
                var icon = Icon("Item Icon", rect, null, 39, 16, 92, 103);
                SetIcon(icon, item.Id);
                Label(rect, item.Name, 187, 17, 467, 53, 36, Theme.Ivory, bold: true);
                Label(rect, Description(item), 189, 71, 442, 42, 28, Theme.Muted);
                Icon("Price Coins", rect, coins, 678, 48, 43, 39);
                var price = Label(rect, item.BasePrice.ToString(), 724, 42, 77, 51, 34,
                    Theme.Ivory, TextAnchor.MiddleRight, bold: true);
                offers.Add((item, button, surface, price));
            }
        }

        private ActionControls Action(RectTransform parent, string name, float x,
            RoundedGradientStyle idle, Action clicked)
        {
            var surface = Surface(name, parent, x, 55, 268, 90, idle);
            var button = MakeButton(surface, clicked);
            var title = Label(surface.rectTransform, name, 0, 0, 268, 90, 40, Theme.Ivory,
                TextAnchor.MiddleCenter, bold: true);
            button.SetStyles(surface, idle, Theme.ActionHover(idle), Theme.ActionPressed(idle),
                Theme.Disabled, title.rectTransform);
            return new ActionControls(button, title);
        }

        private static ProceduralButton MakeButton(RoundedGradientGraphic graphic, Action clicked)
        {
            graphic.raycastTarget = true;
            var button = graphic.gameObject.AddComponent<ProceduralButton>();
            button.targetGraphic = graphic;
            button.onClick.AddListener(() => clicked());
            return button;
        }

        private RectTransform Panel(string name, RectTransform parent, float x, float y,
            float width, float height, RoundedGradientStyle style, float blur, float offset, float alpha)
        {
            var shadow = Theme.Solid(Color.clear);
            shadow.Radius = style.Radius;
            shadow = Theme.Effect(shadow, RoundedGradientEffect.OuterGlow, new Color(0, 0, 0, alpha), blur);
            Surface(name + " Shadow", parent, x, y + offset, width, height, shadow);
            return Surface(name, parent, x, y, width, height, style).rectTransform;
        }

        private RoundedGradientGraphic Surface(string name, RectTransform parent, float x, float y,
            float width, float height, RoundedGradientStyle style) =>
            Graphic(Box(name, parent, x, y, width, height), style);

        private RoundedGradientGraphic Graphic(RectTransform rect, RoundedGradientStyle style)
        {
            var graphic = rect.gameObject.AddComponent<RoundedGradientGraphic>();
            graphic.material = surfaceMaterial;
            graphic.Style = style;
            graphic.raycastTarget = false;
            return graphic;
        }

        private void Line(RectTransform parent, float x, float y, float width, float height, Color color) =>
            Surface("Divider", parent, x, y, width, height, Theme.Solid(color));

        private void PanelDivider(RectTransform parent, float x, float y, float width)
        {
            var style = Theme.Solid(Theme.WithAlpha(Theme.Ember, 0));
            style.FillBottom = Theme.WithAlpha(Theme.Ember, 0.38f);
            style.EdgeSmoothing = 0;
            float half = width * 0.5f;
            var left = Surface("Divider Left", parent, x, y, 1, half, style).rectTransform;
            left.localRotation = Quaternion.Euler(0, 0, 90);
            style.FillTop = style.FillBottom;
            style.FillBottom = Theme.WithAlpha(Theme.Ember, 0);
            var right = Surface("Divider Right", parent, x + half, y, 1, half, style).rectTransform;
            right.localRotation = left.localRotation;
        }

        private Text Label(RectTransform parent, string value, float x, float y, float width,
            float height, int size, Color color, TextAnchor alignment = TextAnchor.MiddleLeft, bool bold = false)
        {
            var text = Box(string.IsNullOrEmpty(value) ? "Dynamic Label" : value, parent,
                x, y, width, height).gameObject.AddComponent<Text>();
            text.font = bold ? boldFont : regularFont;
            text.text = value;
            text.fontSize = size;
            text.color = color;
            text.alignment = alignment;
            text.raycastTarget = false;
            return text;
        }

        private static Image Icon(string name, RectTransform parent, Sprite sprite, float x, float y,
            float width, float height)
        {
            var image = Box(name, parent, x, y, width, height).gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = true;
            image.raycastTarget = false;
            image.enabled = sprite != null;
            return image;
        }

        private void SetIcon(Image image, string id)
        {
            image.sprite = id != null && itemContent.TryGetValue(id, out ItemDefinitionAsset item) ? item.Icon : null;
            image.enabled = image.sprite != null;
        }

        private string Description(ItemDefinition item)
        {
            if (itemContent.TryGetValue(item.Id, out ItemDefinitionAsset asset) &&
                !string.IsNullOrWhiteSpace(asset.Description))
                return asset.Description;

            return item.Healing > 0 ? $"Restores {item.Healing} HP." : "";
        }

        private static RectTransform Box(string name, RectTransform parent, float x, float y,
            float width, float height)
        {
            var rect = NewRect(name, parent);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(width, height);
            return rect;
        }

        private static RectTransform Stretch(string name, RectTransform parent)
        {
            var rect = NewRect(name, parent);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            return rect;
        }

        private static RectTransform NewRect(string name, RectTransform parent)
        {
            var rect = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
            rect.SetParent(parent, false);
            return rect;
        }

        private sealed class ActionControls
        {
            private readonly ProceduralButton button;
            private readonly Text title;

            public ActionControls(ProceduralButton button, Text title)
            {
                this.button = button;
                this.title = title;
            }

            public void SetEnabled(bool enabled)
            {
                button.interactable = enabled;
                title.color = enabled ? Theme.Ivory : Theme.WithAlpha(Theme.Muted, 0.55f);
            }
        }
    }
}
