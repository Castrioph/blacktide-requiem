using System;
using BlacktideRequiem.Core.Events;

namespace BlacktideRequiem.Core.Economy
{
    /// <summary>
    /// Listens for battle-end events and pays out a RewardTable into a CurrencyWallet on Victory.
    /// Pure C# — wire up via Connect/Disconnect around a combat session.
    /// </summary>
    public class RewardDispatcher
    {
        private readonly RewardTable _table;
        private readonly CurrencyWallet _wallet;

        public RewardDispatcher(RewardTable table, CurrencyWallet wallet)
        {
            _table = table ?? throw new ArgumentNullException(nameof(table));
            _wallet = wallet ?? throw new ArgumentNullException(nameof(wallet));
        }

        public void Connect() => GameEvents.OnBattleEnd += OnBattleEnd;
        public void Disconnect() => GameEvents.OnBattleEnd -= OnBattleEnd;

        private void OnBattleEnd(BattleEndEvent e)
        {
            if (e.Result == BattleResult.Victory)
                _table.Payout(_wallet);
        }
    }
}
