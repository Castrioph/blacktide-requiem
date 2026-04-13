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

        /// <summary>
        /// Index into Allies designating the primary Captain (default 0).
        /// Only slots 0-4 valid (slot 5 = guest cannot be primary Captain).
        /// See Traits/Sinergias GDD §2.
        /// </summary>
        public int CaptainIndex;

        /// <summary>
        /// Whether the guest unit (last ally) is a friend unit (second Captain).
        /// If false, guest counts toward thresholds but does not initiate synergies.
        /// </summary>
        public bool IsGuestFriend;
    }

    /// <summary>
    /// Configuration for a single enemy wave within a battle.
    /// See Combate Terrestre GDD §7 (Oleadas).
    /// </summary>
    public class WaveConfig
    {
        /// <summary>Enemy combatants in this wave (max 5).</summary>
        public List<InitiativeEntry> Enemies;

        /// <summary>
        /// Index into Enemies designating the enemy captain (-1 = no captain).
        /// Killing the enemy captain deactivates all enemy synergies.
        /// See Traits/Sinergias GDD §4.
        /// </summary>
        public int EnemyCaptainIndex = -1;
    }
}
