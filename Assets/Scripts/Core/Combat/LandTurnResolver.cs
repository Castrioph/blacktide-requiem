using UnityEngine;
using BlacktideRequiem.Core.Data;
using BlacktideRequiem.Core.Events;

namespace BlacktideRequiem.Core.Combat
{
    /// <summary>
    /// Land combat action resolution: Attack/Ability/Guard/Pass, secondary
    /// effects, Muerte, and the land DoT model (bleed/burn/poison applied to
    /// the actor). Extracted verbatim from CombatManager (ADR-004 §3) — the
    /// land EditMode test suite is the no-regression gate for this move.
    /// </summary>
    public class LandTurnResolver : ITurnResolver
    {
        private CombatManager _manager;

        public void Initialize(CombatManager manager)
        {
            _manager = manager;
        }

        public void OnTurnStart(ICombatant actor)
        {
            // Remove guard at start of turn (GDD §3: removed before acting)
            if (actor is CombatantState unit)
                unit.IsGuarding = false;
        }

        public void ResolveAction(ICombatant actorCombatant, CombatAction action, CombatContext context)
        {
            // Land battles only ever contain CombatantState combatants.
            var actor = (CombatantState)actorCombatant;

            switch (action.Type)
            {
                case ActionType.Guard:
                    actor.IsGuarding = true;
                    GameEvents.PublishGuardActivated(actor);
                    break;

                case ActionType.Pass:
                    break;

                case ActionType.Attack:
                    ResolveOffensiveAction(actor, action);
                    break;

                case ActionType.Ability:
                    if (action.AbilityData != null)
                    {
                        actor.ConsumeMP(action.AbilityData.MPCost);
                    }
                    ResolveOffensiveAction(actor, action);
                    if (action.AbilityData != null)
                    {
                        actor.ActivateCooldown(action.AbilityData);
                    }
                    break;
            }
        }

        public void ApplyPostActionDoTs(ICombatant actorCombatant, bool actorPassed)
        {
            var actor = (CombatantState)actorCombatant;

            // Step 6: Burn (only if acted — not if Passed) (GDD §2)
            if (!actorPassed && actor.HasStatus(StatusEffect.Quemadura))
            {
                int burnDamage = CalculateDoTDamage(actor, StatusEffect.Quemadura);
                int actual = actor.ApplyDamage(burnDamage);
                GameEvents.PublishDamageDealt(new DamageEvent
                {
                    Source = null,
                    Target = actor,
                    ActualDamage = actual,
                    DamageSource = DamageSource.Burn
                });

                if (actor.IsKO)
                    _manager.NotifyDeath(actor);
            }

            // Step 7: Poison
            if (!actor.IsKO && actor.HasStatus(StatusEffect.Veneno))
            {
                int poisonDamage = CalculateDoTDamage(actor, StatusEffect.Veneno);
                int actual = actor.ApplyDamage(poisonDamage);
                GameEvents.PublishDamageDealt(new DamageEvent
                {
                    Source = null,
                    Target = actor,
                    ActualDamage = actual,
                    DamageSource = DamageSource.Poison
                });

                if (actor.IsKO)
                    _manager.NotifyDeath(actor);
            }
        }

        public void ApplyStartOfTurnDoTs(ICombatant actorCombatant)
        {
            var actor = (CombatantState)actorCombatant;

            // Step 3: Bleed damage (Sangrado). Death is handled by the caller.
            if (actor.HasStatus(StatusEffect.Sangrado))
            {
                int bleedDamage = CalculateDoTDamage(actor, StatusEffect.Sangrado);
                int actual = actor.ApplyDamage(bleedDamage);
                GameEvents.PublishDamageDealt(new DamageEvent
                {
                    Source = null,
                    Target = actor,
                    ActualDamage = actual,
                    DamageSource = DamageSource.Bleed
                });
            }
        }

        // ====================================================================
        // ACTION RESOLUTION
        // ====================================================================

        private void ResolveOffensiveAction(CombatantState actor, CombatAction action)
        {
            // Land battles only ever target CombatantState combatants.
            switch (action.TargetType)
            {
                case TargetType.AoeEnemy:
                    var enemyTargets = _manager.GetAliveWaveEnemies();
                    foreach (var target in enemyTargets)
                        ResolveSingleTarget(actor, (CombatantState)target, action);
                    break;

                case TargetType.AllyAoe:
                    var allyTargets = _manager.GetAliveAllies();
                    foreach (var target in allyTargets)
                        ResolveHealOrBuff(actor, (CombatantState)target, action);
                    break;

                case TargetType.Self:
                    ResolveHealOrBuff(actor, actor, action);
                    break;

                case TargetType.SingleAlly:
                    if (action.Target != null)
                        ResolveHealOrBuff(actor, (CombatantState)action.Target, action);
                    break;

                case TargetType.SingleEnemy:
                default:
                    if (action.Target != null)
                        ResolveSingleTarget(actor, (CombatantState)action.Target, action);
                    break;
            }
        }

        private void ResolveSingleTarget(CombatantState actor, CombatantState target, CombatAction action)
        {
            float effAtk = action.IsPhysical
                ? actor.GetEffectiveStat(StatType.ATK)
                : actor.GetEffectiveStat(StatType.MST);
            float effDef = action.IsPhysical
                ? target.GetEffectiveStat(StatType.DEF)
                : target.GetEffectiveStat(StatType.SPR);

            float cri = actor.Template.SecondaryStats.CRI;
            bool isBlinded = actor.HasStatus(StatusEffect.Ceguera);

            var result = DamageCalculator.Calculate(
                effAtk, effDef, action.AbilityPower,
                action.Element, target.Template.Element,
                cri, action.IsPhysical, isBlinded);

            if (result.IsMiss)
            {
                GameEvents.PublishDamageDealt(new DamageEvent
                {
                    Source = actor,
                    Target = target,
                    Result = result,
                    ActualDamage = 0,
                    DamageSource = action.Type == ActionType.Ability
                        ? DamageSource.Ability : DamageSource.Attack
                });
                return;
            }

            int damage = result.FinalDamage;

            // Guard reduction (GDD §Formulas: applied after full DSE calculation)
            bool isGuarded = target.IsGuarding;
            if (isGuarded)
                damage = Mathf.Max(Mathf.FloorToInt(damage * CombatManager.GUARD_REDUCTION), 1);

            int actual = target.ApplyDamage(damage);

            GameEvents.PublishDamageDealt(new DamageEvent
            {
                Source = actor,
                Target = target,
                Result = result,
                IsGuarded = isGuarded,
                ActualDamage = actual,
                DamageSource = action.Type == ActionType.Ability
                    ? DamageSource.Ability : DamageSource.Attack
            });

            // Wake sleeping targets on damage (GDD: Sueño removed by damage)
            if (actual > 0 && target.HasStatus(StatusEffect.Sueno))
            {
                target.RemoveStatus(StatusEffect.Sueno);
                GameEvents.PublishStatusRemoved(new StatusRemovedEvent
                {
                    Target = target,
                    Effect = StatusEffect.Sueno,
                    Reason = StatusRemovalReason.WokenByDamage
                });
            }

            if (target.IsKO)
            {
                _manager.NotifyDeath(target);
            }
            else
            {
                ApplySecondaryEffects(actor, target, action.AbilityData);
            }
        }

        private void ResolveHealOrBuff(CombatantState source, CombatantState target, CombatAction action)
        {
            // Placeholder for heal/buff ability resolution (expanded in S2-04)
            int healAmount = HealCalculator.Calculate(
                source.GetEffectiveStat(StatType.MST), action.AbilityPower);
            int actual = target.ApplyHealing(healAmount);

            GameEvents.PublishHealApplied(new HealEvent
            {
                Source = source,
                Target = target,
                Amount = actual
            });
        }

        // ====================================================================
        // SECONDARY EFFECTS
        // ====================================================================

        private void ApplySecondaryEffects(CombatantState source, CombatantState target,
            AbilityData ability)
        {
            if (ability?.SecondaryEffects == null) return;

            foreach (var effect in ability.SecondaryEffects)
            {
                if (UnityEngine.Random.value > effect.Probability) continue;

                // Muerte: instant kill if target HP% below threshold (bosses immune)
                if (effect.Effect == StatusEffect.Muerte)
                {
                    ResolveMuerte(source, target, effect.Param);
                    continue;
                }

                var status = new StatusInstance
                {
                    Effect = effect.Effect,
                    RemainingTurns = effect.Duration,
                    Param = effect.Param,
                    SourceAbilityId = ability.Id
                };
                target.ApplyStatus(status);

                GameEvents.PublishStatusApplied(new StatusAppliedEvent
                {
                    Source = source,
                    Target = target,
                    Status = status
                });
            }
        }

        /// <summary>
        /// Resolves Muerte (instant kill threshold). Kills target if HP% &lt; threshold.
        /// Bosses are immune.
        /// </summary>
        private void ResolveMuerte(CombatantState source, CombatantState target, float threshold)
        {
            if (target.IsBoss) return;

            float hpPercent = (float)target.CurrentHP / target.MaxHP;
            if (hpPercent < threshold)
            {
                int lethalDamage = target.CurrentHP;
                target.ApplyDamage(lethalDamage);

                GameEvents.PublishDamageDealt(new DamageEvent
                {
                    Source = source,
                    Target = target,
                    ActualDamage = lethalDamage,
                    DamageSource = DamageSource.Ability
                });

                _manager.NotifyDeath(target);
            }
        }

        // ====================================================================
        // DOT CALCULATION
        // ====================================================================

        private int CalculateDoTDamage(CombatantState combatant, StatusEffect dotType)
        {
            for (int i = 0; i < combatant.StatusEffects.Count; i++)
            {
                if (combatant.StatusEffects[i].Effect == dotType)
                {
                    float percent = combatant.StatusEffects[i].Param;
                    int damage = Mathf.Max(
                        Mathf.FloorToInt(combatant.MaxHP * percent),
                        (int)CombatManager.DOT_MIN_DAMAGE);
                    return damage;
                }
            }
            return 0;
        }
    }
}
