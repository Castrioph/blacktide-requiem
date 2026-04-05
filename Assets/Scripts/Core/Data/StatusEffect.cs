namespace BlacktideRequiem.Core.Data
{
    /// <summary>
    /// Combat status effects. See Damage & Stats Engine GDD §6.
    /// </summary>
    public enum StatusEffect
    {
        /// <summary>DoT: X% max HP at end of turn. Always ticks, even if CC'd.</summary>
        Veneno,

        /// <summary>DoT: X% max HP at start of turn (before acting). Can kill before action.</summary>
        Sangrado,

        /// <summary>DoT: X% max HP after performing an action. Skipped if CC'd or turn passed.</summary>
        Quemadura,

        /// <summary>CC: Loses next turn completely. 1 turn duration.</summary>
        Aturdimiento,

        /// <summary>CC: Loses turns until receiving damage (damage wakes). 2 turn duration.</summary>
        Sueno,

        /// <summary>Debuff: Physical attacks have 50% miss chance. Magical unaffected.</summary>
        Ceguera,

        /// <summary>Debuff: Cannot use abilities (basic attack only).</summary>
        Silencio,

        /// <summary>Threshold: Instant kill if target HP below X%. No effect on bosses.</summary>
        Muerte
    }
}
