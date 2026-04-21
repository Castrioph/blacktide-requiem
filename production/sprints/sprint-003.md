# Sprint 3 — Content Loop: Stage, Currency, Rewards, Team Composition

> **Status**: Proposed
> **Method**: Kanban (WIP=1) + lightweight T-shirt sizing (S/M/L)
> **Created**: 2026-04-20
> **Kickoff**: 2026-04-21 (pending user approval)
> **Predecessor**: sprint-002 (10/10 Done, 112 EditMode tests, 0 carryover)

## Goal

Wrap the MVP land-side loop end-to-end: pick a team, select a stage, fight,
collect rewards, return to menu with persistent currency. Turns the combat
prototype into a playable demo shell. Naval + Gacha deferred to Sprint 4.

## Size Legend (new this sprint, per retro action #1)

- **S** = ~1 session (data SO + tests, or small refactor)
- **M** = 2–3 sessions (system + integration + tests)
- **L** = 4+ sessions (cross-cutting, multiple files, new UX)

---

## Board

### Done

| ID | Task | Owner | Size | Acceptance Criteria |
|----|------|-------|------|---------------------|
| S3-01 | Currency System impl (Wallet SO, Doblones + GemasDeCalavera, event on change) | gameplay-programmer | S | CurrencyWallet SO with Add/TrySpend/GetBalance/ResetBalances, BalanceChanged event, atomic transactions, MAX_BALANCE clamp, 19 EditMode tests |
| S3-04 | Refactor DemoBattleSetup to consume `Assets/Data/` .asset files (retro action #2) | gameplay-programmer | S | DemoBattleSetup loads CharacterData from SO, no inline instances. 4 enemy assets added. Inspector wiring required. |
| S3-02 | Stage System impl (StageData SO, StageRegistry, StageController) | gameplay-programmer | M | StageData SO (wave/enemy defs, difficulty), StageRegistry lookup, StageController.BuildBattleConfig, 3 demo stages, 16 EditMode tests |
| S3-03 | Rewards System (drop table SO, payout on victory) | gameplay-programmer | S | RewardTable SO, stage-indexed, paid to Wallet on CombatManager.Victory, 11 EditMode tests |
| S3-05 | Team Composition basic (roster list → 3-slot team → hand off to CombatManager) | gameplay-programmer | M | TeamComposition data class, selection API, integrates into stage launch, 16 EditMode tests |

### In Progress

_(empty)_

### Ready (next up)

_(empty)_

### Backlog — Must Have (Critical Path)

_(empty)_

### Backlog — Should Have

| ID | Task | Owner | Size | Dependencies | Acceptance Criteria |
|----|------|-------|------|-------------|---------------------|
| S3-06 | Stage Select UI (UGUI — list 3 stages, difficulty label, Launch button) | ui-programmer | M | S3-02 | Playable MainMenu → StageSelect → Combat → Results loop. Playtested acceptance row required (retro #4) |
| S3-07 | Team Select UI (UGUI — pick 3 of 3 from demo roster, validate) | ui-programmer | M | S3-05, S3-06 | StageSelect → TeamSelect → Combat flow works. Playtest verified |
| S3-08 | Document Coplay-down fallback in `CLAUDE.md` (retro action #3) | technical-director | S | — | CLAUDE.md snippet: close editor + batchmode OR request manual playtest when Coplay MCP is down |

### Backlog — Nice to Have

| ID | Task | Owner | Size | Dependencies | Acceptance Criteria |
|----|------|-------|------|-------------|---------------------|
| S3-09 | Save/Load basic (JSON, Wallet + unlocked stages) | gameplay-programmer | M | S3-01, S3-02 | SaveLoadService round-trips Wallet and stage progress. 8+ EditMode tests |
| S3-10 | ADR-004: Naval Combat Architecture (prep for S4) | technical-director | S | design/gdd/combate-naval.md | ADR filed; no implementation yet |

---

## Carryover from Sprint 2

_(none — S2 closed with 0 carryover)_

---

## Risks

| Risk | Probability | Impact | Mitigation |
|------|------------|--------|------------|
| UI task scope creep repeats S2-05 pattern (HUD polish after "Done") | Media | Alto | Retro action #4 in effect: UI tasks require playtest verification row on DoD before moving to Done |
| Team composition API conflicts with later Ship/Crew integration (Naval, S4) | Media | Medio | Keep TeamComposition focused on land-unit selection; design a thin interface that naval can extend |
| Save/Load format locks in before Gacha/Inventory exists | Media | Alto | Keep S3-09 (nice-to-have) minimal: version header + forward-compat. Defer roster/inventory save to S4 |
| Coplay MCP disconnects mid-verify (S2-09 repeat) | Media | Medio | Retro action #3 lands early (S3-08); fallback path documented before S3-06/07 verify |

## Dependencies on External Factors

- None. All deps internal to repo.

## Definition of Done (Sprint 3)

- [ ] All Must Have tasks (S3-01..05) Done
- [ ] MainMenu → StageSelect → TeamSelect → Combat → Results → MainMenu loop **playtested by user**
- [ ] Currency persists across a single session (save only if S3-09 lands)
- [ ] 40+ new EditMode tests (running total ≥ 150)
- [ ] UI tasks (S3-06, S3-07) include a playtest-verified checkbox before Done (retro #4)
- [ ] No S1/S2/S3 bugs in delivered features
- [ ] Retro written at close, before Sprint 4 opens (retro #5)
- [ ] Zero TODO/FIXME/HACK markers in `Assets/Scripts/`

## Notes

- Naval combat + Gacha intentionally deferred to Sprint 4 — content loop must
  exist first so gacha pulls have a place to go and naval has a UI host.
- T-shirt sizing introduced per retro action #1. Goal is trend visibility,
  not precision.
- `DemoBattleSetup` refactor (S3-04) is small but blocks clean stage wiring —
  schedule early.
