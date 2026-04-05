namespace BlacktideRequiem.Core.Data
{
    /// <summary>
    /// Condition types for Limit Break activation on abilities.
    /// See Initiative Bar GDD for LB mechanics.
    /// </summary>
    public enum LBCondition
    {
        OnKill,
        OnCrit,
        OnElementAdvantage,
        OnStatusTarget,
        OnLowHP,
        OnAllyDown
    }
}
