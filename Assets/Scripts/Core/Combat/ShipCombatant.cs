using System.Collections.Generic;
using UnityEngine;
using BlacktideRequiem.Core.Data;

namespace BlacktideRequiem.Core.Combat
{
    /// <summary>
    /// Runtime state of a ship in naval combat. Implements ICombatant so the
    /// CombatManager treats it as one combatant on the Initiative Bar.
    /// Owns attackable crew sub-entities (CrewMemberState, no turns) and
    /// derives effective stats from base + upgrades + living crew + buffs.
    ///
    /// EffectiveStat = Base + UpgradeBonus + Σ CrewContribution(viva), then
    /// multiplied by the BuffStack modifier (same model as land units).
    /// The additive part is cached and invalidated only by RecalculateFromCrew.
    ///
    /// See ADR-004 §2/§4 and Combate Naval GDD.
    /// </summary>
    public class ShipCombatant : ICombatant
    {
        public ShipData Ship { get; }
        public ShipUpgradeState Upgrades { get; }

        // --- Combat resources (runtime) ---

        /// <summary>Hull HP. Fixed at construction from initial effective HHP;
        /// crew death does NOT lower the max (only future contributions).</summary>
        public int MaxHHP { get; }
        public int CurrentHHP { get; private set; }

        public int MaxMP { get; }
        public int CurrentMP { get; private set; }

        /// <summary>Maniobra Evasiva active until the ship's next turn.</summary>
        public bool IsManeuvering { get; set; }

        public bool IsKO => CurrentHHP <= 0;
        public bool IsBoss { get; set; }
        public Element Element => Ship.Element;
        public string DisplayName => Ship != null ? Ship.DisplayName : null;

        // --- ICombatant HP surface (maps to hull) ---
        int ICombatant.CurrentHP => CurrentHHP;
        int ICombatant.MaxHP => MaxHHP;

        // --- Crew ---

        /// <summary>Crew members (attackable sub-entities, take no turns).</summary>
        public IReadOnlyList<CrewMemberState> Crew => _crew;
        private readonly List<CrewMemberState> _crew = new();

        /// <summary>Crew member in the Capitán role slot, or null.</summary>
        public CrewMemberState Captain
        {
            get
            {
                for (int i = 0; i < _crew.Count; i++)
                    if (_crew[i].Role == NavalRole.Capitan)
                        return _crew[i];
                return null;
            }
        }

        /// <summary>Whether the guest slot is filled by a friend unit (2nd captain, S4-08).</summary>
        public bool IsGuestFriend { get; }

        // --- Ability pool ---

        /// <summary>Effective pool: BaseAbilities + SeaAbilities of LIVING crew.
        /// Duplicates allowed (GDD edge case: each instance is separate).</summary>
        public IReadOnlyList<AbilityData> AbilityPool => _abilityPool;
        private readonly List<AbilityData> _abilityPool = new();

        // --- Shared pipeline state (ICombatant) ---

        public BuffStack Buffs { get; } = new();
        public bool LBUsedThisRound { get; set; }
        public int CCImmunityTurns { get; set; }

        public List<StatusInstance> StatusEffects { get; } = new();
        IReadOnlyList<StatusInstance> ICombatant.StatusEffects => StatusEffects;

        // --- ITraitCarrier (the ship itself carries no traits; crew do) ---
        public IReadOnlyList<UnitTraitEntry> Traits => null;
        public BuffStack SynergyBuffTarget => Buffs;

        // --- Crew synergies (evaluated over living crew; buffs land on this ship's BuffStack) ---

        /// <summary>Currently active crew trait synergies.</summary>
        public IReadOnlyList<ActiveSynergy> CrewSynergies => _crewSynergies;
        private List<ActiveSynergy> _crewSynergies = new();

        // --- Effective stat cache (additive part: base + upgrade + crew) ---
        private ShipStatBlock _additiveStats;

        private readonly Dictionary<string, int> _cooldowns = new();

        /// <summary>
        /// Builds the ship runtime from authored data. crewBySlot maps
        /// RoleSlot.SlotIndex → assigned unit; missing slots stay empty
        /// (contribute nothing — GDD: partially crewed ships are deployable).
        /// </summary>
        public ShipCombatant(ShipData ship, ShipUpgradeState upgrades,
            IReadOnlyDictionary<int, CharacterData> crewBySlot = null,
            bool isGuestFriend = false)
        {
            Ship = ship;
            Upgrades = upgrades;
            IsGuestFriend = isGuestFriend;

            if (crewBySlot != null && ship.RoleSlots != null)
            {
                foreach (var slot in ship.RoleSlots)
                {
                    if (crewBySlot.TryGetValue(slot.SlotIndex, out var unit) && unit != null)
                        _crew.Add(new CrewMemberState(slot, unit, Buffs));
                }
            }

            RecalculateFromCrew();

            // Resource maxima fixed from INITIAL effective stats (GDD §Formulas 3:
            // current HHP/MP start at effective values; crew death later reduces
            // contributions but not the already-established maxima).
            MaxHHP = Mathf.FloorToInt(GetEffectiveShipStat(ShipStatType.HHP));
            MaxMP = Mathf.FloorToInt(GetEffectiveShipStat(ShipStatType.MP));
            CurrentHHP = MaxHHP;
            CurrentMP = MaxMP;
        }

        // ====================================================================
        // EFFECTIVE STATS
        // ====================================================================

        /// <summary>
        /// Effective naval stat: cached (base + upgrade + living crew) × buff
        /// modifier. Cache is rebuilt only by RecalculateFromCrew (ADR-004 §4).
        /// </summary>
        public float GetEffectiveShipStat(ShipStatType stat)
        {
            float additive = _additiveStats[(int)stat];
            float modifier = Buffs.GetStatModifier(MapToUnitStatType(stat));
            return additive * modifier;
        }

        /// <summary>
        /// ICombatant bridge: maps unit StatType to the naval equivalent so the
        /// shared InitiativeBar (SPD) and formulas read ship stats transparently.
        /// </summary>
        public float GetEffectiveStat(StatType stat)
        {
            return GetEffectiveShipStat(MapToShipStatType(stat));
        }

        /// <summary>
        /// Rebuilds the additive stat cache and the ability pool from LIVING
        /// crew. Call after any crew death (NavalTurnResolver) or at setup.
        /// Captain death synergy deactivation is handled by the CombatManager.
        /// </summary>
        public void RecalculateFromCrew()
        {
            var stats = new ShipStatBlock();
            for (int s = 0; s < ShipStatBlock.COUNT; s++)
            {
                var statType = (ShipStatType)s;
                float total = Ship.BaseStats[s] + Ship.GetUpgradeBonus(statType, Upgrades);

                for (int c = 0; c < _crew.Count; c++)
                {
                    var crew = _crew[c];
                    if (crew.IsDead || crew.Unit == null) continue;

                    float unitStat = crew.Unit.BaseStats[(int)ShipData.MapNavalToUnitStat(statType)];
                    var affinity = GetUnitAffinityForSlot(crew);
                    total += ShipData.CalculateCrewContribution(
                        unitStat, crew.Role, affinity, statType);
                }

                stats[s] = total;
            }
            _additiveStats = stats;

            RebuildAbilityPool();
        }

        /// <summary>
        /// Applies damage to a crew member (Abordaje, Veneno/Sangrado naval).
        /// On death, immediately recalculates ship stats, rebuilds the ability
        /// pool and re-evaluates crew synergies (Combate Naval GDD §6: the dead
        /// member's stats, abilities AND trait count are lost at once; captain
        /// death drops captain-sourced synergies via re-evaluation).
        /// Returns the actual damage dealt; out param signals death this hit.
        /// </summary>
        public int DamageCrewMember(CrewMemberState crew, int damage, out bool died)
        {
            bool wasAlive = !crew.IsDead;
            int actual = crew.ApplyDamage(damage);
            died = wasAlive && crew.IsDead;

            if (died)
            {
                RecalculateFromCrew();
                EvaluateCrewSynergies();
            }
            return actual;
        }

        /// <summary>
        /// Re-evaluates crew trait synergies over LIVING crew with the crew
        /// Capitán as primary captain. Removes previous synergy buffs first.
        /// Buffs land on this ship's BuffStack (ITraitCarrier.SynergyBuffTarget).
        /// Guest-as-second-captain is S4-08 (not evaluated yet).
        /// Returns the new active synergies; event publishing is the caller's
        /// responsibility (CombatManager/NavalTurnResolver, S4-04).
        /// </summary>
        public IReadOnlyList<ActiveSynergy> EvaluateCrewSynergies()
        {
            SynergyEvaluator.RemoveBuffs(_crewSynergies);
            _crewSynergies = new List<ActiveSynergy>();

            var livingCrew = new List<CrewMemberState>();
            int captainIndex = -1;
            for (int i = 0; i < _crew.Count; i++)
            {
                if (_crew[i].IsDead) continue;
                if (_crew[i].Role == NavalRole.Capitan && captainIndex < 0)
                    captainIndex = livingCrew.Count;
                livingCrew.Add(_crew[i]);
            }

            if (captainIndex >= 0)
            {
                _crewSynergies = SynergyEvaluator.Evaluate(
                    livingCrew, captainIndex, isGuestFriend: false);
                SynergyEvaluator.ApplyBuffs(_crewSynergies);
            }

            return _crewSynergies;
        }

        /// <summary>Crew member alive in the Capitán slot? (synergy precondition)</summary>
        public bool CaptainAlive => Captain != null && !Captain.IsDead;

        /// <summary>Living crew members that Abordaje / crew DoTs can target.</summary>
        public List<CrewMemberState> GetLivingCrew()
        {
            var result = new List<CrewMemberState>();
            for (int i = 0; i < _crew.Count; i++)
                if (!_crew[i].IsDead)
                    result.Add(_crew[i]);
            return result;
        }

        private void RebuildAbilityPool()
        {
            _abilityPool.Clear();

            if (Ship.BaseAbilities != null)
                for (int i = 0; i < Ship.BaseAbilities.Count; i++)
                    if (Ship.BaseAbilities[i] != null)
                        _abilityPool.Add(Ship.BaseAbilities[i]);

            for (int c = 0; c < _crew.Count; c++)
            {
                var crew = _crew[c];
                if (crew.IsDead || crew.Unit == null || crew.Unit.SeaAbilities == null)
                    continue;

                for (int a = 0; a < crew.Unit.SeaAbilities.Count; a++)
                {
                    var entry = crew.Unit.SeaAbilities[a];
                    if (entry.Ability != null)
                        _abilityPool.Add(entry.Ability);
                }
            }
        }

        /// <summary>
        /// Affinity used for the mismatch check: the unit's first matching
        /// affinity if any, else a role that differs from the slot (penalty).
        /// </summary>
        private static NavalRole GetUnitAffinityForSlot(CrewMemberState crew)
        {
            if (crew.RoleMatches)
                return crew.Role;
            // No match — return any role different from the slot's so
            // CalculateCrewContribution applies MISMATCH_PENALTY.
            return crew.Role == NavalRole.Capitan ? NavalRole.Artillero : NavalRole.Capitan;
        }

        // ====================================================================
        // DAMAGE / HEALING (hull)
        // ====================================================================

        /// <summary>Hull damage (Cañonazo, abilities, Quemadura). Crew is NOT
        /// affected by hull damage (GDD §6).</summary>
        public int ApplyDamage(int damage)
        {
            int actual = System.Math.Min(damage, CurrentHHP);
            CurrentHHP -= actual;
            return actual;
        }

        /// <summary>Hull repair (Reparar). Clamps to MaxHHP.</summary>
        public int ApplyHealing(int amount)
        {
            int actual = System.Math.Min(amount, MaxHHP - CurrentHHP);
            CurrentHHP += actual;
            return actual;
        }

        /// <summary>Deducts ship MP for an ability. Clamps to 0.</summary>
        public void ConsumeMP(int amount)
        {
            CurrentMP = System.Math.Max(0, CurrentMP - amount);
        }

        // ====================================================================
        // STATUS EFFECTS (ships immune to Sueño/Aturdimiento/Muerte)
        // ====================================================================

        public bool IsImmuneTo(StatusEffect effect)
        {
            return effect == StatusEffect.Sueno
                || effect == StatusEffect.Aturdimiento
                || effect == StatusEffect.Muerte;
        }

        public bool HasStatus(StatusEffect effect)
        {
            for (int i = 0; i < StatusEffects.Count; i++)
                if (StatusEffects[i].Effect == effect)
                    return true;
            return false;
        }

        /// <summary>Applies a status unless the ship is immune (CC/Muerte are
        /// silently ignored — GDD: ships always act). Same type refreshes.</summary>
        public void ApplyStatus(StatusInstance status)
        {
            if (IsImmuneTo(status.Effect))
                return;

            for (int i = 0; i < StatusEffects.Count; i++)
            {
                if (StatusEffects[i].Effect == status.Effect)
                {
                    StatusEffects[i].RemainingTurns = status.RemainingTurns;
                    StatusEffects[i].Param = status.Param;
                    return;
                }
            }
            StatusEffects.Add(status);
        }

        public bool RemoveStatus(StatusEffect effect)
        {
            for (int i = 0; i < StatusEffects.Count; i++)
            {
                if (StatusEffects[i].Effect == effect)
                {
                    StatusEffects.RemoveAt(i);
                    return true;
                }
            }
            return false;
        }

        public List<StatusEffect> TickStatuses()
        {
            var expired = new List<StatusEffect>();
            for (int i = StatusEffects.Count - 1; i >= 0; i--)
            {
                StatusEffects[i].RemainingTurns--;
                if (StatusEffects[i].RemainingTurns <= 0)
                {
                    expired.Add(StatusEffects[i].Effect);
                    StatusEffects.RemoveAt(i);
                }
            }
            return expired;
        }

        // ====================================================================
        // COOLDOWNS (same model as land units)
        // ====================================================================

        public bool IsAbilityReady(AbilityData ability)
        {
            if (ability == null) return false;
            if (CurrentMP < ability.MPCost) return false;
            if (_cooldowns.TryGetValue(ability.Id, out int remaining) && remaining > 0) return false;
            if (HasStatus(StatusEffect.Silencio)) return false;
            return true;
        }

        public void ActivateCooldown(AbilityData ability)
        {
            if (ability != null && ability.Cooldown > 0)
                _cooldowns[ability.Id] = ability.Cooldown;
        }

        public int GetCooldownRemaining(AbilityData ability)
        {
            if (ability == null) return 0;
            return _cooldowns.TryGetValue(ability.Id, out int remaining) ? remaining : 0;
        }

        public void TickCooldowns()
        {
            var expired = new List<string>();
            foreach (var kvp in _cooldowns)
                if (kvp.Value <= 1)
                    expired.Add(kvp.Key);
            foreach (var key in expired)
                _cooldowns.Remove(key);

            var keys = new List<string>(_cooldowns.Keys);
            foreach (var key in keys)
                _cooldowns[key] = _cooldowns[key] - 1;
        }

        // ====================================================================
        // STAT TYPE MAPPING (ICombatant bridge)
        // ====================================================================

        private static ShipStatType MapToShipStatType(StatType stat)
        {
            return stat switch
            {
                StatType.HP  => ShipStatType.HHP,
                StatType.MP  => ShipStatType.MP,
                StatType.ATK => ShipStatType.FPW,
                StatType.DEF => ShipStatType.HDF,
                StatType.MST => ShipStatType.MST,
                StatType.SPR => ShipStatType.RSL,
                StatType.SPD => ShipStatType.SPD,
                _ => ShipStatType.HHP
            };
        }

        private static StatType MapToUnitStatType(ShipStatType stat)
        {
            return stat switch
            {
                ShipStatType.HHP => StatType.HP,
                ShipStatType.MP  => StatType.MP,
                ShipStatType.FPW => StatType.ATK,
                ShipStatType.HDF => StatType.DEF,
                ShipStatType.MST => StatType.MST,
                ShipStatType.RSL => StatType.SPR,
                ShipStatType.SPD => StatType.SPD,
                _ => StatType.HP
            };
        }
    }
}
