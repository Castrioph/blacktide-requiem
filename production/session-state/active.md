# Active Session State

<!-- STATUS -->
Epic: Combate Naval
Feature: Sprint 4 — Naval-first (production/sprints/sprint-004.md)
Task: S4 abierto. Siguiente: S4-01 ADR-004 Naval Combat Architecture (bloquea todo).
<!-- /STATUS -->

- Updated: 2026-06-12
- Sprint/Task: sprint-003 / S3-11 UI Review & Polish (P0+P1+P2 completo)
- Verification Path: Coplay Play mode + capturas ✅ — falta playtest manual usuario (retro #4)

## S3-11 entregado

- Docs: ui-s311-ux-audit.md + ui-s311-visual-design.md (paleta canónica #140F24)
- Escenas movidas a Assets/Scenes/ (stubs borrados), Build Settings reescrito
- CanvasScaler 1080×1920, firstSelected, scroll horizontal off, ghost text fuera
- Headers 180px, botones 540×88+, ColorBlocks oro, disabled WCAG #A08040
- Fuentes Pirata One + Noto Sans (TTF legacy Text — TMP diferido)
- Prefabs: stripes acento, reward strip, estados slot/roster lleno=oro
- BtnClear ELIMINADO (decisión usuario): 2º tap en roster deselecciona
- Aplicador idempotente: Assets/Editor/ApplyS311UIPolish.cs (corrido 2×, OK)

## Flujo verificado (Coplay, post-polish)

MainMenu → StageSelect → TeamSelect → Combat → Results → MainMenu ✅

## Fase 4-5 (team-ui) completadas

- Review UX: P0 8/8 ✅, P1 8/9 (P1-08 cerrado por review Art). Review Art: CONSISTENT ×5 secciones
- Fase 5: BtnBack ColorBlock cream/gold fixed (targetGraphic=label) + paleta canónica en lessons §4
- RewardTables creadas (Assets/Data/Rewards/, 50/80/120 Doblones) y cableadas a stages — cards muestran botín real
- Gap aceptado: D-pad explícito por listas (P2-10) — focus inicial por código

## Sprint 3 cerrado (2026-06-12)

- Commits `4b358c3` (S3-08) + `31d6d06` (S3-11) pushed a origin/main
- Retro: production/sprints/sprint-003-retrospective.md — 100% Must+Should
- Carryover a S4: S3-09 Save/Load (decidir post-gacha), S3-10 ADR-004 (1ª tarea S4)
- Acción clave S4: acceptance data-driven = "assets cableados y visibles en juego"
