# Systems Index: Pirate Gacha RPG

> **Status**: Approved
> **Created**: 2026-03-25
> **Last Updated**: 2026-03-25
> **Source Concept**: design/gdd/game-concept.md

---

## Overview

Este juego es un RPG gacha por turnos con temática pirata que requiere dos
sistemas de combate paralelos (terrestre y naval), un sistema de colección
dual (unidades + barcos), y un sistema de sinergias por traits que conecta
ambos modos. Los pilares de diseño exigen profundidad estratégica en ambos
combates, personajes con identidad propia, y respeto al tiempo del jugador
(auto-battle + desafíos manuales). El scope de la demo abarca 22 sistemas
organizados en 5 capas de dependencia.

---

## Systems Enumeration

| # | System Name | Category | Priority | Status | Design Doc | Depends On |
|---|-------------|----------|----------|--------|------------|------------|
| 1 | Unit Data Model | Core | MVP | Approved | [design/gdd/unit-data-model.md](unit-data-model.md) | — |
| 2 | Ship Data Model | Core | MVP | Approved | [design/gdd/ship-data-model.md](ship-data-model.md) | Unit Data Model |
| 3 | Damage & Stats Engine | Core | MVP | Approved | [design/gdd/damage-stats-engine.md](damage-stats-engine.md) | — |
| 4 | Currency System | Economy | MVP | Approved | [design/gdd/currency-system.md](currency-system.md) | — |
| 5 | Game Flow / Scene Manager | Core | MVP | Approved | [design/gdd/game-flow.md](game-flow.md) | — |
| 6 | Initiative Bar | Gameplay | MVP | Approved | [design/gdd/initiative-bar.md](initiative-bar.md) | Unit Data Model |
| 7 | Enemy System | Gameplay | MVP | Approved | [design/gdd/enemy-system.md](enemy-system.md) | Unit Data Model, Damage & Stats Engine |
| 8 | Stage System | Core | MVP | Approved | [design/gdd/stage-system.md](stage-system.md) | Game Flow |
| 9 | Traits/Sinergias | Gameplay | MVP | Approved | [design/gdd/traits-sinergias.md](traits-sinergias.md) | Unit Data Model |
| 10 | Combate Terrestre | Gameplay | MVP | Approved | [design/gdd/combate-terrestre.md](combate-terrestre.md) | Unit Data Model, Damage & Stats Engine, Initiative Bar, Enemy System |
| 11 | Team Composition | Gameplay | MVP | Approved | [design/gdd/team-composition.md](team-composition.md) | Unit Data Model, Ship Data Model |
| 12 | Combate Naval | Gameplay | MVP | Approved | [design/gdd/combate-naval.md](combate-naval.md) | Ship Data Model, Damage & Stats Engine, Enemy System, Traits/Sinergias, Team Composition |
| 13 | Sistema Gacha | Economy | MVP | Approved | [design/gdd/sistema-gacha.md](sistema-gacha.md) | Unit Data Model, Ship Data Model, Currency System |
| 14 | Combat UI | UI | MVP | Approved | [design/gdd/combat-ui.md](combat-ui.md) | Combate Terrestre, Combate Naval, Initiative Bar |
| 15 | Progresión de Unidades | Progression | Vertical Slice | Approved | [design/gdd/progresion-de-unidades.md](progresion-de-unidades.md) | Unit Data Model, Currency System, Sistema Gacha, Stage System |
| 16 | Rewards System | Economy | Vertical Slice | In Review | [design/gdd/rewards-system.md](rewards-system.md) | Currency System, Unit Data Model |
| 17 | Save/Load System | Persistence | Vertical Slice | Designed | [design/gdd/save-load-system.md](save-load-system.md) | Unit Data Model, Ship Data Model, Currency System |
| 18 | Unit Roster/Inventory | Persistence | Vertical Slice | Designed | [design/gdd/unit-roster-inventory.md](unit-roster-inventory.md) | Unit Data Model, Ship Data Model, Progresión, Save/Load |
| 19 | Menus & Navigation UI | UI | Vertical Slice | Designed | [design/gdd/menus-navigation-ui.md](menus-navigation-ui.md) | Team Composition, Gacha, Progresión, Unit Roster, Stage System |
| 20 | Auto-Battle | Gameplay | Vertical Slice | Designed | [design/gdd/auto-battle.md](auto-battle.md) | Combate Terrestre, Combate Naval |
| 21 | Narrative System | Narrative | Vertical Slice | Designed | [design/gdd/narrative-system.md](narrative-system.md) | Stage System, Game Flow |
| 22 | Equipment System | Progression | Vertical Slice | Designed | [design/gdd/equipment-system.md](equipment-system.md) | Unit Data Model |
| 23 | Tutorial/Onboarding | Meta | Full Vision | Not Started | — | All gameplay systems |

---

## Categories

| Category | Description |
|----------|-------------|
| **Core** | Foundation systems everything depends on — data models, scene management, calculation engines |
| **Gameplay** | The systems that make the game fun — combat, traits, team building, auto-battle |
| **Economy** | Resource creation and consumption — gacha, currencies, rewards |
| **Progression** | How the player grows over time — levels, awakening, duplicates |
| **Persistence** | Save state and continuity — save/load, inventory, roster |
| **UI** | Player-facing information displays — combat HUD, menus, navigation |
| **Narrative** | Story and dialogue delivery — visual novel scenes between stages |
| **Meta** | Systems outside the core game loop — tutorial, onboarding |

---

## Priority Tiers

| Tier | Definition | Target Milestone | Design Urgency |
|------|------------|------------------|----------------|
| **MVP** | Required for the core loop to function — both combat modes, gacha, and basic UI | First playable prototype | Design FIRST |
| **Vertical Slice** | Required for a complete demo experience — progression, save, rewards, auto, narrative | Playable demo | Design SECOND |
| **Full Vision** | Polish, meta, and post-demo features — tutorial, events, PvP | Full game | Design as needed |

---

## Dependency Map

### Foundation Layer (no dependencies)

1. **Unit Data Model** — Defines what a unit IS: stats, abilities (land+sea), traits, rarity. 12 systems depend on it.
2. **Ship Data Model** — Defines what a ship IS: stats, role slots, abilities. Required for naval combat and gacha.
3. **Damage & Stats Engine** — Universal formulas for damage, defense, buffs/debuffs. Both combats use it.
4. **Currency System** — Coins (free/premium) and materials. Gacha and progression depend on it.
5. **Game Flow / Scene Manager** — Screen navigation: main menu → stage select → combat → rewards → back.

### Core Layer (depends on Foundation)

6. **Initiative Bar** — depends on: Unit Data Model (needs initiative stat)
7. **Enemy System** — depends on: Unit Data Model, Damage & Stats Engine (enemies use same formulas)
8. **Stage System** — depends on: Game Flow (navigation between stages)
9. **Save/Load System** — depends on: Unit Data Model, Ship Data Model, Currency System (needs to know what to persist)

### Feature Layer (depends on Core)

10. **Combate Terrestre** — depends on: Unit Data Model, Damage & Stats Engine, Initiative Bar, Enemy System
11. **Traits/Sinergias** — depends on: Unit Data Model (traits live on units)
12. **Team Composition** — depends on: Unit Data Model, Ship Data Model (build land teams + naval crews)
13. **Rewards System** — depends on: Currency System, Unit Data Model (drops can be materials or units)
14. **Progresión de Unidades** — depends on: Unit Data Model, Currency System (materials for level-up/awakening)
15. **Sistema Gacha** — depends on: Unit Data Model, Ship Data Model, Currency System (summon units/ships with currency)

### Feature Layer 2 (depends on Features)

16. **Combate Naval** — depends on: Ship Data Model, Damage & Stats Engine, Enemy System, Traits/Sinergias, Team Composition
17. **Auto-Battle** — depends on: Combate Terrestre, Combate Naval (replays combat without player input)
18. **Unit Roster/Inventory** — depends on: Unit Data Model, Ship Data Model, Progresión, Save/Load
19. **Equipment System** — depends on: Unit Data Model (units have 3 equipment slots; equipment grants stats and abilities)

### Presentation Layer (depends on Features)

19. **Combat UI** — depends on: Combate Terrestre, Combate Naval, Initiative Bar
20. **Menus & Navigation UI** — depends on: Team Composition, Gacha, Progresión, Unit Roster, Stage System
21. **Narrative System** — depends on: Stage System, Game Flow

### Polish Layer (depends on everything)

22. **Tutorial/Onboarding** — depends on: all gameplay systems (teaches mechanics, so they must exist first)

---

## Recommended Design Order

| Order | System | Priority | Layer | Est. Effort |
|-------|--------|----------|-------|-------------|
| 1 | Unit Data Model | MVP | Foundation | M |
| 2 | Ship Data Model | MVP | Foundation | M |
| 3 | Damage & Stats Engine | MVP | Foundation | M |
| 4 | Currency System | MVP | Foundation | S |
| 5 | Game Flow / Scene Manager | MVP | Foundation | S |
| 6 | Initiative Bar | MVP | Core | M |
| 7 | Enemy System | MVP | Core | M |
| 8 | Stage System | MVP | Core | S |
| 9 | Traits/Sinergias | MVP | Feature | L |
| 10 | Combate Terrestre | MVP | Feature | L |
| 11 | Team Composition | MVP | Feature | M |
| 12 | Combate Naval | MVP | Feature 2 | L |
| 13 | Sistema Gacha | MVP | Feature | M |
| 14 | Combat UI | MVP | Presentation | M |
| 15 | Progresión de Unidades | Vertical Slice | Feature | M |
| 16 | Rewards System | Vertical Slice | Feature | S |
| 17 | Save/Load System | Vertical Slice | Persistence | M |
| 18 | Unit Roster/Inventory | Vertical Slice | Persistence | S |
| 19 | Menus & Navigation UI | Vertical Slice | Presentation | M |
| 20 | Auto-Battle | Vertical Slice | Feature | M |
| 21 | Narrative System | Vertical Slice | Narrative | S |
| 22 | Equipment System | Vertical Slice | Progression | M |
| 23 | Tutorial/Onboarding | Full Vision | Meta | M |

Effort: S = 1 session, M = 2-3 sessions, L = 4+ sessions.

---

## Circular Dependencies

- **Traits/Sinergias ↔ Combate Naval**: Los traits afectan al combate naval,
  pero el combate naval define cómo se aplican los traits en contexto naval.
  **Resolución**: Diseñar Traits primero con una interfaz genérica de
  "bonificación condicional". El Combate Naval consume esa interfaz y define
  los contextos navales específicos (roles, barco).

No se encontraron otros ciclos.

---

## High-Risk Systems

| System | Risk Type | Risk Description | Mitigation |
|--------|-----------|-----------------|------------|
| Unit Data Model | Scope | La estructura de datos debe soportar habilidades duales (tierra+mar), traits, rarezas, y progresión. Si no es flexible, retrabajar 12 sistemas. | Diseñar primero, prototipar segundo. Usar ScriptableObjects con herencia. |
| Damage & Stats Engine | Design | Las fórmulas deben escalar desde stage 1 hasta end-game sin romper el balance. Dos modos de combate con fórmulas distintas. | Modelar matemáticamente antes de implementar. Usar curvas configurables. |
| Combate Naval | Design / Technical | Sistema más complejo del juego: roles, sinergias, stats de barco + tripulación. Podría sentirse como un mini-juego separado. | Prototipar temprano. Validar que es divertido antes de pulirlo. |
| Traits/Sinergias | Technical | La combinatoria de traits con bonificaciones condicionales es difícil de escalar y debuggear. | Empezar con 3-4 traits simples. Expandir solo después de validar. |
| Sistema Gacha | Approved | Rates (2%/15%/83%), soft+hard pity (60-90), 50/50 garantía, duplicados (+5%×4), Fragmentos de Alma como item de inventario. | — |

---

## Progress Tracker

| Metric | Count |
|--------|-------|
| Total systems identified | 23 |
| Design docs started | 18 |
| Design docs reviewed | 14 |
| Design docs approved | 14 |
| MVP systems designed | 14/14 |
| Vertical Slice systems designed | 6/8 |

---

## Next Steps

- [ ] Design MVP-tier systems first (use `/design-system [system-name]`)
- [ ] Start with **Unit Data Model** — highest-priority, most systems depend on it
- [ ] Run `/design-review` on each completed GDD
- [ ] Prototype **Combate Terrestre** early to validate core loop (`/prototype combate-terrestre`)
- [ ] Prototype **Combate Naval** to validate differentiator
- [ ] Run `/gate-check pre-production` when MVP systems are designed
- [ ] Plan implementation sprint with `/sprint-plan new`
