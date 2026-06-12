# Active Session State

<!-- STATUS -->
Epic: Combate Naval
Feature: Sprint 4 — Naval-first (production/sprints/sprint-004.md)
Task: S4-04 ✅ implementado y verificado (317/317 tests). Pendiente: commit (pedir OK). Siguiente: S4-05 enemigos navales AI (M).
<!-- /STATUS -->

- Updated: 2026-06-12
- Sprint/Task: sprint-004 / S4-04 naval combat core ✅ (sin commit)
- Verification Path: editor-tools (Coplay check_compile ✅ + TestRunnerApi ✅ 317/317)

## S4-04 (pendiente commit)

- NavalTurnResolver NUEVO: 6 acciones, DoT split (Quemadura→HHP solo con acción;
  Veneno/Sangrado→crew aleatoria viva, RNG inyectable), LB naval máx 1/ronda
  (OnKill/OnLowHP/OnElementAdvantage/OnStatusTarget; OnCrit/OnAllyDown nunca),
  knobs: BOARDING_POWER 0.8, REPAIR_POWER 1.5, REPAIR_MP_COST 20, MANEUVER_REDUCTION 0.5
- Generalización ICombatant completada (los casts "S4-04 generalizes" eliminados):
  CombatManager listas/eventos, CombatContext, GameEvents → ICombatant
- ICombatant += DisplayName, CurrentMP/MaxMP/ConsumeMP
- Eventos navales: OnCrewDamaged, OnCrewDied, OnShipStatsRecalculated,
  OnManeuverActivated; DamageSource.Boarding
- CombatAction: +Maneuver/Boarding/Repair, +SingleCrewEnemy, Target→ICombatant,
  +TargetCrew; factories navales
- HUDs land/EnemyAI/PlayerCombatInput: pattern-match a CombatantState (early return)
- Tests: NavalTurnResolverTests.cs 31 tests TDD (RED por compile verificado →
  GREEN 317/317). AC cubiertos: 3-5, 7-24, 28, 32-34, 36

## Notas para S4-05/06/07

- Sinergias crew: quien arma la batalla llama ship.EvaluateCrewSynergies() (S4-07)
- MaxHHP/MaxMP fijados en constructor SIN sinergias (gap menor GDD §1.3-6;
  relevante solo si una sinergia da +HHP%)
- AoE enemigo usa GetAliveWaveEnemies (mismo límite que land: AI enemiga con AoE
  golpearía a su propio bando — irrelevante hasta S4-05)
- AC 25-27 (abordaje enemigo AI, criaturas no abordan) = S4-05; AC 29-31 guest = S4-08

## Pendiente

- Commit S4-04 (esperando OK del usuario)
