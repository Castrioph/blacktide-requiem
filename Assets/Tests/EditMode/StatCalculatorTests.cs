using NUnit.Framework;
using BlacktideRequiem.Core.Data;
using BlacktideRequiem.Core.Stats;

namespace BlacktideRequiem.Tests
{
    /// <summary>
    /// Tests for the piecewise stat growth formula against the GDD worked examples.
    /// See design/gdd/unit-data-model.md § Formulas.
    /// </summary>
    public class StatCalculatorTests
    {
        // --- GDD Worked Example: 3★ unit at Lv 60 (fully awakened, MaxLevel raised to 60) ---

        [Test]
        public void ThreeStar_HP_AtLevel60_MatchesGDD()
        {
            // Base=600, Growth=28, MaxLevel=60
            // Threshold = floor(0.80 * 60) = 48
            // Normal: 600 + floor(28 * 47) = 600 + 1316 = 1916
            // Accelerated: floor(28 * 1.20) * 12 = 33 * 12 = 396
            // Total without awakening: 2312
            int result = StatCalculator.CalculateStat(600f, 28f, 60, 60);
            Assert.AreEqual(2312, result);
        }

        [Test]
        public void ThreeStar_HP_AtLevel60_WithAwakening_MatchesGDD()
        {
            // GDD total: 2432 = 2312 (level) + 120 (awakening: 50 + 70)
            int levelStat = StatCalculator.CalculateStat(600f, 28f, 60, 60);
            int awakeningBonus = 50 + 70; // 1st + 2nd tier HP bonuses for 3★
            Assert.AreEqual(2432, levelStat + awakeningBonus);
        }

        [Test]
        public void ThreeStar_ATK_AtLevel60_MatchesGDD()
        {
            // Base=60, Growth=3.5, MaxLevel=60
            // Threshold=48
            // Normal: 60 + floor(3.5 * 47) = 60 + 164 = 224
            // Accelerated: floor(3.5 * 1.20) * 12 = floor(4.2) * 12 = 4 * 12 = 48
            // Total without awakening: 272
            int result = StatCalculator.CalculateStat(60f, 3.5f, 60, 60);
            Assert.AreEqual(272, result);
        }

        [Test]
        public void ThreeStar_ATK_AtLevel60_WithAwakening_MatchesGDD()
        {
            // GDD total: 284 = 272 (level) + 12 (awakening: 5 + 7)
            int levelStat = StatCalculator.CalculateStat(60f, 3.5f, 60, 60);
            int awakeningBonus = 5 + 7;
            Assert.AreEqual(284, levelStat + awakeningBonus);
        }

        // --- GDD Worked Example: 5★ unit at Lv 90 (fully awakened, MaxLevel raised to 90) ---

        [Test]
        public void FiveStar_HP_AtLevel90_MatchesGDD()
        {
            // Base=1400, Growth=60, MaxLevel=90
            // Threshold = floor(0.80 * 90) = 72
            // Normal: 1400 + floor(60 * 71) = 1400 + 4260 = 5660
            // Accelerated: floor(60 * 1.20) * 18 = 72 * 18 = 1296
            // Total without awakening: 6956
            int result = StatCalculator.CalculateStat(1400f, 60f, 90, 90);
            Assert.AreEqual(6956, result);
        }

        [Test]
        public void FiveStar_HP_AtLevel90_WithAwakening_MatchesGDD()
        {
            // GDD total: 7436 = 6956 (level) + 480 (awakening: 120 + 160 + 200)
            int levelStat = StatCalculator.CalculateStat(1400f, 60f, 90, 90);
            int awakeningBonus = 120 + 160 + 200;
            Assert.AreEqual(7436, levelStat + awakeningBonus);
        }

        [Test]
        public void FiveStar_ATK_AtLevel90_MatchesGDD()
        {
            // Base=140, Growth=8, MaxLevel=90
            // Threshold=72
            // Normal: 140 + floor(8 * 71) = 140 + 568 = 708
            // Accelerated: floor(8 * 1.20) * 18 = floor(9.6) * 18 = 9 * 18 = 162
            // Total without awakening: 870
            int result = StatCalculator.CalculateStat(140f, 8f, 90, 90);
            Assert.AreEqual(870, result);
        }

        [Test]
        public void FiveStar_ATK_AtLevel90_WithAwakening_MatchesGDD()
        {
            // GDD total: 918 = 870 (level) + 48 (awakening: 12 + 16 + 20)
            int levelStat = StatCalculator.CalculateStat(140f, 8f, 90, 90);
            int awakeningBonus = 12 + 16 + 20;
            Assert.AreEqual(918, levelStat + awakeningBonus);
        }

        // --- Boundary cases ---

        [Test]
        public void Level1_ReturnsBaseStat()
        {
            // At level 1, growth contribution is 0
            int result = StatCalculator.CalculateStat(600f, 28f, 1, 60);
            Assert.AreEqual(600, result);
        }

        [Test]
        public void AtThreshold_UsesNormalGrowthOnly()
        {
            // Level exactly at threshold (48 for MaxLevel 60)
            // Should use only normal growth: 600 + floor(28 * 47) = 600 + 1316 = 1916
            int result = StatCalculator.CalculateStat(600f, 28f, 48, 60);
            Assert.AreEqual(1916, result);
        }

        [Test]
        public void OneAboveThreshold_UsesAcceleratedGrowth()
        {
            // Level 49 (threshold+1): normal through 48, then 1 accelerated level
            // Normal: 600 + floor(28 * 47) = 1916
            // Accelerated: floor(28 * 1.20) * 1 = 33
            // Total: 1949
            int result = StatCalculator.CalculateStat(600f, 28f, 49, 60);
            Assert.AreEqual(1949, result);
        }

        // --- CalculateAllStats ---

        [Test]
        public void CalculateAllStats_ProcessesAllSevenStats()
        {
            var baseStats = new StatBlock { HP = 600, MP = 100, ATK = 60, DEF = 50, MST = 60, SPR = 45, SPD = 55 };
            var growth = new StatBlock { HP = 28, MP = 4, ATK = 3.5f, DEF = 3, MST = 3.5f, SPR = 2.5f, SPD = 1 };

            StatBlock result = StatCalculator.CalculateAllStats(baseStats, growth, 1, 40);

            Assert.AreEqual(600, result.HP);
            Assert.AreEqual(100, result.MP);
            Assert.AreEqual(60, result.ATK);
            Assert.AreEqual(50, result.DEF);
            Assert.AreEqual(60, result.MST);
            Assert.AreEqual(45, result.SPR);
            Assert.AreEqual(55, result.SPD);
        }

        // --- Secondary stats ---

        [Test]
        public void SecondaryStats_CRI_ScalesLinearly()
        {
            var baseStats = new SecondaryStatBlock { CRI = 5f, LCK = 10f };
            var growth = new SecondaryStatBlock { CRI = 0.12f, LCK = 0.5f };

            SecondaryStatBlock result = StatCalculator.CalculateSecondaryStats(baseStats, growth, 40);

            Assert.AreEqual(5f + 0.12f * 39, result.CRI, 0.001f);
        }

        [Test]
        public void SecondaryStats_LCK_ClampsAt100()
        {
            var baseStats = new SecondaryStatBlock { CRI = 5f, LCK = 90f };
            var growth = new SecondaryStatBlock { CRI = 0.12f, LCK = 2f };

            // LCK would be 90 + 2*39 = 168, but capped at 100
            SecondaryStatBlock result = StatCalculator.CalculateSecondaryStats(baseStats, growth, 40);

            Assert.AreEqual(100f, result.LCK, 0.001f);
        }

        // --- FinalStats with awakening ---

        [Test]
        public void CalculateFinalStats_AddsAwakeningBonuses()
        {
            var baseStats = new StatBlock { HP = 600, ATK = 60 };
            var growth = new StatBlock { HP = 28, ATK = 3.5f };
            var awakening = new StatBlock { HP = 120, ATK = 12 };

            StatBlock result = StatCalculator.CalculateFinalStats(baseStats, growth, 60, 60, awakening);

            Assert.AreEqual(2312 + 120, result.HP);
            Assert.AreEqual(272 + 12, result.ATK);
        }
    }
}
