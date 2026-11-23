using System;

namespace RpgInventoryShop.Domain
{
    public sealed class PlayerMana
    {
        public PlayerMana(int maxMana, int currentMana)
        {
            if (maxMana < 1)
                throw new ArgumentOutOfRangeException(nameof(maxMana));
            MaxMana = maxMana;
            RestoreMana(currentMana);
        }

        public int MaxMana { get; }
        public int CurrentMana { get; private set; }
        public bool IsFull => CurrentMana == MaxMana;

        public void RestoreMana(int currentMana)
        {
            if (currentMana < 0 || currentMana > MaxMana)
                throw new ArgumentOutOfRangeException(nameof(currentMana));
            CurrentMana = currentMana;
        }

        public int Recover(int amount)
        {
            if (amount < 0)
                throw new ArgumentOutOfRangeException(nameof(amount));

            int restored = Math.Min(amount, MaxMana - CurrentMana);
            CurrentMana += restored;
            return restored;
        }
    }
}
