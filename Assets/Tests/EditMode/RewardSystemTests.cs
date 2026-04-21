using System;
using NUnit.Framework;
using UnityEngine;
using BlacktideRequiem.Core.Economy;
using BlacktideRequiem.Core.Events;

namespace BlacktideRequiem.Tests.EditMode
{
    /// <summary>
    /// Tests for RewardTable SO and RewardDispatcher. Covers S3-03 acceptance criteria.
    /// </summary>
    [TestFixture]
    public class RewardSystemTests
    {
        private CurrencyWallet _wallet;
        private RewardTable _table;

        [SetUp]
        public void SetUp()
        {
            _wallet = ScriptableObject.CreateInstance<CurrencyWallet>();
            _table = ScriptableObject.CreateInstance<RewardTable>();
            GameEvents.ClearAll();
        }

        [TearDown]
        public void TearDown()
        {
            GameEvents.ClearAll();
            UnityEngine.Object.DestroyImmediate(_wallet);
            UnityEngine.Object.DestroyImmediate(_table);
        }

        // ====================================================================
        // REWARD TABLE
        // ====================================================================

        [Test]
        public void test_rewardtable_payout_singleentry_addsCorrectAmount()
        {
            // Arrange
            _table.Entries.Add(new RewardEntry { Currency = CurrencyType.Doblones, Amount = 500 });

            // Act
            _table.Payout(_wallet);

            // Assert
            Assert.AreEqual(500L, _wallet.GetBalance(CurrencyType.Doblones));
        }

        [Test]
        public void test_rewardtable_payout_multipleEntries_allCurrenciesPaid()
        {
            // Arrange
            _table.Entries.Add(new RewardEntry { Currency = CurrencyType.Doblones, Amount = 300 });
            _table.Entries.Add(new RewardEntry { Currency = CurrencyType.GemasDeCalavera, Amount = 10 });

            // Act
            _table.Payout(_wallet);

            // Assert
            Assert.AreEqual(300L, _wallet.GetBalance(CurrencyType.Doblones));
            Assert.AreEqual(10L, _wallet.GetBalance(CurrencyType.GemasDeCalavera));
        }

        [Test]
        public void test_rewardtable_payout_emptyTable_walletUnchanged()
        {
            // Arrange — empty table

            // Act
            _table.Payout(_wallet);

            // Assert
            Assert.AreEqual(0L, _wallet.GetBalance(CurrencyType.Doblones));
            Assert.AreEqual(0L, _wallet.GetBalance(CurrencyType.GemasDeCalavera));
        }

        [Test]
        public void test_rewardtable_payout_zeroAmount_walletUnchanged()
        {
            // Arrange
            _table.Entries.Add(new RewardEntry { Currency = CurrencyType.Doblones, Amount = 0 });

            // Act
            _table.Payout(_wallet);

            // Assert
            Assert.AreEqual(0L, _wallet.GetBalance(CurrencyType.Doblones));
        }

        [Test]
        public void test_rewardtable_payout_nullWallet_throws()
        {
            // Arrange
            _table.Entries.Add(new RewardEntry { Currency = CurrencyType.Doblones, Amount = 100 });

            // Act / Assert
            Assert.Throws<ArgumentNullException>(() => _table.Payout(null));
        }

        [Test]
        public void test_rewardtable_payout_sameCurrencyMultipleEntries_accumulatesBalance()
        {
            // Arrange
            _table.Entries.Add(new RewardEntry { Currency = CurrencyType.Doblones, Amount = 200 });
            _table.Entries.Add(new RewardEntry { Currency = CurrencyType.Doblones, Amount = 300 });

            // Act
            _table.Payout(_wallet);

            // Assert
            Assert.AreEqual(500L, _wallet.GetBalance(CurrencyType.Doblones));
        }

        // ====================================================================
        // REWARD DISPATCHER
        // ====================================================================

        [Test]
        public void test_rewarddispatcher_victory_payoutsToWallet()
        {
            // Arrange
            _table.Entries.Add(new RewardEntry { Currency = CurrencyType.Doblones, Amount = 1000 });
            var dispatcher = new RewardDispatcher(_table, _wallet);
            dispatcher.Connect();

            // Act
            GameEvents.PublishBattleEnd(new BattleEndEvent { Result = BattleResult.Victory, RoundsElapsed = 3 });

            // Assert
            Assert.AreEqual(1000L, _wallet.GetBalance(CurrencyType.Doblones));

            dispatcher.Disconnect();
        }

        [Test]
        public void test_rewarddispatcher_defeat_noPayoutToWallet()
        {
            // Arrange
            _table.Entries.Add(new RewardEntry { Currency = CurrencyType.Doblones, Amount = 1000 });
            var dispatcher = new RewardDispatcher(_table, _wallet);
            dispatcher.Connect();

            // Act
            GameEvents.PublishBattleEnd(new BattleEndEvent { Result = BattleResult.Defeat, RoundsElapsed = 2 });

            // Assert
            Assert.AreEqual(0L, _wallet.GetBalance(CurrencyType.Doblones));

            dispatcher.Disconnect();
        }

        [Test]
        public void test_rewarddispatcher_afterDisconnect_victoryDoesNotPayout()
        {
            // Arrange
            _table.Entries.Add(new RewardEntry { Currency = CurrencyType.Doblones, Amount = 1000 });
            var dispatcher = new RewardDispatcher(_table, _wallet);
            dispatcher.Connect();
            dispatcher.Disconnect();

            // Act
            GameEvents.PublishBattleEnd(new BattleEndEvent { Result = BattleResult.Victory, RoundsElapsed = 1 });

            // Assert
            Assert.AreEqual(0L, _wallet.GetBalance(CurrencyType.Doblones));
        }

        [Test]
        public void test_rewarddispatcher_nullTable_throwsOnConstruct()
        {
            // Act / Assert
            Assert.Throws<ArgumentNullException>(() => new RewardDispatcher(null, _wallet));
        }

        [Test]
        public void test_rewarddispatcher_nullWallet_throwsOnConstruct()
        {
            // Act / Assert
            Assert.Throws<ArgumentNullException>(() => new RewardDispatcher(_table, null));
        }
    }
}
