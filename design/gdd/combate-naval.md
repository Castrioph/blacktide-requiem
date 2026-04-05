# Combate Naval

> **Status**: Approved
> **Author**: user + agents
> **Last Updated**: 2026-03-27
> **Implements Pillar**: Pillar 1 (Profundidad Estratégica Dual)

## Overview

El Combate Naval es el segundo modo de combate del juego y su principal diferenciador. En encuentros por turnos sobre el mar, el jugador despliega un barco tripulado contra enemigos navales (barcos enemigos y criaturas marinas). A diferencia del combate terrestre donde cada unidad actúa individualmente, en naval el barco actúa como una sola entidad: su tripulación contribuye stats, habilidades y sinergias, pero es el barco quien toma turnos en la Initiative Bar. El jugador dispone de 5 acciones: Cañonazo (ataque básico), Habilidad Naval (del pool del barco), Maniobra Evasiva (reducir daño), Abordaje (atacar directamente a un tripulante enemigo) y Reparar (curar el casco gastando MP). La decisión táctica central es doble: ¿hundir el barco enemigo dañando su casco, o neutralizar su tripulación con Abordajes para debilitarlo progresivamente? Esta dualidad, junto con la ausencia de golpes críticos, el sistema de roles de tripulación y las sinergias de traits navales, crea una experiencia estratégica distinta al combate terrestre — más centrada en composición de equipo y targeting que en RNG.

## Player Fantasy

**Fantasía**: Eres un capitán pirata al mando de tu navío. No peleas cuerpo a cuerpo — diriges. Cada orden que das al barco refleja las habilidades de tu tripulación: el artillero mejora tus cañones, el navegante esquiva proyectiles, el cirujano mantiene al barco operativo. Cuando ordenas un abordaje al barco enemigo, no atacas su casco — atacas al hombre que hace funcionar sus cañones.

**Emoción objetivo**: Control y lectura táctica. En terrestre, la emoción es la ejecución momento a momento (¿qué habilidad uso este turno?). En naval, la emoción es la **deconstrucción del enemigo**: cada crew member que eliminas debilita al barco enemigo de forma tangible — pierde stats, pierde habilidades, pierde sinergias. La satisfacción viene de ver cómo un barco enemigo que era una amenaza se convierte en un cascarón vacío después de una serie de abordajes quirúrgicos.

**Diferenciadores clave vs. Combate Terrestre** (estos DEBEN sentirse diferentes en gameplay):

| Terrestre | Naval |
|-----------|-------|
| 5+1 unidades actúan independientemente | 1 barco actúa como entidad (tripulación es "equipo pasivo") |
| Matas enemigos uno a uno | Eliges: hundir el barco (HHP) O criplear su crew (Abordaje) |
| Los crits generan momentos de RNG explosivo | No hay crits — toda ventaja viene de composición y decisiones |
| La composición importa (sinergias) | La composición importa MÁS (role matching + sinergias + qué habilidades aporta cada crew) |
| Enemigos muertos desaparecen | Crew eliminado debilita al barco progresivamente (pierde stats/habilidades en tiempo real) |
| Guardia = reducir daño personal | Maniobra = reducir daño al barco + Reparar = curar el casco (gestión de recursos naval) |

**Referencia**: La fantasía de comando naval de Assassin's Creed Black Flag (dar órdenes al barco, no pelear personalmente) cruzada con la profundidad de composición de equipo de FFBE.

**Tipo de sistema**: Activo — el jugador toma decisiones cada turno del barco. La decisión central (hundir vs. abordar) es el layer táctico que no existe en terrestre y que justifica la existencia de este segundo modo de combate.

**El sistema falla si**: naval se siente como "terrestre pero con barcos", si el Abordaje no tiene impacto tangible (el jugador siempre prefiere hundir directamente), o si la composición de tripulación no importa más que en terrestre.

## Detailed Design

### Core Rules

> **Nota de visión futura**: El combate naval de la demo es turn-based con mecánicas únicas (Abordaje, crew targeting). Para el juego completo, se evaluará migrar a un sistema de grid táctico estilo Fire Emblem con movimiento + posicionamiento, lo que aumentaría enormemente la diferenciación con el combate terrestre. Esta decisión se tomará tras la demo, basándose en feedback de jugadores y viabilidad técnica. El diseño actual está pensado para ser compatible con esa evolución: stats, fórmulas, crew system y roles se mantienen intactos en ambos modelos.

**1. Inicio de Batalla**
1. El sistema carga la BattleData del Stage System (oleadas navales, enemigos navales, condiciones de misión)
2. Se despliega el barco aliado (tripulación asignada + guest) y la primera oleada de enemigos navales
3. Se calculan los stats efectivos del barco aliado: `EffectiveStat = BaseStat + UpgradeBonus + CrewContribution + TraitBonuses`
4. Se activan sinergias: el Capitán naval (crew en slot Capitán del barco) evalúa traits. Si el guest es amigo de otro jugador, funciona como segundo Capitán con doble activación (ver Traits/Sinergias GDD §2-3)
5. Si existe Capitán enemigo, se activan sinergias enemigas
6. Se inicializa el estado de combate por barco: HHP actual = HHP efectivo, MP actual = MP efectivo, buffs/debuffs vacíos. Se inicializa crew HP individual por rol (ver §6)
7. Comienza Pre-Combat: se calcula el orden de la primera ronda por SPD efectivo de cada barco/entidad en la Initiative Bar

**2. Estructura de Ronda**

Cada ronda sigue el orden definido por la Initiative Bar (mismas reglas que terrestre, pero con barcos como entidades):
1. **Inicio de ronda**: Resetear flag `LBUsedThisRound` a false para todos los barcos/entidades
2. **Turno por turno** (en orden de Initiative Bar): Cada barco/entidad resuelve su turno:
   - Decrementar duración de buffs/debuffs del barco. Los que llegan a 0 se eliminan
   - Sangrado: daña HP de **1 crew member aleatorio vivo** (% HP max del crew). Si no hay crew viva, no hace daño
   - CC check: ships son **inmunes a Aturdimiento y Sueño** (siempre actúan). Criaturas marinas siguen la misma regla. Ceguera y Silencio sí aplican
   - **Fase de acción**: el controlador del barco elige una acción (ver §3 para aliado, §4 para enemigos)
   - Quemadura: si tiene Quemadura Y realizó una acción (Cañonazo, Habilidad, Maniobra, Abordaje, o Reparar), recibe daño al **HHP del barco** (fuego en casco). Pasar Turno NO activa Quemadura
   - Veneno: daña HP de **1 crew member aleatorio vivo** (% HP max del crew). Si no hay crew viva, no hace daño
3. **Fin de ronda**: Verificar condiciones de victoria/derrota. Si ninguna se cumple, nueva ronda

**3. Acciones del Turno (Barco Aliado)**

Cuando es el turno del barco aliado, el jugador elige una de estas 5 acciones (+1):

| Acción | Target | Costo | Efecto |
|--------|--------|-------|--------|
| **Cañonazo** | Single-enemy | Gratis | Ataque básico naval. Usa FPW del barco. AbilityPower 1.0, elemento Neutral. Daña HHP del target |
| **Habilidad Naval** | Varía | MP | Usa una habilidad del pool del barco (BaseAbilities + SeaAbilities de la crew). Cada habilidad define su target type, costo MP, elemento, y efectos |
| **Maniobra Evasiva** | Self | Gratis | Reduce daño recibido en un 50% (tanto al casco HHP como a crew por Abordaje) hasta el siguiente turno del barco. Equivalente naval de Guardia |
| **Abordaje** | Single-crew-enemy | Gratis | Ataca directamente a un crew member del barco enemigo. Usa FPW del barco vs DEF de la unidad objetivo. Al matar un crew member, el barco enemigo recalcula stats inmediatamente |
| **Reparar** | Self | MP | Cura HHP del barco. `HealAmount = floor(MST_efectivo × REPAIR_POWER × BuffMod)`. Siempre disponible sin necesitar habilidad de heal. Costo: `REPAIR_MP_COST` (fijo) |
| **Pasar Turno** | — | Gratis | No realiza acción. No activa Quemadura. Misma mecánica que en terrestre |

**Notas sobre acciones:**
- Cañonazo es equivalente a Ataque Normal terrestre (siempre disponible, gratis, Neutral)
- Abordaje solo es posible si el barco enemigo tiene crew visible (Ships con crew). Criaturas marinas NO tienen crew (no se puede abordar un Kraken)
- Maniobra Evasiva protege tanto casco como crew. Se retira al inicio del siguiente turno del barco
- Reparar gasta MP para curar HHP. No cura crew members — una vez muertos, muertos para el resto de la batalla
- Habilidades navales pueden tener TargetType que apunte a crew enemy (habilidades de tipo "abordaje mejorado"), al barco entero, AoE a todos los enemigos, etc.

**4. Acciones del Turno (Enemigos)**

Los enemigos navales usan el mismo sistema de AI tiers del Enemy System GDD:

- **Barcos Normal**: AI profile (Agresivo, Estratega, etc.). Eligen entre Cañonazo, Habilidad, y Abordaje según perfil. Los barcos Normal **sí pueden abordar** crew del jugador
- **Barcos Elite**: Profile+ con condicionales. Pueden priorizar abordar al Capitán o al crew member que aporta más habilidades
- **Barcos Jefe**: Behavior tree por fase. Pueden alternar entre bombardeo al casco y abordajes tácticos. Tienen habilidades únicas. Pueden usar Limit Break
- **Criaturas Marinas**: Solo usan Cañonazo-equivalente (mordida/tentáculo) y Habilidades. **No pueden abordar** (no tienen crew para enviar). No se les puede abordar
- Los enemigos NO usan Maniobra Evasiva ni Reparar (los enemigos se diseñan con HP suficiente y habilidades defensivas en vez de acciones genéricas de defensa)

**5. Limit Break Naval**

Funciona igual que en terrestre con adaptaciones:
- Las SeaAbilities de la crew pueden tener `CanLimitBreak: true` con sus propias condiciones LB
- Cuando una habilidad naval activa LB, el **barco** recibe un turno extra (se reinserta en la Initiative Bar)
- Max 1 LB por barco por ronda (flag `LBUsedThisRound`)
- En el turno extra, el jugador puede elegir cualquiera de las 5 acciones + Pasar Turno
- Si el crew member que aportó la habilidad LB muere antes de que se use, la habilidad desaparece del pool (no puede activar LB)

**6. Crew Members en Combate**

Los crew members no actúan individualmente — son una "extensión" del barco. Pero son atacables:

**Crew HP por Rol:**

| Rol | Crew HP | Justificación |
|-----|---------|---------------|
| Capitán | 800 | El más resistente — target de mayor valor, difícil de eliminar |
| Intendente | 600 | Medio-alto — gestiona recursos, moderadamente protegido |
| Artillero | 400 | Bajo — potencia ofensiva a cambio de fragilidad. Target atractivo para Abordaje |
| Navegante | 500 | Medio — esquiva proyectiles pero no es tanque |
| Carpintero | 700 | Alto — refuerza el casco, naturalmente resistente |
| Cirujano | 500 | Medio — soporte vital pero no tanque |
| Contramaestre | 600 | Medio-alto — disciplina y resistencia |

**Reglas de crew en combate:**
- Crew HP es **fijo por rol**, no depende del nivel o stats de la unidad asignada. Esto simplifica el balance naval y mantiene la diferenciación: el valor de un crew member viene de sus stats y habilidades contribuidas, no de su supervivencia individual
- Cuando un crew member llega a 0 HP, muere. El barco **recalcula stats inmediatamente**: pierde la contribución del crew muerto (stats, habilidades, trait count)
- Si el crew muerto era el Capitán → sinergias aliadas se desactivan (ver Traits/Sinergias GDD)
- Si el crew muerto era el segundo Capitán (guest amigo) → solo las sinergias del segundo Capitán se desactivan
- Los crew members muertos **no pueden ser revividos** en combate naval. La pérdida es permanente durante la batalla
- Los crew members muertos **se restauran** al terminar el combate (igual que el barco)
- Los crew members **NO** reciben daño por ataques al casco (Cañonazo, habilidades que dañan HHP). Solo mueren por Abordaje directo o habilidades con TargetType = crew

**7. Oleadas**

Mismo modelo que terrestre:
- El Stage System define oleadas con enemigos navales
- El estado del barco aliado (HHP, MP, buffs, crew HP/muertos) **persiste entre oleadas**
- Los enemigos de cada oleada entran con stats completos y crew completa
- La Initiative Bar se recalcula completamente al inicio de cada oleada
- Las sinergias enemigas se recalculan con el capitán de la nueva oleada

**8. Victoria y Derrota**

- **Victoria**: Todos los enemigos de todas las oleadas eliminados (HHP = 0)
- **Derrota**: El barco aliado se hunde (HHP = 0)
- Si toda la crew aliada muere pero el barco tiene HHP > 0, el barco **sigue en combate** con solo BaseAbilities y base stats (sin crew contributions). Es una situación desesperada pero no automáticamente una derrota
- Un barco enemigo se derrota cuando su HHP = 0. Matar a toda su crew no lo hunde directamente, pero lo debilita enormemente (pierde stats/habilidades → fácil de hundir después)
- Criaturas marinas solo se derrotan por HHP = 0 (no tienen crew)

### States and Transitions

**Estados del Combate Naval**

| Estado | Descripción | Transiciones |
|--------|-------------|-------------|
| `PreCombat` | Calculando stats efectivos, activando sinergias, inicializando IB | → `InRound` |
| `InRound` | Ronda activa — entidades actúan en orden de IB | → `WaveTransition` (última entidad enemiga eliminada), → `Victory` (última oleada completada), → `Defeat` (barco aliado HHP = 0) |
| `WaveTransition` | Oleada completada, preparando siguiente: nueva oleada desplegada, IB recalculada, sinergias enemigas re-evaluadas | → `InRound` |
| `Victory` | Todas las oleadas completadas. Pantalla de resultados y recompensas | → (fin de combate) |
| `Defeat` | Barco aliado hundido (HHP = 0). Opciones: Reintentar o Salir | → (fin de combate) |

**Estados del Crew Member (runtime)**

| Estado | Descripción | Transiciones |
|--------|-------------|-------------|
| `Active` | Vivo, contribuyendo stats/habilidades/traits al barco | → `Dead` (crew HP = 0 por Abordaje) |
| `Dead` | Eliminado. No contribuye nada. No puede ser revivido en combate | → `Active` (post-combate, restauración automática) |

**Nota**: A diferencia del combate terrestre, no hay estado `Guarding` para crew members individuales — la Maniobra Evasiva es un estado del barco, no de la crew.

### Interactions with Other Systems

| Sistema | Dirección | Datos que fluyen | Interfaz |
|---------|-----------|-----------------|----------|
| **Ship Data Model** | SDM → CN | Stats efectivos del barco (base + upgrades + crew + traits), pool de habilidades (BaseAbilities + SeaAbilities crew), configuración de role slots, crew assignments | CN lee stats al inicio del combate y **recalcula** cada vez que un crew member muere. SDM provee la fórmula de EffectiveStat. MP es el recurso naval; MST es stat de daño mágico |
| **Damage & Stats Engine** | DSE ↔ CN | Fórmulas de daño naval (FPW vs HDF, MST vs RSL), buffs/debuffs, status effects, orden de procesamiento de turno | CN usa las mismas fórmulas que CT pero con stats navales. CritMod = 1.0 siempre. Ships inmunes a Sueño/Aturdimiento/Muerte |
| **Initiative Bar** | IB ↔ CN | Orden de turnos por SPD efectivo del barco, reinserción LB, reinicio de oleada | Misma interfaz que CT. Cada barco/entidad es un "combatant" en la IB |
| **Enemy System** | ES → CN | EnemyData naval (NavalForm: Ship/Creature), tiers, AI profiles/behavior trees, boss phases, loot tables, IsEncounterCaptain | CN spawns enemigos navales por oleada. Creatures usan stat mapping ATK→FPW etc. |
| **Traits/Sinergias** | TS ↔ CN | Sinergias del Capitán naval + segundo Capitán (guest amigo). TraitBonuses como flat additive al ship stat | CN llama a TS al inicio de combate y al KO/revive de Capitanes. TS devuelve bonificaciones |
| **Team Composition** | TC → CN | Tripulación naval final: barco + unidades asignadas a roles + guest, flag de segundo Capitán | CN recibe el preset naval como input. TC valida que el slot Capitán está ocupado antes de permitir entrar |
| **Stage System** | SS → CN | BattleData naval: oleadas, enemigos por oleada, condiciones de misión, tipo de stage | CN usa esta data para configurar el combate |
| **Save/Load System** | CN → S/L | Nada directamente — CN no persiste estado entre sesiones. El resultado (victoria/derrota) se comunica al Stage System y Rewards System |
| **Rewards System** | CN → RS | Resultado del combate, enemigos eliminados con loot tables, misiones completadas | RS calcula recompensas post-combate |
| **Combat UI** | CN → CUI | Eventos: turno, daño (casco y crew), muerte crew, oleada, LB, sinergias activas/desactivadas, stats del barco actualizados | CUI renderiza todo el estado visual del combate naval |
| **Auto-Battle** | CN ↔ AB | CN notifica a AB: turno del barco, 5 acciones, MP actual, HHP actual, habilidades disponibles, enemigos con HHP. AB devuelve: acción + target. CN la ejecuta | AB nunca devuelve Abordaje ni Maniobra — decisiones tácticas reservadas al jugador. Prioriza: Reparar si HHP < 30% → habilidad mayor AP → Cañonazo. Ver [auto-battle.md](auto-battle.md) |

## Formulas

**1. Daño Naval (Cañonazo / Habilidades)**

Idéntico al DSE §2, usando stats navales:
```
Físico naval: AttackStat = FPW, DefenseStat = HDF
Mágico naval: AttackStat = MST, DefenseStat = RSL

RawDamage = max((EffAttackStat × ATK_MULT) - (EffDefenseStat × DEF_MULT), 1)
FinalDamage = max(floor(RawDamage × AbilityPower × ElementMod × 1.0 × Variance), 1)
```
- CritMod = 1.0 siempre (ships no critean)
- ATK_MULT y DEF_MULT son los mismos valores que en terrestre (definidos en DSE)
- Variance = 0.95–1.05

**2. Daño de Abordaje**

```
AbordajeDamage = max((EffFPW × ATK_MULT) - (CrewDEF × DEF_MULT), 1)
FinalAbordaje = max(floor(AbordajeDamage × BOARDING_POWER × ElementMod × Variance), 1)
```

| Variable | Definición | Valor |
|----------|-----------|-------|
| `EffFPW` | FPW efectivo del barco atacante (con buffs) | Calculado por SDM |
| `CrewDEF` | DEF de la unidad asignada al rol atacado (stat individual del UDM) | Varía por unidad |
| `BOARDING_POWER` | Multiplicador base del Abordaje | 0.8 (menor que Cañonazo para compensar que ataca crew, no casco) |
| `ElementMod` | Elemento del barco atacante vs elemento de la unidad crew | 0.75 / 1.0 / 1.25 |

**Nota**: El Abordaje usa FPW del barco contra la DEF de la unidad crew objetivo. ElementMod usa el elemento del **barco atacante** vs. el elemento de la **unidad crew** (su elemento individual del UDM, no el del barco enemigo). Barcos con alto FPW son efectivos tanto bombardeando como abordando.

**3. Reparar (Healing del casco)**

```
RepairAmount = floor(EffMST × REPAIR_POWER × BuffMod)
RepairAmount = min(RepairAmount, MaxHHP - CurrentHHP)  // no exceder MaxHHP
```

| Variable | Definición | Valor |
|----------|-----------|-------|
| `EffMST` | MST efectivo del barco (con buffs) | Calculado por SDM |
| `REPAIR_POWER` | Multiplicador base de Reparar | 1.5 |
| `REPAIR_MP_COST` | Costo fijo de MP para usar Reparar | 20 |
| `BuffMod` | Multiplicador de buffs/debuffs activos en healing | Según DSE §7 |

**4. Maniobra Evasiva**

```
DamageAfterManiobra = floor(IncomingDamage × MANIOBRA_REDUCTION)
```

| Variable | Definición | Valor |
|----------|-----------|-------|
| `MANIOBRA_REDUCTION` | Factor de reducción de daño | 0.50 (50% reducción) |

Aplica a: daño al casco (Cañonazo, habilidades) Y daño a crew por Abordaje.
NO aplica a: daño de status effects (Sangrado, Quemadura, Veneno) — misma regla que Guardia terrestre.

**5. Healing Naval (Habilidades de Curación)**

Para habilidades de curación en el pool del barco:
```
HealAmount = floor(EffMST × HealPower × BuffMod)
```
- MST del barco es el stat base de curación naval (mismo nombre que MST en terrestre)
- HealPower es el multiplicador de la habilidad (como en DSE §7)
- Healing naval restaura **HHP del barco** (no crew HP). Crew members no pueden ser curados en combate naval — su HP solo baja, nunca sube
- Healing no puede exceder MaxHHP

**6. Worked Example**

```
Barco aliado: Sloop (FPW efectivo: 193, MST efectivo: 120, HDF: 150)
vs. Fragata enemiga (HHP: 3000, HDF: 200, Artillero crew DEF: 80)

--- Cañonazo al casco ---
RawDamage = max((193 × 1.0) - (200 × 0.5), 1) = max(93, 1) = 93
FinalDamage = floor(93 × 1.0 × 1.0 × 1.0 × 1.02) = 94
Fragata HHP: 3000 → 2906

--- Abordaje al Artillero ---
AbordajeDamage = max((193 × 1.0) - (80 × 0.5), 1) = max(153, 1) = 153
FinalAbordaje = floor(153 × 0.8 × 1.0 × 0.98) = 119
Artillero HP: 400 → 281

--- Reparar (barco aliado recibió 250 daño) ---
RepairAmount = floor(120 × 1.5 × 1.0) = 180
Costo: 20 MP. MP actual: 150 → 130
HHP aliado restaura 180 puntos

--- Maniobra Evasiva activa, enemigo Cañonea ---
Daño base enemigo: 200
DamageAfterManiobra = floor(200 × 0.50) = 100
```

## Edge Cases

1. **Toda la crew aliada muere pero el barco tiene HHP > 0**: El barco sigue en combate con solo BaseAbilities y base stats. Reparar sigue disponible si tiene MP. Veneno y Sangrado ya no hacen daño (no hay crew viva).

2. **Abordaje a crew de criatura marina**: No permitido. Las criaturas no tienen crew. El botón de Abordaje aparece deshabilitado contra criaturas.

3. **Abordaje mata al Capitán enemigo**: Sinergias enemigas se desactivan inmediatamente. Stats del barco enemigo se recalculan (pierde contribución del Capitán). Doble debilitamiento.

4. **Abordaje a crew member con Maniobra Evasiva activa (barco enemigo)**: La Maniobra del barco enemigo **sí** reduce el daño del Abordaje en 50%.

5. **MP se agota (0 MP)**: El barco no puede usar Habilidades ni Reparar. Solo Cañonazo, Maniobra, Abordaje y Pasar Turno.

6. **Crew muerto aportaba la única habilidad de curación**: Se pierde del pool. El jugador depende de Reparar (si tiene MP).

7. **Habilidad naval con LB condición OnKill — matar crew por Abordaje**: Matar crew SÍ cuenta como kill para OnKill.

8. **DoTs en combate naval**: Quemadura daña HHP del barco (fuego en casco). Veneno y Sangrado dañan HP de 1 crew member aleatorio vivo. Maniobra Evasiva NO reduce daño de ningún DoT.

9. **Veneno/Sangrado cuando toda la crew está muerta**: No hace daño. El efecto persiste (duración sigue decrementando) pero no hay target válido.

10. **Barco enemigo sin crew (todos muertos)**: El barco sigue activo con base stats + upgrades. Solo usa BaseAbilities. Veneno/Sangrado sobre él no hacen daño adicional.

11. **Quemadura + Pasar Turno en naval**: Pasar Turno NO activa Quemadura (misma regla que terrestre).

12. **Guest crew (amigo) muere por Abordaje**: Sinergias secundarias se desactivan. Stats del barco se recalculan. No se puede revivir.

13. **Oleada nueva con crew muertos**: Los crew muertos persisten entre oleadas. El barco entra debilitado. Crew NO se restaura entre oleadas.

14. **Silencio aplicado al barco**: No puede usar Habilidades. Reparar es acción base (NO habilidad) → **sí se puede usar bajo Silencio**. Cañonazo, Maniobra y Abordaje también disponibles.

15. **Ceguera aplicada al barco**: Ataques físicos (Cañonazo, Abordaje) tienen 50% de fallar (MISS, daño = 0). Habilidades mágicas (MST-based) no afectadas. Mismo valor que en terrestre (ver DSE §6).

16. **Sangrado mata a un crew member al inicio del turno**: Si el crew muerto era el Capitán, sinergias se desactivan antes de que el barco actúe este turno. Stats se recalculan inmediatamente.

## Dependencies

### Dependencias Upstream (CN depende de)

| Sistema | Tipo | Interfaz | GDD |
|---------|------|----------|-----|
| **Ship Data Model** | Hard | Stats efectivos del barco, role slots, crew assignments, BaseAbilities, fórmula EffectiveStat. MP es el recurso naval para habilidades (MST es stat de daño mágico) | ✅ Approved |
| **Damage & Stats Engine** | Hard | Fórmulas de daño naval (FPW vs HDF, MST vs RSL), buffs/debuffs, status effects, healing. CritMod = 1.0. Ships inmunes a Sueño/Aturdimiento/Muerte. Naval DoT split documentado en DSE §6 | ✅ Approved |
| **Initiative Bar** | Hard | Orden de turnos por SPD del barco, reinserción LB, reinicio oleada | ✅ Approved |
| **Enemy System** | Hard | EnemyData naval (NavalForm: Ship/Creature), tiers, AI, boss phases, IsEncounterCaptain | ✅ Approved |
| **Traits/Sinergias** | Hard | Sinergias del Capitán naval + segundo Capitán. TraitBonuses como flat additive al ship stat | ✅ Approved |
| **Team Composition** | Hard | Preset naval: barco + crew + guest, flag segundo Capitán. Valida Capitán obligatorio | ✅ Approved |
| **Stage System** | Hard | BattleData naval: oleadas, enemigos, condiciones de misión | ✅ Approved |

### Dependencias Downstream (dependen de CN)

| Sistema | Tipo | Qué necesita de CN | GDD |
|---------|------|---------------------|-----|
| **Auto-Battle** | Hard | Interfaz de acciones para IA automática. IA naval prioriza: Reparar si HHP < 30% → habilidad mayor AP → Cañonazo. NO usa Abordaje ni Maniobra (decisiones tácticas reservadas al jugador). Target: menor HHP absoluto | ✅ Designed |
| **Combat UI** | Hard | Eventos de combate naval: daño casco, daño crew, muerte crew, oleada, LB, stats actualizados, DoT targets | ⬜ Not Started |

### Cross-System Updates Necesarios

- ~~**Ship Data Model**: Renombrar PWD → MST (Mística) en todas las referencias~~ ✅ DONE
- ~~**Damage & Stats Engine**: Documentar que en naval, Veneno/Sangrado dañan crew (no barco) y Quemadura daña HHP~~ ✅ DONE (added naval CritMod + DoT split to DSE §6)
- ~~**Enemy System**: PWD → MST en stat mappings~~ ✅ DONE
- ~~**Traits/Sinergias**: Verificar que el stat naval buffado para "Malditos" sea MST (no PWD)~~ ✅ DONE
- **Enemy System**: Verificar que AI profiles para barcos incluyan Abordaje como acción posible — pendiente verificar

## Tuning Knobs

| Knob | Valor Actual | Rango Seguro | Afecta a | Notas |
|------|-------------|-------------|----------|-------|
| `BOARDING_POWER` | 0.8 | 0.5–1.2 | Daño del Abordaje. Muy bajo: Abordaje nunca vale la pena vs Cañonazo. Muy alto: siempre conviene abordar | Debe ser atractivo pero no dominante. El jugador debe elegir entre hundir y abordar |
| `REPAIR_POWER` | 1.5 | 1.0–3.0 | Cantidad de HHP curado por Reparar. Muy bajo: Reparar es inútil. Muy alto: barco es inmortal | Interactúa con REPAIR_MP_COST — curar mucho pero caro puede balancearse |
| `REPAIR_MP_COST` | 20 | 10–50 | Costo de MP por Reparar. Muy bajo: spam Reparar sin consecuencia. Muy alto: nunca vale la pena Reparar | Debe competir con el costo de Habilidades por MP |
| `MANIOBRA_REDUCTION` | 0.50 | 0.30–0.70 | Reducción de daño por Maniobra. Misma lógica que GUARD_REDUCTION en terrestre | Protege casco Y crew |
| Crew HP por Rol | Ver §6 | Varía | Cuánto tardan en morir los crew members por Abordaje. HP bajo = Abordaje muy fuerte. HP alto = Abordaje débil | Los roles ofensivos (Artillero) deben ser frágiles. Los defensivos (Carpintero, Capitán) resistentes |
| `MAX_WAVES_PER_NAVAL_BATTLE` | 3 | 1–5 | Duración de batalla naval. Menos oleadas que terrestre por defecto (naval stages más cortos) | El Stage System define las oleadas, CN las ejecuta |
| `NAVAL_DOT_CREW_PERCENT` | 5% (same as terrestre DOT_PERCENT) | 2–15% | % del HP max del crew member que pierde por tick de Veneno/Sangrado. Muy alto: crew muere rápido por DoTs. Muy bajo: DoTs ignorables | Quemadura usa DOT_PERCENT estándar (5%) contra MaxHHP del barco. Veneno/Sangrado usan este valor contra MaxHP del crew member objetivo |

### Knob Interactions

| Knob A | Knob B | Interacción |
|--------|--------|-------------|
| BOARDING_POWER | Crew HP por Rol | Si el daño de Abordaje es alto Y el HP de crew es bajo, crew muere en 1-2 golpes. Puede hacer Abordaje demasiado dominante |
| REPAIR_POWER | REPAIR_MP_COST | Curación vs costo. Si ambos son altos, Reparar es poderoso pero caro (buen equilibrio). Si cura mucho y cuesta poco, rompe el balance |
| REPAIR_MP_COST | Costo de Habilidades | Si Reparar cuesta mucho MP, compite directamente con usar habilidades ofensivas. Crea la tensión "¿me curo o ataco?" |
| Crew HP | NAVAL_DOT_CREW_PERCENT | Si HP de crew es bajo Y DoT % es alto, los DoTs en naval son devastadores (matan crew sin necesidad de abordar) |

## Visual/Audio Requirements

**Visual**
- Escenario de fondo marino: océano abierto, zona costera, tormenta, arrecifes según la Scene del Stage System. Parallax con olas en movimiento
- Barco aliado posicionado a la izquierda, enemigos a la derecha (consistente con terrestre)
- Crew members visibles como sprites pequeños en las posiciones de rol del barco (ver SDM Visual Requirements)
- Crew members muertos: slot vacío con indicador visual (X roja o silueta gris)
- Animaciones mínimas por barco: idle (mecerse), cañonazo (retroceso), habilidad (efecto), daño recibido (impacto), hundimiento
- Animación de Abordaje: breve visual de figura saltando entre barcos (no necesita ser el sprite del crew específico)
- Feedback de daño: números flotantes sobre el casco (blanco) y sobre crew members abordados (rojo)
- Indicador visual de Maniobra Evasiva: escudo/aura azul alrededor del barco
- Indicador visual de Reparar: efecto de martillo/madera sobre el casco
- Indicador visual de crew HP: mini-barras de vida junto a cada crew member en el barco
- Efecto de Quemadura: llamas en el casco del barco
- Efecto de Veneno/Sangrado: efecto sobre el crew member afectado (no sobre el barco)
- Transición de oleada: similar a terrestre (fade/barrido, nuevos enemigos navegan hacia la pantalla)

**Audio**
- Música de combate naval diferente a terrestre: más épica, percusión de tambores de guerra, ambiente oceánico
- SFX de Cañonazo: explosión de cañón + impacto en madera
- SFX de Abordaje: sonido de garfios/cuerdas + espadas (breve)
- SFX de Maniobra: velas tensándose + agua
- SFX de Reparar: martilleo + crujido de madera
- SFX de crew muerto: grito breve + splash (cae al agua)
- SFX de hundimiento: crujido masivo + agua entrando + música tensa
- SFX de Quemadura en barco: fuego crepitando (distinto al terrestre)
- SFX de victoria naval: cuerno de guerra + celebración de crew

## UI Requirements

- **Initiative Bar**: Misma barra horizontal que terrestre pero con iconos de barcos/criaturas en vez de unidades. Icono del barco aliado con miniatura de barco
- **Panel de acciones naval (5 botones + Pasar)**: Diseño premium con identidad pirata naval (timón, carta náutica). Los 5 botones: Cañonazo (icono cañón), Habilidad (icono ancla/libro), Maniobra (icono timón), Abordaje (icono garfio), Reparar (icono martillo). Pasar Turno como botón secundario
- **Sub-menú de habilidades navales**: Similar al terrestre — cartas con icono, nombre, elemento, costo MP, descripción. Habilidades con LB marcadas con borde especial
- **Vista de crew del barco aliado**: Panel lateral o superpuesto mostrando: cada crew member con rol, retrato, mini-barra HP, estado (vivo/muerto). Crew muertos atenuados
- **Targeting de Abordaje**: Al seleccionar Abordaje, los crew members enemigos visibles se resaltan como targets. Cada target muestra: nombre, rol, HP actual, DEF. Tap para seleccionar
- **Targeting de barco**: Al seleccionar Cañonazo o habilidad single-target, los barcos/criaturas enemigos se resaltan. Muestra: HHP bar, elemento, buffs/debuffs
- **Stats del barco en tiempo real**: Panel compacto mostrando stats efectivos del barco aliado. Se actualiza visualmente cuando un crew member muere (flash rojo en stats que bajan)
- **Indicador de MP**: Barra de recurso visible (misma lógica que MP en terrestre). Se gasta al usar Habilidades y Reparar
- **Vista de crew enemigo**: Al tap en barco enemigo, se despliega lista de crew con rol, HP, estado. Permite planificar Abordajes
- **Contador de oleada**: "Oleada X/Y" visible
- **Pantalla de victoria naval**: Resumen con animación de recompensas + stats de la batalla (crew sobrevivientes, HHP restante)
- **Pantalla de derrota**: Animación de hundimiento + opciones Reintentar/Salir

## Acceptance Criteria

**Flujo de Batalla**
1. Una batalla naval se inicia correctamente con datos del Stage System (barco aliado, oleadas enemigas)
2. Stats efectivos del barco se calculan correctamente: BaseStat + UpgradeBonus + CrewContribution + TraitBonuses
3. La batalla transiciona por todos los estados: PreCombat → InRound → WaveTransition → Victory/Defeat
4. Las oleadas transicionan correctamente, reiniciando la Initiative Bar
5. Victoria al hundir la última oleada; Derrota al hundirse el barco aliado (HHP = 0)

**Acciones del Jugador**
6. El jugador puede seleccionar: Cañonazo, Habilidad, Maniobra Evasiva, Abordaje, Reparar, Pasar Turno
7. Cañonazo usa FPW, AbilityPower 1.0, elemento Neutral, daña HHP enemigo
8. Habilidades navales usan MP como recurso y funcionan según su definición
9. Maniobra Evasiva reduce daño recibido (casco Y crew) en 50% hasta siguiente turno
10. Maniobra NO reduce daño de status effects (Quemadura, Veneno, Sangrado)
11. Abordaje permite seleccionar un crew member enemigo como target. Usa FPW vs DEF del crew
12. Abordaje deshabilitado contra criaturas marinas (no tienen crew)
13. Reparar cura HHP del barco gastando MP. Disponible bajo Silencio (es acción base, no habilidad)
14. Pasar Turno no activa Quemadura

**Crew System**
15. Cada crew member tiene HP fijo por rol (Capitán 800, Artillero 400, etc.)
16. Al morir un crew member, el barco recalcula stats inmediatamente (pierde contribución)
17. Al morir un crew member, sus SeaAbilities desaparecen del pool del barco
18. Crew members muertos NO se pueden revivir en combate
19. Crew members muertos persisten entre oleadas (no se restauran)
20. Crew members NO reciben daño por ataques al casco (solo por Abordaje/DoTs crew)

**DoTs Naval**
21. Quemadura daña HHP del barco (fuego en casco), activada solo si realizó acción
22. Veneno daña HP de 1 crew aleatorio vivo al final del turno
23. Sangrado daña HP de 1 crew aleatorio vivo al inicio del turno
24. Si no hay crew viva, Veneno y Sangrado no hacen daño

**Enemigos**
25. Barcos enemigos pueden abordar crew del jugador (bidireccional)
26. Criaturas marinas no pueden abordar ni ser abordadas
27. Barcos Jefe siguen behavior trees con fases de HP y pueden usar LB
28. Ships y criaturas son inmunes a Sueño, Aturdimiento y Muerte

**Sinergias**
29. El Capitán naval (crew en slot Capitán) activa sinergias correctamente
30. Guest amigo funciona como segundo Capitán con doble activación
31. Matar al Capitán por Abordaje desactiva sinergias inmediatamente
32. Matar al Capitán enemigo por Abordaje desactiva sinergias enemigas

**Limit Break**
33. SeaAbilities con CanLimitBreak activan turno extra del barco (no del crew individual)
34. Max 1 LB por barco por ronda
35. Si el crew que aportó la habilidad LB muere, la habilidad desaparece del pool

**Persistencia**
36. Estado del barco (HHP, MP, buffs, crew muertos) persiste entre oleadas
37. Todo se restaura al terminar el combate (barco y crew vuelven a estado completo)

## Open Questions

1. **Evolución a Grid FE post-demo**: El combate naval de la demo es turn-based. Para el juego completo, se evaluará migrar a un sistema de grid táctico estilo Fire Emblem con movimiento y posicionamiento. Esta decisión se tomará tras la demo basándose en feedback de jugadores. El diseño actual es compatible con esa evolución (stats, fórmulas, crew system se mantienen). *Owner: Game Designer / Creative Director. Target: Post-demo.*

2. **Mecánicas adicionales (municiones, viento, embestida)**: Descartadas para la demo para mantener scope. Se evaluarán para el juego completo independientemente de si se migra a grid. *Owner: Game Designer. Target: Post-demo.*

3. **Crew HP por rol vs. por unidad**: Se optó por HP fijo por rol para simplificar balance naval. ¿Se debería reconsiderar si jugadores piden que el nivel de la unidad afecte su supervivencia en naval? *Owner: Systems Designer. Target: Post-demo si hay feedback.*

4. **¿Pueden las habilidades navales revivir crew members?**: Actualmente crew muertos no se reviven en combate. ¿Se debería añadir una habilidad de "rescate" como excepción? Podría crear builds interesantes. *Owner: Game Designer. Target: Durante balanceo de habilidades.*

5. **Balance de Abordaje vs. Bombardeo**: ¿Cómo asegurar que ambas estrategias sean viables sin que una domine? Necesita playtesting extensivo. BOARDING_POWER (0.8) + crew HP por rol son los knobs principales. *Owner: Systems Designer. Target: Prototyping.*

6. **Fortresses como enemy type**: Enemy System las define pero están marcadas como post-demo. Si se incluyen en la demo, necesitan reglas especiales: inmóviles, no abordables, habilidades de bombardeo de área. *Owner: Game Designer. Target: Scope decision.*

7. ~~**¿Cómo interactúa Silencio con Reparar?**~~: **RESUELTO** — Reparar es acción base (no habilidad), Silencio NO la bloquea. Documentado en Edge Case #14.
