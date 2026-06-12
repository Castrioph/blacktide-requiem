# StageSelect Screen — Visual Direction
> **Status**: Approved for implementation (S3-06)
> **Author**: Art Director agent + Creative Director decisions
> **Date**: 2026-04-21
> **Target**: Unity 6.3 UGUI, 1080×1920 portrait reference

---

## Copy Decisions

- **Screen title**: "Seleccionar Misión"
- **Launch button**: "¡ZARPAR!"
- **Back button**: "< Volver"
- **Header subtitle**: "Elige tu destino, corsario."
- **Empty state**: "No hay misiones disponibles. Vuelve más tarde."

---

## 1. Screen Background

Three stacked Image components on root Canvas:

| Layer | Color | Alpha | Notes |
|-------|-------|-------|-------|
| Base fill (solid) | `#0D1B2A` | 255 | Deep Navy |
| Gradient overlay (2×2 PNG baked: top `#1A2744`, bottom `#050D14`) | — | 180/220 | Vertical gradient, top-to-bottom |
| Noise tint (optional) | `#4A7FA5` | 12 | Low-alpha additive — skip if complexity budget is tight |

**Baked gradient PNG**: 2×2 sprite, top row `#1A2744`, bottom row `#050D14`. Import as Sprite, Bilinear filter. Assign to second Image, stretch to full canvas.

---

## 2. Stage Card Visual Spec

**Dimensions**: 900 × 340 px. Margins: 90 px each side. Gap between cards: 24 px. Top of first card: 24 px below header bottom edge.

### Unselected State

| Layer | Color | Alpha | Notes |
|-------|-------|-------|-------|
| Background (solid) | `#3D2810` | 230 | Dark wood brown |
| Top catch-light (8 px, anchored top) | `#8B5E3C` | 180 | Simulates wood grain light |
| Bottom shadow (24 px, anchored bottom) | `#050D14` | 60 | Pool shadow |
| Border (outline Image, inset −2 px each side) | `#5C3D1E` | 255 | Wood-grain border |
| Left accent stripe (8 px wide, full height) | Stage-specific (see §7) | 255 | Color identity marker |

### Selected State

| Layer | Color | Alpha | Change from unselected |
|-------|-------|-------|----------------------|
| Background (solid) | `#4A3018` | 255 | Lighter, fully opaque |
| Border | `#D4A017` | 255 | Gold replaces wood border |
| Outer glow (card size +8 px each side, behind card) | `#FFD700` | 28 | Subtle gold halo |
| Left accent stripe | Same | 255 | Unchanged |

Transition: 0.12s ease-out tween on border color + background color.

---

## 3. Difficulty Indicator

Unicode circles in a single TextMeshPro component (Rich Text enabled).

| State | Character | Color (by stage) |
|-------|-----------|-----------------|
| Filled | `●` U+25CF | Stage accent secondary color |
| Empty | `○` U+25CB | `#2A3A4A` |

**Per-stage filled color**: same as Stage Secondary Accent (§7).

**TMP Rich Text example (Bahía Corsaria, difficulty 1)**:
```
<color=#4FC3F7>●</color><color=#2A3A4A> ○ ○ ○ ○</color>
```

**Difficulty label** (sibling TMP, to the right of dots):

| Difficulty | Label | Color |
|-----------|-------|-------|
| 1 | Fácil | `#EDD9A3` |
| 2 | Normal | `#EDD9A3` |
| 3 | Difícil | `#EDD9A3` |

Font: Noto Sans Regular, 13 sp. Dots: 18 sp. Character spacing: 4 px.

---

## 4. Header Bar

**Height**: 180 px (top-anchored, covers safe area notch zone).

| Layer | Color | Alpha |
|-------|-------|-------|
| Base fill | `#1A0D00` | 255 |
| Bottom gold divider (4 px, anchored bottom) | `#D4A017` | 220 |

**Title text**:
- Text: "Seleccionar Misión"
- Font: Pirata One, 32 sp
- Color: `#D4A017`
- Alignment: Center. Anchor to lower 120 px of header (account for safe area).
- TMP Drop Shadow: offset (2, −2), color `#000000` alpha 160

**Subtitle text** (optional, 6 px below title):
- Text: "Elige tu destino, corsario."
- Font: Noto Sans Regular Italic, 14 sp
- Color: `#EDD9A3`, Alpha 180

---

## 5. Launch Button ("¡ZARPAR!")

**Dimensions**: 540 × 120 px. Centered horizontally. 48 px above bottom safe area edge.

### Enabled State

| Layer | Color | Alpha |
|-------|-------|-------|
| Background | `#D4A017` | 255 |
| Top highlight strip (3 px, anchored top) | `#FFD700` | 200 |
| Bottom shadow strip (3 px, anchored bottom) | `#8B6914` | 120 |

- Label: "¡ZARPAR!" — Pirata One, 26 sp, color `#1A0D00`, Alpha 255
- Letter spacing: 2 px

### Disabled State

| Layer | Color | Alpha |
|-------|-------|-------|
| Background | `#3D3020` | 255 |
| Label | `#6B5A30` | 200 |

**Unity Button ColorBlock** (Color Tint mode):
- Normal: `#D4A017`
- Highlighted: `#E8B420`
- Pressed: `#B8880F`
- Disabled: `#3D3020`

---

## 6. Back Button ("< Volver")

**Placement**: Top-left of header, 24 px from left edge, vertically centered in lower 120 px of header.
**Dimensions (hit target)**: 120 × 64 px.
**Style**: Text-only, no background.

| State | Color | Alpha |
|-------|-------|-------|
| Normal | `#EDD9A3` | 200 |
| Highlighted | `#FFD700` | 220 |
| Pressed | `#FFFFFF` | 255 |

Font: Noto Sans Regular, 16 sp.

---

## 7. Stage-Specific Color Accents

| Stage ID | Name | Primary Accent | Secondary Accent | Rationale |
|----------|------|---------------|-----------------|-----------|
| stage_001 | Bahía Corsaria | `#1E88E5` (Corsair Blue) | `#4FC3F7` (sea foam) | Open sea, daylight raid |
| stage_002 | Muelle Maldito | `#6A1B9A` (Voodoo Violet) | `#CE93D8` (pale violet) | Rot, shadow, cursed harbor |
| stage_003 | Templo Vudú | `#BF360C` (Temple Ember) | `#FF8A65` (ember orange) | Ritual fire, deep jungle |

Primary accent → left stripe on card, border on selected state.
Secondary accent → difficulty dots filled color.

---

## 8. Reward Preview Strip

**Placement**: Bottom-interior of each card. 48 px tall, full card width, anchored to card bottom.

| Layer | Color | Alpha |
|-------|-------|-------|
| Strip background | `#1A0D00` | 180 |
| Top divider (1 px) | `#5C3D1E` | 200 |

**Text layout**: Left-aligned, 16 px left padding.
- Label "Recompensa:": Noto Sans Regular, 12 sp, color `#8B5E3C`
- Value (e.g., "50 Doblones"): Noto Sans Bold, 13 sp, color `#FFD700`

If reward data not wired yet: show `"Botín: ???"` in `#8B5E3C` alpha 140.

---

## 9. Typography Reference

| Element | Font | Size | Color | Alpha |
|---------|------|------|-------|-------|
| Screen title | Pirata One | 32 sp | `#D4A017` | 255 |
| Header subtitle | Noto Sans Regular Italic | 14 sp | `#EDD9A3` | 180 |
| Stage name | Pirata One | 22 sp | `#F5E6C8` | 255 |
| Difficulty label | Noto Sans Regular | 13 sp | `#EDD9A3` | 200 |
| Difficulty dots | TMP (any font) | 18 sp | Per-stage | 255 |
| Reward label | Noto Sans Regular | 12 sp | `#8B5E3C` | 220 |
| Reward value | Noto Sans Bold | 13 sp | `#FFD700` | 255 |
| Launch button | Pirata One | 26 sp | `#1A0D00` / `#6B5A30` | 255/200 |
| Back button | Noto Sans Regular | 16 sp | `#EDD9A3` | 200 |

**Rule**: Pirata One = titles, stage names, primary action only. All secondary info uses Noto Sans.

---

## 10. "No Real Art" Implementation Playbook

### Technique A — Wood Panel (cards)
1. Image solid `#3D2810` — base wood
2. Image solid `#8B5E3C` alpha 40, 8 px tall, anchored top — catch-light
3. Image solid `#050D14` alpha 60, 24 px tall, anchored bottom — shadow pool
4. Border Image (inset −2 px each side), color `#5C3D1E` — wood grain border

### Technique B — Gold Button (Launch)
1. Image solid `#D4A017` — gold base
2. Image solid `#FFD700` alpha 180, 4 px tall, anchored top — bevel catch-light
3. Image solid `#8B6914` alpha 120, 4 px tall, anchored bottom — bevel shadow

### Technique C — Dark Overlay (reward strip, header)
1. Image solid `#000000`, alpha 160–200 — creates recessed/shadowed surface

### Technique D — Gradient Background
- 2×2 PNG, top `#1A2744`, bottom `#050D14`. Bilinear, stretch to canvas.

### Technique E — Accent Stripe
- Single 8 px wide Image, full card height, left-anchored. One saturated color against dark wood reads immediately as thematic identity.

### Minimal Sprite Budget (zero external art required)

| Sprite | Size | Purpose |
|--------|------|---------|
| `ui_bg_ocean_gradient.png` | 2×2 | Screen background |
| `ui_panel_9slice.png` | 32×32, 4 px border | Card border (optional) |
| `ui_btn_9slice.png` | 32×32, 8 px border | Launch button (optional) |

Full screen is buildable with Unity's default 1×1 white sprite in color-only mode. 9-slice sprites are a progressive enhancement.

---

## Visual Weight Hierarchy

1. Selected card gold border + glow — player's current choice
2. Launch button (gold, bottom center) — primary action
3. Stage name text (Pirata One, cream, 22 sp)
4. Header title (Pirata One, gold, 32 sp)
5. Stage accent stripe (thin, high saturation)
6. Difficulty dots (small, color-coded)
7. Reward strip (supplemental)
8. Back button (deliberately unobtrusive)
