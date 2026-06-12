namespace BlacktideRequiem.Core.Combat
{
    /// <summary>
    /// Strategy for resolving the action phase and DoTs of a turn.
    /// CombatManager delegates here; the rest of the pipeline (buff tick,
    /// status tick, CC check, death, waves, events) is shared.
    /// Land and naval combat differ only in this resolver (ADR-004 §3).
    /// </summary>
    public interface ITurnResolver
    {
        /// <summary>Binds the resolver to its orchestrating manager.</summary>
        void Initialize(CombatManager manager);

        /// <summary>
        /// Start-of-turn entity upkeep that is domain-specific.
        /// Land: removes Guard. Naval: clears Maniobra Evasiva.
        /// </summary>
        void OnTurnStart(ICombatant actor);

        /// <summary>Executes the chosen action for the actor and its targets.</summary>
        void ResolveAction(ICombatant actor, CombatAction action, CombatContext context);

        /// <summary>
        /// Applies post-action DoTs.
        /// Land: burn (only if acted) and poison to the actor.
        /// Naval: Quemadura→ship HHP; Veneno→1 random living crew member.
        /// </summary>
        void ApplyPostActionDoTs(ICombatant actor, bool actorPassed);

        /// <summary>
        /// Applies start-of-turn DoTs (Sangrado).
        /// Land: to the actor. Naval: to 1 random living crew member.
        /// Caller (CombatManager) checks actor.IsKO afterwards and handles death.
        /// </summary>
        void ApplyStartOfTurnDoTs(ICombatant actor);
    }
}
