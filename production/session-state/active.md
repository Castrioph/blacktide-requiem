# Active Session State

<!-- STATUS -->
Epic: Combate Naval
Feature: Sprint 4 — Naval-first (production/sprints/sprint-004.md)
Task: S4-05 ✅ implementado (335/335 tests), pendiente commit. Siguiente: S4-06 Naval Combat UI (team-ui, spec-first).
<!-- /STATUS -->

- Updated: 2026-06-12
- Sprint/Task: sprint-004 / S4-05 enemigos navales AI ✅ (AC 25-27; AC 28 ya en S4-04)
- Verification Path: editor-tools (Coplay check_compile ✅ + TestRunnerApi ✅ 335/335)

## S4-05 (implementado, sin commit)

- NavalEnemyAI NUEVO (Core/AI): ICombatInput naval. Perfiles: Agresivo (menor
  HHP; abordaje solo con kill garantizado — estima min damage determinista),
  Estratega NUEVO enum (mayor amenaza FPW/MST; habilidad con ventaja elemental;
  abordaje táctico al Capitán), Defensivo (buff→ataque, sin Guard), Caótico
  (RNG inyectable). Elite Profile+: heal self bajo 30% HHP. Jefe: fases por
  HP% (NavalBossPhase, one-directional) + LB vía SeaAbility entries.
- Reglas globales: enemigos nunca Maniobra/Reparar/Guard; abordaje requiere
  crew viva en AMBOS lados (criaturas nunca abordan ni son abordadas).
- ShipData += Tier/AIProfile/BossPhases; EnemyTier NUEVO enum.
- Assets cableados: balandra Normal/Agresivo, bergantín Elite/Estratega,
  galeón Jefe (fase 2 Estratega <50%), serpiente Normal criatura.
- Tests: NavalEnemyAITests.cs (17) + ShipAssetsTests +1.
- FIX tests S4-04: 3 asserts asumían HDF enemigo 80; real 83 (+3 Artillero
  crew contribution) → bases 100→97, 200→194. Eran flaky por orden RNG.

## Notas para S4-06/07

- CombatRunner usa _defaultEnemyAI compartido (land); naval necesita 1
  NavalEnemyAI POR enemigo (boss tiene estado de fase) — wiring en S4-07
  vía NavalEnemyAI.FromShipData(shipData).
- Crew enemiga: quien arma batalla asigna units a RoleSlots (S4-07 decide
  fuente: preset en StageData naval o crew default por barco).
- Sinergias crew: ship.EvaluateCrewSynergies() lo llama quien arma la batalla.

## Pendiente

- Commit S4-05 (usuario debe aprobar).
