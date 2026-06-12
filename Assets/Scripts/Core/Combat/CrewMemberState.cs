using System.Collections.Generic;
using BlacktideRequiem.Core.Data;

namespace BlacktideRequiem.Core.Combat
{
    /// <summary>
    /// Runtime state of a crew member aboard a ship in naval combat.
    /// NOT an ICombatant: crew members take no turns. They are targets of
    /// Abordaje and Veneno/Sangrado DoTs, and carriers of traits (for
    /// SynergyEvaluator — buffs land on the SHIP's BuffStack) and of
    /// SeaAbilities (for the ship's ability pool).
    /// Crew HP is fixed per role (Combate Naval GDD §6), independent of the
    /// assigned unit's level or stats.
    /// See ADR-004 §2.
    /// </summary>
    public class CrewMemberState : ITraitCarrier
    {
        /// <summary>Role of the slot this crew member fills.</summary>
        public NavalRole Role { get; }

        /// <summary>Slot index on the ship (matches RoleSlot.SlotIndex).</summary>
        public int SlotIndex { get; }

        /// <summary>Whether this crew member sits in the ship's guest slot.</summary>
        public bool IsGuestSlot { get; }

        /// <summary>The assigned unit (stats, sea abilities, traits).</summary>
        public CharacterData Unit { get; }

        /// <summary>Fixed HP for the role (Capitán 800 … Artillero 400).</summary>
        public int MaxHP { get; }

        public int CurrentHP { get; private set; }

        public bool IsDead => CurrentHP <= 0;

        /// <summary>Whether the assigned unit's NavalRoleAffinity matches the slot role.</summary>
        public bool RoleMatches
        {
            get
            {
                if (Unit is UnitData unitData && unitData.NavalRoleAffinity != null)
                    return unitData.NavalRoleAffinity.Contains(Role);
                return false;
            }
        }

        // --- ITraitCarrier ---

        /// <summary>Traits come from the assigned unit.</summary>
        public IReadOnlyList<UnitTraitEntry> Traits => Unit?.Traits;

        /// <summary>Synergy buffs from crew traits land on the SHIP's BuffStack (ADR-004 §5).</summary>
        public BuffStack SynergyBuffTarget { get; }

        public CrewMemberState(RoleSlot slot, CharacterData unit, BuffStack shipBuffs)
        {
            Role = slot.Role;
            SlotIndex = slot.SlotIndex;
            IsGuestSlot = slot.IsGuestSlot;
            Unit = unit;
            SynergyBuffTarget = shipBuffs;
            MaxHP = GetCrewHPForRole(slot.Role);
            CurrentHP = MaxHP;
        }

        /// <summary>
        /// Applies damage (Abordaje or crew-targeted DoT), clamping to 0.
        /// Returns actual damage dealt. The caller (NavalTurnResolver) is
        /// responsible for triggering ship recalculation on death.
        /// </summary>
        public int ApplyDamage(int damage)
        {
            int actual = System.Math.Min(damage, CurrentHP);
            CurrentHP -= actual;
            return actual;
        }

        /// <summary>
        /// Fixed crew HP per role. See Combate Naval GDD §6 table.
        /// </summary>
        public static int GetCrewHPForRole(NavalRole role)
        {
            return role switch
            {
                NavalRole.Capitan       => 800,
                NavalRole.Intendente    => 600,
                NavalRole.Artillero     => 400,
                NavalRole.Navegante     => 500,
                NavalRole.Carpintero    => 700,
                NavalRole.Cirujano      => 500,
                NavalRole.Contramaestre => 600,
                _ => 500
            };
        }
    }
}
