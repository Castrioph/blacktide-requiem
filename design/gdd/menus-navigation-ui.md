# Menus & Navigation UI

> **Status**: Designed
> **Author**: User + Claude Code Game Studios agents
> **Last Updated**: 2026-04-01
> **Implements Pillar**: Pillar 4 (Respeto al Tiempo del Jugador)

## Overview

El Menus & Navigation UI es la capa de presentación visual que implementa la
estructura de navegación definida por el Game Flow. Define los layouts concretos de
cada pantalla dentro de los 5 tabs (Puerto, Aventura, Tripulación, Taberna, Tienda),
las especificaciones de widgets reutilizables (unit cards, currency bar, stat bars,
botones de acción), los patrones de interacción (tap, long-press, swipe, drag), y las
reglas de layout responsivo para mobile portrait y WebGL.

El jugador interactúa con este sistema constantemente — cada tap, cada scroll, cada
popup es parte de Menus & Navigation UI. Pero la interacción debe sentirse tan natural
que el jugador piensa en "subir de nivel a mi unidad", no en "navegar al menú de
progresión". Es la piel visual del juego fuera de combate.

Sin este sistema, todos los sistemas de backend (roster, inventario, gacha,
progresión) existen pero el jugador no puede interactuar con ellos. Es el traductor
entre datos y experiencia de usuario.

## Player Fantasy

**"Todo a un tap de distancia."** El jugador abre el juego y siente que controla todo
desde su puerto: ver su tripulación, preparar equipos, invocar nuevos personajes,
zarpar a la aventura. La UI es el timón de su barco — responde instantáneamente, nunca
confunde, y se ve como parte del mundo pirata (no como un menú genérico pegado encima).

**Tipo de sistema**: Infraestructura visual pura — el jugador nunca piensa "la UI es
buena", simplemente fluye. El éxito se mide por ausencia de fricción.

**Referencia**: FFBE (layout limpio, bottom nav familiar, unit list con filtros
potentes), Genshin Impact (transiciones suaves, popups no intrusivos, currency bar
siempre visible), Arknights (estética integrada en el mundo del juego, no menús
genéricos).

El sistema fracasa si el jugador se pierde buscando una función ("¿dónde subo de
nivel?"), si los menús tardan en cargar, si la información está tan apretada que no se
lee en mobile, o si la UI se siente genérica y desconectada de la temática pirata.

## Detailed Design

### Core Rules

#### 1. Shared UI Components

**A. Top Bar** (persistent on all Hub screens, overlay)

| Element | Position | Content | Interaction |
|---------|----------|---------|-------------|
| Player Name + Rank | Left | "Capitán [Name] — Rango [N]" | Tap → Player profile (future) |
| Doblones (DOB) | Center-left | Coin icon + amount | Tap "+" → deep link to Tienda |
| Gemas de Calavera (GDC) | Center-right | Gem icon + amount | Tap "+" → deep link to Tienda |
| Energía | Right | Lightning icon + current/max | Tap → refill popup (TBD) |

Height: ~48dp. Background: dark wood plank texture, semi-transparent. Currencies
animate count-up/down on change.

**B. Bottom Nav Bar** (visual spec — structure defined in Game Flow GDD)

| Property | Value |
|----------|-------|
| Height | 64dp (56dp icons + 8dp label) |
| Background | Weathered wood plank texture |
| Icon size | 32×32dp, hand-drawn pixel art style |
| Label | 10sp, below icon, pirate-themed font |
| Active indicator | Gold glow behind icon + label turns gold |
| Badge | Red circle (16dp), white number (bold), top-right of icon. "99+" max |
| Safe area | Respects device safe area (notch, home indicator) |

**C. Unit Card Widget** (reusable across roster, team comp, gacha results)

| Element | Content | Size |
|---------|---------|------|
| Portrait | Unit face art, cropped square | Full card area |
| Rarity frame | Border: bronce (3★), plata (4★), oro (5★) | Border overlay |
| Element icon | Small element badge | Top-left, 16×16dp |
| Level badge | "Lv.[N]" | Bottom-center overlay |
| Stars | Rarity stars | Below portrait |
| Status badges | "NEW" (green), "MAX" (red), ⚔️ (land), ⚓ (crew) | Top-right, stacked |

Card size: ~80×100dp (portrait mobile, 4 columns). Tap → Unit Detail. Long-press →
quick preview popup (stats principales).

**D. Confirmation Popup**

| Element | Spec |
|---------|------|
| Backdrop | 50% opacity black, tap-to-dismiss (cancels) |
| Card | Centered, max 80% screen width, rounded corners, parchment texture |
| Title | Bold, 18sp |
| Body | Regular, 14sp |
| Buttons | Cancel (outline, left) + Confirm (filled gold, right). Destructive = red |
| Close | X button top-right |

**E. Resource Cost Widget** (reusable for awakening, upgrades, craft)

`[Icon] [Name] [Owned]/[Required]` — green if sufficient, red if insufficient.

**F. Stat Bar Widget**

Horizontal bar: label left, numeric value right, filled proportional to value.
Color: green (≥66% of max for rarity), yellow (33-66%), red (<33%).
Tap → expand desglose (base + level + awaken + equip + dupes).

---

#### 2. Tab 1: Puerto (Home)

Full-screen port scene as background. UI elements are **overlays** on the edges —
no dedicated zones that push the scene into a small area.

```
┌────────────────────┐
│Cap.Name 💰1200 💎350│ ← Top Bar (overlay)
├────────────────────┤
│┌──────────┐   [📬] │
││🏴 Evento  │   [🏆] │ ← News banner (small
││"Tormenta" │   [📜] │   carousel, top-left)
││ ● ○       │   [👥] │
│└──────────┘   [⚙️] │ ← Side stack (right)
│                     │
│                     │
│   ⛵ [Lead Unit]    │ ← PORT SCENE
│                     │   (full background
│ ~~🌊~~ Puerto ~~🌊~~│    100% of screen)
│                     │
│  🏠🏠  ⚓  🏠🏠    │
│                     │
├────────────────────┤
│ ⚓   🧭   👥   💀  🪙│ ← Nav Bar
└────────────────────┘
```

**Port Scene** (background, animated):
- Animated port: docked ship with lead unit sprite on deck
- Day/night cycle matching device time
- Waves, seagulls, ambient particles
- Tapping the lead unit plays idle animation

**News Banner** (top-left overlay, small carousel):
- ~40% screen width, auto-scroll every 5s
- Event banners, update notes, featured gacha promo
- Tap → deep link to target (Aventura event, Taberna banner, etc.)
- Carousel dots below banner

**Side Stack** (right edge, vertical):

| Icon | Label | Badge | Destination |
|------|-------|-------|-------------|
| 📬 | Mail | Unread count | Mail inbox |
| 🏆 | Logros | Claimable count | Achievements |
| 📜 | Misiones | Unclaimed daily | Daily missions |
| 👥 | Amigos | Friend requests | Friend list (locked until tutorial) |
| ⚙️ | Config | — | Settings |

Icons: 32×32dp, semi-transparent background, badge (red dot + count). Login reward
no necesita botón — es popup automático al entrar (Game Flow GDD §3).

---

#### 3. Tab 2: Aventura (Sailing Route)

Vertical scrollable **sailing route** — the player's ship sails along a dotted sea
path from port to port. Each stop is a stage. Sea background with parallax scroll.

```
┌────────────────────┐
│Cap.Name 💰1200 💎350│ ← Top Bar
├────────────────────┤
│ Cap.1: Isla Naufragio │
│                       │
│  ⛵ YOU ARE HERE      │ ← Ship marker
│  🏝️ 1-3 Arrecife NEW │    at current
│  │  ⚡8 ⚡Tormenta    │    stage
│  │                    │
│  · · · · ·           │ ← Dotted sea
│  │                    │    route
│  🏝️ 1-2 Cala ✓ ⭐⭐░ │
│  │  ⚡5 💀Maldición   │
│  │                    │
│  · · · · ·           │
│  │                    │
│  🏝️ 1-1 Puerto ✓ ⭐⭐⭐│
│  │  ⚡5 🔥Pólvora     │
│  │                    │
│  ⚓ PUERTO (start)    │
│                       │
│  [Ch.1] [Ch.2] [Naval]│ ← Chapter tabs
├────────────────────┤
│ ⚓  🧭  👥  💀  🪙  │
└────────────────────┘
```

**Route Visual Rules:**
- Scroll vertically along the sea route (bottom = start, top = furthest stage)
- Ship icon (⛵) marks the player's current position (next uncleared stage)
- Cleared stages: bright island icon + lit lantern + completion stars (0-3)
- Locked stages: foggy/distant, greyed out, shows unlock condition text
- Sea background with parallax (distant clouds move slower than foreground waves)
- Dotted path between stages animates (flowing water effect)

**Stage Node** (each stop on the route):

| Element | Content |
|---------|---------|
| Island icon | Thematic to the stage's environment |
| Stage name | "1-3 Arrecife" |
| Energy cost | ⚡ icon + number |
| Enemy element | Element icon (helps team planning) |
| Status | ✓ if cleared, "NEW" badge if just unlocked, 🔒 if locked |
| Mission stars | ⭐⭐⭐ (0-3 filled) |

Tap stage → **Stage Detail popup**: full mission list (3 missions), rewards preview,
recommended level, enemy element, "Deploy" button.

**Chapter Tabs** (bottom of route, above nav):
- Horizontal: Ch.1, Ch.2, Naval, Eventos
- Switching chapter loads a different route
- Locked chapters: greyed out with unlock condition text
- Naval chapter: separate ocean route (ships instead of islands)

---

#### 4. Tab 3: Tripulación

Three sub-sections via **sub-tab bar** at top:

```
┌────────────────────┐
│Cap.Name 💰1200 💎350│
├────────────────────┤
│[Unidades] [Barcos] [Equipos]│ ← Sub-tabs
├────────────────────┤
│ Sort: Rarity▼ Filter: ▼│ ← Sort/Filter bar
├────────────────────┤
│┌────┐┌────┐┌────┐┌────┐│
││⭐⭐⭐││⭐⭐⭐││⭐⭐ ││⭐⭐ ││
││Elena││Marco││Yuki││Jin  ││ ← Unit Grid
││Lv30 ││Lv25 ││Lv18││Lv12││   (4 cols)
││🔥MAX││⚡NEW││💀  ││🐾  ││
│└────┘└────┘└────┘└────┘│
│┌────┐┌────┐┌────┐┌────┐│
││⭐  ││⭐  ││⭐  ││    ││
││Rata ││Grub ││Cook ││    ││
││Lv8  ││Lv5  ││Lv3 ││    ││
││⚔️🔥││⚓💀 ││🐾  ││    ││
│└────┘└────┘└────┘└────┘│
├────────────────────┤
│ ⚓  🧭  👥  💀  🪙  │
└────────────────────┘
```

**Sub-tab: Unidades**
- Sort/filter bar at top (as defined in Unit Roster/Inventory GDD §4)
- Grid of Unit Cards (shared widget, 4 cols portrait)
- Tap → Unit Detail Screen (full hub, Roster GDD §5)

**Sub-tab: Barcos**
- List of owned ships (card per ship: visual + name + stats summary + "Active" badge)
- Tap → Ship Detail Screen (Roster GDD §6)

**Sub-tab: Equipos**
- Land presets first, then naval presets
- Preset card: name + 5 unit mini-portraits (or empty slots) + synergy count
- Tap → Preset editor (assign/swap units, rename, preview synergies)
- "+" button to create new preset (max 10 land, 5 naval)

---

#### 5. Tab 4: Taberna (Gacha)

Banner-dominant layout. The featured unit art is the **hero visual** occupying ~60%
of screen. Pull buttons overlay the banner.

```
┌────────────────────┐
│Cap.Name 💰1200 💎350│ ← Top Bar
├────────────────────┤
│                     │
│ ┌─────────────────┐ │
│ │                 │ │
│ │  ⭐⭐⭐⭐⭐         │ │
│ │                 │ │
│ │  ELENA          │ │
│ │  STORMCALLER    │ │ ← Banner Art
│ │                 │ │   (HERO ~60%)
│ │  "Banner        │ │
│ │   Destacado"    │ │
│ │  Quedan 12 días │ │
│ │                 │ │
│ │ ┌─────┐┌─────┐ │ │
│ │ │💎GDC ││🎫TIF │ │ │ ← 2 pull buttons
│ │ │Summon││Summon│ │ │   OVERLAY on banner
│ │ └─────┘└─────┘ │ │
│ │                 │ │
│ │[Ver probabilidades]│ │
│ └─────────────────┘ │
│       ● ○           │ ← Carousel dots
├─────────────────────┤
│ Pity: 73/90  50/50  │ ← Pity compact
│ [Historial]         │
├────────────────────┤
│ ⚓  🧭  👥  💀  🪙  │
└────────────────────┘
```

**Banner Carousel:**
- Swipe between Featured Banner (left dot) and Standard Banner (right dot)
- Each banner: full-width art of featured unit(s) in dramatic pose
- Banner type label + duration (featured only: "Quedan X días")
- "Ver probabilidades" link → popup with full rate table

**Pull Buttons** (overlay on banner, 2-step flow):

| Banner | Button 1 | Button 2 |
|--------|----------|----------|
| Featured | 💎 GDC Summon | 🎫 TIF Summon |
| Standard | 🎫 TIE Summon | (solo TIE, no GDC) |

Tap either button → **quantity popup**:

| Option | Cost | Condition |
|--------|------|-----------|
| Single Pull | 💎300 GDC / 🎫×1 ticket | Always available |
| Multi Pull ×10 | 💎3000 GDC / 🎫×10 tickets | Greyed out if insufficient |

Si el jugador no tiene suficiente recurso, el botón de Summon muestra el coste en
rojo y al hacer tap ofrece deep link a Tienda (para GDC) o indica dónde obtener más
tickets.

**Pity Info** (compact, below carousel):
- "Pity: [N]/90" + "Next 5★: [Guaranteed/50-50]"
- Tap → pity details popup (desglose de los 3 pity counters)

---

#### 6. Tab 5: Tienda (Placeholder in Demo)

| Zone | Content |
|------|---------|
| GDC Packs | Grid of gem pack cards. Tappable → "Demo — no purchases" toast |
| DOB Exchange | GDC → DOB exchange rate (if applicable) |
| Tienda de Almas | Fragmentos → 5★ select. Functional if player has ≥300 fragments |
| Settings shortcut | Link to Settings (also from Puerto side stack) |

In demo, IAP buttons show placeholder. Tienda de Almas IS functional.

---

#### 7. Settings Screen

Accessible from Puerto (⚙️) and Tienda:

| Section | Options |
|---------|---------|
| Audio | Music volume slider (0-100%), SFX volume slider (0-100%) |
| Gameplay | Auto-battle speed (1x / 2x toggle) |
| Idioma | Language selector (placeholder — only Spanish in demo) |
| Cuenta | "Borrar cuenta" (double confirmation, Save/Load GDD) |
| Info | Version, credits, legal |

---

#### 8. Layout Rules (Responsive)

| Rule | Mobile Portrait | WebGL (landscape) |
|------|----------------|-------------------|
| Base resolution | 1080×1920 (16:9) | 1920×1080 |
| Scale mode | Scale with screen width | Fixed 16:9, letterbox if needed |
| Min touch target | 44×44dp | 32×32dp (mouse) |
| Grid columns (roster) | 4 | 6 |
| Text min size | 12sp | 12sp |
| Nav bar | Bottom | Bottom |
| Popups | Max 80% width | Max 50% width |
| Safe area | Respect notch + home indicator | N/A |
| Orientation | Portrait locked (demo) | Landscape (web default) |

### States and Transitions

Los estados de navegación top-level están definidos en Game Flow GDD (Splash → Login
→ Hub → Combat → Results → Narrative). Este sistema añade estados UI-específicos:

#### Popup States

| State | Trigger | Dismisses by |
|-------|---------|-------------|
| Stage Detail | Tap stage node en Aventura | Back, tap outside, "Deploy" |
| Pull Quantity | Tap Summon button en Taberna | Back, tap outside, select option |
| Pity Details | Tap pity counter en Taberna | Back, tap outside |
| Rate Table | Tap "Ver probabilidades" | Back, tap outside |
| Item Detail | Tap item en inventario | Back, tap outside |
| Confirmation | Pre-acción destructiva | Cancel, Confirm, tap outside (= cancel) |
| Insufficient Funds | Intento de acción sin recursos | Close, "Ir a Tienda" |

Solo un popup activo a la vez. Si una acción desde un popup necesita otro popup
(e.g., "Deploy" → friend select), el primero se cierra y se abre el segundo.

#### Sort/Filter Panel State (Tripulación)

| State | Description |
|-------|-------------|
| Collapsed | Solo muestra sort activo + filter icon con dot si activo |
| Expanded | Panel desplegable con todas las opciones de sort/filter |

Toggle con tap en la barra. Se colapsa al seleccionar filtro o al scroll.

### Interactions with Other Systems

| Sistema | Dirección | Interfaz |
|---------|-----------|----------|
| **Game Flow** (#5) | Upstream | Define nav model, screen hierarchy, tabs, transitions. Este sistema implementa la capa visual |
| **Unit Roster/Inventory** (#18) | Upstream | Provee datos de roster, inventario, unit detail para renderizar en Tripulación |
| **Team Composition** (#11) | Upstream | Provee reglas de presets y datos de equipos para sub-tab Equipos |
| **Sistema Gacha** (#13) | Upstream | Provee datos de banners, pity, rates, pull logic. Taberna renderiza estos datos |
| **Progresión de Unidades** (#15) | Upstream | Provee datos de level/awakening para Unit Detail actions |
| **Stage System** (#8) | Upstream | Provee datos de stages, capítulos, progreso para Aventura route |
| **Currency System** (#4) | Upstream | Provee saldos para Top Bar. Deep links "+" → Tienda |
| **Rewards System** (#16) | Upstream | Provee datos de misiones, login calendar, logros para Puerto side stack |
| **Save/Load System** (#17) | Bilateral | UI triggers auto-save tras acciones. Load restaura último estado de tabs |

Este sistema es **leaf de presentación** — no tiene downstream dependents. Todos los
datos fluyen hacia él, y las acciones del usuario fluyen de vuelta a los sistemas
backend via eventos/callbacks.

## Formulas

Este sistema es puramente presentacional — no tiene fórmulas de gameplay propias.
Las fórmulas relevantes viven en otros GDDs y se referencian aquí:

### F1. Notification Badge Count (from Game Flow GDD)

```
TabBadgeCount = sum of pending items within tab scope
```

- Puerto side stack: sum of mail + logros + misiones unclaimed
- Aventura: new unlocked stages + expiring events
- Tripulación: units ready to level up / awaken (si materiales suficientes)
- Taberna: pity guarantee available
- Tienda: new/limited promotions

### F2. Transition Timing (from Game Flow GDD)

| Transition | Max Duration |
|------------|-------------|
| Tab switch | < 100ms (instant) |
| Sub-screen push/pop | < 200ms (animated) |
| Hub → Combat | < 2s (loading screen if > 1s) |
| App cold launch → Puerto | < 5s |

### F3. Responsive Scaling

```
UIScale = ScreenWidth / BASE_WIDTH
ElementSize = BaseSize × UIScale
```

Where `BASE_WIDTH` = 1080px (reference portrait resolution).

## Edge Cases

| Situación | Qué pasa |
|-----------|----------|
| **News banner con 0 eventos** | Banner zone se oculta. Side stack sube para llenar el espacio |
| **Side stack con 0 badges** | Icons visibles pero sin badge dots. No se ocultan |
| **Aventura route con todas las stages cleared** | Ship icon al final de la ruta. No "YOU ARE HERE" sino "RUTA COMPLETA ✓" |
| **Aventura scroll con stage locked** | Scroll permitido hasta ver 1 stage locked (+ su unlock condition). No scroll más allá |
| **Taberna sin banner activo** | Siempre hay al menos el banner estándar (permanente). Si no hay featured, solo se muestra el estándar sin carousel dots |
| **Taberna pull sin recursos** | Botón de Summon muestra coste en rojo. Tap → popup "Recursos insuficientes" con deep link a Tienda (GDC) o fuente de tickets |
| **Tripulación roster vacío** (imposible normalmente) | Mensaje: "Visita la Taberna para reclutar tu primera tripulación" con botón → Taberna |
| **Popup sobre popup** | No permitido. Acción desde popup cierra el actual antes de abrir el nuevo |
| **Tap rápido en pull button** | Deshabilitado tras primer tap hasta que popup responde. Previene doble-pull |
| **Stage Detail popup en stage locked** | Muestra nombre + unlock condition + enemies info (sin "Deploy"). No dead-end |
| **Rotate device (mobile)** | Ignorado — portrait locked en demo |
| **Nav bar badge count > 99** | Muestra "99+" |
| **Port scene performance en low-end** | Particles reducidos, day/night desactivado. Scene estática como fallback |

## Dependencies

### Upstream (todo — este es un leaf de presentación)

| Sistema | Tipo | Qué consume |
|---------|------|-------------|
| Game Flow (#5) | Hard | Nav model, screen hierarchy, tabs, transitions |
| Unit Roster/Inventory (#18) | Hard | Roster data, inventory data, unit detail |
| Team Composition (#11) | Hard | Preset data, formation rules |
| Stage System (#8) | Hard | Stage data, chapter structure, progress |
| Sistema Gacha (#13) | Hard | Banner data, pity, rates, pull flow |
| Progresión de Unidades (#15) | Hard | Level/awaken data para Unit Detail actions |
| Currency System (#4) | Hard | Currency balances para Top Bar |
| Rewards System (#16) | Soft | Misiones, login, logros para Puerto side stack |
| Save/Load System (#17) | Soft | Persistencia de navegación state |

### Downstream

Ninguno — este sistema es el terminal de presentación.

## Tuning Knobs

### Knobs propios

| Knob | Default | Range | Afecta |
|------|---------|-------|--------|
| `SIDE_STACK_ICON_SIZE` | 32dp | 24-48dp | Tamaño de icons en Puerto side stack |
| `SIDE_STACK_OPACITY` | 0.85 | 0.5-1.0 | Transparencia del side stack (para no tapar port scene) |
| `NEWS_BANNER_AUTO_SCROLL` | 5s | 3-10s | Intervalo de auto-scroll del news carousel |
| `NEWS_BANNER_WIDTH_RATIO` | 0.40 | 0.30-0.50 | Ancho del banner como ratio del screen width |
| `AVENTURA_PARALLAX_SPEED` | 0.5 | 0.0-1.0 | Velocidad del parallax en sailing route (0=off) |
| `BANNER_ART_HEIGHT_RATIO` | 0.60 | 0.50-0.70 | Altura del banner art en Taberna como ratio de screen |
| `POPUP_BACKDROP_OPACITY` | 0.50 | 0.30-0.70 | Oscurecimiento del fondo en popups |
| `UNIT_CARD_COLUMNS_PORTRAIT` | 4 | 3-5 | Columnas en grid de roster (portrait) |
| `UNIT_CARD_COLUMNS_LANDSCAPE` | 6 | 4-8 | Columnas en grid de roster (WebGL landscape) |

### Knobs de Game Flow que afectan este sistema (referencia)

- `LOADING_SCREEN_THRESHOLD` (1000ms): cuándo mostrar loading screen
- `TRANSITION_ANIM_DURATION` (200ms): velocidad de animación push/pop
- `BADGE_REFRESH_INTERVAL` (30s): refresh de badge counts
- `NAV_STACK_MAX_DEPTH` (10): profundidad máxima de nav stack por tab

## Acceptance Criteria

| # | Criterio | Verificación |
|---|----------|-------------|
| AC-1 | Puerto muestra port scene full-screen con lead unit, news banner, y side stack | Abrir app → verificar layout matches wireframe |
| AC-2 | Side stack icons muestran badges correctos (mail, logros, misiones) | Tener 3 mails sin leer → badge muestra "3" en icon de mail |
| AC-3 | News banner auto-scroll funciona y tap navega al destino correcto | Esperar 5s → banner cambia. Tap → navega al evento/banner correcto |
| AC-4 | Aventura muestra sailing route scrollable con stages como nodos | Abrir Aventura → route visible, scroll funciona, stages posicionados en ruta |
| AC-5 | Ship marker (⛵) posicionado en el siguiente stage sin clear | Tener 1-1 y 1-2 cleared → ship en 1-3 |
| AC-6 | Tap stage node → Stage Detail popup con missions, rewards, Deploy | Tap 1-1 → popup muestra 3 misiones, rewards, botón Deploy |
| AC-7 | Tripulación sub-tabs (Unidades, Barcos, Equipos) funcionan | Tap cada sub-tab → contenido correcto se muestra |
| AC-8 | Sort y filter en roster producen orden/filtrado correcto | Filtrar 5★ → solo 5★ visibles. Sort by Level → orden correcto |
| AC-9 | Taberna muestra banner hero (~60% screen) con pull buttons overlay | Abrir Taberna → banner domina, botones visibles sobre el art |
| AC-10 | Pull flow 2 pasos: tap Summon → popup Single/Multi con costes | Tap "GDC Summon" → popup muestra Single 💎300 / Multi 💎3000 |
| AC-11 | Swipe entre banners cambia los botones disponibles (GDC+TIF vs TIE) | Swipe a Standard → solo botón TIE visible, no GDC |
| AC-12 | Pity info visible y tap expande detalles de 3 counters | Pity "73/90" visible. Tap → popup con TIE/GDC/TIF pity desglosado |
| AC-13 | Tienda de Almas funcional si fragments ≥ 300 | Con 300+ fragmentos → browse units, seleccionar, confirmar |
| AC-14 | Settings accesible desde Puerto (⚙️) y desde Tienda | Ambas rutas abren la misma Settings screen |
| AC-15 | **Performance**: tab switch < 100ms, all screens render at 60fps | Measure tab switch latency. No frame drops during scroll/transitions |
| AC-16 | **Responsive**: layout correcto en 1080×1920 y 1920×1080 (WebGL) | Verificar en ambas resoluciones, sin overlap ni clipping |

## Visual/Audio Requirements

### Visual

- **Art Style**: Pixel art chibi (gameplay sprites, 2-head proportions) + anime
  illustration (retratos, banner art). UI chrome: weathered wood + gold metal trim
- **Color Palette**: Warm sunset oranges + deep ocean blues + gold accents para UI.
  Weathered wood browns para paneles. Parchment cream para popups/cards
- **Puerto scene**: Hand-painted 2D background. Caribbean colonial town, terracotta
  roofs, stone pier, turquoise sea. Animated: waves, seagulls, lantern glow.
  Day/night cycle matching device time. Lead unit: chibi sprite on ship deck
- **Aventura route**: Illustrated sea chart background with parallax scroll. Island
  nodes with thematic art per stage. Dotted path with animated water flow. Ship
  marker with wake effect. Cleared stages bright + lit lanterns, locked = foggy
- **Taberna banner art**: Full anime illustration of featured unit(s) in dramatic
  pose. Rarity glow effect behind character (gold for 5★)
- **Nav bar**: Weathered wood plank texture. Hand-drawn pixel art icons (32×32dp).
  Gold glow on active tab. Red badge circles with white numbers
- **Popups**: Parchment texture background, rounded corners, gold border trim.
  Semi-transparent black backdrop (50% opacity)
- **Transitions**: Slide left/right for sub-screens. Wave/dissolve for scene changes.
  Port scene: subtle idle animation loop (waves, clouds, seagull flight path)

### Audio

| Screen/Action | Audio | Notes |
|---------------|-------|-------|
| Puerto (Home) | BGM: calm port theme (acoustic guitar, waves, harbor ambience) | Seamless loop |
| Aventura | BGM: adventure map theme (more energetic, drum-driven) | Changes per chapter (future) |
| Tripulación | BGM: continues Puerto theme | No separate BGM |
| Taberna | BGM: mysterious tavern theme (accordion, murmurs, glass clinks) | Builds tension for pulls |
| Tab switch | SFX: subtle wooden "click" | Non-intrusive |
| Back button | SFX: soft canvas flap | Non-intrusive |
| Popup open | SFX: parchment unfold | Soft |
| Popup close | SFX: parchment fold | Soft |
| Badge update | No SFX | Visual only — audio on badge would be annoying |
| Stage select | SFX: anchor chain rattle | Thematic |
| Pull button tap | SFX: coin/gem clink | Satisfying |

## UI Requirements

Nota: la mayoría de specs UI están integradas en Core Rules (§1-8) por ser un sistema
de UI. Esta sección captura reglas transversales no cubiertas arriba.

### Typography

| Use | Font Style | Size | Color |
|-----|-----------|------|-------|
| Player name, headers | Pirate-themed display font (e.g., Pirata One) | 18-24sp | Gold |
| Body text, descriptions | Clean sans-serif (e.g., Noto Sans) | 14sp | Cream/white |
| Numbers (currencies, stats) | Monospace or tabular | 14-16sp | White, gold for currencies |
| Labels (nav bar, buttons) | Same as body, bold | 10-12sp | Cream (inactive), Gold (active) |
| Badges | Bold sans | 10sp | White on red |

### Iconography

- Element icons: 16×16dp, flat color-coded per element (Pólvora=red, Tormenta=blue,
  Maldición=purple, Bestia=green, Acero=grey, Luz=yellow, Sombra=dark purple)
- Currency icons: 24×24dp, detailed (gold doubloon, purple skull gem, blue lightning)
- Side stack icons: 32×32dp, line art style, semi-transparent circle background
- Nav bar icons: 32×32dp, pixel art hand-drawn, thematic to each section

### Animation Standards

| Animation | Duration | Easing |
|-----------|----------|--------|
| Tab switch content | Instant (<100ms) | None |
| Sub-screen push | 200ms | Ease-out |
| Sub-screen pop | 200ms | Ease-in |
| Popup appear | 150ms | Ease-out (scale 0.9→1.0 + fade in) |
| Popup dismiss | 100ms | Ease-in (fade out) |
| Badge count change | 300ms | Bounce |
| Currency count change | 500ms | Count-up with ease-out |
| News banner auto-scroll | 400ms | Ease-in-out |

## Open Questions

| # | Pregunta | Owner | Estado |
|---|----------|-------|--------|
| OQ-1 | ¿Necesitamos una pantalla de "Player Profile" al tap en el nombre del jugador, o es feature futura? | Game Design | Diferido — future feature |
| OQ-2 | ¿La sailing route de Aventura necesita un mini-mapa o indicador de posición cuando la ruta es larga? | UX | Resolver al implementar Ch.2+ |
| OQ-3 | ¿El port scene day/night cycle afecta gameplay o es solo visual? | Game Design | Solo visual (cosmético) |
| OQ-4 | ¿Loading screen tips son estáticos o contextuales (tips sobre el siguiente sistema)? | UX | Estáticos para demo, contextuales en futuro |
