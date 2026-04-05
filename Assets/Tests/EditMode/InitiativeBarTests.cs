using System.Collections.Generic;
using NUnit.Framework;
using BlacktideRequiem.Core.Combat;
using BlacktideRequiem.Core.Data;

namespace BlacktideRequiem.Tests.EditMode
{
    /// <summary>
    /// Tests for Initiative Bar turn ordering system.
    /// See Initiative Bar GDD for specification.
    /// </summary>
    [TestFixture]
    public class InitiativeBarTests
    {
        private InitiativeBar _bar;

        [SetUp]
        public void SetUp()
        {
            _bar = new InitiativeBar();
        }

        // --- Helper Methods ---

        private static CombatantState MakeCombatant(float spd, bool isBoss = false)
        {
            var data = UnityEngine.ScriptableObject.CreateInstance<CharacterData>();
            data.Id = $"test_{spd}";
            var stats = new StatBlock { HP = 100, MP = 50, ATK = 30, DEF = 20, MST = 25, SPR = 15, SPD = spd };
            var state = new CombatantState(data, stats, 1) { IsBoss = isBoss };
            return state;
        }

        private static InitiativeEntry MakeEntry(CombatantState combatant, CombatTeam team, int slot)
        {
            return new InitiativeEntry(combatant, team, slot);
        }

        private List<InitiativeEntry> MakeParty(params (float spd, CombatTeam team, int slot, bool boss)[] specs)
        {
            var entries = new List<InitiativeEntry>();
            foreach (var (spd, team, slot, boss) in specs)
            {
                var combatant = MakeCombatant(spd, boss);
                entries.Add(MakeEntry(combatant, team, slot));
            }
            return entries;
        }

        // ===================================================================
        // Basic SPD Ordering
        // ===================================================================

        [Test]
        public void BeginRound_SortsBySpdDescending()
        {
            var entries = MakeParty(
                (50, CombatTeam.Ally, 0, false),
                (80, CombatTeam.Ally, 1, false),
                (65, CombatTeam.Ally, 2, false),
                (90, CombatTeam.Enemy, 0, false),
                (40, CombatTeam.Enemy, 1, false),
                (70, CombatTeam.Enemy, 2, false)
            );

            _bar.BeginRound(entries);

            var queued = _bar.GetQueuedEntries();
            Assert.AreEqual(6, queued.Count);
            Assert.AreEqual(90f, queued[0].Combatant.BaseStats.SPD);
            Assert.AreEqual(80f, queued[1].Combatant.BaseStats.SPD);
            Assert.AreEqual(70f, queued[2].Combatant.BaseStats.SPD);
            Assert.AreEqual(65f, queued[3].Combatant.BaseStats.SPD);
            Assert.AreEqual(50f, queued[4].Combatant.BaseStats.SPD);
            Assert.AreEqual(40f, queued[5].Combatant.BaseStats.SPD);
        }

        [Test]
        public void BeginRound_IncrementsRoundCounter()
        {
            var entries = MakeParty((50, CombatTeam.Ally, 0, false));

            _bar.BeginRound(entries);
            Assert.AreEqual(1, _bar.RoundNumber);

            _bar.BeginRound(entries);
            Assert.AreEqual(2, _bar.RoundNumber);
        }

        [Test]
        public void BeginRound_ExcludesDeadCombatants()
        {
            var alive = MakeCombatant(80);
            var dead = MakeCombatant(90);
            dead.ApplyDamage(dead.MaxHP); // Kill

            var entries = new List<InitiativeEntry>
            {
                MakeEntry(alive, CombatTeam.Ally, 0),
                MakeEntry(dead, CombatTeam.Ally, 1)
            };

            _bar.BeginRound(entries);

            Assert.AreEqual(1, _bar.GetQueuedEntries().Count);
            Assert.AreEqual(alive, _bar.GetQueuedEntries()[0].Combatant);
        }

        // ===================================================================
        // Tie-Breaking
        // ===================================================================

        [Test]
        public void TieBreak_BossActsBeforeNonBoss()
        {
            var entries = MakeParty(
                (70, CombatTeam.Ally, 0, false),
                (70, CombatTeam.Enemy, 0, true) // Boss
            );

            _bar.BeginRound(entries);

            var queued = _bar.GetQueuedEntries();
            Assert.IsTrue(queued[0].Combatant.IsBoss, "Boss should act first on tie");
        }

        [Test]
        public void TieBreak_AllyBeforeNonBossEnemy()
        {
            var entries = MakeParty(
                (70, CombatTeam.Enemy, 0, false),
                (70, CombatTeam.Ally, 0, false)
            );

            _bar.BeginRound(entries);

            var queued = _bar.GetQueuedEntries();
            Assert.AreEqual(CombatTeam.Ally, queued[0].Team, "Ally should act before non-boss enemy on tie");
        }

        [Test]
        public void TieBreak_LowerSlotFirst_SameTeam()
        {
            var entries = MakeParty(
                (70, CombatTeam.Ally, 2, false),
                (70, CombatTeam.Ally, 0, false),
                (70, CombatTeam.Ally, 1, false)
            );

            _bar.BeginRound(entries);

            var queued = _bar.GetQueuedEntries();
            Assert.AreEqual(0, queued[0].SlotIndex);
            Assert.AreEqual(1, queued[1].SlotIndex);
            Assert.AreEqual(2, queued[2].SlotIndex);
        }

        [Test]
        public void TieBreak_BossEnemyBeforeAlly()
        {
            // GDD: Bosses act first, even over allies
            var entries = MakeParty(
                (70, CombatTeam.Ally, 0, false),
                (70, CombatTeam.Enemy, 0, true) // Boss enemy
            );

            _bar.BeginRound(entries);

            var first = _bar.AdvanceTurn();
            Assert.IsTrue(first.Combatant.IsBoss, "Boss enemy should beat ally on SPD tie");
        }

        // ===================================================================
        // Turn Advancement
        // ===================================================================

        [Test]
        public void AdvanceTurn_ReturnsUnitsInOrder()
        {
            var entries = MakeParty(
                (90, CombatTeam.Ally, 0, false),
                (60, CombatTeam.Enemy, 0, false),
                (30, CombatTeam.Ally, 1, false)
            );

            _bar.BeginRound(entries);

            var first = _bar.AdvanceTurn();
            Assert.AreEqual(90f, first.Combatant.BaseStats.SPD);
            Assert.AreEqual(TurnState.Active, first.State);

            _bar.CompleteCurrentTurn();
            var second = _bar.AdvanceTurn();
            Assert.AreEqual(60f, second.Combatant.BaseStats.SPD);

            _bar.CompleteCurrentTurn();
            var third = _bar.AdvanceTurn();
            Assert.AreEqual(30f, third.Combatant.BaseStats.SPD);
        }

        [Test]
        public void AdvanceTurn_ReturnsNull_WhenRoundOver()
        {
            var entries = MakeParty((50, CombatTeam.Ally, 0, false));

            _bar.BeginRound(entries);

            var first = _bar.AdvanceTurn();
            Assert.IsNotNull(first);

            _bar.CompleteCurrentTurn();
            var second = _bar.AdvanceTurn();
            Assert.IsNull(second);
        }

        [Test]
        public void IsRoundOver_TrueWhenAllActed()
        {
            var entries = MakeParty(
                (80, CombatTeam.Ally, 0, false),
                (60, CombatTeam.Enemy, 0, false)
            );

            _bar.BeginRound(entries);
            Assert.IsFalse(_bar.IsRoundOver);

            _bar.AdvanceTurn();
            _bar.CompleteCurrentTurn();
            Assert.IsFalse(_bar.IsRoundOver);

            _bar.AdvanceTurn();
            _bar.CompleteCurrentTurn();
            Assert.IsTrue(_bar.IsRoundOver);
        }

        // ===================================================================
        // Mid-Round Reorder
        // ===================================================================

        [Test]
        public void Reorder_ResortsQueuedAfterSpdBuff()
        {
            var entries = MakeParty(
                (90, CombatTeam.Ally, 0, false),  // Acts first
                (50, CombatTeam.Ally, 1, false),  // Slow ally
                (70, CombatTeam.Enemy, 0, false)   // Medium enemy
            );

            _bar.BeginRound(entries);

            // First unit acts
            var first = _bar.AdvanceTurn();
            Assert.AreEqual(90f, first.Combatant.BaseStats.SPD);
            _bar.CompleteCurrentTurn();

            // Buff the slow ally's SPD to overtake enemy
            var slowAlly = entries[1].Combatant;
            slowAlly.Buffs.Add(new BuffInstance
            {
                StatAffected = StatType.SPD,
                Percentage = 1.0f, // +100% -> effective 100
                RemainingTurns = 3,
                IsDispellable = true
            });

            _bar.Reorder();

            // Slow ally (now effective 100) should act before enemy (70)
            var next = _bar.AdvanceTurn();
            Assert.AreEqual(slowAlly, next.Combatant, "Buffed ally should now act before enemy");
        }

        [Test]
        public void Reorder_DoesNotAffectActedEntries()
        {
            var entries = MakeParty(
                (90, CombatTeam.Ally, 0, false),
                (70, CombatTeam.Enemy, 0, false),
                (50, CombatTeam.Ally, 1, false)
            );

            _bar.BeginRound(entries);

            // First two units act
            _bar.AdvanceTurn();
            _bar.CompleteCurrentTurn();
            _bar.AdvanceTurn();
            _bar.CompleteCurrentTurn();

            // Buff already-acted unit's SPD
            entries[0].Combatant.Buffs.Add(new BuffInstance
            {
                StatAffected = StatType.SPD,
                Percentage = -0.5f,
                RemainingTurns = 3,
                IsDispellable = true
            });

            _bar.Reorder();

            // Third unit should still be next (no change)
            var next = _bar.AdvanceTurn();
            Assert.AreEqual(entries[2].Combatant, next.Combatant);
        }

        [Test]
        public void Reorder_FiresEvent()
        {
            var entries = MakeParty(
                (80, CombatTeam.Ally, 0, false),
                (60, CombatTeam.Enemy, 0, false)
            );

            _bar.BeginRound(entries);

            bool eventFired = false;
            _bar.OnReorder += () => eventFired = true;

            _bar.Reorder();

            Assert.IsTrue(eventFired);
        }

        // ===================================================================
        // Limit Break
        // ===================================================================

        [Test]
        public void LimitBreak_InsertsExtraTurnAfterCurrent()
        {
            var entries = MakeParty(
                (90, CombatTeam.Ally, 0, false),
                (70, CombatTeam.Enemy, 0, false),
                (50, CombatTeam.Ally, 1, false)
            );

            _bar.BeginRound(entries);

            var first = _bar.AdvanceTurn();

            // Trigger Limit Break for the active unit
            bool result = _bar.InsertLimitBreak(first.Combatant);
            Assert.IsTrue(result);

            _bar.CompleteCurrentTurn();

            // Next should be the Limit Break extra turn
            var next = _bar.AdvanceTurn();
            Assert.AreEqual(first.Combatant, next.Combatant);
            Assert.IsTrue(next.IsLimitBreak);
        }

        [Test]
        public void LimitBreak_MaxOnePerUnitPerRound()
        {
            var entries = MakeParty(
                (90, CombatTeam.Ally, 0, false),
                (50, CombatTeam.Enemy, 0, false)
            );

            _bar.BeginRound(entries);

            var first = _bar.AdvanceTurn();

            Assert.IsTrue(_bar.InsertLimitBreak(first.Combatant));
            Assert.IsFalse(_bar.InsertLimitBreak(first.Combatant), "Second Limit Break should be rejected");
        }

        [Test]
        public void LimitBreak_FailsOnDeadUnit()
        {
            var entries = MakeParty(
                (90, CombatTeam.Ally, 0, false),
                (50, CombatTeam.Enemy, 0, false)
            );

            _bar.BeginRound(entries);
            _bar.AdvanceTurn();

            // Kill the enemy
            entries[1].Combatant.ApplyDamage(entries[1].Combatant.MaxHP);

            Assert.IsFalse(_bar.InsertLimitBreak(entries[1].Combatant));
        }

        [Test]
        public void LimitBreak_FailsOnStunnedUnit()
        {
            var entries = MakeParty(
                (90, CombatTeam.Ally, 0, false),
                (50, CombatTeam.Enemy, 0, false)
            );

            _bar.BeginRound(entries);
            _bar.AdvanceTurn();

            // Stun the enemy
            entries[1].Combatant.ApplyStatus(new StatusInstance
            {
                Effect = StatusEffect.Aturdimiento,
                RemainingTurns = 1
            });

            Assert.IsFalse(_bar.InsertLimitBreak(entries[1].Combatant));
        }

        [Test]
        public void LimitBreak_LastUnitGetsExtraTurn()
        {
            // GDD edge case: Limit Break on last icon still works
            var entries = MakeParty((90, CombatTeam.Ally, 0, false));

            _bar.BeginRound(entries);

            var first = _bar.AdvanceTurn();
            _bar.InsertLimitBreak(first.Combatant);
            _bar.CompleteCurrentTurn();

            Assert.IsFalse(_bar.IsRoundOver, "Extra turn should keep the round going");

            var extra = _bar.AdvanceTurn();
            Assert.IsNotNull(extra);
            Assert.AreEqual(first.Combatant, extra.Combatant);
        }

        [Test]
        public void LimitBreak_ResetsNextRound()
        {
            var entries = MakeParty(
                (90, CombatTeam.Ally, 0, false),
                (50, CombatTeam.Enemy, 0, false)
            );

            _bar.BeginRound(entries);

            var first = _bar.AdvanceTurn();
            _bar.InsertLimitBreak(first.Combatant);

            // Second Limit Break same round should fail
            Assert.IsFalse(_bar.InsertLimitBreak(first.Combatant));

            // New round — should be able to Limit Break again
            _bar.BeginRound(entries);
            _bar.AdvanceTurn();
            Assert.IsTrue(_bar.InsertLimitBreak(entries[0].Combatant));
        }

        [Test]
        public void LimitBreak_FiresEvent()
        {
            var entries = MakeParty((90, CombatTeam.Ally, 0, false));

            _bar.BeginRound(entries);
            _bar.AdvanceTurn();

            InitiativeEntry insertedEntry = null;
            _bar.OnLimitBreakInserted += e => insertedEntry = e;

            _bar.InsertLimitBreak(entries[0].Combatant);

            Assert.IsNotNull(insertedEntry);
            Assert.IsTrue(insertedEntry.IsLimitBreak);
        }

        // ===================================================================
        // Status Effects: Stun
        // ===================================================================

        [Test]
        public void Stun_SkipsTurnAndGrantsCCImmunity()
        {
            var entries = MakeParty(
                (90, CombatTeam.Ally, 0, false),
                (70, CombatTeam.Enemy, 0, false)
            );

            _bar.BeginRound(entries);

            // Stun the first unit
            entries[0].Combatant.ApplyStatus(new StatusInstance
            {
                Effect = StatusEffect.Aturdimiento,
                RemainingTurns = 1
            });

            var next = _bar.AdvanceTurn();

            // Stunned unit should be skipped, enemy acts
            Assert.AreEqual(entries[1].Combatant, next.Combatant, "Stunned unit should be skipped");
            Assert.AreEqual(InitiativeBar.CC_IMMUNITY_DURATION, entries[0].Combatant.CCImmunityTurns);
        }

        [Test]
        public void Stun_RemovesStunStatusAfterSkip()
        {
            var entries = MakeParty((90, CombatTeam.Ally, 0, false));

            _bar.BeginRound(entries);

            entries[0].Combatant.ApplyStatus(new StatusInstance
            {
                Effect = StatusEffect.Aturdimiento,
                RemainingTurns = 1
            });

            _bar.AdvanceTurn(); // Skips stunned unit

            Assert.IsFalse(entries[0].Combatant.HasStatus(StatusEffect.Aturdimiento),
                "Stun should be removed after skip");
        }

        [Test]
        public void Stun_FiresSkipEvent()
        {
            var entries = MakeParty(
                (90, CombatTeam.Ally, 0, false),
                (50, CombatTeam.Enemy, 0, false)
            );

            _bar.BeginRound(entries);

            entries[0].Combatant.ApplyStatus(new StatusInstance
            {
                Effect = StatusEffect.Aturdimiento,
                RemainingTurns = 1
            });

            InitiativeEntry skippedEntry = null;
            _bar.OnTurnSkipped += e => skippedEntry = e;

            _bar.AdvanceTurn();

            Assert.IsNotNull(skippedEntry);
            Assert.AreEqual(entries[0].Combatant, skippedEntry.Combatant);
        }

        // ===================================================================
        // Status Effects: Sleep
        // ===================================================================

        [Test]
        public void Sleep_SkipsTurn()
        {
            var entries = MakeParty(
                (90, CombatTeam.Ally, 0, false),
                (70, CombatTeam.Enemy, 0, false)
            );

            _bar.BeginRound(entries);

            entries[0].Combatant.ApplyStatus(new StatusInstance
            {
                Effect = StatusEffect.Sueno,
                RemainingTurns = 2
            });

            var next = _bar.AdvanceTurn();

            Assert.AreEqual(entries[1].Combatant, next.Combatant, "Sleeping unit should be skipped");
        }

        [Test]
        public void Sleep_DoesNotRemoveStatusOnSkip()
        {
            // Sleep persists until damage, unlike stun which is consumed
            var entries = MakeParty((90, CombatTeam.Ally, 0, false));

            _bar.BeginRound(entries);

            entries[0].Combatant.ApplyStatus(new StatusInstance
            {
                Effect = StatusEffect.Sueno,
                RemainingTurns = 2
            });

            _bar.AdvanceTurn(); // Skips sleeping unit

            Assert.IsTrue(entries[0].Combatant.HasStatus(StatusEffect.Sueno),
                "Sleep should persist after skip (removed by damage, not by skipping)");
        }

        // ===================================================================
        // Death & Revive
        // ===================================================================

        [Test]
        public void RemoveDead_RemovesQueuedUnit()
        {
            var entries = MakeParty(
                (90, CombatTeam.Ally, 0, false),
                (70, CombatTeam.Enemy, 0, false),
                (50, CombatTeam.Ally, 1, false)
            );

            _bar.BeginRound(entries);

            var first = _bar.AdvanceTurn();

            // Kill the enemy mid-round
            _bar.RemoveDead(entries[1].Combatant);

            _bar.CompleteCurrentTurn();
            var next = _bar.AdvanceTurn();

            // Should skip to third entry (enemy is dead)
            Assert.AreEqual(entries[2].Combatant, next.Combatant);
        }

        [Test]
        public void InsertRevived_PlacesAtEndOfBar()
        {
            var entries = MakeParty(
                (90, CombatTeam.Ally, 0, false),
                (70, CombatTeam.Enemy, 0, false)
            );

            _bar.BeginRound(entries);

            // Revive a new combatant with very high SPD
            var revived = MakeCombatant(999);
            _bar.InsertRevived(revived, CombatTeam.Ally, 2);

            // Advance through all turns — revived should be last
            var first = _bar.AdvanceTurn();
            Assert.AreEqual(90f, first.Combatant.BaseStats.SPD);
            _bar.CompleteCurrentTurn();

            var second = _bar.AdvanceTurn();
            Assert.AreEqual(70f, second.Combatant.BaseStats.SPD);
            _bar.CompleteCurrentTurn();

            var third = _bar.AdvanceTurn();
            Assert.AreEqual(revived, third.Combatant, "Revived unit should act last regardless of SPD");
        }

        // ===================================================================
        // Edge Cases (from GDD)
        // ===================================================================

        [Test]
        public void ZeroSpd_UnitActsLast_NotSkipped()
        {
            var entries = MakeParty(
                (70, CombatTeam.Ally, 0, false),
                (0, CombatTeam.Enemy, 0, false),
                (50, CombatTeam.Ally, 1, false)
            );

            _bar.BeginRound(entries);

            _bar.AdvanceTurn(); // 70 SPD
            _bar.CompleteCurrentTurn();
            _bar.AdvanceTurn(); // 50 SPD
            _bar.CompleteCurrentTurn();

            var last = _bar.AdvanceTurn();
            Assert.IsNotNull(last, "0 SPD unit should still get a turn");
            Assert.AreEqual(0f, last.Combatant.BaseStats.SPD);
        }

        [Test]
        public void AllSameSpd_SlotOrderDeterminesSequence()
        {
            var entries = MakeParty(
                (60, CombatTeam.Ally, 2, false),
                (60, CombatTeam.Ally, 0, false),
                (60, CombatTeam.Ally, 1, false)
            );

            _bar.BeginRound(entries);

            var first = _bar.AdvanceTurn();
            Assert.AreEqual(0, first.SlotIndex);
            _bar.CompleteCurrentTurn();

            var second = _bar.AdvanceTurn();
            Assert.AreEqual(1, second.SlotIndex);
            _bar.CompleteCurrentTurn();

            var third = _bar.AdvanceTurn();
            Assert.AreEqual(2, third.SlotIndex);
        }

        [Test]
        public void SixCombatants_FullRound()
        {
            // Acceptance criteria: test with 6 entities
            var entries = MakeParty(
                (80, CombatTeam.Ally, 0, false),
                (65, CombatTeam.Ally, 1, false),
                (50, CombatTeam.Ally, 2, false),
                (75, CombatTeam.Enemy, 0, false),
                (60, CombatTeam.Enemy, 1, false),
                (45, CombatTeam.Enemy, 2, false)
            );

            _bar.BeginRound(entries);

            float[] expectedOrder = { 80, 75, 65, 60, 50, 45 };
            for (int i = 0; i < expectedOrder.Length; i++)
            {
                var turn = _bar.AdvanceTurn();
                Assert.IsNotNull(turn, $"Turn {i + 1} should not be null");
                Assert.AreEqual(expectedOrder[i], turn.Combatant.BaseStats.SPD,
                    $"Turn {i + 1} should be combatant with SPD {expectedOrder[i]}");
                _bar.CompleteCurrentTurn();
            }

            Assert.IsTrue(_bar.IsRoundOver);
        }

        [Test]
        public void MultipleRounds_RecalculatesOrder()
        {
            var entries = MakeParty(
                (80, CombatTeam.Ally, 0, false),
                (60, CombatTeam.Enemy, 0, false)
            );

            // Round 1
            _bar.BeginRound(entries);
            _bar.AdvanceTurn();
            _bar.CompleteCurrentTurn();
            _bar.AdvanceTurn();
            _bar.CompleteCurrentTurn();
            Assert.IsTrue(_bar.IsRoundOver);

            // Buff the slower unit
            entries[1].Combatant.Buffs.Add(new BuffInstance
            {
                StatAffected = StatType.SPD,
                Percentage = 1.0f, // +100% -> 120 effective
                RemainingTurns = 3,
                IsDispellable = true
            });

            // Round 2 — enemy should now act first
            _bar.BeginRound(entries);
            var first = _bar.AdvanceTurn();
            Assert.AreEqual(entries[1].Combatant, first.Combatant,
                "Buffed enemy should act first in round 2");
        }
    }
}
