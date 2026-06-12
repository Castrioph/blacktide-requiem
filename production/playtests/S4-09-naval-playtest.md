# Playtest Report — S4-09 Combate Naval (¿es divertido?)

> **Foco**: Open Question #5 del GDD naval — balance Abordaje vs Bombardeo.
> Knobs en juego: `BOARDING_POWER = 0.8` (NavalTurnResolver.cs:18), crew HP por rol (Capitán 800 … Artillero 400).
> **Estado**: COMPLETADO — 3 runs jugadas por el usuario, analizadas por qa-tester.

## Session Info
- **Date**: 2026-06-13
- **Build**: main @ ce7bb51 (S4-07)
- **Duration**: [tiempo jugado]
- **Tester**: Elliot (usuario / solo dev)
- **Platform**: PC (Unity Editor, Game view portrait)
- **Input Method**: ratón (Input System)
- **Session Type**: Targeted test — balance + fun factor

## Test Focus

Tres runs del stage naval (MainMenu → StageSelect → naval → Results):

1. **Run A — Solo Bombardeo**: nunca usar Abordaje. Solo Cañonazo / Habilidad Naval / Reparar / Maniobra.
2. **Run B — Solo Abordaje**: priorizar Abordaje contra crew enemigo (Cañonazo solo si no hay target válido).
3. **Run C — Libre**: jugar "como te pida el cuerpo". Esta run responde "¿es divertido?".

Anotar por run: rondas hasta victoria/derrota, HHP final propio, crew propia muerta, sensación de riesgo.

## First Impressions (primeros 5 minutos)
- **¿Objetivo claro?** Sí (hundir oleadas, volver con rewards)
- **¿Controles claros?** Sí (sin quejas de input/targeting)
- **Respuesta emocional**: Frustrado (oleada 2) + decepcionado (profundidad ausente)
- **Notas**: la queja dominante no es usabilidad sino contenido: "no veo absolutamente nada de todo lo que detallamos".

## Gameplay Flow

### Qué funcionó bien
- Flujo completo MainMenu→StageSelect→naval→Results funciona (ya validado en S4-07).
- Las 3 runs se completaron sin crashes ni bloqueos.

### Pain points
- Habilidad Naval inutilizable (pool vacío) — Severidad: **Alta** (ver root cause)
- Capitán/traits/sinergias invisibles: nada autorado, nada que ver ni explicar — Severidad: **Alta**
- Oleada 2 (2 barcos) imbatible con el kit actual de 4 acciones efectivas — Severidad: **Alta**
- Reparar insuficiente contra daño de 2 barcos por ronda — Severidad: Media (re-evaluar tras contenido)
- Abordaje sin impacto perceptible — Severidad: Media (re-evaluar tras contenido)

### Puntos de confusión
- Traits del capitán: no se ven ni se explican en ningún sitio de la UI.
- "Por qué es tan simple": el jugador no ve nada del diseño detallado en el GDD (habilidades de crew, sinergias, LB) porque no hay datos autorados.

### Momentos de delight
- Ninguno reportado. Veredicto Run C: "bastante seco".

## Root Cause (análisis qa-tester, 2026-06-13)

**El bug #1 y la pobreza de la demo comparten una sola causa: cero contenido naval autorado.**

Evidencia (assets en repo @ ce7bb51):
- Los 5 `ShipData` (`Assets/Data/Ships/`): `BaseAbilities: []`
- Los 7 `UnitData` (`Assets/Data/Characters/`): `SeaAbilities: []` **y** `Traits: []`
- `Assets/Data/Abilities/` contiene solo las 9 habilidades terrestres de S2/S3

Consecuencias en cadena:
1. `ShipCombatant.AbilityPool` (BaseAbilities + SeaAbilities de crew viva) siempre vacío → HUD desactiva botón "HABILIDAD NAVAL" ("Sin habilidades disponibles") → bug #1.
2. Sin traits → `TraitBonuses = 0`, sinergias de capitán nunca activan → nada que mostrar/explicar.
3. Matar crew enemiga no quita habilidades (no tienen) → Abordaje pierde su payoff de diseño → Run B inviable.
4. Sin habilidades ofensivas/curativas extra → jugador limitado a 4 acciones base → Run A inviable y Run C "seca".
5. Enemy AI tampoco usa habilidades → combate enemigo monótono.

**El código está sano**: resolver, pool, cooldowns, sinergias y LB existen y tienen 344 EditMode tests — pero los tests construyen datos sintéticos, por lo que el vacío de assets fue invisible. Incumple la regla retro S3 ("sistemas data-driven con assets autorados, cableados y **visibles en juego**") declarada en el acceptance de S4-02 y en el DoD del sprint.

## Balance Abordaje vs Bombardeo (Open Question #5)

| Métrica | Run A (Bombardeo) | Run B (Abordaje) | Run C (Libre) |
|---------|-------------------|------------------|---------------|
| Resultado (V/D) | Derrota (oleada 2, vs 2 barcos) | Derrota | — |
| HHP propio final (%) | 0 | 0 | — |
| ¿Se sintió viable? | No — Reparar no compensa el daño de 2 barcos; Maniobra + Cañonazo no bastan para la 2ª oleada | No — Abordaje no genera impacto suficiente; mientras desgastas crew el enemigo bombardea el casco y te mata antes | — (veredicto: "seco, necesita más desarrollo") |

- **¿Una estrategia domina?** Ninguna es viable — ambas pierden en oleada 2. El problema no es el ratio entre ellas sino que falta la capa de habilidades/traits (ver root cause).
- **¿Abordaje se siente arriesgado pero recompensante?** No — solo arriesgado. El payoff (quitar habilidades/stats al matar crew) es invisible porque los enemigos no tienen SeaAbilities autoradas que perder.
- **¿Reparar tiene hueco real?** Insuficiente: floor(MST_eff × 1.5) por 20 MP no compensa el daño entrante de 2 barcos por ronda.

- **¿Una estrategia domina?** [Bombardeo/Abordaje/Equilibrado]
- **¿Abordaje se siente arriesgado pero recompensante?** [ ]
- **¿Matar crew enemigo (quitar habilidades/stats) se nota?** [ ]
- **¿Reparar + Maniobra tienen hueco real en la rotación?** [ ]

## Bugs Encountered
| # | Descripción | Severidad | Reproducible |
|---|-------------|-----------|--------------|
| 1 | Habilidad Naval no hace nada (botón siempre desactivado: pool vacío) | Alta | Sí — 100%, root cause confirmado (datos, no código) |
| 2 | Traits/sinergias de capitán invisibles (cero traits autorados) | Alta | Sí — 100%, misma causa |

## Overall Assessment
- **¿Es divertido? (pregunta del sprint, riesgo #1)**: **Aún no.** "Bastante seco, necesita más desarrollo. Súper pobre, no veo nada de lo que detallamos." PERO: el veredicto no es sobre el diseño — es sobre una demo que ejecuta ~40% del diseño porque falta todo el contenido de habilidades/traits. El riesgo #1 ("naval no es divertido") queda **sin resolver**, no confirmado.
- **Dificultad**: Muy difícil (oleada 2 imbatible) — pero con el kit incompleto; re-medir tras contenido.
- **Pacing**: monótono (4 acciones, sin variedad enemiga).

## Top 3 Priorities
1. **Autorar contenido naval** (bloquea todo lo demás): SeaAbilities por rol de crew (aliadas y enemigas), BaseAbilities de barco, Traits de capitán + crew. Sin esto el playtest de balance no es medible.
2. **Visibilidad de traits/sinergias en UI**: panel o tooltip que muestre traits del capitán y sinergias activas (hueco de S4-06).
3. **Re-balance oleada 2 tras contenido**: con habilidades reales, re-medir Reparar/BOARDING_POWER; si sigue imbatible, bajar stats de la 2ª oleada o subir REPAIR_POWER.

## Decisión de knobs
- `BOARDING_POWER` (0.8) y crew HP: **DIFERIDA** — no medible con pool de habilidades vacío (ninguna estrategia es viable, no por los knobs sino por contenido faltante). Re-playtest tras autorar contenido. Open Question #5 sigue abierta.

## Acción derivada
- Nueva tarea propuesta **S4-10 (Must, regresión de acceptance S4-02)**: autorar y cablear contenido naval — ≥6 SeaAbilities (crew aliada + enemiga), ≥2 BaseAbilities por barco con AI tier, traits de capitán, visibles en juego. Después: re-run S4-09 (runs A/B/C) para cerrar Open Question #5.
