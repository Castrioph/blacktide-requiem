namespace BlacktideRequiem.Core.AI
{
    /// <summary>
    /// AI behavior profile for Normal/Elite enemies.
    /// Determines targeting priority and ability selection.
    /// See Enemy System GDD §7.
    /// </summary>
    public enum AIProfileType
    {
        /// <summary>Targets lowest HP enemy. Uses highest damage ability.</summary>
        Agresivo,

        /// <summary>Targets self or lowest HP ally. Buffs DEF/SPR if no buffs, else attacks.</summary>
        Defensivo,

        /// <summary>Random target, random ability.</summary>
        Caotico
    }
}
