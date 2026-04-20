using NUnit.Framework;
using UnityEngine;
using BlacktideRequiem.Core.Economy;

namespace BlacktideRequiem.Tests.EditMode
{
    /// <summary>
    /// Tests for CurrencyWallet — balance integrity, transaction atomicity,
    /// and change notifications. Covers S3-01 acceptance criteria.
    /// </summary>
    [TestFixture]
    public class CurrencyWalletTests
    {
        private CurrencyWallet _wallet;

        [SetUp]
        public void SetUp()
        {
            _wallet = ScriptableObject.CreateInstance<CurrencyWallet>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_wallet);
        }

        // ====================================================================
        // INITIAL STATE
        // ====================================================================

        [Test]
        public void test_wallet_new_wallet_has_zero_balances()
        {
            Assert.AreEqual(0L, _wallet.GetBalance(CurrencyType.Doblones));
            Assert.AreEqual(0L, _wallet.GetBalance(CurrencyType.GemasDeCalavera));
        }

        // ====================================================================
        // ADD
        // ====================================================================

        [Test]
        public void test_wallet_add_positive_amount_increases_balance()
        {
            bool ok = _wallet.Add(CurrencyType.Doblones, 150);

            Assert.IsTrue(ok);
            Assert.AreEqual(150L, _wallet.GetBalance(CurrencyType.Doblones));
        }

        [Test]
        public void test_wallet_add_zero_amount_rejected()
        {
            bool ok = _wallet.Add(CurrencyType.Doblones, 0);

            Assert.IsFalse(ok);
            Assert.AreEqual(0L, _wallet.GetBalance(CurrencyType.Doblones));
        }

        [Test]
        public void test_wallet_add_negative_amount_rejected()
        {
            bool ok = _wallet.Add(CurrencyType.GemasDeCalavera, -50);

            Assert.IsFalse(ok);
            Assert.AreEqual(0L, _wallet.GetBalance(CurrencyType.GemasDeCalavera));
        }

        [Test]
        public void test_wallet_add_accumulates_across_calls()
        {
            _wallet.Add(CurrencyType.Doblones, 100);
            _wallet.Add(CurrencyType.Doblones, 250);

            Assert.AreEqual(350L, _wallet.GetBalance(CurrencyType.Doblones));
        }

        [Test]
        public void test_wallet_add_clamps_at_max_balance()
        {
            _wallet.Add(CurrencyType.Doblones, CurrencyWallet.MaxBalance - 10);

            bool ok = _wallet.Add(CurrencyType.Doblones, 1000);

            Assert.IsTrue(ok);
            Assert.AreEqual(CurrencyWallet.MaxBalance, _wallet.GetBalance(CurrencyType.Doblones));
        }

        // ====================================================================
        // TRY SPEND
        // ====================================================================

        [Test]
        public void test_wallet_try_spend_sufficient_balance_deducts()
        {
            _wallet.Add(CurrencyType.GemasDeCalavera, 500);

            bool ok = _wallet.TrySpend(CurrencyType.GemasDeCalavera, 300);

            Assert.IsTrue(ok);
            Assert.AreEqual(200L, _wallet.GetBalance(CurrencyType.GemasDeCalavera));
        }

        [Test]
        public void test_wallet_try_spend_exact_balance_empties_wallet()
        {
            _wallet.Add(CurrencyType.Doblones, 100);

            bool ok = _wallet.TrySpend(CurrencyType.Doblones, 100);

            Assert.IsTrue(ok);
            Assert.AreEqual(0L, _wallet.GetBalance(CurrencyType.Doblones));
        }

        [Test]
        public void test_wallet_try_spend_insufficient_balance_rejected()
        {
            _wallet.Add(CurrencyType.Doblones, 100);

            bool ok = _wallet.TrySpend(CurrencyType.Doblones, 150);

            Assert.IsFalse(ok);
            Assert.AreEqual(100L, _wallet.GetBalance(CurrencyType.Doblones));
        }

        [Test]
        public void test_wallet_try_spend_zero_amount_rejected()
        {
            _wallet.Add(CurrencyType.Doblones, 100);

            bool ok = _wallet.TrySpend(CurrencyType.Doblones, 0);

            Assert.IsFalse(ok);
            Assert.AreEqual(100L, _wallet.GetBalance(CurrencyType.Doblones));
        }

        [Test]
        public void test_wallet_try_spend_negative_amount_rejected()
        {
            _wallet.Add(CurrencyType.Doblones, 100);

            bool ok = _wallet.TrySpend(CurrencyType.Doblones, -10);

            Assert.IsFalse(ok);
            Assert.AreEqual(100L, _wallet.GetBalance(CurrencyType.Doblones));
        }

        [Test]
        public void test_wallet_try_spend_never_drops_below_zero()
        {
            bool ok = _wallet.TrySpend(CurrencyType.Doblones, 1);

            Assert.IsFalse(ok);
            Assert.AreEqual(0L, _wallet.GetBalance(CurrencyType.Doblones));
        }

        // ====================================================================
        // ISOLATION BETWEEN CURRENCIES
        // ====================================================================

        [Test]
        public void test_wallet_currencies_are_independent()
        {
            _wallet.Add(CurrencyType.Doblones, 500);

            Assert.AreEqual(500L, _wallet.GetBalance(CurrencyType.Doblones));
            Assert.AreEqual(0L, _wallet.GetBalance(CurrencyType.GemasDeCalavera));
        }

        // ====================================================================
        // EVENT NOTIFICATIONS
        // ====================================================================

        [Test]
        public void test_wallet_balance_changed_fires_on_add()
        {
            CurrencyType? capturedType = null;
            long capturedBalance = -1;
            _wallet.BalanceChanged += (t, b) => { capturedType = t; capturedBalance = b; };

            _wallet.Add(CurrencyType.Doblones, 250);

            Assert.AreEqual(CurrencyType.Doblones, capturedType);
            Assert.AreEqual(250L, capturedBalance);
        }

        [Test]
        public void test_wallet_balance_changed_fires_on_successful_spend()
        {
            _wallet.Add(CurrencyType.GemasDeCalavera, 400);
            int callCount = 0;
            long lastBalance = -1;
            _wallet.BalanceChanged += (t, b) => { callCount++; lastBalance = b; };

            _wallet.TrySpend(CurrencyType.GemasDeCalavera, 100);

            Assert.AreEqual(1, callCount);
            Assert.AreEqual(300L, lastBalance);
        }

        [Test]
        public void test_wallet_balance_changed_does_not_fire_on_rejected_add()
        {
            int callCount = 0;
            _wallet.BalanceChanged += (_, _) => callCount++;

            _wallet.Add(CurrencyType.Doblones, 0);
            _wallet.Add(CurrencyType.Doblones, -5);

            Assert.AreEqual(0, callCount);
        }

        [Test]
        public void test_wallet_balance_changed_does_not_fire_on_rejected_spend()
        {
            int callCount = 0;
            _wallet.BalanceChanged += (_, _) => callCount++;

            _wallet.TrySpend(CurrencyType.Doblones, 100);

            Assert.AreEqual(0, callCount);
        }

        // ====================================================================
        // RESET
        // ====================================================================

        [Test]
        public void test_wallet_reset_clears_both_balances()
        {
            _wallet.Add(CurrencyType.Doblones, 500);
            _wallet.Add(CurrencyType.GemasDeCalavera, 300);

            _wallet.ResetBalances();

            Assert.AreEqual(0L, _wallet.GetBalance(CurrencyType.Doblones));
            Assert.AreEqual(0L, _wallet.GetBalance(CurrencyType.GemasDeCalavera));
        }

        [Test]
        public void test_wallet_reset_fires_event_per_non_zero_currency()
        {
            _wallet.Add(CurrencyType.Doblones, 500);
            int callCount = 0;
            _wallet.BalanceChanged += (_, _) => callCount++;

            _wallet.ResetBalances();

            Assert.AreEqual(1, callCount);
        }
    }
}
