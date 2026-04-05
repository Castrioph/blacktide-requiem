# ADR-001: State Management — ScriptableObjects + Event Bus

> **Status**: Accepted
> **Date**: 2026-03-28
> **Deciders**: User + Technical Director agent
> **Sprint**: S1-02

## Context

The project is a turn-based RPG gacha with 14 interconnected MVP systems. Each
system needs to read static game data (unit stats, ship stats, ability definitions)
and communicate runtime state changes (damage dealt, buff applied, turn started)
to other systems without tight coupling.

The 14 GDDs already assume data structures like `CharacterData`, `ShipData`,
`AbilityEntry` as inspectable assets, and describe inter-system communication
through events (`OnActionResolved`, `OnTurnStart`, `OnWaveComplete`).

Unity 6.3 LTS offers three main paradigms: traditional MonoBehaviour/SO,
DOTS/ECS (Entities 1.3+), and hybrid approaches.

## Decision

**Use ScriptableObjects as static data containers + a lightweight C# Event Bus
for runtime state communication.**

### Static Data Layer (ScriptableObjects)

All game definitions are ScriptableObjects, editable in the Unity Inspector:

- `CharacterData` SO — unit stats, abilities, traits, rarity (UDM GDD)
- `ShipData` SO — ship stats, role slots, base abilities (SDM GDD)
- `AbilityData` SO — ability definitions, target types, costs (shared)
- `StageData` SO — battle configurations, waves, missions (Stage System GDD)
- `BannerData` SO — gacha banner pools, rates (Gacha GDD)
- `TraitData` SO — trait definitions, synergy bonuses (Traits GDD)
- `EnemyData` SO — enemy stats, AI profile, tier (Enemy System GDD)

SOs are **read-only at runtime**. They define "what things ARE." They are never
modified during gameplay.

### Runtime State Layer (Plain C# Classes)

Combat runtime state lives in plain C# classes, not MonoBehaviours:

- `CombatState` — current battle state, wave number, active combatants
- `CombatantState` — current HP/MP, active buffs/debuffs, status effects
- `ShipCombatState` — current HHP/MP, crew HP, active buffs

These are created at battle start, modified during combat, and discarded at
battle end. They reference SOs for base data but own all mutable state.

### Communication Layer (Event Bus)

A simple C# event bus decouples systems:

```csharp
// Static event bus — systems publish and subscribe without knowing each other
public static class GameEvents
{
    // Combat events
    public static event Action<DamageResult> OnDamageDealt;
    public static event Action<HealResult> OnHealApplied;
    public static event Action<CombatantState> OnTurnStart;
    public static event Action<CombatantState> OnTurnEnd;
    public static event Action<BuffData> OnBuffApplied;
    public static event Action<StatusEffect> OnStatusApplied;
    public static event Action<int> OnWaveComplete;      // wave index
    public static event Action<BattleResult> OnBattleEnd;

    // Gacha events
    public static event Action<PullResult> OnGachaPull;

    // Progression events
    public static event Action<CharacterData, int> OnUnitLevelUp;

    // Publish methods with null-safety
    public static void PublishDamageDealt(DamageResult result)
        => OnDamageDealt?.Invoke(result);
    // ... etc
}
```

**Why static events instead of SO events or a message broker:**
- Zero allocation per event (no boxing, no message objects)
- Compile-time type safety
- Simple to debug (breakpoint on Publish method)
- No dependency injection framework needed
- Sufficient for a single-player game with <20 simultaneous subscribers

### Dependency Injection: Constructor/Method Injection (No Framework)

Systems receive their dependencies through constructors or method parameters:

```csharp
public class DamageEngine
{
    public DamageResult Calculate(CombatantState attacker, CombatantState defender,
                                  AbilityData ability)
    {
        // Pure calculation — no singletons, no global state
    }
}
```

No DI container (Zenject/VContainer). For a solo dev project, constructor injection
is sufficient and avoids framework complexity.

## Alternatives Considered

### DOTS/ECS (Entities 1.3+)

- **Rejected**: Overkill for a turn-based RPG with <20 entities in combat.
  DOTS excels at thousands of entities with per-frame updates. Our combat is
  event-driven (player selects action → resolve → next turn). The learning
  curve for a first Unity project is prohibitive, and DOTS APIs have changed
  significantly across Unity 6.x releases.

### Pure MonoBehaviour Singletons

- **Rejected**: Leads to tight coupling (systems reference each other directly),
  untestable code (can't mock singletons easily), and hidden dependencies.
  Every GDD specifies system interfaces — singletons would violate that design.

### Zenject/VContainer DI Framework

- **Rejected for now**: Adds framework complexity for a solo dev. Constructor
  injection achieves the same testability without the learning overhead. Can
  be adopted later if the project grows beyond solo development.

### ScriptableObject Events (Ryan Hipple pattern)

- **Considered but simplified**: SO events are useful for designer-facing
  wiring in the Inspector, but our event consumers are all code systems, not
  scene objects. Static C# events are simpler and faster for code-to-code
  communication. If we need Inspector-wirable events later (e.g., for VFX
  triggers), we can add SO events for specific cases without changing the
  core architecture.

## Consequences

### Positive

- **Matches GDD architecture**: SOs map 1:1 to data structures in the 14 GDDs
- **Testable**: Pure C# classes + constructor injection = unit-testable without Unity runtime
- **Decoupled**: Event bus means Combat UI doesn't import Combate Terrestre — it just listens to `OnDamageDealt`
- **Inspector-friendly**: All game data editable in Unity Inspector (fast iteration)
- **Low complexity**: No frameworks, no ECS boilerplate, no DI containers

### Negative

- **No automatic change propagation**: If a SO is modified in Editor during play mode, listeners don't auto-update (acceptable — SOs are read-only at runtime)
- **Static events are global**: All subscribers see all events. For this project size (~14 systems), this is fine. At 50+ systems, consider scoped event channels.
- **Manual wiring**: No framework auto-discovers dependencies. Each system explicitly subscribes/unsubscribes. This is a feature for a small project (explicit > magic) but could become tedious at scale.

## Compliance

- **Coding Standards**: Public APIs documented, dependency injection over singletons ✅
- **Technical Preferences**: C# naming conventions, PascalCase methods ✅
- **Context Management**: Plain C# classes are serializable for save/load ✅
