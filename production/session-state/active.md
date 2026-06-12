# Active Session State

<!-- STATUS -->
Epic: Combate Naval
Feature: Sprint 4 — Naval-first (production/sprints/sprint-004.md)
Task: S4-01 ADR-004 DONE (Accepted). Siguiente: S4-02a refactor ICombatant, luego S4-02 Ship Data Model.
<!-- /STATUS -->

- Updated: 2026-06-12
- Sprint/Task: sprint-004 / S4-01 ADR-004 Naval Combat Architecture ✅
- Verification Path: ADR review usuario ✅ (4 puntos confirmados)

## S4-01 entregado

- ADR-004 Accepted: docs/architecture/adr-004-naval-combat-architecture.md
- Decisión: reuso por composición — ICombatant (CombatantState + ShipCombatant),
  crew = sub-entidad pasiva, ITurnResolver (Land/Naval), CombatAction extendida,
  SynergyEvaluator → ITraitCarrier
- Data model naval YA existe parcial: ShipData/ShipStatBlock/NavalRole/RoleSlot
- decision-log.md actualizado

## Contexto S3 (cerrado 2026-06-12)

- Flujo verificado: MainMenu → StageSelect → TeamSelect → Combat → Results → MainMenu ✅
- Retro: production/sprints/sprint-003-retrospective.md — 100% Must+Should
- Regla S4: acceptance data-driven = "assets cableados y visibles en juego"
- Pendiente usuario: playtest manual del flujo S3 (retro #4)

## Pendiente

- Ninguno. Scripts one-shot S3 borrados; ADR-004 commiteado.
