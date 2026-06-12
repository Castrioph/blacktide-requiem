using UnityEngine;
using BlacktideRequiem.Core.Data;
using BlacktideRequiem.Core.Events;

namespace BlacktideRequiem.Core.Combat
{
    /// <summary>
    /// Naval combat action resolution: the 6 naval actions (Cañonazo,
    /// Habilidad Naval, Maniobra Evasiva, Abordaje, Reparar, Pasar), the naval
    /// DoT split (Quemadura→ship HHP; Veneno/Sangrado→1 random living crew
    /// member) and naval Limit Break (extra SHIP turn, max 1/round).
    /// Ships never crit (CritMod = 1.0 — effectiveCRI is always 0).
    /// See Combate Naval GDD §2-§5 and ADR-004 §3.
    /// </summary>
    public class NavalTurnResolver : ITurnResolver
    {
        // --- Tuning knobs (Combate Naval GDD §Tuning Knobs) ---
        public const float BOARDING_POWER = 0.8f;
        public const float REPAIR_POWER = 1.5f;
        public const int REPAIR_MP_COST = 20;
        public const float MANEUVER_REDUCTION = 0.50f;

        private CombatManager _manager;
        private readonly System.Random _rng;

        /// <summary>Kills (crew or ship) during the current action — LB OnKill.</summary>
        private int _killsThisAction;

        /// <summary>Element modifier of the last hit this action — LB OnElementAdvantage.</summary>
        private float _lastElementMod;

        /// <summary>Inject an RNG for deterministic tests (crew DoT targeting).</summary>
        public NavalTurnResolver(System.Random rng = null)
        {
            _rng = rng ?? new System.Random();
        }

        public void Initialize(CombatManager manager)
        {
            _manager = manager;
        }

        /// <summary>Maniobra Evasiva expires at the start of the ship's own turn (GDD §3).</summary>
        public void OnTurnStart(ICombatant actor)
        {
            if (actor is ShipCombatant ship)
                ship.IsManeuvering = false;
        }

        // ====================================================================
        // ACTION RESOLUTION (GDD §3)
        // ====================================================================

        public void ResolveAction(ICombatant actorCombatant, CombatAction action, CombatContext context)
        {
            // Naval battles only ever contain ShipCombatant combatants.
            var ship = (ShipCombatant)actorCombatant;

            _killsThisAction = 0;
            _lastElementMod = 1f;

            switch (action.Type)
            {
                case ActionType.Pass:
                    break;

                case ActionType.Maneuver:
                    ship.IsManeuvering = true;
                    GameEvents.PublishManeuverActivated(ship);
                    break;

                case ActionType.Repair:
                    ResolveRepair(ship);
                    break;

                case ActionType.Boarding:
                    ResolveBoarding(ship, action, BOARDING_POWER, DamageSource.Boarding);
                    break;

                case ActionType.Attack:
                    ResolveOffensiveAction(ship, action);
                    break;

                case ActionType.Ability:
                    if (action.AbilityData != null)
                        ship.ConsumeMP(action.AbilityData.MPCost);

                    if (action.TargetType == TargetType.SingleCrewEnemy)
                        ResolveBoarding(ship, action, action.AbilityPower, DamageSource.Ability);
                    else
                        ResolveOffensiveAction(ship, action);

                    if (action.AbilityData != null)
                        ship.ActivateCooldown(action.AbilityData);

                    TryLimitBreak(ship, action);
                    break;
            }
        }

        /// <summary>Reparar: floor(MST_eff × REPAIR_POWER) hull heal for a fixed
        /// MP cost. Base action, not an ability — works under Silencio (edge 14).
        /// Without enough MP it does nothing (edge 5).</summary>
        private void ResolveRepair(ShipCombatant ship)
        {
            if (ship.CurrentMP < REPAIR_MP_COST)
                return;

            ship.ConsumeMP(REPAIR_MP_COST);
            int amount = HealCalculator.Calculate(
                ship.GetEffectiveShipStat(ShipStatType.MST), REPAIR_POWER);
            int actual = ship.ApplyHealing(amount);

            GameEvents.PublishHealApplied(new HealEvent
            {
                Source = ship,
                Target = ship,
                Amount = actual
            });
        }

        // ====================================================================
        // HULL DAMAGE (Cañonazo / abilities — GDD §Formulas 1)
        // ====================================================================

        private void ResolveOffensiveAction(ShipCombatant actor, CombatAction action)
        {
            switch (action.TargetType)
            {
                case TargetType.AoeEnemy:
                    var enemyTargets = _manager.GetAliveWaveEnemies();
                    foreach (var target in enemyTargets)
                        ResolveHullHit(actor, target, action);
                    break;

                case TargetType.AllyAoe:
                    var allyTargets = _manager.GetAliveAllies();
                    foreach (var target in allyTargets)
                        ResolveHealOrBuff(actor, target, action);
                    break;

                case TargetType.Self:
                    ResolveHealOrBuff(actor, actor, action);
                    break;

                case TargetType.SingleAlly:
                    if (action.Target != null)
                        ResolveHealOrBuff(actor, action.Target, action);
                    break;

                case TargetType.SingleEnemy:
                default:
                    if (action.Target != null)
                        ResolveHullHit(actor, action.Target, action);
                    break;
            }
        }

        private void ResolveHullHit(ShipCombatant actor, ICombatant target, CombatAction action)
        {
            // Heal-category abilities route to healing even on odd target types.
            if (action.AbilityData != null && action.AbilityData.Category == AbilityCategory.Heal)
            {
                ResolveHealOrBuff(actor, target, action);
                return;
            }

            float effAtk = action.IsPhysical
                ? actor.GetEffectiveShipStat(ShipStatType.FPW)
                : actor.GetEffectiveShipStat(ShipStatType.MST);
            float effDef = action.IsPhysical
                ? target.GetEffectiveStat(StatType.DEF)   // HDF on ships
                : target.GetEffectiveStat(StatType.SPR);  // RSL on ships

            bool isBlinded = actor.HasStatus(StatusEffect.Ceguera);

            // effectiveCRI 0 → ships never crit (GDD: CritMod = 1.0 always)
            var result = DamageCalculator.Calculate(
                effAtk, effDef, action.AbilityPower,
                action.Element, target.Element,
                0f, action.IsPhysical, isBlinded);

            _lastElementMod = result.ElementMod;

            if (result.IsMiss)
            {
                PublishHullDamage(actor, target, result, false, 0, action);
                return;
            }

            int damage = result.FinalDamage;

            // Maniobra Evasiva halves hull damage (GDD §Formulas 4)
            bool isManeuvered = target is ShipCombatant ts && ts.IsManeuvering;
            if (isManeuvered)
                damage = Mathf.Max(Mathf.FloorToInt(damage * MANEUVER_REDUCTION), 1);

            int actual = target.ApplyDamage(damage);
            PublishHullDamage(actor, target, result, isManeuvered, actual, action);

            if (target.IsKO)
            {
                _killsThisAction++;
                _manager.NotifyDeath(target);
            }
            else
            {
                ApplySecondaryEffects(actor, target, action.AbilityData);
            }
        }

        private static void PublishHullDamage(ShipCombatant actor, ICombatant target,
            DamageResult result, bool isManeuvered, int actual, CombatAction action)
        {
            GameEvents.PublishDamageDealt(new DamageEvent
            {
                Source = actor,
                Target = target,
                Result = result,
                IsGuarded = isManeuvered,
                ActualDamage = actual,
                DamageSource = action.Type == ActionType.Ability
                    ? DamageSource.Ability : DamageSource.Attack
            });
        }

        /// <summary>Naval healing restores ship HHP only — crew HP never goes up
        /// in combat (GDD §Formulas 5).</summary>
        private void ResolveHealOrBuff(ShipCombatant source, ICombatant target, CombatAction action)
        {
            float healPower = action.AbilityData != null && action.AbilityData.HealPower > 0f
                ? action.AbilityData.HealPower
                : action.AbilityPower;
            int amount = HealCalculator.Calculate(
                source.GetEffectiveShipStat(ShipStatType.MST), healPower);
            int actual = target.ApplyHealing(amount);

            GameEvents.PublishHealApplied(new HealEvent
            {
                Source = source,
                Target = target,
                Amount = actual
            });
        }

        // ====================================================================
        // ABORDAJE (GDD §Formulas 2)
        // ====================================================================

        /// <summary>Resolves a hit on an enemy crew member: Abordaje (power =
        /// BOARDING_POWER) or a SingleCrewEnemy ability (power = AbilityPower).
        /// No-op against ships without living crew (sea creatures, AC 12).</summary>
        private void ResolveBoarding(ShipCombatant actor, CombatAction action,
            float power, DamageSource source)
        {
            var targetShip = action.Target as ShipCombatant;
            var crew = action.TargetCrew;
            if (targetShip == null || crew == null || crew.IsDead || crew.Unit == null)
                return;

            float effFPW = actor.GetEffectiveShipStat(ShipStatType.FPW);
            float crewDEF = crew.Unit.BaseStats[(int)StatType.DEF];
            bool isBlinded = actor.HasStatus(StatusEffect.Ceguera);

            // ElementMod: attacking SHIP's element vs the crew UNIT's element (GDD §Formulas 2)
            var result = DamageCalculator.Calculate(
                effFPW, crewDEF, power,
                actor.Element, crew.Unit.Element,
                0f, true, isBlinded);

            _lastElementMod = result.ElementMod;

            if (result.IsMiss)
                return;

            int damage = result.FinalDamage;

            // The defending ship's Maniobra also protects its crew (edge case 4)
            if (targetShip.IsManeuvering)
                damage = Mathf.Max(Mathf.FloorToInt(damage * MANEUVER_REDUCTION), 1);

            DamageCrew(targetShip, crew, damage, source, actor);
        }

        // ====================================================================
        // NAVAL DOT SPLIT (GDD §2, edge case 8)
        // ====================================================================

        /// <summary>Sangrado at turn start → 1 random living crew member.</summary>
        public void ApplyStartOfTurnDoTs(ICombatant actorCombatant)
        {
            var ship = (ShipCombatant)actorCombatant;
            ApplyCrewDoT(ship, StatusEffect.Sangrado, DamageSource.Bleed);
        }

        /// <summary>Quemadura → ship hull (only if it acted); Veneno → 1 random
        /// living crew member. Maniobra never reduces DoT damage (AC 10).</summary>
        public void ApplyPostActionDoTs(ICombatant actorCombatant, bool actorPassed)
        {
            var ship = (ShipCombatant)actorCombatant;

            // Quemadura: fire on the hull, only triggered by acting (AC 14, 21)
            if (!actorPassed && ship.HasStatus(StatusEffect.Quemadura))
            {
                int burnDamage = CalculateDoTDamage(ship, StatusEffect.Quemadura, ship.MaxHHP);
                int actual = ship.ApplyDamage(burnDamage);
                GameEvents.PublishDamageDealt(new DamageEvent
                {
                    Source = null,
                    Target = ship,
                    ActualDamage = actual,
                    DamageSource = DamageSource.Burn
                });

                if (ship.IsKO)
                {
                    _manager.NotifyDeath(ship);
                    return;
                }
            }

            // Veneno: 1 random living crew member (AC 22)
            ApplyCrewDoT(ship, StatusEffect.Veneno, DamageSource.Poison);
        }

        /// <summary>Applies a crew-targeting DoT tick. Without living crew the
        /// effect fizzles — duration still ticks elsewhere (AC 24, edge 9).</summary>
        private void ApplyCrewDoT(ShipCombatant ship, StatusEffect effect, DamageSource source)
        {
            if (!ship.HasStatus(effect))
                return;

            var living = ship.GetLivingCrew();
            if (living.Count == 0)
                return;

            var crew = living[_rng.Next(living.Count)];
            int damage = CalculateDoTDamage(ship, effect, crew.MaxHP);
            DamageCrew(ship, crew, damage, source, null);
        }

        /// <summary>Damages a crew member through the ship API (stats/pool/synergy
        /// recalculation on death happens inside DamageCrewMember) and publishes
        /// the naval events. Counts kills for LB OnKill (edge case 7).</summary>
        private void DamageCrew(ShipCombatant ship, CrewMemberState crew, int damage,
            DamageSource source, ICombatant attacker)
        {
            int actual = ship.DamageCrewMember(crew, damage, out bool died);

            GameEvents.PublishCrewDamaged(new CrewDamageEvent
            {
                Ship = ship,
                Crew = crew,
                ActualDamage = actual,
                Source = source,
                Attacker = attacker
            });

            if (died)
            {
                _killsThisAction++;
                GameEvents.PublishCrewDied(new CrewDiedEvent { Ship = ship, Crew = crew });
                GameEvents.PublishShipStatsRecalculated(ship);
            }
        }

        private static int CalculateDoTDamage(ShipCombatant ship, StatusEffect dotType, int maxHP)
        {
            var statuses = ship.StatusEffects;
            for (int i = 0; i < statuses.Count; i++)
            {
                if (statuses[i].Effect == dotType)
                {
                    return Mathf.Max(
                        Mathf.FloorToInt(maxHP * statuses[i].Param),
                        (int)CombatManager.DOT_MIN_DAMAGE);
                }
            }
            return 0;
        }

        // ====================================================================
        // LIMIT BREAK NAVAL (GDD §5, AC 33-35)
        // ====================================================================

        /// <summary>Grants the SHIP an extra turn when an LB-flagged ability
        /// meets its condition. Max 1 per ship per round (InitiativeBar also
        /// enforces this). OnCrit never triggers (ships don't crit); OnAllyDown
        /// has no naval equivalent (single-ship side).</summary>
        private void TryLimitBreak(ShipCombatant ship, CombatAction action)
        {
            if (action.AbilityEntry is not AbilityEntry entry || !entry.CanLimitBreak)
                return;
            if (ship.LBUsedThisRound)
                return;
            if (!IsLBConditionMet(ship, entry, action))
                return;

            if (_manager.Bar.InsertLimitBreak(ship))
            {
                ship.LBUsedThisRound = true;
                GameEvents.PublishLimitBreakActivated(ship);
            }
        }

        private bool IsLBConditionMet(ShipCombatant ship, AbilityEntry entry, CombatAction action)
        {
            switch (entry.LBCondition)
            {
                case LBCondition.OnKill:
                    return _killsThisAction > 0;
                case LBCondition.OnLowHP:
                    return ship.MaxHHP > 0
                        && (float)ship.CurrentHHP / ship.MaxHHP < entry.LBConditionParam;
                case LBCondition.OnElementAdvantage:
                    return _lastElementMod > 1f;
                case LBCondition.OnStatusTarget:
                    return action.Target != null
                        && System.Enum.TryParse(entry.LBConditionTarget, out StatusEffect status)
                        && action.Target.HasStatus(status);
                default:
                    return false;
            }
        }

        // ====================================================================
        // SECONDARY EFFECTS (statuses on hull hits; ships filter immunities)
        // ====================================================================

        private void ApplySecondaryEffects(ShipCombatant source, ICombatant target,
            AbilityData ability)
        {
            if (ability?.SecondaryEffects == null) return;

            foreach (var effect in ability.SecondaryEffects)
            {
                if (UnityEngine.Random.value > effect.Probability) continue;

                // Ships are immune to Muerte (GDD AC 28) — skip entirely.
                if (target.IsImmuneTo(effect.Effect)) continue;

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
    }
}
