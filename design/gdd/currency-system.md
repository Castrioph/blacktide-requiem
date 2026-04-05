# Currency System

> **Status**: Approved
> **Author**: User + Claude Code Game Studios agents
> **Last Updated**: 2026-04-01
> **Implements Pillar**: Pillar 3 (Recompensa a la Paciencia), Pillar 4 (Respeto al Tiempo del Jugador)

## Overview

The Currency System manages the two abstract currencies in the game: a free soft
currency (earned through gameplay) and a premium hard currency (purchasable with
real money, also earnable in limited quantities). These currencies are the primary
medium of exchange for gacha pulls, progression unlocks (awakening level cap
breaks), shop purchases, and energy refills.

Players interact with currencies passively (earning them as stage rewards, daily
login, missions) and actively (spending them on gacha pulls, unit upgrades, and
shop items). The system tracks balances, validates transactions, and enforces
spending rules. Materials (awakening stones, ship construction parts, equipment)
are NOT currencies — they are inventory items managed by their respective systems.
This system only handles fungible, stackable, abstract resources.

Without this system, there is no economy: no cost to pulling gacha, no friction
in progression, and no monetization path. It is the connective tissue between
earning rewards and spending them.

## Player Fantasy

**The thrill of saving and spending big**: The player who patiently hoards premium
currency across weeks, resisting mediocre banners, then unleashes it all on the
one character they've been waiting for — that moment of "I earned this" is the
emotional peak this system enables. The currency has weight because it was earned
through effort and restraint.

**The satisfaction of steady wealth**: After a long play session clearing stages,
the player sees their soft currency pile grow. They can feel progress even on a
a bad gacha day — because gold keeps flowing and their units keep getting stronger
through upgrades. Currencies are the visible proof that time spent was not wasted.

**This is infrastructure, not spectacle**: Players should never think about the
currency *system*. They should think about what they can buy. If a player is
confused about which currency to use for what, or feels like the game is hiding
costs, the system has failed.

## Detailed Design

### Core Rules

#### 1. Currency Definitions

| Currency | ID | Type | Earn Method | Primary Sinks |
|----------|----|------|------------|---------------|
| **Doblones** | `DOB` | Soft (free) | Stage clear rewards, daily missions, selling items, achievements | Awakening (level cap breaks), shop purchases, equipment upgrades |
| **Gemas de Calavera** | `GDC` | Hard (premium) | IAP (real money), story milestones, achievements, event rewards, daily login (limited) | Featured gacha pulls, energy refills, premium shop items, inventory expansion |

**Gacha pull resources (NOT currencies):**

| Resource | ID | Type | Earn Method | Use |
|----------|----|------|-------------|-----|
| **Ticket de Invocación Estándar** | `TIE` | Inventory item | Login streaks, calendar, mission streaks, achievements, event/chapter first-clears | 1 TIE = 1 single pull on standard banner. 10 TIE = 1 multi-pull |
| **Ticket de Invocación Featured** | `TIF` | Inventory item | Login streak day 28 (1/month) | 1 TIF = 1 single pull on featured banner. 10 TIF = 1 multi-pull |

TIE and TIF are **inventory items**, not currencies. They do not pass through
Currency System's `AddCurrency`/`TrySpend` interface — they are managed by the
inventory system (Unit Roster/Inventory). See Rewards System GDD §§3-8 for all
TIE/TIF sources and Sistema Gacha GDD for pull mechanics.

#### 2. Earning Rules

- Doblones flow freely from gameplay. Every stage cleared awards Doblones. The
  player should never feel "broke" for basic operations (leveling a unit) during
  normal play.
- Gemas de Calavera are scarce from gameplay. A F2P player earns enough for
  approximately **2 multi-pulls per month** on the featured banner (~5,680
  GDC/month recurrent) plus ~15 standard pulls via TIE tickets. This rate is
  a tuning knob.
- Currency rewards are deterministic (not random). A stage always awards the same
  Doblones. Randomness is reserved for item/material drops (Rewards System).

#### 3. Spending Rules

- Every transaction is atomic: currency is deducted only on confirmation. No
  partial transactions.
- Insufficient funds → transaction denied with a prompt showing how to acquire
  more (earn or buy).
- The system validates: `currentBalance >= cost` before any deduction.
- Spending Gemas de Calavera always requires an extra confirmation step ("Are you
  sure? This uses premium currency").

#### 4. Display Rules

- Doblones and Gemas de Calavera are always visible in the top bar of every screen.
- Amounts display with abbreviations at high values: 1,000 → 1K, 1,000,000 → 1M.
- A "+" button next to each currency opens the acquisition screen (shop for GDC,
  "how to earn" for DOB).

#### 5. No Conversion Between Currencies

- Doblones cannot be converted to Gemas de Calavera or vice versa. This prevents
  devaluing premium currency and keeps the economies separate.

### States and Transitions

Currencies are simple integer balances (≥ 0). State transitions:

| Event | Effect | Validation |
|-------|--------|------------|
| Earn (stage clear, mission, login) | Balance += amount | amount > 0 |
| Spend (gacha, upgrade, shop) | Balance -= cost | Balance >= cost |
| IAP purchase (GDC only) | Balance += purchased amount | Payment confirmed by store |
| Refund (failed transaction rollback) | Balance += refunded amount | Only via system, not player-initiated |

### Interactions with Other Systems

| System | Direction | Interface |
|--------|-----------|-----------|
| **Rewards System** | → Currency | Awards Doblones (and occasionally GDC) after stage clear. Calls `AddCurrency(DOB, amount)`. |
| **Sistema Gacha** | Currency → | Checks and deducts GDC for pulls. Calls `TrySpend(GDC, pullCost)` → returns success/fail. |
| **Progresión de Unidades** | Currency → | Checks and deducts Doblones for leveling/awakening. Calls `TrySpend(DOB, cost)`. |
| **Energy System** (separate) | Currency → | Deducts GDC for energy refills. Calls `TrySpend(GDC, refillCost)`. |
| **Save/Load System** | ↔ Currency | Persists and restores both currency balances. Reads `GetBalance(DOB)`, `GetBalance(GDC)`. |
| **Shop** (future) | Currency → | Deducts DOB or GDC for shop purchases. Uses same `TrySpend` interface. |
| **IAP Store** (platform) | → Currency | On successful purchase, calls `AddCurrency(GDC, purchasedAmount)`. |

## Formulas

### 1. Stage Doblones Reward

```
StageReward_DOB = BASE_STAGE_DOB + (StageIndex × DOB_PER_STAGE)
```

| Variable | Value | Description |
|----------|-------|-------------|
| `BASE_STAGE_DOB` | 100 | Minimum Doblones from any stage |
| `StageIndex` | 0-based | Position in the stage progression (Stage 1 = index 0) |
| `DOB_PER_STAGE` | 50 | Additional Doblones per stage increment |

- Stage 1: **100 DOB**, Stage 10: **550 DOB**, Stage 30: **1,550 DOB**

DOB is the **replay reward** — awarded every time a stage is cleared.

**First-clear bonus**: Awards **GDC** (not DOB). See Stage System GDD for
first-clear GDC formula (`BASE_FIRST_CLEAR_GDC + StageIndex × GDC_PER_STAGE`).
Mission objectives also award GDC. This makes stage progression the primary F2P
source of premium currency — cross-system update applied from Stage System GDD.

### 2. Doblones Sinks (costs paid in DOB)

| Sink | Cost Formula | Example (early → late) |
|------|-------------|----------------------|
| Awakening (cap break) | `AWK_BASE_COST × AWK_TIER_MULTIPLIER[tier]` | 1st: 2,000 DOB, 2nd: 6,000 DOB, 3rd: 15,000 DOB |
| Shop item purchase | Defined per item in Shop/Rewards System | Variable |

| Variable | Value | Description |
|----------|-------|-------------|
| `AWK_BASE_COST` | 2,000 | Base cost per awakening (see Progresión de Unidades GDD) |
| `AWK_TIER_MULTIPLIER` | [1, 3, 7.5] | Cost multiplier per awakening tier |

Note: Unit leveling uses **XP from battles + XP items (Ron)**, not Doblones.
Doblones unlock *potential* (awakening cap breaks), not *power directly* (levels,
stats). Ability slots were removed — all unlocked abilities are available per the
UDM. See Progresión de Unidades GDD for full awakening cost details.

### 3. Gacha Pull Cost

| Pull Type | Cost | Banner | Notes |
|-----------|------|--------|-------|
| Single pull (GDC) | 300 GDC | Featured | Standard rate |
| Multi-pull 10x (GDC) | 2,700 GDC | Featured | 10% discount vs 10 singles (3,000). Guarantees 1x 4★+ |
| Single pull (TIF) | 1 TIF | Featured | Same rates as GDC pull. Independent pity counter |
| Multi-pull 10x (TIF) | 10 TIF | Featured | No discount (10 tickets = 10 singles). Guarantees 1x 4★+. Independent pity |
| Single pull (TIE) | 1 TIE | Standard | 1 ticket = 1 pull |
| Multi-pull 10x (TIE) | 10 TIE | Standard | No discount (10 tickets = 10 singles). Guarantees 1x 4★+ |

**Ticket multi-pulls**: Tickets (TIE/TIF) can be used for multi-pulls at a 1:1
rate (10 tickets = 10 pulls). No discount applies — tickets already represent
free value. The 4★+ guarantee on multi-pull applies regardless of payment method.

**Banner separation**: GDC is exclusively for the **featured banner**. The
standard banner uses **TIE only**. TIF works on the featured banner as a free
alternative to GDC but with its own independent pity counter.

**Pity systems (3 independent counters)**:
- **Featured (GDC)**: Soft pity at 60, hard pity at 90. Includes 50/50 mechanic.
  Only GDC pulls advance this counter.
- **Featured (TIF)**: Same thresholds (60/90). No 50/50 — all 5★ in TIF pulls
  follow standard 50/50 rules of the featured banner. Independent counter.
  With ~1 TIF/month, pity is cosmetic but future-proof.
- **Standard (TIE)**: Soft pity at 60, hard pity at 90. No 50/50 (no featured
  unit). All units within each rarity are equiprobable.

### 4. F2P Monthly Income (target)

**GDC (Featured Banner):**

| Source | GDC/day | GDC/month (30d) | Notes |
|--------|---------|-----------------|-------|
| Daily missions (3×30 + 50 bonus) | 140 | 4,200 | See Rewards System §7 |
| Login streak (1,110 GDC / 28-day cycle) | ~40 | ~1,110 | See Rewards System §6 |
| Login calendar (monthly fixed) | ~12 | ~370 | See Rewards System §6 |
| **Total recurrent** | **~190** | **~5,680** | **~2.1 multi-pulls/month** |

One-time GDC (demo lifetime): ~6,560 GDC from stage first-clears + achievements.

**TIE (Standard Banner):**

| Source | TIE/month | Notes |
|--------|-----------|-------|
| Login streak (days 7, 14, 21) | 3 | See Rewards System §6 |
| Login calendar (monthly fixed) | 4 | See Rewards System §6 |
| Mission streak (7-day bonus) | 4 | See Rewards System §7 |
| **Total recurrent** | **~11** | ~1.1 multi-pulls/month |

One-time TIE (demo lifetime): ~26 TIE from event first-clears, chapter bonuses,
and achievements.

**TIF (Featured Banner, free):** ~1/month from login streak day 28.

**Combined F2P monthly (recurrent):** ~19 featured pulls + ~11 standard pulls =
**~30 total pulls/month**. First month is higher (~76 total) due to one-time
sources. Adjustable via tuning knobs.

### 5. Economy Health Ratio

```
EconomyRatio = WeeklyDOBIncome / WeeklyDOBSpend
```

Target: **1.2 – 1.5** (player earns slightly more than they spend on core
progression). Below 1.0 = frustrating grind. Above 2.0 = inflation, nothing
feels expensive.

## Edge Cases

### Currency Balance Edge Cases

| Situation | Resolution |
|-----------|------------|
| Player tries to spend more than they have | Transaction denied. UI shows "Insufficient [currency]" with a shortcut to earn/buy more. No partial spend. |
| Balance reaches 0 | No penalty. Player can still play stages and earn currency. Zero balance blocks only spending actions. |
| Balance would overflow (uint max) | Clamp to `MAX_BALANCE` (default: 999,999,999). Excess is lost. Warn player when within 10% of cap. |
| IAP purchase fails mid-transaction | No currency awarded. Player keeps their money. Retry prompt shown. |
| IAP purchase succeeds but game crashes before crediting | On next launch, verify pending purchases with the store API and credit any undelivered GDC. |
| Player refunds IAP through app store | If GDC already spent: flag account for review (negative GDC balance is illegal — do not allow). If GDC unspent: deduct the refunded amount. |
| Two spend operations race (e.g., gacha pull + shop purchase simultaneously) | All transactions are sequential (queue-based). Second transaction validates balance AFTER the first completes. |
| Reward granted while player is offline (daily login edge) | Rewards are calculated on login, not at midnight. "Daily login" means "first login of each calendar day (UTC)". |
| Negative amount passed to AddCurrency | Reject. `AddCurrency` only accepts positive values. Use `TrySpend` for deductions. |

### Gacha-Specific Edge Cases

| Situation | Resolution |
|-----------|------------|
| Player has 2,700 GDC, starts multi-pull, but a concurrent IAP finishes mid-pull | Pull cost is locked at transaction start. The IAP credits after the pull completes. No interference. |
| Player disconnects during gacha animation | Pull result was determined server-side (or at transaction time). On reconnect, show the result. Currency already deducted. |

## Dependencies

### Upstream Dependencies (systems this one needs)

None. Currency System is a foundation system with no upstream dependencies.

### Downstream Dependents (systems that need this one)

| System | Dependency Type | Interface |
|--------|----------------|-----------|
| **Sistema Gacha** (#13) | Hard | Reads GDC balance, calls `TrySpend(GDC, cost)` for pulls. Cannot function without currency. |
| **Progresión de Unidades** (#15) | Hard | Reads DOB balance, calls `TrySpend(DOB, cost)` for awakening (cap breaks). |
| **Rewards System** (#16) | Hard | Calls `AddCurrency(DOB, amount)` and occasionally `AddCurrency(GDC, amount)` after stage clears. |
| **Save/Load System** (#17) | Hard | Persists `GetBalance(DOB)` and `GetBalance(GDC)`. TIE/TIF balances are persisted by the inventory system, not Currency System. Restores on load. |
| **Energy System** (separate) | Soft | Uses `TrySpend(GDC, refillCost)` for energy refills. Energy works without currency — just can't refill with gems. |

### Cross-System Notes

- **UDM/Progresión alignment**: Leveling uses XP (battles + Ron items), not
  Doblones. Doblones are spent on awakening (cap breaks). Ability slots were
  removed — all unlocked abilities are available (see UDM + Progresión de Unidades GDD).
- **Rewards System** is the primary faucet for both currencies and for TIE/TIF
  tickets. Its design must respect the F2P income targets defined in this GDD's
  Formulas §4. See Rewards System GDD for TIE/TIF source breakdown.
- **TIE/TIF**: These are inventory items, not currencies. Currency System does
  not track them. Inventory (Unit Roster) owns TIE/TIF balances. Sistema Gacha
  consumes them via inventory interface, not `TrySpend`.

## Tuning Knobs

### Per-Currency Knobs

| Knob | Current Value | Range | What It Affects |
|------|--------------|-------|----------------|
| `BASE_STAGE_DOB` | 100 | 50-300 | Baseline Doblones per stage. Too low = grind. Too high = costs feel trivial. |
| `DOB_PER_STAGE` | 50 | 20-100 | How fast stage rewards scale. Too high = late stages make early ones worthless. |
| ~~`FIRST_CLEAR_MULTIPLIER`~~ | ~~3.0~~ | — | **Deprecated**: First-clear now awards GDC (see Stage System GDD). DOB is replay-only. |
| `AWK_BASE_COST` | 2,000 | 1,000-5,000 | Base cost for awakening (cap break). Major progression gate. See Progresión GDD for tier multipliers. |
| `SINGLE_PULL_COST_GDC` | 300 | 200-500 | Gacha pull price. Directly controls pull frequency for F2P. |
| `MULTI_PULL_DISCOUNT` | 10% | 0-20% | Incentive to save for multi-pulls vs. impulse single pulls. |
| `DAILY_LOGIN_GDC` | ~49 (avg) | 25-100 | Average daily GDC from dual login calendar system (streak + monthly fixed). See Rewards System §6. |
| `DAILY_MISSION_GDC` | 140 | 50-200 | Total daily GDC from missions (3×30 per mission + 50 bonus). See Rewards System §7. |
| `MAX_BALANCE` | 999,999,999 | — | Hard cap to prevent overflow. Should never be reached in normal play. |

### Knob Interactions (Danger Zones)

| Knob A | Knob B | Interaction |
|--------|--------|-------------|
| `DOB_PER_STAGE` | `SLOT_BASE_COST` / `CAP_BREAK_COST` | If rewards grow fast but costs don't, unlocks become trivially cheap in late game. Scale costs with progression or tune reward curve. |
| `DAILY_LOGIN_GDC` + `DAILY_MISSION_GDC` | `SINGLE_PULL_COST_GDC` | These three knobs together determine F2P pull frequency. Changing any one shifts the economy. Adjust as a group. |
| First-clear GDC (Stage System) | Stage count | More stages × GDC per first-clear = total F2P GDC income. Balance against gacha pull costs. |
| `MULTI_PULL_DISCOUNT` | `SINGLE_PULL_COST_GDC` | If discount is high and single cost is low, multi-pulls become extremely cheap. Keep the effective multi-pull cost in check. |

## Visual/Audio Requirements

### Visual

- **Doblones icon**: Gold doubloon coin with skull imprint. Should read clearly
  at 32×32px (mobile UI).
- **Gemas de Calavera icon**: Skull-shaped gem, glowing purple/magenta. Premium
  feel — slightly animated idle (subtle pulse).
- **Currency gain animation**: Numbers fly from the source (stage clear banner,
  shop) to the top bar counter. Gold particles for DOB, purple sparkle for GDC.
- **Insufficient funds**: Currency icon shakes + red flash on the cost number.
- **Large purchase (GDC)**: Brief "treasure chest opening" effect on confirmation.

### Audio

- **DOB gain**: Coin clink (light, satisfying, non-intrusive — plays often).
- **GDC gain**: Crystal chime (more impactful, rarer event).
- **Spend (any)**: Subtle "coin drop" sound. Should not be annoying during
  repeated purchases.
- **Insufficient funds**: Soft error buzz.
- **Gacha pull spend**: Distinct from normal spend — builds anticipation (handled
  by Gacha System audio).

## UI Requirements

- **Top bar widget**: Always visible. Shows [DOB icon] [amount] [+] | [GDC icon]
  [amount] [+]. Tapping the amount opens a breakdown popup (recent sources, total
  earned/spent this session).
- **Transaction confirmation**: For DOB: simple "Spend X Doblones?" dialog. For
  GDC: emphasized dialog with yellow/orange border, "Premium Currency!" label, and
  "Are you sure?" phrasing.
- **Acquisition screen (DOB)**: Shows top DOB-earning activities: "Clear stages",
  "Complete daily missions", "Sell items". Not a shop — just guidance.
- **Acquisition screen (GDC)**: IAP shop with gem packs. Shows: pack size, price,
  bonus (first-time double), and best value tag on optimal pack.
- **Currency history** (future, not MVP): Scrollable log of recent transactions:
  "+550 DOB — Stage 10 clear", "-2,700 GDC — Multi-pull".

## Acceptance Criteria

### Data Model Validation

- [ ] Both currencies (DOB, GDC) can be created, stored, and retrieved with correct balances
- [ ] Currency IDs are unique and referenced consistently across all systems

### Transaction Validation

- [ ] `TrySpend(currency, amount)` returns false and does not deduct when balance < cost
- [ ] `TrySpend(currency, amount)` returns true and deducts exactly `amount` when balance ≥ cost
- [ ] `AddCurrency(currency, amount)` increases balance by exactly `amount`
- [ ] `AddCurrency(currency, negativeAmount)` is rejected (returns error)
- [ ] Transactions are atomic — no partial deductions on failure
- [ ] Concurrent transactions are serialized — no race conditions

### Balance Integrity

- [ ] Balance never goes below 0
- [ ] Balance clamps at `MAX_BALANCE` (999,999,999) — excess is discarded
- [ ] Balance survives save/load cycle without loss or corruption
- [ ] Balance survives app kill and restart (persisted before or during transaction)

### IAP Integration

- [ ] Successful IAP adds the correct GDC amount
- [ ] Failed IAP adds nothing
- [ ] Pending IAP from crash is recovered and credited on next launch
- [ ] Refunded IAP deducts GDC (or flags account if already spent)

### Economy Targets

- [ ] Stage rewards match formula: `BASE_STAGE_DOB + (StageIndex × DOB_PER_STAGE)`
- [ ] First-clear bonus awards GDC (not DOB) per Stage System GDD formula
- [ ] First-clear GDC is awarded only once per stage
- [ ] F2P weekly GDC income is within 10% of the 1,800 GDC target (sum of all free sources)

### UI Verification

- [ ] Both currency balances display correctly on the top bar of every screen
- [ ] Large numbers abbreviate correctly (1K, 1M)
- [ ] "+" button next to each currency navigates to the correct acquisition screen
- [ ] Insufficient funds prompt appears with correct messaging and shortcut
- [ ] Premium currency spend shows extra confirmation dialog

## Open Questions

| Question | Owner | Target Resolution |
|----------|-------|-------------------|
| Should there be a "first-time purchase" double bonus for GDC IAP packs? | Economy Designer | Sistema Gacha GDD |
| Should Doblones have any use in naval content specifically (ship upgrades?), or are ship costs purely materials? | Game Designer | Ship progression / Equipment System GDD |
| Should daily login GDC scale with consecutive days (day 1: 30, day 7: 100) or be flat? | Economy Designer | Rewards System GDD |
| Is the energy system its own GDD, or a section within Stage System? | Producer | Stage System GDD or separate GDD |
| What happens to currency on a hypothetical account reset/new game? Full wipe including purchased GDC, or purchased GDC is restored? | Game Designer / Legal | Save/Load System GDD |
