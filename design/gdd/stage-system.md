# Stage System

> **Status**: Approved
> **Author**: User + Claude Code Game Studios agents
> **Last Updated**: 2026-04-01
> **Implements Pillar**: Pillar 1 (Profundidad Estratégica Dual), Pillar 4 (Respeto al Tiempo del Jugador)

## Overview

The Stage System defines how content is organized, structured, and presented to
the player within the Aventura tab. It manages chapters, scenes, battles, and the
enemy/reward configuration for each encounter.

Content is organized hierarchically: **Chapters** (story arcs) contain **Scenes**
(narrative segments), which contain **Battles** (individual combat encounters of
1-5 waves). Each battle defines: which enemies appear in each wave, combat context
(land or sea), energy cost, completion rewards, first-clear bonuses, mission
objectives, and optional narrative triggers.

The player interacts with this system through the stage select screen: they pick a
chapter, select a scene, choose a battle, and enter combat. Enemy composition is
hidden until combat begins — the player sees only the battle name, energy cost,
rewards, and mission objectives. Discovery and surprise are part of the experience.
After combat, rewards are distributed and the next battle unlocks.

The session flow (battle → rewards → upgrade → repeat) is the core loop that
drives daily play. This system is the content backbone — without it, combat
systems have nothing to fight and progression systems have no source of XP and
currency. It sits between Game Flow (which provides the navigation framework) and
the Combat systems (which execute the encounters this system defines).

## Player Fantasy

**"One more battle before I stop."** The Stage System drives the session loop —
the reason the player keeps playing for "just five more minutes."

**The pull of progress**: Each battle cleared is a visible step forward on the
chapter map. The player sees their progress bar fill, the next scene unlock, the
"Clear" badge appear. There's always one more battle between them and a reward
they want.

**The thrill of the unknown**: Enemy composition is hidden until combat starts.
The player enters a battle called "The Trench" and discovers a Kraken boss they
weren't prepared for. The surprise creates memorable moments — and a reason to
retry with a better team.

**The satisfaction of mastery**: Completing all missions on a battle (no deaths,
under N turns, use element X) earns bonus stars. Coming back to a battle with a
stronger team to claim that last star feels like a victory lap.

The system fails if the stage list feels like a soulless checklist, if there's no
reason to replay cleared content, or if the player can't tell at a glance how far
they've progressed.

## Detailed Design

### Core Rules

#### 1. Content Hierarchy

```
Mode (Story, Naval, Eventos)
└── Chapter
    └── Scene
        └── Battle (= 1 combat encounter)
```

| Level | Description | Example |
|-------|-------------|---------|
| **Mode** | Content category in the Aventura tab | Story, Naval, Eventos |
| **Chapter** | Story arc with a thematic identity | "Chapter 1: La Isla del Naufragio" |
| **Scene** | Narrative segment within a chapter (3-5 battles) | "Scene 2: Those Whom the Water Serves" |
| **Battle** | Single combat encounter (1-5 waves of enemies) | "Battle 7: Assassins from the Lake" |

#### 2. Battle Data

| Field | Type | Description |
|-------|------|-------------|
| `BattleId` | string | Unique identifier (e.g., `"ch1_sc1_b3"`) |
| `DisplayName` | string | Player-facing name |
| `CombatContext` | enum | Land, Sea |
| `Waves` | List\<WaveConfig\> | Enemy composition per wave |
| `Rewards` | RewardConfig | DOB, XP, item drops on clear (awarded every run) |
| `FirstClearRewards` | RewardConfig | GDC bonus for first completion (one-time only) |
| `Missions` | List\<Mission\> (3) | Optional objectives with GDC bonus rewards |
| `NarrativeBefore` | NarrativeId? | Narrative scene before first attempt (null = none) |
| `NarrativeAfter` | NarrativeId? | Narrative scene after first clear (null = none) |
| `UnlockCondition` | Condition | What must be done to access this battle (usually: previous battle cleared) |
| `EnergyCost` | int | Energy required to enter. 0 = free (story/naval). Event stages: 10/15/25 by difficulty. See Rewards System GDD §3-4 |
| `RecommendedLevel` | int | Displayed to player as difficulty hint |
| `ZoneId` | string | Zone identifier for Enemy System zone variants |

#### 3. Wave Configuration

| Field | Type | Description |
|-------|------|-------------|
| `WaveIndex` | int | Order in the battle (1-based) |
| `Enemies` | List\<EnemySpawn\> | Which enemies appear |
| `IsBossWave` | bool | If true, this wave has a Jefe enemy |

**EnemySpawn:**

| Field | Type | Description |
|-------|------|-------------|
| `EnemyTemplateId` | string | Reference to EnemyData template |
| `Position` | int | Slot position on the battlefield (1-5 for land, 1-3 for naval) |
| `ZoneOverride` | ZoneVariant? | Optional per-spawn stat/element override |

- Land battles: max 5 enemies per wave
- Naval battles: max 3 enemies per wave (ships/creatures are larger)
- Boss waves typically have 1 Jefe + 0-2 Normal adds

#### 4. Mission System

Each battle has exactly **3 missions** — optional objectives that grant GDC
(premium currency) as bonus rewards. This is a primary F2P income source.

| Mission Type | Example | Typical GDC Reward |
|-------------|---------|-------------------|
| **Survival** | "No allies KO'd" | 10-20 GDC |
| **Performance** | "Clear within 10 turns" | 10-20 GDC |
| **Tactical** | "Deal Tormenta damage", "Use a Limit Break", "Kill boss last" | 15-30 GDC |

- Missions are **visible before entering** the battle (on the battle card)
- Missions can be completed across multiple runs — don't need to clear all 3 at once
- Each mission can only be claimed once (tracked per mission, not per run)
- Completing all 3 missions for a battle awards a **"Complete" star badge**
- First-clear bonus also awards GDC (separate from mission rewards)

**F2P economy role**: First-clear GDC + mission GDC are the main ways free players
earn premium currency through gameplay. DOB flows freely from replays; GDC comes
from one-time progression achievements.

#### 5. Content Modes

| Mode | Location | Combat Context | Unlock |
|------|----------|---------------|--------|
| **Story** | Aventura → Story | Land | Available from start |
| **Naval** | Aventura → Naval | Sea | After Chapter 1 clear |
| **Eventos** | Aventura → Eventos | Land or Sea | After Chapter 1 clear |

- **Story** is the main progression path. Linear chapter-by-chapter.
- **Naval** has its own stage sequence, parallel to story but shorter.
- **Eventos** are time-limited content (not for demo — placeholder section).

#### 6. Progression & Unlock Rules

- Battles within a scene unlock **sequentially** (clear Battle 1 → Battle 2 unlocks)
- Scenes within a chapter unlock sequentially
- Chapters unlock sequentially
- Clearing the last battle of a chapter triggers a chapter-clear celebration + rewards
- Replaying a cleared battle awards normal rewards (DOB, XP, items) but NOT
  first-clear GDC or mission GDC again

#### 7. Energy System

Energy is **not implemented for the demo**. All battles are free to enter with no
limit. The architecture supports adding an energy cost field per battle for future
implementation.

#### 8. Demo Content Scope

| Content | Count | Details |
|---------|-------|---------|
| Chapters (Story) | 2 | Ch1: intro/tutorial arc. Ch2: first real arc. |
| Scenes per chapter | 2-3 | Ch1: 2 scenes (5-6 battles). Ch2: 2-3 scenes (4-5 battles). |
| Total land battles | ~10 | Across both chapters |
| Naval battles | 3 | Separate Naval mode, unlocks after Ch1 |
| Total battles | ~13 | 10 land + 3 naval |
| Waves per battle | 1-5 | Early: 1-2 waves. Later: 3-5. Boss battles: 3+ with boss in final wave. |

### States and Transitions

**Battle states (per player):**

| State | Description | Transitions To |
|-------|-------------|----------------|
| **Locked** | Unlock condition not met. Grayed out on stage select. | Unlocked |
| **Unlocked** | Available to play. No clear badge. | In Progress |
| **In Progress** | Player is currently in combat for this battle | Cleared, Failed |
| **Cleared** | Completed at least once. "Clear" badge shown. | In Progress (replay) |
| **Failed** | Player lost or abandoned. No state change persisted. | In Progress (retry) |
| **Complete** | All 3 missions claimed. Full star badge. | In Progress (replay) |

**Chapter states:**

| State | Description |
|-------|-------------|
| **Locked** | Previous chapter not cleared |
| **In Progress** | At least one battle cleared, not all |
| **Cleared** | All battles cleared. Chapter-clear rewards awarded. |
| **Complete** | All battles cleared + all missions in all battles claimed |

### Interactions with Other Systems

| System | Direction | Data Interface |
|--------|-----------|----------------|
| **Game Flow** | Upstream | Game Flow provides navigation framework. Stage System populates Aventura tab. Uses Game Flow's combat scene transition. |
| **Enemy System** | Stage → Enemy | Stage wave configs reference EnemyData templates. ZoneId feeds into zone variant scaling. |
| **Currency System** | Stage → Currency | Calls `AddCurrency(DOB, amount)` on every clear. Calls `AddCurrency(GDC, amount)` for first-clear and mission rewards. |
| **Rewards System** | Stage → Rewards | Battle clear triggers reward distribution (DOB, XP, items, materials). First-clear and mission GDC granted per battle. |
| **Combate Terrestre** | Stage → CT | Stage System provides wave configs and combat context. CT executes the land combat encounter. |
| **Combate Naval** | Stage → CN | Same as CT but for naval battles. Stage System provides naval wave configs. |
| **Narrative System** | Stage ↔ Narrative | Stage triggers narrative scenes (NarrativeBefore/After). Narrative System provides the content. |
| **Save/Load System** | Stage ↔ Save | Persists: battle clear state, mission completion, chapter progress. Restores on load. |
| **Menus & Navigation UI** | Stage → UI | UI reads battle data for stage select display: name, rewards, missions, clear state. |

**Cross-system update required**: The Currency System GDD currently defines
first-clear as "3x DOB multiplier". This must be updated: first-clear rewards are
**GDC** (not DOB multiplier). DOB is the replay reward; GDC is the first-clear
reward. Update `design/gdd/currency-system.md` §Formulas §1.

## Formulas

### Replay Rewards (DOB — every clear)

```
ReplayDOB = BASE_STAGE_DOB + (StageIndex × DOB_PER_STAGE)
```

| Variable | Value | Description |
|----------|-------|-------------|
| `BASE_STAGE_DOB` | 100 | Minimum DOB from any battle |
| `DOB_PER_STAGE` | 50 | Additional DOB per stage increment |

- Stage 1: 100 DOB, Stage 5: 300 DOB, Stage 10: 550 DOB

### First-Clear Rewards (GDC — one-time)

```
FirstClearGDC = BASE_FIRST_CLEAR_GDC + (StageIndex × GDC_PER_STAGE)
```

| Variable | Value | Description |
|----------|-------|-------------|
| `BASE_FIRST_CLEAR_GDC` | 30 | Minimum GDC from first clear |
| `GDC_PER_STAGE` | 5 | Additional GDC per stage increment |

- Stage 1: 30 GDC, Stage 5: 55 GDC, Stage 10: 80 GDC
- Chapter-clear bonus: additional flat GDC (100 GDC for Ch1, 150 for Ch2)

### Mission Rewards (GDC — one-time per mission)

```
MissionGDC = MISSION_BASE_GDC × MissionDifficultyMod
```

| Variable | Value | Description |
|----------|-------|-------------|
| `MISSION_BASE_GDC` | 15 | Base GDC per mission |
| `MissionDifficultyMod` | 1.0-2.0 | Harder missions give more (tactical > survival) |

- 3 missions × ~15-30 GDC = ~45-90 GDC per battle from missions
- Total GDC per new battle (first-clear + missions): ~75-170 GDC

### F2P GDC Income (Demo estimate)

```
13 battles × ~120 GDC avg (first-clear + missions) = ~1,560 GDC
+ Chapter-clear bonuses: ~250 GDC
= ~1,810 GDC total from stage progression (~6.7 multi-pulls)
```

Combined with daily login + daily missions (~1,800 GDC/week from Currency System),
F2P players have access to significant gacha resources.

### XP Rewards

```
BattleXP = sum of enemy XPRewards per wave (from Enemy System)
```

XP is calculated from enemies killed, not as a flat stage reward. Already defined
in Enemy System (`BASE_ENEMY_XP × TierMultiplier × ZoneStatMultiplier` per enemy).

### Recommended Level

```
RecommendedLevel = BASE_RECOMMENDED_LV + (StageIndex × LV_PER_STAGE)
```

| Variable | Value | Description |
|----------|-------|-------------|
| `BASE_RECOMMENDED_LV` | 5 | First stage recommended level |
| `LV_PER_STAGE` | 3 | Level increase per stage |

- Stage 1: Lv 5, Stage 5: Lv 20, Stage 10: Lv 35

### Variable Definitions (Complete)

| Variable | Type | Default | Range | Description |
|----------|------|---------|-------|-------------|
| BASE_STAGE_DOB | int | 100 | 50-300 | Min DOB per replay |
| DOB_PER_STAGE | int | 50 | 20-100 | DOB scaling per stage |
| BASE_FIRST_CLEAR_GDC | int | 30 | 15-60 | Min GDC on first clear |
| GDC_PER_STAGE | int | 5 | 2-10 | GDC scaling per stage |
| MISSION_BASE_GDC | int | 15 | 5-30 | Base GDC per mission |
| MissionDifficultyMod | float | 1.0 | 1.0-2.0 | Mission reward scaling |
| BASE_RECOMMENDED_LV | int | 5 | 1-10 | First stage recommended level |
| LV_PER_STAGE | int | 3 | 1-5 | Level scaling per stage |

## Edge Cases

| Edge Case | Resolution |
|-----------|------------|
| **Player replays a cleared battle** | Normal rewards (DOB, XP, items) awarded. First-clear GDC and mission GDC NOT re-awarded. |
| **Player completes a mission on a failed run** | Mission progress is NOT saved on failure. Only successful clears count for mission tracking. |
| **Player clears battle but app crashes before rewards** | Battle clear state is saved atomically with rewards. If crash occurs before save, battle is treated as not cleared — player must replay. |
| **Player meets mission condition but loses the battle** | Mission not credited. Must clear AND meet condition in the same run. |
| **Player completes 2 of 3 missions in one run, 1 in another** | Valid. Missions track independently. "Complete" badge awarded when all 3 are claimed. |
| **Last battle of chapter is a boss — player can't beat it** | No skip mechanic. Player must level up, change team, or improve strategy. RecommendedLevel serves as difficulty hint. |
| **Player tries to enter naval battle before Ch1 clear** | Mode is locked in Aventura tab with message: "Completa el Capítulo 1 para zarpar." |
| **All scenes in a chapter cleared but not all missions** | Chapter state = "Cleared" (not "Complete"). Chapter-clear rewards granted. "Complete" requires all missions. |
| **Player gains access to Chapter 2 units before finishing Chapter 1** | Not possible — chapters are sequential. But units obtained from gacha can be used in any chapter. |
| **Battle with 0 waves configured** | Invalid — minimum 1 wave. Validation at content authoring time, not runtime. |
| **Naval battle with land-only enemies** | Invalid — content authoring must match CombatContext. Validation catches mismatches. |

## Dependencies

### Upstream Dependencies

| System | Dependency Type | Interface |
|--------|----------------|-----------|
| Game Flow | Hard | Provides navigation framework, combat scene transitions, Aventura tab structure. |

### Downstream Dependencies

| System | Dependency Type | Interface |
|--------|----------------|-----------|
| Combate Terrestre | Hard | CT receives wave configs from Stage System for land battles. |
| Combate Naval | Hard | CN receives wave configs for naval battles. |
| Enemy System | Hard | Stage wave configs reference EnemyData templates and ZoneIds. |
| Currency System | Hard | Stage rewards call AddCurrency for DOB (replays) and GDC (first-clear + missions). |
| Rewards System | Hard | Post-combat reward distribution uses stage reward config. |
| Narrative System | Hard | Stage triggers narrative scenes via NarrativeBefore/After references. |
| Menus & Navigation UI | Hard | Stage select screen reads battle data (name, rewards, missions, state). |
| Save/Load System | Hard | Persists battle clear state, mission completion, chapter progress. |

## Tuning Knobs

| Knob | Current Value | Range | What It Affects | If Too High | If Too Low |
|------|--------------|-------|----------------|-------------|------------|
| BASE_FIRST_CLEAR_GDC | 30 | 15-60 | GDC from first clearing a battle | F2P too generous, devalues IAP | F2P starved, frustrating |
| GDC_PER_STAGE | 5 | 2-10 | GDC scaling with progression | Late stages give too much GDC | Flat feeling, no progression reward |
| MISSION_BASE_GDC | 15 | 5-30 | GDC per mission objective | Missions too rewarding, trivializes gacha | Missions not worth pursuing |
| Waves per battle | 1-5 | 1-7 | Battle length | Battles drag (Pillar 4 violation) | Battles feel trivial |
| Enemies per wave (land) | 3-5 | 1-5 | Wave difficulty | Overwhelming, too many targets | Too easy, no tactics needed |
| Enemies per wave (naval) | 1-3 | 1-3 | Naval wave difficulty | Screen cluttered with ships | Single target, boring |
| RecommendedLevel scaling | +3/stage | +1 to +5 | Difficulty curve | Player hits wall fast | No sense of challenge growth |
| Chapter-clear GDC bonus | 100/150 | 50-300 | Milestone reward satisfaction | Milestone too generous | Milestone anticlimactic |

### Knob Interactions (Danger Zones)

| Knob A | Knob B | Interaction |
|--------|--------|-------------|
| GDC_PER_STAGE | SINGLE_PULL_COST_GDC (Currency) | Together determine how many stages = 1 gacha pull. If GDC/stage is high and pull cost is low, pulls feel free. |
| RecommendedLevel | Player stat growth (UDM) | If level scaling outpaces player growth, difficulty spikes. Must track UDM growth curves. |
| Waves per battle | Enemy HP budgets (Enemy System) | More waves × tanky enemies = long battles. Total battle time should stay under ~3-5 min for normal, ~5-8 for boss. |
| BASE_FIRST_CLEAR_GDC | Total battle count (demo scope) | More battles × more GDC = higher total F2P income. Balance against gacha rates. |

## Visual/Audio Requirements

> **Note**: Visual direction is placeholder for demo. A visual redesign with more
> personality is planned for post-demo polish.

### Visual

- **Stage select screen**: Vertical list of battles per scene (FFBE-style). Each
  battle card shows: name, NRG cost (future), mission icons, "Clear"/"Complete" badge
- **Chapter select**: Panel with chapter list. Character art of protagonist or
  chapter villain
- **Scene headers**: Scene name as visual separator between battle groups
- **Battle card states**: Locked (gray + padlock), Unlocked (normal color),
  Cleared ("Clear" green badge), Complete (golden star)
- **Mission icons**: 3 small icons per battle card. Gray if incomplete, gold if done
- **Chapter-clear celebration**: Special screen with treasure chest opening, GDC
  flying to counter, chapter name displayed
- **Mode tabs**: Sub-tabs in Aventura: Story | Naval | Eventos. Naval/Eventos
  grayed out with padlock until unlocked

### Audio

- **Stage select BGM**: Per-chapter theme (each chapter has its own select music)
- **Battle unlock SFX**: Chain breaking sound when a battle unlocks
- **Mission complete SFX**: Satisfying chime on mission completion
- **Chapter clear SFX**: Brief pirate fanfare (~3s) + treasure chest sound
- **"Clear" badge SFX**: Stamp/seal sound

## UI Requirements

- **Aventura tab layout**: Mode selector (Story/Naval/Eventos) at top. Scrollable
  content below
- **Chapter select** (left panel): Vertical list of chapters. Active chapter
  expanded. Future chapters locked
- **Battle list** (right panel): Vertical scroll of battle cards grouped by scene.
  Scene name as header
- **Battle card**: Name, recommended level, NRG cost (future, "FREE" placeholder for
  demo), 3 mission icons, clear/complete badge
- **Battle card tap**: Opens popup with: battle name, recommended level, rewards
  preview (DOB + items), first-clear reward (GDC if unclaimed), 3 missions with
  description + state. "Entrar" button to start
- **Mission display**: Each mission shows: description, GDC reward, state
  (pending/completed/claimed)
- **Progress bar**: Per chapter — "X/Y batallas completadas" visible in chapter header
- **Locked content message**: Clear text explaining unlock condition (not just a
  padlock icon)

## Acceptance Criteria

| # | Criterion | How to Verify |
|---|-----------|---------------|
| 1 | Battles unlock sequentially within a scene | Test: clear battle 1 → battle 2 unlocks. Skip battle 2 → battle 3 stays locked. |
| 2 | First-clear GDC is awarded exactly once per battle | Test: clear battle → receive GDC. Replay → no GDC. |
| 3 | Mission GDC is awarded exactly once per mission | Test: complete mission → receive GDC. Re-clear with same condition → no GDC. |
| 4 | Missions can be completed across multiple runs | Test: complete mission 1 in run A, mission 2 in run B → both credited. |
| 5 | Mission progress is NOT saved on failure | Test: meet mission condition but lose → mission not credited. |
| 6 | Replay rewards (DOB, XP) are granted on every clear | Test: replay cleared battle → receive DOB and XP. |
| 7 | Chapter-clear rewards trigger on last battle clear | Test: clear last battle of Ch1 → chapter-clear celebration + bonus GDC. |
| 8 | Naval mode is locked until Chapter 1 cleared | Test: open Aventura before Ch1 clear → Naval grayed out with message. |
| 9 | Battle card shows correct state (Locked/Unlocked/Cleared/Complete) | Visual test: verify each state displays correctly. |
| 10 | "Complete" badge appears only when all 3 missions are claimed | Test: clear 2/3 missions → "Cleared" badge. Clear 3/3 → "Complete" star. |
| 11 | Enemy composition is NOT visible on stage select | Test: battle card shows name/rewards/missions but NOT enemy list. |
| 12 | Recommended level displays correctly per formula | Test: stage 1 = Lv 5, stage 5 = Lv 20, stage 10 = Lv 35. |
| 13 | Wave configs correctly spawn enemies at designated positions | Integration test: enter battle → correct enemies appear per wave config. |
| 14 | Atomic save: battle clear + rewards are saved together | Test: simulate crash after clear → state is either fully saved or not at all. |

## Open Questions

| # | Question | Impact | Status / Resolution |
|---|----------|--------|---------------------|
| 1 | ~~Is energy its own GDD or part of Stage System?~~ | Architecture | **Resolved**: Energy not in demo. Architecture supports future energy cost field per battle. Will be its own GDD when implemented. |
| 2 | ~~Should first-clear reward be DOB or GDC?~~ | Economy | **Resolved**: GDC. First-clear + missions = primary F2P GDC income. DOB flows from replays. Cross-system update needed in Currency System. |
| 3 | Should Hard/EX difficulty modes share the same missions as Normal, or have separate mission sets? | Replay value | Open — not for demo. When Hard mode is added, recommend unique missions per difficulty. |
| 4 | Exploration stages (non-combat, character movement) — how do they fit into the hierarchy? | Future feature | Open — user envisions exploration stages for the future. Current Battle = combat encounter; architecture may need extension for non-combat stage types. |
| 5 | Exact narrative scene placement per battle (which battles trigger NarrativeBefore/After) | Content design | Open — defer to content authoring and Narrative System GDD. |
| 6 | Should chapter-clear celebration be skippable on replay? | UX | Open — recommend yes (skip on repeat clears). First time: unskippable for impact. |
| 7 | Visual redesign for stage select — more personality and unique identity | Visual polish | Open — current design is functional FFBE-style. Post-demo, redesign with more thematic art, maps, or unique chapter presentations. |
