# Technical Preferences

<!-- Populated by /setup-engine. Updated as the user makes decisions throughout development. -->
<!-- All agents reference this file for project-specific standards and conventions. -->

## Engine & Language

- **Engine**: Unity 6.3 LTS (6000.3)
- **Language**: C#
- **Rendering**: URP (Universal Render Pipeline) — recomendado para mobile/web 2D
- **Physics**: Unity Physics 2D (built-in)

## Naming Conventions

- **Classes**: PascalCase (e.g., `PlayerController`)
- **Public fields/properties**: PascalCase (e.g., `MoveSpeed`)
- **Private fields**: _camelCase (e.g., `_moveSpeed`)
- **Methods**: PascalCase (e.g., `TakeDamage()`)
- **Files**: PascalCase matching class (e.g., `PlayerController.cs`)
- **Prefabs**: PascalCase (e.g., `PirateUnit.prefab`)
- **Constants**: UPPER_SNAKE_CASE (e.g., `MAX_CREW_SIZE`)

## Performance Budgets

- **Target Framerate**: 60 fps (mobile + WebGL)
- **Frame Budget**: 16.6ms
- **Draw Calls**: ≤100 per frame (URP 2D + sprite atlases + SRP Batcher)
- **Memory Ceiling**: 512MB (mobile), 1GB (WebGL)
- **Scene Load Time**: ≤3s (mobile), ≤5s (WebGL)
- **Build Size**: ≤150MB base, ≤300MB with assets (Play Store limit)

## Testing

- **Framework**: NUnit + Unity Test Framework (to be configured when coding begins)
- **Minimum Coverage**: [TO BE CONFIGURED]
- **Required Tests**: Balance formulas, gameplay systems, networking (if applicable)

## Forbidden Patterns

- **Singletons**: Use constructor/method injection instead (ADR-001)
- **DOTS/ECS**: Not used in this project — ScriptableObjects + Event Bus (ADR-001)
- **Built-in RP shaders**: Must use URP-compatible shaders only (ADR-002)

## Allowed Libraries / Addons

<!-- Add approved third-party dependencies here -->
- [None configured yet — add as dependencies are approved]

## Architecture Decisions Log

- [ADR-001: State Management — ScriptableObjects + Event Bus](../../docs/architecture/adr-001-state-management.md)
- [ADR-002: Rendering Pipeline — URP 2D Renderer](../../docs/architecture/adr-002-rendering-pipeline.md)
