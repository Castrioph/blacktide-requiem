# Retrospective: Sprint 3 — Content Loop

Period: 2026-04-20 — 2026-06-12
Generated: 2026-06-12

## Metrics

| Metric | Planned | Actual | Delta |
|--------|---------|--------|-------|
| Tasks | 11 (5 Must, 4 Should, 2 Nice) | 9 Done | −2 (ambas Nice) |
| Completion Rate | — | 82% total / **100% Must+Should** | — |
| Effort (T-shirt) | 4S+4M+1L planeado core | 4S+4M+1L Done | 0 |
| Bugs Found (fix commits) | — | 1 | — |
| Bugs Fixed | — | 1 | — |
| Unplanned Tasks Added | — | 1 (S3-11, aprobada por usuario) | — |
| Commits | — | 14 | — |
| EditMode tests (total) | ≥150 DoD | 255 `[Test]` | +105 sobre DoD |

Fix in-sprint: `d442c73 fix(ui): polish MainMenu font size and button target graphic`.

## Velocity Trend

| Sprint | Planned | Completed | Rate |
|--------|---------|-----------|------|
| S1 | 10 | 7 | 70% |
| S2 | 10 | 10 | 100% |
| S3 (current) | 11 | 9 | 82% (100% Must+Should) |

**Trend**: Estable-alta. Los 2 no completados son nice-to-have descope-ados
conscientemente a S4, no fallos de entrega. Primera vez con sizing: la
métrica por tamaño ya es medible (ver Estimation).

**Nota de cadencia**: el sprint duró 53 días de calendario con un hueco de
~7 semanas sin sesiones (21-abr → 12-jun). En sesiones efectivas el trabajo
fue ~6-7 sesiones. Para un solo dev la métrica útil es cards/sesión, no
cards/semana.

## What Went Well

- **100% del camino crítico**: Must (S3-01..05) y Should (S3-06..08, S3-11)
  completos. El loop demo completo existe: MainMenu → StageSelect →
  TeamSelect → Combat → Results → MainMenu, playtested.
- **Patrón specs-por-agentes funcionó**: S3-11 produjo `ui-s311-ux-audit.md`
  (29 hallazgos accionables con evidencia YAML) y `ui-s311-visual-design.md`
  (paleta canónica) antes de tocar una línea — la implementación fue lineal
  porque el diseño estaba cerrado.
- **Aplicador idempotente como patrón nuevo**: `ApplyS311UIPolish.cs`
  (corrible 2× sin duplicados, logs con prefijo) aplicó 29 fixes en 3
  escenas + 2 prefabs + Build Settings en una pasada verificable. Reusable
  para futuros barridos de escenas.
- **Review cruzado cerró huecos**: el review Art confirmó el único item que
  el review UX no pudo verificar (P1-08), y el review UX detectó el
  ColorBlock de BtnBack que Art convirtió en fix de Fase 5.
- **0 deuda técnica**: TODO/FIXME/HACK = 0 por segundo sprint consecutivo.
- **Cadena de rewards cerrada end-to-end**: al detectar que S3-03 nunca tuvo
  data assets, se crearon y cablearon las 3 RewardTables en el mismo sprint
  — el payout de victoria y el preview de botín funcionan con datos reales.

## What Went Poorly

- **S3-03 se marcó Done sin datos**: el sistema RewardTable shipped con 11
  tests pero cero `.asset` autorados y sin wiring a stages. "Paid to Wallet
  on Victory" era acceptance criteria y era imposible que ocurriera en el
  demo real. El criterio se cumplió en test, no en juego. Detección: 7
  semanas después, durante S3-11.
- **S3-08 (mitigación) llegó después del riesgo que mitigaba**: el plan lo
  pedía "early" para proteger las verificaciones de S3-06/07; se documentó
  al final, cuando S3-06/07 ya habían sufrido el disconnect de Coplay que
  pretendía prevenir.
- **Artefactos de escena sucios descubiertos tarde**: escenas duplicadas
  (stubs en `Assets/Scenes/` + reales en raíz), `TeamSelect.unity` mal
  ubicada y 20+ scripts Diag*/Fix* untracked acumulados de sesiones previas.
  Limpieza absorbida por S3-11, pero era ruido evitable.
- **Subagentes de implementación murieron 2× en S3-11** (~10 min y contexto
  perdidos); el pivote a implementación inline con los specs ya escritos
  resolvió. Patrón documentado en memoria del proyecto.

## Blockers Encountered

| Blocker | Duration | Resolution | Prevention |
|---------|----------|------------|------------|
| Coplay MCP caído al abrir sesión S3-11 | ~5 min | Editor estaba arrancando; reconectó solo | Comprobar `tasklist Unity.exe` antes de asumir editor cerrado (en memoria) |
| Batchmode falló (ruta Unity.exe errónea + editor abierto) | ~10 min | Pivote a `execute_script` vía Coplay con wrapper de reflexión | Ruta real documentada: `D:\Unity\6000.3.12f1` (en memoria) |
| 2 agentes ui-programmer muertos a mitad de tarea | ~10 min | Implementación inline usando los specs de Fases 1-2 | Agentes para análisis/spec; inline para implementación larga (en memoria) |

## Estimation Accuracy

Primer sprint con T-shirt sizing (acción #1 de retro S2). Lectura:

| Task | Estimated | Actual | Variance | Likely Cause |
|------|-----------|--------|----------|--------------|
| S3-11 UI Polish | L (4+ sesiones) | 1 sesión | −75% | Specs por agentes + aplicador por script comprimieron el trabajo; L asumía iteración manual en editor |
| S3-06/07 UI | M c/u | ~1 sesión c/u | en línea | — |
| S3-01..05 core | S/M | ~1 sesión c/u | en línea | Specs claros, patrón SO+tests rodado |

**Overall**: 8/9 tareas dentro de ±20%; el único miss grande fue
sobreestimar L. Ajuste: las tareas UI con spec previo + editor scripting
valen M, no L. Reservar L para cross-cutting sin spec.

## Carryover Analysis

| Task | Original Sprint | Times Carried | Reason | Action |
|------|----------------|---------------|--------|--------|
| S3-09 Save/Load básico | S3 (nice) | 1 | Formato de save se decide mejor después del diseño Gacha/Inventory | Re-evaluar en S4 planning |
| S3-10 ADR-004 Naval | S3 (nice) | 1 | No bloqueaba nada en S3; bloquea TODO en S4 | **Primera tarea de S4** |

## Technical Debt Status

- TODO: 0 (previo: 0) · FIXME: 0 (previo: 0) · HACK: 0 (previo: 0)
- Trend: estable en cero
- Concerns no marcados: (a) migración TMP diferida — los textos usan TTF en
  legacy `UI.Text`, suficiente para demo, revisar antes de mobile real;
  (b) ~20 scripts Diag*/Fix*/Verify* untracked en `Assets/Editor/` —
  decidir commitear como toolbox o borrar; (c) navegación D-pad explícita
  por listas (P2-10) pendiente.

## Previous Action Items Follow-Up

| Action Item (Sprint 2) | Status | Notes |
|------------------------|--------|-------|
| 1. T-shirt sizing en el board | Done | Aplicado en sprint-003; primera medición de accuracy arriba |
| 2. Refactor DemoBattleSetup a .assets | Done | S3-04, temprano como pedía |
| 3. Documentar fallback Coplay en CLAUDE.md | Done (tarde) | S3-08 — llegó al final del sprint, no al kickoff |
| 4. DoD "playtested" para tareas UI | Done | S3-06/07/11 todas con verificación playtest antes de Done |
| 5. Retro antes de abrir siguiente sprint | Done | Este documento |

5/5 completadas — sin items recurrentes sin atender.

## Action Items for Next Iteration

| # | Action | Owner | Priority | Deadline |
|---|--------|-------|----------|----------|
| 1 | ADR-004 Naval Combat (ex S3-10) como primera tarea de S4, antes de cualquier implementación naval | technical-director | High | S4 kickoff |
| 2 | Acceptance de sistemas data-driven debe incluir "assets autorados + cableados + visibles en juego", no solo tests | producer | High | S4 kickoff (regla en plantilla de sprint) |
| 3 | Tareas de mitigación de riesgo se ejecutan ANTES que las tareas que protegen, o se eliminan del plan | producer | Med | S4 kickoff |
| 4 | Decidir S3-09 Save/Load en S4 planning, después de definir formato Gacha/Inventory | game-designer | Med | S4 planning |
| 5 | Triaje de scripts Diag*/Fix* untracked en Assets/Editor (commitear toolbox útil, borrar el resto) | tools-programmer | Low | S4 |

## Process Improvements

- **Spec-first con agentes, implementación inline**: el pipeline de S3-11
  (audit → visual spec → aplicador idempotente → review cruzado) es el
  patrón a repetir para cualquier barrido multi-escena.
- **Regla "datos o no es Done"** para sistemas data-driven: un SO sin assets
  cableados es una librería, no una feature (caso S3-03).
- **Ajuste de sizing**: UI-con-spec = M; L solo para cross-cutting sin
  diseño previo.

## Summary

Sprint 3 entregó el shell de demo jugable completo: economía, stages,
rewards con datos reales, composición de equipo y un pase de polish UI que
llevó las 3 pantallas a la paleta canónica con accesibilidad WCAG y soporte
gamepad. 100% del camino crítico, 0 deuda, 5/5 acciones de la retro
anterior cumplidas. El cambio más importante para S4: **acceptance criteria
de sistemas debe exigir datos cableados y visibles en juego** — S3-03 estuvo
"Done" 7 semanas sin poder pagar una sola moneda en el demo real.
