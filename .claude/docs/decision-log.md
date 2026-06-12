# Decision Log

## Stable Decisions

- Engine and language: Unity 6.3 LTS with C#.
- Rendering: URP. See `docs/architecture/adr-002-rendering-pipeline.md`.
- State management: ScriptableObjects plus event bus. Avoid singletons and DOTS.
  See `docs/architecture/adr-001-state-management.md`.
- Land combat runtime lives in `Assets/Scripts/Core/` and is tested through
  Unity EditMode tests.
- `CombatManager` is a pure C# orchestrator, not a `MonoBehaviour`.
- Combat HUD uses UGUI Canvas for the current production flow. UI Toolkit is
  still an option for later menu work, but not the active combat HUD path.
- Prototype combat code is reference only; production behavior belongs in
  `Assets/Scripts/Core/`.
- Naval combat reuses land orchestration by composition: single
  `CombatManager` + `InitiativeBar` over an `ICombatant` interface; ship = one
  combatant, crew = passive sub-entities (no turns); naval divergence isolated
  in `ITurnResolver`. See `docs/architecture/adr-004-naval-combat-architecture.md`.

## Current Direction

- `S2-06` introduces traits/synergies through data in `CharacterData`,
  evaluation in `SynergyEvaluator`, and state ownership in `CombatManager`.
- Synergy-related validation should prefer `Assets/Tests/EditMode/` and the
  existing Unity batch EditMode test command.
