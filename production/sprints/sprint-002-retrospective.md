# Retrospective: Sprint 2 — Production Land Combat

Period: 2026-04-06 — 2026-04-20
Generated: 2026-04-20

## Metrics

| Metric | Planned | Actual | Delta |
|--------|---------|--------|-------|
| Tasks | 10 | 10 | 0 |
| Completion Rate | — | 100% | — |
| Story Points / Effort Days | n/a (Kanban, no estimates) | 14 calendar days | — |
| Bugs Found (sprint-tagged fixes) | — | 3 | — |
| Bugs Fixed | — | 3 | — |
| Unplanned Tasks Added | — | 0 | — |
| Commits | — | 16 | — |

Bug-fix commits in-sprint: `fix(combat): rename AllySingle to SingleAlly`,
`fix(ui): target selection / battle log sizing`, `fix(ui): HUD layout / wave
labels / mid-round wave clear`.

## Velocity Trend

| Sprint | Planned | Completed | Rate |
|--------|---------|-----------|------|
| S1 | 10 (7 must, 3 optional) | 7 | 70% |
| S2 | 10 | 10 | 100% |

**Trend**: Increasing. S1 left three items unfinished (S1-10/11/12), all
promoted to S2 and delivered as S2-05/03/06. Test coverage also grew from
5+ DoD target in S1 to **112 EditMode tests** in S2.

## What Went Well

- **100% Kanban clear**: every card reached Done, including the three S1
  carryovers that were the original risk going in.
- **ADR-first discipline paid off**: S2-01 (combat architecture ADR) landed
  first; no re-architecture happened during S2-02/03/04, avoiding the
  "CombatManager over-engineering" risk flagged in the sprint plan.
- **Zero technical-debt markers**: `grep TODO|FIXME|HACK` across
  `Assets/Scripts/` returns 0 matches. Doc comments on public APIs held.
- **Pragmatic scope pivots**: UI Toolkit → UGUI on S2-05 was documented in
  sprint notes and freed the rest of the sprint from a known risk item.
- **Shared-spec pattern on S2-10** (DemoRosterFactory consumed by both
  editor tool and tests) is reusable for future data-asset tasks.

## What Went Poorly

- **HUD required 2 dedicated fix commits after S2-05 shipped** (`fix(ui):
  target selection…`, `fix(ui): HUD layout…`). This isn't rework per se,
  but it suggests S2-05's acceptance ("driven by CombatManager events")
  was met before the UI was actually usable end-to-end — a thinner
  acceptance bar than other tasks.
- **Coplay MCP disconnected mid-verification** during S2-09 playtest.
  Fallback to Unity batchmode failed ("another Unity instance is running"),
  forcing a manual playtest handoff. No documented recovery path existed.
- **No size estimates on any task** makes velocity and carryover trends
  hard to reason about beyond "10 cards vs 10 cards." S1's 30% miss rate
  is invisible until you count carryovers.
- **S1 retro never written**: sprint-001 was marked Completed and Sprint 2
  opened without a retro file. No previous action items could be checked.

## Blockers Encountered

| Blocker | Duration | Resolution | Prevention |
|---------|----------|------------|------------|
| UI Toolkit learning curve | ~1 day into S2-05 | Pivoted to UGUI Canvas | Prototype UI approach before committing the sprint task |
| Coplay MCP disconnect during S2-09 verify | ~1 session | Manual playtest by user | Document fallback: close editor + batchmode, or ask user early |
| Unity editor locked batchmode | Same as above | User ran tests + playtest in open editor | CLAUDE.md snippet: when Coplay is down, batchmode needs editor closed |

## Estimation Accuracy

No per-task estimates exist (Kanban, no timeboxing). Rough qualitative read:

| Task | Perceived | Actual | Variance | Likely Cause |
|------|-----------|--------|----------|--------------|
| S2-05 Combat HUD | ~1 task | 1 task + 2 polish fixes | +~30% | UI Toolkit detour + UGUI layout iteration |
| S2-06 Traits | should-have | shipped w/ 13 tests | at or under | Clear spec from S1 carryover |
| S2-10 Demo roster | nice-to-have, 3 units | 3 units + 9 abilities + 18 tests | expanded | User requested "más habilidades" mid-task |

**Overall estimation accuracy**: cannot compute — no estimates recorded.
Recommend lightweight T-shirt sizing next sprint so the metric becomes
tractable.

## Carryover Analysis

| Task | Original Sprint | Times Carried | Reason | Action |
|------|----------------|---------------|--------|--------|
| S2-05 (ex S1-10) Combat UI | S1 | 1 | Depended on S1-07 which slipped late | Done in S2 |
| S2-03 (ex S1-11) Enemy System | S1 | 1 | Not started in S1 | Done in S2 |
| S2-06 (ex S1-12) Traits | S1 | 1 | Not started in S1 | Done in S2 |

Zero tasks carry over from S2 → S3.

## Technical Debt Status

- Current TODO count: 0 (previous: unknown, no S1 retro)
- Current FIXME count: 0
- Current HACK count: 0
- Trend: Stable at zero
- Concern: `DemoBattleSetup` still creates CharacterData/AbilityData
  instances inline at runtime rather than consuming the S2-10 .asset files.
  Not a TODO marker but a known duplication worth a follow-up (S3-?).

## Previous Action Items Follow-Up

No S1 retrospective exists, so nothing to track. Creating this retro
establishes the baseline.

## Action Items for Next Iteration

| # | Action | Owner | Priority | Deadline |
|---|--------|-------|----------|----------|
| 1 | Add T-shirt sizing (S/M/L) column to sprint board so velocity is measurable | producer | High | Sprint 3 kickoff |
| 2 | Refactor `DemoBattleSetup` to consume `Assets/Data/` .asset files (remove runtime duplication) | gameplay-programmer | Med | Sprint 3 early |
| 3 | Document Coplay-down fallback in `CLAUDE.md` (close editor + batchmode, or request manual playtest) | technical-director | Med | Sprint 3 kickoff |
| 4 | Define "playtested" acceptance row in sprint board DoD so UI tasks are not marked Done before user verifies in editor | producer | Med | Sprint 3 kickoff |
| 5 | Always write a retro at sprint close — even one paragraph — before opening the next sprint | producer | High | Ongoing |

## Process Improvements

- **Lightweight estimation**: T-shirt sizes, not hours. Goal is trend
  visibility, not precision.
- **Tighter UI task acceptance**: require a playtest screenshot or checklist
  item, not just "driven by events." Would have caught S2-05 polish work
  before the card moved to Done.
- **Retro-before-next-sprint rule**: enforces learning. Missing the S1
  retro meant we entered S2 with unarticulated risk around UI scope.

## Summary

Sprint 2 shipped in full: all ten cards Done, zero carryover, 112 EditMode
tests, and a working MainMenu → Combat → Results loop backed by a real data
pipeline. The single most important change going forward is **adding
lightweight size estimates to the board** — we cannot improve what we
cannot measure, and "100% of 10" tells us less than it looks.
