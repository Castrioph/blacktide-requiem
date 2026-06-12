# ADR-0004: Naval Combat Architecture

## Status

**Accepted** — 2026-06-12. Usuario confirmó los 4 puntos abiertos: (1) refactor previo S4-02a con tests terrestres como gate; (2) extender `CombatAction` compartida con campos navales inertes; (3) generalizar `SynergyEvaluator` a `ITraitCarrier`; (4) crew como sub-entidad pasiva, no `ICombatant`.

## Date

2026-06-12

## Context

### Problem Statement

El Sprint 4 implementa el combate naval, el sistema diferenciador del juego
(Pillar 1: Profundidad Estratégica Dual). Antes de escribir una sola línea de
código de gameplay naval (S4-02..04), hay que decidir **cómo se relaciona el
combate naval con el `CombatManager` terrestre ya en producción** (ADR-003).

El riesgo central, identificado en `sprint-004.md` §Risks, es la **duplicación
masiva**: copiar el state machine, el pipeline de turnos, los DoTs, la gestión
de oleadas, la lógica de sinergias y el manejo de muerte para crear un
`NavalCombatManager` paralelo produciría ~800 líneas duplicadas que divergirían
con cada bugfix futuro. El riesgo opuesto es **forzar el reuso**: doblar el
`CombatManager` terrestre para que entienda barcos y crew puede contaminar el
camino terrestre estable con condicionales navales, rompiendo lo que ya
funciona y está testeado.

Esta ADR debe decidir, con criterio de arquitectura, dónde reusar y dónde
separar — y dejar las interfaces fijadas para que S4-02..04 se implementen sin
re-litigar la estructura.

### Estado actual del código (lo que YA existe)

**Capa de datos naval — implementada (S anterior):**

| Tipo | Estado | Rol |
|------|--------|-----|
| `ShipData` (ScriptableObject) | ✅ | Plantilla de barco: stats, role slots, BaseAbilities, upgrades, fórmulas de crew contribution y upgrade bonus (estáticas) |
| `ShipStatBlock` (struct, 7 stats) | ✅ | HHP/FPW/HDF/MST/MP/RSL/SPD con indexador |
| `ShipStatType` (enum) | ✅ | Índices de stat naval |
| `NavalRole` (enum, 7 roles) | ✅ | Capitán … Contramaestre |
| `RoleSlot` (struct) | ✅ | SlotIndex, Role, IsGuestSlot |
| `ShipUpgradeState` (struct) | ✅ | HullLevel / CannonsLevel / SailsLevel |
| `ShipAcquisition` (enum) | ✅ | Story / Crafted / Gacha |
| `CharacterData.SeaAbilities` | ✅ | `List<AbilityEntry>` — pool sea-tag por unidad |
| `UnitData.NavalRoleAffinity` | ✅ | `List<NavalRole>` por unidad |

**Capa de combate naval — NO existe nada todavía.** No hay runtime state de
barco, ni de crew, ni manager naval, ni acciones navales, ni input naval.

**Capa de combate terrestre — en producción y testeada (ADR-003):**

- `CombatManager` (C# puro) — state machine `PreCombat → InRound ⇄
  WaveTransition → Victory/Defeat`, pipeline de turno
  (buff tick → status tick → cooldown → CC immunity → bleed → CC check →
  acción → burn → poison), muerte, oleadas, LB, eventos.
- `InitiativeBar` (C# puro) — orden por `GetEffectiveStat(StatType.SPD)`,
  tie-break boss/ally/slot, inserción LB, `RemoveDead`. **Genérico**: solo
  depende de `CombatantState` vía `GetEffectiveStat(SPD)`, `IsKO`, `IsBoss`.
- `CombatantState` — runtime de una unidad: HP/MP, buffs, status, cooldowns,
  `GetEffectiveStat(StatType)`. Su comentario ya dice "(unit **or ship**)".
- `SynergyEvaluator` (estático) — opera sobre `IReadOnlyList<CombatantState>`,
  lee `Template.Traits`, umbral de 3, capitán primario + secundario (guest).
- `ICombatInput` / `CombatAction` / `CombatContext` — abstracción de input
  (player UI / AI / auto-battle) con callback async.
- `DamageCalculator`, `HealCalculator`, `ElementTable`, `BuffStack` — fórmulas
  por stat, agnósticas a unidad vs barco (reciben floats).

### Constraints

1. **No tocar el camino terrestre estable.** ADR-003 está Accepted y testeado;
   los cambios navales no deben introducir condicionales `if (isNaval)` en el
   pipeline terrestre.
2. **Data-driven con assets visibles** (regla retro S3): el ADR debe permitir
   que S4-02/S4-07 autoren barcos, crew y oleadas como assets cableados.
3. **Cambios pequeños y reversibles** (CLAUDE.md).
4. **Testable sin Play Mode** — igual que ADR-003, la lógica naval debe ser
   C# puro testeable en EditMode (DoD: ≥25 tests en S4-04).
5. **Compatibilidad futura con grid táctico** (Open Question #1 del GDD): no
   implementar grid, pero no cerrarse la puerta. El diseño debe mantener
   stats/fórmulas/crew/roles intactos si se migra.

### Requirements (del GDD Combate Naval + sprint-004.md S4-01)

- El **barco** es 1 combatiente en la Initiative Bar; la **crew** NO toma turnos
  individuales pero ES atacable (Abordaje) y contribuye stats/habilidades/traits.
- 6 acciones: Cañonazo, Habilidad Naval, Maniobra Evasiva, Abordaje, Reparar,
  Pasar.
- DoT split naval: Quemadura → HHP del barco; Veneno/Sangrado → HP de 1 crew
  member aleatorio vivo.
- Muerte de crew → recálculo inmediato de stats efectivos + retirada de sus
  SeaAbilities del pool. Muerte del Capitán crew → desactiva sinergias.
- Ships inmunes a Sueño/Aturdimiento/Muerte. Ceguera/Silencio sí aplican.
- LB naval da turno extra al **barco** (no al crew), max 1/ronda.
- Oleadas con persistencia de estado del barco (HHP/MP/buffs/crew muertos).
- Sinergias: Capitán crew activa; guest amigo = 2º Capitán (S4-08).

## Decision

### Resumen de la decisión central: **Reuso por composición, no por herencia ni por copia**

Adoptamos un modelo de **tres capas** donde el combate naval **reusa toda la
infraestructura de orquestación y secuenciado** (state machine, Initiative Bar,
pipeline de turno, oleadas, eventos, sinergias) pero **inyecta la semántica
naval mediante una abstracción de combatiente** (`ICombatant`) y un **resolver
de acciones intercambiable** (`ITurnResolver`). Ni se copia el `CombatManager`
(evita duplicación) ni se le añaden ramas `if (naval)` (no contamina lo
terrestre). La diferencia naval vive en:

1. Un combatiente-barco (`ShipCombatant`) que implementa `ICombatant` y posee
   sub-entidades crew (`CrewMemberState`).
2. Un resolver naval (`NavalTurnResolver`) que ejecuta las 6 acciones y el DoT
   split, conmutado por el manager según el tipo de batalla.

El `CombatManager` se **generaliza una vez** (extrae el contrato `ICombatant`
y delega la fase de acción a `ITurnResolver`) y a partir de ahí terrestre y
naval son dos configuraciones del mismo orquestador.

### 1. Capa de orquestación — `CombatManager` generalizado (reuso total)

`CombatManager`, `InitiativeBar` y la maquinaria de estados/oleadas/eventos se
**reusan sin duplicar**. Para que dejen de asumir "combatiente = unidad", se
hace una **extracción de interfaz**: `CombatantState` y `ShipCombatant`
implementan ambos `ICombatant`. La Initiative Bar y el state machine solo
hablan con `ICombatant`.

```
                    ┌──────────────────────────────────────┐
                    │           CombatManager               │
                    │  (C# puro — state machine, oleadas,   │
                    │   pipeline de turno, eventos)         │
                    │                                       │
                    │   ── REUSADO terrestre + naval ──     │
                    └───────────────┬───────────────────────┘
                                    │ orquesta ICombatant[]
                  ┌─────────────────┼──────────────────┐
                  │                 │                  │
          ┌───────▼──────┐  ┌───────▼───────┐  ┌───────▼────────┐
          │ InitiativeBar│  │  ITurnResolver│  │   ICombatInput │
          │  (reusado)   │  │  (estrategia) │  │  (reusado)     │
          └──────────────┘  └───────┬───────┘  └────────────────┘
                                    │ implementaciones
                       ┌────────────┴─────────────┐
              ┌────────▼─────────┐      ┌──────────▼──────────┐
              │ LandTurnResolver │      │  NavalTurnResolver  │
              │ (Attack/Ability/ │      │ (Cañonazo/Habilidad/│
              │  Guard/Pass +    │      │  Maniobra/Abordaje/ │
              │  burn→self/      │      │  Reparar/Pasar +    │
              │  poison→self)    │      │  Quemadura→HHP /    │
              │                  │      │  Veneno-Sangrado→   │
              │                  │      │  crew aleatoria)    │
              └──────────────────┘      └─────────┬───────────┘
                                                  │ opera sobre
                                        ┌─────────▼──────────┐
                                        │   ICombatant       │
                                        ├────────────────────┤
                                        │ CombatantState     │ ← terrestre
                                        │ ShipCombatant      │ ← naval
                                        │   └─ CrewMemberState[]
                                        └────────────────────┘
```

### 2. Capa de combatiente — `ICombatant` + `ShipCombatant` (la abstracción naval)

`ICombatant` captura **exactamente lo que el orquestador y la Initiative Bar
necesitan**, nada más. Es deliberadamente pequeño para no forzar a un barco a
fingir ser una unidad.

```csharp
namespace BlacktideRequiem.Core.Combat
{
    /// <summary>
    /// Lo que el CombatManager y la InitiativeBar necesitan de cualquier
    /// participante en combate, sea una unidad terrestre o un barco naval.
    /// Mantiene el orquestador agnóstico a la semántica de la entidad.
    /// </summary>
    public interface ICombatant
    {
        Element Element { get; }
        bool IsKO { get; }
        bool IsBoss { get; }

        // Superficie de HP (el barco la mapea a HHP) — compartida por UI y DoTs
        int CurrentHP { get; }
        int MaxHP { get; }
        int ApplyDamage(int damage);
        int ApplyHealing(int amount);

        // Initiative + pipeline genérico
        float GetEffectiveStat(StatType stat);   // SPD para orden de turno
        BuffStack Buffs { get; }
        bool LBUsedThisRound { get; set; }

        // Status / CC (ships ignoran Sueño/Aturdimiento/Muerte vía IsImmuneTo)
        bool HasStatus(StatusEffect effect);
        void ApplyStatus(StatusInstance status);
        bool RemoveStatus(StatusEffect effect);
        List<StatusEffect> TickStatuses();
        bool IsImmuneTo(StatusEffect effect);    // ship: true para CC/Muerte
        int CCImmunityTurns { get; set; }
        IReadOnlyList<StatusInstance> StatusEffects { get; }

        // Cooldowns (compartido)
        void TickCooldowns();
    }
}
```

`CombatantState` (terrestre) implementa `ICombatant` añadiendo
`IsImmuneTo` (devuelve false salvo flags existentes) — cambio mínimo y aditivo.

`ShipCombatant` es la **entidad naval** y es lo único realmente nuevo a nivel
de runtime de entidad:

```csharp
namespace BlacktideRequiem.Core.Combat
{
    /// <summary>
    /// Runtime de un barco en combate naval. Implementa ICombatant para que el
    /// CombatManager lo trate como un combatiente más en la Initiative Bar.
    /// Posee crew members atacables (sub-entidades) y deriva sus stats efectivos
    /// de base + upgrades + crew vivo + traits.
    /// </summary>
    public class ShipCombatant : ICombatant
    {
        public ShipData Ship { get; }
        public ShipUpgradeState Upgrades { get; }

        // Recurso de combate (runtime)
        public int CurrentHHP { get; private set; }   // casco; sink a 0
        public int MaxHHP { get; private set; }
        public int CurrentMP { get; private set; }
        public int MaxMP { get; private set; }

        public bool IsManeuvering { get; set; }        // Maniobra Evasiva activa
        public bool IsKO => CurrentHHP <= 0;           // barco hundido
        public bool IsBoss { get; set; }
        public Element Element => Ship.Element;

        // Crew — sub-entidades atacables, NO toman turnos
        public IReadOnlyList<CrewMemberState> Crew => _crew;
        private readonly List<CrewMemberState> _crew;

        public int CaptainSlotIndex { get; }           // crew slot del Capitán
        public bool IsGuestFriend { get; }
        public int GuestSlotIndex { get; }             // 2º capitán (S4-08)

        // Pool de habilidades efectivo (BaseAbilities + SeaAbilities de crew viva)
        public IReadOnlyList<AbilityData> AbilityPool => _abilityPool;
        private readonly List<AbilityData> _abilityPool;

        /// <summary>
        /// Stat efectivo del barco: Base + Upgrade + sum(CrewContribution viva)
        /// + TraitBonuses. Para SPD lo consume la InitiativeBar; para FPW/MST/HDF/RSL
        /// las fórmulas de daño. Buffs se aplican vía BuffStack (mismo modelo).
        /// </summary>
        public float GetEffectiveStat(StatType stat) { /* ver §4 */ }

        /// <summary>
        /// Recalcula stats efectivos y reconstruye el ability pool tras un cambio
        /// de crew (muerte). Lo invoca el NavalTurnResolver al matar crew.
        /// </summary>
        public void RecalculateFromCrew() { /* ver §4 */ }
    }
}
```

```csharp
namespace BlacktideRequiem.Core.Combat
{
    /// <summary>
    /// Runtime de un crew member dentro de un barco. NO es un ICombatant: no
    /// toma turnos. Es target de Abordaje y de DoTs Veneno/Sangrado, y portador
    /// de traits (para SynergyEvaluator) y de SeaAbilities (para el pool).
    /// HP fijo por rol (GDD §6), independiente del nivel de la unidad.
    /// </summary>
    public class CrewMemberState
    {
        public NavalRole Role { get; }
        public CharacterData Unit { get; }    // unidad asignada (traits, sea abilities, stats individuales)
        public int MaxHP { get; }             // fijo por rol (Capitán 800 … Artillero 400)
        public int CurrentHP { get; set; }
        public bool IsDead => CurrentHP <= 0;

        public int ApplyDamage(int dmg);      // Abordaje / DoT crew
    }
}
```

**Justificación de que el crew NO sea `ICombatant`:** el crew nunca aparece en
la Initiative Bar ni ejecuta un turno. Hacerlo `ICombatant` lo metería en
estructuras donde no pertenece y obligaría a casos especiales "saltar crew en
el turno". Mantenerlo como sub-entidad del barco refleja el GDD ("la tripulación
es equipo pasivo") y mantiene la Initiative Bar limpia.

### 3. Capa de resolución de acción — `ITurnResolver` (lo que diverge)

El **único punto donde terrestre y naval difieren de verdad** es la fase de
acción y los DoTs post-acción (terrestre los aplica al actor; naval los reparte
casco/crew). Eso se extrae a una estrategia inyectada en el `CombatManager`.

```csharp
namespace BlacktideRequiem.Core.Combat
{
    /// <summary>
    /// Estrategia de resolución de la fase de acción y de los DoTs de un turno.
    /// El CombatManager delega aquí; el resto del pipeline (buff tick, status
    /// tick, CC check, muerte, oleadas, eventos) es compartido.
    /// </summary>
    public interface ITurnResolver
    {
        /// <summary>Ejecuta la acción elegida sobre el actor y sus targets.</summary>
        void ResolveAction(ICombatant actor, CombatAction action, CombatContext ctx);

        /// <summary>
        /// Aplica los DoTs post-acción.
        /// Terrestre: burn/poison al actor.
        /// Naval: Quemadura→HHP del barco; Veneno/Sangrado→1 crew aleatoria viva.
        /// </summary>
        void ApplyPostActionDoTs(ICombatant actor, bool actorPassed);

        /// <summary>
        /// Aplica los DoTs de inicio de turno (Sangrado).
        /// Terrestre: al actor. Naval: a 1 crew aleatoria viva.
        /// </summary>
        void ApplyStartOfTurnDoTs(ICombatant actor);
    }
}
```

- `LandTurnResolver` encapsula la lógica de `ResolveAction` /
  `ResolveOffensiveAction` / DoTs que hoy vive **inline** en `CombatManager`
  (refactor de extracción, sin cambio de comportamiento → cubierto por los tests
  terrestres existentes como red de seguridad).
- `NavalTurnResolver` implementa las 6 acciones navales y el DoT split. Es el
  grueso del código nuevo de S4-04.

**Acciones navales** se modelan reutilizando `CombatAction` con dos extensiones
mínimas y aditivas (no rompen el camino terrestre):

```csharp
// CombatAction gana (campos nuevos, default = comportamiento terrestre):
//   ActionType extendido: Boarding, Maneuver, Repair  (Attack→Cañonazo reusa Attack)
//   TargetType extendido: SingleCrewEnemy
//   CrewMemberState TargetCrew;   // null salvo Abordaje / habilidad target=crew
```

| Acción naval | Modelado |
|--------------|----------|
| Cañonazo | `ActionType.Attack`, FPW vs HDF, AbilityPower 1.0, Neutral, daña HHP |
| Habilidad Naval | `ActionType.Ability` con `AbilityData` del `AbilityPool`, cuesta MP |
| Maniobra Evasiva | `ActionType.Maneuver` (nuevo) → `ship.IsManeuvering = true` |
| Abordaje | `ActionType.Boarding` (nuevo), `TargetType.SingleCrewEnemy`, FPW vs CrewDEF, daña `CrewMemberState` |
| Reparar | `ActionType.Repair` (nuevo), cura HHP con MST×REPAIR_POWER, cuesta MP, **no es habilidad** (funciona bajo Silencio) |
| Pasar | `ActionType.Pass` (reusado) |

### 4. Stats efectivos del barco y recálculo al morir crew (S4-03)

`ShipCombatant.GetEffectiveStat` delega en las fórmulas **estáticas ya
implementadas** en `ShipData` (`CalculateCrewContribution`, `GetUpgradeBonus`)
y suma `TraitBonuses` (vía BuffStack del barco, igual que terrestre):

```
EffectiveStat = ShipData.BaseStats[stat]
              + ShipData.GetUpgradeBonus(stat, Upgrades)
              + Σ ShipData.CalculateCrewContribution(crew vivo)   // solo crew NO muerta
              + BuffStack (synergy + buffs/debuffs de combate)
```

El recálculo al morir crew es el punto de integración crítico de S4-03:

```
NavalTurnResolver mata crew (Abordaje o DoT) →
  ship.RecalculateFromCrew():
    1. Excluye al crew muerto de las sumas de CrewContribution
    2. Reconstruye AbilityPool sin las SeaAbilities del crew muerto
    3. Si el muerto era el Capitán crew → CombatManager desactiva sinergias
       (mismo CheckCaptainKO, ver §5)
  GameEvents.PublishShipStatsRecalculated(ship)   // UI flash en stats que bajan
```

Se cachea el resultado y se invalida solo en `RecalculateFromCrew` o al
añadir/quitar buffs, para no recalcular en cada lectura de SPD de la Initiative
Bar (ver Performance Implications).

### 5. Sinergias navales — reuso de `SynergyEvaluator` (S4-03/S4-08)

`SynergyEvaluator` opera sobre `IReadOnlyList<CombatantState>` leyendo
`Template.Traits`. En naval, el "equipo" cuyas traits se evalúan es la **crew
del barco**, no otros barcos. La integración:

- El `ShipCombatant` expone la crew como portadores de traits. Para reusar
  `SynergyEvaluator` sin tocar su firma, se evalúa sobre la lista de
  `CharacterData` de la crew viva, con el **Capitán crew** como capitán primario
  y el **guest crew** como 2º capitán (S4-08). Los `TraitBonuses` resultantes se
  aplican como buffs **al BuffStack del barco** (no a crew individuales), porque
  el barco es quien tiene stats de combate.
- Muerte del Capitán crew → `CombatManager.CheckCaptainKO` (lógica reusada) →
  `SynergyEvaluator.RemoveBuffs` sobre el BuffStack del barco.
- Doble activación con guest (S4-08) → mismo mecanismo `Primary`/`Secondary` ya
  presente en `SynergyEvaluator` y en `CombatManager`.

**Decisión de firma:** se prefiere generalizar `SynergyEvaluator` para que
acepte un `IReadOnlyList<ITraitCarrier>` (interfaz extraída con
`Template.Traits`), de modo que tanto `CombatantState` como `CrewMemberState`
lo satisfagan, en lugar de fabricar `CombatantState` falsos para la crew. Es un
refactor pequeño y aditivo cubierto por los tests de sinergias terrestres.

### 6. Configuración de batalla y arranque

`BattleConfig` se generaliza para describir una batalla naval sin duplicar el
state machine. El `CombatManager` recibe un `ITurnResolver` y una lista de
`ICombatant` por oleada:

```
NavalBattleConfig (o BattleConfig con AllyShip + naval waves) →
  CombatManager.StartBattle(config, new NavalTurnResolver(...))
    PreCombat:
      - Construye ShipCombatant aliado (crew asignada + guest) y oleada 0 de enemigos
      - ShipCombatant.RecalculateFromCrew() (stats iniciales)
      - EvaluateAllySynergies sobre la crew (reusado)
      - Inicializa HHP/MP actuales, crew HP por rol
    InRound: pipeline reusado, fase de acción vía NavalTurnResolver
```

El barco aliado y los enemigos navales son **assets autorados** (`ShipData` +
crew + oleadas en `StageData` naval), cumpliendo la regla data-driven (S4-02/07).

### 7. Compatibilidad con grid táctico futuro (Open Question #1)

La abstracción `ITurnResolver` es exactamente el punto de extensión que protege
la evolución a grid. Un futuro `GridNavalTurnResolver` añadiría movimiento y
posicionamiento **sin tocar** stats, fórmulas, crew, roles ni el state machine:
el barco seguiría siendo un `ICombatant`, la crew seguiría siendo
`CrewMemberState`, y solo cambiaría cómo se resuelven las acciones (con coste de
movimiento, rango, line-of-fire). `ShipCombatant` puede ganar una posición
`GridCoord` opcional sin impacto en la demo. Por tanto, **el diseño no se cierra
la puerta**: el grid es un tercer `ITurnResolver`, no una reescritura.

### 8. Estructura de archivos

```
Assets/Scripts/Core/Combat/
├── CombatManager.cs           ← MODIFICADO: opera sobre ICombatant, delega a ITurnResolver
├── ICombatant.cs              ← NUEVO: contrato de combatiente
├── ITurnResolver.cs           ← NUEVO: estrategia de fase de acción + DoTs
├── LandTurnResolver.cs        ← NUEVO: extracción de la lógica terrestre actual (sin cambio de comportamiento)
├── NavalTurnResolver.cs       ← NUEVO (S4-04): 6 acciones navales + DoT split
├── ShipCombatant.cs           ← NUEVO (S4-02/03): runtime del barco, ICombatant
├── CrewMemberState.cs         ← NUEVO (S4-02): runtime de crew (HP por rol, atacable)
├── CombatantState.cs          ← MODIFICADO: implements ICombatant (+ IsImmuneTo)
├── InitiativeBar.cs           ← MODIFICADO: tipa por ICombatant (cambio de firma, no de lógica)
├── SynergyEvaluator.cs        ← MODIFICADO: opera sobre ITraitCarrier (aditivo)
├── CombatAction.cs            ← MODIFICADO: ActionType/TargetType extendidos + TargetCrew (aditivo)
└── (DamageCalculator, HealCalculator, ElementTable, BuffStack — sin cambios)

Assets/Tests/EditMode/Combat/
├── ShipCombatantTests.cs      ← NUEVO (S4-03): EffectiveStat, recálculo al morir crew
├── NavalTurnResolverTests.cs  ← NUEVO (S4-04): 6 acciones, DoT split, LB, oleadas (≥25)
└── (tests terrestres existentes actúan como red de seguridad del refactor)
```

### 9. Lo que se reusa as-is (sin cambios de comportamiento)

| Clase | Reuso | Cambio |
|-------|-------|--------|
| `CombatManager` (state machine, oleadas, muerte, eventos) | Total | Tipar por `ICombatant`, delegar fase de acción a `ITurnResolver` |
| `InitiativeBar` | Total | Solo cambio de firma a `ICombatant` |
| `DamageCalculator` / `HealCalculator` / `ElementTable` | Total | Ninguno (reciben floats) |
| `BuffStack` | Total | Ninguno |
| `SynergyEvaluator` | Total (lógica) | Tipar por `ITraitCarrier` |
| `ICombatInput` / `CombatContext` | Total | Ninguno (player/AI naval implementan lo mismo) |
| Fórmulas estáticas de `ShipData` | Total | Ninguno (ya implementadas) |

## Alternatives Considered

### Alternativa A — `NavalCombatManager` paralelo (copiar el state machine)

Duplicar `CombatManager` en un `NavalCombatManager` que entienda barcos y crew
desde cero.

- **Rejection Reason**: Es exactamente el riesgo "Duplicación masiva con
  CombatManager terrestre" que `sprint-004.md` pide mitigar. Se duplicarían
  ~800 líneas de state machine, oleadas, muerte, LB y eventos que son idénticas
  conceptualmente. Cada bugfix futuro (orden de DoT, edge case de oleada,
  inserción LB) habría que aplicarlo dos veces y divergirían con el tiempo.
  Viola el criterio de Maintainability del marco de decisión. El único 15-20%
  realmente distinto (la fase de acción) no justifica copiar el 80% común.

### Alternativa B — Herencia: `ShipCombatant : CombatantState` y barcos como unidades

Hacer que el barco herede de `CombatantState` y meter `if (isNaval)` en el
pipeline del `CombatManager` para el DoT split y las acciones navales.

- **Rejection Reason**: Contamina el camino terrestre estable con ramas navales
  (viola Constraint 1). `CombatantState` modela HP/MP de una unidad, no
  HHP/crew/casco; forzar un barco en esa jerarquía obliga a campos que no
  aplican y a un "crew" que no encaja en una unidad. La herencia acopla dos
  ciclos de vida distintos; un bug naval podría romper terrestre. La composición
  (`ICombatant` + `ITurnResolver`) da el mismo reuso sin el acoplamiento, y es
  más testeable (Testability) y reversible (Reversibility).

### Alternativa C — Crew como `ICombatant` de pleno derecho en la Initiative Bar

Modelar cada crew member como combatiente que ocupa una posición en la
Initiative Bar (como en un SRPG por unidad).

- **Rejection Reason**: Contradice el pilar de diseño del GDD ("el barco actúa
  como una sola entidad; la tripulación es equipo pasivo") y borraría el
  diferenciador naval (la decisión hundir-vs-abordar). Técnicamente obligaría a
  casos especiales "saltar el turno del crew" en toda la Initiative Bar y el
  pipeline. Mantener el crew como sub-entidad del barco es más simple
  (Simplicity) y fiel al diseño (Correctness).

## Consequences

### Positive

- **Cero duplicación del orquestador**: un solo `CombatManager` y una sola
  `InitiativeBar` para ambos modos; los bugfixes se aplican una vez.
- **Camino terrestre intacto**: el comportamiento terrestre se preserva tras la
  extracción a `LandTurnResolver`, con los tests terrestres existentes como red
  de seguridad (no se añaden ramas `if (naval)`).
- **Diferencia naval localizada**: todo lo que es de verdad distinto vive en
  `NavalTurnResolver` + `ShipCombatant` + `CrewMemberState`. Fácil de razonar y
  de testear aislado.
- **Sinergias y fórmulas reusadas**: `SynergyEvaluator`, `DamageCalculator` y
  las fórmulas de `ShipData` se aprovechan tal cual.
- **Grid-ready**: el grid futuro es un tercer `ITurnResolver`, no una
  reescritura — protege la Open Question #1 sin coste en la demo.
- **Data-driven**: barcos/crew/oleadas como assets, cumpliendo la regla retro S3.

### Negative

- **Refactor previo necesario (S4-02 antes de S4-04)**: extraer `ICombatant` y
  `LandTurnResolver` toca código terrestre estable. Es un coste real aunque sea
  mecánico y esté respaldado por tests.
- **Una indirección más**: `ITurnResolver` añade un nivel de delegación frente
  al `CombatManager` actual que resuelve inline. Justificado por evitar la
  duplicación, pero es complejidad estructural nueva.
- **`CombatAction` crece**: nuevos `ActionType`/`TargetType` y `TargetCrew`. Una
  sola struct compartida sirve a dos dominios; aceptable mientras los campos
  extra sean default-inertes en terrestre.

### Risks (con mitigación)

| Riesgo | Mitigación |
|--------|-----------|
| El refactor de extracción rompe comportamiento terrestre | Hacerlo como S4-02a sin cambios de lógica; los tests terrestres existentes deben pasar verdes antes y después (gate de la tarea) |
| `ICombatant` se infla intentando cubrir crew + barco + unidad | Mantenerlo en el mínimo que Initiative Bar + pipeline necesitan; el crew NO es `ICombatant` |
| Recálculo de stats al morir crew se vuelve hot path | Cachear `EffectiveStat`, invalidar solo en `RecalculateFromCrew`/cambio de buff (ver Performance) |
| Reuso de `SynergyEvaluator` fuerza `CombatantState` falsos para crew | Extraer `ITraitCarrier` en vez de fabricar estados falsos |
| Naval "se siente como terrestre con barcos" (riesgo #1 del sprint) | Es riesgo de **diseño**, no de arquitectura; la arquitectura habilita el DoT split, Abordaje y crew targeting que lo diferencian. Validar en S4-09 |

## Performance Implications

- **Presupuesto**: combate por turnos, sin requisitos de frame-time duros. El
  pipeline procesa un combatiente a la vez; no hay paralelismo. El presupuesto
  efectivo es "no generar hitches perceptibles entre turnos".
- **Recálculo de stats del barco**: `GetEffectiveStat` se llama con frecuencia
  (la Initiative Bar lee SPD al ordenar; las fórmulas leen FPW/HDF/MST/RSL por
  ataque). Recalcular crew contribution + upgrades en cada lectura sería O(slots)
  repetido. **Mitigación**: cachear el `ShipStatBlock` efectivo en
  `ShipCombatant` e invalidar solo en `RecalculateFromCrew` (muerte/cambio de
  crew) y al añadir/quitar buffs. Lecturas → O(1).
- **Crew y oleadas**: tamaños pequeños (≤7 crew, ≤3 oleadas, MAX_WAVES=3). Las
  iteraciones son triviales; no hay preocupación de memoria.
- **Asignaciones**: igual que terrestre, evitar `new List` por lectura en hot
  paths (la Initiative Bar ya tiene este patrón; replicarlo en
  `NavalTurnResolver` para targeting de crew).
- **Veredicto**: sin impacto sobre presupuestos de runtime; el único cuidado es
  el caché de stats efectivos del barco.

## Migration Plan

El combate naval es **aditivo** — no hay datos de jugador ni saves que migrar
(el combate es runtime-only, no persiste). La "migración" es de código,
secuenciada para minimizar riesgo sobre el camino terrestre:

1. **S4-02a — Extracción (refactor sin cambio de comportamiento)**:
   extraer `ICombatant` (implementado por `CombatantState`), `ITurnResolver`
   con `LandTurnResolver` (mover la lógica inline actual del `CombatManager`),
   tipar `InitiativeBar` y `SynergyEvaluator` por las interfaces.
   **Gate**: todos los tests terrestres existentes pasan verdes.
2. **S4-02b — Runtime naval de entidad**: `ShipCombatant`, `CrewMemberState`,
   construcción desde `ShipData` + crew. Assets autorados (≥1 barco aliado, ≥3
   enemigos, ≥1 criatura). EditMode tests de construcción.
3. **S4-03 — Stats efectivos + recálculo**: `GetEffectiveStat` con caché,
   `RecalculateFromCrew`, integración de sinergias de crew. EditMode tests.
4. **S4-04 — `NavalTurnResolver`**: 6 acciones, DoT split, LB naval, oleadas.
   ≥25 EditMode tests. Cubre AC 1-24 + 33-37 del GDD.
5. **S4-05/06/07** — enemigos naval AI, UI naval, stage e integración de flujo
   (fuera del alcance arquitectónico de esta ADR).

**Reversibilidad**: si la abstracción resultara sobreingeniería, revertir es
acotado — el `LandTurnResolver` puede re-inlinearse en `CombatManager` y el
trabajo naval queda contenido en sus propios archivos.

## Validation Criteria

Sabremos que esta decisión fue correcta si:

1. **No hay un segundo state machine**: existe un único `CombatManager` y una
   única `InitiativeBar` sirviendo a terrestre y naval (grep no encuentra
   `NavalCombatManager` ni `BattlePhase` duplicado).
2. **Cero ramas `if (naval)` en el camino compartido**: el `CombatManager` no
   contiene condicionales por tipo de batalla; la diferencia vive en el
   `ITurnResolver` inyectado.
3. **Tests terrestres verdes tras el refactor**: la suite EditMode terrestre
   pasa sin cambios de aserciones tras la extracción (red de seguridad).
4. **≥25 EditMode tests navales** (DoD S4-04) cubriendo las 6 acciones, DoT
   split, recálculo al morir crew, LB naval y persistencia de oleada — sin Play
   Mode.
5. **Recálculo al morir crew correcto y barato**: matar un crew member baja los
   stats efectivos del barco y retira sus SeaAbilities en el mismo turno, y
   `GetEffectiveStat` no recalcula crew contribution en cada lectura (caché).
6. **Grid no requiere reescritura**: un spike de `GridNavalTurnResolver`
   (post-demo) no obliga a tocar `CombatManager`, `ShipCombatant`,
   `CrewMemberState` ni las fórmulas — solo añade un resolver.
7. **Naval data-driven**: la batalla naval arranca desde assets autorados
   (barco + crew + oleadas) visibles en juego, sin valores hardcoded.

## Related Decisions

- **ADR-003** (Combat Architecture) — esta ADR generaliza el `CombatManager`,
  `InitiativeBar` e `ICombatInput` allí definidos vía `ICombatant` +
  `ITurnResolver`, sin cambiar su comportamiento terrestre.
- **ADR-001** (GameEvents event bus) — el combate naval publica por el mismo bus
  (`OnShipStatsRecalculated`, `OnCrewDied`, etc., como extensiones aditivas).
- **GDD Combate Naval** (`design/gdd/combate-naval.md`) — fuente de las 6
  acciones, DoT split, crew HP por rol, LB naval, oleadas, AC 1-37.
- **GDD Ship Data Model** (`design/gdd/ship-data-model.md`) — define
  `EffectiveStat`, crew contribution y upgrades, ya implementados como fórmulas
  estáticas en `ShipData`.
- **GDD Traits/Sinergias** — reusado vía `SynergyEvaluator`; el Capitán crew es
  el capitán primario, el guest crew el secundario (S4-08).
- **Open Question #1 (GDD)** — evolución a grid táctico post-demo; protegida por
  `ITurnResolver` (decisión: no implementar, no cerrar la puerta).
