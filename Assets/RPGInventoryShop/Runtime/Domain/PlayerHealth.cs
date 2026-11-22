using System;

namespace RpgInventoryShop.Domain
{
    public sealed class PlayerHealth
    {
        public PlayerHealth(int maxHp, int currentHp)
        {
            if (maxHp < 1)
                throw new ArgumentOutOfRangeException(nameof(maxHp));
            if (currentHp < 0 || currentHp > maxHp)
                throw new ArgumentOutOfRangeException(nameof(currentHp));

            MaxHp = maxHp;
            CurrentHp = currentHp;
        }

        public int MaxHp { get; }
        public int CurrentHp { get; private set; }
        public bool IsFull => CurrentHp == MaxHp;

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
