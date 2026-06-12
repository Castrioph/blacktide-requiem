using System;
using System.Collections.Generic;
using BlacktideRequiem.Core.Combat;
using BlacktideRequiem.Core.Data;

namespace BlacktideRequiem.UI.Combat.Naval
{
    /// <summary>
    /// Bridges naval player UI interactions to CombatAction via ICombatInput.
    /// CombatRunner calls RequestAction; this class stores the callback and
    /// waits. When the player confirms an action through NavalCombatHUD, the
    /// appropriate Submit* method fires the callback.
    /// Pattern mirrors PlayerCombatInput (land) — see ADR-003 §4.
    /// </summary>
    public class NavalPlayerCombatInput : ICombatInput
    {
        private Action<CombatAction> _pendingCallback;
        private CombatContext _currentContext;

        /// <summary>
        /// Fired when CombatRunner requests player input.
        /// NavalCombatHUD subscribes to enter ActionSelect state.
        /// </summary>
        public event Action OnInputRequested;

        /// <summary>True when the HUD is waiting for the player to choose an action.</summary>
        public bool IsWaitingForInput => _pendingCallback != null;

        /// <summary>The current combat context (actor is always a ShipCombatant).</summary>
        public CombatContext CurrentContext => _currentContext;

        /// <summary>The acting ship (convenience cast — naval battles only).</summary>
        public ShipCombatant ActorShip =>
            _currentContext.Actor as ShipCombatant;

        // ====================================================================
        // ICombatInput
        // ====================================================================

        /// <summary>
        /// Called by CombatRunner. Stores callback and signals HUD to present
        /// the action panel.
        /// </summary>
        public void RequestAction(CombatContext context, Action<CombatAction> callback)
        {
            _currentContext = context;
            _pendingCallback = callback;
            OnInputRequested?.Invoke();
        }

        // ====================================================================
        // SUBMIT HELPERS (called by NavalCombatHUD)
        // ====================================================================

        /// <summary>Cañonazo: basic physical hull attack on a single enemy ship.</summary>
        public void SubmitCannonball(ICombatant targetShip)
        {
            SubmitAction(CombatAction.Cannonball(targetShip));
        }

        /// <summary>Habilidad Naval: uses an ability, optionally on a target.</summary>
        public void SubmitAbility(AbilityData ability, ICombatant target,
            AbilityEntry? entry = null)
        {
            SubmitAction(CombatAction.FromAbility(ability, target, entry));
        }

        /// <summary>
        /// Habilidad Naval (SingleCrewEnemy variant): targets a crew member.
        /// targetShip and targetCrew must both be non-null.
        /// </summary>
        public void SubmitAbilityOnCrew(AbilityData ability, ICombatant targetShip,
            CrewMemberState targetCrew, AbilityEntry? entry = null)
        {
            var action = CombatAction.FromAbility(ability, targetShip, entry);
            action.TargetCrew = targetCrew;
            SubmitAction(action);
        }

        /// <summary>Maniobra Evasiva: halves incoming damage until next turn.</summary>
        public void SubmitManeuver()
        {
            SubmitAction(CombatAction.Maneuver());
        }

        /// <summary>Abordaje: attacks one living crew member of an enemy ship.</summary>
        public void SubmitBoarding(ICombatant targetShip, CrewMemberState targetCrew)
        {
            SubmitAction(CombatAction.Boarding(targetShip, targetCrew));
        }

        /// <summary>Reparar: heals hull HP for a fixed MP cost.</summary>
        public void SubmitRepair()
        {
            SubmitAction(CombatAction.Repair());
        }

        /// <summary>Pasar Turno: yields the turn without acting.</summary>
        public void SubmitPass()
        {
            SubmitAction(CombatAction.PassTurn());
        }

        // ====================================================================
        // QUERY HELPERS (called by NavalCombatHUD for panel population)
        // ====================================================================

        /// <summary>
        /// Returns the full ability pool of the acting ship (duplicates allowed
        /// per GDD edge case — each instance has independent cooldown state).
        /// </summary>
        public IReadOnlyList<AbilityData> GetAbilityPool()
        {
            return ActorShip?.AbilityPool ?? Array.Empty<AbilityData>() as IReadOnlyList<AbilityData>;
        }

        /// <summary>
        /// Whether Abordaje is available: at least one enemy ship in context
        /// has living crew (creatures have no crew — always false for them).
        /// </summary>
        public bool IsBoardingAvailable()
        {
            if (_currentContext.Enemies == null) return false;
            foreach (var enemy in _currentContext.Enemies)
            {
                if (enemy.IsKO) continue;
                if (enemy is ShipCombatant ts && ts.GetLivingCrew().Count > 0)
                    return true;
            }
            return false;
        }

        // ====================================================================
        // INTERNAL
        // ====================================================================

        private void SubmitAction(CombatAction action)
        {
            if (_pendingCallback == null) return;
            var callback = _pendingCallback;
            _pendingCallback = null;
            callback(action);
        }
    }
}
