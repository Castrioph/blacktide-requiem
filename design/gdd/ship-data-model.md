# Ship Data Model

> **Status**: Approved
> **Author**: User + Claude Code Game Studios agents
> **Last Updated**: 2026-03-26
> **Implements Pillar**: Pillar 1 (Profundidad Estratégica Dual)

## Overview

The Ship Data Model defines the data structure for ships — the "mega-units" of
naval combat. A ship is a single combat entity with its own stats, abilities,
and role slots. The crew (player-owned units assigned to role slots) does not
act independently — instead, crew members modify the ship's stats and contribute
abilities that become ship abilities. The ship takes turns on the initiative bar
as one entity.

Ships have 6 core stats that mirror the unit stat split (physical/special),
variable role slots depending on the ship (5-7 in the demo, up to 10+ in the
full game), and their own ability pool enhanced by crew contributions. The
slot configuration IS the ship's identity — a sloop with 5 slots focused on
navigation and speed plays completely differently from a galleon with 7+ slots
spread across combat roles. Not all roles appear on every ship, forcing
strategic crew decisions.

In the demo, ships are acquired through story progression (first ship) and
construction with materials earned in naval stages (subsequent ships). Ship
gacha is a future feature for the full game when the ship roster is large
enough (20+). The demo includes 2-3 ships with distinct identities rather
than rarity tiers.

This system is a foundation model depended on by Combate Naval, Team
Composition, Sistema Gacha (future), Save/Load, and Unit Roster/Inventory.

## Player Fantasy

**"My ship, my rules."** The ship is the player's mobile fortress and the
physical expression of their strategic vision. Where land combat is about
individual unit strength, naval combat is about the whole being greater than
the sum of its parts.

The player should feel like a **captain assembling a crew** — not just picking
the strongest units, but finding the right person for each role. Putting a
fast unit as Navigator makes the ship dodge more. Putting a unit with the
"Artilleros" trait as Gunner triggers synergies that boost cannon damage.
The satisfaction comes from watching a well-assembled crew turn an ordinary
ship into something extraordinary.

Ships should feel **distinct in personality**: a small, fast sloop that darts
between enemies versus a massive galleon that absorbs punishment and fires
devastating broadsides. Choosing which ship to bring to a naval stage is a
meaningful decision, not just "use the highest-rarity one."

The system fails if all ships feel interchangeable, if crew composition
doesn't matter (just use strongest units regardless of role), or if the
ship feels like a UI wrapper around unit combat.

## Detailed Design

### Core Rules

#### 1. Data Architecture

A ship is defined by a `ShipData` asset containing:

| Field | Type | Description |
|-------|------|-------------|
| ShipId | string | Unique identifier (e.g., `ship_sloop_01`) |
| DisplayName | string | Localized name |
| Description | string | Flavor text |
| BaseStats | ShipStatBlock | Base stats at upgrade level 0 |
| Element | enum | Defensive element: Pólvora, Tormenta, Maldición, Bestia, Acero, Luz, Sombra, or Neutral. Determines elemental weakness/resistance (see Damage & Stats Engine §3). |
| RoleSlots | RoleSlot[] | Ordered list of role slots (variable per ship) |
| BaseAbilities | AbilityId[] | Innate ship abilities (always available) |
| UpgradeState | ShipUpgradeState | Current upgrade levels for components |
| AcquisitionMethod | enum | Story / Crafted / Gacha (future) |

**ShipStatBlock** — 6 naval stats:

| Stat | Abbreviation | Description |
|------|-------------|-------------|
| Hull HP | HHP | Ship health pool — ship sinks at 0 |
| Firepower | FPW | Base cannon/weapon damage |
| Hull Defense | HDF | Physical damage reduction |
| Mística | MST | Magical damage output (mirrors unit MST) |
| Magic Points | MP | Resource for ship abilities (analogous to unit MP) |
| Resilience | RSL | Special/magical damage reduction |
| Sail Speed | SPD | Turn order position on the naval initiative bar |

**RoleSlot** structure:

| Field | Type | Description |
|-------|------|-------------|
| SlotIndex | int | Position in the crew layout |
| Role | NavalRole | Which of the 7 roles this slot requires |
| AssignedUnit | UnitData? | Currently assigned unit (null if empty) |
| IsGuestSlot | bool | Whether this slot accepts a friend/guest unit |

Each ship has a unique configuration of role slots. The slot distribution IS the
ship's identity — no two ships should have the same role layout. Not all 7 roles
need to appear on every ship. One slot per ship may be designated as the guest slot
(friend unit with bonus, or own unit without bonus).

#### 2. Naval Roles (7)

Each role defines what stat bonuses a crew member contributes and what function
they serve aboard the ship:

| Role | Primary Stat Bonus | Function |
|------|-------------------|----------|
| **Capitán** | FPW + SPD | Command authority — boosts attack and initiative; may unlock leadership abilities |
| **Intendente** | MST + RSL | Resource management — boosts magical offense and magical resilience |
| **Artillero** | FPW + HDF | Weapons specialist — boosts firepower and physical durability |
| **Navegante** | SPD + RSL | Navigation and evasion — boosts ship speed and special defense |
| **Carpintero** | HHP + HDF | Hull repair and reinforcement — boosts HP and physical defense |
| **Cirujano** | HHP + RSL | Crew healing and morale — boosts HP and magical resilience |
| **Contramaestre** | HDF + SPD | Crew discipline and rigging — boosts defense and speed |

**Role affinity**: Each unit typically has **1 naval role** affinity (stored in
UnitData.NavalRole). Assigning a unit to a matching role slot grants full stat
bonuses. Assigning to a non-matching slot applies a penalty (see Formulas).
This encourages collecting many units to fill diverse ship configurations.

#### 3. Crew Contribution System

Crew members modify the ship through three channels:

**Stat contribution**: Each filled role slot adds bonuses to the ship's stats
based on the role (see table above) and the unit's own stats. The formula
scales with the unit's relevant stats (see Formulas section).

**Ability contribution**: Units assigned to crew slots contribute their
**sea-tagged abilities** to the ship's ability pool. These become ship abilities
usable during the ship's turn. Combined with the ship's base abilities, all
are available each turn (no pre-selection, consistent with the FFBE-style
ability model from Unit Data Model).

**Trait contribution**: Unit traits that have naval effects activate when the
unit is assigned to a crew slot. Trait synergies between crew members function
the same as in land combat (e.g., "Artilleros" trait gives +15% FPW when
multiple crew members share it).

**Empty slots**: An unfilled slot contributes nothing — no stat bonus, no
abilities, no traits. The ship functions with reduced effectiveness but is
still deployable. This is intentional to avoid soft-locking players who
don't have enough units for all slots.

#### 4. Ship Abilities

A ship's available abilities in combat are:

- **Base pool**: Innate abilities defined in ShipData.BaseAbilities (always
  available regardless of crew). Includes basic attack (free, like unit basic
  attack) and ship-specific abilities (cost MP).
- **Crew abilities**: Sea-tagged abilities from all assigned crew members.
  Added to the pool when the unit is assigned, removed when unassigned.

All abilities are available every turn (no pre-selection). Abilities cost MP
(ship's resource stat), with basic attack being free. This mirrors the unit
combat model where abilities cost MP.

#### 5. Ship Acquisition (Demo)

The demo includes 2-3 ships with distinct identities:

| Ship | Acquisition | Slot Count | Identity |
|------|------------|------------|----------|
| Ship 1 | Story reward (early game) | 5 | Balanced starter — covers core roles |
| Ship 2 | Crafted with materials | 6-7 | Specialized — combat-focused or speed-focused |
| Ship 3 | Crafted with materials | 6-7 | Specialized — complements Ship 2's weaknesses |

**Story acquisition**: Ship 1 is given during the narrative as part of the
tutorial/early game. No grind required.

**Crafting**: Ships 2-3 require materials earned from naval stages. Materials
are deterministic drops (not gacha). The crafting recipe is visible from the
start so players can plan their progression.

**No ship gacha in demo**: With only 2-3 ships, gacha would feel empty. Ship
gacha is deferred to the full game when the roster reaches 20+ ships.

**No rarity tiers for ships (demo)**: Ships are distinguished by their role
slot configuration, base stats, and base abilities — not by star rating.

#### 6. Ship Upgrades

Ships improve through **component upgrades**, not XP/levels:

| Component | Stat Affected | Levels | Materials |
|-----------|--------------|--------|-----------|
| **Hull** | HHP, HDF | 3 | Wood, Iron, special drops |
| **Cannons** | FPW, MST, MP | 3 | Iron, Gunpowder, special drops |
| **Sails** | SPD, RSL | 3 | Cloth, Rope, special drops |

Each component has 3 upgrade levels. Upgrades are **material-gated** (no XP
system for ships). Materials come from naval stages, some requiring specific
stage clears or boss drops.

Upgrades are permanent and independent — upgrading Hull doesn't affect Cannon
progress. All three components start at level 0 and can be upgraded in any
order.

### States and Transitions

A ship passes through distinct lifecycle states:

| State | Description | Transitions To |
|-------|-------------|----------------|
| **Blueprint** | Ship definition exists in game data but player hasn't acquired it. Visible in shipyard UI as "locked" with crafting recipe shown. | Owned |
| **Owned** | Player has acquired this ship (story reward or crafted). Exists in player fleet. | Crewed, Upgrading |
| **Crewed** | Has at least one unit assigned to a role slot. Can be partially crewed (not all slots filled). | Deployed, Owned (all crew removed) |
| **Deployed** | Selected as the active ship for a naval stage. Only one ship can be deployed per stage. | In Combat (Sea) |
| **In Combat (Sea)** | Active in a naval encounter. Has runtime state (current HHP, MP, buffs/debuffs). | Owned (combat ends) |
| **Upgrading** | Consuming materials to upgrade a component (Hull/Cannons/Sails). | Owned |
| **Sunk (Runtime)** | HHP reached 0 during combat. Ship is non-functional for remainder of encounter. | Owned (combat ends — ship is restored, sinking is not permanent) |

**Key rules:**
- A ship can be **crewed and upgrading** simultaneously — upgrades don't require uncrewing
- Only **one ship deployed per naval stage** (no fleet battles in demo)
- **Sinking is temporary** — combat-only state, ship is fully restored after combat ends
- Blueprint → Owned is irreversible (ships cannot be un-acquired)
- Ships **cannot be sold or destroyed** in the demo (small roster, losing a ship would be punishing)
- **Partially crewed ships are deployable** — empty slots just contribute nothing
- Combat state (current HHP, MST, buffs) is **runtime only**, does not persist after combat

**Guest slot in naval:**
- One designated role slot per ship has `IsGuestSlot = true`
- Same rules as land guest slot: friend unit gets a bonus, own unit can fill without bonus
- Which specific role slot is the guest slot varies per ship (part of the ship's identity)

### Interactions with Other Systems

The Ship Data Model connects to multiple systems, with an upstream dependency on
the Unit Data Model (needs unit data for crew contributions).

| System | Direction | Data Interface |
|--------|-----------|----------------|
| **Unit Data Model** | UDM → SDM | Reads unit stats, sea abilities, traits, and NavalRoleAffinity for crew contribution calculations |
| **Combate Naval** | SDM → Naval | Reads effective ship stats (base + crew + upgrades), ability pool, and crew traits. Manages runtime combat state (current HHP/MP, buffs) |
| **Initiative Bar** | SDM → Initiative | Reads ship's effective SPD to calculate turn order position in naval combat |
| **Damage & Stats Engine** | SDM → Engine | Reads FPW, HDF, RSL for naval damage/defense formulas |
| **Traits/Sinergias** | SDM ↔ Traits | Reads crew traits to evaluate naval synergies. Writes synergy bonuses back as ship stat modifiers |
| **Team Composition** | SDM → Teams | Reads ship data, role slots, and crew assignments for crew management UI |
| **Currency System** | Currency → SDM | Provides crafting materials for ship construction and upgrade materials for components |
| **Save/Load System** | SDM → Persistence | Reads ship ownership, upgrade levels, and crew assignments for serialization |
| **Rewards System** | SDM → Rewards | Ship blueprints and upgrade materials can be stage rewards |
| **Unit Roster/Inventory** | SDM ↔ Roster | Reads available units for crew assignment. Writes crew assignments back to roster state |
| **Progresión de Unidades** | Indirect | Unit level/stats affect crew contributions — no direct interface, flows through UDM |

**Interface ownership**: The Ship Data Model owns the ship data schema and crew slot
structure. Consuming systems own their interpretation (e.g., Combate Naval owns how
FPW translates to cannon damage; Traits system owns how naval synergies activate).

**Key difference from Unit Data Model**: The SDM has an **upstream dependency** on
the Unit Data Model (needs unit data for crew contributions), whereas the UDM has
no upstream dependencies.

## Formulas

### 1. Crew Stat Contribution

Each role slot contributes **only to its 2 designated naval stats** (see Core
Rules §2). Contribution to all other naval stats is 0.

```
ContributionBase = floor(UnitStat × CREW_SCALING_FACTOR)

If role matches UnitData.NavalRoleAffinity:
  Contribution = ContributionBase
If role does NOT match:
  Contribution = floor(ContributionBase × MISMATCH_PENALTY)
```

- `UnitStat` = the unit's mapped stat for the naval stat being calculated (see mapping table)
- `CREW_SCALING_FACTOR` = 0.15 (units contribute 15% of their stat to the ship)
- `MISMATCH_PENALTY` = 0.50 (non-matching role contributes half)

### 2. Unit Stat → Naval Stat Mapping

| Naval Stat | Unit Stat Source |
|-----------|-----------------|
| HHP (Hull HP) | HP |
| FPW (Firepower) | ATK |
| HDF (Hull Defense) | DEF |
| MST (Mística) | MST |
| RSL (Resilience) | SPR |
| SPD (Sail Speed) | SPD |

**No naval mapping for MP**: The ship has its own MP pool, independent of unit MP.

Each role contributes to 2 naval stats (see Core Rules §2). The crew member's
contribution to each of those stats uses the corresponding unit stat from this table.

### 3. Effective Ship Stat

```
EffectiveStat = BaseStat + UpgradeBonus + sum(CrewContribution[slot]) + TraitBonuses
```

- `BaseStat` = ShipData.BaseStats value for this stat
- `UpgradeBonus` = percentage of BaseStat based on component upgrade level
- `CrewContribution[slot]` = contribution from each filled role slot (formula §1)
- `TraitBonuses` = bonuses from crew trait synergies (defined in Traits/Sinergias GDD)

**Runtime resource stats**: In combat, ships start with current HHP = effective HHP
and current MP = effective MP. These are consumed during combat (HHP by damage,
MP by abilities) and are runtime-only — they reset after combat ends.

### 4. Upgrade Bonus per Component Level

| Component | Stat Affected | Level 0 | Level 1 | Level 2 | Level 3 |
|-----------|--------------|---------|---------|---------|---------|
| **Hull** | HHP, HDF | +0% | +10% base | +25% base | +45% base |
| **Cannons** | FPW, MST, MP | +0% | +10% base | +25% base | +45% base |
| **Sails** | SPD, RSL | +0% | +10% base | +25% base | +45% base |

Non-linear progression: each level is more impactful than the previous one
(+10, +15, +20 incremental). This makes later upgrades feel like meaningful
power spikes.

```
UpgradeBonus = floor(BaseStat × UpgradePercent[level])
```

### 5. Worked Example

**Ship**: Sloop (5 slots), Cannons Level 2, other upgrades Level 0

```
Ship Base FPW: 100
Cannon upgrade Level 2: +25% of 100 = +25

Slot 1 — Artillero (unit ATK=200, role MATCHES):
  ContributionBase = floor(200 × 0.15) = 30
  Role matches → Contribution to FPW = 30

Slot 2 — Capitán (unit ATK=180, role MATCHES):
  ContributionBase = floor(180 × 0.15) = 27
  Role matches → Contribution to FPW = 27

Slot 3 — Carpintero (unit ATK=120, role MATCHES):
  Carpintero boosts HHP + HDF, NOT FPW
  Contribution to FPW = 0

Slot 4 — Artillero (unit ATK=150, role DOES NOT MATCH — unit is a Navegante):
  ContributionBase = floor(150 × 0.15) = 22
  Mismatch → Contribution to FPW = floor(22 × 0.50) = 11

Slot 5 — Guest slot (empty)
  Contribution = 0

Effective FPW = 100 + 25 + 30 + 27 + 0 + 11 + 0 = 193
```

### Variable Definitions

| Variable | Type | Range | Description |
|----------|------|-------|-------------|
| CREW_SCALING_FACTOR | float | 0.10 – 0.25 | Percentage of unit stat contributed to ship |
| MISMATCH_PENALTY | float | 0.25 – 0.75 | Multiplier when unit role doesn't match slot role |
| UpgradePercent[level] | float[] | [0, 0.10, 0.25, 0.45] | Base stat percentage bonus per upgrade level |
| BaseStat | int | 50 – 500 | Ship's base stat before any modifiers |
| UnitStat | int | varies | Unit's stat value at current level + awakening |

## Edge Cases

| Edge Case | Resolution |
|-----------|------------|
| **All crew slots empty** | Ship deploys with base stats + upgrades only. No crew abilities, no trait bonuses. Allowed but UI shows warning. |
| **Same unit in multiple crews** | Allowed (consistent with Unit Data Model). Unit contributes independently to each ship. |
| **Same unit in land team AND naval crew** | Allowed. Land and naval are separate deployment contexts, never simultaneous. |
| **Unit removed from crew mid-session (outside combat)** | Crew contribution recalculated immediately. Sea abilities removed from pool. |
| **Unit assigned to mismatched role with 0 in mapped stat** | Contribution = floor(0 × 0.15 × 0.50) = 0. No negative contributions, just zero. |
| **All 3 components at max upgrade** | Ship is "fully upgraded." No further material sink for this ship. Future: prestige/retrofit system. |
| **Ship sinks in multi-wave naval stage** | Ship remains sunk for remaining waves. Stage fails or continues with reduced capacity (defined in Combate Naval GDD). |
| **Guest slot crew member — friend goes offline** | Snapshot of friend unit is taken at stage entry. Offline status doesn't affect combat. |
| **Duplicate sea abilities from multiple crew** | Each instance is separate in the pool. Player can use ability A from crew member 1 and ability A from crew member 2 in the same turn (costs MP each). |
| **Ship with only 1 slot filled** | Valid. Ship is severely underpowered but functional. Useful in early game when roster is small. |

## Dependencies

| Dependency | Direction | Why |
|-----------|-----------|-----|
| **Unit Data Model** | Upstream (required) | Ship crew system reads unit stats, sea abilities, traits, and NavalRoleAffinity. SDM cannot function without UDM. |
| **Combate Naval** | Downstream | Consumes ship effective stats, ability pool, and crew data to run naval combat encounters. |
| **Initiative Bar** | Downstream | Reads ship SPD for naval turn order. |
| **Damage & Stats Engine** | Downstream | Reads FPW, HDF, RSL for naval damage calculations. |
| **Traits/Sinergias** | Bidirectional | SDM provides crew trait lists; Traits system returns naval synergy bonuses. |
| **Team Composition** | Downstream | UI system for crew management reads ship slots and available units. |
| **Currency System** | Upstream (partial) | Provides crafting materials (ship construction) and upgrade materials (components). SDM defines what's needed; Currency owns the inventory. |
| **Save/Load System** | Downstream | Serializes ship ownership, upgrade state, and crew assignments. |
| **Rewards System** | Downstream | Ship blueprints and upgrade materials appear as stage rewards. |
| **Unit Roster/Inventory** | Bidirectional | Roster provides available units for crew; SDM writes crew assignment state. |

**Critical path**: Unit Data Model must be designed before this system (done).
This system must be designed before Combate Naval and Team Composition.

**This system is NOT required by**: Combate Terrestre, Sistema Gacha (demo),
Progresión de Unidades (indirect only).

## Tuning Knobs

| Knob | Current Value | Safe Range | Gameplay Effect |
|------|--------------|------------|-----------------|
| CREW_SCALING_FACTOR | 0.15 | 0.10 – 0.25 | Higher = crew matters more vs. ship base stats. Too high and base stats become irrelevant; too low and crew composition doesn't matter. |
| MISMATCH_PENALTY | 0.50 | 0.25 – 0.75 | Lower = harsher penalty for wrong role. Too low and players can't improvise crews; too high and role matching is pointless. |
| Upgrade Level 1 bonus | 10% | 5% – 15% | First upgrade impact. Too low feels unrewarding; too high makes Level 0 feel bad. |
| Upgrade Level 2 bonus | 25% | 15% – 35% | Mid upgrade impact. |
| Upgrade Level 3 bonus | 45% | 30% – 60% | Max upgrade impact. Too high and un-upgraded ships feel unplayable. |
| Ship 1 slot count | 5 | 4 – 6 | Starter ship complexity. Fewer slots = simpler early game. |
| Ships 2-3 slot count | 6-7 | 5 – 8 | Crafted ship complexity. More slots = more crew management depth. |
| Crafting material cost (Ship 2) | TBD | — | Pacing gate for second ship acquisition. |
| Crafting material cost (Ship 3) | TBD | — | Pacing gate for third ship. Should feel like a late-demo reward. |
| Component upgrade material cost | TBD per level | — | Progression pacing per upgrade tier. Level 3 should require boss drops. |
| NavalRoleAffinity per unit | 1 (typical) | 1 – 2 | Units with 2 roles are more flexible but less niche. Keep at 1 to incentivize collection. |
| Guest slot bonus | TBD | — | Incentive to use friend units. Defined in Combate Naval GDD. |

## Visual/Audio Requirements

**Visual:**
- Each ship needs a distinct silhouette recognizable at thumbnail size (fleet selection UI)
- Crew members should be visible on the ship during naval combat (small sprites at their role positions)
- Upgrade levels should have visual feedback: Hull (hull texture/reinforcement), Cannons (cannon size/glow), Sails (sail size/design)
- Sinking animation when HHP reaches 0 (dramatic but brief — no gameplay stall)
- Ship stat changes from crew assignment should have a brief visual pulse in the crew management UI (green for increase, red for decrease on swap)

**Audio:**
- Each ship type should have a distinct ambient sound loop (creaking wood, sail flapping — varies by ship size)
- Crew assignment: satisfying "click into place" SFX when assigning a unit to a role slot
- Role match vs. mismatch: subtle positive chime for matching role, neutral sound for mismatch
- Upgrade completion: escalating SFX per level (Level 3 should feel like a major power-up)
- Sinking: hull cracking + water rushing SFX

## UI Requirements

**Crew Management Screen:**
- Ship visual centered, role slots displayed around/on the ship at thematic positions
- Each slot shows: role icon, role name, assigned unit sprite (or empty state)
- Drag-and-drop OR tap-to-assign unit to slot
- Role match indicator: green checkmark if unit's NavalRoleAffinity matches, yellow warning if mismatch
- Stat preview panel: shows effective ship stats updating in real-time as crew changes
- "Auto-assign" button: fills empty slots with best-matching available units (convenience, not optimal)
- Guest slot visually distinct (different border/glow) with friend unit list accessible

**Ship Upgrade Screen:**
- Three component tabs (Hull, Cannons, Sails) with current level and progress
- Material requirements displayed with owned/needed counts
- Preview of stat changes before confirming upgrade
- Upgrade button greyed out if materials insufficient

**Fleet Selection (Pre-Stage):**
- Ship thumbnails with silhouette, name, and effective stat summary
- Crew fill indicator (e.g., "5/7 slots filled")
- Warning icon if ship has empty slots or mismatched roles
- Tap to inspect full crew composition before deploying

**Ship Blueprint/Crafting Screen:**
- Locked ships shown as silhouettes with "???" stats
- Crafting recipe visible: required materials with owned/needed counts
- Craft button with confirmation dialog

## Acceptance Criteria

| # | Criterion | How to Verify |
|---|-----------|---------------|
| 1 | ShipData loads all fields correctly from asset | Unit test: deserialize a ShipData asset, assert all fields populated |
| 2 | Crew contribution formula produces correct values | Unit test: known unit stats + role → expected contribution. Test both match and mismatch. |
| 3 | Effective stat = base + upgrades + crew + traits | Unit test: precalculated ship with known inputs → expected effective stats |
| 4 | Empty slots contribute zero | Unit test: ship with empty slots → effective stats equal base + upgrades only |
| 5 | Mismatch penalty applies correctly | Unit test: same unit in matching vs. non-matching slot → 50% reduction |
| 6 | Upgrade bonuses are non-linear (10%/25%/45%) | Unit test: each upgrade level → expected bonus per component |
| 7 | Ship with 0 crew is deployable | Integration test: deploy empty ship to naval stage, combat starts successfully |
| 8 | Same unit in multiple crews works independently | Integration test: unit in 2 ships → each ship calculates contribution independently |
| 9 | Sea abilities from crew appear in ship ability pool | Integration test: assign unit with sea abilities → abilities visible in combat |
| 10 | Crew removal updates stats and abilities immediately | Integration test: remove unit → stats recalculated, abilities removed from pool |
| 11 | Ship crafting consumes correct materials | Integration test: craft Ship 2 → materials deducted, ship added to fleet |
| 12 | Component upgrades persist across sessions | Save/load test: upgrade Hull to Level 2, save, reload → upgrade preserved |
| 13 | Guest slot accepts friend unit with bonus | Integration test: assign friend → bonus applied. Assign own unit → no bonus. |
| 14 | Role match indicator shows correctly in UI | Visual test: matching unit shows green, mismatched shows yellow |

## Open Questions

| # | Question | Impact | Status / Resolution |
|---|----------|--------|---------------------|
| 1 | How does ship SPD interact with unit SPD on the initiative bar? | Initiative Bar GDD | **Resolved**: Unit SPD contributes to ship SPD via crew contribution formula. Naval combat has a single initiative bar where allied ship and enemy ships are positioned by their effective SPD. Same system as land combat but with ships instead of units. |
| 2 | Can a ship have multiple slots of the same role? (e.g., 2 Artillero slots) | Ship identity design | **Resolved**: Yes — this is part of what makes each ship unique. A warship could have 3 Artillero slots. |
| 3 | What happens when a crew member dies in naval combat? Does the ship lose their stat/ability contribution mid-combat? | Combate Naval GDD | Two options: (a) crew is protected by ship HP, or (b) targeted crew attacks exist. Decide in Combate Naval. |
| 4 | Should ship-vs-ship combat exist (PvP naval)? | Scope | Not in demo. Future consideration for full game. |
| 5 | How do enemy ships work? Do they use the same ShipData model? | Enemy System GDD | Recommended: same model with EnemyShipData extension (mirrors CharacterData → EnemyData pattern). |
| 6 | Full game ship gacha: rarity tiers for ships? | Future design | Deferred. When roster reaches 20+, define ship rarity and gacha pools. |
| 7 | Retrofit/prestige system for fully upgraded ships? | Endgame progression | Deferred. Needed when players max all 3 components — gives continued material sink. |
