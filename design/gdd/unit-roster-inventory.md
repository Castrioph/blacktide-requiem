# Unit Roster/Inventory

> **Status**: Designed
> **Author**: User + Claude Code Game Studios agents
> **Last Updated**: 2026-04-01
> **Implements Pillar**: Pillar 3 (Recompensa a la Paciencia), Pillar 4 (Respeto al Tiempo)

## Overview

El Unit Roster/Inventory es el sistema de gestión del estado runtime del jugador
fuera de combate. Engloba dos subsistemas: el **Roster** (colección de unidades y
barcos obtenidos, con su estado de progresión individual) y el **Inventario** (todos
los items consumibles y materiales: tickets TIE/TIF, Ron de XP, Cristales Elementales,
Fragmentos de Alma, materiales de barco, y piezas de equipamiento).

El jugador interactúa activamente con el roster al consultar sus unidades, asignarlas
a equipos terrestres o tripulaciones navales, y seleccionar candidatos para
leveling/awakening. El inventario es mayormente pasivo — los items se acumulan por
rewards y se consumen por otros sistemas (progresión, gacha, craft). El sistema no
tiene gameplay propio; es la **capa de estado compartido** que conecta adquisición
(Gacha, Rewards) con uso (Combate, Progresión).

Sin este sistema, el jugador no tiene forma de ver qué posee, no puede organizar
equipos, y los sistemas de progresión no tienen un roster sobre el cual operar. Es
infraestructura crítica — invisible cuando funciona, bloqueante si falta.

## Player Fantasy

**"Mi colección, mi orgullo."** El roster es el reflejo tangible de toda la inversión
del jugador. Cada unidad en la lista representa una historia: la 5★ que salió en el
pull de pity, la 3★ que resultó ser clave para una sinergia naval, la primera unidad
max level. Navegar el roster debería sentirse como recorrer un álbum de logros — no
como buscar en un spreadsheet.

Para el inventario, la emoción es más sutil: **tranquilidad de abundancia**. Ver 15
Cristales de Tormenta acumulados genera la sensación de "tengo recursos para cuando
los necesite". El inventario bien organizado reduce la ansiedad de "¿tengo
suficiente?" — el jugador sabe exactamente qué tiene y qué le falta.

Este es un sistema de tipo **infraestructura con momentos de orgullo**. El 90% del
tiempo es invisible (el jugador no piensa en "el inventario"), pero el 10%
restante — cuando abre su roster para presumir su colección o planificar su próximo
awakening — debe sentirse satisfactorio y claro.

**Referencia**: FFBE (lista de unidades con filtros, satisfacción de ver unidades max
level con frame dorado), Genshin (inventario limpio por categorías, siempre sabes qué
tienes).

El sistema fracasa si encontrar una unidad específica requiere scroll infinito sin
filtros, si el jugador no puede saber de un vistazo qué unidades están listas para
awakening, o si el inventario es tan confuso que el jugador no sabe qué materiales
tiene ni para qué sirven.

## Detailed Design

### Core Rules

#### 1. Roster (Colección de Unidades)

El roster contiene todas las unidades que el jugador ha obtenido. **No tiene límite
de capacidad** — el jugador nunca necesita vender o descartar unidades para hacer
espacio. En la demo con 8-12 unidades esto es trivial; para el juego completo, la
decisión se reevaluará si el roster supera ~500 unidades.

**Datos por unidad en roster** (runtime, respaldado por `UnitSaveState`):

| Campo | Fuente | Descripción |
|-------|--------|-------------|
| Template | `UnitData` (ScriptableObject) | Datos estáticos: stats base, habilidades, traits, rarity, visual |
| Level | `UnitSaveState.level` | Nivel actual (1 a MaxLevel según awakening) |
| XP | `UnitSaveState.currentXP` | XP acumulada hacia el siguiente nivel |
| Awakening Tier | `UnitSaveState.awakeningTier` | 0-3 |
| Dupe Count | `UnitSaveState.dupeCount` | 0-4+ (bonus de stats activo) |
| Equipment | `UnitSaveState.equippedItems` | 3 slots: Weapon, Armor, Accessory |
| Unlocked Abilities | Derivado en runtime | `LandAbilities` y `SeaAbilities` donde `UnlockLevel <= level`, más las de awakening si `awakeningTier >= tier requerido`, más las otorgadas por equipment equipado |
| Computed Stats | Calculado en runtime | Stats finales = Base × (1 + DupeBonus) + LevelGrowth + AwakeningBonus + Equipment |
| Assignment | Derivado de `TeamState` + `ShipSaveState` | En qué equipo/barco está asignada (si alguno) |

**Solo se muestran unidades obtenidas** — no hay Pokédex ni siluetas de unidades no
obtenidas. El jugador descubre unidades al obtenerlas del gacha o rewards.

**Nota sobre habilidades**: las habilidades desbloqueadas NO se almacenan en save
data — se derivan del nivel, awakening tier, y equipment al cargar. Esto evita
desincronización si un balance patch cambia el `UnlockLevel` de una habilidad.

#### 2. Flota (Colección de Barcos)

La flota funciona igual que el roster pero para barcos. **Sin límite de capacidad**
(la demo tiene 2-3 barcos).

**Datos por barco en flota** (runtime, respaldado por `ShipSaveState`):

| Campo | Fuente | Descripción |
|-------|--------|-------------|
| Template | `ShipData` (ScriptableObject) | Datos estáticos: stats base, role slots, abilities |
| Hull/Cannons/Sails Level | `ShipSaveState` | Niveles de mejora (0-3 cada uno) |
| Crew Assignments | `ShipSaveState.crewAssignments` | Mapa de SlotIndex → UnitTemplateId |
| Owned | `ShipSaveState.owned` | Si el jugador lo ha adquirido |

#### 3. Inventario (Items y Materiales)

El inventario almacena todos los items consumibles organizados en **categorías con
tabs**:

| Tab | Items | Estructura de datos |
|-----|-------|-------------------|
| **Tickets** | TIE (Ticket Invocación Estándar), TIF (Ticket Invocación Featured) | Contadores simples (int) |
| **Items de XP** | Ron Añejo, Ron de Capitán, Ron Legendario | Contadores simples (int por tipo) |
| **Cristales** | 7 elementos × 3 tiers + Esencia Universal × 3 tiers | Map\<Element, {t1, t2, t3}\> |
| **Materiales de Barco** | Materiales para construcción/mejora de barcos | Map\<MaterialId, int\> |
| **Equipamiento** | Piezas de equipo obtenidas (diseño TBD — Equipment System #22) | List\<EquipmentSaveState\> |
| **Especiales** | Fragmentos de Alma | Contador simple (int) |

**El inventario no tiene límite de capacidad** para items stackeables (todo excepto
equipamiento). El equipamiento podría tener límite, pero eso se decidirá en el
Equipment System (#22).

#### 4. Sorting y Filtering del Roster

**Sorting** (una opción activa, toggle ascendente/descendente):

| Criterio | Orden Default | Descripción |
|----------|--------------|-------------|
| Level | Descendente | Unidades más fuertes primero |
| Rarity | Descendente | 5★ → 4★ → 3★ |
| Element | Agrupado | Agrupa por elemento (orden fijo: Pólvora, Tormenta, Maldición, Bestia, Acero, Luz, Sombra, Neutral) |
| Name | Ascendente | Alfabético A-Z |
| Recent | Descendente | Últimas obtenidas primero (por timestamp de adquisición) |

**Tie-breaker**: cuando el criterio principal empata, se ordena por Rarity desc →
Level desc → Name asc.

**Filtering** (combinable, multiselect dentro de cada categoría):

| Filtro | Opciones | Comportamiento |
|--------|----------|----------------|
| Rarity | 3★, 4★, 5★ | Mostrar solo rarezas seleccionadas |
| Element | 7 elementos + Neutral | Mostrar solo elementos seleccionados |
| Trait | Lista de traits existentes | Mostrar unidades que tengan AL MENOS uno de los traits seleccionados |

Los filtros se combinan con AND entre categorías (Rarity AND Element AND Trait) y con
OR dentro de cada categoría (Pólvora OR Tormenta).

**Persistencia de filtros**: los filtros se resetean al cerrar la pantalla del roster.
No se guardan en save.

#### 5. Unit Detail Screen (Full Hub)

Al seleccionar una unidad del roster, se abre una pantalla de detalle con toda la
información y acciones disponibles:

**Información visible:**

- Portrait + nombre + rarity stars + elemento
- Stats actuales (con desglose: base + level + awakening + equip + dupes)
- Habilidades terrestres desbloqueadas (y las bloqueadas con nivel de desbloqueo)
- Habilidades navales desbloqueadas
- Traits
- Awakening tier actual + requisitos del siguiente tier
- Dupe count + bonus activo
- Equipamiento actual (3 slots)
- Asignación actual (equipo terrestre / tripulación de barco / sin asignar)

**Acciones disponibles:**

- **Level Up**: Consumir items de Ron para dar XP. Muestra preview de stats
  antes/después. Requiere Ron en inventario + no estar al level cap
- **Awaken**: Si está en max level del tier actual. Muestra requisitos (cristales +
  DOB) y preview de beneficios. Botón habilitado solo si tiene todos los materiales
- **Equip**: Abrir selector de equipamiento por slot. Muestra stat diff al
  seleccionar pieza
- **Assign to Team**: Asignar a un slot del equipo terrestre (5+1). Si ya está en
  un slot, ofrece swap
- **Assign to Crew**: Asignar a un role slot del barco activo. Muestra compatibilidad
  de rol
- **View Lore**: Texto narrativo del personaje (si existe)

Si el full hub resulta demasiado denso a nivel UI, las acciones de menor prioridad
(View Lore, Assign to Crew) se mueven a pantallas dedicadas accesibles por shortcut.

#### 6. Ship Detail Screen

Al seleccionar un barco de la flota:

**Información visible:**

- Visual del barco + nombre + stats actuales
- Role slots con crew asignada (o vacíos)
- Niveles de mejora (Hull/Cannons/Sails) + requisitos del siguiente nivel
- Habilidades del barco (base + contribuidas por crew)

**Acciones disponibles:**

- **Assign Crew**: Seleccionar unidad para un role slot. Filtra por NavalRoleAffinity
- **Upgrade**: Mejorar Hull/Cannons/Sails (consume materiales de barco + DOB)
- **Set Active**: Seleccionar como barco activo para combate naval

#### 7. Operaciones del Inventario

El inventario es mayormente **read-only con acciones contextuales**:

- **Consulta**: Ver cantidad de cada item por categoría
- **Item detail**: Tap en un item muestra descripción, cantidad, y dónde obtener más
- **Uso directo**: Solo para items de XP (Ron) — tap en Ron permite seleccionar
  unidad destino para dar XP sin pasar por el roster. Atajo de conveniencia
- **No hay venta/descarte** de materiales en la demo. Los materiales se consumen por
  sistemas (progresión, craft, gacha con tickets)

### States and Transitions

#### Unit Assignment States

| Estado | Descripción | Transiciones válidas |
|--------|-------------|---------------------|
| **Unassigned** | No asignada a ningún equipo ni tripulación | → Assigned to Land Team, → Assigned to Crew |
| **Assigned to Land Team** | En uno de los 5 slots del equipo terrestre | → Unassigned (remove from team) |
| **Assigned to Crew** | En un role slot de un barco | → Unassigned (remove from crew), → Reassign (mover a otro slot del mismo barco) |

**Dual assignment permitido**: una unidad puede estar en el equipo terrestre Y en una
tripulación naval simultáneamente. Son contextos de combate separados — el jugador usa
su land team para stages terrestres y su crew para stages navales. No compiten por las
mismas unidades. Esto maximiza el valor del roster (Pillar 1: profundidad dual) y
evita frustración con un roster de 8-12 unidades en la demo.

#### Ship Assignment States

| Estado | Descripción | Transiciones válidas |
|--------|-------------|---------------------|
| **Not Owned** | Barco no adquirido (no aparece en flota) | → Owned (story reward o craft) |
| **Owned (Inactive)** | Adquirido pero no seleccionado | → Active |
| **Active** | Barco seleccionado para combate naval | → Inactive (al activar otro barco) |

Solo un barco puede estar **Active** a la vez. Activar uno desactiva el anterior.

#### Inventory Item Lifecycle

| Fase | Descripción |
|------|-------------|
| **Adquisición** | Item entra al inventario (gacha → unidad; stage clear → materiales; reward → items) |
| **Almacenamiento** | Item existe en inventario con cantidad ≥ 1 |
| **Consumo** | Item se gasta (Ron → XP, Cristales → Awakening, Tickets → Pull, Ship Mats → Upgrade) |
| **Agotado (×0)** | Cantidad = 0. Item **sigue visible** en inventario con "×0" — el jugador puede tap para ver descripción y dónde obtener más |

Items con cantidad 0 siempre se muestran (no se ocultan). Esto evita que el jugador
olvide que un tipo de material existe y le permite planificar farming.

### Interactions with Other Systems

**Upstream (este sistema CONSUME datos de):**

| Sistema | Datos consumidos | Interfaz |
|---------|-----------------|----------|
| **Unit Data Model** | Templates de unidades (stats base, habilidades, traits, rarity, visual) | Lee `UnitData` ScriptableObjects por `templateId` |
| **Ship Data Model** | Templates de barcos (stats, role slots, abilities) | Lee `ShipData` ScriptableObjects por `shipId` |
| **Progresión de Unidades** | Fórmulas de level-up (XP tables, stat growth), requisitos de awakening, reglas de duplicados | Invoca funciones de Progresión para calcular stats, validar awakening, aplicar XP |
| **Save/Load System** | Estado persistido (UnitSaveState, ShipSaveState, InventoryState, TeamState) | Al iniciar, recibe datos deserializados. Al modificar estado, notifica para auto-save |

**Downstream (otros sistemas CONSUMEN datos de este):**

| Sistema | Datos que consume | Interfaz |
|---------|------------------|----------|
| **Menus & Navigation UI** (#19) | Roster filtrado/ordenado, inventario por categoría, unit detail data | Lee del roster/inventory runtime state para renderizar |
| **Combate Terrestre** (#10) | Equipo terrestre (5+1 unidades con stats computados) | Lee `TeamState.landTeam` → resuelve a unidades completas del roster |
| **Combate Naval** (#12) | Barco activo + crew (con stats y habilidades contribuidas) | Lee `TeamState.activeShipId` + crew assignments del roster |
| **Sistema Gacha** (#13) | Escribe al roster (nueva unidad) e inventario (consume tickets, añade fragmentos si dupe overflow) | Notifica al roster para añadir unidad; notifica al inventario para ajustar cantidades |
| **Rewards System** (#16) | Escribe al inventario (materiales, tickets, Ron) y currencies | Notifica al inventario para añadir items |

**Bidirectional:**

| Sistema | Interacción |
|---------|-------------|
| **Progresión** ↔ **Roster** | Roster muestra estado de progresión; Progresión modifica estado en roster (level up, awaken). El roster valida pre-condiciones, Progresión ejecuta la lógica |
| **Team Composition** (#11) ↔ **Roster** | Team Comp define reglas de formación (5+1 land, role slots naval); Roster ejecuta asignaciones respetando esas reglas |

## Formulas

Este sistema es mayormente un contenedor de estado — las fórmulas pesadas viven en
Progresión de Unidades (XP, stat growth) y Damage & Stats Engine (combate). Las
fórmulas propias son de agregación y derivación:

### F1. Computed Stats (agregación runtime)

```
FinalStat[s] = (BaseStat[s] × (1 + DupeBonus)) + LevelGrowth[s] + AwakeningBonus[s] + EquipmentBonus[s]
```

Donde:
- `BaseStat[s]`: del template `UnitData.BaseStats`
- `DupeBonus`: `0.05 × min(dupeCount, 4)`
- `LevelGrowth[s]`: fórmula piecewise del Unit Data Model (ver Progresión GDD §1)
- `AwakeningBonus[s]`: flat bonus por tier (ver Progresión GDD §2)
- `EquipmentBonus[s]`: suma de bonuses de los 3 slots (Equipment System #22, TBD)

Este sistema **no define** estas fórmulas — las consume y ensambla para display.

### F2. Sort Tie-Breaker Score

```
SortScore = (Rarity × 10000) + (Level × 100) + alphabetical_index(Name)
```

Usado solo como desempate cuando el criterio de sorting principal empata. Rarity
descendente tiene prioridad, luego level descendente, luego nombre ascendente.

### F3. Ability Unlock Check

```
IsUnlocked(ability) =
  (ability.Source == Learned   AND unit.level >= ability.UnlockLevel)       OR
  (ability.Source == Awakening AND unit.awakeningTier >= ability.RequiredTier) OR
  (ability.Source == Equipment AND ability.GrantingEquipId IN unit.equippedItems)
```

Variables:
- `ability.Source`: enum `{Learned, Awakening, Equipment}` del `AbilityEntry`
- `ability.UnlockLevel`: nivel requerido (definido en Unit Data Model)
- `ability.RequiredTier`: tier de awakening requerido (definido en Progresión)
- `unit.equippedItems`: lista de IDs de equipment en los 3 slots

## Edge Cases

| Situación | Qué pasa | Por qué |
|-----------|----------|---------|
| **Obtener dupe con 4+ dupes** | Dupe se convierte en Fragmentos de Alma (3★→3, 4★→10, 5★→50). No se añade como copia. El roster nunca tiene duplicados de la misma unidad | Definido en Progresión GDD §3 |
| **Asignar a land team una unidad en otro slot** | Swap automático: nueva unidad al slot destino, anterior a Unassigned (o intercambian posiciones) | Evita desasignar manualmente |
| **Asignar a crew un slot cuya unidad ya ocupa otro slot del mismo barco** | Swap: la unidad se mueve, el slot anterior queda vacío o recibe la otra | Una unidad no puede ocupar dos slots del mismo barco |
| **Asignar crew sin NavalRoleAffinity compatible** | Se permite con advertencia visual ("Sin afinidad — sin bonificación de rol"). El jugador decide | Con 8-12 unidades en demo, forzar afinidad puede dejar slots vacíos |
| **Remover última unidad del land team** | Permitido. Equipo puede tener 0-5 unidades. Stage System valida mínimo para entrar a combate | Separación de responsabilidades |
| **Remover toda crew del barco activo** | Permitido. Stage System valida mínimo para combate naval | Misma separación |
| **Level up: XP de Ron excede cap** | XP sobrante se pierde. Preview muestra cuánta XP se perdería antes de confirmar | Definido en Progresión GDD §1 |
| **Awakening sin materiales** | Botón deshabilitado. Muestra qué falta y dónde farmear | UX claro, sin dead-end |
| **Roster vacío (primer launch)** | Jugador comienza con 1 unidad de historia. Roster nunca está vacío | Definido en Save/Load GDD §4 (starter state) |
| **Inventory item quantity = 0** | Item visible con "×0" + "Obtén más en: [fuente]" | Decisión en States & Transitions |
| **Equipar item ya equipado por otra unidad** | Auto-desequipa de la otra + notificación "Desequipado de [Unit Name]" | Un equipo no puede estar en dos unidades. Auto-swap ahorra pasos |
| **Sorting/filtering con 0-1 unidades** | Funciona normalmente. No crashes, no estados rotos | Robustez básica |

## Dependencies

### Hard Dependencies (sistema no funciona sin ellos)

| Sistema | Dirección | Interfaz |
|---------|-----------|----------|
| Unit Data Model (#1) | Upstream | `UnitData` templates: stats, abilities, traits, rarity, visual |
| Ship Data Model (#2) | Upstream | `ShipData` templates: stats, role slots |
| Progresión de Unidades (#15) | Upstream | Fórmulas de stat growth, awakening rules, duplicate rules |
| Save/Load System (#17) | Upstream | Serialización/deserialización de todo el estado del roster e inventario |

### Soft Dependencies (funciona sin ellos, funcionalidad reducida)

| Sistema | Dirección | Interfaz |
|---------|-----------|----------|
| Equipment System (#22) | Upstream | Equipment data para 3 slots. Sin él, slots vacíos/deshabilitados |
| Team Composition (#11) | Upstream | Reglas de formación (5+1 land, role slots). Sin él, asignación sin restricciones |

### Downstream Dependents (sistemas que dependen de este)

| Sistema | Qué consume |
|---------|-------------|
| Menus & Navigation UI (#19) | Roster data, inventory data, unit detail para display |
| Combate Terrestre (#10) | Land team resuelto a unidades con stats computados |
| Combate Naval (#12) | Barco activo + crew con stats y abilities |
| Sistema Gacha (#13) | Escribe al roster (nueva unidad) e inventario (fragmentos, tickets) |
| Rewards System (#16) | Escribe al inventario (materiales, items) |

## Tuning Knobs

### Knobs propios del sistema

| Knob | Default | Safe Range | Afecta | Si muy alto | Si muy bajo |
|------|---------|-----------|--------|-------------|-------------|
| `ROSTER_DEFAULT_SORT` | Rarity desc | Cualquier criterio | Orden por defecto al abrir roster | N/A (preferencia) | N/A |
| `INVENTORY_ZERO_ITEMS_VISIBLE` | true | true/false | Si items con ×0 se muestran | N/A | Items olvidados |
| `UNIT_DETAIL_STAT_DECIMALS` | 0 | 0-2 | Decimales en stats mostrados | Ruido visual | Pérdida de precisión |
| `CREW_ALLOW_NO_AFFINITY` | true | true/false | Si se permite crew sin afinidad de rol | Reduce importancia de afinidad | Slots vacíos en demo |
| `EQUIP_AUTO_UNEQUIP` | true | true/false | Si equipar auto-desequipa de otra unidad | Confuso sin notificación | Más pasos manuales |

### Knobs de otros sistemas que afectan este (referencia, no duplicar)

- `MAX_LEVEL_BY_RARITY_AND_AWAKENING` (Progresión GDD): cuándo mostrar "Ready to awaken"
- `DUPE_STAT_BONUS_PER_COPY` (Progresión GDD): 5% — afecta stat display
- `TEAM_SIZE_LAND` (Team Composition): 5+1 — slots visibles en asignación terrestre
- `SHIP_ROLE_SLOTS` (Ship Data Model): variable por barco — slots de crew

## Acceptance Criteria

| # | Criterio | Verificación |
|---|----------|-------------|
| AC-1 | Todas las unidades obtenidas aparecen en roster con datos correctos | Obtener 3 unidades vía gacha → verificar que las 3 aparecen con stats matching template + level |
| AC-2 | Sorting funciona para 5 criterios en ambas direcciones | Ordenar por cada criterio → verificar orden. Toggle asc/desc → verificar inversión |
| AC-3 | Filtering por Rarity, Element, Trait, combinable AND/OR | Filtrar 5★ + Pólvora → solo unidades que cumplen ambos. Pólvora OR Tormenta → ambos elementos |
| AC-4 | Unit Detail muestra info completa: stats (desglose), abilities (locked/unlocked), traits, awakening, dupes, equipment, assignment | Unidad level 15 con 1 dupe → stats = base×1.05 + growth. Abilities UnlockLevel ≤ 15 desbloqueadas |
| AC-5 | Level Up consume Ron y actualiza stats inmediatamente | Ron de Capitán en unidad lv5 → XP sube, si cruza threshold level sube, stats recalculan |
| AC-6 | Awakening consume materiales + DOB, sube cap, muestra nueva ability | 3★ lv30 + materiales → awaken → max level 40, stat bonus aplicado |
| AC-7 | Dual assignment: misma unidad en land team Y crew | Elena en slot 1 land + Gunner crew → aparece en ambos sin conflicto |
| AC-8 | Swap automático en land team y crew | Asignar A al slot 1 (ocupado por B) → B a Unassigned o intercambian |
| AC-9 | Crew sin afinidad: advertencia + asignación permitida | Unidad sin Captain affinity al slot Captain → warning visible, asignación exitosa |
| AC-10 | Inventario muestra categorías con cantidades correctas | Stage dropea 5 Cristales T1 → inventario +5 en categoría correcta |
| AC-11 | Items ×0 visibles con fuente de obtención | Consumir todo Ron Añejo → sigue visible "×0" + fuente |
| AC-12 | Equip auto-desequipa con notificación | Equipar espada de A en B → espada en B, slot A vacío, notificación |
| AC-13 | Save trigger tras operaciones de roster/inventario | Level up → save.json actualizado (o dirty flag para próximo auto-save) |
| AC-14 | Ship Detail: crew, stats, upgrades, Set Active funcional | Abrir barco → slots con crew, stats correctos, Set Active funciona |
| AC-15 | **Performance**: roster con 12 unidades abre en ≤500ms, sort/filter <100ms | Timer en demo. Stress test con 100+ unidades: no exceder 1s |

## Visual/Audio Requirements

### Visual

- **Unit card/tile en roster**: Portrait + rarity frame (bronce 3★, plata 4★, oro 5★)
  + element icon + level badge. Unidades at max level pre-awakening: badge pulsante
  "Ready". Badges sobre card: "NEW" (recién obtenida), "MAX" (at level cap),
  espada icon (land team), ancla icon (crew)
- **Unit detail screen**: Portrait grande + fondo temático por elemento. Stat bars
  con colores (verde=alto, amarillo=medio, rojo=bajo para su rarity). Desglose on-tap
- **Awakening visual**: Flash de luz + frame upgrade (más elaborado por tier).
  Tier 3 = frame especial visible en roster
- **Tickets TIE/TIF**: Icono de **ticket dentro de una botella** — cohesión con la
  animación de tirada y temática pirata. TIE = botella azul, TIF = botella dorada
- **Inventario items**: Iconos claros por categoría. Cristales coloreados por
  elemento. Ron con 3 niveles visuales de botella
- **Ship detail**: Vista lateral del barco con slots de crew como posiciones en
  cubierta. Crew asignada muestra mini-portrait. Guest slot (posición 6 land team)
  con visual diferenciado

### Audio

- **Abrir roster**: Sonido sutil de menú (no intrusivo — se abre frecuentemente)
- **Level up**: SFX satisfactorio de progresión + campanada suave
- **Awakening**: SFX épico proporcional al tier (T1=medio, T3=fanfarria)
- **Equip/assign**: Click metálico (equip), crujido de madera (assign crew)
- **Sort/filter**: Click suave de interfaz
- **Error (materiales insuficientes)**: Tono bajo negativo, no agresivo

## UI Requirements

### Roster Screen

- Grid layout de unit cards (3-4 columnas portrait mobile, 5-6 landscape/web)
- Barra superior: sort dropdown + filter toggles
- Tap card → Unit Detail. Long-press → preview rápido (stats principales)
- Default sort: Rarity descendente

### Unit Detail Screen (Full Hub)

Layout scrollable vertical:
1. **Header**: portrait + nombre + stars + element
2. **Stats panel**: 7 stats con barras + desglose expandible
3. **Abilities panel**: tabs Land/Sea, lista con lock/unlock status
4. **Equipment panel**: 3 slots interactivos
5. **Action buttons**: Level Up | Awaken | Assign
6. **Footer**: Traits + Lore link

### Inventory Screen

- Tab bar horizontal (6 categorías: Tickets, XP Items, Cristales, Ship Mats,
  Equipamiento, Especiales)
- Grid de items dentro de cada tab (icono + cantidad)
- Tap item → popup: nombre, descripción, cantidad, fuente, acción directa si aplica

### Ship Detail Screen

- Visual central del barco con slots interactivos sobre la imagen
- Panel lateral: stats + upgrade buttons
- Botón "Set Active" prominente si no es el barco activo

### Guest Slot (Land Team posición 6)

- Visual diferenciado del resto de slots (borde distinto, icono de "amigo")
- Asignable desde lista de unidades amigas (friend list)
- No se selecciona del roster propio — es siempre una unidad prestada

## Open Questions

| # | Pregunta | Owner | Estado |
|---|----------|-------|--------|
| OQ-1 | Sort "Recent" usa orden de inserción en la lista del save (no requiere timestamp extra). Confirmado viable | — | Resuelto |
| OQ-2 | Equipment System tendrá límite de inventario para piezas — no en demo, sí en futuro | Equipment System #22 | Resuelto para demo |
| OQ-3 | Unidades de historia son unidades normales en el roster. Primeros dupes obtenibles por historia, resto en banner estándar. Sin badge especial | — | Resuelto |
| OQ-4 | Guest slot (posición 6) tiene visual diferente y se asigna desde lista de amigos, no del roster propio | — | Resuelto |
