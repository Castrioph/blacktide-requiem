# S3-11 UX Audit — MainMenu, StageSelect, TeamSelect
> Author: UX Designer agent
> Date: 2026-06-12
> Target: Unity 6.3 UGUI, 1080×1920 portrait reference
> Method: Static YAML analysis + C# script review (editor offline)

---

## Critical Finding Before Section 1

**Scene file duplication detected.** There are two copies of every scene:

| Scene | Path in Scenes/ | Path at Assets/ root | Used by build? |
|-------|----------------|---------------------|----------------|
| MainMenu | Assets/Scenes/MainMenu.unity | Assets/MainMenu.unity | Unknown |
| StageSelect | Assets/Scenes/StageSelect.unity | Assets/StageSelect.unity | Unknown |
| TeamSelect | N/A | Assets/TeamSelect.unity | Yes (wrong location) |

- `Assets/Scenes/StageSelect.unity` contains only a Main Camera — it is empty.
- `Assets/TeamSelect.unity` is at the Assets root, not in Assets/Scenes/ — this
  was already flagged in the session state as a known issue.
- All functional content was read from the root-level copies (`Assets/MainMenu.unity`,
  `Assets/StageSelect.unity`, `Assets/TeamSelect.unity`). Those are the active scenes.
- **P0 fix**: Move `Assets/TeamSelect.unity` to `Assets/Scenes/TeamSelect.unity` and
  delete the empty `Assets/Scenes/StageSelect.unity` stub. Update Build Settings.

---

## Global Finding: CanvasScaler is Wrong on All Three Screens

All three Canvases share the same CanvasScaler settings:

```
m_UiScaleMode: 0           (Constant Pixel Size — NOT Scale With Screen Size)
m_ScaleFactor: 1
m_ReferenceResolution: {x: 800, y: 600}   (unused in mode 0, but signals intent)
```

**Impact:** The UI renders at a fixed physical pixel size regardless of resolution.
On a 1080×1920 device the UI is not scaled at all — elements designed in Unity's
800×600 Game view will appear far too small on a real device and will not adapt to
any other resolution.

**Required fix (P0):** Change all three Canvases to:
```
m_UiScaleMode: 1           (Scale With Screen Size)
m_ReferenceResolution: {x: 1080, y: 1920}
m_ScreenMatchMode: 1       (Match Width Or Height)
m_MatchWidthOrHeight: 1.0  (match height, correct for portrait)
```

All size values documented in the rest of this audit assume the 1080×1920 reference.

---

## Screen 1: MainMenu

### 1.1 Hierarchy (read from Assets/MainMenu.unity)

```
Canvas  [CanvasScaler: ConstantPixelSize, factor 1]
  EventSystem
  GameFlowManager
  Main Camera
  MenuPanel  [RectTransform: anchor (0,0)→(1,1), fill screen]
    TitleText
    BtnStartBattle
      Text  (button label child — named "Text")
      Text  (unnamed second Text child — empty string, size 14, white)
```

**Note:** BtnStartBattle has TWO Text children. One has content "Start Battle"
(size 24, cream color), the other is empty (size 14, white, anchored at center
50%/50% with SizeDelta 100×100). The empty one is a leftover artifact.

### 1.2 Measured RectTransform Values

| GameObject | AnchorMin | AnchorMax | AnchoredPos | SizeDelta |
|-----------|-----------|-----------|-------------|-----------|
| Canvas | (0,0) | (0,0) | (0,0) | (0,0) |
| MenuPanel | (0,0) | (1,1) | (0,0) | (0,0) |
| TitleText | (0.5,0.5) | (0.5,0.5) | (0, +100) | (600, 80) |
| BtnStartBattle | (0.5,0.5) | (0.5,0.5) | (0, -40) | (250, 60) |

**Canvas RectTransform anomaly:** `m_LocalScale: {x:0, y:0, z:0}` — the Canvas
root RectTransform has scale zero. This is normal for a Screen Space Overlay Canvas
(Unity manages it), but worth noting.

### 1.3 Detected Content and Colors

| Element | Text | Font size | Color (YAML) | Color (hex approx) |
|---------|------|-----------|-------------|-------------------|
| TitleText | "BLACKTIDE REQUIEM" | 48 | r:0.95 g:0.78 b:0.25 | ~#F2C740 (Gold) |
| BtnStartBattle image | — | — | r:0.6 g:0.42 b:0.1 | ~#996B1A (BtnActive) |
| Button label | "Start Battle" | 24 | r:0.95 g:0.9 b:0.75 | ~#F2E5BF (Cream) |
| MenuPanel background | — | — | r:0.08 g:0.06 b:0.14 | #140F24 (BgDark) |

### 1.4 UX Problems Detected

**P0 — Wrong copy on button:**
The button label reads "Start Battle" (English). The approved visual direction and
established convention for the game is Spanish. This should read "¡Iniciar Batalla!"
or simply "¡ZARPAR!" to match the StageSelect pattern. Confirmed in script: the text
is set in the scene YAML, not in code.

**P0 — Ghost Text child on BtnStartBattle:**
The second unnamed Text child (fileID 954099865, text="", size 14, color white,
anchor center, SizeDelta 100×100) will overlap the button label at runtime and
could intercept raycasts. It should be deleted.

**P1 — Title position uses center-anchor with absolute offset:**
TitleText is anchored at (0.5, 0.5) with offset (0, +100). This places it 100px
above screen center in the reference resolution. On a taller screen (correct for
1080×1920 portrait) the title will sit in the lower half of the screen rather than
the upper area where branding belongs. The anchor should be top-center with a fixed
top offset.

**P1 — Button too small for touch:**
BtnStartBattle SizeDelta is 250×60 px (in the 800×600 Constant Pixel Size context).
At 1:1 scale on an 1080-wide screen this is only 23% of screen width and 60px tall —
below the 88px minimum touch target. After fixing the CanvasScaler with 1080×1920
reference, the button should be resized to at least 540×88 to meet the touch target
floor.

**P1 — No subtitle / screen context copy:**
There is no subtitle text identifying what the player should do. The screen jumps
directly to the title and one button. For a first-boot experience this is functional
but offers no thematic grounding. The visual direction for StageSelect includes a
subtitle pattern; MainMenu should match.

**P2 — No background hierarchy:**
MenuPanel uses a single solid color (BgDark). The StageSelect visual direction
specifies a three-layer background (solid base + baked gradient + noise tint). The
MainMenu has none of that. Visual inconsistency between screens.

**P2 — No logo placeholder slot:**
Sprites folder contains `title_logo_placeholder.png` but there is no Image component
in the hierarchy using it. The title is pure text. An image slot for the eventual
logo should exist as an inactive placeholder.

**P2 — Font is Unity default (Arial):**
All text in all three screens uses `{fileID: 10102, guid: 0000000000000000e000000000000000}`
which is Unity's built-in Arial. The visual direction specifies Pirata One for titles
and Noto Sans for secondary text. No custom fonts are wired.

### 1.5 ASCII Wireframe — Target State

```
┌─────────────────────────────┐  1080px wide
│                             │
│   [LOGO IMAGE PLACEHOLDER]  │  ← top ~25% of screen
│                             │
│   BLACKTIDE REQUIEM         │  ← Pirata One 48sp, gold #D4A017
│   "El mar cobra lo suyo."   │  ← subtitle Noto Sans Italic 16sp, cream
│                             │
│                             │  ← decorative separator / wave art
│                             │
│   ┌─────────────────────┐   │
│   │    ¡INICIAR MISIÓN! │   │  ← 540×88px, gold btn #D4A017
│   └─────────────────────┘   │
│                             │
└─────────────────────────────┘
```

### 1.6 Data Sources for This Screen

| Displayed element | Source |
|------------------|--------|
| Nothing from game state | MainMenuController has no reads; only fires GameFlowManager.LoadStageSelect() |

---

## Screen 2: StageSelect

### 2.1 Hierarchy (read from Assets/StageSelect.unity)

```
Canvas  [CanvasScaler: ConstantPixelSize, factor 1]
  StageSelectController (on Canvas root)
  EventSystem
  Main Camera
  Background  [anchor (0,0)→(1,1), fill screen, color BgDark]
  Header      [anchor (0,1)→(1,1), pos (0,-40), size (0,80)]
    TitleText
    BtnBack   [anchor (0,0.5)→(0,0.5), pos (60,0), size (100,50)]
      Text: "< Volver"
  StageScrollView  [anchor (0,0)→(1,1), SizeDelta (0,-160)]
    Viewport  [anchor (0,0)→(1,1)]
      Content  [anchor (0,1)→(1,1), pivot (0.5,1)]
        ← stage entries instantiated at runtime
  Footer  [anchor (0,0)→(1,0), pos (0,40), size (0,80)]
    BtnLaunch  [anchor (0.5,0.5)→(0.5,0.5), pos (0,0), size (220,55)]
      Text: "¡ZARPAR!"
  EmptyStateText  [anchor (0.5,0.5)→(0.5,0.5), pos (0,0), size (500,60)]
```

### 2.2 Measured RectTransform Values

| GameObject | AnchorMin | AnchorMax | AnchoredPos | SizeDelta |
|-----------|-----------|-----------|-------------|-----------|
| Background | (0,0) | (1,1) | (0,0) | (0,0) |
| Header | (0,1) | (1,1) | (0,-40) | (0,80) |
| BtnBack | (0,0.5) | (0,0.5) | (60,0) | (100,50) |
| StageScrollView | (0,0) | (1,1) | (0,0) | (0,-160) |
| Content | (0,1) | (1,1) | (0,0) | (0,0) |
| Footer | (0,0) | (1,0) | (0,40) | (0,80) |
| BtnLaunch | (0.5,0.5) | (0.5,0.5) | (0,0) | (220,55) |
| EmptyStateText | (0.5,0.5) | (0.5,0.5) | (0,0) | (500,60) |

### 2.3 Detected Content and Colors

| Element | Text / Color | Notes |
|---------|-------------|-------|
| Background | #140F24 (BgDark) | Solid flat, no gradient |
| Header | #1F172E (PanelDark) | 80px tall — visual direction spec is 180px |
| TitleText | "SELECCIÓN DE MISIÓN", 28sp, gold | Direction spec is 32sp, "Seleccionar Misión" |
| BtnBack image | #996B1A (BtnActive warm gold) | Has solid fill — spec says text-only, no background |
| BtnBack label | "< Volver", 20sp, cream | Size spec is 16sp Noto Sans |
| BtnLaunch image | #3D3020 (disabled state initial) | Correct — starts disabled |
| BtnLaunch label | "¡ZARPAR!", 24sp, cream | Direction spec is 26sp Pirata One, color #1A0D00 |
| Footer | #1F172E (PanelDark) | 80px. Direction spec does not name a footer object |
| EmptyStateText | "No hay misiones disponibles." | Partial — direction spec has "Vuelve más tarde." |

**ScrollRect settings detected:**
- `m_Horizontal: 1` — horizontal scroll is enabled on a vertical stage list. This is wrong.
- `m_Vertical: 1` — correct.
- No scrollbar assigned. Correct per spec.

### 2.4 UX Problems Detected

**P0 — ScrollRect has horizontal scroll enabled:**
`StageScrollView` has `m_Horizontal: 1` and `m_Vertical: 1`. On a vertical card
list the player can swipe horizontally and lose cards off-screen with no visual
feedback. Should be `m_Horizontal: 0`.

**P0 — BtnLaunch starts with m_Interactable: 1 in scene:**
The YAML shows `m_Interactable: 1` for BtnLaunch. The controller calls
`SetLaunchInteractable(false)` in Start(), so it corrects at runtime, but for one
frame the button is fully active. If a player taps very fast after scene load they
could trigger a launch with no stage selected (`_selectedStage == null`), which
OnLaunchClicked() guards against with a null-check — so no crash, but unexpected.
The scene should serialize BtnLaunch as `m_Interactable: 0` to prevent any flash.

**P0 — Header height 80px vs spec 180px:**
The Header `SizeDelta: (0, 80)` places the `TitleText` and `BtnBack` in an 80px
band. The visual direction specifies 180px to accommodate a safe-area notch zone
and the subtitle text. The current header gives no room for a subtitle and is too
cramped for the approved design.

**P1 — BtnBack has solid gold background:**
BtnBack has an Image component with color #996B1A (the gold-active button color).
The visual direction specifies BtnBack as text-only with no background. The current
state makes Back visually identical in weight to the Launch button, violating the
visual hierarchy rule "Back button deliberately unobtrusive."

**P1 — BtnLaunch too small:**
`SizeDelta: (220, 55)` at Constant Pixel Size. The visual direction spec is 540×120px.
Even before the CanvasScaler fix, 55px height is below the 88px touch target minimum.

**P1 — BtnBack hit target 100×50px is below minimum:**
50px height is below 88px minimum. The parent Header is only 80px tall which makes
it structurally impossible to fit an 88px hit target. This is a cascading problem
from the Header height issue.

**P1 — Stage card entries have no LayoutElement in prefab (cannot verify from scene):**
At runtime, `StageEntryUI` prefab is instantiated into the Content VLG. The
`coplay-unity-lessons.md` checklist requires every prefab child to have
`LayoutElement.preferredHeight` set. The stage entry prefab GUID is
`3e84b90ba20f46140a84b969f137ac57` — this cannot be read from the scene YAML. A
separate prefab audit is needed to confirm LayoutElement is present.

**P1 — StageEntryUI only displays name and difficulty; no reward preview:**
The visual direction specifies a reward strip on each card (section 8). The
`StageEntryUI` script has no reference to a reward display element. The script reads
`stageData.DisplayName` and `stageData.DifficultyLevel` only.

**P1 — Difficulty uses ASCII stars not TMP Rich Text dots:**
`BuildDifficultyString()` in StageEntryUI produces `★★☆☆☆` using Unicode characters
on a legacy `UnityEngine.UI.Text`. The visual direction specifies TMP Rich Text with
per-stage colored dot characters. This is partially about visual fidelity but also
functional: star encoding can produce garbled output on devices without the font.

**P2 — No stage accent stripe in scene or prefab spec:**
The visual direction section 7 specifies per-stage left accent stripes (Corsair Blue,
Voodoo Violet, Temple Ember). No accent stripe Image exists in the current StageEntryUI
architecture (only `_stageName`, `_stageDifficulty`, `_border`, `_btnSelect`).

**P2 — EmptyStateText copy is truncated:**
Scene has `"No hay misiones disponibles."` — the visual direction spec is
`"No hay misiones disponibles. Vuelve más tarde."` Copy mismatch.

**P2 — ScrollRect uses m_Elasticity: 0.1 which may feel stiff:**
With only 3 stage cards the list may not need to scroll at all on most displays.
Consider reducing elasticity or clamping scroll behavior for short lists.

### 2.5 ASCII Wireframe — Target State

```
┌─────────────────────────────┐  1080px
│ [← Volver]  SELECCIONAR     │  180px header, gold divider bottom
│              MISIÓN         │  Pirata One 32sp gold
│  "Elige tu destino,         │  Noto Sans Italic 14sp cream
│   corsario."                │
├─────────────────────────────┤
│ ┌─────────────────────────┐ │  Card 900×340px, margin 90px each side
│ │ [BLUE]  Bahía Corsaria  │ │  Left accent stripe, stage name
│ │ ★☆☆☆☆  Fácil           │ │  Difficulty dots + label
│ │  Botín: 50 Doblones     │ │  Reward strip
│ └─────────────────────────┘ │
│ ┌─────────────────────────┐ │
│ │ [PURPLE] Muelle Maldito │ │
│ │  ★★☆☆☆  Normal         │ │
│ │  Botín: 80 Doblones     │ │
│ └─────────────────────────┘ │
│ ┌─────────────────────────┐ │
│ │ [EMBER]  Templo Vudú    │ │
│ │  ★★★☆☆  Difícil        │ │
│ │  Botín: ???             │ │
│ └─────────────────────────┘ │
├─────────────────────────────┤
│     ┌───────────────────┐   │  Footer, BtnLaunch 540×120px
│     │    ¡ZARPAR!       │   │  Disabled initially
│     └───────────────────┘   │
└─────────────────────────────┘
```

### 2.6 Data Sources for This Screen

| Displayed element | Source |
|------------------|--------|
| Stage names | `StageData.DisplayName` via `StageRegistry.Stages` (ScriptableObject) |
| Difficulty level | `StageData.DifficultyLevel` (int 1-5) |
| Stage list | `_stageRegistry` serialized reference on Canvas MonoBehaviour |
| Selected stage passed forward | `GameFlowManager.Instance.SelectedStage = _selectedStage` |

---

## Screen 3: TeamSelect

### 3.1 Hierarchy (read from Assets/TeamSelect.unity)

```
Canvas  [CanvasScaler: ConstantPixelSize, factor 1]
  TeamSelectController (on Canvas root implied — not visible in m_Name scan
                        but controller wiring is in the scene)
  EventSystem
  Main Camera
  Background  [anchor (0,0)→(1,1), fill screen, color #0F0A1A]
  Header      [anchor (0,1)→(1,1), pos (0,-40), size (0,80)]
    TitleText: "SELECCIÓN DE EQUIPO", 28sp, gold
    BtnBack   [anchor... see §3.2]
  SlotsPanel  [anchor (0,1)→(1,1), pos (0,-140), size (0,120)]
    Slot0  [anchor (0,0)→(0.333,1)]
      SlotNameText
      BtnClear
    Slot1  [anchor (0.333,0)→(0.667,1)]
      SlotNameText
      BtnClear
    Slot2  [anchor (0.667,0)→(1,1)]
      SlotNameText
      BtnClear
  RosterScrollView  [with Viewport → Content (VLG + ContentSizeFitter)]
  Footer  [anchor (0,0)→(1,0)]
    BtnConfirm  [size (220,55)]
      Text: (not read — child structure mirrors BtnLaunch)
```

**Note:** There are two objects named `TitleText` in the TeamSelect YAML (fileIDs
367 and 604015064). This happens because the Header contains both a TitleText for
the screen title and a second TitleText in a sub-object. The screen title reads
"SELECCIÓN DE EQUIPO" (28sp, gold). The second TitleText is inside a Header sub-group
that itself has a parent `Header` (fileID 302801336, pos (0,-40), size (0,80)) —
this is the RosterScrollView header sub-bar, not the screen header.

### 3.2 Measured RectTransform Values

| GameObject | AnchorMin | AnchorMax | AnchoredPos | SizeDelta |
|-----------|-----------|-----------|-------------|-----------|
| Background | (0,0) | (1,1) | (0,0) | (0,0) |
| Header (screen) | Not directly read — mirrors StageSelect Header pattern | | |
| SlotsPanel | (0,1) | (1,1) | (0,-140) | (0,120) |
| Slot0 | (0,0) | (0.333,1) | (0,0) | (-8,-8) |
| Slot1 | (0.333,0) | (0.667,1) | (0,0) | (-8,-8) |
| Slot2 | (0.667,0) | (1,1) | (0,0) | (-8,-8) |
| BtnClear (in Slot2) | (0.5,0.5) | (0.5,0.5) | (0,-16) | (80,30) |
| BtnConfirm | (0.5,0.5) | (0.5,0.5) | (0,0) | (220,55) |
| Content (roster) | (0,1) | (1,1) | (0,0) | (0,0) pivot (0.5,1) |

### 3.3 Detected Content and Colors

| Element | Text / Color | Notes |
|---------|-------------|-------|
| Background | r:0.06 g:0.04 b:0.10 | #0F0A1A — slightly darker than BgDark (#140F24) |
| SlotsPanel | r:0.06 g:0.04 b:0.10 | Same very dark color |
| Header (sub) | r:0.12 g:0.09 b:0.18 | #1F172E PanelDark |
| Slot0/1/2 images | r:0.12 g:0.09 b:0.18 | #1F172E PanelDark |
| SlotNameText | "— Vacío —", 18sp, cream | Correct empty-state label |
| BtnClear | r:0.6 g:0.42 b:0.10 | Gold-active color, 80×30 SizeDelta |
| BtnConfirm image | r:0.24 g:0.19 b:0.13 | Disabled initial state |
| BtnClear text "×" | 18sp, cream | Unicode multiply sign (\xD7) |
| Content VLG | padding L:8 R:8 T:4 B:4, spacing:4 | Same as StageSelect Content |

### 3.4 UX Problems Detected

**P0 — BtnClear hit target is 80×30 px — critically too small:**
`SizeDelta: (80, 30)`. 30px height is less than one-third of the 88px minimum touch
target. A player filling a slot must tap an extremely small button to clear it.
Minimum recommended size is 88×88px. Given the slot width (one-third of 1080px =
360px) there is room to go wider — suggest 100×60px minimum, or remove BtnClear
entirely and use a second tap on the roster entry to toggle (the controller already
supports this via OnRosterEntryClicked).

**P0 — BtnConfirm starts m_Interactable: 1 in scene (same issue as BtnLaunch):**
Same one-frame flash risk. Should be serialized as `m_Interactable: 0`.

**P1 — SlotsPanel height 120px divided by 3 slots gives ~37px slot height:**
SlotsPanel SizeDelta is (0, 120). Three slots stacked or side-by-side in 120px means
each slot is 120px tall. But each slot's BtnClear is only 30px — it sits in the
lower half of the slot. The SlotNameText anchors to (0,0.5)→(1,1) occupying the
upper half of the slot. The slot does not have a border/highlight to show it is
"filled" vs "empty" — the only indicator is the SlotNameText changing from
"— Vacío —" to the character name, no color change on the slot background.

**P1 — No visual distinction between filled and empty slots:**
When a slot is filled, only `_slotNameTexts[i].text` changes. The slot Image stays
`#1F172E` regardless of state. There is no gold border or color highlight on filled
slots, no checkmark icon. A player cannot quickly verify team composition at a glance.
Need: filled state should change slot border/background color (suggest BorderSelected
gold #D4A017 applied to the slot Image border layer, matching StageEntryUI pattern).

**P1 — BtnConfirm size 220×55px is too small:**
Same issue as BtnLaunch on StageSelect. Should be 540×88px minimum in 1080×1920 space.
Label content for BtnConfirm was not directly read; check that it reads "¡Zarpar!"
or appropriate Spanish copy. The controller calls `LoadCombat()` on confirm.

**P1 — No character element/class indicator beyond text:**
`TeamRosterEntryUI` displays `_charName` and `_charElement` (Element.ToString() —
enum name, likely "Fire", "Water", "Thunder" etc. in English). No icon slot, no
color coding per element. For a roster of exactly 3 this is functional but bare.

**P1 — Background color inconsistency across screens:**
TeamSelect Background is `#0F0A1A`. MainMenu and StageSelect Background is `#140F24`.
These are close but different. Should share BgDark exactly.

**P2 — Three-column slot layout may be confusing on mobile:**
Slots Slot0/1/2 are laid out as three equal-width columns (each one-third of canvas
width). At 1080px reference, each slot is 360px wide. This is workable for landscape
but in portrait a linear vertical stack (slot 1, slot 2, slot 3 top-to-bottom) reads
more naturally for a selection-then-confirm flow.

**P2 — Roster scroll Content VLG spacing is minimal (4px):**
The same 8/8/4/4 padding and 4px spacing from StageSelect was reused here. Roster
entries are likely ~90px tall each (based on LayoutElement.preferredHeight from
lessons doc). 3 entries at 90px + 4px spacing = ~282px — fits on screen without
scrolling, but if more characters are added the scroll activates with no visual
affordance (no scrollbar assigned).

**P2 — BtnBack on TeamSelect is the same gold-background style as StageSelect:**
Same visual hierarchy problem: back button too heavy.

### 3.5 ASCII Wireframe — Target State

```
┌─────────────────────────────┐  1080px
│ [← Volver]  SELECCIÓN       │  Header 160px
│              DE EQUIPO      │  28sp gold Pirata One
├─────────────────────────────┤
│ EQUIPO SELECCIONADO:        │  Label, Noto Sans 14sp cream
│ ┌──────────┐┌──────────┐┌──────────┐ │  3 slots horizontal, 120px tall each
│ │[SLOT 1]  ││[SLOT 2]  ││[SLOT 3]  │ │  Gold border when filled
│ │— Vacío — ││— Vacío — ││— Vacío — │ │  Character name + [×] when filled
│ └──────────┘└──────────┘└──────────┘ │  88px min height
├─────────────────────────────┤
│ PERSONAJES DISPONIBLES:     │  Section label
│ ┌─────────────────────────┐ │  Roster card 90px height
│ │  Elena  [Agua]      [+] │ │  Gold border when in team
│ └─────────────────────────┘ │
│ ┌─────────────────────────┐ │
│ │  Kael   [Fuego]     [+] │ │
│ └─────────────────────────┘ │
│ ┌─────────────────────────┐ │
│ │  Mirra  [Trueno]    [+] │ │
│ └─────────────────────────┘ │
├─────────────────────────────┤
│     ┌───────────────────┐   │  Footer, BtnConfirm 540×88px
│     │  ¡CONFIRMAR EQUIPO│   │  Gold when valid team, dark when not
│     └───────────────────┘   │
└─────────────────────────────┘
```

### 3.6 Data Sources for This Screen

| Displayed element | Source |
|------------------|--------|
| Roster characters | `_roster` (CharacterData[] serialized in scene) |
| Character names | `CharacterData.DisplayName` |
| Character element | `CharacterData.Element.ToString()` (English enum name) |
| Slot state | `TeamComposition.GetSlot(i)` (in-memory, runtime only) |
| Valid team check | `TeamComposition.IsValid` (all 3 slots filled) |
| Team passed forward | `GameFlowManager.Instance.SelectedTeam = _composition` |

---

## Section 2: Input and Navigation Map

### 2.1 EventSystem Configuration (all three scenes)

| Scene | EventSystem | InputModule | m_FirstSelected | Navigation events |
|-------|------------|-------------|----------------|-------------------|
| MainMenu | Present | InputSystemUIInputModule | fileID: 0 (NONE) | Enabled |
| StageSelect | Present | InputSystemUIInputModule | fileID: 0 (NONE) | Enabled |
| TeamSelect | Present | InputSystemUIInputModule | fileID: 0 (NONE) | Enabled |

All three scenes use `InputSystemUIInputModule` (New Input System) with a shared
`ActionsAsset` (guid `ca9f5fa95ffab41fb9a615ab714db018`). Navigation events are
enabled on all three.

**Critical finding:** `m_FirstSelected: {fileID: 0}` on all three EventSystems.

This means gamepad/keyboard navigation has NO starting focus when the scene loads.
The player must click/tap something first to give focus before D-pad or Tab/arrow
keys do anything. On gamepad, the first button press will do nothing visible.

### 2.2 Button Navigation Mode

Both `BtnLaunch` (StageSelect) and `BtnConfirm`/`BtnClear` (TeamSelect) have
`m_Navigation: m_Mode: 3` (Automatic). Automatic mode lets Unity infer neighbors
from spatial proximity. With only 2-3 buttons per screen this is acceptable, but
it will not navigate correctly into the ScrollRect content (stage cards, roster
entries) because ScrollRect intercepts navigation input.

### 2.3 Gamepad Navigation — Current State Assessment

| Screen | Gamepad today | Reason |
|--------|--------------|--------|
| MainMenu | NOT functional | No firstSelected; BtnStartBattle is never focused |
| StageSelect | NOT functional | No firstSelected; scroll list entries need explicit navigation; BtnBack and BtnLaunch never auto-focused |
| TeamSelect | NOT functional | No firstSelected; slot BtnClear buttons are tiny and not in auto-nav chain |

**What works today:** Mouse click and touch on all screens (pointer events via
InputSystemUIInputModule). Keyboard Tab will cycle focusable elements only after
the first mouse click establishes focus.

### 2.4 Input Map (target state)

| Action | Mouse/Touch | Keyboard | Gamepad |
|--------|------------|----------|---------|
| Start / confirm | Click button | Enter/Space on focus | South button (A/Cross) |
| Back / cancel | Click back button | Escape | East button (B/Circle) |
| Navigate list | Scroll wheel / drag | Up/Down arrows | D-pad Up/Down |
| Select stage card | Click card | Tab to card + Enter | D-pad + South |
| Clear team slot | Click BtnClear | Tab to BtnClear + Enter | West button (X/Square) on slot |

---

## Section 3: Accessibility Checklist

### 3.1 Per-Feature Assessment

| Criterion | MainMenu | StageSelect | TeamSelect | Status |
|-----------|---------|------------|------------|--------|
| Keyboard only usable | FAIL — no firstSelected | FAIL — no firstSelected | FAIL — no firstSelected | Global P0 |
| Gamepad only usable | FAIL | FAIL | FAIL | Global P0 |
| Text readable at minimum size | WARN — 14sp ghost text | WARN — 14sp EmptyState on 800ref | WARN — 18sp in 800ref context | P1 |
| No color-only information | FAIL — disabled buttons differ only by color (no icon, no text change) | FAIL | FAIL | P1 |
| No flashing content | PASS | PASS | PASS | — |
| Subtitles for dialogue | N/A (no dialogue) | N/A | N/A | — |
| UI scales at all resolutions | FAIL — ConstantPixelSize on all | FAIL | FAIL | P0 |
| Touch targets >= 88px | FAIL — BtnStartBattle 60px | FAIL — BtnBack 50px, BtnLaunch 55px | FAIL — BtnClear 30px, BtnConfirm 55px | P0/P1 |

### 3.2 Color Contrast Analysis (using approved palette)

Reference: WCAG AA requires contrast ratio ≥ 4.5:1 for normal text, ≥ 3:1 for large
text (≥18pt / ≥14pt bold).

| Foreground | Background | Hex pair | Approx contrast | Pass/Fail |
|-----------|-----------|----------|----------------|-----------|
| Gold #D4A017 | BgDark #140F24 | 0.40 vs 0.02 | ~7.2:1 | PASS large |
| Cream #EDD9A3 | BgDark #140F24 | 0.72 vs 0.02 | ~12.8:1 | PASS |
| Cream #EDD9A3 | PanelDark #1F172E | 0.72 vs 0.03 | ~11.1:1 | PASS |
| Dark #1A0D00 on Gold #D4A017 | (launch btn label) | 0.01 vs 0.40 | ~6.5:1 | PASS |
| DisabledLabel #6B5A30 on DisabledBg #3D3020 | | 0.11 vs 0.04 | ~2.1:1 | FAIL (no contrast) |

**Contrast failure:** The disabled state label (`#6B5A30` on `#3D3020`) has an
approximate ratio of 2.1:1, which fails WCAG AA. Players with low vision cannot read
disabled button text. Add an accessibility-mode override or ensure disabled state
is communicated through means other than low-contrast text (icon, position, label
change).

---

## Section 4: Data Flow Summary

```
StageData (ScriptableObject)
  └── StageRegistry.Stages (list)
        └── StageSelectController reads on Start()
              ├── Spawns StageEntryUI per stage
              │     ├── StageEntryUI.DisplayName → TitleText
              │     └── StageEntryUI.DifficultyLevel → difficulty string
              └── OnLaunchClicked()
                    └── GameFlowManager.SelectedStage = _selectedStage

CharacterData[] (ScriptableObjects, serialized in scene)
  └── TeamSelectController._roster
        ├── Spawns TeamRosterEntryUI per character
        │     ├── CharacterData.DisplayName → _charName
        │     └── CharacterData.Element.ToString() → _charElement
        └── OnConfirmClicked()
              └── GameFlowManager.SelectedTeam = _composition

GameFlowManager (DontDestroyOnLoad singleton in MainMenu scene)
  ├── .SelectedStage (StageData)
  ├── .SelectedTeam  (TeamComposition)
  ├── LoadStageSelect() / LoadTeamSelect() / LoadCombat() / LoadMainMenu()
  └── Persists across scene loads
```

**Gap:** TeamSelect shows element as English enum name (e.g., "Fire"). For a
Spanish-language game this should be localized ("Fuego"). This is a data/content
gap, not a UX architecture gap.

---

## Section 5: Prioritized Fix List

### P0 — Must Fix Before Any Demo

| ID | Screen | Fix | Verifiable by |
|----|--------|-----|---------------|
| P0-01 | All | CanvasScaler → Scale With Screen Size, ref 1080×1920, match height | Resolution independence check at 2 sizes |
| P0-02 | All | Set m_FirstSelected on each EventSystem to the primary action button | Gamepad A-button activates button on launch |
| P0-03 | TeamSelect | Move Assets/TeamSelect.unity → Assets/Scenes/TeamSelect.unity, update Build Settings | Build succeeds, flow intact |
| P0-04 | StageSelect | Set ScrollRect m_Horizontal to 0 | Cards cannot scroll sideways |
| P0-05 | MainMenu | Delete ghost empty Text child of BtnStartBattle (fileID 954099865) | One Text child in BtnStartBattle |
| P0-06 | MainMenu | Change button label from "Start Battle" to Spanish copy | Localization pass |
| P0-07 | StageSelect | Serialize BtnLaunch m_Interactable: 0 in scene | No interactable flash on load |
| P0-08 | TeamSelect | Serialize BtnConfirm m_Interactable: 0 in scene | No interactable flash on load |

### P1 — Important for Playtest Quality

| ID | Screen | Fix | Verifiable by |
|----|--------|-----|---------------|
| P1-01 | All | Touch targets: BtnStartBattle 540×88, BtnBack 120×64, BtnLaunch 540×120, BtnConfirm 540×88 | Tap test on all buttons |
| P1-02 | All | BtnClear minimum 88×44px (at absolute minimum — 88×88 preferred) | Tap test |
| P1-03 | StageSelect / TeamSelect | Header height 180px, add subtitle text below title | Visual match with visual direction |
| P1-04 | StageSelect | BtnBack: remove solid Image background, style as text-only per visual direction | Back button visually recedes from Launch |
| P1-05 | TeamSelect | Filled slot visual state: add gold border or color change to filled Slot Image | Player can see team at a glance |
| P1-06 | TeamSelect | TitleText re-anchor from center-offset to top-anchored layout | No vertical drift at 1920 height |
| P1-07 | StageSelect | BtnLaunch color block: set to approved ColorBlock (Normal #D4A017, Highlighted #E8B420, Pressed #B8880F) not default Unity white block | Correct hover/press feedback |
| P1-08 | StageSelect | BtnConfirm same ColorBlock fix as P1-07 | |
| P1-09 | MainMenu | TitleText re-anchor to top-center with ~240px top offset | Stays in upper screen at all heights |
| P1-10 | StageSelect | Verify StageEntryUI prefab has LayoutElement.preferredHeight = 90+ | Cards size correctly in scroll |

### P2 — Nice to Have Before Sprint End

| ID | Screen | Fix | Verifiable by |
|----|--------|-----|---------------|
| P2-01 | All | Import Pirata One + Noto Sans fonts, assign to all text elements per visual direction typography table | Visual fidelity check |
| P2-02 | All | Three-layer background (gradient + noise) per visual direction section 1 | Screenshot comparison |
| P2-03 | StageSelect | Add reward strip to StageEntryUI prefab (read from StageData if available) | Cards show "Botín: ???" |
| P2-04 | StageSelect | Add left accent stripe Image to StageEntryUI prefab, per-stage color | Stage identity visual |
| P2-05 | StageSelect | Replace ASCII star difficulty with TMP Rich Text dot system | Visual match with spec |
| P2-06 | StageSelect | Complete EmptyStateText copy: "No hay misiones disponibles. Vuelve más tarde." | Copy consistency |
| P2-07 | TeamSelect | Localize element names to Spanish in CharacterData assets | "Fire" → "Fuego" |
| P2-08 | TeamSelect | Unify Background color to #140F24 (BgDark) matching MainMenu/StageSelect | Visual consistency |
| P2-09 | All | Disabled button state: improve contrast ratio (currently ~2.1:1) | WCAG AA check |
| P2-10 | All | Navigation: set explicit m_SelectOnUp/Down for all buttons to guide D-pad through scroll entries | Full gamepad flow test |
| P2-11 | General | Delete Assets/Scenes/StageSelect.unity (empty stub) and audit all duplicate scene files | Clean project structure |

---

## Quick Reference: File Paths Audited

- `Assets/MainMenu.unity` (active scene)
- `Assets/StageSelect.unity` (active scene)
- `Assets/TeamSelect.unity` (active scene — wrong location)
- `Assets/Scripts/UI/MainMenu/MainMenuController.cs`
- `Assets/Scripts/UI/StageSelect/StageSelectController.cs`
- `Assets/Scripts/UI/StageSelect/StageEntryUI.cs`
- `Assets/Scripts/UI/TeamSelect/TeamSelectController.cs`
- `Assets/Scripts/UI/TeamSelect/TeamRosterEntryUI.cs`
- `docs/art/ui-stageselect-visual-direction.md`
- `.claude/docs/coplay-unity-lessons.md`
