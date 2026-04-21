using System;
using System.Collections.Generic;
using UnityEngine;

namespace BlacktideRequiem.Core.Economy
{
    [CreateAssetMenu(fileName = "reward_", menuName = "Blacktide/Reward Table")]
    public class RewardTable : ScriptableObject
    {
        public List<RewardEntry> Entries = new List<RewardEntry>();

        /// <summary>Pays all entries into the wallet. Skips entries with zero or negative amounts.</summary>
        public void Payout(CurrencyWallet wallet)
        {
            if (wallet == null) throw new ArgumentNullException(nameof(wallet));
            foreach (var entry in Entries)
                wallet.Add(entry.Currency, entry.Amount);
        }
    }

    [Serializable]
    public class RewardEntry
    {
        public CurrencyType Currency;
        public long Amount;
    }
}
