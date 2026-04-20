using System;
using UnityEngine;

namespace BlacktideRequiem.Core.Economy
{
    /// <summary>
    /// Runtime wallet holding balances for every <see cref="CurrencyType"/>.
    /// ScriptableObject so a single wallet asset can be shared across scenes.
    ///
    /// Transactions are atomic and validated: negative or zero amounts are
    /// rejected, spends fail without side-effects when the balance is too
    /// low, and additions clamp at <see cref="MaxBalance"/> to prevent overflow.
    /// See design/gdd/currency-system.md.
    /// </summary>
    [CreateAssetMenu(fileName = "CurrencyWallet", menuName = "Blacktide/Currency Wallet")]
    public class CurrencyWallet : ScriptableObject
    {
        public const long MaxBalance = 999_999_999L;

        [SerializeField] private long doblones;
        [SerializeField] private long gemasDeCalavera;

        /// <summary>
        /// Fires after a balance mutation. Arguments are the currency and the
        /// new balance. Rejected transactions do not raise this event.
        /// </summary>
        public event Action<CurrencyType, long> BalanceChanged;

        public long GetBalance(CurrencyType type)
        {
            return type switch
            {
                CurrencyType.Doblones => doblones,
                CurrencyType.GemasDeCalavera => gemasDeCalavera,
                _ => 0L,
            };
        }

        /// <summary>
        /// Adds a strictly positive amount to the currency balance, clamping at
        /// <see cref="MaxBalance"/>. Returns false and makes no change when
        /// <paramref name="amount"/> is zero or negative.
        /// </summary>
        public bool Add(CurrencyType type, long amount)
        {
            if (amount <= 0)
            {
                return false;
            }

            long current = GetBalance(type);
            long next = current + amount;
            if (next < current || next > MaxBalance)
            {
                next = MaxBalance;
            }

            SetBalance(type, next);
            BalanceChanged?.Invoke(type, next);
            return true;
        }

        /// <summary>
        /// Attempts to spend a strictly positive amount. Returns true and
        /// deducts atomically when the balance is sufficient; otherwise
        /// returns false and leaves the balance unchanged.
        /// </summary>
        public bool TrySpend(CurrencyType type, long amount)
        {
            if (amount <= 0)
            {
                return false;
            }

            long current = GetBalance(type);
            if (current < amount)
            {
                return false;
            }

            long next = current - amount;
            SetBalance(type, next);
            BalanceChanged?.Invoke(type, next);
            return true;
        }

        /// <summary>
        /// Sets both balances to zero. Fires <see cref="BalanceChanged"/> once
        /// per currency when the old value was non-zero.
        /// </summary>
        public void ResetBalances()
        {
            if (doblones != 0)
            {
                doblones = 0;
                BalanceChanged?.Invoke(CurrencyType.Doblones, 0);
            }

            if (gemasDeCalavera != 0)
            {
                gemasDeCalavera = 0;
                BalanceChanged?.Invoke(CurrencyType.GemasDeCalavera, 0);
            }
        }

        private void SetBalance(CurrencyType type, long value)
        {
            switch (type)
            {
                case CurrencyType.Doblones:
                    doblones = value;
                    break;
                case CurrencyType.GemasDeCalavera:
                    gemasDeCalavera = value;
                    break;
            }
        }
    }
}
