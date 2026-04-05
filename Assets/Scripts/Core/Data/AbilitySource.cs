namespace BlacktideRequiem.Core.Data
{
    /// <summary>
    /// How an ability was acquired by a unit.
    /// </summary>
    public enum AbilitySource
    {
        /// <summary>Part of the unit's base ability pool.</summary>
        Learned,

        /// <summary>Granted by equipped gear.</summary>
        Equipment,

        /// <summary>Unlocked through awakening.</summary>
        Awakening
    }
}
