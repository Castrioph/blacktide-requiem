# Enemy System

> **Status**: Approved
> **Author**: User + Claude Code Game Studios agents
> **Last Updated**: 2026-03-26
> **Implements Pillar**: Pillar 1 (Profundidad Estrategica Dual), Pillar 4 (Respeto al Tiempo del Jugador)

## Overview

The Enemy System defines how enemies are constructed, categorized, and configured
for both land and naval combat. It extends the shared `CharacterData` base with
enemy-specific data (`EnemyData`): AI behavior profiles, loot tables, elemental
identity, and zone-based stat variants.

Enemies are organized into three tiers — **Normales** (fodder), **Elites**
(mini-bosses with mechanics), and **Jefes** (bosses with phases, CC/execution
immunity, and Limit Breaks). Each enemy template defines stats, abilities, traits,
and element for both land and sea contexts, though most enemies appear in only one
mode.

The player never interacts with this system directly — they experience it as the
opposition in combat. The system succeeds when enemies feel like deliberate
challenges that reward preparation (reading elements, exploiting traits, targeting
synergy-granters) rather than stat walls to grind through.

This is the bridge between the Unit Data Model (what an enemy IS) and the Combat
systems (how an enemy FIGHTS). It is consumed by Combate Terrestre, Combate Naval,
Stage System, and Combat UI.

## Player Fantasy

**"Every enemy is a puzzle, not a punching bag."** The Enemy System serves three
emotional beats:

**The read**: Before combat starts, the player sees enemy elements, traits, and
tier icons. A veteran player scans the enemy lineup and thinks "Maldición element,
Undead trait — I need Tormenta damage and I should kill the Skeleton Captain first
to break their synergy." This moment of preparation is where Pillar 1 (strategic
depth) lives.

**The pressure**: Elites and bosses change the flow of combat. A boss that enters
a rage phase, gains CC immunity, or uses a Limit Break forces the player to adapt
mid-fight. The player who planned well has an advantage but still needs to
execute — auto-battle won't cut it for hard content (Pillar 4).

**The satisfaction of mastery**: Returning to earlier stages and watching fodder
enemies fall to auto-battle. The contrast between trivial fodder and demanding
bosses makes both feel right — easy content respects your time, hard content
respects your skill.

The system fails if all enemies feel like the same stat block with different
sprites, if bosses are just "normal enemy but more HP", or if there's no reason
to read the enemy lineup before pressing auto.

## Detailed Design

### Core Rules

#### 1. EnemyData (extends CharacterData)

| Field | Type | Description |
|-------|------|-------------|
| `EnemyTier` | enum | Normal, Elite, Jefe |
| `CombatContext` | enum | Land, Sea, Both |
| `AIProfile` | AIProfileId | Reference to AI behavior (profile or behavior tree) |
| `LootTable` | LootTableId | Reference to loot drop configuration |
| `ZoneVariants` | List\<ZoneVariant\> | Stat/ability overrides per zone (see §6) |
| `BossData` | BossInfo? | Null for Normal/Elite. For Jefes: phases, immunities, LB |
| `NavalForm` | enum? | Ship, Creature, Fortress. Null for land-only enemies |
| `XPReward` | int | Base XP granted on kill |
| `DOBReward` | int | Base Doblones granted on kill |
| `IsEncounterCaptain` | bool | If true, this enemy is the captain of its encounter group. Activates enemy synergies via its traits. Killing it deactivates all enemy synergies. Max 1 per wave. (See Traits/Sinergias GDD §4.) |

All `CharacterData` fields are inherited: BaseStats, SecondaryStats, LandAbilities,
SeaAbilities, Element, Traits, VisualData.

#### 2. Enemy Tiers

| Tier | HP Budget | Ability Count | Traits | AI | Special |
|------|-----------|---------------|--------|-----|---------|
| **Normal** | 1x | 2-3 | 0-1 | Profile | None |
| **Elite** | 3-5x | 3-5 | 1-2 | Profile+ (conditional priority) | 1-2 signature mechanics |
| **Jefe** | 8-15x | 5-8 | 2-3 | Behavior tree | Phases, CC/Muerte immunity, can have Limit Break |

- **Normal**: Cannon fodder. Auto-battle handles them. Exist to make the player
  feel powerful and to pad waves. Die in 1-3 hits.
- **Elite**: Named enemies with a gimmick (e.g., "heals allies each turn",
  "counters physical attacks", "buffs ATK when an ally dies"). Force the player
  to prioritize targets. Appear 1-2 per wave.
- **Jefe**: Stage bosses. Multiple HP phases, immune to CC (Sueño, Aturdimiento)
  and Muerte. Can use Limit Breaks. Demand manual play and team preparation.

#### 3. Boss Phases (Jefes only)

Bosses have 1-3 phases defined by HP thresholds:

| Field | Type | Description |
|-------|------|-------------|
| `PhaseThresholds` | List\<float\> | HP % thresholds where phases change (e.g., [0.75, 0.40]) |
| `PhaseAbilities` | List\<List\<AbilityEntry\>\> | Abilities available per phase |
| `PhaseAIProfile` | List\<AIProfileId\> | AI behavior per phase |
| `PhaseOnEnter` | List\<PhaseAction?\> | Action on entering a phase (buff, summon adds, dialogue) |

Phase transitions:
- When boss HP crosses a threshold, the current phase ends and the next begins
- `PhaseOnEnter` triggers immediately (interrupts normal turn flow)
- Phase actions: self-buff, summon adds, cleanse debuffs, change element, dialogue line
- Phases are one-directional (no going back to phase 1 if healed above threshold)
- Healing a boss above a threshold does NOT revert the phase

#### 4. Boss Immunities

| Immunity | Applies To | Rationale |
|----------|-----------|-----------|
| **Muerte** (execution) | All Jefes | Prevents trivializing boss HP pool |
| **Aturdimiento** (stun) | All Jefes | Prevents stun-lock on bosses |
| **Sueño** (sleep) | All Jefes | Prevents skipping boss turns entirely |

Bosses are NOT immune to: Veneno, Sangrado, Quemadura (DoTs), Ceguera, Silencio,
stat debuffs. This is intentional — debuff strategies remain viable against bosses,
but you can't skip their turns.

#### 5. Naval Enemy Forms

| Form | Description | Stats Source | Example |
|------|-------------|-------------|---------|
| **Ship** | Enemy vessel with crew | Ship stats (FPW, HDF, MST, RSL, HHP) | Pirate frigate, Marine galleon |
| **Creature** | Sea monster | CharacterData stats mapped to naval equivalents | Kraken, Sea Serpent |
| **Fortress** | Fortified island/stronghold | High HDF/RSL, low FPW. Stationary. | Fort, Watchtower, Isla Maldita |

- **Ships** use ship stats directly (same as player ships from SDM)
- **Creatures** use a stat mapping: ATK→FPW, DEF→HDF, MST→MST, SPR→RSL, HP→HHP.
  This lets them be defined with CharacterData and fight in naval context.
- **Fortresses** are a special category: extremely high defense, no movement,
  unique abilities (area bombardment, reinforcement summoning). They are Jefe-tier
  by default.

All naval enemy forms are immune to Sueño and Aturdimiento on the initiative bar
(same as player ships, per Initiative Bar GDD).

#### 6. Zone Variants

Enemies can have stat/ability overrides per zone (story chapter, event, difficulty):

| Field | Type | Description |
|-------|------|-------------|
| `ZoneId` | string | Zone identifier (e.g., `"chapter_2"`, `"event_halloween"`) |
| `StatMultiplier` | float | Multiplier applied to all base stats (e.g., 1.5 = 50% stronger) |
| `OverrideElement` | Element? | Change element for this zone (null = keep default) |
| `AdditionalAbilities` | List\<AbilityEntry\> | Extra abilities in this zone |
| `OverrideLoot` | LootTableId? | Different loot for this zone |

This allows reusing enemy templates across zones with scaling difficulty without
creating hundreds of unique enemy definitions.

#### 7. AI System (Hybrid)

**Profiles (Normal/Elite):**

Each profile defines targeting priority and ability selection:

| Profile | Target Priority | Ability Priority | Used By |
|---------|----------------|-----------------|---------|
| **Agresivo** | Lowest HP ally | Highest damage ability with enough MP | Melee attackers, beasts |
| **Estratega** | Highest ATK/MST ally | Elemental advantage ability if available | Casters, elite enemies |
| **Soporte** | Lowest HP ally (enemy side) | Heal/buff if ally HP < 50%, else attack | Healers, commanders |
| **Defensivo** | Self or lowest HP ally | Buff DEF/SPR if no buffs active, else attack | Tanks, guards |
| **Caótico** | Random target | Random ability | Beasts, undead fodder |

Profile+ (Elites) adds conditional overrides:
- "If own HP < 30%, use heal ability"
- "If ally count < 2, use enrage buff"
- "Prioritize target with [trait] if present"

**Behavior Trees (Jefes):**

Bosses use scripted decision trees per phase:

```
Phase 1 (HP > 75%):
  → If no buffs active: cast ATK buff
  → If ally count < max: summon adds (once per phase)
  → Else: use strongest ability on highest-threat target

Phase 2 (HP 40-75%):
  → If debuffs active: cleanse self
  → Use area ability every 3 turns
  → Else: target healer with strongest single-target

Phase 3 (HP < 40%):
  → Limit Break available: use when SPD condition met
  → Use strongest ability every turn
  → Enrage buff (permanent +30% ATK)
```

Each boss has a unique tree authored per encounter. The system provides building
blocks (conditions, actions, selectors) and designers compose them.

#### 8. Loot Tables

| Field | Type | Description |
|-------|------|-------------|
| `Drops` | List\<LootDrop\> | Possible drops |
| `GuaranteedDrops` | List\<ItemRef\> | Always dropped (e.g., boss-specific material) |

**LootDrop:**

| Field | Type | Description |
|-------|------|-------------|
| `ItemId` | string | What drops |
| `Quantity` | Range(min, max) | How many |
| `DropRate` | float (0-1) | Base probability (modified by LCK) |
| `TierRestriction` | EnemyTier? | Only from this tier (null = any) |

Final drop rate: `EffectiveRate = DropRate × (1.0 + KillerLCK / 100)` (per UDM
LCK formula).

### States and Transitions

| State | Description | Transitions To |
|-------|-------------|----------------|
| **Template** | Data-only definition in enemy database | Spawned |
| **Spawned** | Instantiated in a combat encounter with runtime state | Active, Dead |
| **Active** | Currently alive and participating in combat | Dead, Phase Transition (Jefes) |
| **Phase Transition** | Boss crossing HP threshold, executing PhaseOnEnter | Active (next phase) |
| **Dead** | HP = 0. Removed from combat. Loot calculated. | — (terminal) |

- Enemies are **never persistent** — they exist only during combat
- Runtime state (HP, MP, buffs, debuffs) follows DSE per-combatant state
- Dead enemies trigger: loot roll, wave completion check. If the dead enemy is the encounter captain (`IsEncounterCaptain = true`), all enemy synergies deactivate immediately (see Traits/Sinergias GDD §4)

### Interactions with Other Systems

| System | Direction | Data Interface |
|--------|-----------|----------------|
| **Unit Data Model** | UDM → Enemy | EnemyData extends CharacterData base. Shares stats, abilities, traits, element. |
| **Damage & Stats Engine** | DSE ↔ Enemy | Enemy stats feed into DSE formulas identically to player units. DSE processes enemy buffs, debuffs, DoTs, and status effects. |
| **Initiative Bar** | Enemy → IB | Enemy SPD determines turn order. Boss tie-breaking priority (Bosses > Allies > Normals). Jefes can use Limit Breaks. |
| **Combate Terrestre** | Enemy → CT | CT spawns enemies per stage wave config. CT calls DSE with enemy stats for damage calc. CT processes enemy AI decisions. |
| **Combate Naval** | Enemy → CN | CN spawns naval enemies (Ships, Creatures, Fortresses). Same flow as CT but with naval stats. |
| **Stage System** | Stage → Enemy | Stage config defines which enemies appear in which waves, with zone variant overrides. |
| **Traits/Sinergias** | Enemy ↔ Traits | Enemy encounter captain's traits activate side-specific synergies (Captain model). Killing the encounter captain deactivates all enemy synergies immediately. Non-captain enemy deaths do not affect synergy state. See Traits/Sinergias GDD §4. |
| **Rewards System** | Enemy → Rewards | Enemy loot tables feed into post-combat reward calculation. XP and DOB rewards per enemy. |
| **Combat UI** | Enemy → UI | UI reads enemy tier (for icon display), element, HP, status effects, boss phase indicator. |
| **Auto-Battle** | Enemy → Auto | Auto-Battle AI reads enemy stats, element, and tier to make targeting decisions. |

## Formulas

> **Note**: These formulas are intentionally simple for the demo. They will be
> refined as playtesting reveals balance needs.

### Enemy Stat Scaling

```
EnemyStat(zone) = BaseStat × ZoneStatMultiplier
```

- Normales use base stats directly
- Zone multiplier scales all stats proportionally

### HP Budget by Tier

```
NormalHP  = BaseHP × ZoneStatMultiplier
EliteHP   = BaseHP × ZoneStatMultiplier × ELITE_HP_MULTIPLIER
JefeHP    = BaseHP × ZoneStatMultiplier × BOSS_HP_MULTIPLIER
```

### XP Reward

```
XPReward = BASE_ENEMY_XP × TierMultiplier × ZoneStatMultiplier
```

| Tier | TierMultiplier |
|------|---------------|
| Normal | 1.0 |
| Elite | 3.0 |
| Jefe | 10.0 |

### DOB Reward

```
DOBReward = BASE_ENEMY_DOB × TierMultiplier × ZoneStatMultiplier
```

### Loot Drop Rate

```
EffectiveRate = BaseDropRate × (1.0 + KillerLCK / 100)
```

- LCK 0 = 1.0x (base rate)
- LCK 50 = 1.5x
- LCK 100 = 2.0x

### Creature Stat Mapping (Naval)

```
FPW = ATK, HDF = DEF, MST = MST, RSL = SPR, HHP = HP
```

Creatures use CharacterData stats mapped 1:1 to naval equivalents.

### Variable Definitions

| Variable | Type | Default | Range | Description |
|----------|------|---------|-------|-------------|
| BASE_ENEMY_XP | int | 20 | 10-50 | XP granted per Normal enemy kill |
| BASE_ENEMY_DOB | int | 10 | 5-30 | DOB granted per Normal enemy kill |
| ELITE_HP_MULTIPLIER | float | 4.0 | 3.0-5.0 | HP multiplier for Elites over Normals |
| BOSS_HP_MULTIPLIER | float | 12.0 | 8.0-15.0 | HP multiplier for Jefes over Normals |
| ZoneStatMultiplier | float | 1.0 | 1.0-5.0 | Per-zone stat scaling (defined in stage config) |

### Worked Example — Early Game Normal (Land)

```
Skeleton Pirate (Normal, Zone: Chapter 1)
BaseHP = 400, BaseATK = 45, BaseDEF = 30
ZoneStatMultiplier = 1.0

HP = 400, ATK = 45, DEF = 30
Abilities: Basic Attack (1.0), Cutlass Slash (1.2 AbilityPower)
Element: Acero, Traits: ["Undead"]

XPReward = 20 × 1.0 × 1.0 = 20 XP
DOBReward = 10 × 1.0 × 1.0 = 10 DOB
```

### Worked Example — Mid Game Elite (Land)

```
Skeleton Captain (Elite, Zone: Chapter 3)
BaseHP = 600, BaseATK = 70, BaseDEF = 55
ZoneStatMultiplier = 1.8
ELITE_HP_MULTIPLIER = 4.0

HP = 600 × 1.8 × 4.0 = 4,320
ATK = 70 × 1.8 = 126, DEF = 55 × 1.8 = 99
Abilities: Basic Attack, Cutlass Fury (2.0), Rally Undead (+20% ATK buff to allies)
Element: Maldición, Traits: ["Undead", "Commander"]

XPReward = 20 × 3.0 × 1.8 = 108 XP
DOBReward = 10 × 3.0 × 1.8 = 54 DOB
```

### Worked Example — Boss (Land)

```
Barbanegra (Jefe, Zone: Chapter 5 Final)
BaseHP = 1,200, BaseATK = 120, BaseDEF = 90
ZoneStatMultiplier = 2.5
BOSS_HP_MULTIPLIER = 12.0

HP = 1,200 × 2.5 × 12.0 = 36,000
ATK = 120 × 2.5 = 300, DEF = 90 × 2.5 = 225

Phase 1 (HP > 75%): Normal attacks + ATK buff
Phase 2 (HP 40-75%): AoE Pólvora ability + summon 2 Normal adds
Phase 3 (HP < 40%): Enrage (+30% ATK permanent), Limit Break available

XPReward = 20 × 10.0 × 2.5 = 500 XP
DOBReward = 10 × 10.0 × 2.5 = 250 DOB
GuaranteedDrop: "Barbanegra's Cutlass" (awakening material)
```

## Edge Cases

| Edge Case | Resolution |
|-----------|------------|
| **Boss healed above phase threshold** | Phase does NOT revert. Phases are one-directional. |
| **All adds killed, boss in phase that summons adds** | Boss can resummon if PhaseOnEnter allows. "Once per phase" summons cannot repeat. |
| **Elite with heal ability at 0 allies** | Heals self instead. Soporte profile falls back to self-target. |
| **Enemy captain is the last one alive** | Captain is still alive, so enemy synergies remain active (count is based on starting encounter composition, not survivors). Captain's own buff still applies if threshold was met at encounter start. |
| **Boss phase transition mid-turn (e.g., DoT crosses threshold)** | Phase transition triggers after current action resolves, before next unit acts. PhaseOnEnter executes immediately. |
| **Multiple enemies killed simultaneously (AoE)** | Loot calculated per enemy. XP/DOB summed. If the encounter captain is among the killed, all enemy synergies deactivate after the AoE resolves. |
| **Creature in naval combat receives ship-only status** | Creatures follow same immunity rules as ships (immune to Sueño, Aturdimiento on initiative bar). |
| **Fortress with 0 FPW (no attack stat)** | Fortresses can still use abilities (bombardment, reinforcement). They don't need FPW for ability-based damage (uses AbilityPower × MST or custom formula). |
| **Zone variant changes element mid-stage** | Element is set at spawn. If zone overrides element, all enemies of that template in that zone use the override. |
| **Boss uses Limit Break but SPD condition not met** | LB not triggered. Boss uses normal ability from behavior tree instead. |
| **Enemy MP runs out** | Enemy uses Basic Attack (0 MP) like player units. Enemies should be designed with enough MP to last the fight. |
| **Normal enemy with 0 traits** | Valid. No synergies contributed or received. Simplest possible enemy. |

## Dependencies

### Upstream Dependencies

| System | Dependency Type | Interface |
|--------|----------------|-----------|
| Unit Data Model | Hard | EnemyData extends CharacterData base. All stats, abilities, traits, element inherited. |
| Damage & Stats Engine | Hard | Enemies use identical formulas for damage, healing, buffs, DoTs, status effects. |

### Downstream Dependencies

| System | Dependency Type | Interface |
|--------|----------------|-----------|
| Combate Terrestre | Hard | CT spawns land enemies, processes their AI, calls DSE with their stats. |
| Combate Naval | Hard | CN spawns naval enemies (Ship/Creature/Fortress), same flow. |
| Initiative Bar | Hard | Reads enemy SPD for turn order. Handles boss tie-breaking and boss Limit Breaks. |
| Traits/Sinergias | Hard | Reads enemy traits for side-specific synergy calculation. |
| Rewards System | Soft | Reads XPReward, DOBReward, and loot tables post-combat. |
| Combat UI | Soft | Reads tier, element, HP, boss phase for display. |
| Auto-Battle | Soft | Reads enemy tier and element for targeting priority. |
| Stage System | Hard | Defines which enemies appear in which waves and zones. |

**Bidirectional note:** The DSE GDD already references Enemy System as a consumer
(§Interactions). The UDM GDD already defines the EnemyData extension point. No
updates needed to upstream docs.

## Tuning Knobs

| Knob | Current Value | Range | What It Affects | If Too High | If Too Low |
|------|--------------|-------|----------------|-------------|------------|
| ELITE_HP_MULTIPLIER | 4.0 | 3.0-5.0 | Elite tankiness | Elites feel like bosses, waves take too long | Elites die as fast as normals, no threat |
| BOSS_HP_MULTIPLIER | 12.0 | 8.0-15.0 | Boss fight length | Fights are a slog (Pillar 4 violation) | Boss phases fly by, no tension |
| BASE_ENEMY_XP | 20 | 10-50 | XP income from combat | Leveling too fast, content outpaced | Leveling too slow, grinding required |
| BASE_ENEMY_DOB | 10 | 5-30 | DOB income from combat | Economy inflation, sinks become trivial | Currency-starved, frustrating |
| Boss phase thresholds | [0.75, 0.40] | 0.20-0.90 | When boss behavior changes | Too many phase changes feel chaotic | Too few phases, boss is monotonous |
| Profile+ HP threshold | 30% | 15-50% | When elites use emergency abilities | Emergency mode triggers too often | Elite never uses emergency behavior |
| ZoneStatMultiplier per chapter | 1.0, 1.3, 1.6, 1.8, 2.5 | 1.0-5.0 | Difficulty curve across chapters | Spikes too fast, player hits wall | Too flat, no challenge progression |

### Knob Interactions (Danger Zones)

| Knob A | Knob B | Interaction |
|--------|--------|-------------|
| BOSS_HP_MULTIPLIER | Boss phase thresholds | More HP × more phases = very long fights. Tune together. |
| ZoneStatMultiplier | Player stat growth (UDM) | Enemy scaling must track player scaling. If enemies scale faster, difficulty spikes. |
| BASE_ENEMY_XP | ZoneStatMultiplier | XP scales with zone multiplier. If both increase, leveling accelerates too fast in late zones. |
| ELITE_HP_MULTIPLIER | Number of elites per wave | More elites × more HP = longer waves. Keep total wave time reasonable. |

## Visual/Audio Requirements

### Visual

- **Tier indicators**: Icono sobre el sprite del enemigo
  - Normal: sin indicador (son los "default")
  - Elite: borde plateado + icono de espadas cruzadas
  - Jefe: borde dorado + icono de calavera
- **Element badge**: Icono del elemento defensivo junto a la barra de HP (mismo
  sistema de colores que DSE)
- **Boss phase indicator**: Barra de HP dividida en segmentos por fase. Segmentos
  futuros son opacos, el actual es brillante. Al cruzar un threshold: flash +
  efecto de transición
- **PhaseOnEnter visual**: Cámara hace zoom al boss, breve animación (buff aura,
  grietas en el suelo, adds spawneando). 1-2 segundos máximo
- **Fortress sprites**: Más grandes que barcos normales. Ocupan más espacio visual.
  Efecto de "inmóvil" (no se balancea con las olas)
- **Trait synergy visual**: Líneas tenues conectan enemigos que comparten traits
  activos. Se desvanecen cuando un enemigo con ese trait muere
- **Death animation**: Normales se desvanecen rápido (~0.5s). Elites tienen muerte
  más dramática (~1s). Jefes tienen secuencia especial (~2s con efectos)

### Audio

- **Tier-based SFX**: Normales tienen SFX genéricos por tipo (skeleton, beast,
  pirate). Elites tienen SFX ligeramente más intensos. Jefes tienen SFX únicos
  por boss
- **Phase transition**: SFX dramático (rumble + campanada ominosa) al cambiar de fase
- **Boss Limit Break**: SFX especial + breve silencio antes del impacto (build tension)
- **Fortress bombardment**: SFX de cañones pesados, diferente a cañones de barco
  normales
- **Enemy death**: SFX escalado por tier (pop rápido para normales, crash para
  elites, explosión épica para jefes)

## UI Requirements

- **Pre-combat screen**: Lista de enemigos por wave con: sprite, tier icon, element
  badge, nombre. Jefes destacados con borde dorado
- **In-combat enemy display**: Sprite + HP bar + element badge + status effect
  icons + boss phase segments
- **Target selection**: Al seleccionar target, mostrar: nombre, tier, element,
  HP actual/máx, debuffs activos. Para jefes: fase actual y siguiente threshold
- **Boss phase HUD**: Barra de HP con divisores visuales en cada threshold. Número
  de fase ("Fase 2/3") junto al nombre del boss
- **Wave counter**: "Oleada 2/4" en esquina superior para que el jugador sepa
  cuánto falta
- **Loot preview** (post-combat): Lista de drops por enemy con icono, nombre,
  cantidad. Items raros con efecto de brillo
- **Enemy bestiary** (future): Colección de enemigos encontrados con stats,
  element, drops conocidos. No es MVP pero el data model lo soporta

## Acceptance Criteria

| # | Criterion | How to Verify |
|---|-----------|---------------|
| 1 | EnemyData extends CharacterData and compiles with all inherited + enemy-specific fields | Unit test: create EnemyData instance, verify all fields accessible |
| 2 | Three tiers (Normal, Elite, Jefe) produce correct HP budgets | Unit test: same BaseHP × tier multipliers → expected HP values |
| 3 | Boss phases transition at correct HP thresholds | Integration test: deal damage to cross threshold → phase changes, PhaseOnEnter triggers |
| 4 | Boss phases are one-directional (healing above threshold does not revert) | Integration test: heal boss above threshold → phase remains |
| 5 | Jefes are immune to Muerte, Aturdimiento, and Sueño | Integration test: apply each to boss → "IMMUNE", no effect |
| 6 | Jefes are NOT immune to DoTs, Ceguera, Silencio, debuffs | Integration test: apply each to boss → effect applies normally |
| 7 | Naval creatures use correct stat mapping (ATK→FPW, etc.) | Unit test: creature with ATK=100 → FPW=100 in naval context |
| 8 | Fortresses are immune to Sueño/Aturdimiento on initiative bar | Integration test: apply CC to fortress → no effect |
| 9 | Zone variants apply correct stat multiplier | Unit test: enemy with StatMultiplier=1.5 → all stats × 1.5 |
| 10 | Zone element override replaces default element | Unit test: enemy Acero + zone override Maldición → spawns as Maldición |
| 11 | AI profiles select correct targets per priority | Integration test: Agresivo profile → attacks lowest HP ally |
| 12 | Profile+ conditional overrides trigger correctly | Integration test: elite HP < 30% → uses heal ability |
| 13 | Boss behavior tree follows phase-specific logic | Integration test: boss in phase 2 → uses phase 2 abilities/priorities |
| 14 | Loot tables produce correct drop rates modified by LCK | Statistical test: 10,000 kills with LCK=50 → ~1.5x base drop rate |
| 15 | XP/DOB rewards scale correctly with tier and zone | Unit test: Elite in zone 1.8 → XP = 20 × 3.0 × 1.8 = 108 |
| 16 | Killing encounter captain deactivates all enemy trait synergies immediately | Integration test: kill encounter captain → all enemies lose synergy buffs. Kill non-captain enemy → synergies unaffected. |
| 17 | Boss Limit Break triggers only when SPD condition is met | Integration test: boss with LB + SPD condition not met → uses normal ability |
| 18 | Enemy death triggers loot roll, XP grant, and wave check | Integration test: last enemy dies → rewards calculated, wave advances |
| 19 | Enemies ignore MP for ability selection (always can use pattern abilities) | Integration test: enemy with 0 MP → still uses non-basic abilities |
| 20 | Boss element change via PhaseOnEnter updates element badge visually | Visual test: boss crosses phase threshold with element change → badge animates to new element |

## Open Questions

| # | Question | Impact | Status / Resolution |
|---|----------|--------|---------------------|
| 1 | ~~Should Elites have partial CC resistance?~~ | Balance | **Resolved**: Elites have no base CC resistance. Individual enemy profiles can add specific immunities as needed. Bosses have full CC immunity (Sueño, Aturdimiento, Muerte) as a base rule. |
| 2 | ~~How do Fortresses interact with ship movement/positioning?~~ | Naval design | **Resolved**: Fortresses are a post-demo feature. Not included in MVP. Defer full design to Combate Naval GDD when implemented. |
| 3 | ~~Should enemies have visible MP bars?~~ | UI / Design | **Resolved**: Enemies do not use MP. They always execute their AI pattern without MP constraints. MP field exists in EnemyData for DSE compatibility but is ignored for ability selection. Silencio still blocks abilities (independent of MP). |
| 4 | ~~Can bosses change element mid-fight via PhaseOnEnter?~~ | Strategic depth | **Resolved**: Yes. Visual indicator only — flash of new element color over boss + element badge animates to new color (~1s). No text popup. |
| 5 | ~~Should the bestiary track discovered weaknesses?~~ | Discovery | **Resolved**: Yes, future feature. When the player successfully exploits an elemental advantage, that enemy's weakness is permanently revealed in the bestiary. Not MVP. |
| 6 | ~~How many unique enemy templates for the demo?~~ | Content scope | **Resolved**: ~15 land enemies + ~6 naval enemies for the demo. |
| 7 | Should certain Elites be immune to specific status effects beyond the base rules? | Per-enemy design | Open — decided per enemy template during content authoring |
| 8 | How does Silencio interact with enemies that have no MP? Does it still block ALL non-basic abilities? | Design consistency | Open — current ruling: yes, Silencio blocks ability use regardless of MP. Validated by DSE §6. |
