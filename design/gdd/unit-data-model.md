# Unit Data Model

> **Status**: Approved
> **Author**: User + Claude Code Game Studios agents
> **Last Updated**: 2026-03-25
> **Implements Pillar**: Pillar 1 (Profundidad Estratégica Dual), Pillar 2 (Personajes con Alma), Pillar 3 (Recompensa a la Paciencia)

## Overview

The Unit Data Model defines the foundational data structure for all characters
in the game — both player-owned units and enemies. It establishes a shared base
(`CharacterData`) containing stats, abilities (land and sea sets), and traits,
with specialized extensions for player units (`UnitData`: progression, equipment,
rarity, gacha metadata) and enemies (`EnemyData`: AI behavior, loot, zone
variants — designed in the Enemy System GDD).

Players collect units through the gacha system, level them up, equip items,
awaken them with duplicates, and deploy them in two distinct combat contexts:
land teams (5+1 formation) and naval crews (ship role slots). Each unit has
separate ability sets for land and sea combat, meaning a unit's value depends
on the context — a mediocre land fighter may be an exceptional naval crew member.

Traits are shared tags (e.g., "Hijos del Mar", "Artilleros de la Negra") that
trigger synergy bonuses when multiple characters — allies or enemies — share them.
This system creates tactical depth in both collection (building synergistic teams)
and combat (prioritizing enemy targets that enable enemy synergies).

This is the most depended-upon system in the game: 12 of 22 systems read from
this data model. Design decisions here constrain combat, progression, gacha,
UI, and persistence systems.

## Player Fantasy

The Unit Data Model serves three emotional goals that cycle throughout the
player's experience — not all at once, but taking turns as the dominant feeling
depending on the moment:

**Pride in curation** (Pillar 1 — Profundidad Estratégica Dual): When building
teams, the player feels strategic ownership over their roster. "I figured out
the best land team AND a devastating naval crew from the same collection." The
dual ability system makes roster-building a puzzle with multiple valid solutions.

**Excitement of potential** (Pillar 3 — Recompensa a la Paciencia): When pulling
a new unit — even a low-rarity one — the player wonders "Where could this one
fit?" Not every unit will be useful at all times, but the trait and dual-ability
systems should create enough situational value that the player wants to *keep*
units rather than dismiss them. The moment of rediscovering a forgotten unit
as the missing piece for a new synergy should feel like a genuine reward.

**Attachment to characters** (Pillar 2 — Personajes con Alma): Story-relevant
characters are the primary carriers of emotional attachment — the player
remembers them by name and personality, not by stat block. Gacha-only units
may not all generate deep attachment, but investing in any unit through
progression (leveling, awakening, equipment) builds a sense of ownership that
makes the player reluctant to bench them.

The unit system fails if every low-rarity unit feels like guaranteed fodder,
if the player has zero reason to try both combat modes, or if story characters
are mechanically identical to generic gacha fills.

## Detailed Design

### Core Rules

#### 1. Data Architecture

The unit system uses an inheritance-based data model:

```
CharacterData (shared base)
├── UnitData (player-owned units)
└── EnemyData (designed in Enemy System GDD)
```

**CharacterData** (base for all characters):

| Field | Type | Description |
|-------|------|-------------|
| `Id` | string | Unique template identifier (e.g., `"elena_storm"`) |
| `DisplayName` | string | Localized display name |
| `Description` | string | Short character bio |
| `BaseStats` | StatBlock | Base values for all 7 core stats (before level scaling) |
| `SecondaryStats` | SecondaryStatBlock | Base values for secondary stats (CRI, LCK) |
| `LandAbilities` | List\<AbilityEntry\> | Abilities for land combat (see AbilityEntry below) |
| `SeaAbilities` | List\<AbilityEntry\> | Abilities for naval combat (see AbilityEntry below) |
| `Element` | enum | Defensive element: Pólvora, Tormenta, Maldición, Bestia, Acero, Luz, Sombra, or Neutral. Determines elemental weakness/resistance (see Damage & Stats Engine §3). |
| `Traits` | List\<TraitId\> | 1-3 trait tags that enable synergies |
| `VisualData` | VisualRef | Sprite sheet, portrait, animation references |

**StatBlock** (7 core stats):

| Stat | Abbreviation | Description | Role |
|------|-------------|-------------|------|
| Health Points | HP | How much damage the character can take | Survivability |
| Magic Points | MP | Resource spent to use abilities | Ability resource |
| Attack | ATK | Physical damage output | Physical offense |
| Defense | DEF | Physical damage reduction | Physical defense |
| Mística | MST | Magical damage output | Magical offense |
| Spirit | SPR | Magical damage reduction | Magical defense |
| Speed | SPD | Turn order position on the initiative bar | Turn order |

**SecondaryStatBlock** (derived/percentage stats):

| Stat | Abbreviation | Description | Typical Range |
|------|-------------|-------------|---------------|
| Critical Rate | CRI | % chance of dealing a critical hit | 5-30% |
| Luck | LCK | Affects drop rates, may influence CRI and evasion | 1-100 scale |

**AbilityEntry** (reference to an ability definition):

| Field | Type | Description |
|-------|------|-------------|
| `AbilityId` | string | Reference to the ability definition (e.g., `"fireball"`) |
| `UnlockLevel` | int | Level at which this ability becomes available (1 = available immediately) |
| `Source` | enum | `Learned` (base pool), `Equipment` (granted by gear), `Awakening` (unlocked via awakening) |
| `CanLimitBreak` | bool | Whether this ability can trigger a Limit Break (extra turn) when its condition is met |
| `LBCondition` | enum? | Condition type for LB activation: `OnKill`, `OnCrit`, `OnElementAdvantage`, `OnStatusTarget`, `OnLowHP`, `OnAllyDown`. Null if `CanLimitBreak` is false |
| `LBConditionParam` | float? | Optional parameter for conditions with numeric thresholds (e.g., HP < 0.30 for `OnLowHP`). Null if not applicable |
| `LBConditionTarget` | string? | Optional parameter for conditions needing a non-numeric reference (e.g., "Quemadura" for `OnStatusTarget`). Null if not applicable |

Ability details (MP cost, damage formula, targeting, effects) live in the ability
definition referenced by `AbilityId`, not in this data model. Those are designed
in the Damage & Stats Engine GDD.

Secondary stats differ from core stats: they are percentages or multipliers, not
linear values. Their scaling with level is slower and may be influenced by
equipment and traits more than leveling.

**UnitData** (extends CharacterData, player-specific):

| Field | Type | Description |
|-------|------|-------------|
| `Rarity` | enum (3★, 4★, 5★) | Base rarity tier — affects stat growth curves and ability pool size |
| `MaxLevel` | int | Level cap (may increase with awakening) |
| `StatGrowth` | StatBlock | Per-level stat growth rates for core stats |
| `AwakeningData` | AwakeningInfo | Awakening tiers, material requirements, stat/ability unlocks |
| `EquipmentSlots` | 3 slots | Weapon, Armor, Accessory — defined by equipment type compatibility |
| `GachaPool` | enum | Which gacha banner(s) this unit appears in |
| `DuplicateBonus` | DuplicateInfo | What bonuses duplicates provide (stat boost, ability unlock, etc.) |
| `NavalRoleAffinity` | List\<NavalRole\> | Which ship roles this unit is suited for (e.g., Captain, Gunner, Navigator) |

#### 2. Stat System

- All 7 core stats start at a **base value** defined per unit template
- Stats grow per level according to `StatGrowth` rates
- Final core stat at level L: see **Formulas section** for the piecewise growth formula with late-game acceleration
- Secondary stats (CRI, LCK) have their own base values and grow at a slower rate, primarily through equipment and traits
- Stats are further modified by: equipment bonuses, awakening bonuses, trait synergies (in combat), buffs/debuffs (in combat)
- Stat modifiers stack additively within the same category, multiplicatively between categories (details in Damage & Stats Engine GDD)
- MP regeneration rules (per-turn regen, abilities that restore MP) are defined in the Damage & Stats Engine GDD

#### 3. Ability System

- Each unit has two independent ability pools: **Land** and **Sea**
- Abilities are unlocked at specific levels (e.g., `Fireball` at Lv 5, `Flame Wave` at Lv 25)
- Abilities cost **MP** to use. Basic Attack (land) and Basic Action (sea) are free (0 MP)
- In combat, ALL unlocked abilities are available each turn — no pre-selection or active slot limit
- The player chooses one ability per turn, constrained only by available MP
- Equipment can grant additional abilities to either pool
- Awakening can unlock bonus abilities not in the base pool
- A unit mediocre in land abilities can have exceptional sea abilities — ability pools are balanced independently
- Ability details (damage formulas, targeting, MP costs, effects) are defined in the Damage & Stats Engine and individual ability data, not in this model

#### 4. Trait System

- Each unit has **1 to 3 traits** (tags like `"Hijos del Mar"`, `"Artilleros"`, `"Undead"`)
- Traits live on the **CharacterData base** — both player units and enemies have traits
- Traits activate synergy bonuses when multiple characters in the same combat share them
- Synergy rules are defined in the Traits/Sinergias GDD — this model only stores which traits a unit has
- Enemy trait example: a Skeleton Captain with trait `"Undead Commander"` grants +ATK to other Undead enemies. Killing the Captain removes the synergy
- Trait count does NOT correlate with rarity — a 3★ unit can have 3 traits, a 5★ can have 1

#### 5. Rarity System

Three tiers:

| Rarity | Stars | Gacha Rate | Stat Budget | Ability Count (typical) |
|--------|-------|------------|-------------|------------------------|
| Common | 3★ | Highest | Lower base, moderate growth | 3-4 land, 3-4 sea |
| Rare | 4★ | Medium | Medium base, good growth | 4-5 land, 4-5 sea |
| Epic | 5★ | Lowest | High base, excellent growth | 5-7 land, 5-7 sea |

- Rarity affects **stat budget** and **ability pool size**, NOT trait count
- Per Pillar 3: lower-rarity units must have mechanical niches where they excel (specific traits, strong sea abilities, unique synergies)
- A 3★ unit is never strictly worse than a 5★ in every context — the dual combat + trait system ensures situational value
- **Pillar 3 cross-system dependency**: The ~3x stat gap between 3★ and 5★ at max level means 3★ viability depends on trait synergy magnitudes designed in the Traits/Sinergias GDD. If synergies provide less than ~20% effective stat bonuses, the stat gap makes 3★ units non-viable in practice. The Traits GDD MUST address this constraint

#### 6. Equipment System

- 3 slots per unit: **Weapon**, **Armor**, **Accessory**
- Equipment provides: stat bonuses (flat or percentage), and optionally bonus abilities
- Equipment type compatibility: units may have restrictions on what weapon types they can equip (e.g., swords, guns, staves)
- Equipment is a separate data model (designed when needed) — this GDD defines only that units have 3 typed slots

#### 7. Progression

- **Leveling**: Units gain XP from combat and XP materials. Each level increases stats per `StatGrowth`
- **Awakening**: At max level, units can be awakened using specific materials + currency. Awakening: raises max level cap, may unlock new abilities, provides stat bonuses
- **Duplicates**: Pulling a duplicate of an owned unit provides a bonus (e.g., stat boost, ability unlock, or awakening material). Exact mechanics in Progresión de Unidades GDD

#### 8. Naval Role Affinity

- Each unit has a `NavalRoleAffinity` indicating which ship role(s) they can fill (e.g., Captain, Gunner, Navigator, Medic)
- Most units are compatible with **exactly 1 role** (intentionally niche — incentivizes collecting diverse units to fill all ship slots). Rare exceptions may have 2 roles
- Ship role slots and their effects are defined in the Ship Data Model and Combate Naval GDDs
- This model only stores the affinity — the naval systems consume it

### States and Transitions

A unit passes through distinct lifecycle states:

| State | Description | Transitions To |
|-------|-------------|----------------|
| **Template** | Data-only definition (not owned by player). Lives in the game database. | Owned |
| **Owned** | Player has obtained this unit (via gacha, reward, story). Exists in player roster. | In Team, In Crew, Leveling, Awakening |
| **In Team** | Assigned to a land combat team slot (positions 1-5, or slot 6). A unit can be in multiple teams simultaneously. | In Combat (Land) |
| **In Crew** | Assigned to a ship role slot in a naval crew. A unit can be in multiple crews simultaneously. | In Combat (Sea) |
| **In Combat (Land)** | Active in a land combat encounter. Has runtime combat state (current HP/MP, buffs, debuffs). | Owned (combat ends) |
| **In Combat (Sea)** | Active in a naval combat encounter. Has runtime combat state. | Owned (combat ends) |
| **Leveling** | Consuming XP materials to gain levels. | Owned |
| **Awakening** | Consuming materials to awaken (raise level cap, unlock abilities). | Owned |
| **Locked** | Player has marked this unit as protected (cannot be used as material or sold). | Owned |

**Key rules:**
- A unit **can be in multiple teams and multiple crews** simultaneously — no exclusivity restriction
- Combat state (current HP, MP, buffs) is **runtime only** — it does not persist after combat ends
- Template → Owned is irreversible (units cannot be "un-obtained")
- Owned units can be **sold/consumed** (removed from roster permanently)

**Guest/Flex Slot (Friend Unit):**
- Both land teams and naval crews have a **guest slot** that can be filled by a **borrowed friend unit** OR by the **player's own unit**
- In land: slot 6 of the 5+1 formation
- In naval: one designated role slot in the crew (specifics in Combate Naval GDD)
- Borrowed friend units receive a **bonus** (stat boost, extra synergy, or similar incentive) to encourage use of the friend system
- If the player has no friends or their friends' units are unsuitable, they can use their own unit in the guest slot without the bonus
- The exact bonuses are defined in the Combate Terrestre and Combate Naval GDDs

### Interactions with Other Systems

The Unit Data Model is the most connected system in the game. It provides data
to 12 downstream systems and has no upstream dependencies.

| System | Direction | Data Interface |
|--------|-----------|----------------|
| **Initiative Bar** | UDM → Initiative | Reads `SPD` stat to calculate turn order position |
| **Damage & Stats Engine** | UDM → Engine | Reads all core stats (HP, MP, ATK, DEF, MST, SPR) + secondary stats (CRI, LCK) for damage/healing formulas |
| **Enemy System** | UDM ← shared base | Enemies extend the same `CharacterData` base. Enemy GDD defines `EnemyData` extensions |
| **Traits/Sinergias** | UDM → Traits | Reads `Traits` list to evaluate synergy activation. Writes synergy bonuses back as combat modifiers |
| **Combate Terrestre** | UDM → Combat | Reads land abilities, stats, and team composition. Manages runtime combat state (current HP/MP, buffs) |
| **Combate Naval** | UDM → Naval | Reads sea abilities, traits, `NavalRoleAffinity`, and stats. Applies naval synergy bonuses |
| **Team Composition** | UDM → Teams | Reads full unit data for team/crew building UI. Reads `NavalRoleAffinity` for crew slot validation |
| **Sistema Gacha** | UDM → Gacha | Reads `Rarity` and `GachaPool` for summon pool construction and rate calculation |
| **Progresión de Unidades** | UDM ↔ Progression | Reads `MaxLevel`, `StatGrowth`, `AwakeningData`. Writes back level, awakening tier, unlocked abilities |
| **Rewards System** | UDM → Rewards | Unit templates can be rewards (unit drops). Reads unit data for reward display |
| **Save/Load System** | UDM → Persistence | Reads unit template IDs + player state (level, awakening, equipment) for serialization |
| **Unit Roster/Inventory** | UDM → Roster | Reads all owned unit data for display, filtering, sorting |

**Interface ownership**: The Unit Data Model owns the data schema. Consuming systems
own their interpretation of that data (e.g., Damage Engine owns how ATK translates
to damage; Traits system owns how trait tags translate to bonuses).

## Formulas

### Master Stat Growth Formula

```
Threshold = floor(0.80 × MaxLevel)

If L ≤ Threshold:
  Stat(L) = Base + floor(Growth × (L - 1))

If L > Threshold:
  Stat(L) = Base + floor(Growth × (Threshold - 1)) + floor(Growth × 1.20 × (L - Threshold))
```

The formula uses **band summation**: levels 1 through the threshold grow at the
base rate, and only levels above the threshold receive the 1.2x accelerator.
This creates a "last levels feel powerful" effect without retroactively inflating
all previous growth.

**Final stat with awakening:**
```
FinalStat = Stat(CurrentLevel) + sum(AwakeningBonus[tier])
```

### Level Caps

| Rarity | Base Cap | 1st Awakening | 2nd Awakening | 3rd Awakening |
|--------|----------|---------------|---------------|---------------|
| 3★ | 40 | 50 | 60 | — |
| 4★ | 50 | 60 | 70 | — |
| 5★ | 60 | 70 | 80 | 90 |

### Base Stats and Growth Rates Per Rarity

| Stat | 3★ Base | 3★ Growth | 4★ Base | 4★ Growth | 5★ Base | 5★ Growth |
|------|---------|----------|---------|----------|---------|----------|
| HP | 600 | 28 | 900 | 40 | 1,400 | 60 |
| MP | 100 | 4 | 140 | 6 | 200 | 9 |
| ATK | 60 | 3.5 | 90 | 5 | 140 | 8 |
| DEF | 50 | 3 | 75 | 4.5 | 120 | 7 |
| MST | 60 | 3.5 | 90 | 5 | 140 | 8 |
| SPR | 45 | 2.5 | 70 | 4 | 110 | 6.5 |
| SPD | 55 | 1.0 | 65 | 1.2 | 80 | 1.5 |

These are **reference ranges** for typical units. Individual units may deviate
(e.g., a glass cannon 3★ with ATK 75 but HP 550). Per-unit stat templates are
authored in the unit database, using these ranges as guidelines.

**SPD note:** Speed uses narrow growth because all units act once per turn (SPD
determines turn ORDER, not turn FREQUENCY). A special "limit break" mechanic
(designed in the Initiative Bar GDD) allows fast units to act twice under
specific conditions — SPD matters most when that mechanic triggers.

### Secondary Stats

| Stat | 3★ Base | 3★ Growth | 4★ Base | 4★ Growth | 5★ Base | 5★ Growth | Hard Cap |
|------|---------|----------|---------|----------|---------|----------|----------|
| CRI | 5% | 0.12%/Lv | 7% | 0.14%/Lv | 10% | 0.17%/Lv | No cap |
| LCK | 10 | 0.5/Lv | 20 | 0.6/Lv | 30 | 0.7/Lv | 100 |

**CRI** is a percentage stat with **no hard cap**. Natural max CRI: 3★ ~10%, 4★ ~14%, 5★ ~23%. Equipment, traits, and buffs can push CRI above 100% — at that point, the unit always crits and excess CRI converts to bonus critical damage with diminishing returns (see Damage & Stats Engine §5).

**LCK** influences:
- Drop rate multiplier: `1.0 + (LCK / 100)` (LCK 0 = 1.0x baseline, LCK 50 = 1.5x, LCK 100 = 2.0x)
- Bonus CRI from LCK: `floor(LCK / 20)%` (max +5% at LCK 100)
- Evasion from LCK: `floor(LCK / 25)%` (max +4% at LCK 100)

### Awakening Flat Bonuses

| Awakening | 3★ | 4★ | 5★ |
|-----------|-----|-----|-----|
| 1st | +50 HP, +5 ATK/DEF/MST/SPR, +2 SPD | +80 HP, +8 all, +3 SPD | +120 HP, +12 all, +4 SPD |
| 2nd | +70 HP, +7 all, +2 SPD | +110 HP, +11 all, +3 SPD | +160 HP, +16 all, +5 SPD |
| 3rd | — | — | +200 HP, +20 all, +6 SPD |

MP does not receive awakening bonuses (it scales sufficiently through levels).

### Worked Example: 3★ vs 5★ at Max Level

**3★ unit (Lv 60, fully awakened):**
- Acceleration kicks in at Lv 49 (80% of 60)
- HP = 600 + 28×47 + floor(28×1.2)×12 + 120 = 600 + 1,316 + 396 + 120 = **2,432**
- ATK = 60 + 3.5×47 + floor(3.5×1.2)×12 + 12 = 60 + 164 + 48 + 12 = **284**

**5★ unit (Lv 90, fully awakened):**
- Acceleration kicks in at Lv 73 (80% of 90)
- HP = 1,400 + 60×71 + 72×18 + 480 = 1,400 + 4,260 + 1,296 + 480 = **7,436**
- ATK = 140 + 8×71 + 9×18 + 48 = 140 + 568 + 162 + 48 = **918**

**Ratio at max: HP ~3.1x, ATK ~3.2x.** Significant gap, but trait synergies
(+15-25% effective stats) and situational sea abilities keep 3★ units viable
in specific contexts per Pillar 3.

## Edge Cases

### Stat Boundaries

| Situation | Resolution |
|-----------|------------|
| HP reaches 0 | Unit is **KO'd** (removed from combat, cannot act). Not permanently dead. Revivable by abilities if applicable. |
| MP reaches 0 | Unit can only use Basic Attack / Basic Action (0 MP). Cannot use any MP-costing ability. No other penalty. |
| A stat is reduced below 0 by debuffs | **Clamp to 0.** ATK/MST at 0 = 0 damage. DEF/SPR at 0 = full damage taken. SPD at 0 = acts last but still gets a turn. |
| Stat overflow (buffs push beyond max) | **No hard cap on buffed stats.** Stacking buffs is a valid strategy. CRI above 100% converts to bonus crit damage (see Damage & Stats Engine §5). |
| HP exceeds max HP via healing | **Clamp to MaxHP.** No overheal. (Exception: abilities that grant temporary shields — defined per ability.) |
| MP exceeds max MP via recovery | **Clamp to MaxMP.** No over-recovery. |

### Progression Edge Cases

| Situation | Resolution |
|-----------|------------|
| XP awarded at max level | XP is **lost** (not banked). The UI warns only in **XP-focused stages** (not in all stages, to avoid repetitive alerts). |
| Awakening materials consumed but player cancels | **Transaction is atomic**: materials consumed only on confirmation. No partial state. |
| Duplicate pulled of a max-awakened unit | Duplicate still provides a bonus (stat shard, currency, etc.). Never wasted. Details in Progresión GDD. |
| Player tries to sell/consume a Locked unit | **Blocked by UI.** Must unlock first. Two-step protection. |
| Player sells their last copy of a story-required unit | **Allowed**, but unit remains in the "collection log" for story continuity. No longer available for combat. |

### Team/Crew Edge Cases

| Situation | Resolution |
|-----------|------------|
| Same unit in land team AND naval crew | **Allowed.** Units are not exclusive to one mode. (Cannot enter land AND naval combat simultaneously.) |
| Same unit in slots 1-5 and slot 6 (own unit) | **Not allowed.** A unit cannot appear twice in the same team using your own roster. |
| Same unit in slots 1-5 AND as friend unit in slot 6 | **Allowed.** Borrowing a friend's copy of a unit you already own IS permitted. This is an intentional incentive for the friend system — having friends with good units of the same type you use is rewarded. |
| Unit is leveled/awakened while assigned to a team | **Changes apply immediately** to all teams/crews containing that unit. No stale data. |
| Friend unit changes their unit mid-combat | **Snapshot on combat start.** Borrowed unit uses stats from when combat began. |
| No friends available for slot 6 | **Slot 6 defaults to empty** or player selects their own unit. The game never requires a friend unit. |

### Trait/Synergy Edge Cases

| Situation | Resolution |
|-----------|------------|
| Enemy encounter captain is killed | **All enemy synergies deactivate immediately.** Only the captain activates enemy synergies — killing non-captain enemies does not affect synergy state. See Traits/Sinergias GDD §4. |
| Unit with 0 matching traits joins a synergy-heavy team | **No penalty.** Unit doesn't contribute to or benefit from synergies but functions normally. |
| Two different synergies buff the same stat | **Stack additively.** Synergy A +10 ATK + Synergy B +15 ATK = +25 ATK total. |
| Trait shared by allies AND enemies in same combat | **Synergies are side-specific.** Allied traits count allies only; enemy traits count enemies only. No cross-side synergies. |

## Dependencies

### Upstream Dependencies

**None.** The Unit Data Model is a foundation system with no upstream dependencies.

### Downstream Dependencies (systems that depend on this)

| System | Dependency Type | Interface |
|--------|----------------|-----------|
| Initiative Bar | Hard | Reads SPD to calculate turn order |
| Damage & Stats Engine | Hard | Reads all core + secondary stats for combat formulas |
| Enemy System | Hard | Extends shared CharacterData base |
| Traits/Sinergias | Hard | Reads trait list to evaluate synergies |
| Combate Terrestre | Hard | Reads land abilities, stats, team slots |
| Combate Naval | Hard | Reads sea abilities, traits, NavalRoleAffinity |
| Team Composition | Hard | Reads full unit data for team/crew building |
| Sistema Gacha | Hard | Reads rarity, gacha pool membership |
| Progresión de Unidades | Hard | Reads/writes level, awakening, abilities |
| Rewards System | Soft | Unit templates can be rewards (not required for rewards to work with other types) |
| Save/Load System | Hard | Serializes unit template ID + player state |
| Unit Roster/Inventory | Hard | Reads all owned unit data for display |

**Hard** = system cannot function without Unit Data Model.
**Soft** = system is enhanced by Unit Data Model but works without it.

### Bidirectional Note

When downstream system GDDs are written, they MUST reference this document for
data interfaces. If a downstream system needs a field not defined here, the
field must be added to this GDD first — downstream systems do not extend the
data model unilaterally.

## Tuning Knobs

These are designer-adjustable values that can be changed without code modifications.

### Per-Unit Template Knobs

| Knob | Range | What It Affects | If Too High | If Too Low |
|------|-------|----------------|-------------|------------|
| Base Stats (per stat) | See Formulas section | Unit power at Lv 1 | Overpowered early; trivializes content | Feels useless at low levels |
| Stat Growth (per stat) | See Formulas section | Power scaling with levels | Late-game spike too extreme | Leveling feels unrewarding |
| Ability unlock levels | 1 to MaxLevel | When abilities become available | Available too early; no progression pull | Locked too long; unit feels incomplete |
| Ability MP costs | 0-999 | How often abilities can be used | Too expensive; never used | Free to spam; no resource management |
| Number of traits | 1-3 | Synergy potential | Too many synergies active; balance nightmare | Too few connections; synergy system feels dead |
| Naval role affinities | 1 (typical), rarely 2 | Crew building decisions | Unit fits too many roles; less reason to collect | Unit too niche (this is the intended default — most units fill exactly 1 role, incentivizing collection to cover all ship slots) |

### Global System Knobs

| Knob | Current Value | Range | What It Affects |
|------|--------------|-------|----------------|
| GrowthModifier threshold | 80% of MaxLevel | 70-90% | When stat acceleration kicks in |
| GrowthModifier multiplier | 1.20 | 1.10-1.40 | How much stats accelerate in the final band |
| CRI overflow divisor | 50 | 30-100 | How much excess CRI (above 100%) converts to bonus crit damage (see Damage & Stats Engine §5) |
| LCK drop rate divisor | 100 | 50-200 | How much LCK affects drop rates (formula: 1 + LCK/divisor) |
| Level caps per rarity | 40/50/60 | ±10 | Total power ceiling per rarity tier |
| Awakening tier count | 2/2/3 (by rarity) | 1-4 | How much investment a unit can receive |

### Knob Interactions (Danger Zones)

| Knob A | Knob B | Interaction |
|--------|--------|-------------|
| Base Stats | Stat Growth | Both affect final stats. Increasing both compounds — tune one at a time. |
| Ability MP costs | Max MP (from base + growth) | If MP grows too fast relative to costs, resource management disappears. |
| CRI overflow divisor | LCK-to-CRI conversion | LCK contributes to CRI. If CRI base is generous and LCK conversion is high, 100% crit is reached too easily, trivializing the overflow mechanic. |
| Number of traits | Trait synergy bonuses (Traits GDD) | More traits × strong synergies = exponential power. |
| GrowthModifier multiplier | Awakening bonuses | Both reward high-level investment. Stacking makes last levels disproportionately powerful. |

## Visual/Audio Requirements

### Visual

- Each unit needs: pixel art chibi **sprite sheet** (idle, attack, ability cast, hit, KO, victory) + **portrait** (anime-style for menus, gacha reveal, story scenes)
- Rarity differentiation: 3★ units have simpler sprite animations (3-4 frames per action); 5★ units have more elaborate animations (5-8 frames) and special effects on abilities
- Trait visual indicator: a small icon/badge on the unit portrait showing active traits (visible in team composition screen)
- Awakening visual change: awakened units should have a subtle visual upgrade (glow, color shift, or accessory addition) to reward investment
- Naval role indicator: icon overlay showing the unit's naval role affinity (Captain wheel, Gunner cannon, etc.)

### Audio

- Each unit needs: attack SFX, ability cast SFX (1-2 per unit), hit SFX, KO SFX
- Rarity differentiation: 5★ units may have a unique voice line on ability cast (optional for demo)
- Gacha reveal: distinct sound per rarity tier (escalating drama: 3★ basic chime, 4★ fanfare, 5★ full orchestral sting)

## UI Requirements

- **Unit Card**: Displays portrait, name, rarity stars, level, and trait icons. Must fit in roster grid (thumbnail) and expand to full detail view
- **Stat Screen**: Shows all 7 core stats + 2 secondary stats with current values, base values, and equipment/awakening bonuses broken out. Must be readable on mobile.
- **Ability List**: Separate tabs or toggle for Land/Sea abilities. Each ability shows name, MP cost, unlock level, and brief description. Locked abilities show level requirement.
- **Team Builder**: 5 slots + 1 guest slot. Drag-and-drop or tap-to-assign. Filter/sort by stat, rarity, trait, naval role. Friend unit slot visually distinct.
- **Crew Builder**: Ship with role slots. Each slot shows which roles it accepts. Units filtered by NavalRoleAffinity. Friend slot in crew.
- **Trait Display**: In team/crew builder, show active synergies and their bonuses in real-time as units are added/removed.
- **Progression Screen**: Level bar, XP to next level, awakening status, material requirements, equipment slots with equipped items.

## Acceptance Criteria

### Data Model Validation

- [ ] A unit template can be created with all required fields (stats, abilities, traits, rarity, naval role) and loaded at runtime
- [ ] CharacterData base is shared: creating a UnitData and an EnemyData from the same base class compiles and works
- [ ] All 7 core stats + 2 secondary stats are stored, readable, and modifiable
- [ ] Land and sea ability pools are independent — modifying one does not affect the other
- [ ] A unit can have 1-3 traits and they are queryable by the traits system

### Stat System Verification

- [ ] Stat growth formula produces correct values at levels 1, 25, 50, and max for all 3 rarity tiers (compare against Formulas section worked examples)
- [ ] GrowthModifier acceleration activates at exactly 80% of MaxLevel and produces 1.2x growth
- [ ] Awakening flat bonuses apply correctly on top of level-scaled stats
- [ ] Secondary stats (CRI, LCK) scale independently from core stats
- [ ] CRI has no hard cap — values above 100% guarantee crits and excess converts to bonus crit damage per Damage & Stats Engine §5
- [ ] Stats clamp to 0 when debuffed below zero (no negative stats)

### Ability System Verification

- [ ] Abilities unlock at the correct levels per the unit template
- [ ] All unlocked abilities (learned + equipment-granted) are available in combat
- [ ] MP cost is enforced: ability cannot be used if current MP < cost
- [ ] Basic Attack (land) and Basic Action (sea) are always available at 0 MP

### Equipment & Progression

- [ ] 3 equipment slots (Weapon, Armor, Accessory) are functional per unit
- [ ] Equipment stat bonuses apply to the unit's final stats
- [ ] Equipment abilities appear in the unit's available ability list
- [ ] Leveling increases stats per StatGrowth rates
- [ ] Awakening raises MaxLevel and applies flat stat bonuses
- [ ] Level cap respects rarity tier and awakening tier

### Team/Crew Assignment

- [ ] A unit can be assigned to multiple land teams and multiple naval crews simultaneously
- [ ] A unit cannot appear twice in the same team using own units
- [ ] A friend unit that duplicates an owned unit IS allowed in slot 6
- [ ] Friend unit snapshot is taken at combat start (no mid-combat changes)
- [ ] Guest slot works in both land teams (slot 6) and naval crews (designated slot)

### Performance Budget

- [ ] Loading a unit template from data: < 1ms
- [ ] Calculating final stats (base + growth + awakening + equipment + buffs): < 0.5ms per unit
- [ ] Loading a full team of 6 units with all data: < 10ms
- [ ] Memory per unit instance (runtime): < 2KB

## Open Questions

| # | Question | Owner | Target Resolution |
|---|----------|-------|-------------------|
| 1 | ~~How exactly does the "limit break" mechanic for SPD work?~~ | Initiative Bar GDD | **Resolved**: Limit Breaks are ability-triggered (never passive). Some abilities scale with SPD (e.g., "extra turn if user SPD > target SPD"). Max 1 extra turn per unit per round. See `design/gdd/initiative-bar.md`. |
| 2 | ~~What specific naval roles exist and how many slots does a ship have?~~ | Ship Data Model + Combate Naval GDDs | **Resolved**: 7 naval roles (Capitán, Intendente, Artillero, Navegante, Carpintero, Cirujano, Contramaestre). Variable slots per ship (5-7 demo, 10+ full game). See `design/gdd/ship-data-model.md`. |
| 3 | How does equipment type compatibility work? (Weapon categories per unit) | Equipment System (not yet in systems index) | When equipment design begins |
| 4 | What are the specific awakening materials and their acquisition sources? | Progresión de Unidades + Currency System GDDs | When those systems are designed |
| 5 | How does MP regeneration work in combat? (Per-turn regen, abilities, items) | Damage & Stats Engine GDD | When combat formulas are designed |
| 6 | What happens with duplicate bonuses for max-awakened units? | Progresión de Unidades GDD | When progression is designed |
| 7 | ~~Should the Equipment System be added to the systems index?~~ **Resolved**: Added as system #22 in systems index (Vertical Slice, depends on Unit Data Model) | — | Resolved 2026-03-25 |
