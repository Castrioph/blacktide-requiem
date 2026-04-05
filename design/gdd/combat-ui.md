# Combat UI

> **Status**: Approved
> **Author**: user + agents
> **Last Updated**: 2026-03-27
> **Implements Pillar**: Pillar 1 (Profundidad Estratégica Dual), Pillar 4 (Respeto al Tiempo del Jugador)

## Overview

El Combat UI es la capa de presentación e interacción del jugador durante ambos modos de combate (terrestre y naval). Implementado como un **framework único adaptativo**, define un layout base compartido — Initiative Bar, barras de HP, indicadores de estado, números de daño flotantes, contador de oleada, y pantallas de victoria/derrota — que se mantiene constante entre modos. Sobre esta base, cada modo intercambia componentes específicos: el combate terrestre muestra un panel de 4 acciones (Ataque Normal, Habilidad, Guardia, Pasar Turno), selección de targets individuales, y barras de HP por unidad; el combate naval reemplaza el panel por 5 acciones + Pasar (Cañonazo, Habilidad Naval, Maniobra Evasiva, Abordaje, Reparar), añade la vista de crew aliada y enemiga, el panel de stats del barco en tiempo real, la barra de MP como recurso, y el targeting de crew members individuales para Abordajes. El jugador interactúa activamente con esta UI cada turno: lee la Initiative Bar para anticipar el orden, selecciona acciones del panel, elige targets, y recibe feedback visual y auditivo del resultado. La UI también muestra pasivamente sinergias activas, buffs/debuffs con duración, y ventajas/desventajas elementales. Sin este sistema, la profundidad táctica de los dos modos de combate sería invisible — el jugador no podría tomar decisiones informadas, y la experiencia colapsaría en spam de botones sin comprensión.

## Player Fantasy

**Fantasía**: La Combat UI es tu mesa de mando como capitán. En tierra, ves el campo de batalla de un vistazo: quién actúa cuándo, quién está herido, qué sinergias están activas. En mar, ves tu barco, tu tripulación, y el estado del enemigo desmoronándose a medida que eliminas su crew. No estás operando un menú — estás dando órdenes desde el puente de mando.

**Emoción objetivo**: La UI debe generar **tres sensaciones**:

1. **Claridad táctica** (Pillar 1): "Entiendo la situación". El jugador mira la pantalla y en 2-3 segundos sabe: quién actúa próximo (Initiative Bar), quién está en peligro (HP bars rojas), qué ventajas tiene (sinergias activas, ventaja elemental). La información crítica nunca requiere un tap extra para verla.

2. **Agencia decisiva** (Pillar 1): "Mi decisión importa". El panel de acciones presenta opciones claras con suficiente información para decidir (daño estimado, cooldowns, condiciones LB). Seleccionar target muestra el impacto esperado. El jugador nunca siente que está eligiendo a ciegas.

3. **Feedback satisfactorio** (Pillar 4): "Vi el resultado de mi decisión". Números de daño con colores, animaciones de estados aplicados, la Initiative Bar reordenándose tras un buff de SPD, el icono de LB reapareciendo con un rayo. Cada acción tiene una respuesta visual+auditiva proporcional a su importancia.

**Referencia**: FFBE (paneles de acción claros con arte de habilidades, números de daño expresivos), Genshin Impact (feedback visual que hace que cada golpe se sienta impactante), Fire Emblem (lectura táctica del campo antes de actuar).

**Tipo de sistema**: Activo — el jugador interactúa con la UI cada turno. Es la interfaz primaria durante el 70%+ del tiempo de juego.

**El sistema falla si**: el jugador necesita más de 3 segundos para entender el estado del combate, si seleccionar una acción requiere más de 3 taps (acción → habilidad → target como máximo), si la información importante está escondida detrás de menús, o si la UI naval se siente como "otra interfaz" en vez de una variante natural de la terrestre.

## Detailed Design

### Core Rules

> **Nota cross-system**: Este GDD usa la nomenclatura unificada **MST** (Mística) como stat de daño mágico en ambos modos y **MP** como recurso para habilidades en ambos modos (en naval, los barcos tienen pool de MP separado). ~~Actualizar 7 GDDs~~ ✅ DONE (2026-03-27).

**Orientación de pantalla**: Portrait (vertical), locked para la demo. La arquitectura debe permitir landscape futuro sin rediseño (ver Game Flow GDD).

#### Screen Layout (Portrait)

```
┌─────────────────────────────┐
│      INITIATIVE BAR         │  Zona 1: Turn order (compartido)
│  [🔵][🔵][🔵][🔴][🔴][🔴] │
├─────────────────────────────┤
│  Ronda X  Oleada X/Y  [A][2x]⏸│  Zona 2: Round/Wave + Convenience controls
├─────────────────────────────┤
│                             │
│     ┌───┐         ┌───┐    │
│     │A1 │         │E1 │    │  Zona 3: Battlefield (compartido)
│     └───┘  ┌───┐  └───┘    │  Aliados izq, enemigos der
│  ┌───┐     │E2 │     ┌───┐ │  HP/MP bars sobre cada combatiente
│  │A2 │     └───┘     │E3 │ │  Status icons junto a HP
│  └───┘               └───┘ │
│     ┌───┐                  │
│     │A3 │                  │
│     └───┘                  │
│  [Synergies: ⚔️3 🌊2]     │  Zona 4: Active synergies strip
├─────────────────────────────┤
│  [UNIT NAME]  HP ████░ 75% │  Zona 5: Active unit info panel
│  ATK 120  DEF 80  SPD 95   │
├─────────────────────────────┤
│ ┌──────┐ ┌──────┐          │
│ │ATAQUE│ │HABIL.│          │  Zona 6: Action panel (variable)
│ └──────┘ └──────┘          │  Terrestre: 2×2 (4 botones)
│ ┌──────┐ ┌──────┐          │  Naval: 2×3 (6 botones)
│ │GUARD.│ │PASAR │          │
│ └──────┘ └──────┘          │
└─────────────────────────────┘
```

El Combat UI usa un **framework único adaptativo**: 6 zonas con layout fijo, donde las Zonas 1-4 son compartidas entre modos y las Zonas 5-6 se adaptan según el modo de combate (terrestre vs naval).

#### 1. Initiative Bar (Zona 1)

Renderizado según el Initiative Bar GDD:
- Horizontal, ancho completo de pantalla, fondo con estética de tablón de madera / puente de cuerda
- Iconos de ~40×40px: retratos chibi circulares. Borde azul (aliados), borde rojo (enemigos). Jefes ligeramente más grandes con corona dorada
- Icono activo: pulsa/brilla, ligeramente agrandado, con flecha/ancla apuntando al sprite en el campo de batalla
- LB insertion: icono aparece con flash/rayo y se desliza a su posición
- Reorder mid-round: iconos se deslizan suavemente a nuevas posiciones (no snap instantáneo)
- CC skip: icono se atenúa, tiembla, y desaparece con efecto de cadenas (stun) o zzz (sleep)
- Transición de ronda: pulso visual de ola de luz de izquierda a derecha al repoblar
- Long-press en icono: tooltip con nombre, SPD actual, estados activos
- Compresión: si hay >N combatientes, los iconos se comprimen proporcionalmente (sin scroll — lectura instantánea es crítica)
- En naval: iconos de barcos/criaturas en vez de unidades individuales. Icono del barco aliado con miniatura de barco

#### 2. Info de Ronda y Oleada (Zona 2)

Franja compacta debajo de la Initiative Bar:
- Lado izquierdo: "Ronda X" — se actualiza cada ronda
- Lado derecho: "Oleada X/Y" — con transición animada entre oleadas
- En la última oleada: indicador visual de tensión (texto rojo pulsante o icono de peligro)
- Transición entre oleadas: breve animación (fade/barrido) + nuevos enemigos entran a escena

**Convenience Controls** (dentro de Zona 2, alineados a la derecha):
- **Botón AUTO**: Toggle Auto-Battle on/off. Muestra "AUTO" (off, atenuado) o "AUTO" con highlight (on). Solo disponible cuando el stage ha sido completado previamente, o siempre en stages de farming
- **Botón de Velocidad**: Cicla entre 1×, 2×, 3× al tap. Afecta todas las duraciones de animación (COMBAT_SPEED_MULTIPLIER). La velocidad seleccionada persiste entre batallas
- **Botón de Pausa**: Icono ⏸. Abre el menú de pausa (Continuar, Rendirse, Configuración)
- Los tres botones son compactos y siempre visibles (incluso durante turnos enemigos y modo auto)

#### 3. Campo de Batalla (Zona 3)

Ocupa el espacio central (~50% de la pantalla):

**Fondo:**
- Escenario estático o con parallax sutil según la Scene del Stage System
- Terrestre: playa, jungla, fortaleza, cueva
- Naval: océano abierto, costa, tormenta, arrecifes. Olas en movimiento

**Combatientes — Terrestre:**
- Aliados izquierda (hasta 6 sprites chibi), enemigos derecha (hasta 5)
- Distribución vertical escalonada para dar sensación de profundidad
- Animaciones mínimas: idle, ataque, habilidad, daño recibido, muerte, guardia

**Combatientes — Naval:**
- Barco aliado izquierda, enemigos derecha
- Crew members visibles como sprites pequeños en las posiciones de rol del barco
- Crew muertos: slot vacío con X roja o silueta gris
- Animaciones de barco: idle (mecerse), cañonazo (retroceso), habilidad (efecto), daño (impacto), hundimiento

**Sobre cada combatiente (ambos modos):**
- **HP bar**: Gradiente de color (verde > amarillo > rojo por %). Aliados: barra estilizada + número exacto. Enemigos Normal: solo barra. Enemigos Elite/Jefe: barra + número
- **MP bar** (debajo de HP): Barra azul. En terrestre: MP de la unidad activa. En naval: MP del barco
- **Status icons**: Iconos compactos junto a la barra de HP con contador de duración en turnos. Tap para tooltip con nombre y efecto
- **Badge de Capitán**: Corona dorada sobre el Capitán aliado. Corona roja para el Capitán enemigo. Tap en capitán enemigo muestra "Derrota para eliminar sinergias enemigas"
- **Indicador elemental**: Icono pequeño del elemento de la unidad/barco

**Números de daño flotantes** (definidos en DSE):
- Color por elemento: blanco (neutro), rojo/naranja (Pólvora), azul eléctrico (Tormenta), morado (Maldición), marrón (Bestia), gris plateado (Acero), blanco brillante (Luz), negro/violeta (Sombra)
- Crítico: color del elemento + borde dorado + tamaño 1.5x + estrella dorada
- Curación: verde + signo "+"
- MISS: gris tenue (Ceguera)
- IMMUNE: gris tenue (CC immunity, boss vs Muerte)
- Ventaja elemental: flash del color del elemento sobre el objetivo + flecha ↑
- Desventaja elemental: número más pequeño + flecha ↓
- DoT ticks: números más pequeños con icono del efecto (gota verde = Veneno, gota roja = Sangrado, llama = Quemadura)
- Múltiples números simultáneos se escalonan verticalmente para evitar overlap

#### 4. Strip de Sinergias Activas (Zona 4)

Franja horizontal compacta entre el campo y el panel de info:
- Iconos de sinergias activas con badge de count (ej: "⚔️×3")
- Tap en icono: popup mostrando qué unidades tienen el buff y los valores de bonus
- Si no hay sinergias activas: la franja se comprime (gana espacio para el campo)
- En naval: incluye bonuses a stats del barco por sinergias de traits

#### 5. Panel de Info del Combatiente Activo (Zona 5)

Franja que muestra la info detallada del combatiente cuyo turno es:

**Terrestre:**
- Retrato, nombre, nivel, icono de elemento
- HP actual/max, MP actual/max
- Stats principales: ATK, DEF, MST, SPR, SPD (valores efectivos con buffs/debuffs resaltados: verde si buff, rojo si debuff)
- Buffs/debuffs activos con iconos y duración restante

**Naval:**
- Nombre del barco, tipo
- HHP actual/max (barra grande), MP actual/max (barra secundaria azul)
- Stats efectivos del barco: FPW, HDF, MST, RSL, SPD — se actualizan en tiempo real cuando crew muere (flash rojo en stats que bajan)
- Crew summary compacto: iconos de roles ocupados (vivo) / vacíos (muerto)

#### 6. Panel de Acciones (Zona 6)

El corazón de la interacción. Ocupa el ~25% inferior de la pantalla.

**Terrestre — Grid 2×2:**

| Ataque Normal | Habilidades |
|---------------|-------------|
| Guardia       | Pasar Turno |

**Naval — Grid 2×3:**

| Cañonazo | Habilidad Naval | Maniobra Evasiva |
|----------|-----------------|------------------|
| Abordaje | Reparar         | Pasar Turno      |

**Estilo y comportamiento:**
- Cada botón tiene: icono con arte propio (identidad pirata), nombre, animación de selección al tap, feedback visual+auditivo al pulsar
- Panel estilizado como timón o carta de navegación — parte de la fantasía pirata, no un menú genérico
- Botones no disponibles se muestran atenuados. Tap en botón atenuado muestra la razón (ej: "Abordaje no disponible: el enemigo no tiene tripulación", "Habilidad en cooldown: 2 turnos")
- En modo Auto-Battle: el panel se oculta y se muestra un indicador "AUTO" con botón para retomar control manual

**Sub-menú de Habilidades:**

Al pulsar "Habilidades" se despliega un sub-menú que reemplaza temporalmente el panel de acciones:
- Cada habilidad se presenta como una carta/ficha con: icono de habilidad (arte único), nombre estilizado, icono de elemento + color, icono de TargetType, breve descripción del efecto, costo MP
- Long-press/hover: tooltip expandido con daño estimado, efectos secundarios con probabilidades, cooldown restante, condición de LB (si tiene `CanLimitBreak`)
- Habilidades con LB: borde o brillo especial que las distingue
- Habilidades en cooldown: atenuadas con contador visual ("CD: 2")
- Botón "Atrás" para volver al panel principal
- En naval: las habilidades incluyen las SeaAbilities de la crew. Si un crew member muere y aportaba habilidades, estas desaparecen del sub-menú

#### 7. Flujo de Selección de Acción (ambos modos)

El flujo máximo es de **3 taps** para cualquier acción: Acción → Target → Confirmar. La mayoría de acciones requieren 1-2 taps.

| Acción | Taps | Flujo |
|--------|------|-------|
| Ataque Normal / Cañonazo | 2 | Tap botón → Tap target enemigo |
| Habilidad single-target | 3 | Tap "Habilidades" → Tap habilidad → Tap target |
| Habilidad AoE | 2 | Tap "Habilidades" → Tap habilidad (auto sobre todos los targets) |
| Habilidad self/ally-aoe | 2 | Tap "Habilidades" → Tap habilidad (auto-target) |
| Habilidad ally-single | 3 | Tap "Habilidades" → Tap habilidad → Tap aliado |
| Guardia / Maniobra | 1 | Tap botón (auto-target: self) |
| Pasar Turno | 1 | Tap botón (inmediato) |
| Abordaje | 2 | Tap "Abordaje" → Tap crew member enemigo |
| Reparar | 1 | Tap botón (auto-target: self, cuesta MP) |

#### 8. Flujo de Targeting

Al entrar en modo targeting (después de seleccionar una acción que requiere target):
- Los targets válidos se resaltan con borde brillante. Los no válidos se atenúan
- Sobre cada target válido se muestra: ventaja/desventaja elemental (flecha ↑ verde o ↓ roja), HP actual
- Para AoE: preview visual sobre todos los targets antes de confirmar (breve highlight de todos)
- **Abordaje (naval)**: los crew members enemigos se resaltan como targets individuales. Cada target muestra: nombre, rol, HP actual, DEF. Tap para seleccionar
- **Cancelar**: tap en área vacía o botón "Atrás" cancela y vuelve al panel de acciones
- **Timeout**: no hay timeout de selección — el turno espera indefinidamente (juego por turnos, sin presión de tiempo)

#### 9. Flujo de Turno Enemigo

Cuando es turno de un enemigo:
- La Initiative Bar destaca el icono enemigo activo
- Breve pausa (~0.5s) para que el jugador vea quién actúa
- El enemigo ejecuta su acción con animación
- Si es jefe con habilidad especial: nombre de la habilidad aparece brevemente en pantalla (estilo "boss attack name flash")
- El jugador no puede interactuar durante el turno enemigo (excepto botón de pausa/menú)

#### 10. Flujo de Limit Break

Cuando se activa un LB:
- Flash/rayo en la Initiative Bar al reinsertar el icono
- SFX impactante (cañonazo/choque de espadas)
- Breve banner de texto: "¡LIMIT BREAK!" sobre el campo de batalla (~1s)
- El turno extra procede normalmente — panel de acciones se reactiva

#### 11. Flujo de Transición de Oleada

Al derrotar la última unidad/barco de una oleada (si no es la última):
- Breve animación de victoria parcial (~1s)
- Fade/barrido del campo
- Nuevos enemigos entran a escena (en naval: navegan hacia la pantalla)
- "Oleada X/Y" se actualiza en Zona 2 con animación
- Initiative Bar se recalcula y repuebla
- El turno del siguiente combatiente comienza automáticamente

### States and Transitions

| Estado | Descripción | Transiciones |
|--------|-------------|-------------|
| `Loading` | Cargando assets de combate, backgrounds, sprites. Pantalla de carga con info del stage | → `PreCombat` (carga completa) |
| `PreCombat` | Campo desplegado con combatientes. Sinergias activadas. Initiative Bar se puebla. Breve animación de inicio (~1-2s) | → `WaitingForInput` (primer turno aliado), → `EnemyTurn` (si enemigo actúa primero) |
| `WaitingForInput` | Turno de unidad aliada. Panel de acciones activo. Initiative Bar destaca la unidad actual. Zona 5 muestra stats | → `SelectingAbility`, → `Targeting`, → `Animating` (acciones sin target) |
| `SelectingAbility` | Sub-menú de habilidades desplegado reemplazando panel de acciones | → `Targeting` (habilidad single-target), → `Animating` (habilidad AoE/self), → `WaitingForInput` (Atrás) |
| `Targeting` | Selección de target. Targets válidos resaltados con info contextual (HP, elemento, ventaja) | → `Animating` (tap target), → `WaitingForInput` (cancelar desde Ataque), → `SelectingAbility` (cancelar desde Habilidad) |
| `Animating` | Acción resolviéndose. Animación + números de daño + efectos. No interactivo excepto pausa | → `PostAction` (animación completa) |
| `PostAction` | Procesamiento post-acción: DoT ticks, check LB, check muerte (~0.3-0.5s) | → `LimitBreak`, → `WaitingForInput`, → `EnemyTurn`, → `WaveTransition`, → `Victory`, → `Defeat` |
| `LimitBreak` | Banner "¡LIMIT BREAK!" + inserción del icono en Initiative Bar con efecto | → `WaitingForInput` (turno extra aliado), → `EnemyTurn` (turno extra enemigo) |
| `EnemyTurn` | Turno de enemigo. Breve pausa, animación, daño. No interactivo | → `PostAction` |
| `WaveTransition` | Transición de oleada. Nuevos enemigos entran. Initiative Bar recalculada | → `WaitingForInput` o → `EnemyTurn` (según orden) |
| `Victory` | Resumen de batalla, animación de recompensas, misiones completadas. Naval: crew sobrevivientes, HHP restante | → (fin de combate) |
| `Defeat` | Pantalla de derrota. Naval: animación de hundimiento. Opciones: Reintentar, Salir | → `Loading` (Reintentar), → (Salir) |
| `Paused` | Menú de pausa superpuesto. Opciones: Continuar, Rendirse, Configuración | → estado anterior (Continuar), → `Defeat` (Rendirse) |

**Diagrama de flujo simplificado:**
```
Loading → PreCombat → [WaitingForInput ↔ SelectingAbility ↔ Targeting]
                              ↓                                    ↓
                          Animating → PostAction → LimitBreak
                              ↑           ↓
                          EnemyTurn ←─────┘
                                          ↓
                    WaveTransition / Victory / Defeat
```

### Interactions with Other Systems

| Sistema | Dirección | Datos que fluyen | Interfaz |
|---------|-----------|-----------------|----------|
| **Combate Terrestre** | CT → CUI | Estado de batalla: combatientes (HP, MP, buffs, estados, posiciones), resultado de acciones (daño, efectos aplicados, muertes), oleada actual, condiciones de victoria/derrota | CUI suscribe a eventos: `OnActionResolved`, `OnTurnStart`, `OnWaveComplete`, `OnBattleEnd` |
| **Combate Naval** | CN → CUI | Mismo flujo que CT con datos navales: HHP/MP del barco, stats efectivos, crew HP/estado por rol, resultado de Abordajes, resultado de Reparar | CUI suscribe a los mismos tipos de eventos. El modo determina qué componentes renderizar |
| **Initiative Bar** | IB → CUI | Orden de turno actual (lista ordenada), reorder events (mid-round SPD changes), LB insertion events, round start/end | CUI renderiza Zona 1 directamente desde datos de IB |
| **Damage & Stats Engine** | DSE → CUI | Resultados de cálculo: daño final, tipo (físico/mágico), elemento, crit flag, miss flag, healing amount. Stats efectivos post-buff | DSE provee datos numéricos; CUI decide la presentación (color, tamaño, efecto) |
| **Unit Data Model** | UDM → CUI | Datos de display: nombre, retrato, sprite, elemento, rareza, lista de habilidades (nombre, icono, descripción, TargetType, costo MP, cooldown, LB info) | CUI lee UDM para Zona 5 y sub-menú de habilidades |
| **Ship Data Model** | SDM → CUI | Datos de display naval: nombre del barco, tipo, stats base/efectivos, crew assignments (rol + unidad), BaseAbilities | CUI lee SDM para Zona 5 naval y panel de crew |
| **Traits/Sinergias** | TS → CUI | Lista de sinergias activas (nombre, icono, count, unidades buffadas, valores de bonus). Eventos de activación/desactivación | CUI renderiza Zona 4 (strip de sinergias) |
| **Enemy System** | ES → CUI | Datos de display enemigos: nombre, tipo, tier (Normal/Elite/Jefe), HP, elemento, nombre de habilidad (para jefes al usarla) | CUI muestra info enemiga al tap/targeting |
| **Stage System** | SS → CUI | Metadata del stage: nombre, background scene, oleadas, condiciones de misión. Datos de recompensas para Victory | CUI muestra Zona 2 (oleadas) y pantalla de Victory |
| **Auto-Battle** | AB ↔ CUI | AB → CUI: señal de modo auto on/off. CUI → AB: input del jugador para toggle. En auto, CUI oculta panel de acciones y muestra indicador "AUTO" | Toggle bidireccional. CUI sigue mostrando Initiative Bar y campo en auto |

## Formulas

**1. HP Bar Color Gradient**

```
Si HPPercent >= 0.5:    BarColor = lerp(Yellow, Green, (HPPercent - 0.5) / 0.5)
Si HPPercent < 0.5:     BarColor = lerp(Red, Yellow, HPPercent / 0.5)
```

| Variable | Valor | Descripción |
|----------|-------|-------------|
| `HPPercent` | 0.0–1.0 | HP actual / HP max |
| `Green` | #4CAF50 | HP > 75% |
| `Yellow` | #FFC107 | HP ~50% |
| `Red` | #F44336 | HP < 25% |

**2. Damage Number Size Scaling**

```
BaseSize = DAMAGE_FONT_BASE
Si isCrit:          FontSize = BaseSize × CRIT_SIZE_MULTIPLIER
Si isDoT:           FontSize = BaseSize × DOT_SIZE_MULTIPLIER
Si isAdvantage:     FontSize = BaseSize × ADVANTAGE_SIZE_MULTIPLIER
Si isDisadvantage:  FontSize = BaseSize × DISADVANTAGE_SIZE_MULTIPLIER
Si else:            FontSize = BaseSize
```

| Variable | Valor | Descripción |
|----------|-------|-------------|
| `DAMAGE_FONT_BASE` | 24sp | Tamaño base de números de daño |
| `CRIT_SIZE_MULTIPLIER` | 1.5 | Crits son 50% más grandes |
| `DOT_SIZE_MULTIPLIER` | 0.75 | DoT ticks son más pequeños |
| `ADVANTAGE_SIZE_MULTIPLIER` | 1.15 | Ligeramente más grande con ventaja |
| `DISADVANTAGE_SIZE_MULTIPLIER` | 0.85 | Ligeramente más pequeño con desventaja |

**3. Damage Number Float Animation**

```
Position.Y(t) = StartY + FLOAT_DISTANCE × ease_out_quad(t / FLOAT_DURATION)
Opacity(t) = Si t < FLOAT_DURATION × 0.7: 1.0
             Si no: lerp(1.0, 0.0, (t - FLOAT_DURATION × 0.7) / (FLOAT_DURATION × 0.3))
```

| Variable | Valor | Descripción |
|----------|-------|-------------|
| `FLOAT_DURATION` | 1.0s | Tiempo total que el número flota |
| `FLOAT_DISTANCE` | 60px | Distancia vertical que recorre |
| `STAGGER_OFFSET` | 15px | Offset horizontal entre números simultáneos |

**4. Timing de UI**

| Evento | Duración | Notas |
|--------|----------|-------|
| Pausa pre-turno enemigo | 0.5s | Para que el jugador vea quién actúa |
| Animación de acción (básica) | 0.5–1.0s | Ataque normal, guardia |
| Animación de acción (habilidad) | 1.0–2.0s | Habilidades con efecto visual |
| Banner "¡LIMIT BREAK!" | 1.0s | No bloquea gameplay |
| Transición de oleada | 1.5–2.0s | Fade + entrada de enemigos |
| Pantalla de PreCombat | 1.5s | Despliegue + activación de sinergias |
| Reorder animation (Initiative Bar) | 0.3s | Iconos se deslizan |
| LB insertion flash | 0.5s | Flash + deslizamiento del icono |

## Edge Cases

1. **Muchos combatientes en la Initiative Bar (>10)**: Los iconos se comprimen proporcionalmente. Nunca scroll — lectura instantánea es prioritaria. Con 11 combatientes (6 aliados + 5 enemigos), iconos bajan de ~40px a ~33px. Mínimo legible: ~28px (13 combatientes). En naval con pocos barcos (2-4), iconos se mantienen en tamaño normal.

2. **Múltiples números de daño simultáneos (AoE)**: Se escalonan horizontalmente con `STAGGER_OFFSET` (15px) y temporalmente (50ms entre cada uno). Si >5 números simultáneos, se agrupan (ej: "3,250 ×5").

3. **Status effect icons overflow (>6 estados)**: Los primeros 4-5 iconos se muestran directamente. Si hay más, badge "+N" con tap para lista completa. Prioridad: CC > DoT > debuffs > buffs.

4. **Turno del amigo (slot 6)**: Se trata como unidad aliada normal en UI. Panel de acciones aparece. Diferencia visual: badge "AMIGO" en retrato de Zona 5.

5. **Tap rápido durante animación enemiga**: Los taps se ignoran durante `EnemyTurn` y `Animating` (excepto pausa). No hay cola de acciones.

6. **Habilidad en cooldown**: Atenuada y no interactiva. Tap no produce feedback. Contador "CD: X" es suficiente indicación.

7. **Abordaje sin crew enemiga viva**: Botón "Abordaje" se atenúa automáticamente. Tap muestra tooltip: "Sin tripulación enemiga para abordar".

8. **Transición de oleada interrumpe overlays**: Tooltips, targeting highlights, sub-menú de habilidades se cierran automáticamente. UI vuelve a estado limpio.

9. **Derrota durante LB pendiente**: Si unidad muere por DoT post-acción con LB pendiente, LB se cancela (lógica CT/CN). UI transiciona directo a `Defeat` sin banner LB.

10. **Nombre de habilidad de jefe muy largo**: Trunca a `MAX_BOSS_ABILITY_NAME_LENGTH` con "...". Fuente condensada para nombres largos.

11. **Auto-Battle mid-combat**: Panel se oculta (slide down), indicador "AUTO" aparece en esquina. Tap en "AUTO" desactiva — panel reaparece en siguiente turno aliado.

12. **Stats de barco cambian mid-turno (crew muere por DoT)**: Zona 5 se actualiza inmediatamente con flash rojo en stats que cambian. Si crew muerto aportaba habilidad en sub-menú abierto, sub-menú se refresca.

13. **Victoria con 0 recompensas**: Pantalla muestra resumen sin animación de recompensas. Texto indica que el stage ya fue completado.

14. **Sinergias se desactivan mid-batalla (Capitán muere)**: Zona 4 se actualiza. Iconos de sinergias perdidas desaparecen con fade. Flash visual sobre unidades que pierden el buff.

## Dependencies

### Dependencias Upstream (CUI depende de)

| Sistema | Tipo | Interfaz | GDD |
|---------|------|----------|-----|
| **Combate Terrestre** | Hard | Eventos de batalla (turno, acción resuelta, oleada, fin), estado de combatientes | ✅ Approved |
| **Combate Naval** | Hard | Mismos eventos con datos navales (HHP, MP, crew, abordaje, reparar) | ✅ Approved |
| **Initiative Bar** | Hard | Orden de turno, reorder events, LB insertion, round start/end | ✅ Approved |
| **Damage & Stats Engine** | Soft | Resultados numéricos (daño, tipo, elemento, crit, miss, heal). CUI recibe datos vía CT/CN | ✅ Approved |
| **Unit Data Model** | Soft | Datos de display: nombre, retrato, sprite, elemento, habilidades | ✅ Approved |
| **Ship Data Model** | Soft | Datos de display naval: nombre barco, stats, crew, BaseAbilities | ✅ Approved |
| **Traits/Sinergias** | Soft | Sinergias activas para Zona 4 (strip). Sin TS, la strip queda vacía | ✅ Approved |
| **Enemy System** | Soft | Datos de display enemigos (nombre, tier, HP, elemento) | ✅ Approved |
| **Stage System** | Soft | Metadata del stage (nombre, background, oleadas, misiones, recompensas) | ✅ Approved |

### Dependencias Downstream (dependen de CUI)

| Sistema | Tipo | Qué necesita de CUI | GDD |
|---------|------|---------------------|-----|
| **Auto-Battle** | Soft | Toggle on/off, CUI muestra/oculta panel según estado de AB | ⬜ Not Started |

### Cross-System Updates Necesarios

- ~~**UDM, SDM, DSE, CT, CN, TS, ES**: Renombrar MAG → MST (Mística) en todos los GDDs. En naval, separar MST (stat) de MP (recurso). Barcos necesitan campo `MP` base en SDM~~ ✅ DONE
- **Combate Terrestre**: Verificar que CT emita eventos (`OnActionResolved`, `OnTurnStart`, `OnWaveComplete`, `OnBattleEnd`) con la estructura de datos que CUI necesita
- **Combate Naval**: Misma verificación + datos adicionales (crew HP, stats recalculados post-crew-death)

## Tuning Knobs

| Knob | Valor Actual | Rango Seguro | Afecta a | Notas |
|------|-------------|-------------|----------|-------|
| `DAMAGE_FONT_BASE` | 24sp | 18–32sp | Legibilidad de números de daño. Muy bajo: ilegible en mobile. Muy alto: ocupa demasiado espacio | Testear en dispositivos reales |
| `CRIT_SIZE_MULTIPLIER` | 1.5 | 1.2–2.0 | Impacto visual de crits. Muy bajo: no se nota. Muy alto: tapa el sprite | Borde dorado + estrella ya diferencia; tamaño es refuerzo |
| `DOT_SIZE_MULTIPLIER` | 0.75 | 0.5–0.9 | Visibilidad de DoT ticks. Muy bajo: invisible. Muy alto: compite con daño directo | Info secundaria — verse pero no distraer |
| `FLOAT_DURATION` | 1.0s | 0.5–2.0s | Tiempo que los números flotan. Muy corto: no se leen. Muy largo: overlap con siguiente | Interactúa con COMBAT_SPEED_MULTIPLIER |
| `FLOAT_DISTANCE` | 60px | 30–100px | Distancia de flotado. Muy corta: pegados al sprite. Muy larga: salen de pantalla | Portrait = espacio vertical limitado |
| `STAGGER_OFFSET` | 15px | 5–25px | Separación entre números simultáneos. Muy bajo: overlap. Muy alto: dispersión excesiva | Solo aplica con >1 número simultáneo |
| `ENEMY_PAUSE_DURATION` | 0.5s | 0.2–1.0s | Pausa antes de acción enemiga. Muy corta: no se ve quién actúa. Muy larga: combate lento | Afectado por COMBAT_SPEED_MULTIPLIER |
| `WAVE_TRANSITION_DURATION` | 1.75s | 1.0–3.0s | Duración de transición de oleada. Muy corta: desorientante. Muy larga: rompe ritmo | Incluye fade + entrada de enemigos |
| `LB_BANNER_DURATION` | 1.0s | 0.5–2.0s | Duración del banner "¡LIMIT BREAK!". Muy corto: no se lee. Muy largo: interrumpe | Es overlay, no bloquea |
| `MAX_VISIBLE_STATUS_ICONS` | 5 | 3–8 | Iconos de estado antes del "+N". Muy bajo: info oculta. Muy alto: overflow visual | Portrait = espacio horizontal limitado |
| `MAX_BOSS_ABILITY_NAME_LENGTH` | 25 | 15–40 | Caracteres del flash de habilidad de jefe | Fuente condensada mitiga |
| `INITIATIVE_ICON_SIZE` | 40px | 28–48px | Tamaño base de iconos en Initiative Bar. Muy bajo: irreconocible. Muy alto: no caben | Se comprime automáticamente si hay muchos combatientes |
| `COMBAT_SPEED_MULTIPLIER` | 1.0 | 1.0–3.0 | Velocidad de combate (opción del jugador). Afecta todas las duraciones de animación excepto UI de input | Pillar 4. Opciones: 1×, 2×, 3×. Persiste entre batallas |

### Knob Interactions

| Knob A | Knob B | Interacción |
|--------|--------|-------------|
| FLOAT_DURATION | COMBAT_SPEED_MULTIPLIER | A velocidades altas, números deben flotar más rápido o se acumulan |
| ENEMY_PAUSE_DURATION | COMBAT_SPEED_MULTIPLIER | A 3×, la pausa de 0.5s → ~0.17s — puede ser demasiado rápida |
| DAMAGE_FONT_BASE | INITIATIVE_ICON_SIZE | Ambos compiten por espacio visual en portrait |

## Visual/Audio Requirements

**Visual**
- **Estética general**: Pixel art chibi (gameplay) con identidad pirata. La UI usa texturas de madera, cuerdas, pergamino y metal oxidado. Bordes con acabado de plancha naval
- **Panel de acciones**: Estilo timón/carta de navegación. Botones con iconos expresivos (no texto plano). Animación de selección: el botón se hunde ligeramente y emite chispas/brillo al tap
- **Sub-menú de habilidades**: Habilidades presentadas como cartas de pergamino con borde de color elemental. Icono de habilidad con arte propio. Efecto de "voltear carta" al abrir el sub-menú
- **Targeting overlay**: Targets válidos con borde brillante pulsante. Flecha ↑ (verde, ventaja) o ↓ (roja, desventaja) sobre cada target. AoE: highlight simultáneo con efecto de onda
- **Pantalla de victoria**: Fondo de atardecer dorado. Cofre del tesoro se abre revelando recompensas una a una con partículas. Misiones con checkmarks animados. Naval: barco se mece con crew celebrando
- **Pantalla de derrota**: Fondo oscuro/tormentoso. Terrestre: unidades caídas. Naval: hundimiento progresivo (barco se inclina, agua entra, se hunde). Botones Reintentar/Salir estilizados
- **Transición terrestre → naval**: Si se encadenan combates futuramente, animación de "zarpar". Para la demo, los modos son stages separados

**Audio**
- **Música de combate**: Tema pirata enérgico con percusión. Loop principal + variante tensa (jefe/baja HP) + fanfarra de victoria + tema sombrío de derrota
- **Naval vs terrestre**: Naval usa más percusión de tambores de guerra + ambiente oceánico. Terrestre usa más instrumentos de cuerda/flauta
- **SFX de UI**:
  - Tap en botón de acción: click satisfactorio estilo madera/metal
  - Abrir sub-menú habilidades: pergamino desenrollándose
  - Seleccionar target: ping sutil de confirmación
  - Cancelar: click suave reverso
  - Botón AUTO on: click mecánico + engranaje
  - Cambio de velocidad: tick de reloj acelerado
  - Pausa: silencio parcial (música baja, ambiente persiste)
- **SFX de combate**: Definidos en DSE, CT y CN — el CUI los reproduce según eventos recibidos

## UI Requirements

- **Resolución de referencia**: 1080×1920 (portrait Full HD). Scaling proporcional para otras resoluciones manteniendo aspect ratio
- **Safe area**: Respetar notch y bordes redondeados. Zonas 1 y 6 dentro del safe area
- **Touch targets**: Mínimo 44×44px para botones interactivos (estándar HIG / Material Design). Botones del panel de acciones más grandes (~80×60px)
- **Text scaling**: Números de daño y nombres escalan con resolución. Fuente legible a 24sp base en 1080p
- **Loading screen**: Nombre del stage, tipo (terrestre/naval), tip de gameplay aleatorio. Spinner pirata (timón girando)
- **Tooltip system**: Long-press (500ms) muestra tooltip contextual. Se cierra al levantar dedo o tap fuera. No bloquea gameplay
- **Color-blind considerations**: Indicadores no dependen solo del color — usan forma/icono como refuerzo. Ventaja elemental: flecha + color. Crítico: borde + estrella + tamaño. Aliado vs enemigo: borde + posición
- **Landscape future-proofing**: Componentes de UI como módulos reposicionables. Layout portrait define posiciones actuales, pero módulos no asumen coordenadas hardcodeadas
- **Performance**: UI dentro del frame budget. Números flotantes usan object pooling. Iconos de Initiative Bar se reciclan entre rondas

## Acceptance Criteria

**Layout y Componentes**
1. La pantalla de combate se renderiza en portrait con las 6 zonas visibles y correctamente posicionadas
2. La Initiative Bar muestra todos los combatientes ordenados por SPD con bordes azul (aliados) y rojo (enemigos)
3. Los iconos de Initiative Bar se comprimen proporcionalmente cuando hay >10 combatientes (sin scroll)
4. HP bars muestran gradiente de color correcto: verde (>50%), amarillo (~50%), rojo (<50%)
5. MP bar visible debajo de HP en ambos modos (azul)
6. Status icons muestran contador de duración y respetan prioridad de display (CC > DoT > debuffs > buffs)
7. Strip de sinergias muestra sinergias activas con count. Tap muestra detalle. Se oculta si no hay sinergias

**Panel de Acciones**
8. En terrestre: panel muestra 4 botones (Ataque Normal, Habilidades, Guardia, Pasar Turno)
9. En naval: panel muestra 6 botones (Cañonazo, Habilidad Naval, Maniobra Evasiva, Abordaje, Reparar, Pasar Turno)
10. Botones no disponibles se atenúan con razón visible al tap
11. Sub-menú de habilidades muestra: icono, nombre, elemento, costo MP, TargetType, estado de cooldown
12. Habilidades con LB tienen borde/brillo distinguible
13. Habilidades en cooldown muestran contador "CD: X" y no son interactivas

**Interaction Flows**
14. Cualquier acción se completa en máximo 3 taps (Acción → Target → Confirmar)
15. Acciones auto-target (Guardia, Pasar, Reparar, Maniobra) se completan en 1 tap
16. Targeting resalta targets válidos con info elemental (ventaja/desventaja)
17. Abordaje en naval permite seleccionar crew members individuales con info visible (nombre, rol, HP, DEF)
18. Cancelar targeting vuelve al estado anterior (panel o sub-menú de habilidades)
19. Tap en área vacía durante targeting cancela la selección

**Feedback Visual**
20. Números de daño flotan con color correcto por elemento (según tabla DSE)
21. Crits muestran número 1.5x grande + borde dorado + estrella
22. DoT ticks muestran número 0.75x con icono del efecto
23. Animación LB: flash en Initiative Bar + banner "¡LIMIT BREAK!" ~1s
24. Transición de oleada: fade/barrido + "Oleada X/Y" actualizado con animación
25. Muerte de crew enemigo en naval: stats del barco se recalculan visualmente (flash rojo)

**Convenience Controls**
26. Botón AUTO toggle funciona: panel de acciones se oculta/muestra correctamente
27. Botón de velocidad cicla 1×/2×/3× y afecta todas las duraciones de animación
28. La velocidad seleccionada persiste entre batallas
29. Botón de pausa abre menú con Continuar, Rendirse, Configuración

**Pantallas de Fin**
30. Victoria muestra resumen de batalla + animación de recompensas + misiones completadas
31. Victoria naval incluye crew sobrevivientes y HHP restante
32. Derrota muestra opciones Reintentar y Salir
33. Derrota naval incluye animación de hundimiento

**Adaptabilidad**
34. El framework base (Zonas 1-4) es idéntico entre terrestre y naval
35. Solo Zonas 5-6 y paneles específicos cambian entre modos
36. La UI no se rompe con combinaciones extremas (11 combatientes + 6 estados + sinergias activas)

## Open Questions

1. ~~**Cross-system rename MAG → MST**: Propagado a los 7 GDDs afectados en sesión dedicada (2026-03-27).~~ ✅ DONE
2. **MP naval — valores base para barcos**: Los barcos necesitan campo MP en SDM. ¿Cuánto MP base? ¿Escala con upgrades? **Owner**: systems-designer. **Target**: al actualizar SDM
3. **Daño estimado en tooltip de habilidades**: ¿Cómo se calcula? ¿Usa el target seleccionado, promedio, o target con más HP? **Owner**: game-designer. **Target**: durante implementación
4. **Velocidad 3× y legibilidad**: ¿Los números y animaciones siguen siendo legibles a 3×? Necesita testing en dispositivo real. **Owner**: QA. **Target**: durante playtesting
