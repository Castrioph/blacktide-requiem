using System;
using UnityEngine;

namespace BlacktideRequiem.Core.Data
{
    /// <summary>
    /// A single crew slot on a ship. Defines which role is required
    /// and optionally holds an assigned unit reference.
    /// See Ship Data Model GDD §1.
    /// </summary>
    [Serializable]
    public struct RoleSlot
    {
        [Tooltip("Position in the crew layout (0-based)")]
        public int SlotIndex;

        [Tooltip("Which naval role this slot requires")]
        public NavalRole Role;

        [Tooltip("Whether this slot accepts a friend/guest unit")]
        public bool IsGuestSlot;
    }
}
