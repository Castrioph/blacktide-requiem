namespace BlacktideRequiem.Core.Data
{
    /// <summary>
    /// Unit rarity tier. Affects stat budgets, growth curves, ability pool size,
    /// and gacha pull rates.
    /// </summary>
    public enum Rarity
    {
        /// <summary>3-star. Highest gacha rate, lower stats, 2 awakening tiers.</summary>
        Common,

        /// <summary>4-star. Medium gacha rate, medium stats, 2 awakening tiers.</summary>
        Rare,

        /// <summary>5-star. Lowest gacha rate, highest stats, 3 awakening tiers.</summary>
        Epic
    }
}
