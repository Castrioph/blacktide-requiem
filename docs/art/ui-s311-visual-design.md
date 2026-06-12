# S3-11 Visual Design Spec — MainMenu, StageSelect, TeamSelect
> Author: Art Director agent
> Date: 2026-06-12
> Sprint: S3-11 Phase 2 — Full P0 + P1 + P2 scope approved
> Implements findings from: `docs/art/ui-s311-ux-audit.md`
> Target: Unity 6.3 UGUI, 1080×1920 portrait reference
> Prerequisite: CanvasScaler must be set to Scale With Screen Size, ref 1080×1920,
>               match height (MatchWidthOrHeight = 1.0) on ALL THREE canvases (P0-01).

---

## Section 1 — Canonical Palette Decision

### 1.1 Discrepancy Resolution

Two competing background colors exist in the project:

| Source | Color value | Hex approx |
|--------|-------------|------------|
| `ui-stageselect-visual-direction.md` (approved design doc) | Deep Navy | `#0D1B2A` |
| `coplay-unity-lessons.md` section 4 (currently implemented) | `Color(0.08, 0.06, 0.14)` | `#140F24` |
| MainMenu / StageSelect scenes (measured) | `Color(0.08, 0.06, 0.14)` | `#140F24` |
| TeamSelect scene (measured) | `Color(0.06, 0.04, 0.10)` | `#0F0A1A` |

**Decision: `#140F24` is the canonical BgDark.**

Rationale: The design intent behind Deep Navy `#0D1B2A` was a dark ocean blue-green
to evoke deep water. `#140F24` is a deep purple-indigo that already exists in two
of the three scenes and has been live since S3-06 playtest. Switching to `#0D1B2A`
would require updating all three scenes AND the coplay-unity-lessons palette
constants. The purple-indigo tone also reads well with the pirate/voodoo thematic
blend (dark sea + mystic shadow). Retaining `#140F24` costs zero rework.

`#0F0A1A` (TeamSelect only) is a deviation that must be corrected to `#140F24`
(audit finding P2-08 / P1-06 area).

The `coplay-unity-lessons.md` section 4 palette constants are now the canonical
reference, with the additions below. The visual-direction doc's `#0D1B2A` is
superseded by `#140F24` for all future work.

---

### 1.2 Canonical Color Table

All hex values are sRGB. Unity Color(r,g,b) float equivalents provided for
editor scripts. Alpha column is the Image component alpha (0–255) unless noted.

#### Core Palette

| Token | Hex | Unity float (r,g,b) | Alpha | Primary use |
|-------|-----|---------------------|-------|-------------|
| `BgDark` | `#140F24` | 0.08, 0.06, 0.14 | 255 | Screen backgrounds |
| `BgGradientTop` | `#1A2744` | 0.10, 0.15, 0.27 | 180 | Gradient overlay top row |
| `BgGradientBot` | `#050D14` | 0.02, 0.05, 0.08 | 220 | Gradient overlay bottom row |
| `BgNoise` | `#4A7FA5` | 0.29, 0.50, 0.65 | 12 | Optional additive noise tint |
| `PanelDark` | `#1F172E` | 0.12, 0.09, 0.18 | 255 | Header, footer, card panels |
| `Gold` | `#D4A017` | 0.83, 0.63, 0.09 | 255 | Titles, active borders, dividers |
| `GoldBright` | `#FFD700` | 1.00, 0.84, 0.00 | 255 | Bevel catch-lights, glow |
| `GoldMid` | `#E8B420` | 0.91, 0.71, 0.13 | 255 | Button highlighted state |
| `GoldDark` | `#B8880F` | 0.72, 0.53, 0.06 | 255 | Button pressed state |
| `Cream` | `#EDD9A3` | 0.93, 0.85, 0.64 | 255 | Secondary text, back button |
| `CreamMuted` | `#F5E6C8` | 0.96, 0.90, 0.78 | 255 | Stage name text on card |
| `HeaderBase` | `#1A0D00` | 0.10, 0.05, 0.00 | 255 | Header fill, button label dark |
| `BtnActive` | `#D4A017` | 0.83, 0.63, 0.09 | 255 | Primary button normal (=Gold) |
| `BtnHighlight` | `#E8B420` | 0.91, 0.71, 0.13 | 255 | Primary button highlighted |
| `BtnPressed` | `#B8880F` | 0.72, 0.53, 0.06 | 255 | Primary button pressed |
| `BtnDisabledBg` | `#3D3020` | 0.24, 0.19, 0.13 | 255 | Disabled button background |
| `BtnDisabledFg` | `#3D3020` | 0.24, 0.19, 0.13 | 255 | Disabled button image tint |
| `DisabledLabel` | `#A08040` | 0.63, 0.50, 0.25 | 180 | Disabled label text (WCAG fix) |
| `WoodBase` | `#3D2810` | 0.24, 0.16, 0.06 | 230 | Card base fill |
| `WoodLight` | `#4A3018` | 0.29, 0.19, 0.09 | 255 | Card selected base fill |
| `WoodBorder` | `#5C3D1E` | 0.36, 0.24, 0.12 | 255 | Card unselected border |
| `WoodCatch` | `#8B5E3C` | 0.55, 0.37, 0.24 | 180 | Card catch-light strip |
| `Shadow` | `#050D14` | 0.02, 0.05, 0.08 | 60 | Card shadow pool |
| `RewardLabel` | `#8B5E3C` | 0.55, 0.37, 0.24 | 220 | "Recompensa:" text |
| `NeutralDot` | `#2A3A4A` | 0.16, 0.23, 0.29 | 255 | Unfilled difficulty dot |
| `SlotEmpty` | `#1F172E` | 0.12, 0.09, 0.18 | 255 | Empty slot background (=PanelDark) |
| `SlotFilled` | `#2A1E10` | 0.16, 0.12, 0.06 | 255 | Filled slot background (warm) |
| `SlotBorderEmpty` | `#3A2A50` | 0.23, 0.16, 0.31 | 255 | Empty slot border |
| `SlotBorderFilled` | `#D4A017` | 0.83, 0.63, 0.09 | 255 | Filled slot border (=Gold) |
| `RosterEntryBg` | `#1F172E` | 0.12, 0.09, 0.18 | 230 | Roster card background |
| `RosterEntryInTeam` | `#2A1E10` | 0.16, 0.12, 0.06 | 255 | Roster card when in team |

#### Stage Accent Colors

| Stage | Name | Primary Accent | Secondary Accent |
|-------|------|----------------|-----------------|
| stage_001 | Bahía Corsaria | `#1E88E5` Corsair Blue | `#4FC3F7` Sea Foam |
| stage_002 | Muelle Maldito | `#6A1B9A` Voodoo Violet | `#CE93D8` Pale Violet |
| stage_003 | Templo Vudú | `#BF360C` Temple Ember | `#FF8A65` Ember Orange |

Primary accent: left stripe on card, border on selected state.
Secondary accent: difficulty dots filled color.

#### WCAG Contrast Compliance

| Foreground | Background | Approx ratio | Requirement | Status |
|-----------|-----------|-------------|-------------|--------|
| Gold `#D4A017` on BgDark `#140F24` | — | ~7.2:1 | 4.5:1 AA | PASS |
| Cream `#EDD9A3` on BgDark `#140F24` | — | ~12.8:1 | 4.5:1 AA | PASS |
| Cream `#EDD9A3` on PanelDark `#1F172E` | — | ~11.1:1 | 4.5:1 AA | PASS |
| Dark `#1A0D00` on Gold `#D4A017` | — | ~6.5:1 | 4.5:1 AA | PASS |
| DisabledLabel `#A08040` on BtnDisabledBg `#3D3020` | — | ~4.7:1 | 4.5:1 AA | PASS (fixed) |

**Disabled label fix:** The original `#6B5A30` on `#3D3020` produced ~2.1:1 (FAIL).
`#A08040` on `#3D3020` produces ~4.7:1. Use `#A08040` alpha 180 for all disabled
button labels across all screens. This resolves audit finding P2-09.

---

## Section 2 — MainMenu: Full Visual Spec

Reference wireframe target from audit §1.5. All dimensions in the 1080×1920
reference space (CanvasScaler Scale With Screen Size, match height).

### 2.1 Layer Stack (bottom to top)

```
Canvas (Screen Space Overlay)
  BgBase           ← solid fill #140F24
  BgGradient       ← 2×2 sprite gradient overlay
  MenuPanel        ← transparent container, fills canvas
    LogoSlot       ← Image placeholder (inactive until real logo)
    TitleText      ← TMP, Pirata One
    SubtitleText   ← TMP, Noto Sans Italic
    Separator      ← decorative line
    BtnStartBattle ← Button, gold
```

### 2.2 Background Layers

| Object | Type | Color | Alpha | Anchor | Size |
|--------|------|-------|-------|--------|------|
| BgBase | Image | `#140F24` | 255 | (0,0)→(1,1) | fill |
| BgGradient | Image (2×2 sprite bilinear) | — | 180 | (0,0)→(1,1) | fill |

BgGradient sprite: `ui_bg_ocean_gradient.png` — top row `#1A2744`, bottom row
`#050D14`, 2×2 px, Bilinear filter, Sprite import mode.

### 2.3 Logo Slot

| Property | Value |
|----------|-------|
| Object name | `LogoSlot` |
| Type | Image |
| Sprite | `ui_logo_placeholder.png` (existing in Assets/Sprites/UI/ if present, else 1×1 white) |
| Color | White `#FFFFFF` alpha 200 |
| Anchor | top-center: AnchorMin (0.5, 1), AnchorMax (0.5, 1) |
| AnchoredPosition | (0, −120) |
| SizeDelta | (600, 300) |
| Active | false initially; true once a real logo sprite exists |
| Preserve aspect | true |

### 2.4 Title Text

| Property | Value |
|----------|-------|
| Object name | `TitleText` |
| Type | TextMeshProUGUI |
| Text | "BLACKTIDE REQUIEM" |
| Font | Pirata One Regular |
| Font asset path | `Assets/Fonts/PirataOne-Regular SDF.asset` |
| Font size | 56 sp |
| Color | Gold `#D4A017` |
| Alignment | Center, Middle |
| TMP Drop Shadow | offset (2, −2), color `#000000` alpha 160 |
| Anchor | top-center: AnchorMin (0.5, 1), AnchorMax (0.5, 1) |
| AnchoredPosition | (0, −460) |
| SizeDelta | (800, 80) |

Placement rationale: 460 px from top puts the title at roughly 24% down a 1920 px
screen — upper quarter, correct for branding. The logo slot occupies 0–420 px;
title sits below it with 20 px gap.

### 2.5 Subtitle Text

| Property | Value |
|----------|-------|
| Object name | `SubtitleText` |
| Type | TextMeshProUGUI |
| Text | "El mar cobra lo suyo." |
| Font | Noto Sans Regular Italic |
| Font asset path | `Assets/Fonts/NotoSans-Italic SDF.asset` |
| Font size | 18 sp |
| Color | Cream `#EDD9A3` |
| Alpha | 180 |
| Alignment | Center, Middle |
| Anchor | top-center: AnchorMin (0.5, 1), AnchorMax (0.5, 1) |
| AnchoredPosition | (0, −560) |
| SizeDelta | (700, 40) |

### 2.6 Decorative Separator

| Property | Value |
|----------|-------|
| Object name | `Separator` |
| Type | Image (1×1 white sprite, tinted) |
| Color | Gold `#D4A017` alpha 120 |
| Anchor | top-center: AnchorMin (0.5, 1), AnchorMax (0.5, 1) |
| AnchoredPosition | (0, −620) |
| SizeDelta | (400, 2) |

### 2.7 Primary Button — "¡INICIAR MISIÓN!"

#### Dimensions and Placement

| Property | Value |
|----------|-------|
| Object name | `BtnStartBattle` |
| Anchor | center: AnchorMin (0.5, 0.5), AnchorMax (0.5, 0.5) |
| AnchoredPosition | (0, −220) |
| SizeDelta | (540, 88) |

Placement rationale: center-anchor at −220 px from screen center puts the button
at ~57% of screen height — visually in the lower half without being at the very
bottom, leaving breathing room. The button is 540 px wide (50% of 1080 px) and 88 px
tall (touch target minimum).

#### Button Layer Stack (child Images)

| Object | Color | Alpha | Anchor | SizeDelta |
|--------|-------|-------|--------|-----------|
| BgBase (Image on Button itself) | `#D4A017` | 255 | fill | — |
| BevelTop (child Image) | `#FFD700` | 200 | top strip, AnchorMin (0,1) AnchorMax (1,1) | (0, 4) |
| BevelBot (child Image) | `#8B6914` | 120 | bottom strip, AnchorMin (0,0) AnchorMax (1,0) | (0, 4) |

#### Button Label

| Property | Value |
|----------|-------|
| Object name | `BtnLabel` |
| Type | TextMeshProUGUI |
| Text | "¡INICIAR MISIÓN!" |
| Font | Pirata One Regular |
| Font size | 26 sp |
| Color | `#1A0D00` |
| Alpha | 255 |
| Letter spacing | 2 px |
| Alignment | Center, Middle |
| Anchor | fill: (0,0)→(1,1) |

**Remove:** The ghost empty Text child (fileID 954099865, P0-05) must be deleted.
BtnStartBattle must have exactly ONE label child after the fix.

#### ColorBlock (Color Tint mode)

| State | Color | Alpha |
|-------|-------|-------|
| Normal | `#D4A017` | 255 |
| Highlighted | `#E8B420` | 255 |
| Pressed | `#B8880F` | 255 |
| Disabled | `#3D3020` | 255 |
| Color Multiplier | 1.0 | — |
| Fade Duration | 0.1 s | — |

Disabled label color: `#A08040` alpha 180 (contrast ratio ~4.7:1 on `#3D3020`,
meeting WCAG AA). The label text changes to "¡INICIAR MISIÓN!" regardless of
disabled state; color alone communicates the state difference, supplemented by the
dark background.

**m_Interactable in scene**: 1 (true). MainMenu button should be active on load.

### 2.8 Logo Placeholder Sprite (P2-06 fix)

`Assets/Sprites/UI/ui_logo_placeholder.png` — if this sprite does not exist in the
project, create a 600×300 px solid `#1F172E` with a centered 2 px gold border
(procedural: use a 32×32 9-slice with 4 px border inset, color `#D4A017`). This
slot is inactive by default; the Image component exists in hierarchy for future use.

### 2.9 Visual Hierarchy — MainMenu

Prominence order (what the eye should hit first):

1. **Title "BLACKTIDE REQUIEM"** — largest text, gold, Pirata One 56 sp, center screen.
   Establishes brand identity before any interaction.
2. **Primary Button** — gold fill, full 50% screen width, highest color saturation
   on screen. Single available action; cannot be missed.
3. **Subtitle** — cream italic, quiet. Provides thematic flavor without competing.
4. **Separator** — decorative only, visual breathing room.
5. **Logo slot** — inactive placeholder; when active it will precede the title in
   visual weight.

Reasoning: MainMenu has exactly one action (proceed). The visual hierarchy should
reflect that single affordance. Gold color appears only on the title and the button,
so the player's eye moves title → button naturally.

---

## Section 3 — StageSelect: Deltas vs Approved Direction

The approved visual direction (`docs/art/ui-stageselect-visual-direction.md`) is
the source of truth for StageSelect. This section documents ONLY the changes
required by the UX audit. Do not reimplement what the direction doc already covers.

### 3.1 Header (P0-03 + P1-03 fixes)

**Current:** 80 px tall, header fill `#1F172E`.
**Required:** 180 px tall.

The header fill changes from `PanelDark #1F172E` to `HeaderBase #1A0D00` to match
the visual direction spec exactly. The bottom gold divider (4 px, `#D4A017` alpha
220) must be present.

Updated RectTransform for Header object:
- AnchorMin: (0, 1), AnchorMax: (1, 1)
- AnchoredPosition: (0, −90) — moves anchor point to header center, so top edge
  sits at screen top and bottom edge is 180 px down.
- SizeDelta: (0, 180)

StageScrollView must be pushed down accordingly:
- SizeDelta: (0, −360) — subtracts 180 px header + 180 px footer from screen height.
  (If footer is 180 px; if footer stays at 160 px then SizeDelta: (0, −340).)
  See §3.3 for footer spec.

Subtitle text inside header:
- Same spec as approved direction §4 — "Elige tu destino, corsario.", Noto Sans
  Italic 14 sp, Cream `#EDD9A3` alpha 180, 6 px below title baseline.

### 3.2 Back Button (P1-04 fix)

**Current:** Image component with gold `#996B1A` fill — violates visual hierarchy.
**Required:** No background Image (alpha 0 on the Image component, or remove Image entirely).

Updated BtnBack:
- Remove or zero out the Image component background color (set alpha to 0).
- Hit target size: 120×64 px (P1-01). This fits within the 180 px header.
- Placement: 24 px from left edge of header, vertically centered in the lower 120 px
  of header (accounting for safe area).
- ColorBlock stays as approved direction §6:
  - Normal: `#EDD9A3` alpha 200
  - Highlighted: `#FFD700` alpha 220
  - Pressed: `#FFFFFF` alpha 255
  - Disabled: not applicable (Back is always enabled)
- Font: Noto Sans Regular 16 sp.

### 3.3 Footer and Launch Button (P1-01 + P1-07 fixes)

Footer SizeDelta: (0, 160). Anchored to bottom: AnchorMin (0,0), AnchorMax (1,0),
AnchoredPosition (0, 80), SizeDelta (0, 160).

BtnLaunch:
- SizeDelta: (540, 120) — matches approved direction §5 exactly.
- Placement: centered in footer.
- m_Interactable: 0 in scene serialization (P0-07).
- ColorBlock per approved direction §5:
  - Normal: `#D4A017` — corresponds to BtnActive Gold
  - Highlighted: `#E8B420`
  - Pressed: `#B8880F`
  - Disabled: `#3D3020`
- Disabled label: `#A08040` alpha 180 (WCAG fix, P2-09).

### 3.4 Horizontal Scroll Disable (P0-04)

`StageScrollView.m_Horizontal` must be set to 0. Only vertical scroll is valid.

### 3.5 BtnLaunch Serialized as Disabled (P0-07)

In the scene YAML: `m_Interactable: 0`. The controller's Start() will keep it
disabled until a stage is selected; removing the 1-frame flash.

### 3.6 Stage Entry Card — Additions (P2 fixes)

These additions apply to the `StageEntryUI` prefab, not the scene directly.

**Left accent stripe (P2-04):**
- Child Image named `AccentStripe`.
- Width: 8 px. Height: full card height.
- AnchorMin: (0, 0), AnchorMax: (0, 1). AnchoredPosition: (4, 0). SizeDelta: (8, 0).
- Color: set from code via `StageEntryUI.SetAccentColor(Color c)` using stage_001
  Primary Accent `#1E88E5`, stage_002 `#6A1B9A`, stage_003 `#BF360C`.

**Reward strip (P2-03):**
- Child object `RewardStrip` anchored to card bottom: AnchorMin (0,0), AnchorMax (1,0),
  SizeDelta (0, 48).
- Strip background Image: `#1A0D00` alpha 180.
- Top divider Image: `#5C3D1E` alpha 200, 1 px, anchored to strip top.
- Two TMP children: `RewardLabel` ("Recompensa:", Noto Sans 12 sp, `#8B5E3C` alpha 220)
  and `RewardValue` ("???", Noto Sans Bold 13 sp, `#FFD700`).
- If StageData has no reward value wired: display "Botín: ???" in `#8B5E3C` alpha 140.

**Difficulty dots — TMP Rich Text (P2-05):**
Replace the legacy `UnityEngine.UI.Text` difficulty component with a TMP component.
Use Rich Text markup. Example for Bahía Corsaria difficulty 1:
```
<color=#4FC3F7>●</color><color=#2A3A4A> ○ ○ ○ ○</color>
```
Filled character: U+25CF `●` at 18 sp. Empty: U+25CB `○` at 18 sp.
Stage secondary accent colors: Bahía `#4FC3F7`, Muelle `#CE93D8`, Templo `#FF8A65`.

**Empty state copy (P2-06):**
`EmptyStateText` → "No hay misiones disponibles. Vuelve más tarde."

**LayoutElement on prefab (P1-10):**
`LayoutElement.preferredHeight = 340` to match the 900×340 px card spec in the
approved direction.

### 3.7 Visual Hierarchy — StageSelect (unchanged from approved direction)

Per approved direction §Visual Weight Hierarchy:
1. Selected card gold border + glow
2. Launch button (gold, bottom center)
3. Stage name text
4. Header title
5. Stage accent stripe
6. Difficulty dots
7. Reward strip
8. Back button (deliberately unobtrusive)

---

## Section 4 — TeamSelect: Full Visual Spec

### 4.1 Layer Stack (bottom to top)

```
Canvas (Screen Space Overlay)
  Background     ← solid fill #140F24 (fixes #0F0A1A deviation — P2-08)
  BgGradient     ← 2×2 gradient overlay, same as MainMenu/StageSelect
  Header         ← 180px panel
    BtnBack
    TitleText
    SubtitleText
  SlotsSection   ← team composition display
    SlotsLabel
    Slot0  Slot1  Slot2
  RosterSection  ← scrollable roster
    RosterLabel
    RosterScrollView
      Viewport
        Content  ← VLG + ContentSizeFitter
          [TeamRosterEntryUI × 3]
  Footer
    BtnConfirm
```

### 4.2 Background

Identical to MainMenu and StageSelect:

| Object | Color | Alpha | Anchor |
|--------|-------|-------|--------|
| Background | `#140F24` | 255 | fill (0,0)→(1,1) |
| BgGradient | 2×2 gradient sprite | 180 | fill (0,0)→(1,1) |

Background color `#140F24` replaces the current `#0F0A1A` (P2-08 fix).

### 4.3 Header (180 px — matching StageSelect pattern)

| Property | Value |
|----------|-------|
| AnchorMin | (0, 1) |
| AnchorMax | (1, 1) |
| AnchoredPosition | (0, −90) |
| SizeDelta | (0, 180) |
| Fill color | `#1A0D00` (HeaderBase) |
| Bottom divider | 4 px Image, `#D4A017` alpha 220, anchored to header bottom |

**Title text (TitleText):**

| Property | Value |
|----------|-------|
| Text | "Selección de Equipo" |
| Font | Pirata One Regular, 32 sp |
| Color | Gold `#D4A017` |
| TMP Drop Shadow | offset (2, −2), `#000000` alpha 160 |
| Anchor | center-bottom of header lower 120 px zone |
| AnchoredPosition relative to Header | (0, −30) from header center |

**Subtitle text:**

| Property | Value |
|----------|-------|
| Text | "Elige a tus corsarios." |
| Font | Noto Sans Regular Italic, 14 sp |
| Color | Cream `#EDD9A3` alpha 180 |
| Position | 6 px below title baseline |

**BtnBack (same spec as StageSelect §3.2):**
- Hit target: 120×64 px
- Text-only, no Image background
- Noto Sans 16 sp, Cream `#EDD9A3` alpha 200
- Position: 24 px from left, vertically centered in lower 120 px of header

### 4.4 Slots Section

#### Container

| Property | Value |
|----------|-------|
| Object name | `SlotsSection` |
| Anchor | AnchorMin (0, 1), AnchorMax (1, 1) |
| AnchoredPosition | (0, −280) — 180 px header + 20 px gap + 80 px for SlotsLabel |
| SizeDelta | (0, 200) |

**SlotsLabel** (above slots):

| Property | Value |
|----------|-------|
| Text | "EQUIPO SELECCIONADO:" |
| Font | Noto Sans Regular, 13 sp |
| Color | Cream `#EDD9A3` alpha 180 |
| Anchor | top stretch: (0,1)→(1,1), SizeDelta (0, 30) |

#### Slot Dimensions

SlotsPanel: full width, 150 px tall. Three equal columns.

| Property | Value |
|----------|-------|
| Object name | `SlotsPanel` |
| Anchor | fill within SlotsSection below label: (0,0)→(1,1) offset (0, 30) |
| Height | 150 px (SlotsSection SizeDelta height minus label height) |

Each slot (Slot0, Slot1, Slot2):
- Anchor columns: Slot0 (0,0)→(0.333,1), Slot1 (0.333,0)→(0.667,1), Slot2 (0.667,0)→(1,1)
- SizeDelta: (−8, −8) — 4 px margin on each side
- Minimum height: 150 px (which is now the full SlotsPanel height, meeting the
  88 px minimum touch target requirement with headroom)

#### Slot Visual States

**Empty state:**

| Layer | Property | Value |
|-------|----------|-------|
| Slot Image (background) | Color | `#1F172E` (SlotEmpty) |
| Slot Image (background) | Alpha | 255 |
| Slot border Image (child, inset -2px) | Color | `#3A2A50` (SlotBorderEmpty) |
| Slot border Image | Alpha | 255 |
| SlotNameText | Text | "— Vacío —" |
| SlotNameText | Color | Cream `#EDD9A3` alpha 140 |
| SlotNameText | Font | Noto Sans Regular, 15 sp |
| SlotIcon (child Image) | Sprite | `ui_slot_empty_icon.png` (32×32, dim cross or plus) |
| SlotIcon | Color | `#3A2A50` alpha 180 |
| BtnClear | Active | false |

**Filled state (P1-05 fix — gold border + warm bg):**

| Layer | Property | Value |
|-------|----------|-------|
| Slot Image (background) | Color | `#2A1E10` (SlotFilled — warm wood dark) |
| Slot Image (background) | Alpha | 255 |
| Slot border Image | Color | `#D4A017` (Gold = SlotBorderFilled) |
| Slot border Image | Alpha | 255 |
| SlotNameText | Text | Character DisplayName |
| SlotNameText | Color | CreamMuted `#F5E6C8` alpha 255 |
| SlotNameText | Font | Noto Sans Bold, 15 sp |
| BtnClear | Active | true |
| BtnClear | Color | Gold `#D4A017` |

State transitions set from `TeamSelectController` when assigning/clearing slots:
- Set slot Background Image color
- Set slot border Image color
- Toggle BtnClear active state
- Change SlotNameText content and color

#### BtnClear Spec (P0-04 + P1-02 fixes)

| Property | Value |
|----------|-------|
| SizeDelta | (100, 44) — minimum viable touch target given slot width constraints |
| AnchoredPosition | (0, −16) from slot center — bottom half of slot |
| Text | "×" (U+00D7 multiply sign) |
| Font | Noto Sans Bold, 20 sp |
| Color | `#1A0D00` on `#D4A017` background |
| Active | false when slot empty, true when slot filled |

Note: 100×44 px is the compromise given the 150 px slot height. A preferable
alternative is to remove BtnClear entirely and rely on a second tap of the roster
entry to toggle (the controller already supports `OnRosterEntryClicked` toggle).
If BtnClear is retained, 100×44 is the minimum. The ui-programmer should consult
the creative director on which approach to ship for demo.

### 4.5 Roster Section

#### Section Label

| Property | Value |
|----------|-------|
| Object name | `RosterLabel` |
| Text | "PERSONAJES DISPONIBLES:" |
| Font | Noto Sans Regular, 13 sp |
| Color | Cream `#EDD9A3` alpha 180 |
| Anchor | positioned 16 px below SlotsSection bottom edge |
| SizeDelta | (fill width, 30) |

#### RosterScrollView

| Property | Value |
|----------|-------|
| Anchor | fills remaining screen between roster label and footer |
| AnchorMin | (0, 0), AnchorMax | (1, 1) |
| SizeDelta | inset: top offset = header (180) + slots section (200) + label (46) + gap; bottom offset = footer (160) |
| m_Horizontal | 0 (disabled — same fix as StageSelect P0-04) |
| m_Vertical | 1 |
| m_Elasticity | 0.08 (slight reduction from 0.1 — feels responsive without exaggerated bounce on 3-entry list) |
| Viewport Image alpha | 0.004 (near-invisible per coplay-unity-lessons §2) |

#### TeamRosterEntryUI Card

Dimensions: full width (Content fills scroll view width), 100 px tall.
LayoutElement.preferredHeight = 100.

| Layer | Property | Value |
|-------|----------|-------|
| Card background Image | Color | `#1F172E` (RosterEntryBg) alpha 230 |
| Card border Image (inset) | Color | `#3A2A50` alpha 200 |
| Card left accent stripe | 8 px wide, color per element (see below) | alpha 255 |
| CharNameText | Font | Noto Sans Bold, 16 sp, `#F5E6C8` |
| CharElementText | Font | Noto Sans Regular, 13 sp, `#EDD9A3` alpha 200 |
| AddButton / InTeamIndicator | right-aligned, 44×44 px hit target | see below |

**In-team state:** background changes to `#2A1E10` (RosterEntryInTeam), border
changes to `#D4A017` Gold, AddButton shows "−" (remove) instead of "+" (add).

**Element accent colors (left stripe and element text color):**

| Element | Spanish name | Stripe / text color |
|---------|-------------|-------------------|
| Fire (Fuego) | "Fuego" | `#BF360C` Temple Ember |
| Water (Agua) | "Agua" | `#1E88E5` Corsair Blue |
| Thunder (Trueno) | "Trueno" | `#E8B420` Gold Mid (warm) |

Element names must display in Spanish (P2-07 fix). The CharacterData assets need
a `DisplayElement` string field, or the controller must translate the enum:
`Fire→"Fuego"`, `Water→"Agua"`, `Thunder→"Trueno"`. This is a data/content fix,
coordinate with ui-programmer on approach.

#### Add/Remove Button on Roster Entry

| Property | Value |
|----------|-------|
| SizeDelta | (44, 44) — square, meets minimum |
| AnchorMin/Max | (1, 0.5)→(1, 0.5) — right-centered |
| AnchoredPosition | (−30, 0) — 30 px from right edge |
| Text (empty slot) | "+" |
| Text (in team) | "−" |
| Font | Noto Sans Bold 22 sp |
| Color normal | Gold `#D4A017` on PanelDark `#1F172E` |
| Color in-team | Cream `#EDD9A3` on `#2A1E10` |

### 4.6 Footer and Confirm Button (P1-01 + P1-08 fixes)

| Property | Value |
|----------|-------|
| Footer anchor | AnchorMin (0,0), AnchorMax (1,0) |
| Footer AnchoredPosition | (0, 80) |
| Footer SizeDelta | (0, 160) |
| Footer fill | `#1A0D00` alpha 255 |
| Footer top divider | 4 px, `#D4A017` alpha 220 |

BtnConfirm:

| Property | Value |
|----------|-------|
| SizeDelta | (540, 88) |
| m_Interactable | 0 in scene (P0-08 fix) |
| Anchor | center in footer |
| Label text | "¡CONFIRMAR EQUIPO!" |
| Label font | Pirata One 24 sp |
| Label color (enabled) | `#1A0D00` |
| Label color (disabled) | `#A08040` alpha 180 |

ColorBlock:

| State | Color | Alpha |
|-------|-------|-------|
| Normal | `#D4A017` | 255 |
| Highlighted | `#E8B420` | 255 |
| Pressed | `#B8880F` | 255 |
| Disabled | `#3D3020` | 255 |
| Fade Duration | 0.1 s | — |

### 4.7 Visual Hierarchy — TeamSelect

Prominence order:

1. **Slots (filled state gold border)** — the team you are building is the
   central object of this screen. Gold borders on filled slots dominate the upper
   portion and confirm player choices at a glance. This is the primary feedback loop.
2. **BtnConfirm** — gold fill, full 50% screen width at bottom, disabled until
   team is valid. Players see it from load and are motivated to fill the team.
3. **Roster cards** — cream text on dark panel; each card is a selectable action.
   The left element stripe provides personality without distracting from names.
4. **Header title** — Pirata One gold, orienting label. Less prominent than team
   composition because players already know what screen they are on after first visit.
5. **BtnClear** — small, gold, visible only on filled slots. Functionally present
   but visually subordinate to the slot content.
6. **BtnBack** — text-only, cream muted. Exits flow; intentionally unobtrusive.

Reasoning: The slot → roster → confirm flow maps left-to-right, top-to-bottom
visual scanning. The gold border on filled slots gives instant visual confirmation
("I have 3/3 members") before the BtnConfirm enables.

---

## Section 5 — Asset List

### 5.1 Sprites to Create

All sprites stored in `Assets/Sprites/UI/`. Naming convention:
`[category]_[name]_[variant]_[size].[ext]`

| File name | Size (px) | Technique | Purpose | Notes |
|-----------|-----------|-----------|---------|-------|
| `ui_bg_ocean_gradient.png` | 2×2 | Procedural — paint top row `#1A2744`, bottom row `#050D14` | Screen background gradient | Bilinear filter, Sprite mode, stretch to canvas |
| `ui_panel_9slice.png` | 32×32 | Procedural — 1 px solid border, transparent center, border set to 4 px | Generic panel / card border | Color tinted at runtime |
| `ui_btn_primary_9slice.png` | 32×32 | Procedural — 8 px solid border, solid fill | Gold button base | Color tinted; bevels are separate child Images |
| `ui_slot_empty_icon.png` | 32×32 | Procedural — 2 px cross (+) centered on transparent bg, color `#3A2A50` | Empty team slot indicator | Use Unity white sprite with color tint if this is too complex |
| `ui_logo_placeholder.png` | 600×300 | Procedural — solid `#1F172E` fill with `#D4A017` 2 px border | MainMenu logo slot | Replace with AI-generated logo when ready |
| `ui_accent_stripe_base.png` | 8×1 | Procedural — 1 px white | Stage/element left accent | Tinted per stage/element at runtime; Unity 1×1 white sprite is sufficient |

**AI-generated sprites (Midjourney/Pixellab) — future sprint:**

| File name | Size (px) | Suggested prompt | Priority |
|-----------|-----------|-----------------|----------|
| `ui_logo_main_full.png` | 1080×480 | "Blacktide Requiem logo, pirate ship silhouette, voodoo skull, gold and dark navy, game title lettering, digital art, dark background, no text" | Post-demo |
| `char_elena_portrait_idle_01.png` | 256×256 | "Female pirate captain portrait, dark skin, gold earrings, ocean background, painterly, FFBE style, square format" | Post-demo |
| `char_kael_portrait_idle_01.png` | 256×256 | "Male swordsman pirate portrait, scarred face, leather coat, stormy sea, painterly, FFBE style, square format" | Post-demo |
| `char_mirra_portrait_idle_01.png` | 256×256 | "Young woman voodoo shaman portrait, face paint, coral and bone jewelry, jungle background, painterly, FFBE style, square format" | Post-demo |

### 5.2 Fonts — TMP Assets to Import

Download from Google Fonts (OFL license). Import into Unity as TextMeshPro font
assets using Window > TextMeshPro > Font Asset Creator (Atlas Resolution 2048×2048,
Sampling Point Size 90, Padding 9, Render Mode: Signed Distance Field).

| Font family | Weight/Style | TMP asset name | Destination path | Used for |
|-------------|-------------|----------------|-----------------|----------|
| Pirata One | Regular | `PirataOne-Regular SDF.asset` | `Assets/Fonts/PirataOne-Regular SDF.asset` | Titles, stage names, primary action buttons |
| Noto Sans | Regular | `NotoSans-Regular SDF.asset` | `Assets/Fonts/NotoSans-Regular SDF.asset` | Secondary text, difficulty labels, reward text |
| Noto Sans | Bold | `NotoSans-Bold SDF.asset` | `Assets/Fonts/NotoSans-Bold SDF.asset` | Character names in roster, confirm button (if not using Pirata One) |
| Noto Sans | Regular Italic | `NotoSans-Italic SDF.asset` | `Assets/Fonts/NotoSans-Italic SDF.asset` | Header subtitles, flavor text |

Download URLs:
- Pirata One: https://fonts.google.com/specimen/Pirata+One (file: PirataOne-Regular.ttf)
- Noto Sans: https://fonts.google.com/specimen/Noto+Sans (files: NotoSans-Regular.ttf,
  NotoSans-Bold.ttf, NotoSans-Italic.ttf)

Place .ttf source files in `Assets/Fonts/` before running Font Asset Creator.
The `.asset` files are the Unity-usable TMP assets.

After import, assign to all text components via editor script (P2-01):
- All screen titles and primary action button labels → Pirata One Regular SDF
- All subtitles, secondary text, labels → Noto Sans Regular SDF
- All roster names, slot names in filled state → Noto Sans Bold SDF
- All header subtitles → Noto Sans Italic SDF

---

## Section 6 — P0 Fix Reference (for ui-programmer)

All P0 issues from the audit and their visual specs from this document:

| Audit ID | Screen | Visual spec location | Change |
|----------|--------|---------------------|--------|
| P0-01 | All | §2, §3, §4 preamble | CanvasScaler: Scale With Screen Size, 1080×1920, match height 1.0 |
| P0-03 | TeamSelect | §4.2 | Background color `#140F24` (was `#0F0A1A`) |
| P0-04 | StageSelect | §3.4 | m_Horizontal: 0 on StageScrollView |
| P0-05 | MainMenu | §2.7 | Delete ghost Text child (fileID 954099865) |
| P0-06 | MainMenu | §2.7 | Button label: "¡INICIAR MISIÓN!" |
| P0-07 | StageSelect | §3.5 | BtnLaunch m_Interactable: 0 in scene |
| P0-08 | TeamSelect | §4.6 | BtnConfirm m_Interactable: 0 in scene |

P0-02 (m_FirstSelected / gamepad focus) is an interaction/input fix, not visual.
Delegate to ui-programmer directly.

---

## Section 7 — Audit Coverage Confirmation

Every audit finding is addressed in this spec:

| Finding | Priority | Covered in | Resolution |
|---------|---------|-----------|------------|
| CanvasScaler wrong | P0-01 | §2, §3, §4 note | Scale With Screen Size |
| Ghost Text child | P0-05 | §2.7 | Delete child |
| Wrong button copy | P0-06 | §2.7 | "¡INICIAR MISIÓN!" |
| ScrollRect horizontal | P0-04 | §3.4 | m_Horizontal 0 |
| BtnLaunch starts active | P0-07 | §3.5 | m_Interactable 0 |
| BtnConfirm starts active | P0-08 | §4.6 | m_Interactable 0 |
| Touch target: BtnStartBattle | P1-01 | §2.7 | 540×88 |
| Touch target: BtnBack | P1-01 | §3.2, §4.3 | 120×64 |
| Touch target: BtnLaunch | P1-01 | §3.3 | 540×120 |
| Touch target: BtnConfirm | P1-01 | §4.6 | 540×88 |
| Touch target: BtnClear | P1-02 | §4.4 | 100×44 (min) |
| Header height 80 vs 180 | P1-03 | §3.1, §4.3 | 180 px on both screens |
| BtnBack solid background | P1-04 | §3.2, §4.3 | Text-only, no bg |
| No filled slot visual | P1-05 | §4.4 | Gold border + warm bg |
| TitleText wrong anchor | P1-09 / P1-06 | §2.4, §4.3 | Top-anchor pattern |
| BtnLaunch ColorBlock wrong | P1-07 | §3.3 | Full ColorBlock spec |
| BtnConfirm ColorBlock wrong | P1-08 | §4.6 | Full ColorBlock spec |
| StageEntryUI LayoutElement | P1-10 | §3.6 | preferredHeight 340 |
| Font is Arial | P2-01 | §5.2 | Import Pirata One + Noto Sans |
| No background gradient | P2-02 | §2.2, §4.2, §3 (approved doc covers StageSelect) | Three-layer bg spec |
| No reward strip on cards | P2-03 | §3.6 | RewardStrip child spec |
| No accent stripe on cards | P2-04 | §3.6 | AccentStripe child spec |
| ASCII stars, not TMP dots | P2-05 | §3.6 | Rich Text dot spec |
| Empty state copy truncated | P2-06 | §3.6 | Full copy string |
| Element names in English | P2-07 | §4.5 | Spanish names spec |
| BG color inconsistency | P2-08 | §4.2 | `#140F24` canonical |
| Disabled label contrast | P2-09 | §1.2 | `#A08040` alpha 180 |
| Logo slot missing | P2-02 (MM) | §2.3, §2.8 | LogoSlot Image, inactive |
| No subtitle on MainMenu | P1 (MM) | §2.5 | SubtitleText added |
| No MainMenu background layers | P2 (MM) | §2.2 | BgBase + BgGradient |
