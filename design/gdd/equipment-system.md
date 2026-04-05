# Equipment System

> **Status**: Designed
> **Author**: User + Claude Code Game Studios agents
> **Last Updated**: 2026-04-05
> **Implements Pillar**: Pillar 1 (Profundidad Estratégica Dual), Pillar 3 (Recompensa a la Paciencia)

## Overview

El Equipment System gestiona los objetos equipables que potencian las stats de
las unidades. Cada unidad tiene 3 slots (Weapon, Armor, Accessory) y cada pieza
de equipo proporciona bonificaciones de stats, y opcionalmente habilidades bonus.

El equipo tiene rareza (1★ a 5★), puede subir de nivel gastando materiales y
DOB, y se obtiene de cuatro fuentes: drops de stages, crafting con materiales,
compra en tienda de DOB, y recompensas por duplicados de personajes.

El sistema añade una capa de personalización sobre la progresión de unidades:
dos jugadores con la misma unidad 5★ al mismo nivel pueden tener rendimientos
muy distintos según su equipo. Para la demo, el sistema incluye un catálogo de
~20-30 items distribuidos entre las tres categorías y cinco rarezas, suficiente
para que el jugador sienta que sus decisiones de equipamiento importan sin
necesitar un catálogo masivo.

El equipo afecta solo a combate terrestre de forma directa (stats de la unidad).
En combate naval, las stats de la tripulación ya se procesan de forma diferente
(según el Ship Data Model), pero el equipo de cada tripulante sigue contribuyendo
a sus stats base que alimentan los bonuses de rol naval.

## Player Fantasy

**"Esta espada es lo que me hace diferente."** El equipo es la expresión más
directa de las decisiones del jugador. Dos capitanes con la misma tripulación
pueden jugar de formas completamente distintas según cómo equiparon a sus
unidades — uno apostó por velocidad y críticos, el otro por tanqueo y
supervivencia.

El sistema debe generar tres emociones:
- **Descubrimiento**: "Este accesorio da +CRI y una habilidad de fuego — perfecto
  para mi unidad de Pólvora". El jugador busca combinaciones entre equipo, stats,
  y traits.
- **Progresión tangible**: Subir de nivel un arma y ver los números crecer es
  satisfacción inmediata. A diferencia del leveling de unidades (que es gradual),
  un upgrade de equipo puede cambiar cómo se siente una unidad en combate de un
  momento a otro.
- **Inversión con peso**: Un arma 5★ a nivel máximo representa horas de farmeo
  y decisiones de recurso. Moverla de una unidad a otra debería sentirse como una
  decisión estratégica, no como algo trivial.

El sistema falla si el equipo óptimo es obvio (siempre la misma arma para todos),
si las diferencias entre rarezas de equipo son tan grandes que las armas 1-3★ son
basura instantánea, o si el jugador nunca tiene que tomar una decisión real sobre
a quién equipar qué.

## Detailed Rules

### 1. Slots y Categorías

Cada unidad tiene exactamente **3 slots**:

| Slot | Categoría | Subtipos temáticos | Función principal |
|------|-----------|-------------------|-------------------|
| **Weapon** | Armas de pirata | Sables/Cutlass, Pistolas de chispa, Mosquetes, Dagas/Cuchillos de abordaje, Hachas de abordaje | ATK, habilidades ofensivas |
| **Weapon** | Armas místicas | Bastones voodoo, Huesos tallados, Relicarios malditos | MST, habilidades mágicas |
| **Armor** | Protección pirata | Casacas de capitán, Cueros curtidos, Cotas remendadas, Gabardinas enceradas | HP, DEF, SPR |
| **Accessory** | Amuletos y reliquias | Amuletos voodoo, Calaveras parlantes, Brújulas encantadas, Monedas malditas, Ojos de cristal, Mapas del más allá | Stats variados, CRI, LCK, SPD, habilidades |

**Weapon Types**: `Blade` (sables, cutlass), `Firearm` (pistolas, mosquetes),
`Voodoo` (bastones, huesos, relicarios), `Dagger` (dagas, cuchillos),
`Heavy` (hachas de abordaje).

**Compatibilidad**: Cada unidad tiene un `WeaponType` en su UnitData que define
qué tipos de arma puede equipar. Armor y Accessory son **universales** —
cualquier unidad puede equipar cualquiera.

### 2. Equipment Data Model

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `EquipId` | string | Identificador único (e.g., `"rusty_machete"`) |
| `DisplayName` | string | Nombre visible |
| `Description` | string | Texto descriptivo/lore |
| `Category` | enum | Weapon, Armor, Accessory |
| `WeaponType` | enum? | Solo para Weapons: Blade, Firearm, Voodoo, Dagger, Heavy |
| `Rarity` | enum | 1★, 2★, 3★, 4★, 5★ |
| `MaxLevel` | int | Nivel máximo (varía por rareza) |
| `BaseStats` | StatBonusBlock | Bonificaciones de stats a nivel 1 |
| `StatGrowth` | StatBonusBlock | Crecimiento de stats por nivel |
| `BonusAbility` | AbilityId? | Habilidad otorgada al equipar (null = ninguna) |
| `BonusAbilityContext` | enum? | Land, Sea, Both — en qué combate aplica la habilidad |
| `CraftRecipe` | CraftRecipe? | Si es crafteable: materiales requeridos (null = no crafteable) |
| `SellPrice` | int | DOB obtenidos al vender |

**StatBonusBlock** — los stats que el equipo puede modificar:

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `HP` | int | Bonus plano a HP |
| `ATK` | int | Bonus plano a ATK |
| `DEF` | int | Bonus plano a DEF |
| `MST` | int | Bonus plano a MST |
| `SPR` | int | Bonus plano a SPR |
| `SPD` | int | Bonus plano a SPD |
| `CRI` | float | Bonus a Critical Rate (porcentual, e.g., 0.05 = +5%) |
| `LCK` | int | Bonus plano a LCK |

> Bonuses son siempre **flat** (planos). No hay bonuses porcentuales a stats
> principales para la demo — simplifica el cálculo y evita scaling exponencial.
> Excepción: CRI es inherentemente porcentual.

### 3. Rareza de Equipo

| Rareza | MaxLevel | Stat Budget (relativo) | Bonus Ability | Fuentes principales |
|--------|----------|----------------------|---------------|---------------------|
| 1★ | 10 | 1.0x | Nunca | Drops historia early, tienda DOB |
| 2★ | 15 | 1.5x | Nunca | Drops historia/eventos, tienda DOB |
| 3★ | 20 | 2.2x | Raro (~20% de items 3★) | Drops eventos, crafting, historia selectos |
| 4★ | 25 | 3.0x | Frecuente (~50%) | Drops raros eventos, crafting, dupes |
| 5★ | 30 | 4.0x | Siempre | Crafting endgame, dupes de 5★ |

- El equipo 1-2★ sirve de puente en early game — será reemplazado pero no inútil
- El equipo 3★ es el "workhorse" del mid-game y de la demo
- El equipo 4-5★ es aspiracional en la demo — pocos items, difícil de maxear

### 4. Upgrade (Leveling de Equipo)

El equipo sube de nivel consumiendo **Materiales de Forja** + **DOB**:

| Material | Nombre | XP que otorga | Obtención |
|----------|--------|--------------|-----------|
| `steel_shard` | Esquirla de Acero | 100 XP | Drops comunes |
| `obsidian_chunk` | Trozo de Obsidiana | 500 XP | Drops mid-game, crafting |
| `kraken_ingot` | Lingote de Kraken | 2000 XP | Drops de eventos, bosses |
| `powder_cursed` | Pólvora Maldita | 500 XP | Drops de stages con enemigos voodoo |
| `bone_dust` | Polvo de Hueso | 500 XP | Drops de eventos, bosses |

- Pólvora Maldita otorga **+20% XP** si se usa en armas místicas (Voodoo)
- Polvo de Hueso otorga **+20% XP** si se usa en accesorios
- Todos los materiales funcionan en cualquier equipo (los bonus son opcionales)
- Cada nivel requiere XP acumulativa (ver Fórmulas)
- El costo en DOB escala con el nivel actual y la rareza
- **No hay riesgo de fallo** — el upgrade siempre funciona (respeto al tiempo)

### 5. Crafting

Algunos equipos se obtienen crafteando con materiales dropeados en stages:

| Campo | Descripción |
|-------|-------------|
| `Materials` | Lista de (MaterialId, cantidad) requeridos |
| `DOBCost` | Costo en DOB para craftear |
| `UnlockCondition` | Qué desbloquea la receta (stage clear, capítulo, etc.) |

- Las recetas son **visibles desde el inicio** (el jugador sabe qué puede
  craftear aunque no tenga materiales)
- Los materiales no obtenidos se muestran en gris con la fuente de obtención
- Craftear es instantáneo (sin timers)
- El equipo crafteado sale a nivel 1

### 6. Obtención — Fuentes

| Fuente | Rarezas típicas | Detalle |
|--------|----------------|---------|
| **Drops de eventos** | 2★-5★ | Fuente principal de equipo. Cada evento rota equipo temático diferente. |
| **Drops de historia** | 1★-3★ | Solo stages específicas (no todas). Ch1: equipo 1-2★. Ch2-3: algún 3★. Pocos items, seleccionados. |
| **Crafting** | 3★-5★ | Recetas con materiales de eventos y bosses. Los 5★ requieren materiales de boss. |
| **Tienda de DOB** | 1★-2★ | Equipo básico starter. |
| **Recompensas de dupes** | 3★-5★ | Equipo exclusivo vinculado a la unidad o genérico de alta rareza. |

### 7. Gestión de Inventario

- El equipo es **global** — no está vinculado a una unidad hasta que se equipa
- **Equipar/Desequipar** es gratuito e instantáneo
- Una pieza de equipo solo puede estar en **una unidad a la vez**
- Si el jugador quiere mover un arma de Unidad A a Unidad B, se ofrece **swap
  automático** con confirmación
- **Vender equipo**: cualquier pieza no equipada se puede vender por DOB
- **Bloqueo**: el jugador puede bloquear equipo para evitar ventas accidentales
- **Sin límite de inventario** para la demo (feature futura)

### 8. Demo Equipment Catalog

**Weapons — Blade (Sables/Cutlass)**:

| Rareza | Nombre | Stats | Habilidad |
|--------|--------|-------|-----------|
| 1★ | Machete Herrumbroso | +ATK | — |
| 2★ | Cutlass de Marinero | +ATK, +SPD leve | — |
| 3★ | Sable del Contramaestre | +ATK sólido | Corte Salado (daño + reduce DEF) |
| 4★ | Espada del Motín | +ATK alto, +CRI | Primer Golpe (daño extra si actúas primero) |

**Weapons — Firearm (Pistolas/Mosquetes)**:

| Rareza | Nombre | Stats | Habilidad |
|--------|--------|-------|-----------|
| 1★ | Pistola Oxidada | +ATK leve | — |
| 2★ | Pistola de Chispa | +ATK | — |
| 3★ | Mosquete de Abordaje | +ATK, +CRI | Disparo Certero (daño ignora 20% DEF) |
| 4★ | Trabuco del Capitán | +ATK alto | Andanada (daño AoE a fila) |

**Weapons — Voodoo (Bastones/Relicarios)**:

| Rareza | Nombre | Stats | Habilidad |
|--------|--------|-------|-----------|
| 2★ | Muñeco de Trapo | +MST | — |
| 3★ | Bastón de Mangrove | +MST, +SPR | Raíces del Pantano (slow a 1 enemigo) |
| 4★ | Cráneo del Bokor | +MST alto | Maldición de Barón (daño Maldición + poison) |
| 5★ | Cetro de Mamá Brigitte | +MST enorme, +HP | Danza de los Muertos (revive aliado 30% HP) |

**Weapons — Dagger (Dagas/Cuchillos)**:

| Rareza | Nombre | Stats | Habilidad |
|--------|--------|-------|-----------|
| 1★ | Cuchillo de Cocina | +ATK leve, +SPD leve | — |
| 2★ | Daga de Abordaje | +ATK, +SPD | — |
| 3★ | Filo del Traidor | +ATK, +CRI, +SPD | Puñalada Trasera (daño ×2 si el objetivo ya actuó) |
| 4★ | Colmillo de Serpiente Marina | +ATK, +SPD alto | Veneno Abisal (daño + poison fuerte) |

**Weapons — Heavy (Hachas)**:

| Rareza | Nombre | Stats | Habilidad |
|--------|--------|-------|-----------|
| 1★ | Hacha de Leñador | +ATK | — |
| 2★ | Hacha de Abordaje | +ATK, +HP leve | — |
| 3★ | Destral del Berserker | +ATK alto, -SPD | Golpe Devastador (daño alto, baja prioridad) |
| 4★ | Ancla de Guerra | +ATK enorme, +HP, -SPD | Maremoto (daño AoE + stun 1 turno) |

**Armor**:

| Rareza | Nombre | Stats | Habilidad |
|--------|--------|-------|-----------|
| 1★ | Camisa Raída | +HP mínimo | — |
| 2★ | Cuero de Tiburón | +DEF, +SPR leve | — |
| 3★ | Casaca del Capitán | +HP, +DEF, +SPR | — |
| 3★ | Túnica del Houngan | +HP, +SPR, +MST leve | — |
| 4★ | Gabardina del Muerto | +HP, +SPR alto | Reduce daño Maldición 10% |
| 5★ | Coraza de la Reina Anne | +HP, +DEF, +SPR masivos | Voluntad Inquebrantable (sobrevive 1 golpe letal/batalla) |

**Accessory**:

| Rareza | Nombre | Stats | Habilidad |
|--------|--------|-------|-----------|
| 1★ | Moneda Mordida | +LCK leve | — |
| 2★ | Ojo de Cristal | +CRI | — |
| 3★ | Brújula que No Apunta al Norte | +SPD, +LCK | Intuición (+evasión 1 turno) |
| 3★ | Diente de Cocodrilo | +ATK, +CRI | — |
| 4★ | Medallón de Davy Jones | +HP, +SPR | Profundidades (absorbe daño Tormenta) |
| 5★ | Corazón en el Cofre | +HP, +DEF | 3 cargas: cada golpe letal consume 1 y deja 1 HP |

## Formulas

### XP para subir de nivel equipo

```
XP_to_next(level, rarity) = BASE_EQUIP_XP × (1 + level × EQUIP_XP_SCALE) × RarityMod
```

| Variable | Valor | Rango | Descripción |
|----------|-------|-------|-------------|
| `BASE_EQUIP_XP` | 50 | 30-100 | XP base para nivel 1→2 |
| `EQUIP_XP_SCALE` | 0.3 | 0.2-0.5 | Factor de escalado por nivel |
| `RarityMod` | ver tabla | — | Multiplicador por rareza |

| Rareza | RarityMod | XP nivel 1→2 | XP nivel max-1→max |
|--------|-----------|-------------|---------------------|
| 1★ | 0.8 | 52 | 172 |
| 2★ | 1.0 | 65 | 358 |
| 3★ | 1.2 | 78 | 546 |
| 4★ | 1.5 | 98 | 878 |
| 5★ | 2.0 | 130 | 1,430 |

**Ejemplo**: Subir un arma 3★ de nivel 1 a 20 cuesta ~5,800 XP total = ~12
Trozos de Obsidiana o ~58 Esquirlas de Acero.

### Costo DOB por upgrade

```
DOB_cost(level, rarity) = BASE_EQUIP_DOB × RarityMod × level
```

| Variable | Valor | Rango | Descripción |
|----------|-------|-------|-------------|
| `BASE_EQUIP_DOB` | 20 | 10-50 | DOB base por nivel |

**Ejemplo**: Subir un arma 3★ del nivel 10 al 11 cuesta 20 × 1.2 × 10 =
240 DOB.

### Stat scaling por nivel

```
Stat_at_level(L) = BaseStat + (StatGrowth × (L - 1))
```

Crecimiento lineal. Cada punto de StatGrowth otorga exactamente esa cantidad
por nivel.

### Precio de venta

```
SellPrice = BASE_SELL × RarityMod × (1 + level × 0.1)
```

| Variable | Valor | Descripción |
|----------|-------|-------------|
| `BASE_SELL` | 50 DOB | Base de venta |

**Ejemplo**: Arma 3★ nivel 10 → 50 × 1.2 × 2.0 = 120 DOB.

### Bonus XP por material especializado

```
EffectiveXP = MaterialXP × (1 + SPECIALTY_BONUS)
```

| Variable | Valor | Condición |
|----------|-------|-----------|
| `SPECIALTY_BONUS` | 0.20 (+20%) | Pólvora Maldita en arma Voodoo, Polvo de Hueso en Accessory |

## Edge Cases

| Edge Case | Resolución |
|-----------|------------|
| **Jugador intenta equipar arma incompatible** | Botón de equipar deshabilitado. Tooltip: "Esta unidad no puede usar [WeaponType]". |
| **Jugador vende equipo equipado** | No permitido. Debe desequipar primero. Botón de venta deshabilitado con mensaje. |
| **Jugador vende equipo bloqueado** | No permitido. Debe desbloquear primero. |
| **Jugador mueve arma de Unidad A a Unidad B** | Popup de confirmación: "Esta arma está equipada en [Unidad A]. ¿Mover a [Unidad B]?". Si acepta, se desequipa de A y equipa en B atómicamente. |
| **Jugador intenta upgradear equipo a nivel máximo** | Botón de upgrade deshabilitado. Mensaje: "Nivel máximo alcanzado". |
| **Jugador no tiene materiales suficientes para upgrade** | Botón habilitado pero al pulsar: muestra cuánto falta y dónde obtenerlo. |
| **Jugador no tiene DOB suficientes para upgrade** | Mismo que materiales: muestra déficit y link a fuentes de DOB. |
| **Equipo con BonusAbility en contexto incorrecto** | Si el arma da habilidad Land-only y la unidad está en combate naval, la habilidad no aparece en el pool de habilidades. Stats siguen aplicando. |
| **Jugador obtiene dupe de equipo que ya tiene** | Son instancias independientes. Puede tener 2 copias del mismo item (diferentes niveles, equipadas en distintas unidades). |
| **Crash durante upgrade** | Upgrade es atómico: o se aplica completo (nivel sube + materiales/DOB consumidos) o no se aplica. |
| **Equipo crafteado cuando el jugador tiene exactamente los materiales** | Válido — los materiales se consumen y el equipo aparece en inventario a nivel 1. |
| **Habilidad de equipo "Voluntad Inquebrantable" + otra fuente de sobrevivir golpe letal** | No se apilan. Solo una fuente se activa (prioridad: la que se consumiría primero). |
| **Corazón en el Cofre pierde las 3 cargas** | El equipo sigue equipado pero la habilidad queda inactiva el resto de la batalla. Se restauran las cargas al inicio de la siguiente batalla. |

## Dependencies

### Upstream Dependencies

| Sistema | Tipo | Interfaz |
|---------|------|----------|
| **Unit Data Model** | Hard | Define los 3 slots por unidad, `WeaponType` de compatibilidad, y que stats de equipo se bakan en stats finales antes de combate. |
| **Currency System** | Hard | DOB como costo de upgrade y crafting. |
| **Stage System** | Hard | Stages específicas dropean equipo y materiales de forja. |
| **Sistema Gacha** | Soft | Duplicados de personajes pueden otorgar equipo como recompensa. |

### Downstream Dependencies

| Sistema | Tipo | Interfaz |
|---------|------|----------|
| **Damage & Stats Engine** | Hard | DSE lee stats finales de unidad (que incluyen bonuses de equipo). Equipment abilities tienen su propio Element y AbilityPower. |
| **Combate Terrestre** | Hard | Stats de equipo afectan directamente el rendimiento en combate. Habilidades de equipo aparecen en el pool de la unidad. |
| **Combate Naval** | Indirect | Stats de equipo contribuyen a stats base del tripulante, que alimentan bonuses de rol naval (Ship Data Model). |
| **Initiative Bar** | Hard | SPD de equipo afecta EffectiveSPD → orden de turnos. |
| **Save/Load System** | Hard | Persiste: inventario de equipo (id, nivel, equipado en quién, bloqueado), materiales de forja. |
| **Progresión de Unidades** | Soft | Duplicados pueden generar equipo (coordinación con mecánica de dupes). |
| **Menus & Navigation UI** | Hard | UI de equipo en la pantalla de detalle de unidad y sub-tab de inventario. |

## Tuning Knobs

| Knob | Valor actual | Rango | Qué afecta | Si muy alto | Si muy bajo |
|------|-------------|-------|------------|-------------|-------------|
| `BASE_EQUIP_XP` | 50 | 30-100 | XP base para subir nivel | Equipo sube muy lento | Equipo se maxea trivialmente |
| `EQUIP_XP_SCALE` | 0.3 | 0.2-0.5 | Curva de XP por nivel | Niveles finales carísimos | Progresión plana, sin sensación de inversión |
| `BASE_EQUIP_DOB` | 20 | 10-50 | DOB por upgrade | Drain de DOB excesivo (compite con leveling/awakening) | DOB irrelevante como costo |
| `RarityMod` (por rareza) | 0.8-2.0 | 0.5-3.0 | Diferencia de costo entre rarezas | Equipo 5★ imposible de subir | No hay diferencia entre rarezas |
| `SPECIALTY_BONUS` | 0.20 | 0.10-0.50 | Incentivo de usar material correcto | Material genérico se siente mal | No vale la pena buscar material específico |
| `MaxLevel` por rareza | 10/15/20/25/30 | ±5 por tier | Techo de poder por rareza | Equipo bajo rareza compite con alto | Gap demasiado grande entre rarezas |
| `BASE_SELL` | 50 DOB | 20-100 | DOB al vender equipo | Vender es demasiado rentable (farm de venta) | Vender no vale la pena |
| Drop rate equipo en eventos | ~15% por run | 5-30% | Velocidad de obtención | Jugador tiene todo rápido, pierde motivación | Frustración, nunca consigue nada |
| Drop rate equipo en historia | ~5% en stages selectas | 2-10% | Equipo early-game | Demasiado equipo early, resta valor a eventos | Jugador llega a mid-game sin equipo |

### Knob Interactions

| Knob A | Knob B | Interacción |
|--------|--------|-------------|
| `BASE_EQUIP_DOB` | DOB income del Stage System | Si upgrade cuesta mucho DOB y stages dan poco, el jugador debe elegir entre subir unidades o equipo. Buena tensión si es moderada, frustración si es excesiva. |
| `MaxLevel` por rareza | Stat budget por rareza | Si MaxLevel alto + stat budget alto, equipo 5★ domina completamente. Mantener gap razonable (~4x entre 1★ max y 5★ max). |
| Drop rate eventos | `EQUIP_XP_SCALE` | Si equipo droppea mucho pero cuesta mucho subirlo, el cuello de botella se mueve a materiales de forja. Balancear ambos. |
| `SPECIALTY_BONUS` | Disponibilidad de Pólvora/Polvo | Si el bonus es alto pero los materiales son ultra-raros, el bonus existe solo en teoría. |

## Acceptance Criteria

| # | Criterio | Cómo verificar |
|---|----------|----------------|
| 1 | Cada unidad tiene exactamente 3 slots (Weapon, Armor, Accessory) | Test: abrir detalle de cualquier unidad → 3 slots visibles. |
| 2 | Solo armas del WeaponType compatible se pueden equipar | Test: unidad tipo Blade → intentar equipar Voodoo → bloqueado. Equipar Blade → funciona. |
| 3 | Armor y Accessory son universales | Test: cualquier unidad puede equipar cualquier armor/accessory sin restricción. |
| 4 | Stats de equipo se reflejan en stats finales de la unidad | Test: equipar arma +10 ATK → stat ATK de la unidad sube 10. Desequipar → vuelve al valor original. |
| 5 | Upgrade consume materiales + DOB y sube el nivel | Test: upgrade arma nivel 5→6 → materiales y DOB se restan, stats del arma suben según StatGrowth. |
| 6 | Upgrade falla si no hay materiales o DOB suficientes | Test: intentar upgrade sin recursos → mensaje de error con déficit, nada se consume. |
| 7 | No hay riesgo de fallo en upgrades | Test: 100 upgrades seguidos → 100% éxito. |
| 8 | BonusAbility aparece en el pool de habilidades en combate | Test: equipar Sable del Contramaestre → "Corte Salado" disponible en combate terrestre. |
| 9 | BonusAbility respeta BonusAbilityContext | Test: habilidad Land-only → no aparece en combate naval. Stats sí aplican. |
| 10 | Equipo solo puede estar en una unidad a la vez | Test: equipar arma en Unidad A → intentar en Unidad B → popup de swap/confirmación. |
| 11 | Vender equipo equipado está bloqueado | Test: seleccionar equipo equipado en venta → botón deshabilitado con mensaje. |
| 12 | Bloqueo de equipo impide venta | Test: bloquear equipo → intentar vender → bloqueado. Desbloquear → se puede vender. |
| 13 | Crafting consume materiales y DOB, genera equipo nivel 1 | Test: craftear item → materiales y DOB se restan, item aparece en inventario a nivel 1. |
| 14 | Recetas visibles aunque falten materiales | Test: abrir crafting → recetas con materiales faltantes se muestran en gris con fuente. |
| 15 | Inventario de equipo persiste entre sesiones | Test: obtener equipo → cerrar app → reabrir → equipo sigue en inventario con nivel y asignación correctos. |
