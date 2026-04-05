# Initiative Bar

> **Status**: Approved
> **Author**: User + Claude Code Game Studios agents
> **Last Updated**: 2026-03-26
> **Implements Pillar**: Pillar 1 (Profundidad Estratégica Dual)

## Overview

The Initiative Bar is a **round-based timeline** system that determines turn
order in both land and naval combat. It is a horizontal bar displayed at the top
of the combat screen, showing the action sequence for the current round.

At the start of each round, all combatants (allies and enemies) are sorted by
their SPD stat from highest to lowest and placed on the bar as icons, left to
right. Each combatant acts **once per round** in this order. When a combatant
takes their turn, their icon is removed from the bar. When the last icon is
removed, the round ends and a new round begins — the bar is recalculated with
current SPD values, reflecting any buffs, debuffs, or status effects applied
during the previous round.

The player reads the bar to plan ahead: "The enemy boss acts before my healer
this round — I need to use a defensive ability now" or "If I buff my unit's SPD,
they'll act first next round." The visible order transforms combat from reactive
guessing into deliberate planning — the core of Pillar 1 (Profundidad Estratégica
Dual).

A special **"Limit Break" mechanic** allows certain fast units to break the
one-turn-per-round rule and act twice in a single round under specific conditions.
This makes SPD investment meaningful beyond just acting early.

In land combat, each unit has its own icon on the bar. In naval combat, each
**ship** (not individual crew members) has an icon, using the ship's effective SPD.

## Player Fantasy

**The chess master who sees three moves ahead**: The player looks at the
initiative bar and reads the round like a general reading a battlefield map.
"The boss acts third — if I debuff its ATK with my fastest unit, my squishy
mage will survive the hit." This planning is the core satisfaction: every turn
is a puzzle where the answer is written on the bar if you know how to read it.

**The adrenaline of the Limit Break**: Your fastest unit has been building
toward a Limit Break all fight. When it triggers, their icon reappears on the
bar — a second action this round. The enemy was supposed to survive to their
turn, but now your unit strikes again. The crowd goes wild. Limit Breaks should
feel like clutch moments that reward investment in SPD and team building.

**The dread of losing the order**: The enemy caster stuns your fastest unit.
The bar shifts — your DPS that was supposed to act first now acts after the
boss. The round that was safe is now dangerous. Status effects that alter the
bar create tension and force adaptation, keeping every round fresh.

## Detailed Design

### Core Rules

#### 1. Round Structure

Combat is divided into **rounds**. Each round:

1. **Pre-combat phase** (first round only): Apply passive abilities, equipment
   effects, and pre-combat items that modify SPD.
2. **Calculate order**: Sort all alive combatants by effective SPD (highest first).
3. **Populate the bar**: Place all combatant icons on the initiative bar, left
   to right.
4. **Execute turns**: The leftmost icon acts. After acting, the icon is removed.
5. **Mid-round reorder**: If any SPD change occurs (buff, debuff, passive trigger),
   the **remaining unacted icons** are immediately resorted by current effective SPD.
6. **Check for extra turns**: If the acting unit triggered a Limit Break, their
   icon is re-inserted at the appropriate position in the remaining bar.
7. **Round ends**: When no icons remain, start a new round (go to step 2).

Effective SPD = base SPD + equipment + awakening bonuses + passive abilities +
temporary buffs/debuffs. SPD changes are **always immediate** — the bar reorders
mid-round whenever any combatant's SPD changes.

#### 2. Tie-Breaking Priority

When two or more combatants have the same effective SPD:

| Priority | Rule |
|----------|------|
| 1st | **Bosses** act first (boss flag on enemy data) |
| 2nd | **Player allies** act before non-boss enemies |
| 3rd | Among allies, **party slot order** (slot 1 before slot 2) |
| 4th | Among enemies of same priority, **enemy slot order** |

This means the player has a slight advantage on ties (except vs. bosses), which
feels fair and rewards matching enemy SPD.

#### 3. Limit Break (Extra Turn)

Extra turns are **always triggered by abilities or traits**, never passively by
having high SPD. However, some Limit Break abilities **scale with SPD**:

**Trigger types:**

| Type | Example | SPD Role |
|------|---------|----------|
| **Conditional ability** | "Golpe Relámpago: Attack. If user SPD > target SPD, gain an extra turn." | SPD is the condition |
| **Probability trait** | "Velocidad Pirata: If this unit acts first in the round, 30% chance of extra turn." | Position (determined by SPD) is the condition |
| **Kill trigger** | "Ejecución: If this ability kills the target, gain an extra turn." | SPD irrelevant (anyone can trigger) |
| **Buff ability** | "Vientos a Favor: Grant target ally an extra turn this round." | SPD irrelevant (support ability) |

**Rules for extra turns:**
- A unit can gain **at most 1 extra turn per round** (no infinite chains).
- The extra turn is inserted **after the current turn** in the bar (the unit acts
  again as the next icon, before other remaining combatants).
- The extra turn uses the unit's normal action (choose ability, attack, etc.) — it
  is a full turn, not a free action.
- Limit Break extra turns are **not affected by CC immunity**. If the unit has a
  Limit Break pending but gets stunned before it resolves, the stun takes priority
  and the Limit Break is lost.

#### 4. Status Effects and the Initiative Bar

Status effects from the Damage & Stats Engine interact with the bar:

| Effect | Impact on Initiative Bar |
|--------|------------------------|
| **Aturdimiento** (1 turn) | Unit's icon is **removed from the bar without acting**. They skip their turn this round. CC immunity granted for 1 turn afterward (next round, they cannot be stunned). |
| **Sueño** (until damaged) | Unit's icon is **removed without acting** each round until they take damage. Damage from any source wakes them. CC immunity granted for 1 turn after waking. |
| **SPD buff/debuff** | Takes effect **immediately**. Remaining unacted icons are resorted by current effective SPD. A slow unit that gets a SPD buff can jump ahead of units that haven't acted yet. |
| **Death/KO** | Unit's icon is **immediately removed** from the bar. They do not act. |
| **Revive** | Revived unit is placed at the **end of the current round's bar** (acts last this round). Next round, they are positioned normally by SPD. |

#### 5. Naval Combat Adaptation

In naval combat, the initiative bar works identically but with **ships** instead
of units:

- Each ship (allied + enemy) has one icon on the bar.
- Ship SPD = base Sail Speed + upgrade bonuses + crew contributions (defined in SDM).
- Ships are **immune to Aturdimiento and Sueño** (per Damage & Stats Engine).
- Limit Break abilities exist for ships (e.g., crew-triggered abilities that grant
  the ship an extra action).
- Tie-breaking rules apply the same way (boss ships first, then player ship, etc.).

#### 6. Pre-Combat Phase (First Round)

Before the first round, the following are applied in order:

1. Equipment stat bonuses (always active)
2. Passive abilities (e.g., "Battle Start: +20% SPD for 3 rounds")
3. Pre-combat items (if the game supports consumable items before combat)
4. Trait synergy bonuses that affect SPD

The first round's ordering already includes all these modifiers. There is no
"raw stats only" round.

### States and Transitions

**Initiative Bar states per combatant:**

| State | Description | Bar Behavior |
|-------|-------------|-------------|
| **Queued** | Waiting for their turn this round | Icon visible on bar |
| **Active** | Currently taking their turn | Icon highlighted/pulsing |
| **Acted** | Has completed their turn this round | Icon removed from bar |
| **Skipped (CC)** | Stunned or asleep — turn forfeited | Icon removed with "skip" visual effect |
| **Dead** | KO'd during the round | Icon removed immediately |
| **Limit Break** | Extra turn granted | Icon re-inserted into bar after current turn |

**Round lifecycle:**

```
[Round Start] → Calculate SPD → Populate Bar →
→ [Next unit acts] → Remove icon →
  → (SPD changed?) → Resort remaining icons
  → (Limit Break?) → Re-insert icon
→ (if bar empty) → [Round End] → [Next Round Start]
→ (if bar not empty) → [Next unit acts] (loop)
```

### Interactions with Other Systems

| System | Direction | Interface |
|--------|-----------|-----------|
| **Unit Data Model** (#1) | UDM → Initiative | Reads effective SPD (base + growth + awakening + equipment) for each unit to calculate round order. |
| **Ship Data Model** (#2) | SDM → Initiative | Reads ship effective SPD (base + upgrades + crew contributions) for naval combat order. |
| **Damage & Stats Engine** (#3) | DSE → Initiative | DSE applies status effects (Aturdimiento, Sueño) that cause turn skips. DSE applies SPD buffs/debuffs that trigger immediate bar reorder. |
| **Combate Terrestre** (#10) | Initiative → Combat | Provides the turn order. Combat system asks "who acts next?" and Initiative Bar responds. Combat triggers abilities that may grant Limit Breaks. |
| **Combate Naval** (#12) | Initiative → Combat | Same interface as land, but with ships. |
| **Combat UI** (#14) | Initiative → UI | Provides the current bar state (icon list, positions, active unit, Limit Break indicators) for visual rendering. |
| **Auto-Battle** (#20) | Initiative → Auto | Auto-battle reads the turn order to decide optimal ability usage. |

## Formulas

### 1. Turn Order Calculation

```
EffectiveSPD = BaseSPD + EquipmentSPD + AwakeningBonus + BuffTotal + PassiveTotal
```

Where `BuffTotal` = sum of all active SPD buff/debuff values (capped at ±100% of
base per Damage & Stats Engine buff cap rules).

**Sorting**: `sort(combatants, by: EffectiveSPD, descending)` with tie-breaking
priority as defined in Core Rules §2.

### 2. Limit Break — SPD Conditional

For abilities with "if user SPD > target SPD" conditions:

```
LimitBreakTriggered = (attacker.EffectiveSPD > target.EffectiveSPD)
```

No formula — it is a binary comparison at the moment the ability resolves.

### 3. Limit Break — Probability Trait

For traits with "X% chance of extra turn":

```
if random(0, 100) < TRAIT_EXTRA_TURN_CHANCE:
    grant extra turn
```

| Variable | Value | Description |
|----------|-------|-------------|
| `TRAIT_EXTRA_TURN_CHANCE` | 30% | Base probability for SPD-related traits. Defined per trait. |

### 4. Bar Position After Reorder

When the bar reorders mid-round, only **unacted combatants** are resorted:

```
remainingIcons = bar.filter(state == Queued)
remainingIcons.sort(by: EffectiveSPD, descending, tieBreak: priority)
bar = remainingIcons
```

Already-acted and already-skipped combatants are not re-added.

### 5. Round Counter

```
RoundNumber += 1 (on each Round Start)
```

Used by: duration-based buffs/debuffs ("lasts 3 rounds"), UI display ("Round 5"),
and potentially difficulty scaling in future systems.

## Edge Cases

| Situation | Resolution |
|-----------|------------|
| All allies have the same SPD | Party slot order determines sequence (slot 1 first). Predictable for the player. |
| SPD buff moves a unit ahead of the currently-active unit | No effect — the active unit completes their turn. The reorder applies to **queued** icons only. |
| SPD debuff drops a unit to 0 SPD | Unit acts **last** this round (and every round while at 0 SPD) but still gets a turn. SPD 0 ≠ skip. |
| A unit gains a Limit Break but is the last icon on the bar | Their extra turn still happens — they are re-inserted as the sole remaining icon and act immediately. Then the round ends. |
| A unit gains a Limit Break AND another unit also gains one in the same round | Each resolves independently. Max 1 extra turn per unit per round, but multiple units can each get 1 extra turn. |
| An ability buffs SPD of a unit that already acted this round | The buff is applied (stored on the unit) but has no effect on bar position this round — the unit already acted. It will affect next round's ordering. |
| A unit is revived and a Limit Break is triggered in the same turn | Revived unit goes to end of bar. Limit Break unit goes after current turn. Both are queued independently. |
| All enemies die before all allies act | Round ends immediately (victory). Remaining ally turns are not executed. |
| All allies die before all enemies act | Round ends immediately (defeat). Remaining enemy turns are not executed. |
| A stunned unit receives a SPD buff | The buff is applied but the stun still skips their turn. Next round they benefit from the SPD buff. |
| An ability grants an extra turn to a dead unit | No effect — dead units cannot receive extra turns. The Limit Break is wasted. |
| SPD tie between a boss and a player unit | Boss acts first (tie-breaking rule §2). |

## Dependencies

### Upstream Dependencies

| System | Dependency Type | Interface |
|--------|----------------|-----------|
| **Unit Data Model** (#1) | Hard | Reads SPD stat (base + growth + awakening) for land combat ordering. |
| **Ship Data Model** (#2) | Hard | Reads ship effective SPD for naval combat ordering. |
| **Damage & Stats Engine** (#3) | Hard | Provides SPD buff/debuff values and CC status effects (Aturdimiento, Sueño) that interact with the bar. |

### Downstream Dependents

| System | Dependency Type | Interface |
|--------|----------------|-----------|
| **Combate Terrestre** (#10) | Hard | Consumes turn order to determine who acts next. |
| **Combate Naval** (#12) | Hard | Same interface, ships instead of units. |
| **Combat UI** (#14) | Hard | Renders the bar visually from Initiative Bar's state. |
| **Auto-Battle** (#20) | Soft | Reads turn order for AI decision-making. |

## Tuning Knobs

| Knob | Current Value | Range | What It Affects |
|------|--------------|-------|----------------|
| `MAX_EXTRA_TURNS_PER_UNIT` | 1 | 1-2 | Maximum Limit Break extra turns per unit per round. 2 would be very powerful — only increase with extreme caution. |
| `TRAIT_EXTRA_TURN_CHANCE` | 30% | 10-50% | Base probability for SPD-related extra turn traits. Too high = extra turns feel routine. Too low = not worth building around. |
| `VISIBLE_FUTURE_ICONS` | 8 | 5-12 | How many icons the bar displays. In practice, limited by combatant count (max ~11 in land: 6 allies + 5 enemies). |
| `REORDER_ANIMATION_DURATION` | 300ms | 100-500ms | How long the bar reorder animation takes. Must be visible enough to notice but not slow enough to disrupt flow. |
| `CC_IMMUNITY_DURATION` | 1 turn | 1-2 turns | How long CC immunity lasts after stun/wake. At 2 turns, stun-based strategies become very weak. |

### Knob Interactions

| Knob A | Knob B | Interaction |
|--------|--------|-------------|
| `MAX_EXTRA_TURNS_PER_UNIT` | `TRAIT_EXTRA_TURN_CHANCE` | If both are high, fast units dominate. A 50% chance of 2 extra turns would be broken. |
| `CC_IMMUNITY_DURATION` | Aturdimiento design (DSE) | Longer immunity makes stun less effective, shifting meta toward damage over CC. |
| SPD stat range (UDM) | Tie-breaking rules | If SPD range is narrow (55-80 base), ties happen often. If wide, ties are rare and tie rules matter less. |

## Visual/Audio Requirements

### Visual

- **Bar background**: Horizontal wooden plank / rope bridge aesthetic. Positioned
  below the HP bars, above the battlefield.
- **Unit icons**: Chibi portrait circles (same as unit card thumbnails). Ally icons
  have blue border, enemy icons have red border. Boss icons are slightly larger
  with a gold crown indicator.
- **Active unit**: Icon pulses/glows and is slightly enlarged. A small arrow or
  anchor marker points down to the acting unit on the battlefield.
- **Limit Break insertion**: Icon appears with a flash/lightning effect and slides
  into position. Distinct from normal reorder animation.
- **Reorder animation**: Icons smoothly slide to their new positions (not instant
  snap). Duration = `REORDER_ANIMATION_DURATION`.
- **Stun/Sleep skip**: Icon grays out, shakes, and fades away with a chain/zzz
  visual effect.
- **Round transition**: Brief visual pulse across the bar (wave of light left to
  right) when a new round begins and icons repopulate.

### Audio

- **Turn start SFX**: Subtle "tick" when the next unit becomes active.
- **Limit Break SFX**: Dramatic sting (sword clash / cannon boom) when an extra
  turn is inserted. Must feel impactful and rewarding.
- **Reorder SFX**: Quick "shuffle" sound when icons shift positions mid-round.
- **Stun skip SFX**: Chain rattle (matches stun visual).
- **Round start SFX**: Soft bell or drum beat marking the new round.

## UI Requirements

- **Position**: Top of combat screen, below HP/status area, above battlefield.
  Full width of the screen.
- **Icon size**: ~40×40px per icon. Must be readable on mobile (identifiable at
  a glance). When combatant count exceeds available space, icons compress
  proportionally (no scrolling — instant readability is critical).
- **Capacity**: Show all combatants in the current round (max ~11 in land combat,
  fewer in naval).
- **Round counter**: Small "Round X" label at the left edge or above the bar.
- **Limit Break indicator**: When a unit's extra turn is queued, show a small
  lightning bolt badge on their icon.
- **Long press**: Long-pressing an icon on the bar shows the unit's name, current
  SPD, and active status effects in a tooltip popup.
- **Auto-battle mode**: Bar is still visible but not interactive (player cannot
  pause to read it in auto-mode, but can see the flow).

## Acceptance Criteria

### Turn Order Validation

- [ ] Units are sorted by effective SPD (highest first) at round start
- [ ] Tie-breaking follows priority: boss > ally > normal enemy > slot order
- [ ] Round recalculates correctly after all icons are removed

### Mid-Round Reorder

- [ ] SPD buff on a queued unit immediately resorts remaining icons
- [ ] SPD buff on an already-acted unit does NOT affect current round bar
- [ ] Bar animation shows icons sliding to new positions during reorder

### Limit Break

- [ ] Ability-triggered extra turn inserts icon after current turn
- [ ] Max 1 extra turn per unit per round is enforced
- [ ] Limit Break on a dead unit has no effect
- [ ] Limit Break on a stunned unit is lost (stun takes priority)

### Status Effects

- [ ] Aturdimiento removes icon without acting, grants CC immunity
- [ ] Sueño removes icon each round until damage received
- [ ] CC immunity prevents stun/sleep for exactly `CC_IMMUNITY_DURATION` turns
- [ ] Death immediately removes icon from bar

### Naval Combat

- [ ] Ships use ship SPD, not individual unit SPD
- [ ] Ships are immune to Aturdimiento and Sueño on the bar
- [ ] Limit Break works for ships via crew-triggered abilities

### Performance

- [ ] Bar reorder completes within `REORDER_ANIMATION_DURATION`
- [ ] Turn order calculation takes < 1ms (no perceivable delay)

## Open Questions

| Question | Owner | Target Resolution |
|----------|-------|-------------------|
| Exact visual layout of the combat screen (bar position relative to HP bars, battlefield, and ability buttons) | UX Designer | Combat UI GDD |
| Which specific Limit Break abilities will exist in the demo? | Game Designer | Combate Terrestre / ability data |

### Resolved During Design

| Question | Resolution |
|----------|-----------|
| Bar orientation | Horizontal, top of combat screen |
| CTB vs Round-based | Round-based: each unit acts once per round, bar recalculates between rounds |
| SPD buff/debuff timing | Immediate — bar reorders mid-round when SPD changes |
| Limit Break trigger | Ability-triggered + SPD scaling (never passive/automatic) |
| Tie-breaking | Bosses first, then allies, then normal enemies, then slot order |
| Limit Break cap | Max 1 extra turn per unit per round |
| Next round preview | No — too imprecise with mid-round reorders, adds visual clutter |
| Can enemies Limit Break? | Yes — especially bosses, to create tension and unpredictability |
| 11+ combatant bar overflow | Icons compress proportionally (no scrolling) |
| Pre-combat consumable items | Deferred — not in demo |
| UDM open question ("limit break for SPD") | Resolved: ability-triggered extra turns, not passive SPD threshold |
