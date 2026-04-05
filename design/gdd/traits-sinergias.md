# Traits/Sinergias

> **Status**: Approved
> **Author**: User + Claude Code Game Studios agents
> **Last Updated**: 2026-03-27 (updated: dual-Captain mechanic from TC GDD)
> **Implements Pillar**: Pillar 3 (Recompensa a la Paciencia), Pillar 1 (Profundidad Estratégica Dual)

## Overview

The Traits/Sinergias system defines how shared tags (traits) on characters create
combat bonuses when activated by a designated Captain. Each unit has 1-3 traits
stored in their CharacterData. Traits are inert labels until a Captain activates
them: in land combat, the player designates any unit as Captain (free slot choice);
in naval combat, the unit assigned to the ship's Capitán role slot is the Captain.

When a Captain's trait is shared by 3 or more allies in the same team (including
the Captain), that trait's synergy activates, granting a stat buff (~10-15%) to
all allies who share the trait. Only the Captain's traits are checked for
activation — other units' traits only matter for matching, not for initiating
synergies. This creates a central team-building decision: who leads, and who
supports their vision.

The demo launches with a single synergy tier (3-unit threshold, stat buffs only).
The system is architecturally designed to scale: future content adds stronger
trait bonuses on newer units, additional tiers (e.g., Tier 2 at 5+ units), and
special effects beyond raw stats. When older units fall behind, a trait-upgrade
system (akin to Dokkan's EZA) refreshes their synergy values, creating a healthy
collect-and-revisit cycle.

Enemies also have traits and a designated captain per encounter group. Killing the
enemy captain deactivates all enemy synergies — not by reducing headcount, but by
removing the activator. This creates a tactical priority target in every fight.
Trait synergies are side-specific: allied synergies count allies only, enemy
synergies count enemies only.

## Player Fantasy

**"That forgotten sailor was the missing piece."** (Pillar 3 — Recompensa a la
Paciencia)

The trait system exists to deliver one specific moment: the player pulls or
rediscovers a unit they had dismissed, notices it shares a trait with their
Captain, realizes it completes a synergy — and suddenly their entire team gets
stronger. That moment of *connection* between seemingly unrelated units is the
emotional core of this system.

**The strategist's pride** (Pillar 1 — Profundidad Estratégica Dual): Choosing
who to designate as Captain is a declaration of strategy. The player who picks a
3★ Captain because their traits align perfectly with the team should feel smarter
than the player who defaults to the highest-rarity unit. In naval, choosing which
unit fills the Capitán slot on the ship becomes doubly meaningful — it determines
both the role bonus AND which synergies activate.

**The scout's intuition**: When facing a new enemy group, the player scans for
the enemy captain. "If I take out their leader first, their synergies collapse."
This creates a natural tactical layer in every encounter without adding mechanical
complexity.

The system fails if: the player never notices synergies activating (UI failure),
if optimal play is always "use highest rarity, ignore traits" (balance failure),
or if every team naturally activates synergies without effort (composition is too
easy — no meaningful choice).

## Detailed Design

### Core Rules

#### 1. Trait Data Structure

A trait is a named tag with a global definition and per-unit bonus values:

**TraitDefinition** (global, shared):

| Field | Type | Description |
|-------|------|-------------|
| `TraitId` | string | Unique identifier (e.g., `"hijos_del_mar"`) |
| `DisplayName` | string | Localized name (e.g., "Hijos del Mar") |
| `Description` | string | Flavor text describing the faction/origin |
| `Icon` | SpriteRef | Badge icon displayed on unit cards and synergy UI |
| `Category` | enum | `Faction` for demo. Future: `Origin`, `Class`, `Curse`, etc. |

**UnitTraitEntry** (per-unit, stored in CharacterData.Traits):

| Field | Type | Description |
|-------|------|-------------|
| `TraitId` | string | Reference to the TraitDefinition |
| `SynergyBonus` | StatModifier[] | Stat bonuses this unit contributes when the synergy is active (e.g., `[{stat: ATK, percent: 0.12}]`) |

Synergy bonuses are defined **per unit**, not per trait globally. This allows
newer units to have stronger bonuses for the same trait, enabling controlled
power creep and later rebalancing of older units (EZA-style refresh).

If a unit has a trait but its `SynergyBonus` array is empty or undefined, the
unit **counts toward the threshold** but receives no buff when the synergy is
active. This is not an authoring error — it enables "enabler" units that help
activate synergies for others without benefiting themselves.

#### 2. Captain Mechanic

Synergies only activate through a designated **Captain**:

- **Land combat**: The player designates any unit in their team (slots 1-5) as
  Captain before combat. This is a free choice — no role restriction. The guest
  unit (slot 6) cannot be designated as the primary Captain.
- **Naval combat**: The unit assigned to the ship's **Capitán role slot** is
  automatically the Captain. No additional designation needed.

Only the Captain's traits are checked for synergy activation. Other units' traits
are only relevant for matching (counting toward thresholds).

**Second Captain (Friend Unit)**:
- If the guest unit (slot 6 in land, guest slot in naval) is a **friend unit**
  (from another player), it functions as a **second Captain** for synergies.
- The second Captain's traits are evaluated independently after the primary
  Captain's traits, using the same activation rules.
- If both Captains share a trait, the synergy activates **twice** — each
  activation generates its own independent SynergyBonus buffs that stack.
- If the guest unit is a **player-owned unit** (no friends available), it does
  NOT function as a second Captain — it only counts toward thresholds.
- This mechanic incentivizes strategic friend selection: choosing friends whose
  Captain units share traits with your own Captain for double activation.

If the Captain is KO'd during combat, allied synergies **deactivate immediately**
— specifically, after the action that caused the KO fully resolves (all hits of
a multi-hit ability, AoE damage to all targets) but before the next combatant's
turn begins. If the Captain is revived, synergies **reactivate** with the same
timing. This creates tactical stakes around protecting your Captain.

#### 3. Synergy Activation (Demo: 1 Tier)

**Primary Captain evaluation**:
For each trait on the primary Captain:
1. Count how many allies in the team (including both Captains) share that trait
2. If count ≥ 3 → synergy activates
3. All allies who share the trait receive their individual `SynergyBonus` as a
   permanent, non-dispellable buff for the duration of combat

**Second Captain evaluation** (if guest is a friend unit):
For each trait on the second Captain:
1. If this trait was already activated by the primary Captain, it activates
   **again** — a second independent application of SynergyBonus buffs
2. If this trait was NOT checked by the primary Captain, count allies sharing
   it (including both Captains). If count ≥ 3 → synergy activates
3. Buffs from the second activation stack additively with the first

Units that don't share an active trait receive no buff from it.

**Double activation**: When both Captains share a trait and it meets threshold,
units with that trait receive their `SynergyBonus` **twice** (two independent
buff instances). Both instances are permanent, non-dispellable, and subject to
the DSE ±100% buff cap collectively.

**Activation timing**: Synergies are evaluated at **combat start** and
**re-evaluated** whenever a Captain is KO'd or revived. Primary and second
Captain are evaluated independently — if the primary Captain is KO'd, only
their synergies deactivate; the second Captain's synergies remain active (and
vice versa). They are NOT recalculated when non-Captain allies are KO'd (the
count is based on the starting team composition, not surviving members).

**Maximum simultaneous synergies**: Limited by the combined trait count of both
Captains. With a primary Captain (1-3 traits) and a second Captain (1-3 traits),
the theoretical maximum is 6 synergy activations. In practice during the demo
(3 traits total), most teams will activate 0-2 synergies.

#### 4. Enemy Synergies

Enemy groups can have a designated **enemy captain** (marked in encounter data).
The enemy captain's traits activate synergies for the enemy side using the same
rules: count enemies sharing each of the captain's traits, threshold of 3.

Killing the enemy captain **deactivates all enemy synergies immediately**. This
is the primary way to disrupt enemy synergies — not by reducing headcount.

If no enemy captain is designated for an encounter, enemies have no active
synergies (their traits are inert).

#### 5. Demo Trait Roster (3 Traits)

| TraitId | Display Name | Lore | Stat Buffed (Land) | Stat Buffed (Naval) | Demo Bonus Range |
|---------|-------------|------|-------------------|--------------------|----|
| `hijos_del_mar` | Hijos del Mar | Piratas de sangre salada, nacidos en el océano | ATK | FPW | +10-15% |
| `malditos` | Malditos | Tripulación maldita, poder oscuro y sobrenatural | MST | MST | +10-15% |
| `hierro_viejo` | Hierro Viejo | Veteranos curtidos en mil batallas, piel de hierro | DEF | HDF | +10-15% |

Each trait buffs exactly one stat per combat mode. The specific percentage is
defined per unit (see §1) within the 10-15% range for demo units. Future units
may exceed this range.

#### 6. Cross-System Update: UDM Trait Field

The Unit Data Model currently stores `Traits` as `List<TraitId>`. This must be
updated to `List<UnitTraitEntry>` to include per-unit synergy bonuses. The
`CharacterData` base class change affects both player units and enemies.

### States and Transitions

Traits themselves are static data — they don't have lifecycle states. The
**synergy state** is a runtime combat concept:

| State | Description | Transitions To |
|-------|-------------|----------------|
| **Inactive** | Trait exists on units but no Captain activates it, or threshold not met | Active (combat starts with valid Captain + threshold) |
| **Active** | Threshold met, Captain alive — all matching allies receive SynergyBonus buffs | Suppressed (Captain KO'd), Inactive (combat ends) |
| **Suppressed** | Captain is KO'd — synergy was active but is temporarily disabled. Buffs removed. | Active (Captain revived), Inactive (combat ends) |

**Enemy synergy states** follow the same model, with one difference: there is
no "Suppressed" state for enemies — killing the enemy captain transitions
directly to Inactive (enemy captains cannot be revived in the demo).

**State transitions happen immediately** — there is no delay or "next turn"
wait. When the Captain falls, buffs are removed before the next action resolves.

### Interactions with Other Systems

| System | Direction | Data Interface |
|--------|-----------|----------------|
| **Unit Data Model** | UDM → Traits | Reads `List<UnitTraitEntry>` from CharacterData to identify which traits each unit has and their bonus values. **Cross-system update**: UDM field changes from `List<TraitId>` to `List<UnitTraitEntry>`. |
| **Ship Data Model** | SDM ↔ Traits | SDM provides crew trait lists (via assigned units). Traits system returns `TraitBonuses` value consumed by SDM's effective stat formula: `EffectiveStat = BaseStat + UpgradeBonus + CrewContribution + TraitBonuses`. |
| **Damage & Stats Engine** | Traits → DSE | Synergy bonuses are injected as **permanent, non-dispellable buffs** into the DSE buff system. They follow standard buff stacking rules (additive per stat, ±100% cap). |
| **Combate Terrestre** | Traits → CT | At combat start, CT calls the trait evaluation function with the team composition, primary Captain designation, and second Captain flag (friend unit). Receives the list of active buffs to apply. Re-evaluates independently on primary/second Captain KO/revive. |
| **Combate Naval** | Traits → CN | Same interface as CT. Captain is determined by the ship's Capitán role slot. Trait bonuses apply to the ship's effective stats via the SDM formula. |
| **Enemy System** | Enemy → Traits | Enemy encounter data includes an optional `EnemyCaptainId` field. The trait system reads enemy captain's traits and evaluates enemy-side synergies using the same rules. |
| **Team Composition** | Traits → TC | Team builder UI reads trait data to show synergy previews in real-time as units are added/removed and Captain is designated. TC provides the second Captain flag (friend vs own unit) for dual evaluation. See TC GDD for synergy preview formula. |
| **Combat UI** | Traits → UI | Provides: active synergy names/icons, buff indicators on affected units, "Captain" badge on the designated unit, visual feedback on enemy captain. |
| **Initiative Bar** | Indirect | No direct interaction. SPD is not buffed by any demo trait, but the system supports SPD buffs in future traits. |
| **Auto-Battle** | Traits → Auto | Auto-battle AI should prioritize killing the enemy captain when enemy synergies are active. |

**Interface ownership**: The Traits system owns synergy evaluation logic
(activation rules, threshold checks, bonus calculation). The DSE owns how
those bonuses are applied to combat math. The SDM owns how trait bonuses
integrate into effective ship stats.

## Formulas

### 1. Synergy Activation Check

```
// Primary Captain evaluation
For each trait T in PrimaryCaptain.Traits:
  MatchCount = count(allies where T in ally.Traits)  // includes both Captains
  If MatchCount >= SYNERGY_THRESHOLD:
    T is ACTIVE (primary)

// Second Captain evaluation (only if guest is friend unit)
If SecondCaptain exists:
  For each trait T in SecondCaptain.Traits:
    MatchCount = count(allies where T in ally.Traits)  // includes both Captains
    If MatchCount >= SYNERGY_THRESHOLD:
      T is ACTIVE (secondary)
      // If T was already ACTIVE (primary), this is a DOUBLE ACTIVATION
```

### 2. Synergy Buff Application

For each active synergy (primary and secondary evaluated independently):

```
When trait T is ACTIVE, for each ally that has trait T:
  For each StatModifier in ally.UnitTraitEntry[T].SynergyBonus:
    Apply permanent buff: StatModifier.stat += StatModifier.percent

// Double activation: if T is active from BOTH Captains, buffs apply TWICE
// (two independent buff instances, stacking additively)
```

These buffs feed into the DSE's existing modifier system:

```
EffectiveStat = BaseStat × clamp(1.0 + sum(all_buffs) - sum(all_debuffs), 0.0, 2.0)
```

Synergy buffs count as part of `sum(all_buffs)` and are subject to the ±100% cap.

### 3. Naval Trait Bonus Integration

In naval combat, trait bonuses modify the **ship's effective stats** (not the
crew member's unit stats). The bonus is applied after crew contributions:

```
EffectiveShipStat = BaseStat + UpgradeBonus + sum(CrewContribution) + TraitBonuses

TraitBonuses = sum of all active synergy bonuses for this stat
             = sum(SynergyBonus.percent × BaseStat) for each crew member with active trait
```

Note: In naval, the percentage is applied to the **ship's base stat**, not the
crew member's unit stat. This keeps naval synergies proportional to the ship's
power level.

**Important**: Naval trait bonuses are **flat additive values** added to the ship's
effective stat, NOT percentage buffs flowing through the DSE ±100% cap system.
This means naval synergies are uncapped — they add directly to the ship stat
formula alongside crew contributions and upgrades. This is intentional: the SDM
effective stat formula (`BaseStat + UpgradeBonus + CrewContribution + TraitBonuses`)
is additive, not multiplicative. The ±100% DSE cap only applies to land combat
synergy buffs where they enter the DSE buff pipeline.

### 4. Worked Example: Land Combat (Single Captain)

```
Team: Captain[HdM, Mald] + UnitA[HdM] + UnitB[HdM] + UnitC[Mald] + UnitD[] + Guest[HdM] (own unit)

Guest is player-owned → NOT second Captain

Primary Captain check for "Hijos del Mar":
  Captain(HdM) + UnitA(HdM) + UnitB(HdM) + Guest(HdM) = 4 matches
  4 >= 3 → ACTIVE

Primary Captain check for "Malditos":
  Captain(Mald) + UnitC(Mald) = 2 matches
  2 < 3 → INACTIVE

Apply HdM synergy (1x):
  Captain: +12% ATK | UnitA: +10% ATK | UnitB: +15% ATK | Guest: +12% ATK
  UnitC, UnitD: no HdM → no buff
```

### 5. Worked Example: Double Captain Activation

```
Team: Captain[HdM, Mald] + UnitA[HdM] + UnitB[HdM] + UnitC[Mald] + UnitD[] + Friend[HdM, Mald]

Friend is from another player → SECOND CAPTAIN

Primary Captain check for "Hijos del Mar":
  Captain + UnitA + UnitB + Friend = 4 matches → ACTIVE (primary)
Primary Captain check for "Malditos":
  Captain + UnitC + Friend = 3 matches → ACTIVE (primary)

Second Captain check for "Hijos del Mar":
  Already active from primary → DOUBLE ACTIVATION
Second Captain check for "Malditos":
  Already active from primary → DOUBLE ACTIVATION

Apply HdM synergy (2x — double activation):
  Captain: +12% ATK × 2 = +24% ATK total
  UnitA:   +10% ATK × 2 = +20% ATK total
  UnitB:   +15% ATK × 2 = +30% ATK total
  Friend:  +12% ATK × 2 = +24% ATK total

Apply Malditos synergy (2x — double activation):
  Captain: +12% MST × 2 = +24% MST total
  UnitC:   +10% MST × 2 = +20% MST total
  Friend:  +11% MST × 2 = +22% MST total

Total buffs on Captain: +24% ATK, +24% MST (both subject to ±100% DSE cap)
```

### Variable Definitions

| Variable | Type | Default | Range | Description |
|----------|------|---------|-------|-------------|
| SYNERGY_THRESHOLD | int | 3 | 2-5 | Minimum allies sharing a Captain's trait to activate synergy |
| SynergyBonus.percent | float | varies | 0.05-0.25 | Per-unit synergy buff magnitude (demo: 0.10-0.15) |
| StatModifier.stat | enum | — | ATK, DEF, MST, SPR, HP, SPD, FPW, HDF, RSL, HHP | Which stat the synergy buffs |

## Edge Cases

| Edge Case | Resolution |
|-----------|------------|
| **Captain has 0 matching allies for any trait** | No synergies activate. Captain functions normally without buffs. Common in early game with small roster. |
| **All 5 allies + guest share the Captain's trait** | Synergy activates for all 6. Maximum benefit scenario — intentionally possible but hard to achieve (requires 6 units with same trait). |
| **Captain has 3 traits, all meet threshold** | All 3 synergies activate simultaneously. Each unit receives buffs for every matching trait they have. Buffs stack additively per the DSE rules. |
| **Guest/friend unit shares Captain's trait** | Guest counts toward the threshold AND receives the buff. This incentivizes choosing friends with matching traits — a healthy social mechanic. |
| **Guest unit IS the same template as Captain** | Allowed (per UDM rules: friend copy of owned unit is permitted in slot 6). Both contribute to trait count. |
| **Captain is KO'd mid-round** | Synergies deactivate immediately. Buffs are removed before the next action resolves. If other units had pending turns this round, they act without the synergy buff. |
| **Captain is revived the same round they were KO'd** | Synergies reactivate immediately upon revival. Buffs are restored. |
| **Captain is KO'd and no revive is available** | Synergies remain Suppressed for the rest of combat. The team must win without synergy support. |
| **Two different active traits buff the same stat** | Buffs stack additively (per DSE rules). E.g., if trait A gives +12% ATK and trait B also gives +10% ATK to the same unit, total = +22% ATK buff. Subject to ±100% cap. |
| **Enemy captain is KO'd in wave 1 of a multi-wave battle** | Enemy synergies are deactivated for wave 1 only. Each wave has its own encounter data with its own enemy captain designation. |
| **Enemy encounter has no designated captain** | No enemy synergies activate. Enemies have trait tags but they remain inert. Normal encounter. |
| **Unit with 0 traits is designated Captain** | No synergies can activate (no traits to check). Legal but suboptimal — the UI should warn the player. |
| **All non-Captain allies are KO'd** | Synergies remain active (count is based on starting composition, not survivors). The Captain alone still benefits from their own synergy buff if the threshold was met at combat start. |
| **Trait bonus percent is 0% on a unit** | Valid — unit counts toward the threshold but receives no buff. Could be used for "enabler" units that help others activate synergies without benefiting themselves. |
| **Same trait appears on both sides (ally + enemy)** | Side-specific evaluation. Allied synergies and enemy synergies are calculated independently. No cross-side interaction. |
| **Both Captains share a trait (double activation)** | The synergy activates twice. Each activation applies SynergyBonus independently. Buffs stack additively (e.g., +12% × 2 = +24%). Subject to DSE ±100% cap. |
| **Second Captain (friend) is KO'd** | Only the second Captain's synergies deactivate. Primary Captain's synergies remain active. If the friend is revived, second Captain synergies reactivate. |
| **Primary Captain KO'd, second Captain alive** | Primary synergies deactivate, second Captain synergies remain active. Traits unique to the primary Captain lose activation; shared traits drop from double to single activation. |
| **Guest is player-owned unit, not friend** | No second Captain. Guest counts toward thresholds but does not initiate synergy evaluation. Single Captain only. |
| **Both Captains have different traits, both meet threshold** | All qualifying traits activate — one set from each Captain. No double activation for non-shared traits, but both sets are active simultaneously. |

## Dependencies

### Upstream Dependencies (systems this depends on)

| System | Dependency Type | Interface |
|--------|----------------|-----------|
| **Unit Data Model** | Hard | Reads `List<UnitTraitEntry>` from CharacterData. Requires cross-system update: field type change from `List<TraitId>` to `List<UnitTraitEntry>`. |
| **Ship Data Model** | Hard (naval only) | Reads crew assignments to determine which units are on the ship and who fills the Capitán slot. |
| **Damage & Stats Engine** | Hard | Synergy buffs are applied through the DSE buff system. Depends on DSE's permanent buff infrastructure. |
| **Enemy System** | Soft | Reads optional `EnemyCaptainId` from encounter data. System works without it (no enemy synergies). |

### Downstream Dependencies (systems that depend on this)

| System | Dependency Type | Interface |
|--------|----------------|-----------|
| **Combate Naval** | Hard | Naval combat requires trait evaluation for ship stat calculation. Without traits, the `TraitBonuses` term in the ship effective stat formula is always 0. |
| **Combate Terrestre** | Soft | Land combat functions without traits (buffs are simply 0). Traits enhance it but are not required for combat to work. |
| **Team Composition** | Soft | Team builder shows synergy previews. Functions without traits but loses a key UI feature. |
| **Combat UI** | Soft | Displays synergy indicators. Functions without them. |
| **Auto-Battle** | Soft | Auto AI uses enemy captain targeting priority. Functions without it (just loses tactical optimization). |

### Bidirectional Note

The **Unit Data Model** requires a cross-system update to its `Traits` field
structure. This change must be coordinated — the UDM GDD should be updated to
reflect `List<UnitTraitEntry>` and note that synergy bonus values are defined
by this system.

## Tuning Knobs

### Global System Knobs

| Knob | Current Value | Safe Range | What It Affects | If Too High | If Too Low |
|------|--------------|------------|-----------------|-------------|------------|
| SYNERGY_THRESHOLD | 3 | 2-5 | How many allies needed to activate a synergy | Hard to activate, system feels dead | Too easy, no composition effort needed |
| Demo trait count | 3 | 3-6 | Number of distinct traits available | Spread too thin, synergies rarely activate | Too concentrated, no variety |
| Max traits per unit | 3 | 1-3 | How many traits a single unit can have | Too many synergies possible, balance nightmare | Units feel disconnected, fewer team-building options |

### Per-Unit Knobs

| Knob | Current Range | Safe Range | What It Affects | If Too High | If Too Low |
|------|--------------|------------|-----------------|-------------|------------|
| SynergyBonus percent (demo) | 10-15% | 5-25% | Buff magnitude when synergy is active | Synergies dominate; non-synergy teams unviable | Not worth building around; ignored by players |
| Trait count per unit | 1-3 | 1-3 | How connected a unit is to the trait web | Unit fits too many teams (less reason to collect others) | Unit too niche (only fits one team) |

### Future Scaling Knobs (Post-Demo)

| Knob | Description |
|------|-------------|
| Tier 2 threshold | Higher threshold (e.g., 5+) for stronger bonuses on newer units |
| Tier 2 bonus magnitude | Stronger percentages or special effects for newer generations |
| EZA-style trait refresh | Increase older units' SynergyBonus values via content update |
| New trait categories | Origin, Class, Curse — expanding the trait web beyond Factions |

### Knob Interactions (Danger Zones)

| Knob A | Knob B | Interaction |
|--------|--------|-------------|
| SYNERGY_THRESHOLD | Traits per unit | Lower threshold + more traits per unit = too many synergies active. If threshold drops to 2, limit traits per unit to 1-2. |
| SynergyBonus percent | DSE buff cap (±100%) | Synergy buffs share the cap with ability buffs. If synergies take 30%+ of the cap, there's less room for in-combat buffs. Keep synergy bonuses modest. |
| Demo trait count | Unit roster size | With 3 traits and 12 units, each trait covers ~8 units. If roster shrinks to 8, each trait covers ~5 — synergies are easy. If roster grows to 20, consider adding traits. |
| SynergyBonus percent | Rarity stat gap (~3x) | This is the Pillar 3 constraint. If synergy bonuses are below ~10%, the stat gap makes 3★ units non-viable even with synergies. Keep bonuses at 10%+ minimum. |

## Visual/Audio Requirements

### Visual

- **Captain badge**: A **sombrero pirata** (pirate hat) icon displayed on the
  Captain unit in combat and team builder. Must be clearly visible on the chibi
  sprite (above head or beside name). Second Captain (friend) shows the same
  hat with a secondary style (e.g., slightly smaller or different color accent).
- **Trait icons**: Small badge icons (16x16 px minimum on mobile) for each trait,
  displayed on unit cards and in the synergy panel. Each of the 3 demo traits
  needs a unique icon: Hijos del Mar (wave/anchor), Malditos (skull/curse),
  Hierro Viejo (iron shield).
- **Synergy activation feedback**: When combat starts and synergies activate, a
  brief visual pulse on all buffed units (colored glow matching the trait icon).
  Duration: ~0.5s.
- **Synergy deactivation feedback**: When Captain is KO'd, a visual "shatter" or
  fade effect on the trait icon in the synergy panel. Buffed units lose their glow.
- **Enemy captain indicator**: A distinct marker (red crown or skull icon) above
  the enemy captain to signal "priority target". Should be visible without tapping
  the enemy.
- **Synergy panel**: A compact UI element showing active synergies during combat
  (trait icon + count, e.g., "Hijos del Mar 4/3").

### Audio

- **Synergy activation**: A satisfying "power up" chime when synergies activate
  at combat start. One sound regardless of how many synergies activate.
- **Synergy deactivation**: A muted "break" sound when Captain is KO'd and
  synergies collapse.
- **Enemy captain kill**: A distinct "leadership broken" SFX when the enemy
  captain falls, reinforcing the tactical reward.

## UI Requirements

- **Team Builder — Captain designation**: A toggle or tap action to designate a
  unit as Captain (slots 1-5 only). The selected Captain shows the crown badge.
  Changing Captain updates the synergy preview panel in real-time.
- **Team Builder — Synergy preview panel**: Shows all of the current Captain's
  traits, how many team members share each, and whether the threshold (3) is met.
  Updates live as units are added/removed. Example: "Hijos del Mar: 2/3 (need 1
  more)" in yellow, "Hijos del Mar: 3/3" in green.
- **Combat HUD — Active synergies strip**: A compact horizontal strip showing
  active synergy icons with count badges. Tapping a synergy icon shows which
  units are buffed and the buff values.
- **Combat HUD — Captain indicator**: Crown badge on the Captain's portrait in
  the turn order bar and on their chibi sprite.
- **Combat HUD — Enemy captain marker**: Red crown on the enemy captain's
  portrait and sprite. Tapping shows "Defeat to remove enemy synergies".
- **Crew Builder (Naval) — Synergy preview**: Same synergy preview as land team
  builder, but Captain is automatically the unit in the Capitán slot. Shows ship
  stat changes from trait bonuses.
- **Unit detail screen — Trait display**: Shows the unit's traits with icons,
  names, and their SynergyBonus values. Example: "Hijos del Mar: +12% ATK when
  active".
- **Pre-combat summary**: Before entering combat, show a summary of active
  synergies for the current team/crew composition.

## Acceptance Criteria

### Synergy Activation

- [ ] Synergy activates when Captain has a trait shared by 3+ allies (including Captain)
- [ ] Synergy does NOT activate when fewer than 3 allies share the Captain's trait
- [ ] Only the Captain's traits are checked for activation — non-Captain traits are ignored as activators
- [ ] Multiple synergies activate simultaneously if the Captain has multiple traits meeting threshold
- [ ] Guest unit (slot 6) counts toward threshold and receives buff if they share the trait

### Captain Mechanic

- [ ] In land combat, any unit in slots 1-5 can be designated Captain (no role restriction)
- [ ] In naval combat, the unit in the ship's Capitán role slot is automatically Captain
- [ ] Slot 6 (guest) cannot be designated as primary Captain in land combat
- [ ] Unit with 0 traits can be designated Captain (legal but no synergies activate)
- [ ] UI shows a warning when a 0-trait unit is designated Captain

### Second Captain (Friend Unit)

- [ ] Friend unit in slot 6/guest functions as second Captain — its traits are evaluated for synergy activation
- [ ] Player-owned unit in slot 6/guest does NOT function as second Captain
- [ ] When both Captains share a trait and threshold is met, synergy activates twice (double SynergyBonus)
- [ ] Primary Captain KO'd: only primary synergies deactivate; second Captain synergies remain active
- [ ] Second Captain KO'd: only secondary synergies deactivate; primary Captain synergies remain active
- [ ] Double activation buffs stack additively and are subject to DSE ±100% cap

### Buff Application

- [ ] Synergy buffs apply as permanent, non-dispellable buffs in the DSE system
- [ ] Each unit receives its own `SynergyBonus` value (not a global trait value)
- [ ] Buffs are additive with other buffs and subject to the ±100% stat cap
- [ ] Only units sharing the active trait receive the buff — non-matching allies get nothing

### Captain KO/Revival

- [ ] Allied synergies deactivate immediately when Captain is KO'd
- [ ] Allied synergies reactivate immediately when Captain is revived
- [ ] Non-Captain ally KO does NOT affect synergy activation (count based on starting team)

### Enemy Synergies

- [ ] Enemy captain's traits activate enemy synergies following the same threshold rules
- [ ] Killing the enemy captain deactivates all enemy synergies immediately
- [ ] Encounter with no designated enemy captain has no enemy synergies
- [ ] Enemy synergies and allied synergies are evaluated independently (side-specific)

### Naval Integration

- [ ] Trait bonuses apply to ship effective stats (not crew member unit stats)
- [ ] TraitBonuses term in ship effective stat formula produces correct values
- [ ] Synergy evaluation uses the ship's Capitán slot occupant as Captain

### Performance

- [ ] Synergy evaluation (all traits, full team): < 1ms
- [ ] Re-evaluation on Captain KO/revive: < 0.5ms
- [ ] Memory per active synergy state: < 256 bytes

## Open Questions

| # | Question | Owner | Target Resolution |
|---|----------|-------|-------------------|
| 1 | ~~How does the Captain designation persist?~~ | Team Composition GDD | **Resolved**: Captain is designated per preset (default: slot 1). Persisted with the preset in save data. Changing team composition does not reset Captain — it stays on the designated slot until the player explicitly changes it. See Team Composition GDD §Core Rules. |
| 2 | Should the enemy captain be visually revealed from combat start, or should the player discover them? | Combat UI / Game Design | When Combat UI is designed |
| 3 | How do trait bonuses interact with the Equipment System? Can equipment grant or modify traits? | Equipment System GDD | When Equipment is designed |
| 4 | For the EZA-style trait refresh: what triggers an upgrade? Special event stage, materials, or automatic with a content update? | Live-ops / Progresión design | Post-demo |
| 5 | Should naval trait bonuses apply to ship base stat (current design) or to crew contribution values? The current choice (base stat) makes synergies scale with ship upgrades, not with unit levels. | Balance validation | During prototyping |
| 6 | ~~Cross-system update needed: UDM `Traits` field change from `List<TraitId>` to `List<UnitTraitEntry>`.~~ | Coordination | **Resolved**: UDM edge cases updated 2026-03-27. Full field type change deferred to implementation — UDM GDD notes the dependency. Enemy System GDD also updated with `IsEncounterCaptain` field and Captain model. |
