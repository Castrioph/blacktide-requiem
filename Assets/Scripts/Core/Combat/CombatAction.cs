using System.Collections.Generic;
using BlacktideRequiem.Core.Data;

namespace BlacktideRequiem.Core.Combat
{
    // NOTE: TargetType is used by both CombatAction and AbilityData (Core.Data).
    // Kept here for now; consider moving to Core.Data if more data types need it.
    /// <summary>
    /// Types of actions a combatant can take on their turn.
    /// See Combate Terrestre GDD §3.
    /// </summary>
    public enum ActionType
    {
        Attack,
        Ability,
        Guard,
        Pass
    }

    /// <summary>
    /// Target selection modes for abilities.
    /// See Combate Terrestre GDD §3.
    /// </summary>
    public enum TargetType
    {
        SingleEnemy,
        AoeEnemy,
        Self,
        SingleAlly,
        AllyAoe
    }

    /// <summary>
    /// A fully resolved action chosen by a player or AI.
    /// CombatManager consumes this to resolve the turn.
    /// See ADR-003 §4 (Input Abstraction).
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

        /// <summary>Primary target. Null for AoE, self-target, Guard, Pass.</summary>
        public CombatantState Target;

        /// <summary>Display name for the action (e.g., "Rayo", "Attack").</summary>
        public string ActionName;

        /// <summary>Reference to the AbilityEntry if this is an ability action.</summary>
        public AbilityEntry? AbilityEntry;

        /// <summary>Reference to the AbilityData asset (null for Attack/Guard/Pass).</summary>
        public AbilityData AbilityData;

        /// <summary>Creates an ability action from an AbilityData asset.</summary>
        public static CombatAction FromAbility(AbilityData ability, CombatantState target,
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
        public static CombatAction BasicAttack(CombatantState target, bool isPhysical)
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
    }

    /// <summary>
    /// Context provided to ICombatInput so it can make informed decisions.
    /// See ADR-003 §4.
    /// </summary>
    public struct CombatContext
    {
        /// <summary>The combatant whose turn it is.</summary>
        public CombatantState Actor;

        /// <summary>All living allies.</summary>
        public List<CombatantState> Allies;

        /// <summary>All living enemies.</summary>
        public List<CombatantState> Enemies;
    }
}
