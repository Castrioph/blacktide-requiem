using System;
using System.Collections.Generic;
using UnityEngine;
using BlacktideRequiem.Core.Combat;
using BlacktideRequiem.Core.Data;

namespace BlacktideRequiem.Core.AI
{
    /// <summary>
    /// AI decision-maker for naval enemies (ships and sea creatures).
    /// Implements ICombatInput. One instance per enemy: bosses keep
    /// one-directional phase state across turns.
    ///
    /// Global naval rules (Combate Naval GDD §4):
    /// - Enemies never use Maniobra Evasiva or Reparar.
    /// - Boarding requires living crew on BOTH sides: creatures (no crew)
    ///   never board, and ships without living crew cannot send boarders.
    ///   Targets without living crew (creatures) cannot be boarded.
    /// - Enemies ignore MP costs (same as land, GDD Open Questions #3) but
    ///   respect cooldowns and Silencio.
    ///
    /// Tiers (Enemy System GDD §7):
    /// - Normal: plain profile.
    /// - Elite: Profile+ — below ELITE_EMERGENCY_HP_THRESHOLD uses a heal
    ///   ability if available.
    /// - Jefe: behavior tree — profile switches per HP phase (NavalBossPhase),
    ///   one-directional. LB flows through LB-flagged SeaAbility entries.
    /// </summary>
    public class NavalEnemyAI : ICombatInput
    {
        /// <summary>Elite Profile+ emergency threshold (Enemy System GDD knob).</summary>
        public const float ELITE_EMERGENCY_HP_THRESHOLD = 0.30f;

        public EnemyTier Tier { get; }
        public AIProfileType Profile { get; }

        private readonly IReadOnlyList<NavalBossPhase> _bossPhases;
        private readonly System.Random _rng;

        /// <summary>Deepest boss phase reached (0 = base profile). Never decreases.</summary>
        private int _phaseReached;

        public NavalEnemyAI(EnemyTier tier, AIProfileType profile,
            IReadOnlyList<NavalBossPhase> bossPhases = null, System.Random rng = null)
        {
            Tier = tier;
            Profile = profile;
            _bossPhases = bossPhases;
            _rng = rng ?? new System.Random();
        }

        /// <summary>Builds the AI from the enemy fields authored on ShipData.</summary>
        public static NavalEnemyAI FromShipData(ShipData data, System.Random rng = null)
        {
            return new NavalEnemyAI(data.Tier, data.AIProfile, data.BossPhases, rng);
        }

        public void RequestAction(CombatContext context, Action<CombatAction> callback)
        {
            // Naval battles only ever contain ShipCombatant combatants.
            var actor = (ShipCombatant)context.Actor;

            // Elite Profile+ emergency override (Enemy System GDD §7)
            if (Tier == EnemyTier.Elite && TryEmergencyHeal(actor, out var emergency))
            {
                callback(emergency);
                return;
            }

            var action = EffectiveProfile(actor) switch
            {
                AIProfileType.Estratega => DecideEstratega(actor, context),
                AIProfileType.Defensivo => DecideDefensivo(actor, context),
                AIProfileType.Caotico => DecideCaotico(actor, context),
                _ => DecideAgresivo(actor, context)
            };
            callback(action);
        }

        // ====================================================================
        // BOSS PHASES (AC 27)
        // ====================================================================

        /// <summary>Profile for the current boss phase. Phases are checked in
        /// authored order (descending thresholds) and never revert.</summary>
        private AIProfileType EffectiveProfile(ShipCombatant actor)
        {
            if (Tier != EnemyTier.Jefe || _bossPhases == null || _bossPhases.Count == 0)
                return Profile;

            float hpFraction = actor.MaxHHP > 0
                ? (float)actor.CurrentHHP / actor.MaxHHP
                : 0f;

            int phase = 0;
            for (int i = 0; i < _bossPhases.Count; i++)
                if (hpFraction < _bossPhases[i].HPThreshold)
                    phase = i + 1;

            if (phase > _phaseReached)
                _phaseReached = phase;

            return _phaseReached == 0 ? Profile : _bossPhases[_phaseReached - 1].Profile;
        }

        // ====================================================================
        // AGRESIVO — lowest HHP target; kill-secured boarding, else max damage
        // ====================================================================

        private CombatAction DecideAgresivo(ShipCombatant actor, CombatContext context)
        {
            var target = FindLowestHP(context.Enemies) as ShipCombatant;
            if (target == null) return CombatAction.PassTurn();

            // Opportunistic boarding: only when the kill is guaranteed (AC 25)
            if (CanBoard(actor, target))
            {
                var weakest = FindWeakestCrew(target);
                if (weakest != null
                    && EstimateMinBoardingDamage(actor, target, weakest) >= weakest.CurrentHP)
                    return CombatAction.Boarding(target, weakest);
            }

            var ability = FindHighestDamageAbility(actor);
            if (ability != null)
                return CombatAction.FromAbility(ability.Value.Ability, target, ability.Value.Entry);

            return CombatAction.Cannonball(target);
        }

        // ====================================================================
        // ESTRATEGA — highest threat target; elemental edge, else board Capitán
        // ====================================================================

        private CombatAction DecideEstratega(ShipCombatant actor, CombatContext context)
        {
            var target = FindHighestThreat(context.Enemies) as ShipCombatant;
            if (target == null) return CombatAction.PassTurn();

            var elemental = FindElementalAdvantageAbility(actor, target.Element);
            if (elemental != null)
                return CombatAction.FromAbility(elemental.Value.Ability, target, elemental.Value.Entry);

            // Tactical boarding: Capitán first (kills synergies), else the
            // crew member contributing the most SeaAbilities
            if (CanBoard(actor, target))
            {
                var crewTarget = FindHighestValueCrew(target);
                if (crewTarget != null)
                    return CombatAction.Boarding(target, crewTarget);
            }

            var ability = FindHighestDamageAbility(actor);
            if (ability != null)
                return CombatAction.FromAbility(ability.Value.Ability, target, ability.Value.Entry);

            return CombatAction.Cannonball(target);
        }

        // ====================================================================
        // DEFENSIVO — buff if unbuffed; else attack (never Maniobra/Reparar)
        // ====================================================================

        private CombatAction DecideDefensivo(ShipCombatant actor, CombatContext context)
        {
            if (actor.Buffs.All.Count == 0)
            {
                var buff = FindAbilityByCategory(actor, AbilityCategory.Buff);
                if (buff != null)
                    return CombatAction.FromAbility(buff.Value.Ability, actor, buff.Value.Entry);
                // Land AI would Guard here; naval enemies attack instead (GDD §4)
            }

            var target = FindLowestHP(context.Enemies) as ShipCombatant;
            if (target == null) return CombatAction.PassTurn();

            var ability = FindHighestDamageAbility(actor);
            if (ability != null)
                return CombatAction.FromAbility(ability.Value.Ability, target, ability.Value.Entry);

            return CombatAction.Cannonball(target);
        }

        // ====================================================================
        // CAÓTICO — random valid action (still never Maniobra/Reparar)
        // ====================================================================

        private CombatAction DecideCaotico(ShipCombatant actor, CombatContext context)
        {
            if (context.Enemies.Count == 0) return CombatAction.PassTurn();

            var options = new List<CombatAction>();
            var target = (ShipCombatant)context.Enemies[_rng.Next(context.Enemies.Count)];

            options.Add(CombatAction.Cannonball(target));

            foreach (var available in GetAvailableAbilities(actor))
            {
                var abilityTarget = available.Ability.TargetType switch
                {
                    TargetType.Self => (ICombatant)actor,
                    TargetType.AllyAoe => actor,
                    TargetType.SingleAlly => actor,
                    _ => target
                };
                if (available.Ability.TargetType == TargetType.SingleCrewEnemy)
                {
                    if (!CanBoard(actor, target)) continue;
                    var living = target.GetLivingCrew();
                    var crewAction = CombatAction.FromAbility(
                        available.Ability, target, available.Entry);
                    crewAction.TargetCrew = living[_rng.Next(living.Count)];
                    options.Add(crewAction);
                    continue;
                }
                options.Add(CombatAction.FromAbility(
                    available.Ability, abilityTarget, available.Entry));
            }

            if (CanBoard(actor, target))
            {
                var living = target.GetLivingCrew();
                options.Add(CombatAction.Boarding(target, living[_rng.Next(living.Count)]));
            }

            return options[_rng.Next(options.Count)];
        }

        // ====================================================================
        // ELITE PROFILE+ OVERRIDE
        // ====================================================================

        private bool TryEmergencyHeal(ShipCombatant actor, out CombatAction action)
        {
            action = default;
            if (actor.MaxHHP <= 0) return false;
            if ((float)actor.CurrentHHP / actor.MaxHHP >= ELITE_EMERGENCY_HP_THRESHOLD)
                return false;

            var heal = FindAbilityByCategory(actor, AbilityCategory.Heal);
            if (heal == null) return false;

            action = CombatAction.FromAbility(heal.Value.Ability, actor, heal.Value.Entry);
            return true;
        }

        // ====================================================================
        // BOARDING RULES (AC 25-26)
        // ====================================================================

        /// <summary>Both sides need living crew: creatures never board and
        /// cannot be boarded (Combate Naval GDD §4).</summary>
        private static bool CanBoard(ShipCombatant actor, ShipCombatant target)
        {
            return actor.GetLivingCrew().Count > 0
                && target.GetLivingCrew().Count > 0;
        }

        /// <summary>Worst-case boarding damage (min variance, no crit). Returns 0
        /// when blinded — a 50% miss chance never guarantees a kill.</summary>
        private static int EstimateMinBoardingDamage(ShipCombatant actor,
            ShipCombatant target, CrewMemberState crew)
        {
            if (actor.HasStatus(StatusEffect.Ceguera)) return 0;

            float effFPW = actor.GetEffectiveShipStat(ShipStatType.FPW);
            float crewDEF = crew.Unit.BaseStats[(int)StatType.DEF];

            var result = DamageCalculator.CalculateDeterministic(
                effFPW, crewDEF, NavalTurnResolver.BOARDING_POWER,
                actor.Element, crew.Unit.Element,
                isCrit: false, effectiveCRI: 0f,
                variance: DamageCalculator.VARIANCE_MIN);

            int damage = result.FinalDamage;
            if (target.IsManeuvering)
                damage = Mathf.Max(
                    Mathf.FloorToInt(damage * NavalTurnResolver.MANEUVER_REDUCTION), 1);
            return damage;
        }

        // ====================================================================
        // TARGET SELECTION
        // ====================================================================

        private static ICombatant FindLowestHP(List<ICombatant> combatants)
        {
            if (combatants == null || combatants.Count == 0) return null;

            ICombatant lowest = combatants[0];
            for (int i = 1; i < combatants.Count; i++)
                if (combatants[i].CurrentHP < lowest.CurrentHP)
                    lowest = combatants[i];
            return lowest;
        }

        /// <summary>Threat = max(FPW, MST) effective (ATK/MST map to FPW/MST on ships).</summary>
        private static ICombatant FindHighestThreat(List<ICombatant> combatants)
        {
            if (combatants == null || combatants.Count == 0) return null;

            ICombatant best = combatants[0];
            float bestThreat = Threat(best);
            for (int i = 1; i < combatants.Count; i++)
            {
                float threat = Threat(combatants[i]);
                if (threat > bestThreat)
                {
                    best = combatants[i];
                    bestThreat = threat;
                }
            }
            return best;

            static float Threat(ICombatant c) => Mathf.Max(
                c.GetEffectiveStat(StatType.ATK), c.GetEffectiveStat(StatType.MST));
        }

        private static CrewMemberState FindWeakestCrew(ShipCombatant ship)
        {
            CrewMemberState weakest = null;
            foreach (var crew in ship.GetLivingCrew())
                if (weakest == null || crew.CurrentHP < weakest.CurrentHP)
                    weakest = crew;
            return weakest;
        }

        /// <summary>Capitán if alive (boarding him kills synergies), else the
        /// crew member contributing the most SeaAbilities.</summary>
        private static CrewMemberState FindHighestValueCrew(ShipCombatant ship)
        {
            if (ship.CaptainAlive) return ship.Captain;

            CrewMemberState best = null;
            int bestCount = -1;
            foreach (var crew in ship.GetLivingCrew())
            {
                int count = crew.Unit != null && crew.Unit.SeaAbilities != null
                    ? crew.Unit.SeaAbilities.Count
                    : 0;
                if (count > bestCount)
                {
                    best = crew;
                    bestCount = count;
                }
            }
            return best;
        }

        // ====================================================================
        // ABILITY SELECTION
        // ====================================================================

        private readonly struct AvailableAbility
        {
            public readonly AbilityData Ability;
            public readonly AbilityEntry? Entry;

            public AvailableAbility(AbilityData ability, AbilityEntry? entry)
            {
                Ability = ability;
                Entry = entry;
            }
        }

        /// <summary>
        /// Ready abilities with their AbilityEntry (needed so LB flags reach the
        /// resolver): ship BaseAbilities (no entry) + living crew SeaAbilities.
        /// Filters cooldown and Silencio; ignores MP (enemies, per GDD).
        /// </summary>
        private static List<AvailableAbility> GetAvailableAbilities(ShipCombatant actor)
        {
            var available = new List<AvailableAbility>();
            if (actor.HasStatus(StatusEffect.Silencio)) return available;

            if (actor.Ship.BaseAbilities != null)
            {
                foreach (var ability in actor.Ship.BaseAbilities)
                {
                    if (ability == null) continue;
                    if (actor.GetCooldownRemaining(ability) > 0) continue;
                    available.Add(new AvailableAbility(ability, null));
                }
            }

            foreach (var crew in actor.GetLivingCrew())
            {
                if (crew.Unit == null || crew.Unit.SeaAbilities == null) continue;
                foreach (var entry in crew.Unit.SeaAbilities)
                {
                    if (entry.Ability == null) continue;
                    if (actor.GetCooldownRemaining(entry.Ability) > 0) continue;
                    available.Add(new AvailableAbility(entry.Ability, entry));
                }
            }
            return available;
        }

        private static AvailableAbility? FindHighestDamageAbility(ShipCombatant actor)
        {
            AvailableAbility? best = null;
            float bestPower = 0f;
            foreach (var available in GetAvailableAbilities(actor))
            {
                if (available.Ability.Category != AbilityCategory.Damage) continue;
                if (available.Ability.AbilityPower > bestPower)
                {
                    best = available;
                    bestPower = available.Ability.AbilityPower;
                }
            }
            return best;
        }

        /// <summary>Highest-power damage ability with elemental advantage
        /// against the target's defensive element.</summary>
        private static AvailableAbility? FindElementalAdvantageAbility(
            ShipCombatant actor, Element targetElement)
        {
            AvailableAbility? best = null;
            float bestPower = 0f;
            foreach (var available in GetAvailableAbilities(actor))
            {
                if (available.Ability.Category != AbilityCategory.Damage) continue;
                if (ElementTable.GetElementMod(available.Ability.Element, targetElement)
                    <= ElementTable.NEUTRAL_MOD) continue;
                if (available.Ability.AbilityPower > bestPower)
                {
                    best = available;
                    bestPower = available.Ability.AbilityPower;
                }
            }
            return best;
        }

        private static AvailableAbility? FindAbilityByCategory(
            ShipCombatant actor, AbilityCategory category)
        {
            foreach (var available in GetAvailableAbilities(actor))
                if (available.Ability.Category == category)
                    return available;
            return null;
        }
    }
}
