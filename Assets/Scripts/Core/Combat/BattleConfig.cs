using System;
using System.Collections.Generic;

namespace BlacktideRequiem.Core.Combat
{
    /// <summary>
    /// Configuration for a single battle. Created from Stage System data.
    /// Defines ally lineup and enemy waves.
    /// See Combate Terrestre GDD §1 and ADR-003.
    /// </summary>
    public class BattleConfig
    {
        /// <summary>Ally combatant entries (max 6: 5 own + 1 friend).</summary>
        public List<InitiativeEntry> Allies;

        /// <summary>Enemy waves. Each wave is a list of combatant entries.</summary>
        public List<WaveConfig> Waves;
    }

    /// <summary>
    /// Configuration for a single enemy wave within a battle.
    /// See Combate Terrestre GDD §7 (Oleadas).
    /// </summary>
    public class WaveConfig
    {
        /// <summary>Enemy combatants in this wave (max 5).</summary>
        public List<InitiativeEntry> Enemies;
    }
}
