namespace BlacktideRequiem.Core.Combat
{
    /// <summary>
    /// Battle state machine phases.
    /// See Combate Terrestre GDD §States and ADR-003.
    /// </summary>
    public enum BattlePhase
    {
        /// <summary>Not yet started.</summary>
        None,

        /// <summary>Loading data, deploying units, calculating synergies.</summary>
        PreCombat,

        /// <summary>Processing turns sequentially per Initiative Bar.</summary>
        InRound,

        /// <summary>Wave cleared, deploying next wave.</summary>
        WaveTransition,

        /// <summary>All enemies eliminated across all waves.</summary>
        Victory,

        /// <summary>All allies eliminated.</summary>
        Defeat
    }
}
