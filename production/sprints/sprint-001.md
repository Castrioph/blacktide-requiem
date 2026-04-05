# Sprint 1 — Foundations & Core Loop Prototype

> **Status**: Active
> **Method**: Kanban (no timeboxing — work until done)
> **Created**: 2026-03-28
> **WIP Limit**: 1 task in progress at a time

## Goal

Establecer el proyecto Unity, tomar decisiones arquitectónicas clave, y prototipar
el combate terrestre para validar la hipótesis central del juego.

---

## Board

### Done

| ID | Task | Owner | Acceptance Criteria |
|----|------|-------|---------------------|
| S1-01 | Crear proyecto Unity 6.3 LTS + estructura de carpetas + .gitignore | engine-programmer | Proyecto compila, estructura match `CLAUDE.md` |
| S1-02 | ADR: State Management (ScriptableObjects + Event Bus, no singletons) | technical-director | ADR en `docs/architecture/` con decisión y rationale |
| S1-03 | ADR: Rendering Pipeline (URP 2D setup) | technical-director | ADR + URP configurado en proyecto |
| S1-04 | Implementar Unit Data Model (CharacterData SO, StatBlock) | gameplay-programmer | ScriptableObject creable, stats legibles, 15 unit tests passing |

### In Progress

_(empty)_

### Ready (next up)

| ID | Task | Owner | Dependencies | Acceptance Criteria |
|----|------|-------|-------------|---------------------|
| S1-05 | Implementar Damage & Stats Engine (fórmula master + buffs) | gameplay-programmer | S1-04 | Fórmulas del DSE GDD implementadas, 5+ unit tests |

### Backlog — Critical Path

| ID | Task | Owner | Dependencies | Acceptance Criteria |
|----|------|-------|-------------|---------------------|
| S1-06 | Implementar Initiative Bar (orden de turnos por SPD) | gameplay-programmer | S1-04 | Turnos correctos según SPD, test con 6 entidades |
| S1-07 | Prototipar Combate Terrestre (core loop jugable) | prototyper | S1-05, S1-06 | 3 unidades vs 2 enemigos, ataque+habilidad+guardia, victoria/derrota funcional |

### Backlog — Should Have

| ID | Task | Owner | Dependencies | Acceptance Criteria |
|----|------|-------|-------------|---------------------|
| S1-08 | Definir Vertical Slice Scope (documento) | producer | — | Doc en `design/` con stages, unidades, barcos del demo |
| S1-09 | Configurar performance budgets en technical-preferences.md | technical-director | S1-01 | Valores concretos para FPS, frame budget, draw calls, memory |
| S1-10 | Combat UI placeholder (HP bars, turno actual, botones acción) | ui-programmer | S1-07 | Prototipo jugable sin arte final |

### Backlog — Nice to Have

| ID | Task | Owner | Dependencies | Acceptance Criteria |
|----|------|-------|-------------|---------------------|
| S1-11 | Enemy System básico (2 AI profiles: Agresivo, Caótico) | ai-programmer | S1-05, S1-06 | Enemigos toman decisiones autónomas según profile |
| S1-12 | Traits/Sinergias básico (1 trait funcional) | gameplay-programmer | S1-04, S1-05 | Buff se activa con 3+ unidades del mismo trait |

---

## Risks

| Risk | Probabilidad | Impacto | Mitigación |
|------|-------------|---------|------------|
| Curva de aprendizaje Unity (primer proyecto) | Alta | Alto | Empezar con sistemas simples (data model), usar `/prototype` para iterar rápido |
| URP 2D setup complejo | Media | Medio | Usar configuración default de URP 2D, no customizar shaders en sprint 1 |
| Scope creep en prototipo | Media | Medio | El prototipo es THROWAWAY — sin arte, sin polish, solo validar el loop |

## Definition of Done

- [x] Proyecto Unity compila y corre
- [x] 2+ ADRs en `docs/architecture/`
- [ ] Prototipo de combate terrestre jugable (placeholder art)
- [ ] Core loop validado: seleccionar acción → resolver turno → victoria/derrota
- [ ] 5+ unit tests para Damage & Stats Engine
- [ ] No S1/S2 bugs en features entregadas
- [ ] Design documents actualizados si hay desviaciones

## Notes

- Este sprint cubre los 3 blockers del gate-check Pre-Production → Production:
  1. Prototipo (S1-07)
  2. Sprint plan (este documento)
  3. Vertical slice scope (S1-08)
- El prototipo es throwaway code en `prototypes/` — no en `Assets/Scripts/`
- Todas las decisiones arquitectónicas deben documentarse en ADRs antes de escribir código en `Assets/Scripts/`
