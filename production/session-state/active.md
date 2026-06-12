# Active Session State

<!-- STATUS -->
Epic: Combate Naval
Feature: Sprint 4 — Naval-first (production/sprints/sprint-004.md)
Task: S4-01 ✅ + S4-02a refactor ✅ (255/255 tests). Siguiente: S4-02b ShipCombatant + CrewMemberState + assets.
<!-- /STATUS -->

- Updated: 2026-06-12
- Sprint/Task: sprint-004 / S4-02a extracción ICombatant ✅
- Verification Path: editor-tools (Coplay check_compile + TestRunnerApi) ✅ 255/255

## S4-01 entregado

- ADR-004 Accepted (commit f...): reuso por composición — ICombatant,
  crew pasiva, ITurnResolver (Land/Naval), CombatAction extendida, ITraitCarrier

## S4-02a entregado (pendiente commit)

- Nuevos: ICombatant.cs, ITraitCarrier.cs, ITurnResolver.cs, LandTurnResolver.cs
- ICombatant incluye superficie HP (CurrentHP/MaxHP/ApplyDamage/Healing → barco=HHP; ADR actualizado)
- InitiativeEntry.Combatant: ICombatant + helper .Unit (CombatantState|null)
- CombatManager: resolver inyectable (default Land), casts interinos a CombatantState
  en listas/eventos — S4-04 los generaliza
- Tests: gate verificado con stash-baseline (fallo TeamComposition era PREEXISTENTE,
  bug del test: instancias de roster distinto; arreglado → 255/255)
- Runner permanente: Assets/Editor/RunEditModeTests.cs (TestRunnerApi → test-results-s402a.txt)

## Contexto S3 (cerrado 2026-06-12)

- Flujo verificado: MainMenu → StageSelect → TeamSelect → Combat → Results → MainMenu ✅
- Retro: production/sprints/sprint-003-retrospective.md — 100% Must+Should
- Regla S4: acceptance data-driven = "assets cableados y visibles en juego"
- Pendiente usuario: playtest manual del flujo S3 (retro #4)

## Pendiente

- Ninguno. Scripts one-shot S3 borrados; ADR-004 commiteado.
