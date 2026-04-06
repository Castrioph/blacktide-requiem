using System;

namespace BlacktideRequiem.Core.Combat
{
    /// <summary>
    /// Abstraction for combat input sources (player UI, enemy AI, auto-battle).
    /// CombatManager calls RequestAction and waits for the callback.
    /// See ADR-003 §4 (Input Abstraction).
    /// </summary>
    public interface ICombatInput
    {
        /// <summary>
        /// Requests an action for the given context.
        /// The implementation calls the callback when the action is ready.
        /// For AI: calls back immediately. For player UI: calls back on button press.
        /// </summary>
        void RequestAction(CombatContext context, Action<CombatAction> callback);
    }
}
