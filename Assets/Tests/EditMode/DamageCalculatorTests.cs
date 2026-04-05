using NUnit.Framework;
using BlacktideRequiem.Core.Combat;
using BlacktideRequiem.Core.Data;

namespace BlacktideRequiem.Tests
{
    /// <summary>
    /// Tests for the Damage & Stats Engine against GDD worked examples and edge cases.
    /// See design/gdd/damage-stats-engine.md § Formulas.
    /// </summary>
    public class DamageCalculatorTests
    {
        // --- GDD Worked Example: Early Game (Land) ---

        [Test]
        public void EarlyGame_NoCrit_MatchesGDD()
        {
            // ATK=130, DEF=110, Ability=1.5, Acero vs Bestia (advantage), no crit, variance=1.02
            // RawDamage = max(130*1.8 - 110*1.0, 1) = max(234-110,1) = 124
            // Final = floor(124 * 1.5 * 1.25 * 1.0 * 1.02) = floor(237.15) = 237
            var result = DamageCalculator.CalculateDeterministic(
                effectiveAtk: 130f, effectiveDef: 110f,
                abilityPower: 1.5f,
                abilityElement: Element.Acero, targetElement: Element.Bestia,
                isCrit: false, effectiveCRI: 12f,
                variance: 1.02f);

            Assert.AreEqual(124, result.RawDamage);
            Assert.AreEqual(237, result.FinalDamage);
            Assert.IsFalse(result.IsCritical);
            Assert.AreEqual(1.25f, result.ElementMod);
        }

        [Test]
        public void EarlyGame_WithCrit_MatchesGDD()
        {
            // Same inputs but with crit: CritMod=1.5 (12% CRI, no overflow)
            // Final = floor(124 * 1.5 * 1.25 * 1.5 * 1.02) = floor(355.725) = 355
            var result = DamageCalculator.CalculateDeterministic(
                effectiveAtk: 130f, effectiveDef: 110f,
                abilityPower: 1.5f,
                abilityElement: Element.Acero, targetElement: Element.Bestia,
                isCrit: true, effectiveCRI: 12f,
                variance: 1.02f);

            Assert.AreEqual(355, result.FinalDamage);
            Assert.IsTrue(result.IsCritical);
            Assert.AreEqual(1.5f, result.CritMod);
        }

        // --- GDD Worked Example: Late Game (Land) ---

        [Test]
        public void LateGame_WithBuffAndCrit_MatchesGDD()
        {
            // EffectiveATK=1105 (850*1.3), DEF=600, Ability=3.5, Polvora vs Acero, crit, variance=0.98
            // RawDamage = round(1105*1.8 - 600*1.0) = round(1989-600) = 1389
            // Final = floor(1389 * 3.5 * 1.25 * 1.5 * 0.98) = floor(8933.006) = 8933
            // Note: GDD shows 8924 but has an arithmetic error in the worked example.
            var result = DamageCalculator.CalculateDeterministic(
                effectiveAtk: 1105f, effectiveDef: 600f,
                abilityPower: 3.5f,
                abilityElement: Element.Polvora, targetElement: Element.Acero,
                isCrit: true, effectiveCRI: 45f,
                variance: 0.98f);

            Assert.AreEqual(1389, result.RawDamage);
            Assert.AreEqual(8933, result.FinalDamage);
        }

        [Test]
        public void LateGame_NoCrit_MatchesGDD()
        {
            // Same but no crit: floor(1389 * 3.5 * 1.25 * 1.0 * 0.98) = floor(5955.34) = 5955
            // Note: GDD shows 5949 but has an arithmetic error in the worked example.
            var result = DamageCalculator.CalculateDeterministic(
                effectiveAtk: 1105f, effectiveDef: 600f,
                abilityPower: 3.5f,
                abilityElement: Element.Polvora, targetElement: Element.Acero,
                isCrit: false, effectiveCRI: 45f,
                variance: 0.98f);

            Assert.AreEqual(5955, result.FinalDamage);
        }

        // --- GDD Worked Example: Naval Combat ---

        [Test]
        public void Naval_NoCrit_MatchesGDD()
        {
            // FPW=193, HDF=150, Ability=2.0, Polvora vs Acero, no crit (ships don't crit), variance=1.03
            // RawDamage = max(193*1.8 - 150*1.0, 1) = max(347.4-150,1) = 197
            // Final = floor(197 * 2.0 * 1.25 * 1.0 * 1.03) = floor(507.325) = 507
            var result = DamageCalculator.CalculateDeterministic(
                effectiveAtk: 193f, effectiveDef: 150f,
                abilityPower: 2.0f,
                abilityElement: Element.Polvora, targetElement: Element.Acero,
                isCrit: false, effectiveCRI: 0f,
                variance: 1.03f);

            Assert.AreEqual(197, result.RawDamage);
            Assert.AreEqual(507, result.FinalDamage);
        }

        // --- Minimum damage ---

        [Test]
        public void MinimumDamage_AlwaysAtLeast1()
        {
            // ATK=1, DEF=9999 → RawDamage clamped to 1
            var result = DamageCalculator.CalculateDeterministic(
                effectiveAtk: 1f, effectiveDef: 9999f,
                abilityPower: 1.0f,
                abilityElement: Element.Neutral, targetElement: Element.Neutral,
                isCrit: false, effectiveCRI: 0f);

            Assert.AreEqual(1, result.RawDamage);
            Assert.AreEqual(1, result.FinalDamage);
        }

        // --- CRI overflow ---

        [Test]
        public void CritOverflow_150CRI_GivesBonusDamage()
        {
            // CRI 150%: bonus = floor((150-100)/50) * 0.01 = floor(1) * 0.01 = 0.01
            // CritMod = 1.50 + 0.01 = 1.51
            float bonus = DamageCalculator.GetCritBonusDamage(150f);
            Assert.AreEqual(0.01f, bonus, 0.001f);
        }

        [Test]
        public void CritOverflow_200CRI_MatchesGDD()
        {
            // CRI 200%: bonus = floor((200-100)/50) * 0.01 = floor(2) * 0.01 = 0.02
            // CritMod = 1.50 + 0.02 = 1.52
            float bonus = DamageCalculator.GetCritBonusDamage(200f);
            Assert.AreEqual(0.02f, bonus, 0.001f);
        }

        [Test]
        public void CritOverflow_350CRI_MatchesGDD()
        {
            // CRI 350%: bonus = floor((350-100)/50) * 0.01 = floor(5) * 0.01 = 0.05
            // CritMod = 1.50 + 0.05 = 1.55
            float bonus = DamageCalculator.GetCritBonusDamage(350f);
            Assert.AreEqual(0.05f, bonus, 0.001f);
        }

        [Test]
        public void CritOverflow_Below100_NoBonusDamage()
        {
            Assert.AreEqual(0f, DamageCalculator.GetCritBonusDamage(50f));
            Assert.AreEqual(0f, DamageCalculator.GetCritBonusDamage(100f));
        }
    }

    /// <summary>
    /// Tests for the elemental advantage table.
    /// </summary>
    public class ElementTableTests
    {
        // --- Pentagonal cycle ---

        [Test]
        public void PentagonalCycle_AdvantageChain()
        {
            // Polvora → Acero → Bestia → Maldicion → Tormenta → Polvora
            Assert.AreEqual(1.25f, ElementTable.GetElementMod(Element.Polvora, Element.Acero));
            Assert.AreEqual(1.25f, ElementTable.GetElementMod(Element.Acero, Element.Bestia));
            Assert.AreEqual(1.25f, ElementTable.GetElementMod(Element.Bestia, Element.Maldicion));
            Assert.AreEqual(1.25f, ElementTable.GetElementMod(Element.Maldicion, Element.Tormenta));
            Assert.AreEqual(1.25f, ElementTable.GetElementMod(Element.Tormenta, Element.Polvora));
        }

        [Test]
        public void PentagonalCycle_DisadvantageChain()
        {
            Assert.AreEqual(0.75f, ElementTable.GetElementMod(Element.Acero, Element.Polvora));
            Assert.AreEqual(0.75f, ElementTable.GetElementMod(Element.Bestia, Element.Acero));
            Assert.AreEqual(0.75f, ElementTable.GetElementMod(Element.Maldicion, Element.Bestia));
            Assert.AreEqual(0.75f, ElementTable.GetElementMod(Element.Tormenta, Element.Maldicion));
            Assert.AreEqual(0.75f, ElementTable.GetElementMod(Element.Polvora, Element.Tormenta));
        }

        // --- Luz / Sombra ---

        [Test]
        public void LuzSombra_MutualAdvantage()
        {
            Assert.AreEqual(1.25f, ElementTable.GetElementMod(Element.Luz, Element.Sombra));
            Assert.AreEqual(1.25f, ElementTable.GetElementMod(Element.Sombra, Element.Luz));
        }

        [Test]
        public void LuzSombra_VsPentagonal_IsNeutral()
        {
            Assert.AreEqual(1.0f, ElementTable.GetElementMod(Element.Luz, Element.Polvora));
            Assert.AreEqual(1.0f, ElementTable.GetElementMod(Element.Sombra, Element.Acero));
            Assert.AreEqual(1.0f, ElementTable.GetElementMod(Element.Bestia, Element.Luz));
            Assert.AreEqual(1.0f, ElementTable.GetElementMod(Element.Tormenta, Element.Sombra));
        }

        // --- Same element ---

        [Test]
        public void SameElement_IsNeutral()
        {
            Assert.AreEqual(1.0f, ElementTable.GetElementMod(Element.Polvora, Element.Polvora));
            Assert.AreEqual(1.0f, ElementTable.GetElementMod(Element.Luz, Element.Luz));
            Assert.AreEqual(1.0f, ElementTable.GetElementMod(Element.Sombra, Element.Sombra));
        }

        // --- Neutral ---

        [Test]
        public void NeutralAbility_AlwaysNeutral()
        {
            Assert.AreEqual(1.0f, ElementTable.GetElementMod(Element.Neutral, Element.Polvora));
            Assert.AreEqual(1.0f, ElementTable.GetElementMod(Element.Neutral, Element.Luz));
            Assert.AreEqual(1.0f, ElementTable.GetElementMod(Element.Neutral, Element.Sombra));
        }

        [Test]
        public void NeutralTarget_AlwaysNeutral()
        {
            Assert.AreEqual(1.0f, ElementTable.GetElementMod(Element.Polvora, Element.Neutral));
            Assert.AreEqual(1.0f, ElementTable.GetElementMod(Element.Luz, Element.Neutral));
        }

        // --- Non-adjacent pentagonal elements are neutral ---

        [Test]
        public void NonAdjacentPentagonal_IsNeutral()
        {
            // Polvora vs Bestia: not adjacent in cycle
            Assert.AreEqual(1.0f, ElementTable.GetElementMod(Element.Polvora, Element.Bestia));
            Assert.AreEqual(1.0f, ElementTable.GetElementMod(Element.Polvora, Element.Maldicion));
            Assert.AreEqual(1.0f, ElementTable.GetElementMod(Element.Acero, Element.Tormenta));
        }
    }

    /// <summary>
    /// Tests for buff stack mechanics.
    /// </summary>
    public class BuffStackTests
    {
        [Test]
        public void BuffsStackAdditively()
        {
            var stack = new BuffStack();
            stack.Add(new BuffInstance { StatAffected = StatType.ATK, Percentage = 0.20f, RemainingTurns = 3, IsDispellable = true });
            stack.Add(new BuffInstance { StatAffected = StatType.ATK, Percentage = 0.15f, RemainingTurns = 2, IsDispellable = true });

            // 1.0 + 0.20 + 0.15 = 1.35
            Assert.AreEqual(1.35f, stack.GetStatModifier(StatType.ATK), 0.001f);
        }

        [Test]
        public void BuffModCap_At200Percent()
        {
            var stack = new BuffStack();
            stack.Add(new BuffInstance { StatAffected = StatType.ATK, Percentage = 1.20f, RemainingTurns = 3, IsDispellable = true });

            // 1.0 + 1.20 = 2.20 → clamped to 2.0
            Assert.AreEqual(2.0f, stack.GetStatModifier(StatType.ATK), 0.001f);
        }

        [Test]
        public void DebuffFloor_At0Percent()
        {
            var stack = new BuffStack();
            stack.Add(new BuffInstance { StatAffected = StatType.DEF, Percentage = -1.20f, RemainingTurns = 3, IsDispellable = true });

            // 1.0 + (-1.20) = -0.20 → clamped to 0.0
            Assert.AreEqual(0.0f, stack.GetStatModifier(StatType.DEF), 0.001f);
        }

        [Test]
        public void OpposingBuffsNetCorrectly()
        {
            var stack = new BuffStack();
            stack.Add(new BuffInstance { StatAffected = StatType.ATK, Percentage = 0.30f, RemainingTurns = 3, IsDispellable = true });
            stack.Add(new BuffInstance { StatAffected = StatType.ATK, Percentage = -0.20f, RemainingTurns = 2, IsDispellable = true });

            // 1.0 + 0.30 - 0.20 = 1.10
            Assert.AreEqual(1.10f, stack.GetStatModifier(StatType.ATK), 0.001f);
        }

        [Test]
        public void TickTurns_RemovesExpired()
        {
            var stack = new BuffStack();
            stack.Add(new BuffInstance { StatAffected = StatType.ATK, Percentage = 0.30f, RemainingTurns = 1, IsDispellable = true });
            stack.Add(new BuffInstance { StatAffected = StatType.ATK, Percentage = 0.20f, RemainingTurns = 3, IsDispellable = true });

            stack.TickTurns();

            // First buff expired (was 1 turn), second still active (now 2 turns)
            Assert.AreEqual(1, stack.All.Count);
            Assert.AreEqual(1.20f, stack.GetStatModifier(StatType.ATK), 0.001f);
        }

        [Test]
        public void PermanentBuffs_NotRemovedByTick()
        {
            var stack = new BuffStack();
            stack.Add(new BuffInstance { StatAffected = StatType.DEF, Percentage = 0.20f, RemainingTurns = -1, IsDispellable = false });

            stack.TickTurns();

            Assert.AreEqual(1, stack.All.Count);
            Assert.AreEqual(1.20f, stack.GetStatModifier(StatType.DEF), 0.001f);
        }

        [Test]
        public void DispelBuffs_RemovesOnlyDispellableBuffs()
        {
            var stack = new BuffStack();
            stack.Add(new BuffInstance { StatAffected = StatType.ATK, Percentage = 0.20f, RemainingTurns = -1, IsDispellable = false }); // permanent trait
            stack.Add(new BuffInstance { StatAffected = StatType.ATK, Percentage = 0.30f, RemainingTurns = 3, IsDispellable = true }); // temporary

            int removed = stack.DispelBuffs();

            Assert.AreEqual(1, removed);
            Assert.AreEqual(1.20f, stack.GetStatModifier(StatType.ATK), 0.001f);
        }

        [Test]
        public void DifferentStats_Independent()
        {
            var stack = new BuffStack();
            stack.Add(new BuffInstance { StatAffected = StatType.ATK, Percentage = 0.30f, RemainingTurns = 3, IsDispellable = true });
            stack.Add(new BuffInstance { StatAffected = StatType.DEF, Percentage = -0.20f, RemainingTurns = 2, IsDispellable = true });

            Assert.AreEqual(1.30f, stack.GetStatModifier(StatType.ATK), 0.001f);
            Assert.AreEqual(0.80f, stack.GetStatModifier(StatType.DEF), 0.001f);
            Assert.AreEqual(1.00f, stack.GetStatModifier(StatType.MST), 0.001f); // unaffected
        }
    }

    /// <summary>
    /// Tests for healing calculations.
    /// </summary>
    public class HealCalculatorTests
    {
        [Test]
        public void BasicHeal_CalculatesCorrectly()
        {
            // MST=200, HealPower=2.0 → floor(200*2.0) = 400
            int result = HealCalculator.Calculate(200f, 2.0f);
            Assert.AreEqual(400, result);
        }

        [Test]
        public void Heal_MinimumIs1()
        {
            int result = HealCalculator.Calculate(0.1f, 0.1f);
            Assert.AreEqual(1, result);
        }
    }
}
