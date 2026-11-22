using System;

namespace RpgInventoryShop.Domain
{
    public sealed class GoldWallet
    {
        public GoldWallet(int balance)
        {
            RestoreBalance(balance);
        }

        public void RestoreBalance(int balance)
        {
            if (balance < 0)
                throw new ArgumentOutOfRangeException(nameof(balance));

            Balance = balance;
        }

        public int Balance { get; private set; }

        public bool CanSpend(int amount) => amount >= 0 && amount <= Balance;

        public bool CanCredit(int amount) => amount >= 0 && amount <= int.MaxValue - Balance;

        public bool TrySpend(int amount)
        {
            if (!CanSpend(amount))
                return false;

            Balance -= amount;
            return true;
        }

        public bool TryCredit(int amount)
        {
            if (!CanCredit(amount))
                return false;

            Balance += amount;
            return true;
        }
    }
}
