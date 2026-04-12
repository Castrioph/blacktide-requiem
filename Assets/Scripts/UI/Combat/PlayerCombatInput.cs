using System;
using System.Collections.Generic;
using BlacktideRequiem.Core.Combat;
using BlacktideRequiem.Core.Data;

namespace BlacktideRequiem.UI.Combat
{
    /// <summary>
    /// Bridges player UI interactions to CombatAction via ICombatInput.
    /// CombatRunner calls RequestAction; this class stores the callback
    /// and waits. When the player clicks a UI button, SubmitAction is
    /// called, which invokes the stored callback.
    /// See ADR-003 §4.
    /// </summary>
    public class PlayerCombatInput : ICombatInput
    {
        private Action<CombatAction> _pendingCallback;
        private CombatContext _currentContext;

        /// <summary>Fired when CombatRunner requests player input. CombatHUD uses this to enter ActionSelect.</summary>
        public event Action OnInputRequested;

        /// <summary>True when waiting for the player to choose an action.</summary>
        public bool IsWaitingForInput => _pendingCallback != null;

        /// <summary>The current combat context (actor, allies, enemies).</summary>
        public CombatContext CurrentContext => _currentContext;

        /// <summary>
        /// Called by CombatRunner. Stores the callback and waits for player input.
        /// </summary>
        public void RequestAction(CombatContext context, Action<CombatAction> callback)
        {
            _currentContext = context;
            _pendingCallback = callback;
            OnInputRequested?.Invoke();
        }

        /// <summary>
        /// Called by UI when the player has fully chosen an action + target.
        /// </summary>
        public void SubmitAction(CombatAction action)
        {
            if (_pendingCallback == null) return;

            var callback = _pendingCallback;
            _pendingCallback = null;
            callback(action);
        }

        /// <summary>Submits a basic attack on the given target.</summary>
        public void SubmitAttack(CombatantState target)
        {
            bool isPhysical = _currentContext.Actor.GetEffectiveStat(StatType.ATK)
                           >= _currentContext.Actor.GetEffectiveStat(StatType.MST);
            SubmitAction(CombatAction.BasicAttack(target, isPhysical));
        }

        /// <summary>Submits a Guard action.</summary>
        public void SubmitGuard()
        {
            SubmitAction(CombatAction.Guard());
        }

        /// <summary>Submits a Pass action.</summary>
        public void SubmitPass()
        {
            SubmitAction(CombatAction.PassTurn());
        }

        /// <summary>Submits an ability on the given target.</summary>
        public void SubmitAbility(AbilityData ability, CombatantState target)
        {
            SubmitAction(CombatAction.FromAbility(ability, target));
        }

        /// <summary>
        /// Gets abilities available for the current actor
        /// (unlocked, off cooldown, not silenced).
        /// </summary>
        public List<AbilityData> GetAvailableAbilities()
        {
            var result = new List<AbilityData>();
            var actor = _currentContext.Actor;
            if (actor == null) return result;

            bool silenced = actor.HasStatus(StatusEffect.Silencio);
            if (silenced) return result;

            foreach (var entry in actor.Template.LandAbilities)
            {
                if (entry.Ability == null) continue;
                if (entry.UnlockLevel > actor.Level) continue;
                if (actor.GetCooldownRemaining(entry.Ability) > 0) continue;
                result.Add(entry.Ability);
            }
            return result;
        }
    }
}
