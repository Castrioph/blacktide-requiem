# Active Session State

<!-- STATUS -->
Epic: Combate Naval
Feature: Sprint 4 — Naval-first (production/sprints/sprint-004.md)
Task: S4-06 Naval Combat UI — implementado + verificado con capture; PENDIENTE playtest usuario (DoD) y commit.
<!-- /STATUS -->

- Updated: 2026-06-12
- Sprint/Task: sprint-004 / S4-06 (specs + HUD + assets) — 335/335 tests
- Verification Path: editor-tools (check_compile ✅, capture_ui_canvas ✅ x4, smoke test clicks vía SimNavalActions)

## S4-06 estado

- Specs aprobados: docs/art/ui-s406-naval-ux-spec.md + ui-s406-naval-visual-design.md.
  Decisiones usuario: D1 chips overlay en sprite, D2 LB barra binaria, D3 inspección incluida.
- Código: Assets/Scripts/UI/Combat/Naval/ (NavalCombatHUD, CrewChipOverlay,
  NavalShipView, NavalUIFactory, NavalUIColors, NavalPlayerCombatInput) +
  Runtime/Combat/NavalCombatBootstrap. CombatRunner += resolver/enemyInputSelector.
- asmdef BlacktideRequiem += Unity.InputSystem (proyecto es Input System-only;
  EventSystem necesita InputSystemUIInputModule, NO StandaloneInputModule).
- 20 iconos IA en Assets/Resources/Sprites/UI/Naval/ (Coplay gemini, ~$2.7).
- Escena: Assets/Scenes/NavalCombat.unity (BuildNavalCombatScene.Execute la regenera).
  Batalla auto-start: marea_espectral (crew elena/kael/mirra) vs 3 oleadas
  (balandra / bergantín+serpiente / galeón jefe).
- Smoke test OK: cañonazo+targeting, abordaje con chips (zoom+focus+confirm),
  AI enemiga, log español, IB, oleada 1/3.

## Pendiente S4-06 (antes de Done)

- Playtest manual usuario: open NavalCombat.unity → Play. Probar: Maniobra,
  Reparar, Escape/click-dcho cancelar, hover inspección crew, oleadas 2-3,
  jefe, victoria/derrota. Helpers: Assets/Editor/SimNavalActions.cs.
- Commit tras playtest (pedir al usuario). Luego S4-07 stage naval + flujo.

## Notas para S4-07

- NavalCombatBootstrap es temporal: S4-07 lo sustituye por StageData naval
  en el flujo MainMenu→StageSelect→Combat→Results.
- Agente ui-programmer murió a mitad (patrón conocido); resto se hizo inline.
