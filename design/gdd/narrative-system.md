# Narrative System

> **Status**: Designed
> **Author**: User + Claude Code Game Studios agents
> **Last Updated**: 2026-04-05
> **Implements Pillar**: Pillar 3 (Personajes con Identidad)
> **Scope Note**: This is a **demo placeholder** system. The narrative format,
> story, and delivery will be redesigned for the full game.

## Overview

El Narrative System entrega escenas de historia en formato visual novel (fondo +
retrato de personaje + caja de diálogo) en puntos clave de la progresión del
Stage System. Para la demo, las escenas son lineales, siempre saltables, y se
disparan solo en intro y final de cada capítulo (4 escenas totales: intro Ch1,
final Ch1, intro Ch2, final Ch2).

El protagonista es un capitán genérico (sin nombre fijo para la demo —
placeholder). La historia es placeholder: establece contexto mínimo para
justificar por qué el jugador está luchando en cada capítulo, pero no es
contenido definitivo.

El sistema consta de:
- **NarrativeScene**: secuencia de pasos (diálogo, cambio de fondo, transición)
- **NarrativePlayer**: motor que ejecuta la secuencia paso a paso
- **NarrativeTrigger**: enganche con Stage System vía los campos
  `NarrativeBefore`/`NarrativeAfter`

Las escenas se definen como datos (ScriptableObject o JSON), no hardcodeadas,
para que sean fáciles de reemplazar cuando llegue el sistema definitivo.

## Player Fantasy

**"¿Por qué estoy luchando?"** — El Narrative System responde esa pregunta. No
busca contar una historia épica (eso es para la versión completa), sino dar el
mínimo contexto para que los combates se sientan con propósito y no como una
lista de peleas random.

El jugador es un capitán pirata que acaba de empezar su aventura. Las escenas
le dan:
- Un motivo para avanzar ("hay un tesoro al final de esta isla")
- Un antagonista básico por capítulo ("este pirata controla la zona")
- Un momento de cierre al completar un arco ("derrotaste al pirata, la isla es
  tuya")

El sistema falla si el jugador siente que las escenas son un estorbo entre él y
el gameplay. Por eso son cortas (máximo ~1 minuto de lectura), siempre
saltables, y solo aparecen en puntos clave (no entre cada batalla).

Para la demo, la narrativa es la capa más fina posible: suficiente para que el
juego no se sienta vacío, poco suficiente para no invertir tiempo en contenido
que será reemplazado.

## Detailed Rules

### 1. Estructura de datos — NarrativeStep

Una escena narrativa es una lista ordenada de **steps**. Cada step es una
instrucción atómica:

| Step Type | Campos | Descripción |
|-----------|--------|-------------|
| `Dialogue` | `speaker`, `text`, `portrait`, `position` | Muestra texto con retrato del hablante |
| `SetBackground` | `backgroundId`, `transition` | Cambia el fondo (fade, cut, slide) |
| `ShowCharacter` | `characterId`, `portrait`, `position`, `transition` | Muestra/cambia un personaje en pantalla |
| `HideCharacter` | `characterId`, `transition` | Quita un personaje |
| `Wait` | `seconds` | Pausa breve (para timing dramático) |
| `PlaySFX` | `sfxId` | Efecto de sonido puntual |
| `PlayBGM` | `bgmId`, `fadeIn` | Cambia la música de fondo |
| `EndScene` | — | Marca el fin de la escena |

**Posiciones de personaje**: `Left`, `Center`, `Right` (máximo 2 personajes
simultáneos para la demo).

### 2. NarrativeScene Data

```yaml
NarrativeScene:
  sceneId: "ch1_intro"
  displayName: "El Naufragio"
  steps:
    - { type: SetBackground, backgroundId: "beach_storm", transition: "fade" }
    - { type: ShowCharacter, characterId: "captain", portrait: "neutral", position: "left" }
    - { type: Dialogue, speaker: "Capitán", text: "¿Dónde... dónde estoy?" }
    - { type: ShowCharacter, characterId: "old_sailor", portrait: "smiling", position: "right" }
    - { type: Dialogue, speaker: "Marinero Viejo", text: "En la Isla del Naufragio, muchacho." }
    - ...
    - { type: EndScene }
```

### 3. NarrativePlayer (motor de ejecución)

- Lee la lista de steps y los ejecuta en orden
- En steps `Dialogue`: muestra texto con efecto typewriter, espera input del
  jugador (tap) para avanzar
- En steps no-diálogo (`SetBackground`, `ShowCharacter`, etc.): ejecuta
  inmediatamente y pasa al siguiente
- **Skip**: botón visible en todo momento. Al pulsar, salta directo a
  `EndScene`
- **Auto**: no implementado para la demo (feature de versión completa)
- **Historial**: no implementado para la demo

### 4. NarrativeTrigger (enganche con Stage System)

| Campo en Battle Data | Cuándo se dispara | Condición |
|---------------------|-------------------|-----------|
| `NarrativeBefore` | Antes del primer intento de esa batalla | Solo primera vez. No en replays. |
| `NarrativeAfter` | Después del primer clear de esa batalla | Solo primera vez. No en replays. |

Flujo completo:

```
Stage Select → (NarrativeBefore si aplica) → Combat → Victory →
(NarrativeAfter si aplica) → Results
```

Las escenas vistas se marcan como `seen` en el save. El jugador puede re-verlas
desde un menú (no para demo — feature futura).

### 5. Demo Scene Map

| SceneId | Trigger | Batalla | Contenido placeholder |
|---------|---------|---------|----------------------|
| `ch1_intro` | NarrativeBefore | Ch1, Scene1, Battle1 | Capitán naufraga en isla, marinero viejo le explica la situación |
| `ch1_finale` | NarrativeAfter | Ch1, última batalla | Capitán derrota al pirata local, consigue su primer barco |
| `ch2_intro` | NarrativeBefore | Ch2, Scene1, Battle1 | Capitán zarpa hacia nueva isla, encuentra resistencia |
| `ch2_finale` | NarrativeAfter | Ch2, última batalla | Capitán establece su reputación, teaser de lo que viene |

### 6. UI Layout

```
┌─────────────────────────────────┐
│          [Background]           │
│                                 │
│   [Portrait L]    [Portrait R]  │
│                                 │
│ ┌─────────────────────────────┐ │
│ │ Speaker Name                │ │
│ │ Dialogue text with          │ │
│ │ typewriter effect...        │ │
│ └─────────────────────────────┘ │
│                        [Skip ▶] │
└─────────────────────────────────┘
```

- Fondo ocupa toda la pantalla
- Retratos en los laterales (~40% de altura de pantalla)
- Caja de diálogo en la parte inferior (~25% de pantalla)
- Nombre del hablante resaltado sobre la caja
- Botón Skip siempre visible (esquina inferior derecha)
- Tap en cualquier parte de la pantalla = avanzar al siguiente step

## Formulas

### Timing de escenas

```
SceneDuration_max = NUM_DIALOGUE_STEPS × AVG_READ_TIME
```

| Variable | Valor | Rango | Descripción |
|----------|-------|-------|-------------|
| `AVG_READ_TIME` | 4s | 3-6s | Tiempo promedio de lectura por línea de diálogo |
| `MAX_DIALOGUE_STEPS` | 15 | 8-20 | Máximo de líneas de diálogo por escena |
| `TYPEWRITER_SPEED` | 30 chars/s | 20-50 | Velocidad del efecto typewriter |
| `TRANSITION_DURATION` | 0.5s | 0.3-1.0s | Duración de fades/transiciones de fondo |
| `WAIT_MAX` | 2.0s | 0.5-3.0s | Máxima pausa dramática |

**Duración máxima por escena**: 15 pasos × 4s = ~60 segundos de lectura
(objetivo: <=1 minuto).

**Duración total narrativa demo**: 4 escenas × ~45s promedio = ~3 minutos
totales.

## Edge Cases

| Edge Case | Resolución |
|-----------|------------|
| **Jugador pulsa Skip durante typewriter** | Primer tap completa el texto de la línea actual. Segundo tap avanza al siguiente step. Botón Skip siempre salta toda la escena. |
| **Jugador cierra la app durante una escena narrativa** | La escena NO se marca como `seen`. Al reentrar, se vuelve a disparar desde el inicio. |
| **NarrativeBefore apunta a un sceneId que no existe** | Se ignora silenciosamente — el jugador pasa directo al combate. Log de warning en dev builds. |
| **Escena narrativa sin EndScene** | Se trata como finalizada al agotar todos los steps. Warning en dev builds. |
| **Jugador hace replay de batalla con NarrativeBefore** | La escena NO se reproduce (ya está marcada `seen`). Directo al combate. |
| **Jugador pierde el combate tras ver NarrativeBefore** | La escena ya fue marcada `seen` al iniciar. No se repite en el retry. |
| **SetBackground con asset no cargado** | Muestra fondo negro como fallback. Log de error. |
| **ShowCharacter con portrait no encontrado** | Muestra silueta placeholder. Log de error. |
| **Escena con 0 steps** | Se salta inmediatamente. Equivale a no tener escena. |
| **Dos personajes en la misma posición** | El segundo reemplaza al primero en esa posición. |

## Dependencies

### Upstream Dependencies

| Sistema | Tipo | Interfaz |
|---------|------|----------|
| **Stage System** | Hard | Proporciona los campos `NarrativeBefore`/`NarrativeAfter` por batalla. El Stage System decide cuándo disparar escenas narrativas. |
| **Game Flow** | Hard | Game Flow gestiona la transición al estado `Narrative` (nav bar oculta) y la salida hacia Combat o Hub. |

### Downstream Dependencies

| Sistema | Tipo | Interfaz |
|---------|------|----------|
| **Save/Load System** | Hard | Persiste el set de `sceneId`s marcados como `seen`. Restaura al cargar partida. |

### Assets requeridos (por escena)

| Asset | Cantidad demo | Formato |
|-------|--------------|---------|
| Fondos | ~4-6 únicos | Sprite 1080x1920 (portrait) |
| Retratos de personaje | ~4-6 personajes × 2-3 expresiones | Sprite ~512x512, fondo transparente |
| BGM | 1-2 tracks narrativos | Audio clip, loop-friendly |
| SFX | ~3-5 (pasos, espada, olas, etc.) | Audio clip one-shot |

> **Nota**: Todos los assets narrativos son placeholder para la demo. Pueden ser
> AI-generated o tomados de asset packs con licencia.

## Tuning Knobs

| Knob | Valor actual | Rango | Qué afecta | Si muy alto | Si muy bajo |
|------|-------------|-------|------------|-------------|-------------|
| `TYPEWRITER_SPEED` | 30 chars/s | 20-50 | Velocidad de aparición del texto | Se lee demasiado rápido, pierde efecto | Jugador se impacienta esperando |
| `TRANSITION_DURATION` | 0.5s | 0.3-1.0s | Duración de fades de fondo/personajes | Transiciones lentas, rompen el ritmo | Cambios bruscos, sin fluidez |
| `MAX_DIALOGUE_STEPS` | 15 | 8-20 | Longitud máxima de una escena | Escenas demasiado largas, jugador pierde interés | No hay espacio para contar nada |
| `WAIT_MAX` | 2.0s | 0.5-3.0s | Pausa dramática máxima | Jugador cree que se trabó el juego | Sin impacto dramático |
| `AVG_READ_TIME` | 4s | 3-6s | Referencia para calcular duración total | N/A (solo diseño) | N/A |

### Knob Interactions

| Knob A | Knob B | Interacción |
|--------|--------|-------------|
| `TYPEWRITER_SPEED` | `MAX_DIALOGUE_STEPS` | Más pasos × typewriter lento = escenas muy largas. Mantener duración total <=60s. |
| `TRANSITION_DURATION` | Número de `SetBackground` steps | Muchos cambios de fondo × transiciones largas = espera acumulada. |

## Acceptance Criteria

| # | Criterio | Cómo verificar |
|---|----------|----------------|
| 1 | El NarrativePlayer ejecuta una secuencia de steps en orden | Test: cargar escena con 5+ steps → cada uno se ejecuta secuencialmente. |
| 2 | Dialogue steps muestran typewriter y esperan tap para avanzar | Test: aparece texto progresivamente. Tap completa línea. Segundo tap avanza. |
| 3 | Skip salta toda la escena inmediatamente | Test: pulsar Skip en cualquier punto → transición directa a lo que siga (combate o hub). |
| 4 | Escenas se disparan solo la primera vez (NarrativeBefore) | Test: entrar en batalla con NarrativeBefore → escena se reproduce. Replay → sin escena. |
| 5 | Escenas post-clear se disparan solo la primera vez (NarrativeAfter) | Test: ganar batalla con NarrativeAfter → escena se reproduce. Replay + ganar → sin escena. |
| 6 | El estado `seen` persiste entre sesiones | Test: ver escena → cerrar app → reabrir → replay batalla → sin escena. |
| 7 | SetBackground cambia el fondo con la transición especificada | Visual test: fade, cut, slide funcionan correctamente. |
| 8 | ShowCharacter/HideCharacter posiciona retratos correctamente | Visual test: personajes aparecen en Left/Center/Right según datos. |
| 9 | SceneId inexistente no bloquea al jugador | Test: configurar NarrativeBefore con ID inválido → jugador entra directo al combate. |
| 10 | Nav bar está oculta durante escenas narrativas | Visual test: durante la escena no se ve la barra de navegación inferior. |
| 11 | Duración de cada escena demo es <=60 segundos de lectura | Revisar: contar dialogue steps × AVG_READ_TIME <= 60s. |
| 12 | Las 4 escenas demo existen y son jugables | Playtest: ch1_intro, ch1_finale, ch2_intro, ch2_finale se reproducen correctamente. |
