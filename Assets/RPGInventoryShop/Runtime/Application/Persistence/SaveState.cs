using System;
using RpgInventoryShop.Domain;

namespace RpgInventoryShop.Application
{
    public enum SaveStatus
    {
        Success,
        RecoveredBackup,
        Missing,
        Corrupt,
        UnsupportedVersion,
        NewerVersion,
        InvalidState,
        UnknownItem,
        IoError,
        IndeterminateVersion
    }

    public sealed class SaveState
    {
        private readonly Inventory inventory;
        private readonly GoldWallet wallet;
        private readonly PlayerHealth health;
        private readonly IItemCatalog catalog;

        public SaveState(Inventory inventory, GoldWallet wallet, PlayerHealth health, IItemCatalog catalog)
        {
            this.inventory = inventory ?? throw new ArgumentNullException(nameof(inventory));
            this.wallet = wallet ?? throw new ArgumentNullException(nameof(wallet));
            this.health = health ?? throw new ArgumentNullException(nameof(health));
            this.catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
        }

        public SaveData Capture()
        {
            var slots = new SavedSlot[Inventory.SlotCount];
            for (int index = 0; index < slots.Length; index++)
            {
                ItemStack stack = inventory.GetSlot(index);
                if (stack != null)
                    slots[index] = new SavedSlot { ItemId = stack.Item.Id, Quantity = stack.Quantity };
            }

            return new SaveData
            {
                Version = SaveData.CurrentVersion,
                Gold = wallet.Balance,
                Hp = health.CurrentHp,
                Slots = slots
            };
        }

        public SaveStatus Restore(SaveData data)
        {
            SaveStatus status = Resolve(data, out var slots, out int gold, out int hp);
            if (status != SaveStatus.Success)
                return status;
            if (!inventory.TryRestoreSlots(slots))
                return SaveStatus.InvalidState;

            // Both values were validated before replacing the inventory. The session is synchronous.
            wallet.RestoreBalance(gold);
            health.RestoreHp(hp);
            return SaveStatus.Success;
        }

        public SaveStatus Validate(SaveData data)
        {
            SaveStatus status = Resolve(data, out var slots, out _, out _);
            if (status != SaveStatus.Success)
                return status;

            return inventory.CanRestoreSlots(slots) ? SaveStatus.Success : SaveStatus.InvalidState;
        }

        private SaveStatus Resolve(
            SaveData data, out (ItemDefinition Item, int Quantity)[] slots, out int gold, out int hp)
        {
            slots = null;
            gold = data?.Gold ?? 0;
            hp = data?.Hp ?? 0;
            if (data == null)
                return SaveStatus.Corrupt;
            if (data.Version > SaveData.CurrentVersion)
                return SaveStatus.NewerVersion;
            if (data.Version != SaveData.CurrentVersion)
                return SaveStatus.UnsupportedVersion;
            if (gold < 0 || hp < 0 || hp > health.MaxHp || data.Slots?.Length != Inventory.SlotCount)
                return SaveStatus.InvalidState;

            slots = new (ItemDefinition, int)[Inventory.SlotCount];
            for (int index = 0; index < slots.Length; index++)
            {
                SavedSlot saved = data.Slots[index];
                if (saved == null)
                    continue;
                if (string.IsNullOrWhiteSpace(saved.ItemId) || saved.Quantity < 1)
                    return SaveStatus.InvalidState;
                if (!catalog.TryGet(saved.ItemId, out ItemDefinition item) || item?.Id != saved.ItemId)
                    return SaveStatus.UnknownItem;
                if (saved.Quantity > item.StackLimit)
                    return SaveStatus.InvalidState;

                slots[index] = (item, saved.Quantity);
            }

            return SaveStatus.Success;
        }
    }
}
