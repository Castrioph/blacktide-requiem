# Rewards System

> **Status**: Designed
> **Author**: User + Claude Code Game Studios agents
> **Last Updated**: 2026-03-31
> **Implements Pillar**: Pillar 3 (Recompensa a la Paciencia), Pillar 4 (Respeto al Tiempo del Jugador)

## Overview

El Rewards System centraliza la distribución de todas las recompensas del juego:
recompensas post-combate (DOB, XP, drops de materiales), first-clear y misiones
de stage (GDC), daily login, misiones diarias, y logros. Es el "grifo" principal
de la economía — define qué recibe el jugador, cuándo, y en qué cantidad por cada
actividad completada.

El jugador interactúa con este sistema de forma pasiva (recibiendo rewards al
completar actividades) y activa (revisando misiones diarias pendientes, reclamando
login rewards, verificando progreso de logros). Las recompensas de stages se
dividen en dos categorías: stages de historia/naval otorgan DOB (determinista) +
XP + GDC (first-clear/misiones); stages de eventos otorgan materiales de
progresión (Ron para XP, Cristales Elementales para awakening) como drops
principales. Esta separación crea dos motivaciones distintas: historia para
avanzar y ganar premium currency, eventos para farmear materiales de mejora.

Sin este sistema, no hay razón para repetir contenido, no hay flujo de materiales
hacia la progresión, y no hay incentivos diarios que retengan al jugador. Es el
puente entre "jugar" y "crecer".

## Player Fantasy

**"Cada batalla me acerca a algo."** El jugador nunca debería terminar un stage
sintiendo que perdió el tiempo. Incluso el replay más rutinario produce DOB y XP
que acercan al siguiente awakening o nivel. La pantalla de recompensas no es un
trámite — es la confirmación visible de que el tiempo invertido valió la pena.

**La emoción del daily login**: Abrir el juego cada mañana y ver el reward del
día esperándote. No es una obligación — es un regalo que dice "bienvenido de
vuelta". Los streaks de login generan anticipación: "¿Qué me toca mañana?" El
sistema de login debe sentirse como abrir un cofre del tesoro, no como fichar en
el trabajo.

**Misiones como guía, no como tarea**: Las misiones diarias no son un checklist
tedioso — son sugerencias de qué hacer hoy. "Completa 3 stages" le da al jugador
un objetivo claro cuando abre el juego sin saber qué hacer. La recompensa en GDC
hace que seguir las misiones se sienta como la opción inteligente, no la única
opción.

**El farmeo con propósito**: Cuando el jugador entra a un stage de eventos a
buscar Cristales de Tormenta, tiene un objetivo claro: "Necesito 10 cristales
para despertar a Elena". El farmeo deja de ser grind cuando cada run es un paso
medible hacia una meta concreta. La pantalla de rewards debe mostrar claramente
cuánto falta.

El sistema fracasa si la pantalla de rewards se skipea automáticamente, si el
jugador no sabe para qué sirve lo que le dieron, o si el daily login se siente
como una cadena que castiga por no entrar.

## Detailed Design

### Core Rules

#### 1. Reward Sources

| Fuente | Tipo | Frecuencia | Rewards principales |
|--------|------|------------|---------------------|
| **Story/Naval stage clear** | Post-combate | Cada clear | DOB (determinista), XP (por enemigos), GDC (first-clear + misiones, one-time) |
| **Evento de Cristales** | Post-combate | Rotación diaria + energía | Cristales Elementales (garantía + bonus RNG) |
| **Evento de Ron** | Post-combate | Siempre abierto + energía | Ron Añejo/Capitán/Legendario (garantía + bonus RNG) |
| **Daily Login (Streak)** | Calendario | Consecutivo, reset al faltar | GDC, DOB, materiales (escala hasta día 28) |
| **Daily Login (Mensual)** | Calendario | Fijo por fecha | GDC, DOB, materiales (reward por cada día del mes) |
| **Daily Missions** | Objetivos | 3/día, reset medianoche UTC | GDC (30 cada + 50 bonus = 140 GDC/día) |
| **Achievements** | Permanentes | One-time | GDC, DOB, items raros (hitos de progresión) |

#### 2. Story/Naval Stage Rewards

Definidos en Stage System y Currency System. Este sistema los distribuye
post-combate:

**Replay rewards (cada clear):**
- DOB: `BASE_STAGE_DOB + (StageIndex × DOB_PER_STAGE)` — siempre igual
- XP: Suma del `XPReward` de cada enemigo derrotado (Enemy System)
- Sin drops de materiales en story/naval

**One-time rewards (primer clear):**
- GDC: `BASE_FIRST_CLEAR_GDC + (StageIndex × GDC_PER_STAGE)` — solo primer clear
- Mission GDC: 3 misiones por battle, `MISSION_BASE_GDC × MissionDifficultyMod`
  — solo una vez por misión

**Chapter-clear bonus:** GDC flat + TIE:
- Capítulo 1: 100 GDC + 1× TIE
- Capítulo 2: 150 GDC + 1× TIE
- Naval (todos los stages navales): 100 GDC + 1× TIE

#### 3. Event System: Cristales Elementales

**Rotación semanal:**

| Día | Evento | Material |
|-----|--------|----------|
| Lunes | Fragua de Pólvora | Ceniza de Pólvora |
| Martes | Templo de Tormenta | Perla de Tormenta |
| Miércoles | Cripta de Maldición | Hueso Maldito |
| Jueves | Bosque de Bestias | Escama de Bestia |
| Viernes | Forja de Acero | Lingote de Acero |
| Sábado | Santuario Dual | Prisma de Luz + Fragmento de Sombra |
| Domingo | Todos abiertos | Todos los cristales disponibles |

Cada evento de cristales tiene 3 dificultades:

| Dificultad | Waves | Rec. Lv | Energía | Garantizado | Bonus RNG |
|------------|-------|---------|---------|-------------|-----------|
| Básico | 1-2 | 10 | 10 | 2× T1 | 30% +1 T1 |
| Medio | 3 | 25 | 15 | 3× T1, 1× T2 | 30% +1 T1, 15% +1 T2 |
| Boss | 5 | 40 | 25 | 2× T2, 1× T3 | 25% +1 T2, 10% +1 T3 |

**First-clear bonus (Boss):** Al completar por primera vez un Boss de evento de
cristales, el jugador recibe **1× TIE** como bonus one-time (además de los drops
normales). 7 eventos elementales × 1 TIE = 7 TIE one-time de cristales.

Eventos también otorgan DOB y XP reducidos (50% del valor equivalente por
StageIndex respecto a story stages).

El **bottleneck** es doble: rotación diaria (solo puedes farmear un elemento por
día, excepto Sábado/Domingo) + energía (cada run consume energía del pool
compartido).

#### 4. Event System: Ron (Taberna del Puerto)

El evento de Ron está **siempre abierto** — disponible todos los días. Comparte
el pool de energía con los eventos de cristales, forzando al jugador a decidir
cómo distribuir su energía diaria.

| Dificultad | Waves | Rec. Lv | Energía | Garantizado | Bonus RNG |
|------------|-------|---------|---------|-------------|-----------|
| Básico | 1-2 | 10 | 10 | 2× Ron Añejo | 30% +1 Ron Añejo |
| Medio | 3 | 25 | 15 | 2× Ron Capitán | 25% +1 Ron Capitán, 10% +1 Ron Añejo |
| Boss | 5 | 40 | 25 | 1× Ron Legendario | 20% +1 Ron Capitán, 5% +1 Ron Legendario |

**First-clear bonus (Boss Ron):** Al completar por primera vez el Boss de Ron,
el jugador recibe **1× TIE** como bonus one-time.

#### 5. Drop System (Garantía + Bonus RNG)

Estructura universal para todas las loot tables de eventos:

```
RewardConfig {
  guaranteed: List<ItemDrop>    // Siempre se obtienen
  bonusPool: List<BonusDrop>    // Cada uno con chance independiente
  dob: int                      // DOB fijos (50% de story equivalent)
  xp: int                       // XP fija (menor que story)
}
```

- Cada `BonusDrop` tiene un `chance` (0.0–1.0) y se evalúa independientemente
- El jugador puede obtener 0, 1, o múltiples bonus drops en un run
- Los drops se muestran en la pantalla de rewards con los bonus marcados como
  "¡Bonus!" con efecto visual especial
- Los drops no se aplican automáticamente — van al inventario del jugador

#### 6. Daily Login: Sistema Dual

El jugador recibe rewards de **dos calendarios independientes** cada día que
entra al juego:

**Calendario de Streak (consecutivo):**

Avanza 1 día por cada login consecutivo. Si el jugador no entra un día, el
streak se reinicia a Día 1. El ciclo tiene 28 días; al completarlo se reinicia.

| Día | Reward | Día | Reward |
|-----|--------|-----|--------|
| 1 | 30 GDC | 15 | 50 GDC |
| 2 | 500 DOB | 16 | 3,000 DOB |
| 3 | 1× Ron Añejo | 17 | 2× Ron Añejo |
| 4 | 30 GDC | 18 | 50 GDC |
| 5 | 1,000 DOB | 19 | 4,000 DOB |
| 6 | 2× Ron Añejo | 20 | 1× Cristal T1 random |
| **7** | **100 GDC + 1× Ron Capitán + 1× TIE** | **21** | **200 GDC + 1× Ron Legendario + 1× TIE** |
| 8 | 40 GDC | 22 | 60 GDC |
| 9 | 1,500 DOB | 23 | 5,000 DOB |
| 10 | 1× Cristal T1 random | 24 | 2× Cristal T1 random |
| 11 | 40 GDC | 25 | 60 GDC |
| 12 | 2,000 DOB | 26 | 6,000 DOB |
| 13 | 1× Ron Capitán | 27 | 1× Cristal T2 random |
| **14** | **150 GDC + 2× Cristal T1 + 1× TIE** | **28** | **300 GDC + 1× Ticket de Invocación Featured** |

GDC total del streak completo (28 días): **1,110 GDC** + 3 TIE + 1 Ticket Featured.

**Ticket de Invocación Featured (día 28):** Equivale a 1 pull featured gratis
(ahorra 300 GDC). Es el reward máximo del streak — motivación para no romperlo.

**Ticket de Invocación Estándar (TIE):** Se obtiene en hitos semanales (días 7,
14, 21). Cada uno equivale a 1 pull del banner estándar. 3 TIE por ciclo.

**Calendario Mensual (fijo por fecha):**

Rewards predefinidos para cada día del mes calendario (1-28/30/31). No depende
de consecutividad — el jugador recibe el reward del día actual si entra al juego.
Si no entra, pierde el reward de ese día.

| Patrón | Días | Reward |
|--------|------|--------|
| GDC | 1, 5, 9, 13, 17, 21, 25, 29 | 20 GDC |
| DOB | 2, 6, 10, 14, 18, 22, 26, 30 | 500-2,000 DOB |
| Materiales | 3, 7, 11, 15, 19, 23, 27, 31 | 1× Ron Añejo o 1× Cristal T1 |
| Hito | 8, 16, 24 | 30 GDC + material especial |
| Hito + TIE | 4, 12, 20, 28 | 30 GDC + 1× TIE |

GDC total mensual (30 días, asumiendo login diario): ~370 GDC + 4 TIE.

**Login reward total (ambos calendarios, login diario perfecto):**
- Streak: ~1,110 GDC/mes + 3 TIE + 1 Ticket Featured + materiales
- Mensual: ~370 GDC/mes + 4 TIE + materiales
- **Total: ~1,480 GDC/mes** (~49 GDC/día) + **7 TIE/mes** + 1 Ticket Featured/mes

Nota: El Currency System estimaba 50 GDC/día de daily login. El sistema dual
da ~49 GDC/día — alineado con el target original. Cross-system update necesario
para añadir TIE como recurso.

**Reclamación:** Popup automático al entrar al juego mostrando ambos rewards
simultáneamente (cofre del streak arriba, calendario abajo). Un tap para
reclamar ambos.

#### 7. Daily Missions

3 misiones diarias seleccionadas de un pool amplio. Reset a medianoche UTC.

**Pool de misiones:**

| # | Misión | Condición | GDC |
|---|--------|-----------|-----|
| 1 | Aventurero | Completa 3 stages (cualquier modo) | 30 |
| 2 | Eventero | Completa 1 stage de evento (cualquier tipo) | 30 |
| 3 | Inversor | Gasta 1,000 DOB (cualquier sink) | 30 |
| 4 | Entrenador | Sube 1 nivel a cualquier unidad (batalla o Ron) | 30 |
| 5 | Marinero | Completa 1 batalla naval | 30 |
| 6 | Apostador | Haz 1 pull de gacha (single o multi) | 30 |
| 7 | Cazarrecompensas | Derrota 1 jefe (boss wave) | 30 |
| 8 | Explorador | Completa 1 stage de un capítulo no terminado | 30 |
| 9 | Coleccionista | Obtén cualquier material de un evento | 30 |
| 10 | Maestro de armas | Usa 3 habilidades de tipo elemental en combate | 30 |
| 11 | Estratega | Completa 1 stage sin que muera ninguna unidad | 30 |
| 12 | Persistente | Completa 5 stages en total (cualquier modo) | 30 |

Cada día se seleccionan 3 del pool (sin repetir). Selección determinista por
seed diario para evitar manipulación.

**Bonus por completar las 3:** +50 GDC.

**Total diario:** 3×30 + 50 = **140 GDC/día** = **980 GDC/semana** =
**~4,200 GDC/mes**.

**Bonus semanal (TIE):** Si el jugador completa las 3 misiones diarias durante
7 días consecutivos, recibe **1 TIE** como bonus semanal. Contador visible en
la pantalla de misiones: "Racha de misiones: X/7 días". Al obtener el TIE, el
contador se reinicia. Máximo ~4 TIE/mes por esta vía.

> **Cross-system update:** Currency System `DAILY_MISSION_GDC` → 140 GDC/día.

#### 8. Achievements (Logros)

Hitos permanentes one-time. Se reclaman manualmente desde la pantalla de logros.

**Categorías y ejemplos (demo scope — 53 logros):**

**Progresión (10 logros):**

| Logro | Condición | Reward |
|-------|-----------|--------|
| Primer paso | Completa tu primer stage | 30 GDC |
| Marinero novato | Completa tu primera batalla naval | 50 GDC |
| Capítulo 1 completo | Todas las batallas de Ch1 | 100 GDC + 1× TIE |
| Capítulo 2 completo | Todas las batallas de Ch2 | 150 GDC + 1× TIE |
| Todas las misiones Ch1 | 3/3 misiones en todas las batallas de Ch1 | 100 GDC |
| Todas las misiones Ch2 | 3/3 misiones en todas las batallas de Ch2 | 150 GDC |
| Navegante completo | Todas las batallas navales | 100 GDC + 1× TIE |
| Veterano | Completa 50 stages en total | 100 GDC |
| Leyenda | Completa 100 stages en total | 200 GDC |
| Sin caídos | Completa 10 stages sin ninguna unidad KO | 100 GDC |

**Colección (10 logros):**

| Logro | Condición | Reward |
|-------|-----------|--------|
| Primera tripulación | Obtén 3 unidades | 30 GDC |
| Tripulación creciente | Obtén 5 unidades | 50 GDC |
| Capitán reclutador | Obtén 8 unidades | 100 GDC |
| Todos a bordo | Obtén 12 unidades (todas de demo) | 200 GDC + 2× TIE |
| Estrella brillante | Obtén tu primera unidad 5★ | 100 GDC + 1× TIE |
| Colección estelar | Obtén 3 unidades 5★ | 200 GDC |
| Primer barco | Obtén tu primer barco | 50 GDC |
| Flota | Obtén los 3 barcos de la demo | 150 GDC + 1× TIE |
| Diversidad elemental | Obtén al menos 1 unidad de cada elemento | 150 GDC |
| Cazador de traits | Ten 3 unidades que compartan un trait | 100 GDC |

**Poder (10 logros):**

| Logro | Condición | Reward |
|-------|-----------|--------|
| Primer nivel | Sube una unidad a nivel 10 | 30 GDC |
| Entrenamiento serio | Sube una unidad a nivel 20 | 50 GDC |
| Guerrero curtido | Sube una unidad a nivel 30 | 100 GDC |
| Élite | Sube una unidad a nivel 50 | 150 GDC |
| Primer despertar | Despierta una unidad (1st awakening) | 50 GDC + 2× Ron Capitán + 1× TIE |
| Despertar avanzado | Alcanza 2nd awakening en una unidad | 100 GDC + 1× Cristal T2 random |
| Poder supremo | Alcanza 3rd awakening en una 5★ | 200 GDC + 1× Ron Legendario + 1× TIE |
| Compañero fiel | Obtén el 1er duplicado de cualquier unidad | 50 GDC |
| Vínculo máximo | Alcanza 4 duplicados en una unidad | 150 GDC + 1× TIE |
| Alma fragmentada | Acumula 100 Fragmentos de Alma | 100 GDC |

**Economía (10 logros):**

| Logro | Condición | Reward |
|-------|-----------|--------|
| Primer gasto | Gasta DOB por primera vez | 20 GDC |
| Ahorrador | Acumula 10,000 DOB | 50 GDC |
| Rico | Acumula 50,000 DOB | 100 GDC |
| Magnate | Gasta 100,000 DOB en total | 150 GDC |
| Primera invocación | Haz tu primer pull de gacha | 30 GDC |
| Invocador habitual | Haz 10 pulls totales | 50 GDC |
| Invocador veterano | Haz 30 pulls totales | 100 GDC + 1× TIE |
| Multi! | Haz tu primer multi-pull (10x) | 50 GDC |
| Fortuna sonríe | Obtén una 5★ de un pull (no pity) | 100 GDC |
| Paciencia recompensada | Activa el pity system | 100 GDC + 1× TIE |

**Eventos (8 logros):**

| Logro | Condición | Reward |
|-------|-----------|--------|
| Primer evento | Completa 1 stage de evento | 30 GDC |
| Eventero regular | Completa 10 stages de evento | 50 GDC |
| Eventero veterano | Completa 30 stages de evento | 100 GDC |
| Domador de jefes | Derrota un boss de evento | 100 GDC + 1× Ron Legendario + 1× TIE |
| Todos los elementos | Completa al menos 1 evento de cada elemento | 150 GDC |
| Tabernero | Completa 10 stages del evento de Ron | 50 GDC |
| Alquimista | Obtén 10 Cristales T2 | 100 GDC |
| Maestro elemental | Obtén 3 Cristales T3 | 200 GDC + 1× TIE |

**Hitos de login (5 logros):**

| Logro | Condición | Reward |
|-------|-----------|--------|
| Bienvenido | Entra al juego 3 días | 20 GDC |
| Habitual | Entra al juego 7 días | 50 GDC |
| Dedicado | Entra al juego 14 días | 100 GDC |
| Leal | Entra al juego 28 días | 200 GDC + 1× TIE |
| Streak de 7 | Mantén un streak de login de 7 días consecutivos | 100 GDC |

**Total GDC de logros en demo:** ~5,190 GDC (one-time income). Equivalente a
~17.3 singles o ~1.92 multi-pulls repartidos a lo largo de toda la demo.

**Total TIE de logros en demo:** 15 TIE (one-time). Distribuidos en logros
seleccionados de cada categoría (ver columna Reward).

### States and Transitions

**Post-combat reward flow:**

| Estado | Descripción | Transiciona a |
|--------|-------------|---------------|
| **Combat End** | Batalla terminada (victoria) | Calculating |
| **Calculating** | Sistema calcula rewards: DOB, XP, drops, first-clear, misiones | Displaying |
| **Displaying** | Pantalla de rewards muestra items uno por uno | Claiming |
| **Claiming** | Jugador toca para reclamar (o auto-claim tras timer) | Done |
| **Done** | Rewards aplicados al inventario/balance | Return to stage select |

**En derrota:** No se otorgan rewards. Sin penalización — el jugador conserva su
energía y puede reintentar (en stages de historia). En eventos, la energía SÍ se
consume en derrota.

**Daily login flow:**

| Estado | Descripción |
|--------|-------------|
| **App Launch** | Detecta si es un nuevo día calendario (UTC) |
| **Streak Check** | ¿Último login fue ayer? → streak +1. ¿Hace 2+ días? → streak = 1 |
| **Calendar Check** | ¿Qué día del mes es? → lookup reward en calendario mensual |
| **Display** | Popup dual: reward de streak (arriba) + reward mensual (abajo) |
| **Claim** | Tap para reclamar ambos. Items van a inventario, currencies a balance |

**Mission flow:**

| Estado | Descripción |
|--------|-------------|
| **Active** | Misión visible, progreso tracking en background |
| **Completed** | Condición cumplida, GDC pendiente de reclamar |
| **Claimed** | GDC reclamado, misión grayed out |
| **All Claimed** | Las 3 misiones reclamadas → bonus de 50 GDC automático |

**Achievement flow:**

| Estado | Descripción |
|--------|-------------|
| **Locked** | Condición no visible (logros ocultos) o visible pero no cumplida |
| **In Progress** | Progreso parcial visible (X/Y) |
| **Completed** | Condición cumplida, reward pendiente de reclamar. Badge "!" en menú |
| **Claimed** | Reward reclamado. Logro marcado como completado |

### Interactions with Other Systems

| Sistema | Dirección | Interfaz |
|---------|-----------|----------|
| **Currency System** | RS → CS | Calls `AddCurrency(DOB, amount)` y `AddCurrency(GDC, amount)` al distribuir rewards. Respeta interfaz existente |
| **Unit Data Model** | UDM → RS | RS referencia IDs de items (Ron, Cristales) definidos en inventario. UDM define qué materiales existen |
| **Stage System** | SS → RS | Stage define `RewardConfig` por batalla. RS ejecuta la distribución post-combate usando esa config |
| **Progresión de Unidades** | RS → PU | RS otorga Ron y Cristales Elementales como drops. PU los consume para leveling y awakening |
| **Sistema Gacha** | RS → SG | Ticket de Invocación (login reward) se canjea en el gacha como pull gratis |
| **Enemy System** | ES → RS | Enemigos definen `XPReward`. RS suma XP de enemigos derrotados y la distribuye |
| **Save/Load System** | RS ↔ SL | Persiste: streak counter, calendario mensual progress, mission progress, achievement progress, inventario de materiales |
| **Combat UI** | RS → CUI | Pantalla post-combate es parte del flujo de Combat UI pero los datos vienen de RS |
| **Menus & Navigation UI** | RS → MUI | Pantalla de logros, misiones diarias, calendario de login viven en los menús |
| **Energy System** (futuro) | EN → RS | Eventos consumen energía. Energy System valida si hay energía suficiente antes de entrar |

**Interfaz ownership:** RS posee la lógica de distribución de rewards, las loot
tables de eventos, los calendarios de login, las misiones diarias, y los logros.
No posee las currencies (Currency System), ni los templates de materiales (UDM),
ni las configs de stages (Stage System).

**Cross-system updates necesarios:**
- Currency System: `DAILY_MISSION_GDC` 100 → 140 GDC/día
- Currency System: `DAILY_LOGIN_GDC` 50 → ~49 GDC/día (promedio del sistema dual)
- Currency System: Añadir TIE como nuevo recurso. Recalcular F2P Weekly Income
- Currency System: Añadir Ticket Featured como recurso (1/mes del streak día 28)
- Sistema Gacha: Añadir banner estándar (TIE) y Ticket Featured
- Stage System: Agregar referencia a energía en eventos (campo `EnergyCost`)
- Progresión de Unidades: Confirmar que Ron y Cristales tienen drop source definido (este GDD lo resuelve)

**Divergencia deliberada con game-concept.md:** El game concept dice "Sin
penalización por perder — se conserva la energía/intento." Este GDD establece
que **en eventos**, la energía SÍ se consume en derrota. Esto es intencional:
los eventos tienen rewards valiosos y el consumo de energía en derrota incentiva
al jugador a prepararse antes de entrar. Los story stages siguen la regla del
game concept (sin penalización).

## Formulas

### 1. Pull Economy Overview

El juego tiene dos recursos de gacha separados:
- **GDC (Gemas de Calavera)**: Moneda premium para banner **featured** (300 GDC = 1 pull)
- **Ticket de Invocación Estándar (TIE)**: Item para banner **estándar** (1 ticket = 1 pull)

> **Cross-system update**: Currency System debe añadir TIE como nuevo recurso.
> Sistema Gacha debe dividirse en banner featured (GDC) y banner estándar (TIE).

**F2P Monthly Targets:**

| Banner | Pulls/mes (recurrente) | Recurso | Multi equivalentes |
|--------|----------------------|---------|-------------------|
| Featured | ~19 | ~5,680 GDC | ~2.1 multis |
| Estándar | ~15 recurrente (~20 primer mes) | 15-20 TIE | N/A (sin multi) |
| **Total** | **~34-39** | — | — |

### 2. GDC Income Recurrente (Featured Banner)

```
MonthlyGDC = DailyMissionsGDC + LoginStreakGDC + LoginCalendarGDC
```

| Fuente | GDC/día | GDC/mes (30 días) |
|--------|---------|-------------------|
| Daily Missions (3×30 + 50 bonus) | 140 | 4,200 |
| Login Streak (1,110 GDC / 28 días) | ~40 | ~1,110 |
| Login Calendario Mensual | ~12 | ~370 |
| **Total recurrente** | **~190** | **~5,680** |

Equivale a **~18.9 pulls featured/mes** o **~2.1 multi-pulls/mes**.

### 3. Ticket de Invocación Estándar (TIE) Income

| Fuente | TIE/mes | Notas | Definido en |
|--------|---------|-------|-------------|
| Login Streak (hitos días 7, 14, 21) | 3 | 1 TIE por hito semanal (día 28 = Featured, no TIE) | §6 Streak Calendar |
| Login Calendario Mensual (hitos con TIE) | 4 | 1 TIE en días 4, 12, 20, 28 del mes | §6 Calendario Mensual |
| Misión semanal (streak de misiones) | 4 | 1 TIE por completar las 3 misiones diarias 7 días consecutivos | §7 Daily Missions |
| **Total recurrente (demo)** | **~11** | Fuentes que se repiten cada mes | |

**Fuentes one-time de TIE (no recurrentes en demo):**

| Fuente | TIE total | Notas | Definido en |
|--------|-----------|-------|-------------|
| Event Boss first-clear | 8 | 1 TIE por primer clear de cada Boss (7 cristales + 1 Ron). One-time en demo; recurrente en juego completo con nuevos eventos | §3-4 Event System |
| Chapter-clear bonus | 3 | 1 TIE por completar Ch1, Ch2, y Naval | §2 Story/Naval Rewards |
| Achievements | 15 | Repartidos en 15 logros seleccionados; ~5/mes amortizado en 3 meses | §8 Achievements |
| **Total one-time (demo)** | **~26** | | |

**Nota — Misión semanal TIE:** Si el jugador completa las 3 misiones diarias
durante 7 días consecutivos, recibe 1 TIE como bonus semanal. Tracking:
contador interno `consecutive_daily_complete` (0-7). Al llegar a 7, otorga TIE
y reinicia. No es una misión visible separada — es un bonus oculto del sistema
de misiones con indicador de progreso (6/7 días) en la UI.

### 4. GDC One-Time Income (Demo Lifetime)

| Fuente | GDC total | Notas |
|--------|-----------|-------|
| Stage first-clear + missions | ~1,810 | 13 battles × ~120 GDC avg + chapter bonuses |
| Achievements | ~4,750 | ~53 logros con GDC rewards |
| **Total one-time** | **~6,560** | ~21.9 pulls featured |

### 5. TIE One-Time Income (Demo Lifetime)

| Fuente | Tickets | Notas |
|--------|---------|-------|
| Achievements (colección, progresión) | ~15 | Hitos one-time que dan tickets |
| Chapter-clear bonus | ~3 | 1 ticket por completar cada capítulo + naval |
| **Total one-time** | **~18** | |

### 6. First Month F2P Totals (Recurrente + One-Time)

| Banner | Recurrente | One-time | Primer mes |
|--------|-----------|----------|------------|
| Featured (GDC) | ~19 | ~22 (stages + logros) | **~41** |
| Estándar (TIE) | ~20 (con logros) | ~15 (logros restantes) | **~35** |
| **Total primer mes** | | | **~76 pulls** |

Los meses siguientes bajan a ~34 pulls/mes recurrentes (19 featured + 15 estándar)
a medida que se agotan las fuentes one-time de logros.

### 7. Event Stage DOB/XP (reducido vs Story)

```
EventDOB = floor((BASE_STAGE_DOB + (EquivStageIndex × DOB_PER_STAGE)) × EVENT_DOB_MODIFIER)
EventXP = floor((BASE_STAGE_XP + (EquivStageIndex × XP_PER_STAGE)) × EVENT_XP_MODIFIER)
```

| Variable | Valor | Descripción |
|----------|-------|-------------|
| `EVENT_DOB_MODIFIER` | 0.5 | Eventos dan 50% del DOB equivalente |
| `EVENT_XP_MODIFIER` | 0.5 | Eventos dan 50% del XP equivalente |
| `EquivStageIndex` | Básico=5, Medio=15, Boss=25 | Índice equivalente por dificultad |

Ejemplos:
- Básico: **175 DOB**, **125 XP**
- Medio: **425 DOB**, **275 XP**
- Boss: **700 DOB**, **425 XP**

### 8. Material Farming Rate (Cristales)

```
ExpectedCristalesPerRun[diff] = Guaranteed + sum(BonusChance × BonusAmount)
```

| Dificultad | Garantizado | E[Bonus] | E[Total/run] |
|------------|-------------|----------|--------------|
| Básico | 2× T1 | 0.3 T1 | **2.3 T1** |
| Medio | 3× T1, 1× T2 | 0.3 T1, 0.15 T2 | **3.3 T1 + 1.15 T2** |
| Boss | 2× T2, 1× T3 | 0.25 T2, 0.1 T3 | **2.25 T2 + 1.1 T3** |

**Awakening farming targets:**
- 1st Awakening (5× T1): ~2-3 runs Básico
- 2nd Awakening (10× T1 + 3× T2): ~3 runs Básico + 3 runs Medio
- 3rd Awakening (15× T2 + 1× T3): ~7 runs Medio + 1 run Boss

### 9. Ron Farming Rate

| Dificultad | Garantizado | E[Bonus] | E[XP total/run] |
|------------|-------------|----------|-----------------|
| Básico | 2× Añejo | 0.3 Añejo | **1,150 XP** |
| Medio | 2× Capitán | 0.25 Cap + 0.1 Añ | **5,675 XP** |
| Boss | 1× Legendario | 0.2 Cap + 0.05 Leg | **11,000 XP** |

### 10. Energy Budget (Provisional)

```
DailyEventRuns = DAILY_ENERGY / EventEnergyCost[difficulty]
```

| Variable | Valor | Descripción |
|----------|-------|-------------|
| `DAILY_ENERGY` | TBD | Energía diaria (Energy System — no diseñado aún) |
| `EVENT_ENERGY_BASIC` | 10 | Energía por run básico |
| `EVENT_ENERGY_MEDIUM` | 15 | Energía por run medio |
| `EVENT_ENERGY_BOSS` | 25 | Energía por run boss |

### Variable Definitions (Complete)

| Variable | Type | Default | Range | Descripción |
|----------|------|---------|-------|-------------|
| EVENT_DOB_MODIFIER | float | 0.5 | 0.3-0.8 | Ratio DOB eventos vs story |
| EVENT_XP_MODIFIER | float | 0.5 | 0.3-0.8 | Ratio XP eventos vs story |
| EVENT_ENERGY_BASIC | int | 10 | 5-15 | Energía por run básico |
| EVENT_ENERGY_MEDIUM | int | 15 | 10-25 | Energía por run medio |
| EVENT_ENERGY_BOSS | int | 25 | 15-40 | Energía por run boss |
| DAILY_MISSION_GDC | int | 30 | 15-50 | GDC por misión diaria |
| MISSION_BONUS_GDC | int | 50 | 25-100 | Bonus por completar las 3 misiones |
| STREAK_CYCLE_DAYS | int | 28 | 14-30 | Duración del ciclo de streak |
| SINGLE_PULL_COST_GDC | int | 300 | 200-500 | Costo de 1 pull featured |
| MULTI_PULL_COST_GDC | int | 2,700 | 1,800-4,500 | Costo de 10-pull featured (10% desc) |

## Edge Cases

### Post-Combat Rewards

| Edge Case | Resolución |
|-----------|------------|
| Player pierde un stage de evento | No se otorgan drops de materiales. La energía SÍ se consume (a diferencia de story stages donde se conserva). Esto incentiva preparación antes de entrar |
| Player se desconecta durante pantalla de rewards | Rewards se calculan y persisten atómicamente al final del combate, antes de mostrar la pantalla. Si se desconecta, al reconectar los rewards ya están en inventario. La pantalla de rewards se marca como "vista" |
| Player cierra la app en la pantalla de rewards sin reclamar | Auto-claim: rewards se aplican al inventario al calcularse, la pantalla es solo presentación. No hay reclamación manual post-combate |
| Bonus drop de material que el jugador ya tiene al máximo stack | Los materiales no tienen stack máximo en la demo. Si se implementa un cap futuro, el exceso se convierte a DOB |
| Stage de evento da XP pero toda la party está al cap | XP se pierde silenciosamente (consistente con Progresión de Unidades) |

### Daily Login

| Edge Case | Resolución |
|-----------|------------|
| Player entra a las 23:59 UTC y sale a las 00:01 UTC | Login se registra con la fecha del primer acceso (23:59 = día anterior). El nuevo día se detecta solo al relanzar la app o al navegar al menú principal |
| Player no entra 1 día y pierde el streak | Streak se reinicia a Día 1. No hay recuperación ni compra de "streak shield". El reward del calendario mensual fijo NO se pierde — solo el streak |
| Player completa el streak de 28 días | Se reinicia automáticamente a Día 1. Puede volver a ganar todos los rewards del ciclo |
| Player cambia zona horaria del dispositivo | Login se basa en hora UTC del servidor, no del dispositivo. Manipular timezone no genera logins extra |
| Player entra al juego por primera vez a mitad de mes | Streak empieza en Día 1 independientemente de la fecha. El calendario mensual da el reward del día actual del mes (puede perderse los días previos) |
| Día 29-31 del mes en calendario mensual | Meses con >28 días repiten el reward del día 28 para los días extra. El patrón de 28 es el ciclo completo |

### Daily Missions

| Edge Case | Resolución |
|-----------|------------|
| Player completa la condición de una misión que no está activa hoy | No cuenta. Solo las 3 misiones del día actual trackean progreso |
| Player completa 2 misiones y el día cambia (UTC) | Progreso se reinicia. Las 2 misiones completadas se pierden si no se reclamó el GDC. Auto-claim al cambio de día para evitar pérdida |
| Misión "Haz 1 pull de gacha" pero el player no tiene GDC ni tickets | Misión se queda activa sin completar. No es obligatorio completar las 3. El bonus de 50 GDC solo se obtiene al completar todas |
| Las 3 misiones del día son todas incompletables | El pool tiene suficiente variedad (12 misiones) para que siempre haya al menos 1-2 completables. Seed se valida para no generar combinaciones imposibles |
| Player completa misión pero no la reclama antes del reset | Auto-claim al final del día: rewards pendientes de misiones completadas se aplican automáticamente antes del reset |

### Achievements

| Edge Case | Resolución |
|-----------|------------|
| Player cumple condición de un logro pero no lo reclama | Logros completados quedan en estado "Completed" indefinidamente hasta reclamar. Badge "!" permanece en el menú |
| Player cumple condición de un logro sin saberlo | Popup de notificación aparece: "¡Logro desbloqueado: [nombre]!" con botón para ir a reclamar |
| Player deshace la condición (ej: vende unidad después de logro "Obtén 5 unidades") | Logros son one-way: una vez cumplida la condición, no se revoca aunque se deshaga |
| Logro "Obtén una 5★ de un pull (no pity)" — cómo se detecta | El Sistema Gacha marca cada pull como "natural" o "pity". El logro solo se completa si el pull fue natural |

### Event System

| Edge Case | Resolución |
|-----------|------------|
| Sábado (Santuario Dual): player quiere Luz pero le dan Sombra | Sábado ofrece DOS stages separados: uno de Luz y otro de Sombra. El jugador elige cuál correr. Cada uno tiene su propia loot table |
| Domingo (todos abiertos): demasiadas opciones | UI organiza los eventos por elemento con iconos claros. "Todos abiertos" no es un stage nuevo — desbloquea los 7 stages individuales |
| Player entra a evento a las 23:55 UTC Lunes y termina a las 00:05 UTC Martes | El evento activo se determina al ENTRAR al stage, no al terminar. Drops son del evento del Lunes |
| No hay energía para entrar a un evento | Botón de entrar deshabilitado con mensaje "Energía insuficiente" y shortcut para refill (GDC) o espera |

### Tickets de Invocación Estándar (TIE)

| Edge Case | Resolución |
|-----------|------------|
| Player acumula tickets sin usarlos | Sin límite de acumulación. Los tickets no expiran |
| Player tiene tickets pero no quiere usar el banner estándar | Los tickets NO son convertibles a GDC ni a nada. Solo sirven para el banner estándar |
| Player obtiene un ticket de una fuente y ya tiene 999 | Sin cap práctico en la demo. Max teórico: 9,999 tickets |

## Dependencies

### Upstream Dependencies (RS depende de)

| Sistema | Tipo | Interfaz | GDD |
|---------|------|----------|-----|
| **Currency System** | Hard | `AddCurrency(DOB, amount)`, `AddCurrency(GDC, amount)` para distribuir rewards de currencies. RS es el faucet principal | ✅ Approved (necesita update: TIE + daily GDC target) |
| **Unit Data Model** | Hard | Define IDs de materiales (Ron, Cristales Elementales) como items de inventario. RS referencia estos IDs en loot tables | ✅ Approved |
| **Stage System** | Hard | Define `RewardConfig` por batalla (DOB, GDC first-clear, missions). RS ejecuta la distribución usando esa config | ✅ Approved (necesita update: EnergyCost campo) |
| **Enemy System** | Soft | Enemigos definen `XPReward`. RS suma XP de enemigos derrotados. RS funciona sin esto (usa XP flat) | ✅ Approved |
| **Energy System** | Hard (futuro) | Valida energía antes de entrar a eventos. Sin Energy System, eventos son ilimitados | ⬜ No diseñado |

### Downstream Dependencies (dependen de RS)

| Sistema | Tipo | Qué necesita de RS | GDD |
|---------|------|---------------------|-----|
| **Progresión de Unidades** | Hard | RS es la fuente de Ron (XP items) y Cristales Elementales. Sin RS, no hay materiales para leveling/awakening | ✅ Approved |
| **Sistema Gacha** | Hard | RS otorga TIE (Ticket de Invocación Estándar) y Tickets Featured. Gacha necesita saber qué tipos de tickets existen | ✅ Approved (necesita update: banner estándar + TIE) |
| **Save/Load System** | Hard | Persiste: streak counter, calendario mensual progress, mission progress, achievement progress, inventario de TIE y materiales | ⬜ Not Started |
| **Unit Roster/Inventory** | Soft | Muestra materiales en inventario (Ron, Cristales, TIE) que RS otorga | ⬜ Not Started |
| **Menus & Navigation UI** | Hard | Pantallas de: logros, misiones diarias, calendario de login, evento select. UI lee datos de RS | ⬜ Not Started |
| **Combat UI** | Soft | Pantalla post-combate muestra rewards calculados por RS | ✅ Approved |

### Cross-System Updates Pendientes

| Sistema | Cambio requerido | Prioridad |
|---------|-----------------|-----------|
| **Currency System** | Añadir TIE como recurso. Actualizar `DAILY_MISSION_GDC` a 140. Actualizar `DAILY_LOGIN_GDC` a ~47. Recalcular F2P Weekly GDC Income | Alta |
| **Sistema Gacha** | Añadir banner estándar (pool permanente) con TIE como moneda. Separar banner featured (GDC) de estándar (TIE). Definir pool/rates del banner estándar | Alta |
| **Stage System** | Añadir campo `EnergyCost` a BattleData (provisional, para cuando Energy System se diseñe). Añadir reference a event stages | Media |
| **Progresión de Unidades** | Confirmar que Ron y Cristales tienen drop source definido → resuelto por este GDD. Cerrar Q1 | Baja (informativo) |

## Tuning Knobs

### Event Drop Knobs

| Knob | Valor | Rango | Afecta a | Si muy alto | Si muy bajo |
|------|-------|-------|----------|-------------|-------------|
| Cristal T1 garantizado (Básico) | 2 | 1-4 | Velocidad de 1st awakening | Awakening trivial | Grind excesivo |
| Cristal T2 garantizado (Medio) | 1 | 1-3 | Velocidad de 2nd awakening | Devalúa dificultad medio | 2nd awakening inalcanzable |
| Cristal T3 garantizado (Boss) | 1 | 1-2 | Velocidad de 3rd awakening | 3rd awakening demasiado fácil | Solo 1 unidad 5★ maxed en la demo |
| Bonus chance T1 | 30% | 10-50% | Varianza de farming | Farming muy corto | Garantía+bonus casi igual = boring |
| Bonus chance T2 | 15% | 5-30% | Valor de repetir medio | T2 abundante, Medio pierde valor | T2 escaso, frustración |
| Bonus chance T3 | 10% | 3-20% | Rareza de T3 | T3 demasiado común | Nunca se ve un T3 bonus |
| Ron Añejo garantizado (Básico) | 2 | 1-3 | XP income base | Leveling muy rápido | Early game lento |
| Ron Capitán garantizado (Medio) | 2 | 1-3 | XP income medio | Mid-game leveling trivial | Grind de Ron excesivo |
| Ron Legendario garantizado (Boss) | 1 | 1-2 | XP income boss | Boss farming = level skip | Ron Legendario no se siente especial |

### Economy Knobs

| Knob | Valor | Rango | Afecta a | Si muy alto | Si muy bajo |
|------|-------|-------|----------|-------------|-------------|
| DAILY_MISSION_GDC | 30/misión | 15-50 | Featured pulls F2P | Demasiados pulls, devalúa IAP | F2P frustrado, abandona |
| MISSION_BONUS_GDC | 50 | 25-100 | Incentivo de completar las 3 | Bonus > misiones, distorsiona | No incentiva completar todo |
| TIE mensual (recurrente) | ~22 | 15-30 | Standard pulls F2P | Estándar se siente gratis | Estándar ignorado por jugadores |
| EVENT_DOB_MODIFIER | 0.5 | 0.3-0.8 | DOB de eventos vs story | Eventos dan tanto DOB como story | No vale la pena repetir eventos por DOB |
| EVENT_XP_MODIFIER | 0.5 | 0.3-0.8 | XP de eventos vs story | Eventos dan demasiada XP | Eventos no contribuyen al leveling |
| EVENT_ENERGY_BASIC | 10 | 5-15 | Runs diarios de básico | Pocos runs, farming lento | Demasiados runs, materiales abundan |
| EVENT_ENERGY_MEDIUM | 15 | 10-25 | Runs diarios de medio | Pocos runs de medio | Medio reemplaza básico |
| EVENT_ENERGY_BOSS | 25 | 15-40 | Runs diarios de boss | Boss = 1 run/día max | Boss farmeable en loop |
| STREAK_CYCLE_DAYS | 28 | 14-30 | Duración del streak | Difícil completar un ciclo | Ciclo demasiado corto, rewards triviales |

### Login Calendar Knobs

| Knob | Valor | Rango | Afecta a |
|------|-------|-------|----------|
| GDC por día normal (streak) | 30-60 | 15-100 | GDC income del streak |
| DOB por día normal (streak) | 500-6,000 | 200-10,000 | DOB income del streak |
| GDC hito semanal (streak) | 100-300 | 50-500 | Valor de los hitos |
| TIE en hitos (streak) | 1 por hito | 0-2 | Standard pulls del streak |
| GDC por día (mensual fijo) | 20-30 | 10-50 | GDC income del calendario |

### Knob Interactions (Danger Zones)

| Knob A | Knob B | Interacción |
|--------|--------|-------------|
| DAILY_MISSION_GDC | SINGLE_PULL_COST_GDC | Juntos determinan días para 1 pull. 140/300 = ~2.1 días/pull. Cambiar uno sin el otro distorsiona |
| Drop cristales | AWK material counts (Progresión) | 2 garantizados T1 con 5 necesarios = ~3 runs. Si subes los drops O bajas los requisitos, awakening se trivializa |
| TIE income | Pool del banner estándar (Gacha) | Más tickets con pool pequeño = muchos dupes rápido. Equilibrar con tamaño del pool |
| EVENT_ENERGY_* | DAILY_ENERGY (Energy System) | Energía diaria ÷ costo por run = runs máximos. Si energía sube sin subir costos, farming se descontrola |
| Ron income | XP curve (Progresión) | Ron abundante + curva suave = leveling instantáneo. Ron escaso + curva agresiva = grind |
| Streak GDC | Mensual GDC | Juntos = login GDC total. Streak debe ser la parte más valiosa (incentiva consecutividad) |

## Visual/Audio Requirements

### Visual

**Pantalla de rewards post-combate:**
- Cofre del tesoro se abre al centro de la pantalla
- Items aparecen uno por uno con animación de salto desde el cofre
- DOB: monedas doradas volando al contador del top bar
- GDC: gemas púrpura con sparkle (solo en first-clear)
- Materiales (Ron, Cristales): icono del item con nombre y cantidad
- **Bonus drops**: marcados con banner "¡Bonus!" dorado + efecto de brillo extra
- TIE: ticket dorado con borde brillante — debe sentirse especial
- XP: barra de XP de cada unidad se llena visualmente (compacto, no individual)

**Daily Login popup:**
- Pantalla dividida en dos secciones:
  - **Arriba**: Streak calendar — muestra los 28 días como path/camino con el día
    actual iluminado. Días completados con checkmark. Hitos (7, 14, 21, 28) más
    grandes y brillantes
  - **Abajo**: Calendario mensual — grid clásico de calendario con el día actual
    marcado. Reward del día visible
- Animación de cofre al reclamar (breve, ~1 segundo)
- Streak counter visible: "Racha: X días"

**Misiones diarias:**
- 3 cards verticales con icono, descripción, barra de progreso, y reward (GDC)
- Card completada: brillo dorado + checkmark verde
- Bonus bar al fondo: se llena con cada misión completada. Al llenarse (3/3):
  efecto de explosión de monedas + "¡Bonus!" popup

**Logros:**
- Grid/lista scrolleable organizada por categorías (tabs: Progresión, Colección,
  Poder, Economía, Eventos, Login)
- Cada logro: icono, nombre, descripción, barra de progreso (X/Y), reward,
  estado (locked/in-progress/completed/claimed)
- Logro completado sin reclamar: pulso dorado en el icono + badge "!"
- Al reclamar: animación de cofre pequeño + items volando al inventario

**Evento select:**
- Cards horizontales por evento disponible hoy (cristales del elemento + Ron)
- Cada card: nombre del evento, icono del material, 3 botones de dificultad
  (Básico/Medio/Boss) con recommended level y energía cost
- Evento cerrado (no es su día): card gris con candado y texto "Disponible: [día]"

### Audio

| Elemento | SFX | Notas |
|----------|-----|-------|
| Cofre de rewards (post-combat) | Crujido de madera + clic de cerradura | ~1 seg, satisfactorio |
| Item aparece del cofre | Pop suave + tintineo | Rápido, no molesto en repetición |
| Bonus drop | Chime extra más agudo + sparkle | Debe sonar "afortunado" |
| DOB volando al counter | Cascada de monedas (corto) | Same que Currency System |
| GDC volando al counter | Cristal chime | Same que Currency System |
| TIE obtenido | Fanfarria breve (~1.5 seg) | Más impactante que materiales normales |
| Login streak reward | Cofre + chime ascendente | Tono varía con día del streak (más épico = más avanzado) |
| Misión completada | Stamp/sello satisfactorio | Breve, no interrumpe gameplay |
| Bonus 3/3 misiones | Mini-fanfarria pirata (~2 seg) | Momento de celebración |
| Logro desbloqueado | Campana + whoosh | Notificación no intrusiva |
| Logro reclamado | Cofre pequeño + chime | Similar a login pero más corto |

## UI Requirements

### Pantalla de Rewards Post-Combate
- Se muestra automáticamente al ganar un stage (story, naval, o evento)
- Layout: cofre central, items aparecen en grid debajo
- Botón "Continuar" (o tap anywhere) para cerrar
- First-clear rewards y mission rewards se muestran SEPARADOS de replay rewards
  con labels "Primer Clear" y "Misión Completada"
- Tap rápido / skip: muestra todos los items al instante (Pillar 4)

### Pantalla de Daily Login
- Se muestra automáticamente al entrar al juego (1 vez por día)
- NO bloquea el juego — se puede cerrar con X o tap fuera
- Botón "Reclamar" grande y visible
- Muestra preview del reward de mañana (streak): "Mañana: [icon] [reward]"
- Si streak se rompió: mensaje breve "Racha reiniciada" (no punitivo, sin
  culpa — tono neutro)

### Pantalla de Daily Missions
- Accesible desde menú principal (tab dedicada o sub-sección)
- Timer visible: "Reinicio en: XX:XX:XX"
- Badge "!" en el tab cuando hay misiones completables/completadas
- Botón "Reclamar" por misión individual + "Reclamar Todo" si hay varias

### Pantalla de Achievements
- Accesible desde menú principal
- Filtros por categoría (tabs)
- Filtro de estado: "Sin reclamar" para ir directo a rewards pendientes
- Botón "Reclamar Todo" para logros completados
- Progreso global: "X/53 logros completados" en header

### Pantalla de Event Select
- Dentro de la tab "Aventura" → sub-tab "Eventos"
- Muestra día actual y qué eventos están abiertos
- Preview de calendario semanal: qué evento abre cada día (para planificación)
- Para cada evento: 3 dificultades con lock visual si level insuficiente
- Counter de energía visible en la esquina
- "¿Dónde farmear [material]?" → link desde pantalla de awakening en Progresión
  que lleva directamente al evento correspondiente

## Acceptance Criteria

### Post-Combat Rewards

| # | Criterio | Cómo verificar |
|---|----------|----------------|
| 1 | Story stages otorgan DOB correcto por fórmula | Test: stage 1 = 100 DOB, stage 10 = 550 DOB. Replay = misma cantidad |
| 2 | First-clear GDC se otorga exactamente una vez | Test: clear stage → recibe GDC. Replay → no recibe GDC |
| 3 | Mission GDC se otorga exactamente una vez por misión | Test: completar misión → GDC. Re-clear con misma condición → no GDC |
| 4 | Event stages otorgan materiales garantizados correctos | Test: Básico cristales = 2× T1, Medio = 3× T1 + 1× T2, Boss = 2× T2 + 1× T3 |
| 5 | Event stages otorgan Ron garantizado correcto | Test: Básico = 2× Añejo, Medio = 2× Capitán, Boss = 1× Legendario |
| 6 | Bonus drops tienen las probabilidades correctas | Test estadístico: 1000 runs simulados, verificar que distribución de bonus coincide con chances definidas (±5%) |
| 7 | Derrota en evento NO otorga drops pero SÍ consume energía | Test: perder en evento → sin materiales, energía reducida |
| 8 | Derrota en story NO consume energía ni otorga rewards | Test: perder en story → energía intacta, sin rewards |
| 9 | DOB/XP de eventos = 50% del equivalente story | Test: evento Básico (equiv stage 5) = 175 DOB, 125 XP |

### Event System

| # | Criterio | Cómo verificar |
|---|----------|----------------|
| 10 | Rotación diaria correcta (Lun=Pólvora, Mar=Tormenta...) | Test: verificar cada día de la semana muestra el evento correcto |
| 11 | Sábado muestra DOS eventos separados (Luz + Sombra) | Test: sábado → 2 stages de cristales disponibles |
| 12 | Domingo muestra TODOS los eventos de cristales | Test: domingo → 7 stages de cristales + Ron disponibles |
| 13 | Evento de Ron disponible todos los días | Test: cada día de la semana, Taberna del Puerto accesible |
| 14 | Cambio de evento ocurre a medianoche UTC | Test: evento de Lunes activo hasta 23:59 UTC. A las 00:00 cambia a Martes |

### Daily Login

| # | Criterio | Cómo verificar |
|---|----------|----------------|
| 15 | Streak avanza con login consecutivo | Test: entrar 3 días seguidos → streak = 3 |
| 16 | Streak se reinicia al faltar 1 día | Test: entrar Lun, Mar, faltar Mié, entrar Jue → streak = 1 |
| 17 | Calendario mensual da reward del día actual | Test: entrar el día 7 del mes → reward del día 7 |
| 18 | Rewards de ambos calendarios se otorgan simultáneamente | Test: login → popup muestra reward de streak + reward mensual |
| 19 | Streak de 28 días se reinicia automáticamente | Test: completar día 28 → siguiente login = día 1 del streak |
| 20 | TIE se otorgan correctamente en hitos del streak (días 7, 14, 21) | Test: llegar a día 7 → recibe 1 TIE |
| 21 | Ticket Featured se otorga en día 28 del streak | Test: completar streak 28 → recibe 1 Ticket Featured |

### Daily Missions

| # | Criterio | Cómo verificar |
|---|----------|----------------|
| 22 | 3 misiones distintas aparecen cada día | Test: verificar que las 3 misiones son diferentes entre sí |
| 23 | Misiones se reinician a medianoche UTC | Test: misión completada a las 23:00 → a las 00:01 nuevas misiones |
| 24 | Completar las 3 misiones otorga +50 GDC bonus | Test: completar 3/3 → bonus de 50 GDC automático |
| 25 | Auto-claim al final del día para misiones completadas no reclamadas | Test: completar misión, no reclamar, esperar reset → GDC creditado |
| 26 | Misiones no completadas no otorgan GDC | Test: completar 1/3, reset → solo 30 GDC recibido (no 90+50) |

### Achievements

| # | Criterio | Cómo verificar |
|---|----------|----------------|
| 27 | Logros se completan al cumplir condición | Test: obtener 3 unidades → logro "Primera tripulación" = Completed |
| 28 | Logros requieren reclamación manual | Test: logro completado → reward pendiente hasta tap |
| 29 | Logros son one-time y no se revocan | Test: obtener logro "5 unidades", vender 1 → logro sigue completado |
| 30 | Badge "!" aparece en menú cuando hay logros sin reclamar | Visual test: completar logro → menú principal muestra badge |
| 31 | Total de logros en demo ≥ 45 | Content test: contar logros implementados |

### Economy Validation

| # | Criterio | Cómo verificar |
|---|----------|----------------|
| 32 | F2P recurrente ≈ 19 featured pulls/mes | Simulación: 30 días de login + missions = ~5,680 GDC = ~18.9 pulls |
| 33 | F2P recurrente ≈ 15 standard tickets/mes (sin logros) | Simulación: contar TIE de login streak (3) + calendar (4) + boss first-clear (4) + misión semanal (4) = 15 |
| 34 | 1st awakening farmeable en ~3 días de eventos | Simulación: 2-3 runs Básico/día × 2 días = 5+ T1 cristales |
| 35 | Inventario de materiales persiste entre sesiones | Save/load test: obtener materiales → cerrar → abrir → materiales intactos |

## Open Questions

> **Nota de escalado**: Los ratios actuales (~40 pulls/mes recurrentes) son para
> la demo. A medida que el juego completo añada más contenido (eventos temporales,
> PvP, guilds, exploration stages, etc.), se incrementarán progresivamente las
> fuentes de income para dar más recompensas al jugador. El target a largo plazo
> es acercarse a ~50-60 pulls/mes (nivel HSR) cuando haya suficiente contenido
> para sostenerlo.

| # | Pregunta | Impacto | Owner | Target |
|---|----------|---------|-------|--------|
| 1 | **Energy System**: ¿Cuánta energía diaria? ¿Cómo se recarga (tiempo, GDC, items)?  Define cuántos runs de evento puede hacer el jugador/día, que controla el farming rate | High — bottleneck de toda la economía de materiales | Economy Designer | GDD separado (Energy System) |
| 2 | **Banner estándar pool & rates**: ¿Qué unidades están en el pool estándar? ¿Mismas rates que featured (2%/15%/83%)? ¿Pity system en estándar? | High — define el valor de los TIE | Economy Designer | Update de Sistema Gacha GDD |
| 3 | **Ticket Featured en día 28**: ¿Es un ticket que funciona como 1 pull gratis en el banner featured actual, o un pull en el banner permanente de unidades con rate up? | Medium — afecta al valor del streak máximo | Game Designer | Update de Sistema Gacha GDD |
| 4 | **Eventos navales**: Los eventos de cristales actuales son todos combate terrestre (Land). ¿Deberían algunos eventos tener variante naval? Ej: Boss de eventos = naval | Medium — conecta eventos con combate naval | Game Designer | Al diseñar contenido de eventos |
| 5 | **Scaling de logros post-demo**: La demo tiene ~53 logros. ¿Cómo escala el sistema? ¿Logros por temporada/season? ¿Logros infinitos (kill 100, kill 1000...)? | Low (post-demo) — arquitectura futura | Game Designer | Post-demo |
| 6 | ~~**Misión semanal formal**~~: **Resuelto** — se implementa como bonus oculto del sistema de misiones con contador visible "Racha de misiones: X/7 días". No es misión separada ni logro | — | — | Definido en §7 |
| 7 | **Eventos de tiempo limitado vs permanentes**: En la demo, ¿los eventos de cristales/Ron son permanentes? ¿O hay eventos de tiempo limitado con rewards exclusivos? | Low (demo) — la demo solo tiene eventos permanentes. Temporales = post-demo | Producer | Post-demo |
| 8 | **Cross-system update de Currency System y Gacha**: Los cambios de TIE, daily GDC target, y banner estándar necesitan propagarse. ¿Se actualiza antes de seguir con otros GDDs? | High — otros sistemas referencian la economía | Producer | Antes del Save/Load GDD |
