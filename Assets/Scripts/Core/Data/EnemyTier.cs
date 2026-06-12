namespace BlacktideRequiem.Core.Data
{
    /// <summary>
    /// Enemy tier: determines HP budget, AI sophistication and special
    /// mechanics. Normal uses a plain profile, Elite adds Profile+ conditional
    /// overrides, Jefe uses a phase-based behavior tree.
    /// See Enemy System GDD §2.
    /// </summary>
    public enum EnemyTier
    {
        Normal,
        Elite,
        Jefe
    }
}
