using System;
using UnityEngine;
using BlacktideRequiem.Core.Data;

namespace BlacktideRequiem.Core.Combat
{
    /// <summary>
    /// Result of a damage calculation. Contains all info needed for UI display.
    /// </summary>
    public struct DamageResult
    {
        public int FinalDamage;
        public int RawDamage;
        public bool IsCritical;
        public float CritMod;
        public float ElementMod;
        public float Variance;
        public bool IsMiss;
    }

    /// <summary>
    /// Pure calculation class for the master damage formula.
    /// No state, no MonoBehaviour. Receives inputs, returns outputs.
    /// See Damage & Stats Engine GDD §1 (Formulas).
    ///
    /// Formula:
    ///   EffectiveATK = ATK * clamp(1.0 + sum(buffs) - sum(debuffs), 0.0, 2.0)
    ///   EffectiveDEF = DEF * clamp(1.0 + sum(buffs) - sum(debuffs), 0.0, 2.0)
    ///   RawDamage = max(EffectiveATK * ATK_MULT - EffectiveDEF * DEF_MULT, 1)
    ///   FinalDamage = max(floor(RawDamage * AbilityPower * ElementMod * CritMod * Variance), 1)
    /// </summary>
    public static class DamageCalculator
    {
        public const float ATTACK_MULTIPLIER = 1.8f;
        public const float DEFENSE_MULTIPLIER = 1.0f;
        public const float CRIT_BASE_MULTIPLIER = 1.50f;
        public const float CRIT_OVERFLOW_DIVISOR = 50f;
        public const float VARIANCE_MIN = 0.95f;
        public const float VARIANCE_MAX = 1.05f;
        public const float BLIND_MISS_CHANCE = 0.50f;

        /// <summary>
        /// Calculates damage for an attack.
        /// </summary>
        /// <param name="effectiveAtk">Attacker's effective offensive stat (after buffs).</param>
        /// <param name="effectiveDef">Defender's effective defensive stat (after buffs).</param>
        /// <param name="abilityPower">Ability damage multiplier (basic attack = 1.0).</param>
        /// <param name="abilityElement">Offensive element of the ability.</param>
        /// <param name="targetElement">Defensive element of the target.</param>
        /// <param name="effectiveCRI">Attacker's effective CRI (no cap).</param>
        /// <param name="isPhysical">True for physical (ATK/DEF), false for magical (MST/SPR).</param>
        /// <param name="isBlinded">Whether the attacker has Ceguera (physical miss chance).</param>
        /// <param name="variance">Variance value (0.95-1.05). Pass -1 for random.</param>
        public static DamageResult Calculate(
            float effectiveAtk,
            float effectiveDef,
            float abilityPower,
            Element abilityElement,
            Element targetElement,
            float effectiveCRI,
            bool isPhysical,
            bool isBlinded = false,
            float variance = -1f)
        {
            var result = new DamageResult();

            // Blind miss check (physical only)
            if (isBlinded && isPhysical)
            {
                float roll = UnityEngine.Random.value;
                if (roll < BLIND_MISS_CHANCE)
                {
                    result.IsMiss = true;
                    result.FinalDamage = 0;
                    return result;
                }
            }

            // Raw damage (RoundToInt avoids float imprecision with ATTACK_MULTIPLIER 1.8f)
            float rawDamage = effectiveAtk * ATTACK_MULTIPLIER - effectiveDef * DEFENSE_MULTIPLIER;
            result.RawDamage = Mathf.Max(Mathf.RoundToInt(rawDamage), 1);

            // Element modifier
            result.ElementMod = ElementTable.GetElementMod(abilityElement, targetElement);

            // Critical hit
            result.CritMod = CalculateCritMod(effectiveCRI, out bool isCrit);
            result.IsCritical = isCrit;

            // Variance
            if (variance < 0f)
                result.Variance = UnityEngine.Random.Range(VARIANCE_MIN, VARIANCE_MAX);
            else
                result.Variance = variance;

            // Final damage
            float finalDamage = result.RawDamage * abilityPower * result.ElementMod * result.CritMod * result.Variance;
            result.FinalDamage = Mathf.Max(Mathf.FloorToInt(finalDamage), 1);

            return result;
        }

        /// <summary>
        /// Deterministic damage calculation (no randomness). For tests and AI evaluation.
        /// </summary>
        public static DamageResult CalculateDeterministic(
            float effectiveAtk,
            float effectiveDef,
            float abilityPower,
            Element abilityElement,
            Element targetElement,
            bool isCrit,
            float effectiveCRI,
            float variance = 1.0f)
        {
            var result = new DamageResult();

            float rawDamage = effectiveAtk * ATTACK_MULTIPLIER - effectiveDef * DEFENSE_MULTIPLIER;
            result.RawDamage = Mathf.Max(Mathf.RoundToInt(rawDamage), 1);

            result.ElementMod = ElementTable.GetElementMod(abilityElement, targetElement);

            if (isCrit)
            {
                result.IsCritical = true;
                result.CritMod = CRIT_BASE_MULTIPLIER + GetCritBonusDamage(effectiveCRI);
            }
            else
            {
                result.CritMod = 1.0f;
            }

            result.Variance = variance;

            float finalDamage = result.RawDamage * abilityPower * result.ElementMod * result.CritMod * result.Variance;
            result.FinalDamage = Mathf.Max(Mathf.FloorToInt(finalDamage), 1);

            return result;
        }

        /// <summary>
        /// Calculates CritMod. Returns whether the hit is critical.
        /// </summary>
        private static float CalculateCritMod(float effectiveCRI, out bool isCrit)
        {
            float roll = UnityEngine.Random.Range(0f, 100f);
            if (roll < effectiveCRI)
            {
                isCrit = true;
                return CRIT_BASE_MULTIPLIER + GetCritBonusDamage(effectiveCRI);
            }
            isCrit = false;
            return 1.0f;
        }

        /// <summary>
        /// Calculates bonus crit damage from CRI overflow (above 100%).
        /// Every CRIT_OVERFLOW_DIVISOR points above 100 = +1% crit damage.
        /// </summary>
        public static float GetCritBonusDamage(float effectiveCRI)
        {
            if (effectiveCRI <= 100f) return 0f;
            return Mathf.Floor((effectiveCRI - 100f) / CRIT_OVERFLOW_DIVISOR) * 0.01f;
        }
    }
}
