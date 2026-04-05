# Combate Terrestre

> **Status**: Approved
> **Author**: user + agents
> **Last Updated**: 2026-03-27
> **Implements Pillar**: Pillar 1 (Combate Táctico)

## Overview

El Combate Terrestre es el modo de combate principal del juego. En encuentros por turnos, el jugador despliega un equipo de hasta 6 unidades (5 propias + 1 amigo) contra oleadas de enemigos en escenarios terrestres. Cada ronda, las unidades actúan según su velocidad (Initiative Bar), ejecutando habilidades ofensivas, defensivas o de soporte que se resuelven a través del Damage & Stats Engine. El jugador toma decisiones tácticas cada turno: a quién atacar, qué habilidad usar, cuándo activar Limit Breaks, y cómo gestionar recursos (cooldowns, estados alterados). Sin este sistema, el juego no tendría su loop de combate central — es donde convergen todas las mecánicas de unidades, progresión y estrategia.

## Player Fantasy

**Fantasía**: Eres un capitán pirata que lidera a su tripulación en batallas tácticas donde cada decisión cuenta. No ganas por fuerza bruta — ganas porque leíste la situación, explotaste debilidades elementales y coordinaste a tu equipo.

**Emoción objetivo**: Satisfacción táctica. El momento "ajá" cuando el jugador descubre que aplicar Quemadura primero y luego un ataque de Pólvora maximiza el daño. La tensión de decidir si gastar el Limit Break ahora o guardarlo para la siguiente oleada. El alivio de sobrevivir a un turno de jefe con el curandero justo a tiempo.

**Referencia**: FFBE (combate por turnos con profundidad de equipo, cadenas de habilidades y sinergias entre unidades) y Fire Emblem (lectura táctica de matchups — triángulo de armas como análogo a nuestro pentágono elemental).

**Tipo de sistema**: Activo — el jugador toma decisiones cada turno. No es infraestructura invisible, es el corazón de la experiencia.

## Detailed Design

### Core Rules

**1. Inicio de Batalla**
1. El sistema carga la BattleData del Stage System (oleadas, enemigos, condiciones de misión)
2. Se despliega el equipo aliado (5 unidades + 1 amigo) y la primera oleada de enemigos (max 5)
3. Se activan sinergias: el Capitán aliado activa traits si se cumple el threshold (≥3 unidades con el trait). Si el amigo (slot 6) es de otro jugador, funciona como segundo Capitán — sus traits se evalúan independientemente para doble activación (ver Traits/Sinergias GDD §2-3). Lo mismo para el Capitán enemigo si existe
4. Se inicializa el estado de combate por combatiente: HP actual, buffs/debuffs vacíos, cooldowns en 0, LB disponible = true
5. Comienza la Phase Pre-Combat (ver Initiative Bar GDD): se calcula el orden de la primera ronda por SPD

**2. Estructura de Ronda**

Cada ronda sigue el orden definido por la Initiative Bar:
1. **Inicio de ronda**: Resetear flag `LBUsedThisRound` a false para todos los combatientes
2. **Turno por turno** (en orden de Initiative Bar): Cada combatiente resuelve su turno según el orden de procesamiento (definido en DSE):
   - Decrementar duración de buffs/debuffs de este combatiente. Los que llegan a 0 se eliminan
   - Decrementar CC Immunity (si tiene, se consume y salta CC check)
   - Sangrado: recibe daño de Sangrado (% HP max)
   - CC check: si tiene CC activo (Stun/Freeze/Sleep, o [Provisional] Confuse), pierde el turno ([Provisional] Confuse: actúa aleatoriamente contra cualquier bando)
   - **Fase de acción**: si no está bajo CC, el combatiente actúa (ver sección 3 para aliados, sección 4 para enemigos)
   - Quemadura: si tiene Quemadura Y realizó una acción (Ataque Normal, Habilidad, o Guardia), recibe daño de Quemadura. Pasar Turno NO activa Quemadura
   - Veneno: recibe daño de Veneno (% HP max)
3. **Fin de ronda**: Verificar condiciones de victoria/derrota. Si ninguna se cumple, nueva ronda

**3. Acciones del Turno (Unidades Aliadas)**

Cuando es el turno de una unidad aliada, el jugador elige una de estas acciones:

| Acción | Descripción | Targeting |
|--------|-------------|-----------|
| **Ataque Normal** | Golpe básico usando la stat ofensiva principal de la unidad (ATK o MST según tipo). Sin cooldown | Seleccionar 1 enemigo |
| **Habilidad** | Usa una habilidad del kit de la unidad (AbilityEntry del UDM). Puede tener cooldown y efectos variados | Según TargetType: single-enemy (elegir), aoe-enemy (auto todos), self (auto), ally-single (elegir aliado), ally-aoe (auto aliados) |
| **Guardia** | La unidad se defiende. Reduce el daño recibido hasta su siguiente turno | Sin target (self) |
| **Pasar Turno** | La unidad no hace nada. No activa daño de Quemadura | Sin target |

**4. Acciones del Turno (Enemigos)**

Los enemigos actúan según su tier (definido en Enemy System GDD):
- **Normal**: Selección aleatoria ponderada entre sus habilidades disponibles. Target aleatorio con peso hacia unidades con baja HP o ventaja elemental
- **Elite**: Patrón de IA semi-fijo con gimmicks (priorizar healer, rotación de habilidades, contraataques condicionales)
- **Jefe**: Behavior tree completo. Cambios de fase al cruzar umbrales de HP. Inmunidades, patrones predecibles, habilidades exclusivas por fase

**5. Limit Break (Turno Extra)**

Limit Break es la mecánica que permite a una unidad romper el límite de 1 acción por ronda, obteniendo un turno extra inmediato.

- **No es una acción separada**: es una propiedad de ciertas habilidades (`CanLimitBreak: true` en AbilityEntry)
- **Condición de activación**: cada habilidad LB define una `LBCondition` que debe cumplirse al resolver la habilidad. Ejemplos de condiciones:
  - `OnKill`: Matar al objetivo con esta habilidad
  - `OnCrit`: La habilidad resulta en golpe crítico
  - `OnElementAdvantage`: El target es débil al elemento del ataque
  - `OnStatusTarget`: El target tiene un estado alterado específico
  - `OnLowHP`: La unidad actuante tiene HP < umbral definido
  - `OnAllyDown`: Hay al menos 1 aliado con HP ≤ 0
  - (Nuevas condiciones se definen al diseñar unidades)
- **Límite**: Máximo 1 Limit Break por unidad por ronda. El flag `LBUsedThisRound` se resetea al inicio de cada ronda
- **Resolución**: Tras ejecutar una habilidad con `CanLimitBreak: true`, si la condición se cumple Y `LBUsedThisRound == false`:
  1. Marcar `LBUsedThisRound = true`
  2. La unidad se reinserta en la Initiative Bar inmediatamente después del combatiente actual
  3. En su turno extra, la unidad puede realizar cualquier acción normal (Ataque, Habilidad, Guardia, Pasar)
  4. El turno extra sigue el mismo orden de procesamiento (CC check, Sangrado, etc.)
- **Enemigos**: Los enemigos también pueden tener habilidades con LB. El mismo sistema aplica

**6. Resolución de Habilidades**

Cuando un combatiente usa una habilidad:
1. Verificar cooldown disponible (si no disponible, la habilidad no es seleccionable)
2. Determinar target(s) según TargetType de la habilidad
3. Para cada target, resolver usando la Master Damage Formula del DSE:
   a. Aplicar buffs/debuffs a stats: `EffectiveStat = BaseStat × clamp(1.0 + sum(buffs) - sum(debuffs), 0.0, 2.0)`
   b. Calcular daño base: `RawDamage = max((EffectiveATK × ATTACK_MULTIPLIER) - (EffectiveDEF × DEFENSE_MULTIPLIER), 1)` (físico: ATK vs DEF, mágico: MST vs SPR)
   c. Aplicar multiplicadores: `FinalDamage = max(floor(RawDamage × AbilityPower × ElementMod × CritMod × Variance), 1)`
   c. Aplicar daño al HP del target
   d. Aplicar efectos secundarios (buffs, debuffs, estados alterados) con sus probabilidades
4. Activar cooldown de la habilidad
5. Check Limit Break: si la habilidad tiene `CanLimitBreak: true`, evaluar `LBCondition`. Si se cumple y `LBUsedThisRound == false`, activar turno extra (ver sección 5)

**7. Oleadas**

Una batalla puede tener entre 1 y 5 oleadas (definido en BattleData del Stage System):
- Al eliminar a todos los enemigos de una oleada, la siguiente oleada se despliega
- **Estado aliado persiste**: HP actual, buffs/debuffs activos, cooldowns en progreso
- **Enemigos nuevos**: Entran con HP completo, sin estados, cooldowns en 0
- **Sinergias**: Las sinergias aliadas se mantienen (el equipo no cambia). Las sinergias enemigas se recalculan con el capitán de la nueva oleada
- **La ronda se reinicia entre oleadas**: Al comenzar una nueva oleada, se recalcula la Initiative Bar con todos los combatientes vivos (aliados + nuevos enemigos). Esto significa que enemigos rápidos podrían actuar antes que el healer aliado — el jugador debe asegurarse de curar y prepararse antes de eliminar al último enemigo de la oleada anterior

**8. Condiciones de Victoria y Derrota**

- **Victoria**: Todos los enemigos de todas las oleadas eliminados (HP ≤ 0)
- **Derrota**: Todas las unidades aliadas con HP ≤ 0 (incluyendo amigo)
- **Verificación**: Se comprueba tras cada acción que cause daño para feedback inmediato
- **Empate simultáneo**: Si el último aliado y último enemigo caen en la misma resolución → **Derrota** (el jugador pierde en empate)

### States and Transitions

**Estados de Batalla**

| Estado | Descripción | Transiciones válidas |
|--------|-------------|---------------------|
| `PreCombat` | Cargando datos, desplegando unidades, calculando sinergias, primera Initiative Bar | → `InRound` |
| `InRound` | Procesando turnos secuencialmente según Initiative Bar | → `WaveTransition` (última unidad enemiga muerta, quedan oleadas), → `Victory` (última oleada limpiada), → `Defeat` (todos aliados caídos), → `InRound` (nueva ronda si la actual termina sin victoria/derrota) |
| `WaveTransition` | Oleada limpiada, desplegando siguiente oleada, recalculando sinergias enemigas, reiniciando Initiative Bar | → `InRound` |
| `Victory` | Todos los enemigos eliminados. Mostrar resultados, otorgar recompensas, evaluar misiones | → (fin de batalla, retorno a Stage System) |
| `Defeat` | Todos los aliados caídos. Mostrar pantalla de derrota | → (fin de batalla, opciones: reintentar, salir) |

**Estados de Combatiente**

| Estado | Descripción | Transiciones válidas |
|--------|-------------|---------------------|
| `Idle` | Esperando su turno en la Initiative Bar | → `Acting` (le toca actuar) |
| `Acting` | Es el turno de este combatiente. El jugador (o IA) selecciona acción | → `Idle` (acción completada, sin LB), → `LimitBreak` (condición LB cumplida) |
| `LimitBreak` | Turno extra por Limit Break. Se reinserta en Initiative Bar | → `Acting` (turno extra comienza) |
| `Guarding` | Ha usado Guardia. Recibe daño reducido hasta su siguiente turno | → `Idle` (su siguiente turno llega, se quita Guardia antes de actuar) |
| `Incapacitated` | Bajo CC (Stun/Freeze/Sleep). No puede actuar | → `Idle` (CC expira o es limpiado) |
| `Confused` | Bajo Confuse. Actúa aleatoriamente | → `Idle` (Confuse expira o es limpiado) |
| `Dead` | HP ≤ 0. No participa en Initiative Bar | → `Idle` (revivido por habilidad de revive, con HP parcial) |

**Notas sobre transiciones:**
- `Guarding` es un sub-estado de `Idle` — la unidad sigue en la Initiative Bar normalmente, pero tiene el modificador de reducción de daño activo
- Un combatiente puede estar `Idle` con múltiples buffs/debuffs/estados activos — estos no son estados de la máquina de estados sino datos del runtime state (definido en DSE)
- **Revive**: Habilidades de revive transicionan `Dead` → `Idle` con un porcentaje de HP máximo. La unidad revivida se coloca al **final de la Initiative Bar de la ronda actual** (actúa última este turno). En la siguiente ronda, se posiciona normalmente por SPD

### Interactions with Other Systems

| Sistema | Dirección | Datos que fluyen | Interfaz |
|---------|-----------|-----------------|----------|
| **Unit Data Model** | UDM → CT | CharacterData (stats, habilidades, elemento, tipo, traits), StatBlock (HP/ATK/DEF/MST/SPR/SPD/CRT) | CT lee los datos estáticos de cada unidad al iniciar batalla. No modifica UDM |
| **Damage & Stats Engine** | CT ↔ DSE | CT envía: atacante, defensor, habilidad, elemento. DSE devuelve: daño final, efectos aplicados, crit result. DSE gestiona: buffs/debuffs activos, estados alterados, runtime state por combatiente | CT orquesta cuándo se llama a DSE; DSE calcula el resultado. El orden de procesamiento de turno (CC→Sangrado→Acción→Quemadura→Veneno) es de DSE, CT lo ejecuta |
| **Initiative Bar** | CT ↔ IB | CT informa: combatientes vivos y su SPD actual, eventos de muerte, reinicio de oleada. IB devuelve: orden de turnos de la ronda, posición de inserción para LB | CT solicita el siguiente combatiente; IB lo calcula. CT notifica cambios (muerte, SPD buff, LB) para que IB reordene |
| **Enemy System** | ES → CT | EnemyData (stats, habilidades, tier, AI pattern, IsEncounterCaptain, fase de jefe), decisiones de IA | CT consulta ES para obtener datos de enemigos y sus decisiones de turno. ES decide qué hace cada enemigo; CT ejecuta la acción resultante |
| **Traits/Sinergias** | TS → CT | Bonificaciones activas por sinergias (stat buffs para unidades con el trait) | CT solicita sinergias activas al inicio de batalla y al cambiar de oleada. TS calcula basándose en el equipo y el Capitán; CT aplica los buffs resultantes |
| **Stage System** | SS → CT | BattleData (oleadas, WaveConfig, condiciones de misión, rewards) | CT lee la configuración de batalla al iniciar. Al terminar (victoria), CT devuelve resultado para que SS evalúe misiones y otorgue rewards |
| **Combate Naval** | CT ← CN | (Futuro) Compartirán muchas mecánicas base pero con reglas diferentes (posiciones navales, cañones). CT es el sistema base; CN extenderá/modificará reglas | Interfaz a definir en el GDD de Combate Naval |
| **Auto-Battle** | CT ↔ AB | CT notifica a AB: turno aliado, acciones disponibles, habilidades con cooldowns, HP de todos los combatientes. AB devuelve: acción + target. CT la ejecuta como input del jugador | AB reemplaza la "Fase de acción" del jugador con IA aliada. CT no cambia — recibe la misma interfaz de acción+target venga del jugador o de la IA. Ver [auto-battle.md](auto-battle.md) |
| **Combat UI** | CT ↔ CUI | CT envía: estado de batalla, turno actual, HP/buffs de todos, acciones disponibles. CUI envía: acción seleccionada por el jugador, target seleccionado | CUI es la capa de presentación. CT emite eventos (turno_start, damage_dealt, unit_died, wave_complete) que CUI renderiza |
| **Team Composition** | TC → CT | Equipo seleccionado (5 unidades + amigo), Capitán designado | TC entrega el equipo pre-batalla. CT lo recibe como input en PreCombat |

## Formulas

**Guardia — Reducción de Daño**

```
DamageReceived_Guard = floor(IncomingDamage × GUARD_REDUCTION_MULTIPLIER)
```

| Variable | Definición | Rango |
|----------|-----------|-------|
| `IncomingDamage` | Daño final calculado por DSE antes de aplicar Guardia | ≥ 0 |
| `GUARD_REDUCTION_MULTIPLIER` | Porcentaje de daño que pasa estando en Guardia | 0.50 (50% daño recibido = 50% reducción) |

- Guardia se aplica **después** del cálculo completo de DSE (incluyendo buffs DEF, ElementMod, etc.)
- Guardia solo reduce daño de ataques y habilidades directas. **No** reduce daño de estados (Sangrado, Quemadura, Veneno)
- Guardia dura hasta el siguiente turno de la unidad (se retira al inicio de su fase de acción)

**Ejemplo**: Unidad en Guardia recibe un ataque con FinalDamage = 500 → `floor(500 × 0.50)` = 250 de daño

**Ataque Normal — Habilidad Implícita**

El Ataque Normal es una habilidad implícita que toda unidad posee. Se calcula con la fórmula estándar del DSE:

```
FinalDamage = floor(RawDamage × AbilityPower_Normal × ElementMod × CritMod × Variance)
```

| Variable | Valor para Ataque Normal |
|----------|-------------------------|
| `AbilityPower_Normal` | 1.0 (multiplicador base, sin bonificación) |
| `Element` | Neutral (ElementMod = 1.0 siempre). Una habilidad pasiva puede modificar esto para unidades específicas |
| `TargetType` | single-enemy |
| `Cooldown` | 0 (siempre disponible) |
| `CanLimitBreak` | false (el ataque normal no puede activar LB) |
| `Effects` | Ninguno |

**Revive — Restauración de HP**

```
ReviveHP = floor(Target.MaxHP × REVIVE_HP_PERCENT)
```

| Variable | Definición | Rango |
|----------|-----------|-------|
| `Target.MaxHP` | HP máximo de la unidad revivida | > 0 |
| `REVIVE_HP_PERCENT` | Porcentaje de HP con el que revive | Definido por la habilidad de revive (típico: 0.20-0.50) |

- El porcentaje exacto es un atributo de cada habilidad de revive, no un valor global
- La unidad revivida se coloca al final de la Initiative Bar de la ronda actual (actúa última). En la siguiente ronda, se posiciona por SPD

## Edge Cases

1. **Último aliado y último enemigo mueren simultáneamente**: Resultado es **Derrota**. El jugador pierde en empate.

2. **Unidad aliada muere por Sangrado/Veneno antes de poder actuar**: La muerte se procesa inmediatamente. Si era la última aliada, Derrota. Si quedan aliados, turno saltado, Initiative Bar continúa.

3. **Unidad con Guardia recibe daño de estado (Sangrado/Veneno/Quemadura)**: Guardia **NO** reduce el daño de estados. Guardia solo reduce daño de ataques y habilidades directas.

4. **LB se activa pero la unidad muere por Quemadura/Veneno post-acción**: El turno extra se cancela. La unidad está muerta y no puede actuar.

5. **Dos unidades activan LB en la misma ronda**: Cada una recibe su turno extra (max 1 por unidad). Se insertan en la Initiative Bar en el orden en que se activaron.

6. **Enemigo Jefe cambia de fase por daño de una habilidad AoE**: Los cambios de fase se procesan **después** de que la acción completa se resuelva. La inmunidad de la nueva fase no aplica retroactivamente al daño que causó el cambio.

7. **Último enemigo de oleada muere por estado de daño**: La oleada se completa inmediatamente. Los combatientes aliados que aún no han actuado pierden su turno restante (la ronda se reinicia con la nueva oleada).

8. **Guardia + CC en el mismo turno**: Si una unidad usa Guardia y luego es aturdida (Stun/Freeze/Sleep), mantiene la reducción de Guardia hasta su siguiente turno. La Guardia se retira al inicio de su turno, independientemente de si puede actuar.

9. **Guardia durante turno extra LB**: Si una unidad activa LB y usa Guardia en su turno extra, la Guardia se aplica normalmente (dura hasta su siguiente turno en la siguiente ronda). No se acumula con una Guardia previa — la nueva reemplaza la anterior.

10. **Habilidad de revive en una oleada donde ya no quedan enemigos**: El aliado revivido entra en la siguiente oleada con el HP de revive.

11. **Revive de la unidad amiga (slot 6)**: La unidad amiga puede ser revivida sin restricciones especiales.

12. **Pasar Turno bajo Quemadura**: Pasar Turno **no** activa Quemadura. Es la contramedida del jugador: sacrificar un turno para evitar daño.

13. **Habilidad con LB condición OnKill usada como AoE que mata varios enemigos**: La condición OnKill se evalúa una sola vez al finalizar la resolución. Si al menos 1 enemigo murió, la condición se cumple. No se otorgan múltiples turnos extra.

14. **[Provisional — si se implementan cooldowns] Cooldowns entre oleadas**: Los cooldowns NO se resetean entre oleadas. Persisten como parte del estado aliado.

15. **Buff/Debuff con duración "hasta fin de batalla"**: Persiste entre oleadas. Solo se elimina al terminar la batalla completa.

16. **[Provisional — si se implementa Confuse] Unidad confusa sin targets válidos**: Confuse solo selecciona combatientes vivos. Si no hay targets del bando opuesto, ataca a un aliado del mismo bando.

## Dependencies

### Dependencias Upstream (CT depende de)

| Sistema | Tipo | Interfaz | GDD |
|---------|------|----------|-----|
| **Unit Data Model** | Hard | CharacterData, StatBlock, AbilityEntry con CanLimitBreak y LBCondition | ✅ Approved |
| **Damage & Stats Engine** | Hard | Fórmulas de daño, buffs/debuffs, estados alterados, orden de procesamiento de turno | ✅ Approved |
| **Initiative Bar** | Hard | Orden de turnos, reinserción LB, reinicio de oleada, tie-breaking | ✅ Approved |
| **Enemy System** | Hard | EnemyData, tiers, AI patterns, fases de jefe, IsEncounterCaptain | ✅ Approved |
| **Traits/Sinergias** | Soft | Bonificaciones de sinergias activas. CT funciona sin sinergias, pero es mejor con ellas | ✅ Approved |
| **Stage System** | Hard | BattleData, WaveConfig, condiciones de misión | ✅ Approved |
| **Team Composition** | Hard | Equipo seleccionado (5+1), Capitán designado, flag de segundo Capitán (true si slot 6 es amigo de otro jugador) | ⬜ Designed |

### Dependencias Downstream (dependen de CT)

| Sistema | Tipo | Qué necesita de CT | GDD |
|---------|------|---------------------|-----|
| **Combate Naval** | Hard | Mecánicas base de combate para extender con reglas navales | ⬜ Not Started |
| **Auto-Battle** | Hard | Interfaz de acciones para reemplazar input del jugador con IA. AB reemplaza la "Fase de acción" con IA aliada (prioridad: curar < 30% HP → habilidad mayor AP → Ataque Normal). Target: menor HP absoluto. No usa Guardia ni Pasar Turno | ✅ Designed |
| **Combat UI** | Hard | Eventos de combate (turno, daño, muerte, oleada) para renderizar | ⬜ Not Started |

### Nota sobre UDM

El Unit Data Model necesita una actualización para soportar las nuevas propiedades de habilidad:
- `CanLimitBreak: bool` en AbilityEntry
- `LBCondition: LBConditionType` en AbilityEntry (enum con OnKill, OnCrit, OnElementAdvantage, etc.)
- `LBConditionParam: float` (opcional, para condiciones con umbral numérico como OnLowHP)
- `LBConditionTarget: string` (opcional, para condiciones que necesitan referencia no numérica como OnStatusTarget → "Quemadura")

Esta actualización debe hacerse antes de implementar Combate Terrestre.

## Tuning Knobs

| Knob | Valor Actual | Rango Seguro | Afecta a | Notas |
|------|-------------|-------------|----------|-------|
| `GUARD_REDUCTION_MULTIPLIER` | 0.50 | 0.30–0.70 | Utilidad de Guardia. < 0.30 hace Guardia demasiado fuerte (poco daño recibido). > 0.70 hace Guardia inútil | Solo aplica a daño directo, no estados |
| `MAX_WAVES_PER_BATTLE` | 5 | 1–5 | Duración de batalla. Más oleadas = más desgaste, más gestión de recursos entre oleadas | Definido en Stage System pero CT lo ejecuta |
| `MAX_ENEMIES_PER_WAVE` | 5 | 1–5 | Dificultad por oleada. Más enemigos = más presión, más valor de AoE | Definido en Stage System |
| `MAX_LB_PER_UNIT_PER_ROUND` | 1 | 1–2 | Poder ofensivo. Si se sube a 2, las unidades pueden actuar hasta 3 veces por ronda. Peligro de power creep extremo | Cambiar a 2 solo si el meta lo necesita mucho más tarde |
| `NORMAL_ATTACK_ABILITY_POWER` | 1.0 | 0.5–1.5 | Daño base del ataque normal. < 1.0 fuerza uso de habilidades. > 1.0 hace ataque normal competitivo con habilidades | Interactúa con balance de habilidades del DSE |
| `REVIVE_HP_PERCENT` (típico) | 0.20–0.50 | 0.10–0.75 | HP al revivir. Muy bajo = revivir es inútil (muere al siguiente golpe). Muy alto = revivir no tiene coste real | Valor específico por habilidad de revive, no global |
| `LB_CONDITIONS` (pool) | OnKill, OnCrit, OnElementAdvantage, OnStatusTarget, OnLowHP, OnAllyDown | — | Diversidad de activación de LB. Añadir nuevas condiciones expande espacio de diseño de unidades | Cada nueva condición debe tener contraplay claro |
| `[Provisional] COOLDOWN_DURATION` (por habilidad) | Varía | 0–10 turnos | Frecuencia de uso de habilidades. Cooldowns altos = más dependencia de ataque normal y rotación | Si se implementa el sistema de cooldowns |

## Visual/Audio Requirements

**Visual**
- Escenario de fondo estático o con parallax sutil (playa, jungla, fortaleza, cueva) según la Scene del Stage System
- Unidades aliadas posicionadas a la izquierda, enemigos a la derecha (estilo JRPG clásico)
- Animaciones mínimas necesarias por unidad: idle, ataque, habilidad, daño recibido, muerte, guardia
- Indicadores visuales de estado: iconos sobre la unidad para buffs/debuffs activos, color/brillo para estados (rojo pulsante para Quemadura, verde para Veneno, etc.)
- Feedback visual de daño: números flotantes con color (blanco normal, amarillo crítico, rojo fuego, morado veneno)
- Indicador visual claro para habilidades con LB disponible (brillo, borde especial) cuando la condición puede cumplirse
- Efecto visual de transición de oleada (fade, barrido, o entrada de nuevos enemigos)

**Audio**
- Música de combate dinámica: intro de batalla, loop principal, loop tenso (jefe/baja HP), victoria, derrota
- SFX por tipo de acción: ataque físico, ataque mágico, habilidad (variado por elemento), guardia, pasar turno
- SFX de estados: aplicación de estado, tick de daño de estado, expiración de estado
- SFX de Limit Break: sonido distintivo al activar turno extra
- SFX de oleada: transición entre oleadas, última oleada (tensión adicional)
- Feedback auditivo de selección: navegar por acciones, confirmar target, cancelar

## UI Requirements

- **Initiative Bar**: Barra horizontal superior con iconos de todos los combatientes ordenados por SPD. Icono activo destacado. Inserción visual de LB (icono reaparece con efecto)
- **Panel de acciones (diseño premium)**: Panel estilizado con identidad visual pirata. Los 4 botones de acción (Ataque Normal, Habilidades, Guardia, Pasar Turno) deben ser iconos grandes y expresivos con arte propio, no botones planos. Cada acción tiene una animación de selección y feedback visual/auditivo al pulsar. El panel debe sentirse como un timón o carta de navegación — parte de la fantasía pirata, no un menú genérico
- **Sub-menú de habilidades (showcase de unidad)**: Cada habilidad se presenta como una carta o ficha con: icono de habilidad (arte único), nombre estilizado, elemento (icono + color), TargetType (icono), breve descripción del efecto. Al hacer hover/long-press se expande un tooltip detallado con: daño estimado, efectos secundarios con probabilidades, cooldown restante (si aplica), condición de LB (si tiene `CanLimitBreak`). Las habilidades con LB tienen un borde o brillo especial que las distingue. Las habilidades en cooldown se muestran atenuadas con contador visual. El sub-menú debe transmitir poder y personalidad de la unidad — es el momento donde el jugador conecta con el kit de su personaje
- **Targeting**: Al seleccionar acción single-target, los targets válidos se resaltan con indicador de ventaja/desventaja elemental. Tap/click para confirmar. Para AoE, mostrar preview visual sobre todos los targets antes de confirmar
- **Barras de HP**: Visibles para todos los combatientes. Aliados: barra estilizada + número exacto. Enemigos: barra (número visible para Elites y Jefes, oculto para Normal)
- **Indicadores de estado**: Iconos compactos junto a la barra de HP con duración restante. Al hacer hover/tap se muestra nombre del estado y efecto
- **Contador de oleada**: "Oleada X/Y" visible con transición animada entre oleadas
- **Mensaje de turno**: Indicación de quién actúa con retrato de la unidad
- **Pantalla de victoria**: Resumen de batalla con animación de recompensas, misiones completadas (del Stage System)
- **Pantalla de derrota**: Opciones: Reintentar, Salir

## Acceptance Criteria

**Flujo de Batalla**
1. Una batalla se inicia correctamente con los datos del Stage System (equipo aliado, oleadas enemigos)
2. La batalla transiciona por todos los estados: PreCombat → InRound → WaveTransition → Victory/Defeat
3. Las oleadas transicionan correctamente al morir el último enemigo, reiniciando la Initiative Bar
4. La victoria se detecta al eliminar la última oleada; la derrota al caer el último aliado
5. El empate simultáneo (último aliado y enemigo mueren a la vez) resulta en Derrota

**Acciones del Jugador**
6. El jugador puede seleccionar: Ataque Normal, Habilidad, Guardia, Pasar Turno en cada turno aliado
7. Ataque Normal usa la stat ofensiva principal y tiene AbilityPower 1.0
8. Habilidades respetan su TargetType: single-enemy requiere selección, AoE aplica automáticamente
9. Guardia reduce daño directo recibido en un 50% hasta el siguiente turno
10. Guardia NO reduce daño de estados (Sangrado, Quemadura, Veneno)
11. Pasar Turno no ejecuta acción y no activa daño de Quemadura

**Limit Break**
12. Solo habilidades con `CanLimitBreak: true` pueden activar turno extra
13. El turno extra se activa únicamente si `LBCondition` se cumple Y `LBUsedThisRound == false`
14. Máximo 1 LB por unidad por ronda; el flag se resetea al inicio de ronda
15. La unidad con LB se reinserta en la Initiative Bar inmediatamente después del turno actual
16. En el turno extra, la unidad puede realizar cualquier acción (Ataque, Habilidad, Guardia, Pasar)
17. Si la unidad muere por Quemadura/Veneno post-acción, el turno extra se cancela
18. Condición OnKill con AoE que mata múltiples enemigos otorga solo 1 turno extra

**Enemigos**
19. Enemigos Normal eligen acciones con selección aleatoria ponderada
20. Enemigos Elite siguen patrones de IA semi-fijos con gimmicks
21. Enemigos Jefe ejecutan behavior trees con cambios de fase al cruzar umbrales de HP
22. Los cambios de fase de jefe se procesan después de resolver completamente la acción que los causó

**Oleadas**
23. El estado aliado (HP, buffs, cooldowns) persiste entre oleadas
24. Los enemigos de cada oleada entran con HP completo y sin estados
25. La Initiative Bar se recalcula completamente al inicio de cada oleada
26. Las sinergias enemigas se recalculan con el capitán de la nueva oleada

**Integración de Sistemas**
27. El DSE calcula correctamente todo el daño usando la Master Damage Formula: `RawDamage = max((EffATK × ATK_MULT) - (EffDEF × DEF_MULT), 1)` → `FinalDamage = max(floor(RawDamage × AbilityPower × ElementMod × CritMod × Variance), 1)`
28. La Initiative Bar determina el orden de turnos por SPD con tie-breaking correcto
29. Las sinergias del Traits/Sinergias se aplican correctamente al inicio de batalla y entre oleadas
30. Los estados alterados (Sangrado, Quemadura, Veneno, Stun, Freeze, Sleep) se procesan en el orden de turno definido por DSE

**Revive**
31. Una habilidad de revive transiciona una unidad de Dead a Idle con HP parcial
32. La unidad revivida se coloca al final de la Initiative Bar de la ronda actual (actúa última)
33. La unidad amiga (slot 6) puede ser revivida sin restricciones

## Open Questions

1. **¿Se implementará sistema de cooldowns en habilidades?** — Planteado pero no confirmado. Afecta a la rotación de habilidades y el valor del Ataque Normal. Decisión pendiente antes de implementación. *Owner: Game Designer*

2. **¿Se implementará el estado Confuse?** — Estado CC que hace actuar aleatoriamente. Añade complejidad de diseño y edge cases. Decidir si merece la pena para el demo. *Owner: Game Designer*

3. **¿Cómo escala la dificultad entre oleadas de una misma batalla?** — ¿Los enemigos de oleadas posteriores tienen stats más altos, o la dificultad viene solo de la composición? Podría definirse en el Stage System. *Owner: Systems Designer*

4. **¿Puede una habilidad de LB activarse durante un turno extra de LB?** — Actualmente no (max 1 LB/ronda). ¿Se quiere dejar abierta la puerta para "cadenas de LB" en el futuro como mecánica de unidades legendarias? *Owner: Game Designer*

5. **¿Qué pasa con los campos de `CanLimitBreak` y `LBCondition` en el UDM para habilidades de soporte/curación?** — ¿Las habilidades de curación/buff pueden tener LB? Si sí, crea combos interesantes (curar → turno extra → atacar). *Owner: Game Designer*
