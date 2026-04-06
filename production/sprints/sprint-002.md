# Sprint 2 — Production Land Combat

> **Status**: Active
> **Method**: Kanban (no timeboxing — work until done)
> **Created**: 2026-04-06
> **WIP Limit**: 1 task in progress at a time

## Goal

Build the production-quality land combat system: a reusable CombatManager,
enemy AI, traits system, and Combat UI — transforming the throwaway prototype
into a shippable foundation.

---

## Carryover from Sprint 1

| Task | Reason | New ID |
|------|--------|--------|
| S1-10 Combat UI placeholder | Not started — promoted to Must Have | S2-05 |
| S1-11 Enemy System basic | Not started — promoted to Must Have | S2-03 |
| S1-12 Traits/Sinergias basic | Not started — promoted to Should Have | S2-06 |

---

## Board

### Done

| ID | Task | Owner | Acceptance Criteria |
|----|------|-------|---------------------|
| S2-01 | ADR: Combat Architecture (CombatManager, state machine, event flow) | technical-director | ADR in `docs/architecture/adr-003-combat-architecture.md` |
| S2-02 | Production CombatManager (land combat state machine + event bus) | gameplay-programmer | Manages full battle lifecycle, emits events for UI, uses existing InitiativeBar/DamageCalculator. 17 unit tests |
| S2-04 | AbilityData ScriptableObject + ability resolution | gameplay-programmer | AbilityData SO (power, element, MP cost, cooldown, target type, secondary effects), CombatManager resolves them, cooldown tracking, MP consumption. 14 unit tests |
| S2-03 | Enemy System (AI profiles: Agresivo, Defensivo, Caótico) | ai-programmer | 3 AI profiles, ICombatInput implementation, team-aware CombatContext. 16 unit tests |

### In Progress

_(empty)_

### Ready (next up)

| ID | Task | Owner | Dependencies | Acceptance Criteria |
|----|------|-------|-------------|---------------------|
| S2-05 | Combat UI with UI Toolkit (HP bars, initiative bar, action buttons, battle log) | ui-programmer | S2-02 ✅ | Production UI using UXML/USS, driven by CombatManager events. Playable land battle with real UI |

### Backlog — Should Have

| ID | Task | Owner | Dependencies | Acceptance Criteria |
|----|------|-------|-------------|---------------------|
| S2-06 | Traits/Sinergias basic (3 traits: Hijos del Mar, Malditos, Hierro Viejo) | gameplay-programmer | S2-02 | Trait detection + buff application in combat, 5+ unit tests |
| S2-07 | Status Effects in combat (Aturdimiento, Sueño, Veneno, Ceguera) | gameplay-programmer | S2-02 | Status effects trigger from abilities, display in UI, 8+ unit tests |
| S2-08 | Ship Data Model (ShipData SO, ShipStatBlock, role slots) | gameplay-programmer | — | ScriptableObject creable, ship stats legible, 5+ unit tests |

### Backlog — Nice to Have

| ID | Task | Owner | Dependencies | Acceptance Criteria |
|----|------|-------|-------------|---------------------|
| S2-09 | Game Flow / Scene Manager (basic screen transitions) | engine-programmer | — | Main Menu → Combat → Results flow working |
| S2-10 | Create 3 demo CharacterData SOs (from vertical slice roster) | game-designer | S2-04 | 3 playable units with abilities, varied elements/roles |

---

## Risks

| Risk | Probability | Impact | Mitigation |
|------|------------|--------|------------|
| UI Toolkit runtime quirks (Unity 6.3) | Media | Alto | User has web dev background (HTML/CSS), mitigates learning curve. Consult engine-reference docs. UGUI fallback if critical blocker |
| CombatManager over-engineering | Media | Alto | Keep close to prototype logic, just structured. ADR first (S2-01) |
| Event bus architecture unfamiliar | Media | Medio | Simple C# events first, upgrade later if needed |

## Definition of Done

- [ ] Production land combat playable with UI Toolkit (not IMGUI)
- [ ] Enemy AI makes autonomous decisions (3 profiles)
- [ ] Abilities defined as ScriptableObjects with MP cost
- [ ] Combat architecture documented in ADR
- [ ] 25+ new unit tests across combat systems
- [ ] No S1/S2 bugs in delivered features
- [ ] Design documents updated if any deviations

## Notes

- UI Toolkit chosen over UGUI — user has professional web dev background
  (HTML/CSS), making UXML/USS a natural fit. Also scales better for the
  many menu screens needed in later sprints (gacha, roster, stage select).
- The prototype (`Assets/Scripts/Prototypes/CombatV1/`) is reference only —
  production code must be rewritten to production standards in `Assets/Scripts/`.
- All new production code requires doc comments on public APIs and unit tests.
