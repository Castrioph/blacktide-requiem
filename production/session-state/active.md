# Active Session State

<!-- STATUS -->
Epic: Combate Naval
Feature: Sprint 4 — Naval-first (production/sprints/sprint-004.md)
Task: S4-06 ✅ (812ee18) + S4-07 ✅ (ce7bb51, playtested por usuario). Sprint core completo. Siguiente: S4-09 playtest report (¿es divertido?) o S4-08 guest capitán o retro.
<!-- /STATUS -->

- Updated: 2026-06-13
- Sprint/Task: sprint-004 / S4-07 ✅ commiteado y pushed — 344/344 tests
- Verification Path: editor-tools (compile ✅, flow sim MainMenu→StageSelect→
  TeamSelect→NavalCombat→Results ✅, Results muestra "150 Doblones, 5 Gemas")

## S4-07 estado

- NavalStageData : StageData (PlayerShip + NavalWaves + EnemyCrewPool) +
  NavalStageController.BuildNavalBattle (pure C#, 9 tests) — crew ciclada en
  slots, 1 NavalEnemyAI por enemigo, sinergias evaluadas.
- GameFlowManager.LoadCombat → escena NavalCombat si stage is NavalStageData
  (SceneRegistry.NavalCombat). Bootstrap flow-driven con fallback demo.
- Assets: stage_004_mar_de_los_lamentos (registrado en StageRegistry, accent
  teal en StageAccentPalette), reward_stage_004 (150 DOB + 5 GDC),
  player_wallet (Assets/Data/Economy/). RewardDispatcher conectado en
  bootstrap; Results muestra línea "Recompensas:" en victoria.
- FIX: Results.unity tenía StandaloneInputModule legacy → InputSystemUIInputModule
  (FixEventSystemModules.cs recorre todas las escenas del build).
- Gotcha: estado de play session contamina GFM entre corridas de sim —
  verificar SelectedStage tras cada paso si un sim da resultados raros.

## Pendiente

- Sprint 4 Must+Should completos (S4-01..07 ✅, playtest usuario OK tras fijar
  Game view a portrait). Nice-to-have: S4-08 guest 2º capitán, S4-09
  /playtest-report (Open Question #5: balance Abordaje vs Bombardeo).
- Cierre de sprint: /retrospective antes de abrir Sprint 5 (gacha + save/load).
