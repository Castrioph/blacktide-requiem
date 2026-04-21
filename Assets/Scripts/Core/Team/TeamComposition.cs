using System;
using System.Collections.Generic;
using BlacktideRequiem.Core.Data;

namespace BlacktideRequiem.Core.Team
{
    /// <summary>
    /// Manages the player's selected team for a combat session.
    /// Holds an immutable roster and up to <see cref="MaxSlots"/> selected characters.
    /// Pure C# — no Unity dependencies.
    /// </summary>
    public class TeamComposition
    {
        public const int MaxSlots = 3;

        private readonly IReadOnlyList<CharacterData> _roster;
        private readonly CharacterData[] _slots;

        public IReadOnlyList<CharacterData> Roster => _roster;

        public TeamComposition(IReadOnlyList<CharacterData> roster)
        {
            if (roster == null) throw new ArgumentNullException(nameof(roster));
            if (roster.Count == 0) throw new ArgumentException("Roster cannot be empty.", nameof(roster));
            _roster = roster;
            _slots = new CharacterData[MaxSlots];
        }

        /// <summary>Character in the given slot, or null if empty.</summary>
        public CharacterData GetSlot(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= MaxSlots)
                throw new ArgumentOutOfRangeException(nameof(slotIndex));
            return _slots[slotIndex];
        }

        /// <summary>
        /// Assigns <paramref name="character"/> to <paramref name="slotIndex"/>.
        /// Returns false if the character is not in the roster, the slot is out of range,
        /// or the character is already assigned to another slot.
        /// </summary>
        public bool SelectCharacter(int slotIndex, CharacterData character)
        {
            if (slotIndex < 0 || slotIndex >= MaxSlots) return false;
            if (character == null) return false;
            if (!RosterContains(character)) return false;

            for (int i = 0; i < MaxSlots; i++)
                if (i != slotIndex && _slots[i] == character) return false;

            _slots[slotIndex] = character;
            return true;
        }

        /// <summary>Empties the given slot. No-op if already empty or index out of range.</summary>
        public void ClearSlot(int slotIndex)
        {
            if (slotIndex >= 0 && slotIndex < MaxSlots)
                _slots[slotIndex] = null;
        }

        /// <summary>Empties all slots.</summary>
        public void ClearAll()
        {
            for (int i = 0; i < MaxSlots; i++)
                _slots[i] = null;
        }

        /// <summary>True when at least one slot is filled.</summary>
        public bool IsValid => FilledSlotCount > 0;

        /// <summary>Number of non-empty slots.</summary>
        public int FilledSlotCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < MaxSlots; i++)
                    if (_slots[i] != null) count++;
                return count;
            }
        }

        /// <summary>
        /// Returns filled slots in index order, suitable for
        /// <c>StageController.BuildBattleConfig(stage, composition.GetTeam())</c>.
        /// </summary>
        public IReadOnlyList<CharacterData> GetTeam()
        {
            var team = new List<CharacterData>(MaxSlots);
            for (int i = 0; i < MaxSlots; i++)
                if (_slots[i] != null) team.Add(_slots[i]);
            return team;
        }

        private bool RosterContains(CharacterData character)
        {
            for (int i = 0; i < _roster.Count; i++)
                if (_roster[i] == character) return true;
            return false;
        }
    }
}
