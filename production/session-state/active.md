# Active Session State

<!-- STATUS -->
Epic: Combate Naval
Feature: Sprint 4 — Naval-first (production/sprints/sprint-004.md)
Task: S4-02 COMPLETO (a: refactor ✅ commiteado; b: ShipCombatant+assets ✅ pendiente commit). Siguiente: S4-03 stats engine + recálculo.
<!-- /STATUS -->

- Updated: 2026-06-12
- Sprint/Task: sprint-004 / S4-02 Ship Data Model ✅
- Verification Path: editor-tools (Coplay check_compile + TestRunnerApi) ✅ 275/275

## S4-02a (commiteado)

- ICombatant (+superficie HP→HHP), ITraitCarrier, ITurnResolver, LandTurnResolver
- InitiativeEntry: ICombatant + helper .Unit; SynergyEvaluator → ITraitCarrier
- CombatManager: resolver inyectable; casts interinos hasta S4-04
- Fix test preexistente TeamComposition (instancias roster) → 255/255
- Runner: Assets/Editor/RunEditModeTests.cs → test-results-s402a.txt (gitignored)

## S4-02b (pendiente commit)

- ShipCombatant (ICombatant: hull, MP, statuses+inmunidades, cooldowns, pool,
  cache aditivo base+upgrade+crew × buff modifier) + CrewMemberState (HP fijo
  por rol, ITraitCarrier → buffs al BuffStack del barco)
- 5 assets: Assets/Data/Ships/ (Marea Espectral aliado, Balandra/Bergantín/
  Galeón enemigos N/E/J, Serpiente Abisal criatura sin crew)
- 20 tests nuevos (ShipCombatantTests 16 + ShipAssetsTests 4) → 275/275
- Nota: "visible en juego" del acceptance se completa en S4-07 (stage naval)
- Crew contribution usa BaseStats nivel 1 (consistente con StageController)

## Contexto S3 (cerrado 2026-06-12)

- Flujo verificado: MainMenu → StageSelect → TeamSelect → Combat → Results → MainMenu ✅
- Retro: production/sprints/sprint-003-retrospective.md — 100% Must+Should
- Regla S4: acceptance data-driven = "assets cableados y visibles en juego"
- Pendiente usuario: playtest manual del flujo S3 (retro #4)

## Pendiente

- Ninguno. Scripts one-shot S3 borrados; ADR-004 commiteado.
