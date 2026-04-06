using System;
using System.Collections.Generic;
using UnityEngine;
using BlacktideRequiem.Core.Combat;
using BlacktideRequiem.Core.Data;

namespace BlacktideRequiem.Core.AI
{
    /// <summary>
    /// AI decision-maker for enemy combatants. Implements ICombatInput.
    /// Selects target and ability based on the assigned AI profile.
    ///
    /// Enemies ignore MP costs (GDD §Open Questions #3) but respect
    /// cooldowns and Silencio.
    ///
    /// See Enemy System GDD §7 and ADR-003 §4.
    /// </summary>
    public class EnemyAI : ICombatInput
    {
        private readonly AIProfileType _profile;

        public AIProfileType Profile => _profile;

        public EnemyAI(AIProfileType profile)
        {
            _profile = profile;
        }

        /// <summary>
        /// Decides an action for the current combatant and invokes the callback immediately.
        /// </summary>
        public void RequestAction(CombatContext context, Action<CombatAction> callback)
        {
            var action = _profile switch
            {
                AIProfileType.Agresivo => DecideAgresivo(context),
                AIProfileType.Defensivo => DecideDefensivo(context),
                AIProfileType.Caotico => DecideCaotico(context),
                _ => DecideAgresivo(context)
            };
            callback(action);
        }

        // ====================================================================
        // AGRESIVO — lowest HP target, highest damage ability
        // ====================================================================

        private CombatAction DecideAgresivo(CombatContext context)
        {
            var target = FindLowestHP(context.Enemies);
            if (target == null) return CombatAction.PassTurn();

            var ability = FindHighestDamageAbility(context.Actor);
            if (ability != null)
                return CombatAction.FromAbility(ability, target);

            return CombatAction.BasicAttack(target, IsPhysicalAttacker(context.Actor));
        }

        // ====================================================================
        // DEFENSIVO — buff self if no buffs, else attack
        // ====================================================================

        private CombatAction DecideDefensivo(CombatContext context)
        {
            var actor = context.Actor;

            // If no buffs active, try to use a buff/defensive ability
            if (actor.Buffs.All.Count == 0)
            {
                var buffAbility = FindBuffAbility(actor);
                if (buffAbility != null)
                {
                    // Target self or lowest HP ally
                    var buffTarget = FindLowestHP(context.Allies) ?? actor;
                    return CombatAction.FromAbility(buffAbility, buffTarget);
                }

                // No buff ability available — use Guard as fallback
                return CombatAction.Guard();
            }

            // Has buffs — attack
            var target = FindLowestHP(context.Enemies);
            if (target == null) return CombatAction.PassTurn();

            var damageAbility = FindHighestDamageAbility(actor);
            if (damageAbility != null)
                return CombatAction.FromAbility(damageAbility, target);

            return CombatAction.BasicAttack(target, IsPhysicalAttacker(actor));
        }

        // ====================================================================
        // CAOTICO — random everything
        // ====================================================================

        private CombatAction DecideCaotico(CombatContext context)
        {
            if (context.Enemies.Count == 0) return CombatAction.PassTurn();

            var target = context.Enemies[UnityEngine.Random.Range(0, context.Enemies.Count)];
            var abilities = GetAvailableAbilities(context.Actor);

            if (abilities.Count > 0)
            {
                var ability = abilities[UnityEngine.Random.Range(0, abilities.Count)];
                return CombatAction.FromAbility(ability, target);
            }

            return CombatAction.BasicAttack(target, IsPhysicalAttacker(context.Actor));
        }

        // ====================================================================
        // HELPER METHODS
        // ====================================================================

        /// <summary>Finds the combatant with the lowest current HP.</summary>
        private static CombatantState FindLowestHP(List<CombatantState> combatants)
        {
            if (combatants == null || combatants.Count == 0) return null;

            CombatantState lowest = combatants[0];
            for (int i = 1; i < combatants.Count; i++)
            {
                if (combatants[i].CurrentHP < lowest.CurrentHP)
                    lowest = combatants[i];
            }
            return lowest;
        }

        /// <summary>
        /// Gets all damage abilities ready to use (off cooldown, not silenced).
        /// Enemies ignore MP costs per GDD.
        /// </summary>
        private static List<AbilityData> GetAvailableAbilities(CombatantState actor)
        {
            var available = new List<AbilityData>();
            if (actor.HasStatus(StatusEffect.Silencio)) return available;

            foreach (var entry in actor.Template.LandAbilities)
            {
                if (entry.Ability == null) continue;
                if (entry.UnlockLevel > actor.Level) continue;
                if (actor.GetCooldownRemaining(entry.Ability) > 0) continue;
                available.Add(entry.Ability);
            }
            return available;
        }

        /// <summary>Finds the highest AbilityPower damage ability available.</summary>
        private static AbilityData FindHighestDamageAbility(CombatantState actor)
        {
            var abilities = GetAvailableAbilities(actor);
            AbilityData best = null;
            float bestPower = 0f;

            foreach (var ability in abilities)
            {
                if (ability.Category != AbilityCategory.Damage) continue;
                if (ability.AbilityPower > bestPower)
                {
                    best = ability;
                    bestPower = ability.AbilityPower;
                }
            }
            return best;
        }

        /// <summary>Finds a buff-category ability available for use.</summary>
        private static AbilityData FindBuffAbility(CombatantState actor)
        {
            var abilities = GetAvailableAbilities(actor);
            foreach (var ability in abilities)
            {
                if (ability.Category == AbilityCategory.Buff)
                    return ability;
            }
            return null;
        }

        /// <summary>Determines if this combatant should use physical or magical basic attacks.</summary>
        private static bool IsPhysicalAttacker(CombatantState actor)
        {
            return actor.GetEffectiveStat(StatType.ATK) >= actor.GetEffectiveStat(StatType.MST);
        }
    }
}
