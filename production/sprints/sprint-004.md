# Sprint 4 — Combate Naval (Naval-first)

> **Status**: Active
> **Method**: Kanban (WIP=1) + T-shirt sizing (S/M/L)
> **Created**: 2026-06-12
> **Kickoff**: 2026-06-12
> **Predecessor**: sprint-003 (100% Must+Should, retro: sprint-003-retrospective.md)
> **Scope decision (user)**: Naval-first. Gacha + Save/Load completos diferidos a Sprint 5.

## Sprint Goal

Combate naval jugable end-to-end: el jugador entra desde StageSelect a una
misión naval, lucha con su barco (6 acciones, crew system, abordaje) contra
oleadas de barcos enemigos, y vuelve con rewards — validando que el sistema
diferenciador del juego es divertido antes de pulirlo.

## Capacity

- Sesiones estimadas: ~7-8 (cadencia solo-dev irregular; S3 entregó 2S+4M+1L equivalente)
- Buffer: ~20% (1-2 sesiones para imprevistos/fixes)
- Carga planeada core: 2S + 4M + 1L (en línea con velocity S3)
- Ajuste de sizing (retro S3): UI-con-spec = M; L solo para cross-cutting sin spec

## Tasks

### Must Have (Critical Path)

| ID | Task | Owner | Size | Dependencies | Acceptance Criteria |
|----|------|-------|------|-------------|---------------------|
| S4-01 | ADR-004: Naval Combat Architecture (ex S3-10) | technical-director | S | design/gdd/combate-naval.md, ship-data-model.md | ADR resuelve: reutilización vs paralelo respecto a CombatManager terrestre; cómo se integra Initiative Bar con barcos; interfaz de traits en contexto naval; compatibilidad con evolución grid post-demo. **Primera tarea del sprint, bloquea todo.** |
| S4-02 | Ship Data Model: ShipData SO + 7 roles de crew + CrewContribution | gameplay-programmer | M | S4-01 | ShipData SO (HHP/MP/FPW/MST/SPD, slots por rol), crew HP fijo por rol (Capitán 800 … Artillero 400), CrewContribution a stats/habilidades. **Assets autorados y cableados** (≥1 barco aliado + ≥3 enemigos navales + ≥1 criatura marina), visibles en juego (regla retro S3). EditMode tests. |
| S4-03 | Naval stats engine: EffectiveStat + recálculo al morir crew | gameplay-programmer | M | S4-02 | `EffectiveStat = Base + Upgrade + CrewContribution + TraitBonuses`; muerte de crew recalcula stats y quita sus SeaAbilities del pool inmediatamente; capitán muerto desactiva sinergias. EditMode tests. |
| S4-04 | Naval combat core: rondas, 6 acciones, crew combat, DoTs, LB, oleadas | gameplay-programmer | L | S4-03 | Acciones: Cañonazo, Habilidad Naval, Maniobra Evasiva (50% casco+crew), Abordaje (FPW vs DEF crew, no contra criaturas), Reparar (MST×REPAIR_POWER, funciona bajo Silencio), Pasar. DoTs naval (Quemadura→HHP solo con acción; Veneno/Sangrado→crew aleatoria viva). LB naval (turno extra de barco, max 1/ronda). Oleadas con persistencia de estado. Victoria/derrota. Cubre AC 1-24 + 33-37 del GDD. EditMode tests (≥25). |

### Should Have

| ID | Task | Owner | Size | Dependencies | Acceptance Criteria |
|----|------|-------|------|-------------|---------------------|
| S4-05 | Enemigos navales: AI tiers + criaturas marinas | ai-programmer | M | S4-04 | Barcos Normal/Elite/Jefe con perfiles del Enemy System (Jefe con fases + LB); criaturas marinas sin abordaje bidireccional; enemigos no usan Maniobra/Reparar; inmunidades (Sueño/Aturdimiento/Muerte). AC 25-28. EditMode tests. |
| S4-06 | Naval Combat UI (spec-first con team-ui) | team-ui | M | S4-04 | Spec visual ANTES de implementar (patrón S3-11); paleta canónica; acciones + crew targeting para Abordaje + barras HHP/MP + crew status; capture Coplay + playtest usuario (DoD retro S2-#4). |
| S4-07 | Stage naval demo + integración en flujo | gameplay-programmer | S | S4-04, S3 flow | StageData naval (tipo de stage naval, oleadas navales) seleccionable en StageSelect; flujo MainMenu→StageSelect→(naval)→Combat Naval→Results→MainMenu; RewardTable cableada y visible. Playtest usuario. |

### Nice to Have

| ID | Task | Owner | Size | Dependencies | Acceptance Criteria |
|----|------|-------|------|-------------|---------------------|
| S4-08 | Sinergias navales avanzadas: guest como 2º Capitán | gameplay-programmer | S | S4-03 | Doble activación de sinergias con guest; muerte de 2º capitán desactiva solo las suyas. AC 29-32 completos. |
| S4-09 | Playtest report estructurado del combate naval (¿es divertido?) | qa-tester + usuario | S | S4-06, S4-07 | `/playtest-report` con foco en balance Abordaje vs Bombardeo (Open Question #5 del GDD); decisión informada para knobs BOARDING_POWER/crew HP. |

## Carryover from Previous Sprint

| Task | Reason | New Estimate |
|------|--------|-------------|
| S3-10 → S4-01 ADR-004 Naval | No bloqueaba S3; bloquea todo S4 | S (primera tarea) |
| S3-09 Save/Load | Decisión de planning: diferido a S5 junto a Gacha (el formato de save depende del inventario/roster de gacha) | — (S5) |

## Risks

| Risk | Probability | Impact | Mitigation |
|------|------------|--------|------------|
| Naval no es divertido (riesgo #1 del systems-index) | Media | Crítico | S4-04 jugable cuanto antes; S4-09 playtest report con pregunta explícita; knobs BOARDING_POWER/crew HP listos para iterar |
| Duplicación masiva con CombatManager terrestre | Media | Alto | ADR-004 primero (S4-01) decide reuso vs paralelo ANTES de escribir código |
| UI naval crece a L (patrón S2-05/S3-11) | Media | Medio | Spec-first obligatorio (S4-06 exige spec visual antes de código); sizing M con cut-line claro |
| Crew targeting UI confusa (elegir crew enemigo para Abordaje) | Media | Medio | Wireframe en el spec de S4-06; fallback: lista simple de crew en panel |
| Sistemas sin datos (lección S3-03) | Baja | Alto | Regla en acceptance de S4-02/S4-07: assets autorados + cableados + visibles en juego |

## Dependencies on External Factors

- Ninguna. Todo interno al repo. Coplay/Unity env documentado en memoria.

## Definition of Done (Sprint 4)

- [ ] Todas las Must Have (S4-01..04) Done
- [ ] Flujo naval completo **playtested por el usuario** (StageSelect → naval → Results)
- [ ] Sistemas data-driven con assets autorados, cableados y visibles en juego (regla retro S3)
- [ ] Tareas UI con verificación playtest antes de Done (retro S2 #4)
- [ ] 40+ EditMode tests nuevos (total ≥ 295)
- [ ] Cero TODO/FIXME/HACK en `Assets/Scripts/`
- [ ] Sin bugs S1-S4 en features entregadas
- [ ] Retro escrita al cierre, antes de abrir Sprint 5

## Notes

- **Gacha + Save/Load → Sprint 5** (decisión usuario 2026-06-12, opción
  Naval-first). Razón: naval es el diferenciador y el sistema de mayor
  riesgo de diseño; gacha es mecánicamente simple y sin save aporta poco.
- Mitigaciones de riesgo van ANTES de las tareas que protegen (retro S3 #3):
  por eso ADR-004 es S4-01 y el spec de UI precede a su implementación.
- Open Questions del GDD naval que este sprint NO resuelve: evolución a grid
  (post-demo), municiones/viento (descartado demo), fortresses (post-demo).
