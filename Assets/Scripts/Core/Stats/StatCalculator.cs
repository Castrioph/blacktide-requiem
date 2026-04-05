using UnityEngine;
using BlacktideRequiem.Core.Data;

namespace BlacktideRequiem.Core.Stats
{
    /// <summary>
    /// Pure calculation class for stat growth. No singletons, no MonoBehaviour.
    /// Implements the piecewise growth formula from the Unit Data Model GDD.
    ///
    /// Formula:
    ///   Threshold = floor(0.80 * MaxLevel)
    ///   If L &lt;= Threshold: Stat(L) = Base + floor(Growth * (L - 1))
    ///   If L &gt; Threshold:  Stat(L) = Base + floor(Growth * (Threshold - 1)) + floor(Growth * 1.20 * (L - Threshold))
    /// </summary>
    public static class StatCalculator
    {
        /// <summary>
        /// Percentage of MaxLevel at which stat growth acceleration begins.
        /// </summary>
        public const float GROWTH_THRESHOLD_PERCENT = 0.80f;

        /// <summary>
        /// Multiplier applied to growth rate above the threshold.
        /// </summary>
        public const float GROWTH_ACCELERATOR = 1.20f;

        /// <summary>
        /// Calculates a single core stat at a given level using the piecewise growth formula.
        /// </summary>
        /// <param name="baseStat">Base stat value at level 1.</param>
        /// <param name="growth">Per-level growth rate.</param>
        /// <param name="level">Current level (1-based).</param>
        /// <param name="maxLevel">Maximum level cap (determines acceleration threshold).</param>
        /// <returns>The calculated stat value (floored to int).</returns>
        public static int CalculateStat(float baseStat, float growth, int level, int maxLevel)
        {
            int threshold = Mathf.FloorToInt(GROWTH_THRESHOLD_PERCENT * maxLevel);

            if (level <= threshold)
            {
                return Mathf.FloorToInt(baseStat + growth * (level - 1));
            }

            float normalGrowth = Mathf.FloorToInt(growth * (threshold - 1));
            float acceleratedGrowth = Mathf.FloorToInt(growth * GROWTH_ACCELERATOR) * (level - threshold);
            return Mathf.FloorToInt(baseStat + normalGrowth + acceleratedGrowth);
        }

        /// <summary>
        /// Calculates all 7 core stats at a given level.
        /// </summary>
        public static StatBlock CalculateAllStats(StatBlock baseStats, StatBlock growth, int level, int maxLevel)
        {
            var result = new StatBlock();
            for (int i = 0; i < StatBlock.COUNT; i++)
            {
                result[i] = CalculateStat(baseStats[i], growth[i], level, maxLevel);
            }
            return result;
        }

        /// <summary>
        /// Calculates final stats including awakening flat bonuses.
        /// </summary>
        /// <param name="baseStats">Base stat values at level 1.</param>
        /// <param name="growth">Per-level growth rates.</param>
        /// <param name="level">Current level.</param>
        /// <param name="maxLevel">Current max level (after awakening cap raises).</param>
        /// <param name="awakeningBonuses">Sum of all awakening tier stat bonuses.</param>
        /// <returns>Final stat block with awakening bonuses applied.</returns>
        public static StatBlock CalculateFinalStats(
            StatBlock baseStats,
            StatBlock growth,
            int level,
            int maxLevel,
            StatBlock awakeningBonuses)
        {
            StatBlock levelStats = CalculateAllStats(baseStats, growth, level, maxLevel);
            for (int i = 0; i < StatBlock.COUNT; i++)
            {
                levelStats[i] += awakeningBonuses[i];
            }
            return levelStats;
        }

        /// <summary>
        /// Calculates secondary stats at a given level.
        /// </summary>
        public static SecondaryStatBlock CalculateSecondaryStats(
            SecondaryStatBlock baseStats,
            SecondaryStatBlock growth,
            int level)
        {
            return new SecondaryStatBlock
            {
                CRI = baseStats.CRI + growth.CRI * (level - 1),
                LCK = Mathf.Min(baseStats.LCK + growth.LCK * (level - 1), 100f)
            };
        }
    }
}
