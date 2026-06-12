# Active Session State

<!-- STATUS -->
Epic: Combate Naval
Feature: Sprint 4 — Naval-first (production/sprints/sprint-004.md)
Task: S4-02 ✅ + S4-03 ✅ (286/286 tests). Siguiente: S4-04 naval combat core (L) — NavalTurnResolver: 6 acciones, DoT split, LB, oleadas.
<!-- /STATUS -->

- Updated: 2026-06-12
- Sprint/Task: sprint-004 / S4-03 Naval stats engine ✅
- Verification Path: editor-tools (Coplay check_compile + TestRunnerApi) ✅ 286/286

## S4-02a (commiteado)

- ICombatant (+superficie HP→HHP), ITraitCarrier, ITurnResolver, LandTurnResolver
- InitiativeEntry: ICombatant + helper .Unit; SynergyEvaluator → ITraitCarrier
- CombatManager: resolver inyectable; casts interinos hasta S4-04
- Fix test preexistente TeamComposition (instancias roster) → 255/255
- Runner: Assets/Editor/RunEditModeTests.cs → test-results-s402a.txt (gitignored)

## S4-02b (commiteado)

- ShipCombatant + CrewMemberState + 5 assets Assets/Data/Ships/ + 20 tests
- "Visible en juego" del acceptance se completa en S4-07 (stage naval)
- Crew contribution usa BaseStats nivel 1 (consistente con StageController)

## S4-03 (pendiente commit)

- ShipCombatant.DamageCrewMember: muerte → RecalculateFromCrew +
  EvaluateCrewSynergies en el mismo call (stats, pool y trait count — GDD §6)
- EvaluateCrewSynergies: SynergyEvaluator sobre crew VIVA, capitán crew =
  primario; buffs → BuffStack del barco; re-evaluación idempotente
  (guest 2º capitán diferido a S4-08); GetLivingCrew para targeting
- 11 tests NavalStatsEngineTests → 286/286 (31 nuevos en S4; DoD pide 40+)
- Eventos GameEvents navales (OnCrewDied etc.) diferidos a S4-04 (quien orquesta)

## Contexto S3 (cerrado 2026-06-12)

- Flujo verificado: MainMenu → StageSelect → TeamSelect → Combat → Results → MainMenu ✅
- Retro: production/sprints/sprint-003-retrospective.md — 100% Must+Should
- Regla S4: acceptance data-driven = "assets cableados y visibles en juego"
- Pendiente usuario: playtest manual del flujo S3 (retro #4)

## Pendiente

- Ninguno. Scripts one-shot S3 borrados; ADR-004 commiteado.
