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
        [SerializeField] private Text hpText, goldText, slotCount, detailTitle, detailDescription, statusText;
        [SerializeField] private RectTransform hpFill, frame;
        [SerializeField] private Image detailIcon;
        [SerializeField] private ActionControls use, buy, sell;
        [SerializeField] private InventorySlotView[] slotViews;
        [SerializeField] private ScrollRect merchantScroll;
        [SerializeField] private MerchantRowView merchantRowPrefab;
        [SerializeField] private GameObject merchantPreview;
        [SerializeField] private Vector2 merchantRowOrigin = new(14, -10);
        [SerializeField, Min(0)] private float merchantRowSpacing = 16;

        private readonly Dictionary<string, ItemDefinitionAsset> itemContent = new(StringComparer.Ordinal);
        private readonly List<(ItemDefinition Item, MerchantRowView View)> offers = new();
        private InventoryShopPresenter presenter;
        private bool started, controlsBound;

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
            if (merchantPreview)
                merchantPreview.SetActive(false);
            OnRectTransformDimensionsChange();
            presenter?.Initialize();
            if (slotViews.Length > 0 && EventSystem.current)
                EventSystem.current.SetSelectedGameObject(slotViews[0].Button.gameObject);
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
            if (!controlsBound)
                BindControls(screen.Offers);

            hpText.text = $"HP  {screen.Hp} / {screen.MaxHp}";
            float ratio = screen.Hp / (float)screen.MaxHp;
            hpFill.gameObject.SetActive(ratio > 0);
            hpFill.anchorMax = new Vector2(ratio, 1);
            goldText.text = $"{screen.Gold} Gold";
            slotCount.text = $"{screen.Slots.Count(stack => stack != null)} / {screen.Slots.Length}";

            for (int index = 0; index < slotViews.Length; index++)
            {
                var stack = screen.Slots[index];
                var slot = slotViews[index];
                SetIcon(slot.Icon, stack?.Item.Id);
                slot.Count.text = stack?.Quantity > 1 ? $"×{stack.Quantity}" : "";
                var normal = index == screen.SelectedSlot ? Theme.SlotSelected : InventorySlotTheme.Normal;
                var hover = index == screen.SelectedSlot ? normal : InventorySlotTheme.Hover;
                slot.Button.SetStyles(slot.Surface, normal, hover, hover, normal);
            }

            foreach (var (item, row) in offers)
            {
                bool affordable = screen.Gold >= item.BasePrice;
                bool selected = screen.SelectedOffer == item.Id;
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

        private void BindControls(ItemDefinition[] items)
        {
            controlsBound = true;
            for (int index = 0; index < slotViews.Length; index++)
            {
                int selectedIndex = index;
                slotViews[index].Button.onClick.AddListener(() => SlotSelected?.Invoke(selectedIndex));
            }

            use.Bind(Theme.Use, () => UseRequested?.Invoke());
            buy.Bind(Theme.Buy, () => BuyRequested?.Invoke());
            sell.Bind(Theme.Sell, () => SellRequested?.Invoke());

            var content = merchantScroll.content;
            float stride = ((RectTransform)merchantRowPrefab.transform).rect.height + merchantRowSpacing;
            content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,
                Mathf.Max(merchantScroll.viewport.rect.height, 24 + items.Length * stride));
            for (int index = 0; index < items.Length; index++)
            {
                var item = items[index];
                var row = Instantiate(merchantRowPrefab, content, false);
                row.name = $"Offer · {item.Name}";
                ((RectTransform)row.transform).anchoredPosition = merchantRowOrigin - Vector2.up * (index * stride);
                row.ItemName.text = item.Name;
                row.Description.text = Description(item);
                row.Price.text = item.BasePrice.ToString();
                SetIcon(row.Icon, item.Id);
                row.Button.onClick.AddListener(() => OfferSelected?.Invoke(item.Id));
                offers.Add((item, row));
            }
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

        [Serializable]
        private sealed class ActionControls
        {
            [SerializeField] private ProceduralButton button;
            [SerializeField] private Text title;

            public void Bind(RoundedGradientStyle idle, Action clicked)
            {
                button.SetStyles((RoundedGradientGraphic)button.targetGraphic, idle, Theme.ActionHover(idle),
                    Theme.ActionPressed(idle), Theme.Disabled, title.rectTransform);
                button.onClick.AddListener(() => clicked());
            }

            public void SetEnabled(bool enabled)
            {
                button.interactable = enabled;
                title.color = enabled ? Theme.Ivory : Theme.WithAlpha(Theme.Muted, 0.55f);
            }
        }
    }
}
