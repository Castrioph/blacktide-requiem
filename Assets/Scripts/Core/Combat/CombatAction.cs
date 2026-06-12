using System.Collections.Generic;
using BlacktideRequiem.Core.Data;

namespace BlacktideRequiem.Core.Combat
{
    // NOTE: TargetType is used by both CombatAction and AbilityData (Core.Data).
    // Kept here for now; consider moving to Core.Data if more data types need it.
    /// <summary>
    /// Types of actions a combatant can take on their turn.
    /// Land: Attack/Ability/Guard/Pass (Combate Terrestre GDD §3).
    /// Naval adds Maneuver/Boarding/Repair; Cañonazo reuses Attack
    /// (Combate Naval GDD §3, ADR-004 §3).
    /// </summary>
    public enum ActionType
    {
        Attack,
        Ability,
        Guard,
        Pass,
        Maneuver,
        Boarding,
        Repair
    }

    /// <summary>
    /// Target selection modes for abilities.
    /// SingleCrewEnemy is naval-only: targets one crew member of an enemy ship
    /// (Abordaje and "boarding-like" abilities). See Combate Naval GDD §3.
    /// </summary>
    public enum TargetType
    {
        SingleEnemy,
        AoeEnemy,
        Self,
        SingleAlly,
        AllyAoe,
        SingleCrewEnemy
    }

    /// <summary>
    /// A fully resolved action chosen by a player or AI.
    /// CombatManager consumes this to resolve the turn.
    /// See ADR-003 §4 (Input Abstraction) and ADR-004 §3.
    /// </summary>
    public struct CombatAction
    {
        /// <summary>What type of action is being taken.</summary>
        public ActionType Type;

        /// <summary>Ability power multiplier (1.0 for basic attack).</summary>
        public float AbilityPower;

        /// <summary>Offensive element (Neutral for basic attack).</summary>
        public Element Element;

        /// <summary>True for physical (ATK/DEF), false for magical (MST/SPR).</summary>
        public bool IsPhysical;

        /// <summary>How the ability selects targets.</summary>
        public TargetType TargetType;

        /// <summary>Primary target (land unit or ship). Null for AoE, self-target, Guard, Pass.</summary>
        public ICombatant Target;

        /// <summary>Crew member target. Only set for Boarding / SingleCrewEnemy
        /// abilities; Target then holds the crew member's ship.</summary>
        public CrewMemberState TargetCrew;

        /// <summary>Display name for the action (e.g., "Rayo", "Attack").</summary>
        public string ActionName;

        /// <summary>Reference to the AbilityEntry if this is an ability action.</summary>
        public AbilityEntry? AbilityEntry;

        /// <summary>Reference to the AbilityData asset (null for Attack/Guard/Pass).</summary>
        public AbilityData AbilityData;

        /// <summary>Creates an ability action from an AbilityData asset.</summary>
        public static CombatAction FromAbility(AbilityData ability, ICombatant target,
            AbilityEntry? entry = null)
        {
            return new CombatAction
            {
                Type = ActionType.Ability,
                AbilityPower = ability.AbilityPower,
                Element = ability.Element,
                IsPhysical = ability.IsPhysical,
                TargetType = ability.TargetType,
                Target = target,
                ActionName = ability.DisplayName,
                AbilityEntry = entry,
                AbilityData = ability
            };
        }

        /// <summary>Creates a basic attack action targeting a single enemy.</summary>
        public static CombatAction BasicAttack(ICombatant target, bool isPhysical)
        {
            return new CombatAction
            {
                Type = ActionType.Attack,
                AbilityPower = 1.0f,
                Element = Element.Neutral,
                IsPhysical = isPhysical,
                TargetType = TargetType.SingleEnemy,
                Target = target,
                ActionName = "Attack",
                AbilityEntry = null
            };
        }

        /// <summary>Creates a guard action.</summary>
        public static CombatAction Guard()
        {
            return new CombatAction
            {
                Type = ActionType.Guard,
                ActionName = "Guard"
            };
        }

        /// <summary>Creates a pass action.</summary>
        public static CombatAction PassTurn()
        {
            return new CombatAction
            {
                Type = ActionType.Pass,
                ActionName = "Pass"
            };
        }

        // ====================================================================
        // NAVAL FACTORIES (Combate Naval GDD §3)
        // ====================================================================

        /// <summary>Cañonazo: free physical hull attack, FPW vs HDF, Neutral, power 1.0.</summary>
        public static CombatAction Cannonball(ICombatant target)
        {
            return new CombatAction
            {
                Type = ActionType.Attack,
                AbilityPower = 1.0f,
                Element = Element.Neutral,
                IsPhysical = true,
                TargetType = TargetType.SingleEnemy,
                Target = target,
                ActionName = "Cañonazo"
            };
        }

        /// <summary>Maniobra Evasiva: halves incoming hull AND crew damage until
        /// the ship's next turn. Naval equivalent of Guard.</summary>
        public static CombatAction Maneuver()
        {
            return new CombatAction
            {
                Type = ActionType.Maneuver,
                ActionName = "Maniobra Evasiva"
            };
        }

        /// <summary>Abordaje: attacks one crew member of an enemy ship.
        /// Ship FPW vs the crew unit's DEF. Not possible against creatures.</summary>
        public static CombatAction Boarding(ICombatant targetShip, CrewMemberState targetCrew)
        {
            return new CombatAction
            {
                Type = ActionType.Boarding,
                AbilityPower = 1.0f,
                Element = Element.Neutral,
                IsPhysical = true,
                TargetType = TargetType.SingleCrewEnemy,
                Target = targetShip,
                TargetCrew = targetCrew,
                ActionName = "Abordaje"
            };
        }

        /// <summary>Reparar: heals hull for MST × REPAIR_POWER at a fixed MP cost.
        /// Base action, NOT an ability — works under Silencio (GDD edge case 14).</summary>
        public static CombatAction Repair()
        {
            return new CombatAction
            {
                Type = ActionType.Repair,
                ActionName = "Reparar"
            };
        }
    }

    /// <summary>
    /// Context provided to ICombatInput so it can make informed decisions.
    /// Lists hold ICombatant (land units or ships); land-only consumers
    /// (EnemyAI, land UI) downcast to CombatantState. See ADR-003 §4 / ADR-004.
    /// </summary>
    public struct CombatContext
    {
        /// <summary>The combatant whose turn it is.</summary>
        public ICombatant Actor;

        /// <summary>All living allies.</summary>
        public List<ICombatant> Allies;

        /// <summary>All living enemies.</summary>
        public List<ICombatant> Enemies;
    }
}
