# Damage & Stats Engine

> **Status**: Approved
> **Author**: User + Claude Code Game Studios agents
> **Last Updated**: 2026-03-26
> **Implements Pillar**: Pillar 1 (Profundidad Estratégica Dual)

## Overview

The Damage & Stats Engine is the mathematical backbone of all combat in the game.
It is an invisible system — players never interact with it directly, but every
damage number, heal tick, buff icon, and status effect they see is produced by it.

The engine defines: how stats translate into damage (physical ATK vs DEF, magical
MST vs SPR), how critical hits amplify damage (with uncapped CRI scaling), how
7 pirate-themed elements (Pólvora, Tormenta, Maldición, Bestia, Acero + Luz/Sombra)
create advantage/disadvantage matchups, how buffs and debuffs modify stats
(temporary and permanent, stackable, with ±100% cap), how status effects alter
combat flow (Veneno, Aturdimiento, Ceguera, Silencio, Sangrado, Sueño, Muerte),
how healing restores HP, and how dispels remove active effects.

The engine serves both land and naval combat with a shared formula core. Land
combat uses unit stats directly (ATK, DEF, MST, SPR); naval combat uses ship
effective stats (FPW, HDF, MST, RSL) which are derived from ship base + upgrades
+ crew contributions (defined in Ship Data Model). The formulas are structurally
identical — only the stat names change — ensuring consistent feel across both modes.

This system has no upstream dependencies but is consumed by every combat-related
system: Combate Terrestre, Combate Naval, Enemy System, Initiative Bar, Traits/
Sinergias, Auto-Battle, and Combat UI.

## Player Fantasy

**"Every number tells a story."** The Damage & Stats Engine is invisible
infrastructure — the player never thinks about it directly. Its success is
measured by what the player *feels* through it:

**The thrill of a critical chain**: A high-CRI unit lands a critical hit and
the damage number explodes across the screen. The player who invested in CRI
feels validated — their build choice mattered.

**The satisfaction of elemental mastery**: Bringing a Tormenta team against
Pólvora enemies and watching the damage multipliers stack. The player feels
smart for reading the stage and preparing the right composition.

**The tension of status effects**: The enemy captain casts Sueño on your
healer. Do you use a dispel turn, or push through with damage? Every status
effect should create a micro-decision that keeps combat engaging.

**The fairness of scaling**: A new player deals small but meaningful damage.
A late-game player deals massive damage but faces proportionally tougher
enemies. At every stage, the player should feel like their stats *matter* —
never like damage is random or arbitrary.

The system fails if damage feels random (no clear relationship between stats
and output), if elements are ignorable (no reason to plan compositions), if
buffs/debuffs are either overpowered (mandatory) or irrelevant (ignorable),
or if naval combat feels mathematically disconnected from land combat.

## Detailed Design

### Core Rules

#### 1. Damage Formula (Land Combat)

Base formula for physical and magical damage:

**Step 1 — Apply buffs/debuffs to stats:**
```
EffectiveATK = ATK × clamp(1.0 + sum(atk_buffs) - sum(atk_debuffs), 0.0, 2.0)
EffectiveDEF = DEF × clamp(1.0 + sum(def_buffs) - sum(def_debuffs), 0.0, 2.0)
```

Buffs modify stats **before** the damage formula. Each stat has its own buff/debuff
stack with the ±100% cap (see §4). This means both attacker offensive buffs AND
defender defensive buffs are accounted for.

**Step 2 — Calculate raw damage:**
```
RawDamage = (EffectiveATK × ATTACK_MULTIPLIER) - (EffectiveDEF × DEFENSE_MULTIPLIER)
RawDamage = max(RawDamage, 1)   // minimum damage is always 1
```

- Physical: ATK vs DEF
- Magical: MST vs SPR

**Step 3 — Apply modifier chain:**
```
FinalDamage = floor(RawDamage × AbilityPower × ElementMod × CritMod × Variance)
FinalDamage = max(FinalDamage, 1)
```

| Modifier | Description | Typical Range |
|----------|-------------|---------------|
| AbilityPower | Ability multiplier (basic attack = 1.0) | 0.5 – 5.0 |
| ElementMod | Elemental advantage/disadvantage | 0.75 / 1.0 / 1.25 |
| CritMod | 1.0 if not critical; 1.5+ if critical | 1.0 – 2.0+ |
| Variance | Random variation so numbers are not always identical | 0.95 – 1.05 |

#### 2. Damage Formula (Naval Combat)

Structurally identical to land combat, only stat names change:

- Physical naval: AttackStat = FPW, DefenseStat = HDF
- Magical naval: AttackStat = MST, DefenseStat = RSL

Same buff-then-formula process and modifiers (AbilityPower, ElementMod, CritMod, Variance) apply.
This ensures the player learns one system and applies it to both modes.

**Ships do not critical hit.** CritMod = 1.0 always in naval combat. This keeps
naval balance simpler and creates a meaningful difference between modes: in land,
investing in CRI matters; in naval, crew composition and stat optimization matter.

#### 3. Elemental System (7 Elements)

**Element assignment model:**
- **Offensive element** = defined per ability (field `Element` in ability data).
  Determines what type of damage the attack deals.
- **Defensive element** = defined per unit/enemy/ship (field `Element` in
  CharacterData and ShipData). Determines what damage types the entity is
  weak/resistant to.
- The basic attack is always **Neutral** (ElementMod = 1.0 regardless of target).
  This may change in future development.
- ElementMod is calculated: **ability element** vs. **target's defensive element**.
- `Element` field added to CharacterData (UDM) and ShipData (SDM) —
  cross-system update applied.

**Pentagonal advantage cycle (5 elements):**

```
Pólvora → Acero → Bestia → Maldición → Tormenta → Pólvora
```

Each element deals bonus damage to the next and receives bonus damage from the
previous in the cycle.

| Element | Theme | Strong against → | Weak against → |
|---------|-------|-----------------|----------------|
| **Pólvora** | Explosions, cannons, fire | Acero | Tormenta |
| **Tormenta** | Lightning, water, tides | Pólvora | Maldición |
| **Maldición** | Voodoo, curses, dark magic | Tormenta | Bestia |
| **Bestia** | Sea creatures, monsters, kraken | Maldición | Acero |
| **Acero** | Swords, armor, naval engineering | Bestia | Pólvora |

**Dual pair (2 elements):**

```
Luz ↔ Sombra (strong against each other)
```

Both Luz and Sombra deal bonus damage to each other. The 5 pentagonal elements
are neutral against Luz and Sombra.

**Element modifiers:**

| Matchup | ElementMod | Effect |
|---------|-----------|--------|
| Advantage (ability strong vs. target) | 1.25 | +25% damage |
| Neutral | 1.00 | No modifier |
| Disadvantage (ability weak vs. target) | 0.75 | -25% damage |
| Luz vs. Sombra (both directions) | 1.25 | +25% damage mutual |
| Same element (ability = target) | 1.00 | Neutral |
| Neutral ability (basic attack) | 1.00 | Always neutral |

#### 4. Buff/Debuff System

**Modifier types:**

| Type | Duration | Example |
|------|----------|---------|
| **Temporary** | N turns (decremented at the start of the affected unit's turn) | "ATK +30% for 3 turns" |
| **Permanent** | Until dispelled or until combat ends | "DEF +20% (trait passive)" |

**Stacking rules:**
- Buffs to the same stat **stack additively**: +20% ATK + +15% ATK = +35% ATK
- Each stat has its own independent buff/debuff stack
- **Cap of ±100% per stat**: a stat cannot increase beyond double (+100%) or
  decrease below 0 (-100%) from buffs/debuffs combined. This cap may be removed
  in future development once balance is validated.
- Buffs modify stats **before** the damage formula (see §1). Both attacker
  offensive buffs and defender defensive buffs are applied to their respective stats.

```
StatModifier(stat) = clamp(1.0 + sum(stat_buffs) - sum(stat_debuffs), 0.0, 2.0)
EffectiveStat = BaseStat × StatModifier(stat)
```

**Dispel system:**
- **Offensive dispel**: Removes all temporary buffs from the target. Permanent
  buffs cannot be dispelled.
- **Defensive dispel (Purify)**: Removes all temporary debuffs and status effects
  from the ally.
- Abilities can specify: selective dispel (only 1 buff/debuff), total dispel, or
  type-specific dispel (e.g., remove only DoTs).

#### 5. Critical Hit System

```
If random(0, 100) < EffectiveCRI:
  CritMod = CRIT_BASE_MULTIPLIER + CritBonusDamage
Else:
  CritMod = 1.0
```

- `CRIT_BASE_MULTIPLIER` = 1.50 (base critical = +50% damage)
- `EffectiveCRI` = CRI base + equipment + traits + buffs (**no cap**)
- When EffectiveCRI ≥ 100%, the unit always crits. Excess CRI converts to bonus
  critical damage:

```
CritBonusDamage = floor((EffectiveCRI - 100) / CRIT_OVERFLOW_DIVISOR) × 0.01
```

- `CRIT_OVERFLOW_DIVISOR` = 50 (every 50 CRI above 100 = +1% extra crit damage)
- Example: CRI 200% → always crits, CritMod = 1.50 + 0.02 = 1.52
- Example: CRI 350% → always crits, CritMod = 1.50 + 0.05 = 1.55

This creates diminishing returns above 100% — worth investing in but not
degenerate.

**Cross-system update required**: The Unit Data Model currently specifies a CRI
hard cap at 50%. This must be updated to remove the cap.

**Ships**: CritMod = 1.0 always (ships do not critical hit).

#### 6. Status Effects

| Effect | Type | Duration | Mechanic | Stackable |
|--------|------|----------|----------|-----------|
| **Veneno** | DoT | 3 turns | Loses X% of max HP at the **end** of its turn. Slow, inevitable damage. | No (refreshes duration) |
| **Sangrado** | DoT | 3 turns | Loses X% of max HP at the **start** of its turn (before acting). Aggressive — can kill before the unit acts. Bestia-themed. | No (refreshes duration) |
| **Quemadura** | DoT | 3 turns | Loses X% of max HP **after performing an action** (Ataque Normal, Habilidad, or Guardia). If the unit is CC'd or passes its turn, Quemadura does NOT tick. Pólvora-themed. See Combate Terrestre GDD §2 for action classification. | No (refreshes duration) |
| **Aturdimiento** | CC | 1 turn | Loses next turn completely | No |
| **Sueño** | CC | 2 turns | Loses turns until receiving damage (damage wakes them up) | No |
| **Ceguera** | Debuff | 2 turns | Physical attacks have 50% chance to miss (MISS, damage = 0). Magical abilities unaffected | No (refreshes) |
| **Silencio** | Debuff | 2 turns | Cannot use abilities (basic attack only) | No (refreshes) |
| **Muerte** | Threshold | Instant | If target's HP is below X%, the target is executed instantly. No effect if above threshold. | N/A |

**DoT stacking**: Veneno, Sangrado, and Quemadura are different effects and
**stack with each other**. A single DoT type does not stack with itself (refreshes
duration instead). All three can be active simultaneously on the same target.

**DoT timing summary**:
- **Sangrado** → start of turn (before action)
- **Quemadura** → after ability use (skipped if CC'd or turn passed)
- **Veneno** → end of turn (after action)

**CC immunity**: After being stunned or woken from Sueño, the unit gains
**1 turn of CC immunity** to prevent stun-locks.

**Muerte**: Does not work against bosses (execution immunity). The threshold X%
is defined per ability (e.g., "execute if HP < 30%").

**Status effects in naval**: Ships can receive status effects (Quemadura on the
hull, Maldición on the crew), but ships are **immune to Sueño and Aturdimiento**
(a ship doesn't fall asleep or get stunned). Muerte does not apply to ships.

**Naval CritMod**: Ships do not critical hit. CritMod = 1.0 always in naval combat.

**Naval DoT split**: In naval combat, DoTs target different parts of the ship:
- **Quemadura** → damages ship HHP (fire on hull). Triggers after action, same rule as terrestre.
- **Veneno** → damages HP of 1 random living crew member (end of turn). If no crew alive, no damage.
- **Sangrado** → damages HP of 1 random living crew member (start of turn). If no crew alive, no damage.
- Naval DoT damage = `NAVAL_DOT_CREW_PERCENT` × crew member's max HP (see Combate Naval GDD §Tuning Knobs).
- Quemadura damage to hull uses standard `DOT_PERCENT` × ship MaxHHP.

#### 7. Healing

```
HealAmount = floor(HealerStat × HealPower × BuffMod)
```

- `HealerStat` = healer's MST (in naval: effective MST of the ship, as defined
  in Combate Naval GDD §5)
- `HealPower` = ability heal multiplier (e.g., 2.0 for a strong heal)
- `BuffMod` = buffs/debuffs affecting healing output
- Healing cannot exceed MaxHP (clamp, as defined in UDM)
- Healing does not critical hit (simplifies healer balance)
- No elemental advantage/disadvantage on healing

### States and Transitions

The Damage & Stats Engine is a calculation engine, not a stateful entity. It does
not have its own lifecycle states. However, it manages **runtime combat state**
for each combatant (unit or ship) during an encounter:

**Per-combatant runtime state:**

| Field | Type | Description |
|-------|------|-------------|
| CurrentHP | int | Current health (starts at MaxHP, clamped to 0–MaxHP) |
| CurrentMP | int | Current mana (starts at MaxMP, clamped to 0–MaxMP) |
| ActiveBuffs | List\<BuffInstance\> | Active buffs with remaining duration |
| ActiveDebuffs | List\<DebuffInstance\> | Active debuffs with remaining duration |
| ActiveStatusEffects | List\<StatusInstance\> | Active status effects with remaining duration |
| CCImmunityTurns | int | Remaining turns of CC immunity (0 = vulnerable) |
| IsKO | bool | True if CurrentHP = 0 |

**BuffInstance / DebuffInstance:**

| Field | Type | Description |
|-------|------|-------------|
| StatAffected | enum | Which stat is modified |
| Percentage | float | Magnitude (e.g., +30% or -20%) |
| RemainingTurns | int | Turns left (-1 = permanent) |
| Source | AbilityId | Which ability applied this (for UI display) |
| IsDispellable | bool | Whether dispel can remove it |

**Turn processing order** (per combatant):
1. Decrement buff/debuff durations → remove expired
2. Decrement CC immunity
3. Apply **Sangrado** DoT (start of turn — before acting)
4. Check Sueño/Aturdimiento → skip turn if CC'd (go to step 7)
5. Unit acts (selects and uses ability, or passes turn)
6. Apply **Quemadura** DoT (after ability use — skipped if CC'd or turn passed)
7. Apply **Veneno** DoT (end of turn — **always** ticks if active, even if CC'd)
8. Turn ends

This state is **runtime only** — it is created when combat starts and destroyed
when combat ends. Nothing persists to the save file.

### Interactions with Other Systems

| System | Direction | Data Interface |
|--------|-----------|----------------|
| **Unit Data Model** | UDM → DSE | Reads all core stats (HP, MP, ATK, DEF, MST, SPR, SPD), secondary stats (CRI, LCK), and defensive Element for damage calculations |
| **Ship Data Model** | SDM → DSE | Reads effective ship stats (HHP, FPW, HDF, MST, RSL) and defensive Element for naval damage calculations |
| **Combate Terrestre** | DSE → CT | Provides damage calculation, healing, buff/debuff application, status effect processing, and turn processing order. CT calls DSE functions each turn. |
| **Combate Naval** | DSE → CN | Same interface as Combate Terrestre but using naval stat names. CN calls DSE with ship stats instead of unit stats. |
| **Enemy System** | DSE → Enemy | Enemies use the same formulas. Enemy stats and Element feed into DSE the same way player units do. |
| **Traits/Sinergias** | Traits → DSE | Trait synergy bonuses are applied as permanent buffs through the buff system. Traits system calculates bonuses; DSE applies them to combat state. |
| **Initiative Bar** | Indirect | DSE does not directly interact with Initiative Bar, but status effects (Aturdimiento, Sueño) cause turn skips which the Initiative Bar must handle. |
| **Equipment System** | Equipment → DSE | Equipment stat bonuses are baked into unit stats before DSE reads them. Equipment-granted abilities have their own Element and AbilityPower. |
| **Combat UI** | DSE → UI | Provides all display data: damage numbers, heal numbers, buff/debuff icons with durations, status effect indicators, HP/MP bars. |
| **Auto-Battle** | DSE → Auto | Auto-Battle AI reads available abilities and their expected damage (via DSE formulas) to make ability selection decisions. |

**Interface ownership**: The DSE owns all combat math. Consuming systems provide
inputs (stats, ability data) and receive outputs (damage dealt, healing done,
effects applied). No consuming system may override or bypass DSE calculations.

**Shared formula core**: The DSE exposes a single calculation interface that works
for both land and naval. The caller specifies which stats to use; the DSE doesn't
know or care whether it's calculating land or naval damage.

## Formulas

### Master Damage Formula

```
EffectiveATK = ATK × clamp(1.0 + sum(atk_buffs) - sum(atk_debuffs), 0.0, 2.0)
EffectiveDEF = DEF × clamp(1.0 + sum(def_buffs) - sum(def_debuffs), 0.0, 2.0)
RawDamage = max((EffectiveATK × ATTACK_MULTIPLIER) - (EffectiveDEF × DEFENSE_MULTIPLIER), 1)
FinalDamage = max(floor(RawDamage × AbilityPower × ElementMod × CritMod × Variance), 1)
```

### Variable Definitions

| Variable | Type | Default | Range | Description |
|----------|------|---------|-------|-------------|
| ATTACK_MULTIPLIER | float | 1.8 | 1.5 – 2.5 | Scales offensive stat contribution |
| DEFENSE_MULTIPLIER | float | 1.0 | 0.5 – 1.5 | Scales defensive stat reduction |
| AbilityPower | float | 1.0 | 0.5 – 5.0 | Per-ability damage multiplier (basic attack = 1.0) |
| ElementMod | float | 1.0 | 0.75 / 1.0 / 1.25 | Elemental advantage/disadvantage |
| CritMod | float | 1.0 | 1.0 – 2.0+ | Critical hit multiplier |
| CRIT_BASE_MULTIPLIER | float | 1.50 | 1.25 – 2.0 | Base crit damage bonus |
| CRIT_OVERFLOW_DIVISOR | float | 50 | 25 – 100 | CRI overflow → crit damage conversion rate |
| Variance | float | 1.0 | 0.95 – 1.05 | Random damage spread (±5%) |
| StatModifier | float | 1.0 | 0.0 – 2.0 | Per-stat buff/debuff multiplier (applied before formula) |
| DOT_PERCENT | float | 5% | 2% – 10% | % of max HP lost per DoT tick |
| MUERTE_THRESHOLD | float | 30% | 10% – 50% | HP threshold for execution (per ability) |

### Why ATTACK_MULTIPLIER > DEFENSE_MULTIPLIER?

The ratio ATK×1.8 vs DEF×1.0 ensures **offense wins over defense** at equal stats.
This is intentional for a gacha RPG:
- Players feel their investment in ATK/MST is rewarded with visible damage growth
- Fights don't stall into attrition wars (respects Pillar 4: Respeto al Tiempo)
- Tanks are valuable for *reducing* damage, not *negating* it

If DEF were equal or higher, high-DEF units would take 1 damage from everything,
making damage dealers feel useless.

### Worked Example: Early Game (Land)

```
3★ Unit (Lv 20) attacks 3★ Enemy (Lv 20)
ATK = 130, Enemy DEF = 110
Ability: "Slash" (AbilityPower 1.5, Element: Acero)
Enemy Element: Bestia (Acero is strong against Bestia)
CRI = 12%, no buffs active

RawDamage = max(130 × 1.8 - 110 × 1.0, 1) = max(234 - 110, 1) = 124
ElementMod = 1.25 (advantage)
CritMod = 1.0 (12% chance, assume no crit)
BuffMod = 1.0
Variance = 1.02 (random)

FinalDamage = floor(124 × 1.5 × 1.25 × 1.0 × 1.0 × 1.02)
            = floor(237.15)
            = 237

If critical hit:
FinalDamage = floor(124 × 1.5 × 1.25 × 1.5 × 1.0 × 1.02)
            = floor(355.73)
            = 355
```

### Worked Example: Late Game (Land)

```
5★ Unit (Lv 80, awakened) attacks Boss
ATK = 850, Boss DEF = 600, ATK buff +30% active
Ability: "Cañón Infernal" (AbilityPower 3.5, Element: Pólvora)
Boss Element: Acero (Pólvora strong)
CRI = 45%

EffectiveATK = 850 × clamp(1.0 + 0.30, 0.0, 2.0) = 850 × 1.3 = 1,105
EffectiveDEF = 600 × 1.0 = 600 (no buffs)
RawDamage = max(1105 × 1.8 - 600 × 1.0, 1) = max(1989 - 600, 1) = 1,389
ElementMod = 1.25
CritMod = 1.5 (assume crit)
Variance = 0.98

FinalDamage = floor(1389 × 3.5 × 1.25 × 1.5 × 0.98)
            = floor(8,933.0)
            = 8,933

Without crit:
FinalDamage = floor(1389 × 3.5 × 1.25 × 1.0 × 0.98)
            = floor(5,955.3)
            = 5,955
```

### Worked Example: Naval Combat

```
Ship (FPW effective = 193, Cannons Lv 2) attacks Enemy Ship (HDF = 150)
Ability: "Broadside" (AbilityPower 2.0, Element: Pólvora)
Enemy Ship Element: Acero (advantage)
No buffs, no crit (ships don't crit)

RawDamage = max(193 × 1.8 - 150 × 1.0, 1) = max(347.4 - 150, 1) = 197
ElementMod = 1.25
CritMod = 1.0 (always)
BuffMod = 1.0
Variance = 1.03

FinalDamage = floor(197 × 2.0 × 1.25 × 1.0 × 1.0 × 1.03)
            = floor(507.3)
            = 507
```

### Scaling Analysis

| Scenario | ATK | DEF | Ability | FinalDamage (approx) |
|----------|-----|-----|---------|---------------------|
| Early (Lv 20, 3★, basic atk) | 130 | 110 | 1.0 | ~124 |
| Mid (Lv 40, 4★, mid ability) | 290 | 220 | 2.0 | ~616 |
| Late (Lv 80, 5★, strong ability) | 850 | 600 | 3.5 | ~5,955 |
| Late + crit + buff + element | 850 (buffed→1105) | 600 | 3.5 | ~8,933 |

Damage scales roughly **50-70x** from early to late game with all modifiers.
This is consistent with FFBE-style gacha RPG scaling where late-game units deal
orders of magnitude more damage than early ones.

## Edge Cases

| Edge Case | Resolution |
|-----------|------------|
| **ATK much lower than DEF (RawDamage ≤ 0)** | Clamped to 1. Every attack deals at least 1 damage. Even a weak unit contributes something. |
| **Multiple DoTs active simultaneously** | All tick at their respective timings. Sangrado (start), Quemadura (after action), Veneno (end). Max combined = 3 × DOT_PERCENT per turn if all conditions met. |
| **Sangrado kills a unit** | Unit is KO'd at start of their turn before acting. Death is processed immediately. |
| **Veneno kills a unit** | Unit is KO'd at end of their turn after acting. They get their action but die afterward. |
| **Quemadura kills a unit** | Unit is KO'd immediately after using their ability. The ability still resolves. |
| **Muerte on a unit at exactly X% HP** | Muerte triggers at **strictly below** threshold. At exactly 30%, Muerte does NOT trigger. |
| **Muerte on a boss** | No effect. Bosses have execution immunity. |
| **Buff cap reached (+100%) and another buff applied** | Buff is added to the list (for duration tracking and dispel), but effective BuffMod stays clamped at 2.0. If an earlier buff expires, the new one becomes active. |
| **Debuff pushes stat to 0** | Stat floors at 0 via BuffMod floor of 0.0. ATK at 0 = RawDamage = 1 (minimum). DEF at 0 = full damage taken. |
| **Dispel on a unit with no buffs/debuffs** | No effect. Ability is consumed (MP spent) but nothing is removed. |
| **Sueño + Sangrado interaction** | Sangrado ticks at start of turn (step 3) before Sueño check (step 4). The damage from Sangrado wakes the unit. Unit takes Sangrado damage, wakes up, then acts normally. |
| **Sueño + Veneno interaction** | Unit is CC'd at step 4, skips action. Veneno still ticks at end of turn (step 7). Veneno does NOT wake the unit (it ticks after the action phase). |
| **Sueño + Quemadura interaction** | Unit is CC'd, skips action. Quemadura does NOT tick (requires ability use). Sueño effectively suppresses Quemadura. |
| **Quemadura + pass turn** | If the player voluntarily passes their turn (no ability used), Quemadura does NOT tick. This is a valid tactical choice to avoid Quemadura damage at the cost of a turn. |
| **Aturdimiento applied to a unit with CC immunity** | No effect. The stun fails, ability still costs MP. CC immunity prevents the status, not the damage from the ability. |
| **Crit on a healing ability** | Healing does not crit. CritMod is not applied to healing calculations. |
| **Elemental advantage on Neutral ability** | ElementMod = 1.0 always for Neutral abilities, regardless of target element. |
| **Two opposing buffs on same stat (+30% ATK buff and -20% ATK debuff)** | Stack additively: net +10% ATK. StatModifier = 1.10, EffectiveATK = ATK × 1.10. |
| **Variance produces 0 damage (extremely low raw + variance 0.95)** | Final floor of max(FinalDamage, 1) ensures at least 1 damage. |
| **Ship receives Muerte** | No effect. Ships are immune to execution. |
| **Ship receives Sueño or Aturdimiento** | No effect. Ships are immune to CC. |
| **Ability with AbilityPower 0** | Deals 0 × everything = 0, clamped to 1. Edge case for utility abilities that shouldn't deal damage — these should use a non-damage ability type instead. |

## Dependencies

| Dependency | Direction | Why |
|-----------|-----------|-----|
| **Unit Data Model** | Upstream | Reads core stats (ATK, DEF, MST, SPR, HP, MP), secondary stats (CRI, LCK), and defensive Element. DSE needs UDM to have Element field added. |
| **Ship Data Model** | Upstream | Reads effective ship stats (FPW, HDF, MST, RSL, HHP) and defensive Element. DSE needs SDM to have Element field added. |
| **Combate Terrestre** | Downstream | Calls DSE for all damage, healing, buff, and status effect calculations in land combat. |
| **Combate Naval** | Downstream | Calls DSE for all damage, healing, buff, and status effect calculations in naval combat. |
| **Enemy System** | Downstream | Enemies use the same DSE formulas. Enemy stats and Element feed into DSE identically to player units. |
| **Traits/Sinergias** | Upstream (partial) | Trait synergy bonuses feed into DSE as permanent buffs. DSE applies them; Traits system calculates them. |
| **Initiative Bar** | Downstream (indirect) | CC effects (Aturdimiento, Sueño) cause turn skips that the Initiative Bar must handle. |
| **Equipment System** | Upstream (indirect) | Equipment stat bonuses are baked into unit stats before DSE reads them. Equipment abilities carry their own Element and AbilityPower. |
| **Combat UI** | Downstream | Reads DSE outputs for display: damage numbers, heal values, buff/debuff icons, status effect indicators. |
| **Auto-Battle** | Downstream | Reads DSE formulas to estimate expected damage for AI ability selection. |

**Critical path**: UDM and SDM must add `Element` field before DSE can be
implemented. This is a data model update, not a design dependency — the DSE
formulas are self-contained.

**No circular dependencies**: DSE is a pure calculation layer. It receives inputs
and returns outputs. It never calls back into the systems that call it.

## Tuning Knobs

| Knob | Current Value | Safe Range | Gameplay Effect |
|------|--------------|------------|-----------------|
| ATTACK_MULTIPLIER | 1.8 | 1.5 – 2.5 | Higher = offense dominates more. Too high and tanks are useless; too low and fights stall. |
| DEFENSE_MULTIPLIER | 1.0 | 0.5 – 1.5 | Higher = defense matters more. Too high and low-ATK units deal 1 damage always. |
| ATK/DEF ratio | 1.8:1.0 | — | The ratio matters more than individual values. Keep offense > defense for pacing. |
| CRIT_BASE_MULTIPLIER | 1.50 | 1.25 – 2.0 | Higher = crits more impactful. Too high and CRI becomes the only stat that matters. |
| CRIT_OVERFLOW_DIVISOR | 50 | 25 – 100 | Lower = overflow CRI more rewarding. Too low and stacking CRI past 100% is degenerate. |
| ElementMod (advantage) | 1.25 | 1.15 – 1.50 | Higher = elements matter more. Too high and wrong-element teams are unplayable. |
| ElementMod (disadvantage) | 0.75 | 0.50 – 0.85 | Lower = more punishment for wrong element. Should mirror advantage symmetrically. |
| BuffMod cap | ±100% | ±50% – ±150% | Higher = buffs more powerful. Too high and buff stacking becomes mandatory. |
| Variance range | ±5% | ±0% – ±10% | Higher = more random feeling. Too high and players can't predict outcomes. |
| DOT_PERCENT | 5% | 2% – 10% | Higher = DoTs more threatening. Too high and DoTs dominate over direct damage. |
| MUERTE_THRESHOLD (per ability) | 30% | 10% – 50% | Higher = easier executions. Too high and Muerte abilities trivialize bosses (if they weren't immune). |
| BLIND_MISS_CHANCE | 50% | 25% – 75% | Higher = Ceguera more punishing for physical attackers. Too high and physical units are unusable while blinded. |
| CC immunity duration | 1 turn | 1 – 3 turns | Higher = harder to chain CC. Too high and CC abilities feel wasted. |
| Sangrado/Veneno/Quemadura duration | 3 turns | 2 – 5 turns | Higher = more total DoT damage. Too high and one application is too much value. |
| Sueño duration | 2 turns | 1 – 3 turns | Higher = more punishing. Balanced by "wake on damage" mechanic. |

**Knob interactions (danger zones):**

| Knob A | Knob B | Interaction |
|--------|--------|-------------|
| ATTACK_MULTIPLIER | DEFENSE_MULTIPLIER | Ratio determines overall TTK (time to kill). Changing one without the other shifts balance. |
| CRIT_BASE_MULTIPLIER | CRI growth rates (UDM) | If CRI grows fast AND crit multiplier is high, damage variance becomes extreme. |
| ElementMod | Number of elements (7) | More elements × higher mod = harder to build "safe" teams. 7 elements at 1.25 is manageable. |
| DOT_PERCENT | DoT duration | Total DoT damage = % × turns. Increasing both compounds. Tune one at a time. |
| BuffMod cap | Trait synergy bonuses (Traits GDD) | Trait bonuses feed into BuffMod. If traits give large % AND cap is high, power ceiling explodes. |

## Visual/Audio Requirements

**Visual:**
- Damage numbers float above the target with color coding by element:
  - **Blanco**: daño neutro
  - **Rojo/naranja**: Pólvora (icono llama)
  - **Azul eléctrico**: Tormenta (icono rayo)
  - **Morado**: Maldición (icono calavera)
  - **Marrón**: Bestia (icono garra)
  - **Gris plateado**: Acero (icono espada)
  - **Blanco brillante (con glow)**: Luz (icono estrella)
  - **Negro/violeta oscuro**: Sombra (icono ojo)
- **Crítico**: color del elemento + **borde dorado** + tamaño 1.5x grande + estrella dorada
- **Curación**: verde + signo **"+"** delante del número
- **"MISS"**: gris tenue (Ceguera)
- **"IMMUNE"**: gris tenue (CC immunity, boss vs Muerte)
- Color always represents element; shape/border distinguishes type (crit, heal, miss)
- Ventaja elemental: breve flash del color del elemento sobre el objetivo + flecha ↑
- Desventaja elemental: número ligeramente más pequeño + flecha ↓
- DoT ticks: números más pequeños con icono del efecto (gota verde = Veneno, gota roja = Sangrado, llama = Quemadura)
- Status effect icons sobre la barra de vida del afectado con contador de turnos
- Buff/debuff: flechas verdes ↑ (buff) o rojas ↓ (debuff) junto al stat afectado al aplicarse

**Audio:**
- Hit SFX varía por tipo: físico (impacto metálico), mágico (whoosh arcano), elemental (SFX temático del elemento)
- Crítico: SFX base amplificado + crunch/impacto adicional
- DoT tick: SFX sutil por tipo (burbujeo = Veneno, desgarro = Sangrado, crepitar = Quemadura)
- Status applied: SFX distintivo por efecto (campanada ominosa = Sueño, cadenas = Aturdimiento, silbido = Silencio)
- Muerte (ejecución): SFX dramático cuando el umbral se activa
- Dispel: SFX de "cristal rompiéndose" al eliminar buffs/debuffs
- Curación: SFX suave y positivo (campanillas, brillo)

## UI Requirements

**Combat HUD (both modes):**
- HP bar over each unit/ship with color gradient (green > yellow > red by %)
- MP/MST bar below HP (blue for MP, orange for MST)
- Active status effect icons next to bars with remaining turn counter
- Buff icons (green ↑) and debuff icons (red ↓) with tooltip on hold/tap

**Damage Display:**
- Floating numbers with light physics (bounce + fade out in ~1 second)
- Criticals are 1.5x larger than normal numbers with golden border
- Multiple simultaneous numbers stagger vertically to avoid overlap

**Elemental Indicator:**
- Ability selection: element icon visible on each ability
- Target selection: advantage/disadvantage indicator (↑ green / ↓ red / — neutral) based on ability element vs. target's defensive element
- Pre-combat summary: enemy defensive elements visible (once discovered)

**Status Effect Panel:**
- Hold/tap on a status icon: tooltip with name, description, remaining turns, and source
- Visual separation between buffs (green border), debuffs (red border), and status effects (yellow border)

## Acceptance Criteria

| # | Criterion | How to Verify |
|---|-----------|---------------|
| 1 | Damage formula produces correct values for known inputs | Unit test: precalculated ATK/DEF/ability → expected damage. Test physical and magical. |
| 2 | Minimum damage is always 1 (never 0 or negative) | Unit test: ATK=1, DEF=9999 → FinalDamage = 1 |
| 3 | Elemental advantage/disadvantage applies correctly | Unit test: all 7 elements × 7 matchups = correct ElementMod per table |
| 4 | Pentagonal cycle is consistent (no contradictions) | Unit test: verify A→B→C→D→E→A cycle produces correct advantage chain |
| 5 | Luz/Sombra deal bonus damage to each other | Unit test: Luz vs Sombra = 1.25, Sombra vs Luz = 1.25 |
| 6 | Critical hits apply correct multiplier | Unit test: forced crit → FinalDamage = non-crit × 1.5 |
| 7 | CRI overflow produces diminishing bonus damage | Unit test: CRI 150% → CritMod 1.51, CRI 200% → 1.52, CRI 350% → 1.55 |
| 8 | Buffs stack additively within category | Unit test: +20% ATK + +15% ATK → BuffMod = 1.35 |
| 9 | BuffMod is capped at 0.0–2.0 | Unit test: +120% buff → BuffMod = 2.0, -120% debuff → BuffMod = 0.0 |
| 10 | Sangrado ticks at start of turn (before action) | Integration test: unit with Sangrado → HP reduced before ability selection |
| 11 | Quemadura ticks after ability use only | Integration test: CC'd unit with Quemadura → no tick. Acting unit → tick after ability. |
| 12 | Veneno ticks at end of turn | Integration test: unit with Veneno → HP reduced after action completes |
| 13 | Sueño wakes on Sangrado damage but not on Veneno | Integration test: sleeping unit with Sangrado wakes, sleeping unit with Veneno stays asleep |
| 14 | Quemadura does not tick when passing turn | Integration test: unit passes turn → Quemadura does not fire |
| 15 | CC immunity prevents stun/sleep for 1 turn after CC ends | Integration test: stunned → next turn immune → turn after vulnerable |
| 16 | Muerte executes below threshold, fails above | Unit test: HP at 29% of max with 30% threshold → executed. HP at 30% → not executed. |
| 17 | Muerte fails against bosses | Integration test: boss at 1% HP + Muerte ability → "IMMUNE" |
| 18 | Ships are immune to Sueño, Aturdimiento, Muerte | Integration test: apply each to ship → no effect |
| 19 | Ships do not critical hit | Integration test: ship attacks 1000 times → CritMod = 1.0 always |
| 20 | Naval formula uses ship stats, not unit stats | Integration test: ship with FPW=200 → damage uses 200, not crew member ATK |
| 21 | Healing does not crit and does not exceed MaxHP | Unit test: heal when HP = MaxHP-10 → heals exactly 10 |
| 22 | Dispel removes temporary buffs/debuffs but not permanent | Integration test: unit with 1 permanent + 1 temporary buff → dispel removes temporary only |
| 23 | Variance stays within ±5% | Statistical test: 10,000 damage calculations → all within 0.95–1.05 range |

## Open Questions

| # | Question | Impact | Status / Resolution |
|---|----------|--------|---------------------|
| 1 | Should LCK affect status effect resistance? (e.g., higher LCK = lower chance of being poisoned) | Balance | Open — defer to playtesting. Could add a formula: `ResistChance = LCK / 200` (max 50% at LCK 100). |
| 2 | Do elements have secondary effects? (e.g., Pólvora abilities have higher chance to apply Quemadura) | Design depth | Open — could be per-ability rather than per-element. Defer to ability design. |
| 3 | How does MP regeneration work? (per-turn regen, abilities, items) | UDM flagged this | Open — options: flat regen per turn, % regen, or no auto-regen (only via abilities/items). Decide in Combate Terrestre GDD. |
| 4 | Should naval combat have unique status effects? (e.g., "Abordaje" = boarding action) | Naval identity | Open — defer to Combate Naval GDD. |
| 5 | How does Quemadura interact with counter-attacks or reactive abilities? | Edge case | Open — if a unit counter-attacks, that counts as "using an ability" and triggers Quemadura. To be validated in playtesting. |
| 6 | CRI cap removed — how does this affect UDM secondary stat tables? | Cross-system | Requires UDM update: remove 50% hard cap, adjust CRI growth rates if needed. |
| 7 | Element field needs to be added to CharacterData (UDM) and ShipData (SDM) | Cross-system | Requires updates to both approved GDDs before implementation. |
| 8 | Where does the ability data model live? (AbilityPower, Element, HealPower, targeting, effect type) | Architecture | Open — UDM defines AbilityEntry (Id, UnlockLevel, Source) but not the ability definition itself. Needs a dedicated Ability Data section in this GDD, in Combate Terrestre, or in a separate Ability System GDD. |
