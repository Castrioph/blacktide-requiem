using System;
using System.Collections.Generic;
using BlacktideRequiem.Core.Data;

namespace BlacktideRequiem.Core.Combat
{
    /// <summary>
    /// Which team a combatant belongs to, used for tie-breaking.
    /// </summary>
    public enum CombatTeam
    {
        Ally,
        Enemy
    }

    /// <summary>
    /// Turn state of a combatant within a single round.
    /// See Initiative Bar GDD §States and Transitions.
    /// </summary>
    public enum TurnState
    {
        Queued,
        Active,
        Acted,
        Skipped,
        Dead
    }

    /// <summary>
    /// An entry on the initiative bar representing one combatant's turn.
    /// </summary>
    public class InitiativeEntry
    {
        /// <summary>Runtime combat state for this combatant.</summary>
        public CombatantState Combatant { get; }

        /// <summary>Team assignment for tie-breaking.</summary>
        public CombatTeam Team { get; }

        /// <summary>Slot index within team (0-based). Lower = higher priority on ties.</summary>
        public int SlotIndex { get; }

        /// <summary>Current turn state.</summary>
        public TurnState State { get; set; }

        /// <summary>Whether this entry is a Limit Break (extra turn) insertion.</summary>
        public bool IsLimitBreak { get; set; }

        public InitiativeEntry(CombatantState combatant, CombatTeam team, int slotIndex)
        {
            Combatant = combatant;
            Team = team;
            SlotIndex = slotIndex;
            State = TurnState.Queued;
        }
    }

    /// <summary>
    /// Round-based initiative timeline that determines turn order in combat.
    /// Pure logic — no MonoBehaviour, no Unity dependencies. Fully testable.
    ///
    /// Sorting: Effective SPD descending, with tie-breaking:
    ///   1. Bosses first
    ///   2. Allies before non-boss enemies
    ///   3. Lower slot index first (within same team)
    ///
    /// See Initiative Bar GDD for full specification.
    /// </summary>
    public class InitiativeBar
    {
        private readonly List<InitiativeEntry> _bar = new();
        private readonly HashSet<CombatantState> _limitBreakUsedThisRound = new();

        /// <summary>Current round number (1-based, incremented on each BeginRound).</summary>
        public int RoundNumber { get; private set; }

        /// <summary>Read-only view of the current bar state.</summary>
        public IReadOnlyList<InitiativeEntry> Entries => _bar;

        /// <summary>True when no Queued entries remain in the bar.</summary>
        public bool IsRoundOver
        {
            get
            {
                for (int i = 0; i < _bar.Count; i++)
                {
                    if (_bar[i].State == TurnState.Queued)
                        return false;
                }
                return true;
            }
        }

        /// <summary>
        /// Returns the currently Active entry, or null if none.
        /// </summary>
        public InitiativeEntry ActiveEntry
        {
            get
            {
                for (int i = 0; i < _bar.Count; i++)
                {
                    if (_bar[i].State == TurnState.Active)
                        return _bar[i];
                }
                return null;
            }
        }

        /// <summary>
        /// Fired when the bar is reordered mid-round (after SPD change).
        /// </summary>
        public event Action OnReorder;

        /// <summary>
        /// Fired when a Limit Break extra turn is inserted.
        /// </summary>
        public event Action<InitiativeEntry> OnLimitBreakInserted;

        /// <summary>
        /// Fired when a combatant's turn is skipped (stun/sleep).
        /// </summary>
        public event Action<InitiativeEntry> OnTurnSkipped;

        /// <summary>
        /// Begins a new round. Populates the bar with all alive combatants,
        /// sorted by effective SPD with tie-breaking rules.
        /// </summary>
        /// <param name="combatants">
        /// All combatants with their team and slot info.
        /// </param>
        public void BeginRound(List<InitiativeEntry> combatants)
        {
            RoundNumber++;
            _bar.Clear();
            _limitBreakUsedThisRound.Clear();

            for (int i = 0; i < combatants.Count; i++)
            {
                if (combatants[i].Combatant.IsKO)
                    continue;

                combatants[i].State = TurnState.Queued;
                combatants[i].IsLimitBreak = false;
                _bar.Add(combatants[i]);
            }

            SortBar(0);
        }

        /// <summary>
        /// Advances to the next combatant. Returns the entry that becomes Active,
        /// or null if the round is over.
        /// Automatically skips stunned/sleeping combatants.
        /// </summary>
        public InitiativeEntry AdvanceTurn()
        {
            // Deactivate current active entry
            var current = ActiveEntry;
            if (current != null && current.State == TurnState.Active)
                current.State = TurnState.Acted;

            // Find next queued entry
            for (int i = 0; i < _bar.Count; i++)
            {
                if (_bar[i].State != TurnState.Queued)
                    continue;

                var entry = _bar[i];

                // Check for stun
                if (entry.Combatant.HasStatus(StatusEffect.Aturdimiento))
                {
                    entry.State = TurnState.Skipped;
                    entry.Combatant.RemoveStatus(StatusEffect.Aturdimiento);
                    entry.Combatant.CCImmunityTurns = CC_IMMUNITY_DURATION;
                    OnTurnSkipped?.Invoke(entry);
                    continue;
                }

                // Check for sleep
                if (entry.Combatant.HasStatus(StatusEffect.Sueno))
                {
                    entry.State = TurnState.Skipped;
                    OnTurnSkipped?.Invoke(entry);
                    continue;
                }

                entry.State = TurnState.Active;
                return entry;
            }

            return null;
        }

        /// <summary>
        /// Marks the currently active combatant as having completed their turn.
        /// </summary>
        public void CompleteCurrentTurn()
        {
            var current = ActiveEntry;
            if (current != null)
                current.State = TurnState.Acted;
        }

        /// <summary>
        /// Resorts remaining queued entries by current effective SPD.
        /// Call after any SPD change (buff/debuff applied mid-round).
        /// Only affects Queued entries — Active, Acted, Skipped, Dead are untouched.
        /// </summary>
        public void Reorder()
        {
            // Find first queued index
            int firstQueued = -1;
            for (int i = 0; i < _bar.Count; i++)
            {
                if (_bar[i].State == TurnState.Queued)
                {
                    firstQueued = i;
                    break;
                }
            }

            if (firstQueued < 0)
                return;

            SortBar(firstQueued);
            OnReorder?.Invoke();
        }

        /// <summary>
        /// Inserts a Limit Break extra turn for a combatant.
        /// The extra turn is inserted immediately after the current Active entry.
        /// Returns false if the combatant already used a Limit Break this round,
        /// is dead, or is stunned.
        /// See Initiative Bar GDD §Limit Break rules.
        /// </summary>
        public bool InsertLimitBreak(CombatantState combatant)
        {
            // Dead units cannot receive extra turns
            if (combatant.IsKO)
                return false;

            // Stunned units lose their Limit Break
            if (combatant.HasStatus(StatusEffect.Aturdimiento))
                return false;

            // Max 1 extra turn per unit per round
            if (_limitBreakUsedThisRound.Contains(combatant))
                return false;

            _limitBreakUsedThisRound.Add(combatant);

            // Find the combatant's team and slot from existing entries
            CombatTeam team = CombatTeam.Ally;
            int slot = 0;
            for (int i = 0; i < _bar.Count; i++)
            {
                if (_bar[i].Combatant == combatant)
                {
                    team = _bar[i].Team;
                    slot = _bar[i].SlotIndex;
                    break;
                }
            }

            var extraEntry = new InitiativeEntry(combatant, team, slot)
            {
                State = TurnState.Queued,
                IsLimitBreak = true
            };

            // Insert after current active entry
            int insertIndex = 0;
            for (int i = 0; i < _bar.Count; i++)
            {
                if (_bar[i].State == TurnState.Active)
                {
                    insertIndex = i + 1;
                    break;
                }
            }

            _bar.Insert(insertIndex, extraEntry);
            OnLimitBreakInserted?.Invoke(extraEntry);
            return true;
        }

        /// <summary>
        /// Immediately removes a dead combatant from the bar.
        /// </summary>
        public void RemoveDead(CombatantState combatant)
        {
            for (int i = _bar.Count - 1; i >= 0; i--)
            {
                if (_bar[i].Combatant == combatant && _bar[i].State == TurnState.Queued)
                {
                    _bar[i].State = TurnState.Dead;
                }
            }
        }

        /// <summary>
        /// Inserts a revived combatant at the end of the current round's bar.
        /// Next round they are positioned normally by SPD.
        /// </summary>
        public void InsertRevived(CombatantState combatant, CombatTeam team, int slotIndex)
        {
            var entry = new InitiativeEntry(combatant, team, slotIndex)
            {
                State = TurnState.Queued
            };
            _bar.Add(entry);
        }

        /// <summary>
        /// Gets all currently queued entries (for UI display).
        /// </summary>
        public List<InitiativeEntry> GetQueuedEntries()
        {
            var result = new List<InitiativeEntry>();
            for (int i = 0; i < _bar.Count; i++)
            {
                if (_bar[i].State == TurnState.Queued || _bar[i].State == TurnState.Active)
                    result.Add(_bar[i]);
            }
            return result;
        }

        /// <summary>
        /// Sorts bar entries starting from the given index.
        /// Only sorts Queued entries; non-queued entries retain their position.
        /// Uses tie-breaking: boss > ally > enemy > slot order.
        /// </summary>
        private void SortBar(int startIndex)
        {
            // Extract queued entries from startIndex onward
            var queued = new List<InitiativeEntry>();
            var nonQueued = new List<(int index, InitiativeEntry entry)>();

            for (int i = startIndex; i < _bar.Count; i++)
            {
                if (_bar[i].State == TurnState.Queued)
                    queued.Add(_bar[i]);
                else
                    nonQueued.Add((i, _bar[i]));
            }

            queued.Sort(CompareEntries);

            // Rebuild from startIndex: interleave non-queued at their positions,
            // fill remaining with sorted queued
            int queuedIdx = 0;
            for (int i = startIndex; i < _bar.Count; i++)
            {
                bool isNonQueued = false;
                for (int j = 0; j < nonQueued.Count; j++)
                {
                    if (nonQueued[j].index == i)
                    {
                        isNonQueued = true;
                        break;
                    }
                }

                if (!isNonQueued && queuedIdx < queued.Count)
                {
                    _bar[i] = queued[queuedIdx++];
                }
            }

            // If bar was all queued (typical case), just replace
            if (nonQueued.Count == 0)
            {
                for (int i = 0; i < queued.Count; i++)
                {
                    _bar[startIndex + i] = queued[i];
                }
            }
        }

        /// <summary>
        /// Comparison function for sorting entries by SPD descending with tie-breaking.
        /// </summary>
        private static int CompareEntries(InitiativeEntry a, InitiativeEntry b)
        {
            float spdA = a.Combatant.GetEffectiveStat(StatType.SPD);
            float spdB = b.Combatant.GetEffectiveStat(StatType.SPD);

            // Higher SPD first
            int spdCompare = spdB.CompareTo(spdA);
            if (spdCompare != 0)
                return spdCompare;

            // Tie-breaking 1: Bosses first
            if (a.Combatant.IsBoss != b.Combatant.IsBoss)
                return a.Combatant.IsBoss ? -1 : 1;

            // Tie-breaking 2: Allies before non-boss enemies
            if (a.Team != b.Team)
                return a.Team == CombatTeam.Ally ? -1 : 1;

            // Tie-breaking 3: Lower slot index first
            return a.SlotIndex.CompareTo(b.SlotIndex);
        }

        /// <summary>CC immunity duration in turns after stun/wake.</summary>
        public const int CC_IMMUNITY_DURATION = 1;
    }
}
