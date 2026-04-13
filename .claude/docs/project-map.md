# Project Map

## Runtime Code

- `Assets/Scripts/Core/Combat/`
  Combat state machine, wave flow, damage/heal calculators, initiative,
  configuration, and synergy evaluation.
- `Assets/Scripts/Core/Data/`
  ScriptableObject and stat definitions such as `CharacterData`, `AbilityData`,
  stat blocks, traits, and related combat data.
- `Assets/Scripts/Core/AI/`
  Enemy AI profiles and combat input adapters.
- `Assets/Scripts/Core/Events/`
  Game event bus and combat-facing events.

## Tests

- `Assets/Tests/EditMode/`
  NUnit/Unity EditMode coverage for combat, AI, stats, abilities, and synergies.

## Planning and Architecture

- `production/sprints/sprint-002.md`
  Current production sprint for land combat.
- `docs/architecture/adr-003-combat-architecture.md`
  CombatManager architecture and event flow.
- `docs/architecture/adr-001-state-management.md`
  ScriptableObjects plus event bus direction.

## Current Focus

- Sprint 2 centers on production land combat.
- The active task is `S2-06`: basic traits/synergies.
- Combat HUD already pivoted to UGUI; prototype code is reference only.

## Ignore for Recovery

- `Library/`, `Temp/`, `Logs/`, and `UserSettings/`
- Generated `.csproj` and solution files unless the task is editor/tooling specific
