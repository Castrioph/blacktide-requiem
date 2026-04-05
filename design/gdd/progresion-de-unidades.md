# Progresión de Unidades

> **Status**: Approved
> **Author**: User + Claude Code Game Studios agents
> **Last Updated**: 2026-03-28
> **Implements Pillar**: Pillar 3 (Recompensa a la Paciencia), Pillar 4 (Respeto al Tiempo)

## Overview

La Progresión de Unidades gestiona cómo las unidades del jugador crecen en poder
fuera de combate. El sistema unifica tres ejes de inversión: **leveling**
(experiencia que sube stats), **awakening** (materiales que rompen el techo de
nivel y desbloquean habilidades), y **duplicados** (copias del gacha que otorgan
bonificaciones de stats o se convierten en Fragmentos de Alma).

El jugador interactúa activamente con este sistema desde el menú de unidad —
seleccionando a quién subir de nivel, decidiendo cuándo despertar, y gestionando
los beneficios de duplicados. Es el puente entre la adquisición (Gacha) y el uso
(Combate): una unidad recién obtenida es débil hasta que el jugador invierte
recursos en ella, creando un ciclo de inversión emocional alineado con Pillar 3
(Recompensa a la Paciencia).

El sistema se apoya en el Unit Data Model para las fórmulas de crecimiento de
stats y en el Currency System para los costos en Doblones. Las mecánicas de
duplicados están coordinadas con el Sistema Gacha. La persistencia del progreso
depende del Save/Load System.

## Player Fantasy

**"Este personaje es MÍO."** La progresión transforma una unidad genérica del
gacha en un compañero personal. El jugador que invirtió semanas subiendo a su
unidad 3★ favorita siente orgullo cuando esa unidad, despertada y con duplicados,
rinde a la par de una 5★ sin inversión. La progresión no es un trámite — es la
historia de cómo el jugador convirtió potencial en poder.

El sistema debe generar tres emociones clave:
- **Anticipación**: "Solo me faltan 3 niveles para desbloquear Tormenta de Fuego"
- **Satisfacción**: El momento de despertar — stats saltan, nueva habilidad
  aparece, el cap se eleva
- **Orgullo de inversión**: Una unidad con 4 dupes y max awakening es un logro
  visible que otros jugadores pueden apreciar (futuro: perfil de jugador)

El sistema fracasa si subir de nivel se siente como un trámite automático sin
decisión, si el awakening es tan caro que el jugador nunca lo alcanza en la demo,
o si los duplicados de unidades no deseadas se sienten como basura sin valor
(Pillar 3: cada copia debe tener valor).

## Detailed Design

### Core Rules

#### 1. Leveling (XP → Stats)

Las unidades ganan XP de dos fuentes:
- **Batalla**: Al completar un stage, TODAS las unidades del equipo (land o naval
  crew) ganan la misma cantidad de XP. No importa si participaron activamente.
- **Items de XP (Ron)**: Consumibles que otorgan XP plana directamente.

| Item | XP Otorgado | Obtención |
|------|-------------|-----------|
| Ron Añejo | 500 XP | Stages de eventos (sección Eventos), rewards de misión |
| Ron de Capitán | 2,500 XP | Stages de eventos (dificultad media+), first-clear rewards |
| Ron Legendario | 10,000 XP | Stages de eventos (boss), logros |

Al ganar suficiente XP, la unidad sube de nivel automáticamente. Cada nivel
incrementa stats según la Master Stat Growth Formula del UDM. Las habilidades
se desbloquean al alcanzar su `UnlockLevel` definido en `AbilityEntry`.

- Una unidad no puede ganar XP más allá de su level cap actual
- XP sobrante al llegar al cap se **pierde** (no se acumula en overflow)
- Unidades al cap muestran un badge en el roster indicando que el awakening
  está disponible

#### 2. Awakening (Materiales + DOB → Level Cap + Habilidades)

Cuando una unidad alcanza su level cap, puede ser despertada para:
- Aumentar su max level (ver tabla de caps en UDM)
- Recibir flat stat bonuses (ver tabla de awakening bonuses en UDM)
- Desbloquear habilidades de awakening (definidas por unidad en `AwakeningData`)

**Requisitos de awakening:**

| Awakening | Materiales Elementales | Doblones | Disponible para |
|-----------|----------------------|----------|-----------------|
| 1st | 5× Cristal Elemental (T1) | 2,000 DOB | 3★, 4★, 5★ |
| 2nd | 10× Cristal Elemental (T1) + 3× Cristal Elemental (T2) | 6,000 DOB | 3★, 4★, 5★ |
| 3rd | 15× Cristal Elemental (T2) + 1× Cristal Elemental (T3) | 15,000 DOB | Solo 5★ |

**Cristales Elementales** — materiales temáticos por elemento de la unidad:

| Material | Elemento | Obtención |
|----------|----------|-----------|
| Ceniza de Pólvora | Pólvora | Stages con enemigos Pólvora |
| Perla de Tormenta | Tormenta | Stages con enemigos Tormenta |
| Hueso Maldito | Maldición | Stages con enemigos Maldición |
| Escama de Bestia | Bestia | Stages con enemigos Bestia |
| Lingote de Acero | Acero | Stages con enemigos Acero |
| Prisma de Luz | Luz | Stages con enemigos Luz |
| Fragmento de Sombra | Sombra | Stages con enemigos Sombra |

Cada material existe en 3 tiers:
- **Tier 1**: Stages de eventos elementales (dificultad básica)
- **Tier 2**: Stages de eventos elementales (dificultad media+)
- **Tier 3**: Stages de eventos elementales (boss / dificultad máxima)

El jugador necesita cristales del **elemento de la unidad** que va a despertar.
Unidades con elemento Neutral usan **Esencia Universal** (mismo tier system,
dropea en cualquier stage).

#### 3. Duplicados (Copias extra → Stat Boost / Fragmentos de Alma)

Coordinado con el Sistema Gacha:

| Copia # | Efecto |
|---------|--------|
| 1ª (original) | — |
| 2ª (1er dupe) | +5% all base stats |
| 3ª (2do dupe) | +10% all base stats |
| 4ª (3er dupe) | +15% all base stats |
| 5ª (4to dupe) | +20% all base stats (cap) |
| 6ª+ | Convertido a Fragmentos de Alma |

- Los bonus de duplicado son **permanentes** y se aplican multiplicativamente
  sobre base stats (antes de level growth):
  `EffectiveBase = Base × (1 + DuplicateBonus)`
- Los bonus se aplican inmediatamente al obtener el dupe

**Fragmentos de Alma:**

Copias 6ª+ se convierten automáticamente en Fragmentos de Alma. Los fragmentos
son items de inventario acumulables, canjeables en la **Tienda de Almas** por
una unidad 5★ garantizada a elección del pool permanente (no featured).

| Rareza del dupe | Fragmentos por dupe |
|----------------|-------------------|
| 3★ | 3 |
| 4★ | 10 |
| 5★ | 50 |

**Costo de canjear una 5★: 300 Fragmentos.**

Equivalencias orientativas:
- Solo dupes 3★: ~100 dupes sobrantes
- Solo dupes 4★: ~30 dupes sobrantes
- Solo dupes 5★: ~6 dupes sobrantes
- Mix realista F2P: ~50-60 dupes sobrantes (varias semanas/meses)

Todos los valores de fragmentos son **tuning knobs** (ver sección Tuning Knobs).

#### 4. Doblones como recurso de progresión

Los Doblones (DOB) se usan exclusivamente para costos de awakening en este
sistema. El leveling usa XP (batallas + Ron), no Doblones. Esto separa la
moneda soft de la experiencia directa: DOB desbloquea *potencial* (cap break),
XP realiza ese potencial (niveles).

> **Nota**: El Currency System originalmente definía "ability slot unlocks" como
> sink de DOB. Este concepto se elimina — el UDM establece que todas las
> habilidades desbloqueadas por nivel están disponibles sin límite de slots.
> El Currency System necesita una actualización cross-system para reflejar esto.

### States and Transitions

| Estado | Descripción | Transiciona a |
|--------|-------------|---------------|
| **New** | Recién obtenida (gacha o reward). Nivel 1, sin inversión | Leveling |
| **Leveling** | Ganando XP activamente. Nivel < cap actual | At Cap |
| **At Cap** | Nivel = cap actual. No puede ganar más XP. Badge en roster | Awakened (si materiales + DOB) |
| **Awakened** | Se despertó al menos 1 vez. Cap subió, stats bonificados | Leveling (nuevo tramo), At Cap (nuevo cap) |
| **Max** | Último awakening completado + nivel = cap final | — |

Flujo típico:
```
New → Leveling → At Cap → Awakened → Leveling → At Cap → Awakened → ... → Max
```

Los duplicados no afectan el estado — son bonus pasivos que se aplican en
cualquier momento independientemente del estado de progresión.

### Interactions with Other Systems

| Sistema | Dirección | Interfaz |
|---------|-----------|----------|
| **Unit Data Model** | UDM → PU | Lee `BaseStats`, `StatGrowth`, `MaxLevel`, `AwakeningData`, `AbilityEntry.UnlockLevel`. PU no modifica templates — solo estado del jugador (nivel, awakening tier, dupe count) |
| **Currency System** | CS ↔ PU | PU llama `TrySpend(DOB, cost)` para awakening. CS valida balance y deduce |
| **Sistema Gacha** | SG → PU | Cuando un pull resulta en duplicado, SG notifica a PU. PU aplica bonus de dupe (+5% stats) o convierte a Fragmentos de Alma (6ª+) |
| **Rewards System** | RS → PU | RS otorga items de Ron (XP) y Cristales Elementales como drops de stage. PU los consume cuando el jugador los usa |
| **Stage System** | SS → PU | Al completar un stage, SS reporta XP ganada. PU la distribuye equitativamente a todas las unidades del equipo |
| **Combate Terrestre** | CT → PU | Informa qué unidades participaron (para XP de batalla). En la demo, todas reciben igual XP |
| **Combate Naval** | CN → PU | Informa crew del barco para XP de batalla naval. Toda la crew recibe XP igual |
| **Save/Load System** | PU ↔ SL | Persiste: nivel actual, XP acumulada, awakening tier, dupe count, inventario de Ron/Cristales/Fragmentos |
| **Unit Roster/Inventory** | PU → UR | Roster lee estado de progresión para mostrar nivel, awakening stars, badge de cap |
| **Combat UI** | PU → CUI | CUI muestra stats actuales que incluyen bonuses de awakening y duplicados |

**Interfaz ownership**: PU posee el estado de progresión por unidad (nivel, XP,
awakening tier, dupe bonus). UDM posee los templates. Currency posee los balances.
Rewards posee los drops.

## Formulas

### 1. XP requerida por nivel

```
XP_ToLevel(L) = BASE_XP + floor(XP_SCALE × L^XP_EXPONENT)
```

| Variable | Valor | Descripción |
|----------|-------|-------------|
| `BASE_XP` | 50 | XP mínima para subir de nivel |
| `XP_SCALE` | 10 | Factor de escala |
| `XP_EXPONENT` | 1.8 | Curvatura — 1.0 = lineal, 2.0 = cuadrática |

Ejemplos:
- Lv 1→2: 50 + floor(10 × 1^1.8) = **60 XP**
- Lv 10→11: 50 + floor(10 × 10^1.8) = **681 XP**
- Lv 30→31: 50 + floor(10 × 30^1.8) = **4,854 XP**
- Lv 50→51: 50 + floor(10 × 50^1.8) = **12,139 XP**
- Lv 80→81: 50 + floor(10 × 80^1.8) = **27,542 XP**

**XP acumulada total** para llegar a nivel L:
```
TotalXP(L) = sum(XP_ToLevel(i), i = 1 to L-1)
```

Orientación: llegar a Lv 40 (cap base 3★) requiere ~65,000 XP total. Un jugador
activo en la demo debería alcanzarlo en ~1 semana de juego para su unidad principal.

### 2. XP ganada por stage

```
StageXP = BASE_STAGE_XP + (StageIndex × XP_PER_STAGE)
```

| Variable | Valor | Descripción |
|----------|-------|-------------|
| `BASE_STAGE_XP` | 100 | XP mínima por completar cualquier stage |
| `StageIndex` | 0-based | Posición en la progresión de stages |
| `XP_PER_STAGE` | 30 | XP adicional por stage increment |

- Stage 1: **100 XP** (repartida entre todas las unidades)
- Stage 5: **220 XP**
- Stage 10: **370 XP**

La XP se reparte por igual entre todas las unidades del equipo activo (land:
6 unidades, naval: crew del barco). Ejemplo: Stage 10 con equipo terrestre
de 6 → 370 / 6 = **61 XP por unidad**.

### 3. Stats finales con toda la progresión

```
EffectiveBase = BaseStats × (1 + DuplicateBonus)
FinalStat(L) = StatGrowth(L, EffectiveBase) + sum(AwakeningBonus[tier])
```

Donde `StatGrowth(L, EffectiveBase)` es la Master Stat Growth Formula del UDM
aplicada con `EffectiveBase` en lugar de `Base`.

### 4. Awakening costs

```
AwakeningDOBCost = AWK_BASE_COST × AWK_TIER_MULTIPLIER[tier]
```

| Variable | Valor | Descripción |
|----------|-------|-------------|
| `AWK_BASE_COST` | 2,000 | Costo base en DOB |
| `AWK_TIER_MULTIPLIER` | [1, 3, 7.5] | Multiplicador por tier (1st=×1, 2nd=×3, 3rd=×7.5) |

### 5. Fragmentos de Alma

```
FragmentosGanados = FRAG_PER_RARITY[rareza_del_dupe]
Costo5Star = FRAG_COST_5STAR
```

| Variable | Valor | Descripción |
|----------|-------|-------------|
| `FRAG_PER_RARITY[3★]` | 3 | Fragmentos por dupe sobrante de 3★ |
| `FRAG_PER_RARITY[4★]` | 10 | Fragmentos por dupe sobrante de 4★ |
| `FRAG_PER_RARITY[5★]` | 50 | Fragmentos por dupe sobrante de 5★ |
| `FRAG_COST_5STAR` | 300 | Costo para canjear una 5★ del pool permanente |

## Edge Cases

| Edge Case | Resolución |
|-----------|------------|
| **Unidad recibe XP al cap** | XP se pierde. No hay overflow ni acumulación. Badge en roster indica cap alcanzado |
| **Jugador usa Ron en unidad al cap** | No permitido. Botón de usar Ron deshabilitado. Mensaje: "Nivel máximo — ¡despierta esta unidad!" |
| **Awakening sin suficientes DOB** | Botón deshabilitado. Costo en rojo con balance actual |
| **Awakening sin suficientes cristales** | Botón deshabilitado. Materiales faltantes con link a stages donde farmearlos |
| **Dupe de unidad ya con 4 dupes (cap +20%)** | Se convierte automáticamente a Fragmentos de Alma. Notificación: "+X Fragmentos de Alma" |
| **Dupe de unidad nunca antes obtenida** | No es dupe — se añade al roster como unidad nueva. No da fragmentos ni bonus |
| **Unidad Neutral necesita awakening** | Usa Esencia Universal en lugar de cristales elementales temáticos. Mismo tier system |
| **Intentar despertar unidad que no está al cap** | No permitido. Awakening solo se habilita cuando nivel = cap actual |
| **3★ o 4★ intenta 3rd awakening** | No disponible. Solo 5★ tienen 3rd awakening. UI no muestra la opción |
| **Unidad Max recibe XP de batalla** | XP se pierde silenciosamente. Las demás unidades del equipo sí la reciben |
| **Todas las unidades del equipo al cap/max** | XP del stage se pierde para todas. Situación rara en la demo |
| **Ron da XP suficiente para subir varios niveles** | Se aplican todos los niveles de golpe. Habilidades de niveles intermedios se desbloquean todas. XP que exceda el cap se pierde |
| **Fragmentos suficientes pero pool vacío** | No debería ocurrir (pool permanente tiene 1-2 unidades 5★). Botón deshabilitado si ocurre |
| **Awakening cambia visual del sprite** | Cada awakening aplica modificaciones incrementales al sprite (aura, detalles de ropa, efectos). Ver Visual/Audio Requirements |

## Dependencies

### Dependencias Upstream (PU depende de)

| Sistema | Tipo | Interfaz | GDD |
|---------|------|----------|-----|
| **Unit Data Model** | Hard | `BaseStats`, `StatGrowth`, `MaxLevel`, `AwakeningData`, `AbilityEntry.UnlockLevel`, `DuplicateBonus`, `Rarity` | ✅ Approved |
| **Currency System** | Hard | `TrySpend(DOB, cost)` para awakening. PU no gestiona Doblones directamente | ✅ Approved |
| **Sistema Gacha** | Hard | Notifica dupes al obtener unidades repetidas. PU decide si aplicar bonus o convertir a Fragmentos | ✅ Approved |
| **Stage System** | Soft | Reporta XP ganada al completar stage. PU funciona sin stages (puede usar solo Ron) | ✅ Approved |

### Dependencias Downstream (dependen de PU)

| Sistema | Tipo | Qué necesita de PU | GDD |
|---------|------|---------------------|-----|
| **Unit Roster/Inventory** | Hard | Nivel actual, awakening tier, dupe count, estado de progresión para UI | ⬜ Not Started |
| **Menus & Navigation UI** | Soft | Datos de progresión para pantalla de detalle de unidad | ⬜ Not Started |
| **Save/Load System** | Hard | Serializar/deserializar: nivel, XP, awakening tier, dupe count, inventario | ⬜ Not Started |
| **Rewards System** | Soft | Define qué items de progresión existen (Ron, Cristales) para otorgarlos como drops | ⬜ Not Started |
| **Combat UI** | Soft | Stats finales calculados (con awakening + dupes) para mostrar en combate | ✅ Approved |

### Cross-System Updates Necesarios

- **Currency System**: Eliminar referencia a "ability slot unlocks" como sink de
  DOB. DOB solo se usa para awakening en Progresión
- **Rewards System** (cuando se diseñe): Debe definir drop rates de Ron y Cristales
  Elementales por stage

## Tuning Knobs

| Knob | Valor Actual | Rango Seguro | Afecta a | Notas |
|------|-------------|-------------|----------|-------|
| `BASE_XP` | 50 | 20–100 | XP mínima por nivel. Muy bajo: niveles iniciales instantáneos. Muy alto: early game lento | Interactúa con XP_SCALE |
| `XP_SCALE` | 10 | 5–20 | Factor de escala. Muy bajo: niveles altos rápidos. Muy alto: grind excesivo | Interactúa con XP_EXPONENT |
| `XP_EXPONENT` | 1.8 | 1.4–2.2 | Curvatura. <1.5: casi lineal. >2.0: muro en niveles altos | Knob más sensible — cambios pequeños, impacto grande |
| `BASE_STAGE_XP` | 100 | 50–200 | XP base por stage. Ritmo de leveling por batalla | Interactúa con XP_PER_STAGE |
| `XP_PER_STAGE` | 30 | 10–60 | Incremento XP por stage. Muy alto: stages tardíos dan demasiada XP | Escala con total de stages |
| `RON_AÑEJO_XP` | 500 | 200–1,000 | XP del Ron común | Interactúa con drop rate en Rewards |
| `RON_CAPITAN_XP` | 2,500 | 1,000–5,000 | XP del Ron medio | |
| `RON_LEGENDARIO_XP` | 10,000 | 5,000–25,000 | XP del Ron grande. Debe sentirse como reward significativo | |
| `AWK_BASE_COST` | 2,000 | 1,000–5,000 | Costo base awakening en DOB | Interactúa con DOB income (Currency) |
| `AWK_TIER_MULTIPLIER` | [1, 3, 7.5] | [1, 2–4, 5–10] | Escalado costo por tier. Tier 3 alto = gate para 5★ max | |
| `AWK_T1_COUNT` | [5, 10, 15] | [3–8, 7–15, 10–20] | Cristales T1 por awakening tier | Interactúa con drop rates |
| `AWK_T2_COUNT` | [0, 3, 15] | [0, 2–5, 10–20] | Cristales T2 por awakening tier. T2 escaso en early game | |
| `AWK_T3_COUNT` | [0, 0, 1] | [0, 0, 1–3] | Cristales T3 para 3rd awakening. Solo 5★ | Extremadamente raro |
| `FRAG_PER_RARITY[3★]` | 3 | 1–5 | Fragmentos por dupe 3★. Muy bajo: no valen nada. Muy alto: 5★ fácil | Interactúa con FRAG_COST_5STAR |
| `FRAG_PER_RARITY[4★]` | 10 | 5–20 | Fragmentos por dupe 4★. Sweet spot de acumulación | |
| `FRAG_PER_RARITY[5★]` | 50 | 25–100 | Fragmentos por dupe 5★. El salto grande | |
| `FRAG_COST_5STAR` | 300 | 150–500 | Costo para canjear 5★. Muy bajo: devalúa gacha. Muy alto: inalcanzable | Knob más importante de economía de fragmentos |
| `DUPE_BONUS_PERCENT` | 5% | 3–10% | Bonus por dupe. Muy alto: 4 dupes = +40%, demasiado poder | Cap de 4 dupes lo contiene |
| `DUPE_CAP` | 4 | 3–6 | Máximo dupes con bonus. Después del cap → Fragmentos | |

### Knob Interactions

| Knob A | Knob B | Interacción |
|--------|--------|-------------|
| XP_EXPONENT | RON values | Curva agresiva + Ron bajo = grind puro de items en late game |
| AWK_BASE_COST | DOB income (Currency) | Awakening caro + DOB income bajo = jugador nunca despierta en la demo |
| FRAG_PER_RARITY | FRAG_COST_5STAR | Ratio de "cuántos dupes para un 5★". Ajustar uno sin el otro rompe la economía |
| DUPE_BONUS_PERCENT | Stat gap 3★ vs 5★ (UDM) | Dupes altos cierran gap de rareza. Bueno para Pillar 3, riesgoso para monetización |
| AWK material counts | Drop rates (Rewards) | 15 cristales T2 con drop de 1/stage = ~15 runs del stage correcto |

## Visual/Audio Requirements

**Visual**
- **Sprite evolution por awakening**: Cambios incrementales al sprite:
  - **1st Awakening**: Aura sutil de color elemental (glow)
  - **2nd Awakening**: Detalles adicionales en ropa/armadura + aura más intensa
  - **3rd Awakening (solo 5★)**: Rediseño parcial — versión "épica" con efectos
    visuales permanentes según elemento
- **Pantalla de level up**: Flash breve con número de nuevo nivel. Sin animación
  larga (Pillar 4). Si sube varios niveles, muestra solo el final
- **Pantalla de awakening**: Animación corta (~2-3 seg) de transformación. Sprite
  viejo se desvanece, nuevo aparece con partículas elementales. Momento de
  satisfacción — merece pausa dramática
- **Ron items**: Iconos de botella con 3 niveles de detalle (sencilla, ornamentada,
  dorada con brillo)
- **Cristales elementales**: 7 variantes de color por elemento × 3 tiers de
  tamaño/brillo
- **Fragmentos de Alma**: Llama azul espectral dentro de una botella cerrada.
  Estética pirata — como un alma capturada. Mismo icono siempre, el contador
  muestra cantidad

**Audio**
- **Level up**: SFX corto satisfactorio (chime ascendente). No interrumpe flow
- **Awakening**: SFX dramático (~2 seg) — crack/ruptura + resonancia. Más épico
  por tier
- **Dupe bonus**: SFX sutil de "power up" (breve, positivo)
- **Fragmentos de Alma**: SFX etéreo/fantasmal (susurro + tintineo cristalino)

## UI Requirements

- **Pantalla de detalle de unidad**: Sprite (con awakening visual), nivel/cap,
  barra de XP, stats actuales, awakening stars (0-3), dupe count (0-4),
  elemento, traits
- **Botón "Subir Nivel"**: Sub-pantalla de consumir Ron. Inventario de Ron,
  preview de niveles ganados, habilidades que se desbloquean. Deshabilitado al cap
- **Botón "Despertar"**: Sub-pantalla de awakening. Requisitos (cristales + DOB),
  preview de bonuses (stats, habilidades, nuevo cap). Deshabilitado si no al cap
  o faltan recursos. Materiales faltantes en rojo con link "¿Dónde farmear?" →
  abre stages de eventos correspondientes
- **Tienda de Almas**: Accesible desde menú principal o roster. Contador de
  Fragmentos, pool rotativo de 5★ canjeables con preview, botón canjear
  (deshabilitado si <300 Fragmentos)
- **Badge de cap en roster**: Indicador compacto ("MAX" o icono flecha) en tarjeta
  de unidad. No intrusivo pero visible al scrollear
- **Preview de stats**: Al usar Ron o despertar, stats antes/después en dos
  columnas (actual → nuevo) con incrementos en verde

## Acceptance Criteria

**Leveling**
1. Una unidad gana XP al completar un stage y sube de nivel según la curva
2. Todas las unidades del equipo reciben la misma XP independientemente de participación
3. Ron Añejo/Capitán/Legendario otorgan la XP correcta al consumirlos
4. Usar Ron que sube varios niveles desbloquea todas las habilidades intermedias
5. Una unidad al cap no gana XP (se pierde silenciosamente)
6. Ron no se puede usar en una unidad al cap (botón deshabilitado)

**Awakening**
7. Awakening solo se habilita cuando nivel = cap actual
8. Awakening requiere cristales del elemento correcto + DOB
9. Unidades Neutral usan Esencia Universal en lugar de cristales elementales
10. Tras awakening: max level sube, stat bonuses se aplican, habilidades se desbloquean
11. 3★/4★ solo tienen 2 awakenings, 5★ tiene 3. UI no muestra opciones no disponibles
12. Awakening con recursos insuficientes: botón deshabilitado con feedback visual
13. El sprite de la unidad cambia visualmente con cada awakening

**Duplicados**
14. Dupe (copias 2-5) aplica +5% acumulativo a base stats inmediatamente
15. Dupe (copia 6+) convierte automáticamente a Fragmentos de Alma según rareza
16. Fragmentos se acumulan correctamente en inventario
17. Tienda de Almas permite canjear 300 Fragmentos por una 5★ a elección del pool permanente

**Progresión completa**
18. Stats finales = growth con EffectiveBase (dupes) + awakening bonuses. Verificar con worked example del UDM
19. Estado de progresión persiste entre sesiones (Save/Load)
20. Badge de "at cap" aparece en roster para unidades al límite

## Open Questions

1. **Stages de eventos — estructura y límites**: Ron y Cristales se obtienen
   exclusivamente en stages especiales de la sección de eventos (no en story
   stages). ¿Cuántas veces al día se pueden correr? Sin energía en la demo,
   necesitamos otro bottleneck (intentos diarios, rotación de stages por día
   de la semana, o dejarlos ilimitados y controlar por drop amounts).
   **Owner**: economy-designer. **Target**: al diseñar Rewards System
2. **Tienda de Almas — cadencia de rotación**: Pool rotativo confirmado. ¿Rotación
   semanal, quincenal, o mensual? Con 2-3 unidades 5★ en la demo es menor,
   pero debe diseñarse para escalar.
   **Owner**: economy-designer. **Target**: post-demo
3. **Awakening sprite variants — pipeline de arte**: Cada unidad necesita 2-3
   variantes de sprite (1 por awakening). Con 8-12 unidades en la demo = 16-36
   sprites adicionales. ¿Es viable para un solo dev? Alternativa: solo variantes
   para 5★, resto usa overlays genéricos (auras).
   **Owner**: art-director. **Target**: durante producción de assets
