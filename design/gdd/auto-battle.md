# Auto-Battle

> **Status**: Designed (Review: Approved)
> **Author**: user + agents
> **Last Updated**: 2026-04-01
> **Implements Pillar**: Pillar 4 (Respeto al Tiempo del Jugador)

## Overview

El Auto-Battle es el sistema que automatiza las decisiones del jugador durante el combate, tanto terrestre como naval. Cuando está activo, una IA aliada reemplaza la selección de acciones y targeting en cada turno — el combate se ejecuta con las mismas reglas, fórmulas y estados, solo cambia quién decide. El jugador puede activar o desactivar el auto-battle en cualquier momento de cualquier combate (toggle libre). Adicionalmente, en stages ya completados (first-clear done), el sistema ofrece un repeat loop automático: el jugador configura cuántas repeticiones quiere, y el sistema encadena combate → rewards → re-entry sin intervención, gastando energía por cada run. Complementando al auto, un multiplicador de velocidad de combate (x1, x2, x4) acelera las animaciones y la resolución de turnos, disponible tanto en modo manual como auto. Sin este sistema, el jugador se vería obligado a repetir manualmente contenido trivial para farmear recursos, violando el Pilar 4 (Respeto al Tiempo del Jugador). El sistema falla si la IA es tan buena que el contenido difícil se resuelve en auto (elimina el desafío), o si es tan mala que el jugador no confía en ella ni para contenido fácil.

## Player Fantasy

**Fantasía**: Eres un capitán pirata que ha entrenado bien a su tripulación. Cuando el enemigo no es una amenaza real, puedes dar la orden "adelante" y tu crew resuelve el combate por su cuenta — atacan, usan sus habilidades, protegen el barco. Pero cuando la situación se complica, la tripulación sin capitán toma decisiones mediocres: no explotan debilidades elementales, no coordinan Limit Breaks, no priorizan targets. Ahí es donde tú retomas el timón.

**Emoción objetivo**: Confianza delegada. El auto-battle es la recompensa por haber construido un equipo fuerte — cuando funciona, el jugador siente que su inversión en progresión valió la pena. Cuando falla en contenido difícil, el jugador siente que *él* es lo que marca la diferencia, no solo sus stats.

**Referencia**: Epic Seven (auto con targeting configurable pero IA limitada que no optimiza sinergias) y Summoners War (auto + repeat loop para farming eficiente). Nos diferenciamos de FFBE (cuyo auto es demasiado básico — solo repite última acción).

**Tipo de sistema**: Infraestructura de confort. El jugador no "juega" el auto-battle — lo activa y observa. La experiencia es de relajación y eficiencia, no de engagement activo. El contenido difícil es el que exige volver al modo manual.

**El sistema falla si**: El jugador puede dejar en auto todo el contenido del juego (falta dificultad), o si la IA es tan torpe que pierde en stages triviales (falta competencia básica).

## Detailed Design

### Core Rules

**1. Toggle Auto-Battle**

- El jugador puede activar/desactivar el auto-battle en cualquier momento durante un combate (terrestre o naval) pulsando el botón Auto
- Al activar **durante un turno aliado sin acción seleccionada**, la IA toma el control inmediatamente y elige la acción de ese turno. Al desactivar durante un turno aliado, el jugador retoma el control inmediatamente si aún no se ha ejecutado acción
- Si el jugador ya confirmó una acción y esta se está resolviendo (animación en curso), el toggle toma efecto al finalizar la resolución
- **Tuning note**: Si el toggle instantáneo causa inputs accidentales o confusión (el jugador quería desactivar pero la IA ya eligió), se puede cambiar a "toma efecto en el siguiente turno aliado". Marcar como knob configurable: `AUTO_TOGGLE_TIMING` (instant / next-turn)
- El estado del toggle (on/off) persiste entre oleadas de la misma batalla
- El estado del toggle se resetea a OFF al iniciar una nueva batalla (no se "recuerda" entre batallas)

**2. IA Terrestre — Comportamiento**

La IA aliada reemplaza la selección de acción y targeting en cada turno de una unidad aliada. Sigue esta prioridad:

| Prioridad | Condición | Acción |
|-----------|-----------|--------|
| 1 | Hay habilidad de curación disponible (TargetType = ally-single o ally-aoe) Y algún aliado tiene HP < 30% | Usar habilidad de curación en el aliado con menor % HP |
| 2 | Hay habilidades ofensivas disponibles (no en cooldown) | Usar la habilidad con mayor AbilityPower disponible |
| 3 | No hay habilidades disponibles | Ataque Normal |

**Targeting terrestre:**
- Ataque single-target: Selecciona al enemigo con menor HP absoluto (rematar)
- Ataque AoE: Se ejecuta automáticamente sobre todos los enemigos
- Curación single-target: Selecciona al aliado con menor % HP
- Curación AoE: Se ejecuta automáticamente sobre todos los aliados

**Lo que la IA terrestre NO hace (limitaciones deliberadas):**
- NO usa Guardia nunca — la IA es puramente ofensiva/reactiva
- NO usa Pasar Turno nunca (incluso bajo Quemadura — acepta el daño)
- NO considera ventajas/desventajas elementales al elegir target
- NO intenta activar Limit Breaks intencionalmente (si se activan, es por coincidencia)
- NO prioriza targets por tipo (no focaliza healers o jefes)
- NO coordina entre unidades (cada turno se decide en aislamiento)

**3. IA Naval — Comportamiento**

La IA naval reemplaza las decisiones del barco aliado. Sigue esta prioridad:

| Prioridad | Condición | Acción |
|-----------|-----------|--------|
| 1 | HHP del barco < 30% Y tiene MP suficiente para Reparar | Reparar |
| 2 | Hay habilidades navales ofensivas disponibles (con MP) | Usar la habilidad naval con mayor AbilityPower |
| 3 | No hay habilidades disponibles o sin MP | Cañonazo |

**Targeting naval:**
- Cañonazo / habilidad single-target: Selecciona al enemigo (barco/criatura) con menor HHP absoluto
- Habilidad AoE: Se ejecuta automáticamente sobre todos los enemigos
- Reparar: Self (automático)

**Lo que la IA naval NO hace (limitaciones deliberadas):**
- NO usa Abordaje nunca — la decisión táctica de atacar crew es exclusiva del capitán (jugador)
- NO usa Maniobra Evasiva nunca — la IA no anticipa daño entrante
- NO gestiona MP estratégicamente (gasta en habilidades hasta agotar, luego Cañonazo)
- NO prioriza targets por sinergias o capitán enemigo
- NO considera elementos al elegir target

**4. Repeat Loop**

Disponible solo en stages con first-clear completado. Permite encadenar múltiples runs del mismo stage sin intervención:

1. El jugador selecciona un stage ya completado y activa Repeat
2. Elige cantidad de repeticiones: x1, x5, x10, o infinito (hasta agotar energía)
3. El sistema verifica energía suficiente para al menos 1 run
4. Se inicia la batalla con Auto-Battle ON automáticamente
5. Al ganar: se otorgan rewards, se descuenta energía, se inicia la siguiente run
6. El jugador puede cancelar el loop en cualquier momento (botón Stop visible durante el combate)
7. Al cancelar, se conservan las rewards de todas las runs completadas hasta ese punto

**Condiciones de parada del Repeat Loop:**
- Repeticiones completadas (alcanzó el número configurado)
- Derrota: el loop se detiene, se muestra pantalla de derrota normal. Las rewards de runs anteriores ya fueron otorgadas
- Energía insuficiente para la siguiente run
- Cancelación manual del jugador
- Inventario lleno (si se implementa límite de inventario en el futuro)

**5. Multiplicador de Velocidad**

Acelera las animaciones y la resolución visual del combate. No afecta mecánicas — solo presentación.

| Velocidad | Disponibilidad | Efecto |
|-----------|---------------|--------|
| x1 | Siempre | Velocidad base de animaciones y resolución |
| x2 | Siempre | Animaciones y resolución al doble de velocidad |
| x4 | Tras completar Capítulo 1 | Animaciones y resolución a cuádruple velocidad |

- El multiplicador aplica tanto en modo manual como en auto-battle
- El multiplicador persiste entre batallas (se recuerda la última selección del jugador)
- El multiplicador afecta: animaciones de ataque, números de daño flotantes, transiciones de oleada, resolución de estados (DoTs, buffs), banners BetweenRuns en repeat loop. NO afecta: pantalla de rewards en batalla normal (el jugador necesita verla), tiempo de selección del jugador en modo manual, resumen final de repeat loop
- En repeat loop, se recomienda x4 para farming eficiente, pero el jugador elige

### States and Transitions

El Auto-Battle no tiene una máquina de estados propia compleja — se superpone sobre los estados de combate existentes (CT/CN). Pero el Repeat Loop sí tiene estados:

**Estados del Auto-Battle Toggle**

| Estado | Descripción | Transiciones |
|--------|-------------|-------------|
| `Off` | Modo manual. El jugador controla todas las decisiones | → `On` (jugador pulsa botón Auto) |
| `On` | Modo automático. La IA decide en cada turno aliado | → `Off` (jugador pulsa botón Auto), → `Off` (batalla termina — reset) |

**Estados del Repeat Loop**

| Estado | Descripción | Transiciones |
|--------|-------------|-------------|
| `Inactive` | No hay repeat loop activo | → `Configuring` (jugador selecciona Repeat en stage completado) |
| `Configuring` | Jugador elige cantidad de repeticiones (x1/x5/x10/∞) | → `Running` (confirma), → `Inactive` (cancela) |
| `Running` | Loop en ejecución. Ciclo: combate → rewards → re-entry | → `BetweenRuns` (victoria, preparando siguiente run), → `Stopped` (derrota/cancelación/sin energía) |
| `BetweenRuns` | Procesando rewards de la run completada, verificando energía para la siguiente | → `Running` (energía suficiente, siguiente run), → `Stopped` (sin energía / repeticiones completadas) |
| `Stopped` | Loop detenido. Muestra resumen de runs completadas | → `Inactive` (jugador confirma) |

**Estados del Multiplicador de Velocidad**

| Estado | Descripción | Transiciones |
|--------|-------------|-------------|
| `x1` | Velocidad normal | → `x2` (jugador pulsa botón velocidad) |
| `x2` | Doble velocidad | → `x4` (si desbloqueado) o → `x1` (si x4 no desbloqueado) |
| `x4` | Cuádruple velocidad. Requiere Capítulo 1 completado | → `x1` (jugador pulsa botón velocidad — ciclo) |

El botón de velocidad cicla entre los valores disponibles: x1 → x2 → x4 → x1 (o x1 → x2 → x1 si x4 no desbloqueado).

### Interactions with Other Systems

| Sistema | Dirección | Datos que fluyen | Interfaz |
|---------|-----------|-----------------|----------|
| **Combate Terrestre** | CT ↔ AB | CT notifica a AB: "es turno de unidad aliada, aquí están las acciones disponibles, habilidades con cooldowns, HP de todos los combatientes". AB devuelve: acción seleccionada + target. CT la ejecuta como si fuera input del jugador | AB reemplaza la "Fase de acción" del jugador. CT no cambia — recibe la misma interfaz de acción+target venga del jugador o de la IA |
| **Combate Naval** | CN ↔ AB | CN notifica a AB: "es turno del barco aliado, aquí están las 5 acciones, MP actual, HHP actual, habilidades disponibles, enemigos con HHP". AB devuelve: acción + target. CN la ejecuta | Misma interfaz que CT pero con acciones navales. AB nunca devuelve Abordaje ni Maniobra |
| **Stage System** | SS → AB | Provee el flag `firstClearDone` del stage para determinar si Repeat Loop está disponible | AB consulta el estado de completado del stage antes de ofrecer Repeat |
| **Rewards System** | RS ↔ AB | En repeat loop, AB solicita rewards al finalizar cada run. RS calcula y otorga normalmente | Sin cambios en RS — cada run en el loop es una ejecución normal del flujo rewards |
| **Currency System** | CS ↔ AB | AB consulta energía disponible antes de cada run del repeat loop. CS descuenta energía por run | AB verifica `energía actual >= EnergyCost` del stage antes de iniciar cada repetición |
| **Combat UI** | CUI ↔ AB | CUI muestra botón Auto (toggle on/off), botón Speed (x1/x2/x4), botón Repeat (en stage select). Durante auto, CUI muestra indicador visual de "AUTO" activo. Durante repeat, muestra contador "Run X/Y" y botón Stop | AB emite eventos: auto_toggled, speed_changed, repeat_started, repeat_run_complete, repeat_stopped |
| **Save/Load System** | S/L ↔ AB | Persiste: velocidad seleccionada (x1/x2/x4), desbloqueo de x4. NO persiste: estado de auto-toggle (siempre OFF al iniciar batalla) | Datos mínimos — solo preferencia de velocidad |

## Formulas

**1. Umbral de curación de la IA terrestre**

```
ShouldHeal = any ally where (CurrentHP / MaxHP) < HEAL_THRESHOLD
HealTarget = ally with lowest (CurrentHP / MaxHP)
```

| Variable | Definición | Valor |
|----------|-----------|-------|
| `HEAL_THRESHOLD` | % de HP por debajo del cual la IA prioriza curar | 0.30 (30%) |
| `CurrentHP` | HP actual del aliado | Leído del runtime state |
| `MaxHP` | HP máximo del aliado | Leído del UDM + buffs |

**2. Umbral de reparación de la IA naval**

```
ShouldRepair = (CurrentHHP / MaxHHP) < REPAIR_THRESHOLD AND CurrentMP >= REPAIR_MP_COST
RepairTarget = Self (siempre)
```

| Variable | Definición | Valor |
|----------|-----------|-------|
| `REPAIR_THRESHOLD` | % de HHP por debajo del cual la IA prioriza reparar | 0.30 (30%) |
| `CurrentHHP` | HHP actual del barco | Leído del runtime state |
| `MaxHHP` | HHP máximo del barco | Calculado por SDM |
| `REPAIR_MP_COST` | Costo de Reparar | 20 (definido en Combate Naval GDD) |

**3. Selección de habilidad ofensiva**

```
SelectedAbility = ability with max(AbilityPower) where cooldown == 0 AND (mode == naval ? currentMP >= ability.MPCost : true)
If no ability available: fallback to Normal Attack (terrestre) or Cañonazo (naval)
```

**4. Selección de target ofensivo**

```
Terrestre: Target = enemy with min(CurrentHP) where CurrentHP > 0
Naval:     Target = enemy with min(CurrentHHP) where CurrentHHP > 0
```

No hay fórmulas de daño propias — todas las acciones se resuelven a través del DSE con las mismas fórmulas que el modo manual.

## Edge Cases

1. **IA con todas las habilidades en cooldown**: Usa Ataque Normal (terrestre) o Cañonazo (naval). Nunca se queda sin acción.

2. **IA terrestre con habilidad de curación pero el único aliado bajo 30% HP es ella misma**: Si la habilidad es self-cast o ally-single, se cura a sí misma. Si la habilidad es ally-single y no puede targetear self, usa habilidad ofensiva en su lugar.

3. **IA terrestre con múltiples habilidades del mismo AbilityPower**: Selecciona la primera en la lista de habilidades de la unidad (orden del UDM). Determinístico, no aleatorio.

4. **IA naval sin MP y HHP < 30%**: No puede Reparar. Usa Cañonazo (fallback). La IA no tiene forma de recuperar la situación — esta es una de las razones por las que el auto falla en contenido difícil.

5. **Toggle auto durante animación de Limit Break**: El toggle toma efecto tras resolver el LB completo (turno original + turno extra). No se puede interrumpir un LB a mitad.

6. **Toggle auto durante turno enemigo**: El toggle se registra pero no toma efecto hasta el siguiente turno aliado. No afecta resolución de turnos enemigos.

7. **Repeat loop con derrota en la primera run**: El loop se detiene, se muestra pantalla de derrota normal. No hay rewards previas que conservar. El jugador no pierde nada extra (la energía de esa run ya se descontó al entrar).

8. **Repeat loop con energía exacta para 1 run pero configurado en x5**: Se ejecuta 1 run. Al terminar, el loop se detiene por energía insuficiente. Muestra resumen: "1/5 runs completadas".

9. **Repeat loop infinito con 0 energía**: No se puede iniciar. El sistema verifica energía >= EnergyCost antes de la primera run.

10. **Jugador sube de velocidad a x4 durante un turno en resolución**: El cambio aplica inmediatamente a las animaciones restantes del turno actual. No espera al siguiente turno.

11. **IA terrestre con unidad bajo Quemadura**: La IA no usa Pasar Turno para evitar Quemadura. Siempre actúa (ataca o cura), aceptando el daño de Quemadura. Esta es una limitación deliberada — un jugador manual pasaría turno para evitar el daño.

12. **IA terrestre con unidad bajo CC (Stun/Freeze/Sleep)**: El turno se salta automáticamente (regla de CT). La IA no tiene que decidir nada — el CC lo resuelve CT antes de llegar a la fase de acción.

13. **IA naval con Silencio activo en el barco**: No puede usar Habilidades. La prioridad salta a Cañonazo. Reparar sigue disponible (es acción base, no habilidad) si HHP < 30%.

14. **IA naval con Ceguera activa**: La IA no lo sabe — sigue eligiendo Cañonazo/habilidades físicas normalmente. El 50% de miss se resuelve en el DSE. La IA no compensa cambiando a habilidades mágicas.

15. **Repeat loop + jugador desactiva auto mid-run**: El jugador retoma control manual. El repeat loop sigue activo — al ganar esta run, se inicia la siguiente run con auto ON de nuevo. Si el jugador quiere control manual permanente, debe cancelar el repeat loop.

16. **Cambio de velocidad durante repeat loop entre runs (pantalla BetweenRuns)**: Permitido. El nuevo multiplicador aplica a la siguiente run.

## Dependencies

### Dependencias Upstream (AB depende de)

| Sistema | Tipo | Interfaz | GDD |
|---------|------|----------|-----|
| **Combate Terrestre** | Hard | Provee la interfaz de acción+target por turno aliado. AB necesita: lista de acciones disponibles, habilidades con cooldowns, HP de todos los combatientes, estados activos | ✅ Approved |
| **Combate Naval** | Hard | Provee la interfaz de acción+target por turno del barco. AB necesita: 5 acciones disponibles, MP actual, HHP actual, habilidades con costos, enemigos con HHP | ✅ Approved |
| **Stage System** | Soft | Provee flag `firstClearDone` para habilitar Repeat Loop. AB funciona sin repeat (solo toggle auto), pero repeat necesita este dato | ✅ Approved |
| **Currency System** | Soft | Provee energía actual para validar repeat loop. AB funciona sin repeat, pero repeat necesita verificar energía | ✅ Approved |
| **Rewards System** | Soft | Otorga rewards al final de cada run en repeat loop. Sin cambios en RS — AB solo lo invoca | ✅ Designed |
| **Save/Load System** | Soft | Persiste preferencia de velocidad (x1/x2/x4) y desbloqueo de x4 | ✅ Designed |

### Dependencias Downstream (dependen de AB)

| Sistema | Tipo | Qué necesita de AB | GDD |
|---------|------|---------------------|-----|
| **Combat UI** | Hard | Eventos de AB para renderizar: botón Auto, indicador AUTO activo, botón Speed, contador repeat, botón Stop | ✅ Approved |

### Cross-System Updates Necesarios

- ~~**Combate Terrestre**: Actualizar sección "Interactions"~~ ✅ DONE (actualizado en esta sesión)
- ~~**Combate Naval**: Actualizar sección "Interactions"~~ ✅ DONE (actualizado en esta sesión)
- **Stage System**: AB usa `clearedBattles: Set<BattleId>` del Save/Load System para determinar first-clear. No requiere cambio en Stage System
- **Save/Load System**: Actualizar `autoBattleSpeed` para incluir x4 y flag `speedX4Unlocked: bool` (o derivar de progreso de Capítulo 1)
- **Combat UI**: Necesitará sección de UI para los controles de AB (botón Auto, Speed, Repeat)

## Tuning Knobs

| Knob | Valor Actual | Rango Seguro | Afecta a | Notas |
|------|-------------|-------------|----------|-------|
| `HEAL_THRESHOLD` | 0.30 | 0.15–0.50 | Cuándo la IA prioriza curar. Muy bajo: aliados mueren antes de que la IA cure. Muy alto: la IA cura constantemente y pierde DPS | Interactúa con la dificultad del contenido — en stages fáciles no importa, en difíciles es la diferencia entre ganar y perder |
| `REPAIR_THRESHOLD` | 0.30 | 0.15–0.50 | Cuándo la IA naval prioriza Reparar. Misma lógica que HEAL_THRESHOLD pero para HHP del barco | Interactúa con REPAIR_MP_COST — si el umbral es alto y el costo es alto, la IA gasta todo el MP en reparar |
| `AUTO_TOGGLE_TIMING` | instant | instant / next-turn | Si el toggle toma efecto inmediato o en el siguiente turno aliado. Instant = más responsive pero riesgo de inputs accidentales. Next-turn = más predecible pero menos control | Probar con instant primero, cambiar a next-turn si hay problemas de UX |
| `SPEED_X4_UNLOCK` | Capítulo 1 completado | Cap 1 / Cap 2 / Siempre | Cuándo se desbloquea x4. Más temprano = QoL desde el inicio. Más tarde = recompensa de progresión | Si los jugadores se quejan de no tener x4, bajar el requisito |
| `REPEAT_OPTIONS` | x1, x5, x10, ∞ | Configurable | Opciones de repetición disponibles. Se pueden ajustar si los jugadores piden otros valores (x3, x20) | Infinito debe mantenerse — es el modo farming principal |
| `AI_ABILITY_SELECTION` | max AbilityPower | max AP / random / rotate | Cómo la IA elige habilidades. Max AP es simple y efectivo. Random añade variedad pero puede ser subóptimo. Rotate usa habilidades en orden cíclico | Max AP es el default. Cambiar solo si los jugadores reportan que la IA es demasiado predecible o ignora habilidades útiles |

### Knob Interactions

| Knob A | Knob B | Interacción |
|--------|--------|-------------|
| HEAL_THRESHOLD | Dificultad del stage | En stages fáciles, el umbral no importa (nadie baja de 30%). En stages difíciles, un umbral muy bajo = IA no cura a tiempo → derrota. Esto es deliberado: la IA debe fallar en contenido difícil |
| REPAIR_THRESHOLD | REPAIR_MP_COST (de CN) | Si el umbral es alto (50%) y el costo es alto (50 MP), la IA gasta MP en reparar y no puede usar habilidades ofensivas. Se vuelve un loop defensivo ineficiente |
| AI_ABILITY_SELECTION | Balance de habilidades | Si todas las habilidades tienen AP similar, el knob no importa. Si hay una habilidad con AP mucho mayor, max AP la spammea exclusivamente |

## Visual/Audio Requirements

**Visual**

- **Indicador AUTO**: Texto o badge "AUTO" visible en la esquina superior de la pantalla de combate cuando el auto-battle está activo. Color dorado/ámbar para distinguirlo del resto de la UI. Animación sutil de pulso para indicar que está ON
- **Indicador de velocidad**: Badge compacto junto al botón Speed mostrando "x1", "x2" o "x4". El x4 tiene un brillo o color diferente (premium feel) para reforzar que es un desbloqueo
- **Botón Auto**: Icono de timón (temática pirata — el capitán delega el timón). ON = timón girando (animado). OFF = timón estático
- **Botón Speed**: Icono de velas o viento. Más velas/viento = más velocidad
- **Repeat Loop — contador**: Overlay compacto "Run 3/10" visible durante combate. Al completar una run, breve flash de transición (fade rápido) antes de iniciar la siguiente
- **Repeat Loop — pantalla BetweenRuns**: Resumen mínimo de rewards de la run (versión comprimida de la pantalla de rewards normal — no full screen, solo un banner con items obtenidos que se auto-cierra en 1-2 segundos a velocidad x1)
- **Repeat Loop — resumen final**: Al detenerse el loop, pantalla de resumen con: total de runs completadas, rewards acumuladas totales, energía gastada, motivo de parada (completado / derrota / sin energía / cancelado)
- **x4 desbloqueo**: Notificación sutil al completar Capítulo 1: "x4 velocidad desbloqueada" con icono de velas

**Audio**

- **Toggle Auto ON**: Sonido breve de campana de barco o silbato de contramaestre ("¡Adelante!")
- **Toggle Auto OFF**: Sonido de timón agarrado / madera firme (el capitán retoma el control)
- **Cambio de velocidad**: Click mecánico sutil (como ajustar un mecanismo). Pitch más alto a mayor velocidad
- **Repeat Loop inicio**: Sonido de ancla levantándose (zarpar)
- **Repeat Loop run completada**: Coin drop breve (rewards) — no intrusivo
- **Repeat Loop detenido**: Sonido de ancla cayendo (atracar)
- **En x2/x4**: La música de combate NO se acelera — mantiene tempo normal. Solo se aceleran SFX de acciones y animaciones. Esto evita que la música suene ridícula en x4

## UI Requirements

- **Botón Auto (en combate)**: Posición fija en la esquina superior derecha de la pantalla de combate, siempre visible. Tamaño touch-friendly (mínimo 44x44 dp). Icono de timón con estado visual claro: OFF = gris/apagado, ON = dorado/brillante con pulso. Al pulsar, feedback háptico (vibración corta en mobile). No requiere confirmación — toggle directo
- **Botón Speed (en combate)**: Junto al botón Auto (a su izquierda). Badge mostrando "x1"/"x2"/"x4". Tap para ciclar. x4 aparece bloqueado (candado) si no se ha completado Capítulo 1. Al intentar usar x4 bloqueado, toast: "Completa el Capítulo 1 para desbloquear x4"
- **Botón Stop (en repeat loop)**: Reemplaza o se superpone al botón de pausa durante el repeat loop. Rojo, prominente, fácil de pulsar. Confirmación antes de cancelar: "¿Detener farming? (3/10 runs completadas)"
- **Contador de repeat (en combate)**: Overlay no intrusivo en la esquina superior izquierda: "Run 3/10" o "Run 7/∞". Fuente pequeña, semitransparente, no compite con la info de combate
- **Selector de repeat (en stage select)**: En la pantalla de batalla del Stage System, un botón "Repeat" aparece junto al botón "Iniciar Batalla" solo si el stage tiene first-clear. Al pulsar, despliega selector: x1, x5, x10, ∞. Mostrar energía necesaria total: "x5 = 50 energía (tienes 120)". Si la energía no alcanza para la cantidad seleccionada, warning: "Solo tienes energía para 2 runs"
- **Resumen BetweenRuns**: Banner horizontal en la parte inferior que muestra iconos de rewards obtenidos en la run + "Run 3/10 completada". Se auto-cierra en 1-2s (escalado por velocidad). No bloquea la pantalla
- **Resumen final de repeat**: Pantalla completa al detenerse el loop. Lista de rewards acumuladas (agrupadas — no 10 pantallas separadas), runs completadas, energía total gastada, motivo de parada. Botón "OK" para cerrar
- **Layout mobile (portrait)**: Botones Auto y Speed en esquina superior derecha, no interfieren con Initiative Bar (superior centro) ni con panel de acciones (inferior). Contador repeat en esquina superior izquierda
- **Layout WebGL (landscape)**: Misma disposición, con más espacio horizontal. Botones pueden ser ligeramente más grandes

## Acceptance Criteria

**Auto-Battle Toggle**
1. El botón Auto activa/desactiva la IA aliada durante cualquier combate (terrestre o naval)
2. Con `AUTO_TOGGLE_TIMING = instant`, la IA toma control inmediato del turno actual si no se ha seleccionado acción
3. Si el jugador ya confirmó acción y la animación está en curso, el toggle toma efecto tras resolver la acción
4. El toggle se resetea a OFF al iniciar una nueva batalla
5. El toggle persiste entre oleadas de la misma batalla

**IA Terrestre**
6. La IA usa habilidad de curación cuando algún aliado tiene HP < HEAL_THRESHOLD (30%)
7. La IA selecciona la habilidad ofensiva con mayor AbilityPower disponible (sin cooldown)
8. La IA usa Ataque Normal cuando no hay habilidades disponibles
9. La IA nunca usa Guardia ni Pasar Turno
10. La IA no considera ventaja elemental al elegir target
11. La IA selecciona al enemigo con menor HP absoluto como target (rematar)
12. La IA selecciona al aliado con menor % HP como target de curación

**IA Naval**
13. La IA usa Reparar cuando HHP < REPAIR_THRESHOLD (30%) y tiene MP suficiente
14. La IA selecciona la habilidad naval ofensiva con mayor AbilityPower disponible (con MP)
15. La IA usa Cañonazo cuando no hay habilidades disponibles o sin MP
16. La IA nunca usa Abordaje ni Maniobra Evasiva
17. La IA selecciona al enemigo con menor HHP absoluto como target
18. La IA no gestiona MP estratégicamente (gasta hasta agotar)

**Repeat Loop**
19. Repeat solo está disponible en stages con first-clear completado
20. El jugador puede elegir x1, x5, x10, o infinito repeticiones
21. El sistema verifica energía suficiente antes de cada run
22. Al ganar una run, se otorgan rewards y se inicia la siguiente automáticamente
23. Al perder, el loop se detiene y se muestra pantalla de derrota normal
24. El jugador puede cancelar el loop en cualquier momento con botón Stop
25. Al cancelar o detenerse, las rewards de runs completadas ya fueron otorgadas
26. El resumen muestra "X/Y runs completadas" al detenerse

**Multiplicador de Velocidad**
27. El botón Speed cicla entre x1 → x2 → x4 → x1 (o x1 → x2 → x1 si x4 no desbloqueado)
28. x4 se desbloquea al completar Capítulo 1
29. El multiplicador afecta animaciones y resolución visual, no mecánicas de combate
30. El multiplicador funciona tanto en modo manual como auto
31. La preferencia de velocidad persiste entre batallas (guardada en Save/Load)
32. El multiplicador NO afecta la pantalla de rewards ni el tiempo de selección del jugador en manual

## Open Questions

1. ~~**¿Debería la IA considerar buffs/debuffs de soporte?**~~ **RESUELTO — No.** La IA se mantiene simple (curar/atacar/básico). Usar buffs/debuffs haría la IA demasiado competente y permitiría pasar contenido difícil en auto, violando el Pilar 4.

2. **¿Repeat loop con skip battle (sin mostrar combate)?** — Algunos gachas permiten skip total: no se muestra el combate, solo resultado + rewards. Reduce farming a un loading bar. Descartado para la demo, evaluable para el juego completo si hay demanda. *Owner: UX Designer. Target: Full Vision.*

3. **¿La IA naval debería usar Abordaje en algún caso?** — El diseño actual prohíbe Abordaje en auto para mantener la diferenciación manual/auto. Si los jugadores reportan que la IA naval es demasiado ineficiente, se podría añadir un Abordaje simple (target al crew con menor HP) como opción configurable post-playtesting. *Owner: Game Designer. Target: Post-playtesting.*

4. ~~**¿Multiplicador x4 también para la pantalla de rewards en repeat?**~~ **RESUELTO — Sí.** Los banners BetweenRuns en repeat loop se aceleran con el multiplicador de velocidad para evitar que repeat x10 se sienta lento. Actualizar la regla del multiplicador: NO afecta pantalla de rewards en batalla normal, SÍ afecta banners BetweenRuns en repeat loop.

5. ~~**¿Perfiles de IA configurables por el jugador?**~~ **RESUELTO — No para la demo.** La IA tiene un comportamiento fijo. Se reevaluará solo si hay demanda post-lanzamiento.
