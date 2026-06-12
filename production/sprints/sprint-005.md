# Sprint 5 — Vertical Slice Visual (PIVOT)

> **Status**: Active
> **Method**: Kanban (WIP=1) + T-shirt sizing (S/M/L)
> **Created**: 2026-06-13
> **Predecessor**: sprint-004 (cerrado anticipadamente — pivot decision 2026-06-13)
> **Contexto del pivot**: tras playtest S4-09 el usuario consideró cancelar el
> proyecto: 4 sprints de sistemas testeados (344 tests) pero cero contenido
> autorado y presentación de texto plano. Decisión (opción 1): congelar TODOS
> los sistemas nuevos y dedicar un sprint a que UNA batalla naval se vea y se
> sienta como un juego. Ver production/playtests/S4-09-naval-playtest.md.

## Sprint Goal

Una batalla naval (stage_004) que el usuario juegue y diga "esto ya parece un
juego": arte, contenido de habilidades/traits autorado y visible, feedback
visual de combate (juice), música y SFX. **Criterio de éxito binario del
usuario; si falla, decisión de cancelación con datos.**

## Reglas del pivot (vigentes todo el sprint)

1. **Cero sistemas nuevos.** Solo contenido, arte, audio y presentación sobre
   el motor existente. S4-08, gacha y save/load congelados.
2. **Toda tarea termina en algo visible/audible en pantalla.** Nada se da por
   Done sin `capture_ui_canvas` o playtest del usuario.
3. **Assets first, wiring second** (regla memoria 2026-04-21).
4. Agentes especializados obligatorios para arte/UX (art-director, ux-designer).

## Capacity

- Sesiones estimadas: ~7-8 (velocity S4: 2S+4M+1L)
- Buffer: ~20%
- Carga planeada: 2S + 4M + 1S/M

## Tasks

### Must Have (Critical Path)

| ID | Task | Owner | Size | Dependencies | Acceptance Criteria |
|----|------|-------|------|-------------|---------------------|
| S5-01 | Art Bible + pipeline de arte IA | art-director | S | — | Dirección visual (pirate/voodoo/mystic, paleta canónica existente), specs por tipo de asset (sprites barco, retratos, iconos, fondos), prompts/pipeline reproducible (Coplay generate_or_edit_images / Pixellab), 2-3 assets de prueba aprobados por el usuario. **Primera tarea: bloquea S5-03.** |
| S5-02 | Contenido naval autorado (fix root cause S4-09) | game-designer + gameplay-programmer | M | — (paralelo a S5-01) | ≥6 SeaAbilities (crew aliada + enemiga, por rol), ≥2 BaseAbilities por barco, 3 traits de capitán (vertical-slice-scope) + sinergias activables; cableado a los 4 barcos + criatura y a AI tiers; botón Habilidad Naval funcional en juego. EditMode tests de datos (assets no vacíos). |
| S5-03 | Arte de batalla naval | art-director + technical-artist | M | S5-01 | Sprites de los 4 barcos + serpiente abisal, fondo marino del stage, retratos de crew en chips, iconos de las 6 acciones + habilidades. Importados, cableados en NavalShipView/HUD, verificados con capture_ui_canvas. |
| S5-04 | Combat juice | gameplay-programmer + technical-artist | M | S5-03 | Números de daño flotantes, hit flash + shake al impacto, tween de barcos (balanceo idle, retroceso), barras HHP/MP animadas, feedback visual de estados y de muerte de crew. Lista CERRADA — sin añadidos fuera de esta. Playtest usuario. |
| S5-05 | Audio: música + SFX | audio-director + sound-designer | S/M | S5-04 parcial | Música de batalla naval (loop), SFX: cañonazo, abordaje, reparar, maniobra, habilidad, victoria, derrota (Coplay generate_music/generate_sfx). Mezcla básica sin clipping. Playtest usuario. |

### Should Have

| ID | Task | Owner | Size | Dependencies | Acceptance Criteria |
|----|------|-------|------|-------------|---------------------|
| S5-06 | Visibilidad de traits/sinergias en UI | ui-programmer + ux-designer | S | S5-02 | Panel/tooltip que muestra traits del capitán y sinergias activas con explicación (queja directa del playtest: "no se ve nada de los traits ni se explica"). |
| S5-07 | Re-balance + re-playtest S4-09 | qa-tester + usuario | S | S5-02..05 | Re-run runs A/B/C con contenido; oleada 2 superable con al menos una estrategia; decisión de knobs BOARDING_POWER/crew HP (cierra Open Question #5). |

### Nice to Have

| ID | Task | Owner | Size | Dependencies | Acceptance Criteria |
|----|------|-------|------|-------------|---------------------|
| S5-08 | Arte en MainMenu/StageSelect/TeamSelect | art-director | S | S5-01 | Retratos y fondos en pantallas previas al combate para coherencia del slice. |

## Carryover from Previous Sprint

| Task | Reason | New Estimate |
|------|--------|-------------|
| S4-08 guest 2º capitán | Congelado por pivot; sin traits visibles sería invisible en juego | Post-pivot (S6 si el gate pasa) |
| Gacha + Save/Load (ex-plan S5) | Desplazados por el pivot | S6+ |

## Risks

| Risk | Probability | Impact | Mitigation |
|------|------------|--------|------------|
| Arte IA inconsistente entre assets | Alta | Alto | Art Bible PRIMERO (S5-01) con prompts fijos y paleta; aprobar muestras con el usuario antes de producir en lote |
| "Parece un juego" es subjetivo | Media | Crítico | Referencia concreta: screenshot de combate FFBE como checklist visual en S5-01; checkpoints con el usuario tras S5-03 y S5-04, no solo al final |
| Juice scope creep | Media | Medio | Lista cerrada en acceptance de S5-04; todo extra a backlog |
| Coplay caído en sesiones de arte/audio | Media | Medio | Fallback documentado en CLAUDE.md; generación de imágenes también posible fuera de Coplay (Pixellab/Midjourney + import manual) |
| Fatiga/desmotivación del usuario | Media | Crítico | Checkpoints visuales frecuentes (algo que VER cada sesión); sprint corto y con final binario |

## Dependencies on External Factors

- Generadores IA (Coplay image/sfx/music, Pixellab/Midjourney) disponibles.

## Definition of Done (Sprint 5)

- [ ] Todas las Must Have (S5-01..05) Done
- [ ] Cada tarea verificada visualmente (capture_ui_canvas o playtest usuario)
- [ ] Cero assets con listas vacías en datos cableados al stage demo
- [ ] **Gate final: el usuario juega stage_004 completo y responde a "¿esto ya
      parece un juego?" — Sí → continuar a S6; No → conversación de cancelación
      con datos**
- [ ] Retro escrita al cierre
