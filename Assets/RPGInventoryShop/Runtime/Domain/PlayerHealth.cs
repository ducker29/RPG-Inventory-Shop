using System;

namespace RpgInventoryShop.Domain
{
    public sealed class PlayerHealth
    {
        public PlayerHealth(int maxHp, int currentHp)
        {
            if (maxHp < 1)
                throw new ArgumentOutOfRangeException(nameof(maxHp));
            MaxHp = maxHp;
            RestoreHp(currentHp);
        }

        public int MaxHp { get; }
        public int CurrentHp { get; private set; }
        public bool IsFull => CurrentHp == MaxHp;

        public void RestoreHp(int currentHp)
        {
            if (currentHp < 0 || currentHp > MaxHp)
                throw new ArgumentOutOfRangeException(nameof(currentHp));
            CurrentHp = currentHp;
        }

        public int Heal(int amount)
        {
            if (amount < 0)
                throw new ArgumentOutOfRangeException(nameof(amount));

            int restored = Math.Min(amount, MaxHp - CurrentHp);
            CurrentHp += restored;
            return restored;
        }
    }
}
