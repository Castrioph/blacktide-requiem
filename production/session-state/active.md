# Active Session State

<!-- STATUS -->
Epic: Combate Naval
Feature: Sprint 4 — Naval-first (production/sprints/sprint-004.md)
Task: S4-06 ✅ (812ee18). S4-07 implementado + flujo verificado con capture; PENDIENTE playtest usuario (DoD sprint) y commit.
<!-- /STATUS -->

- Updated: 2026-06-13
- Sprint/Task: sprint-004 / S4-07 stage naval + flujo — 344/344 tests (9 nuevos)
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

## Pendiente (antes de cerrar S4-06+S4-07 como Done)

- Playtest usuario flujo completo: Play en MainMenu → Misiones → "Mar de los
  Lamentos" → ZARPAR → equipo → combate naval (3 oleadas, jefe) → Results →
  Volver. Sims de apoyo: SimNavalFlow.cs, SimNavalActions.cs.
- Commit S4-07 tras playtest. Quedan: S4-08/S4-09 (nice-to-have) + retro.
