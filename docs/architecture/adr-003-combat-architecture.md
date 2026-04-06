# ADR-003: Combat Architecture — CombatManager, State Machine, Event Flow

> **Status**: Accepted
> **Date**: 2026-04-06
> **Deciders**: User + Technical Director agent
> **Sprint**: S2-01

## Context

Sprint 1 validated the core combat loop via a throwaway prototype
(`Assets/Scripts/Prototypes/CombatV1/CombatPrototype.cs`). The prototype mixes
game logic, AI, state management, and IMGUI rendering in a single 700-line
MonoBehaviour. Production needs clean separation into:

1. **CombatManager** — orchestrates the battle state machine
2. **Enemy AI** — decides enemy actions (separate from orchestration)
3. **Combat UI** — renders state and captures player input (UI Toolkit)
4. **Existing systems** — InitiativeBar, DamageCalculator, CombatantState (reused as-is)

The Combate Terrestre GDD defines 5 battle states (PreCombat, InRound,
WaveTransition, Victory, Defeat), a per-turn processing order (buffs tick →
CC check → bleed → action → burn → poison), and interactions with 9 other
systems. The architecture must support all of this while remaining testable
and decoupled.

## Decision

### 1. CombatManager — Pure C# State Machine

`CombatManager` is a **plain C# class** (not a MonoBehaviour). It owns the
battle state machine and orchestrates turn processing. A thin MonoBehaviour
wrapper (`CombatRunner`) drives it via coroutines.

```
┌─────────────┐
│ CombatRunner │  (MonoBehaviour — owns coroutine lifecycle)
│  (thin)      │
└──────┬───────┘
       │ delegates to
┌──────▼───────┐
│CombatManager │  (pure C# — owns state machine, testable)
│              │──→ InitiativeBar (existing)
│              │──→ DamageCalculator (existing)
│              │──→ IEnemyAI (interface)
│              │──→ ICombatInput (interface — player or auto-battle)
│              │──→ GameEvents (event bus, ADR-001)
└──────────────┘
```

**Why pure C# + wrapper:**
- Unit-testable without Unity runtime (NUnit tests, no Play Mode needed)
- The prototype proved coroutines work well for turn sequencing
- Wrapper pattern is minimal (~30 lines) and handles only MonoBehaviour concerns

### 2. Battle States

Matching the GDD exactly:

```
PreCombat → InRound ⇄ WaveTransition
                ↓
         Victory / Defeat
```

| State | Responsibility |
|-------|---------------|
| `PreCombat` | Load BattleConfig, create CombatantStates, calculate synergies, init InitiativeBar |
| `InRound` | Process turns sequentially per Initiative Bar order |
| `WaveTransition` | Deploy next wave, recalculate enemy synergies, reset Initiative Bar |
| `Victory` | Emit `OnBattleEnd(Victory)`, calculate rewards |
| `Defeat` | Emit `OnBattleEnd(Defeat)` |

State is stored as an enum. Transitions are explicit methods with guards.

### 3. Turn Processing Order

Each turn follows the GDD processing pipeline:

```
1. Tick buffs/debuffs (decrement durations, remove expired)
2. Tick CC immunity (decrement, consume)
3. Bleed damage (if has Sangrado)
4. CC check (Stun/Sleep/Confuse → skip or random action)
5. Action phase (player input OR enemy AI)
6. Burn damage (if has Quemadura AND acted — not if Passed)
7. Poison damage (if has Veneno)
8. Death check after each damage source
9. LB check (if ability had CanLimitBreak and condition met)
```

This is implemented as a `ProcessTurn(CombatantState)` method that emits
events at each step, allowing UI to animate each phase.

### 4. Input Abstraction — ICombatInput

Player input and Auto-Battle share the same interface:

```csharp
public interface ICombatInput
{
    /// Returns the action chosen for this combatant's turn.
    /// Async pattern: CombatManager yields until this completes.
    CombatAction GetAction(CombatContext context);
}
```

```csharp
public struct CombatContext
{
    public CombatantState Actor;
    public List<CombatantState> Allies;
    public List<CombatantState> Enemies;
    public List<AbilityData> AvailableAbilities; // filtered by cooldown
}

public struct CombatAction
{
    public ActionType Type;        // Attack, Ability, Guard, Pass
    public AbilityData Ability;    // null for Attack/Guard/Pass
    public CombatantState Target;  // null for self-target/AoE/Guard/Pass
}
```

- `PlayerCombatInput` — bridges UI Toolkit events to CombatAction
- `EnemyAI` — implements ICombatInput using AI profiles from Enemy System GDD
- `AutoBattleInput` — implements ICombatInput using ally AI rules (future, S2 backlog)

**Why an interface instead of direct UI coupling:**
- CombatManager doesn't know about UI Toolkit, IMGUI, or AI
- Enables unit testing with mock inputs
- Auto-Battle drops in without changing CombatManager

### 5. Event Flow — GameEvents (ADR-001)

CombatManager publishes events at each meaningful moment. UI subscribes and
renders. No return channel — UI sends player input through `ICombatInput`,
not through events.

```
CombatManager publishes:          UI subscribes:
─────────────────────────         ─────────────
OnBattleStart(config)        →   Show combat scene, init HP bars
OnRoundStart(roundNum)       →   Update round counter
OnTurnStart(combatant)       →   Highlight active unit, show actions
OnActionChosen(action)       →   Animate action
OnDamageDealt(result)        →   Show damage numbers, update HP bar
OnHealApplied(result)        →   Show heal numbers, update HP bar
OnBuffApplied(buff)          →   Show buff icon
OnStatusApplied(status)      →   Show status icon
OnTurnSkipped(combatant)     →   Show "Stunned!" text
OnUnitDied(combatant)        →   Play death animation
OnWaveComplete(waveIndex)    →   Wave transition animation
OnBattleEnd(result)          →   Victory/Defeat screen
```

### 6. File Structure

```
Assets/Scripts/
├── Core/
│   ├── Combat/              (existing — DamageCalculator, InitiativeBar, etc.)
│   │   ├── CombatManager.cs       ← NEW: state machine + turn processing
│   │   ├── CombatAction.cs        ← NEW: action structs
│   │   ├── CombatContext.cs       ← NEW: context for input providers
│   │   ├── ICombatInput.cs        ← NEW: input abstraction
│   │   └── BattleConfig.cs        ← NEW: battle setup data
│   ├── Data/                (existing — CharacterData, StatBlock, etc.)
│   │   └── AbilityData.cs         ← NEW: ability ScriptableObject
│   ├── AI/
│   │   ├── EnemyAI.cs             ← NEW: AI profiles
│   │   └── AIProfile.cs           ← NEW: AI profile ScriptableObject
│   └── Events/
│       └── GameEvents.cs          ← NEW: static event bus (ADR-001)
├── Runtime/
│   └── Combat/
│       └── CombatRunner.cs        ← NEW: MonoBehaviour wrapper
└── UI/
    └── Combat/
        ├── CombatHUD.cs           ← NEW: UI Toolkit code-behind
        ├── CombatHUD.uxml         ← NEW: UI structure
        ├── CombatHUD.uss          ← NEW: UI styles
        └── PlayerCombatInput.cs   ← NEW: bridges UI → ICombatInput
```

### 7. What We Reuse As-Is from Sprint 1

| Class | Role | Changes Needed |
|-------|------|---------------|
| `DamageCalculator` | Damage formula | None |
| `HealCalculator` | Heal formula | None |
| `ElementTable` | Element modifiers | None |
| `InitiativeBar` | Turn ordering | None |
| `CombatantState` | Runtime unit state | Minor: add cooldown tracking, guard flag |
| `BuffStack` | Buff management | None |
| `StatCalculator` | Stat growth | None |
| `CharacterData` | Unit template | None (AbilityData is new SO) |

## Alternatives Considered

### Single MonoBehaviour (like the prototype)

- **Rejected**: Works for 700 lines, won't work for 2000+. Untestable,
  impossible to swap input sources. The prototype proved the logic works;
  now it needs structure.

### Full ECS / Command Pattern

- **Rejected**: Overkill for turn-based sequential combat. Each turn has
  one actor doing one thing. A command queue adds complexity without benefit
  when there's no parallelism or undo requirement.

### Async/Await instead of Coroutines

- **Considered**: Unity 6.3 supports async/await with Awaitable. However,
  coroutines are simpler for this use case (sequential turn processing with
  visual delays), and the prototype already validated the coroutine approach.
  Can migrate to Awaitable later if coroutines become limiting.

## Consequences

### Positive

- **Testable**: CombatManager is pure C#, tests don't need Unity Play Mode
- **GDD-compliant**: States and turn order match the GDD exactly
- **Swappable input**: Player, AI, and Auto-Battle use the same interface
- **Decoupled UI**: UI Toolkit code never touches combat logic
- **Incremental**: Can build CombatManager + mock input first, then UI, then AI

### Negative

- **CombatRunner wrapper**: Extra file, but it's trivially small (~30 lines)
- **Event explosion**: Many event types. Manageable at current scale (~12 events)
- **No undo/replay**: Sequential processing can't rewind. Not needed for this game
