# Game Flow / Scene Manager

> **Status**: Approved
> **Author**: User + Claude Code Game Studios agents
> **Last Updated**: 2026-03-26
> **Implements Pillar**: Pillar 4 (Respeto al Tiempo del Jugador)

## Overview

The Game Flow / Scene Manager defines every screen in the game, the valid
transitions between them, and the navigation model the player uses to move
through the experience. It implements a **hub-based architecture** with a
persistent bottom navigation bar — the standard mobile gacha pattern proven by
FFBE, OPTC, and similar titles.

The player interacts with this system constantly but should never *notice* it.
Navigation is instant, intuitive, and always one tap away from any major
function. The Home screen ("Puerto") is the center of gravity — the place the
player returns to between activities. The bottom nav bar provides direct access
to major sections (content, unit management, gacha, etc.) from anywhere.

Without this system, there is no game — just disconnected scenes. It is the
skeleton that holds every other system's UI together, defines the player's
session flow, and ensures the core loop (stage → rewards → upgrade → repeat)
feels seamless rather than fragmented.

## Player Fantasy

**Effortless command over your pirate empire**: The player opens the game and
is immediately in their Puerto — their dock, their base of operations. From
here, the world is at their fingertips: set sail for adventure, manage their
crew, visit the tavern for new recruits, check their ship. The fantasy is
being a captain who commands everything from the helm — not a player wrestling
with menus.

**This is infrastructure, not spectacle**: Like the Currency System, this
system succeeds when it's invisible. The player should feel "I want to do X"
and be doing X within 1-2 taps. If navigation ever feels like an obstacle
between the player and fun, it has failed.

**Pillar 4 alignment — Respeto al Tiempo del Jugador**: Every transition
should be fast. Loading screens should be minimal or masked with thematic art.
The player who has 15 minutes should spend 14 of those in gameplay, not in
menus.

## Detailed Design

### Core Rules

#### 1. Navigation Model: Hub + Bottom Nav Bar

The game uses a **hub-based navigation** model with a **persistent bottom nav bar**
visible on all non-combat screens. The nav bar has 5 tabs:

| Tab | Name | Icon | Contains |
|-----|------|------|----------|
| 1 | **Puerto** | Anchor | Home hub, Daily login, News banners, Mail, Friends, Notifications |
| 2 | **Aventura** | Compass | Story stages, Event stages, Naval stages (future content modes) |
| 3 | **Tripulación** | Crew badge | Unit roster, Ship management, Team composition (land + naval) |
| 4 | **Taberna** | Skull mug | Gacha: unit summon banners, ship summon (future) |
| 5 | **Tienda** | Treasure chest | IAP gem packs, DOB shop, Promotions, Settings |

- **Puerto** is the default tab on app launch.
- The **active tab** is visually highlighted in the nav bar.
- The nav bar is **hidden during combat** and **narrative sequences** to maximize
  screen space. It reappears on the results screen.
- Tapping the active tab scrolls to top / resets to the section's root view.
- **Notification badges** (red dots with count) appear on tabs with pending items
  (unclaimed rewards, new mail, available gacha pity, etc.).

#### 2. Screen Hierarchy

The game has 3 layers of screens:

| Layer | Examples | Behavior |
|-------|----------|----------|
| **Full screens** | Puerto, Aventura, Combat, Gacha reveal | Replace the current view. Nav bar visible (except combat). |
| **Sub-screens** | Unit detail, Stage select, Ship crew editor | Push onto a navigation stack within a tab. Back button returns to parent. |
| **Overlays/Popups** | Confirmation dialogs, Reward popups, Insufficient funds | Appear on top of the current screen with a dimmed backdrop. Dismiss with tap outside or close button. |

Each tab maintains its own **navigation stack**. Switching tabs does NOT reset
the other tab's position — returning to Tripulación shows the same sub-screen
you left.

#### 3. App Launch Flow

```
[Cold Launch] → Splash Screen → Login (auto/silent) → Data sync →
→ Daily Login Reward popup (if unclaimed) → Puerto (Home)
```

- Splash screen: studio logo + pirate theme, max 2 seconds.
- Login: automatic/silent (no login screen for demo — local save only).
- Data sync: check for updates, download if needed. If offline, proceed with
  local data.
- Daily login reward: show popup only if today's reward is unclaimed. One tap
  to claim.
- First-ever launch: redirect to Tutorial/Onboarding instead of Puerto.

#### 4. Combat Flow (separate from hub navigation)

```
[Stage Select] → Friend/Guest unit select → Team preview →
→ [Narrative scene (optional, first-time only)] →
→ Combat (waves 1-N) → [Victory/Defeat] → Results screen →
→ Back to Stage Select (or retry on defeat)
```

- Combat is a **separate scene** that replaces the hub entirely (no nav bar).
- The results screen shows rewards earned and has two buttons:
  - **"Next"** → advance to next stage (if sequential)
  - **"Stage Select"** → return to stage list
  - On defeat: **"Retry"** (same team) or **"Edit Team"** (return to team setup)
- No energy is consumed on defeat (per game concept: "Sin penalización por
  perder").

#### 5. Navigation Rules

| Rule | Description |
|------|-------------|
| **1-tap access** | Every major function is accessible from the nav bar in 1 tap. |
| **2-tap max** | Any specific action (e.g., "level up unit X") requires at most 2 taps from the nav bar. |
| **Back button** | Every sub-screen has a visible back button (top-left). Android hardware back works identically. |
| **No dead ends** | Every screen has a way out. Combat can be paused and abandoned (forfeit). |
| **No forced detours** | The player is never forced to visit a screen they didn't choose (except tutorial, first launch only). |
| **Deep linking** | Shortcuts (from notifications, news banners, insufficient funds prompts) navigate directly to the target screen. |
| **State preservation** | Switching tabs preserves each tab's state. Only a full app restart resets tab positions. |

#### 6. Screen Lock / Unlock Progression

Some screens are locked until the player reaches certain milestones:

| Screen | Unlock Condition | Locked Behavior |
|--------|-----------------|-----------------|
| Aventura: Naval stages | Complete Chapter 1 of the story | Tab visible, section grayed out with "Complete Chapter 1 to set sail!" |
| Taberna: Ship summon | Complete first naval stage | Banner hidden until unlocked |
| Eventos | Complete Chapter 1 of the story | Section grayed out in Aventura with "Complete Chapter 1 to unlock Events" |
| Friends | Complete tutorial | Button hidden on Puerto |

Locked sections show a **clear unlock condition** ("Complete Chapter 3 to unlock
Naval Combat"), never just a lock icon with no explanation.

### States and Transitions

**Top-level app states:**

| State | Description | Nav Bar | Transitions To |
|-------|-------------|---------|---------------|
| **Splash** | Logo + loading | Hidden | → Login |
| **Login** | Auto-authenticate + sync | Hidden | → Puerto (or Tutorial if first launch) |
| **Hub** | Any nav bar tab active | Visible | → Combat, → Narrative |
| **Combat** | In a battle (land or naval) | Hidden | → Results |
| **Results** | Post-combat rewards | Hidden (shows own nav) | → Hub (stage select), → Combat (retry/next) |
| **Narrative** | Visual novel scene | Hidden | → Combat (if pre-stage), → Hub (if standalone) |
| **Popup** | Overlay on any state | Unchanged | → Previous state (dismiss) |

**Tab-internal navigation** uses a stack:
- Push: enter sub-screen (e.g., Tripulación → Unit Detail)
- Pop: back button returns to previous screen in the stack
- Reset: tapping the active tab clears the stack and returns to root

### Interactions with Other Systems

| System | Direction | Interface |
|--------|-----------|-----------|
| **Stage System** (#8) | Game Flow → | Game Flow provides the scene transition framework. Stage System populates Aventura with stage data and triggers combat scenes. |
| **Narrative System** (#21) | Game Flow → | Game Flow manages when narrative scenes play (pre/post stage). Narrative System provides the content. |
| **Currency System** (#4) | ← Currency | Currency top bar widget is rendered on all Hub screens. "+" buttons deep-link to Tienda. |
| **Menus & Navigation UI** (#19) | Game Flow → | Game Flow defines the screen structure and navigation rules. Menus UI implements the visual presentation. |
| **Energy System** (separate) | ← Energy | Energy display in Puerto top bar. Insufficient energy blocks stage entry with prompt to refill (deep-link to Tienda). |
| **Save/Load System** (#17) | ↔ Save | Auto-save triggers on key transitions: leaving combat, completing a transaction, switching tabs. Load restores last Hub state on app launch. |
| **Tutorial/Onboarding** (#23) | ← Tutorial | Tutorial can override Game Flow to force specific navigation (guide player to first stage, lock other tabs temporarily). |

## Formulas

### 1. Transition Timing Targets

| Transition | Max Duration | Notes |
|------------|-------------|-------|
| Tab switch (within hub) | Instant (< 100ms) | No loading — tabs are pre-loaded or cached |
| Sub-screen push/pop | < 200ms | Animated slide transition |
| Hub → Combat scene | < 2 seconds | Full scene load. Show thematic loading art if > 1s |
| Combat → Results | Instant (< 100ms) | Same scene, different UI state |
| Results → Hub | < 1 second | Scene unload + hub restore |
| App cold launch → Puerto | < 5 seconds | Splash + auth + sync + render |
| App warm resume | < 500ms | Restore from background |

### 2. Loading Screen Threshold

```
If TransitionTime > LOADING_SCREEN_THRESHOLD:
    Show themed loading screen (pirate art + tip)
Else:
    Direct transition (no loading screen)
```

| Variable | Value | Description |
|----------|-------|-------------|
| `LOADING_SCREEN_THRESHOLD` | 1,000ms | Show loading screen only if transition takes longer than this |

### 3. Notification Badge Count

```
TabBadgeCount = sum of pending items within that tab's scope
```

- Puerto: unclaimed daily login + unread mail + friend requests
- Aventura: new unlocked stages + expiring events
- Tripulación: units ready to level up / awaken (if materials sufficient)
- Taberna: pity guarantee available + free pull available
- Tienda: new/limited promotions

Badges display as red dot (if count = 0 but has "new" flag) or red circle with
number (if count > 0). Max display: "99+".

## Edge Cases

### Navigation Edge Cases

| Situation | Resolution |
|-----------|------------|
| Player presses back on Puerto (root of Home tab) | Show "Exit game?" confirmation dialog. Do not exit without confirmation. |
| Player presses back rapidly during transition | Queue back actions, process after transition completes. Never leave the player in a broken state. |
| Player switches tab while a sub-screen is loading | Cancel the pending load, switch to new tab. The abandoned tab retains its previous state (before the load started). |
| Player receives notification mid-combat | Queue it. Show after combat ends (on Results screen or when returning to Hub). Never interrupt combat. |
| App goes to background mid-combat | Pause combat. On resume, restore exact combat state. If app is killed, the combat is **lost** (no mid-combat save in demo). |
| App goes to background mid-transaction (gacha pull, IAP) | Transaction is atomic. If IAP: verify on resume via store API. If gacha: pull was resolved at start, animation can be replayed. |
| Deep link targets a locked screen | Show the lock message explaining the unlock condition instead of navigating to the screen. |
| Player taps nav bar tab they're already on | Reset that tab's navigation stack to root view (scroll to top if already at root). |
| Internet lost while in Hub | Continue with local data. Show subtle "Offline" indicator. Block actions that require connectivity (IAP, friend list). Retry silently on reconnect. |
| Internet lost during combat | Combat is local — no interruption. Rewards are calculated locally and synced when connectivity returns. |

### First-Launch Edge Cases

| Situation | Resolution |
|-----------|------------|
| Player force-closes during tutorial | On next launch, resume tutorial from last completed step. |
| Player completes tutorial but app crashes before saving | Tutorial completion state is saved at each step, not only at the end. |
| Player skips tutorial (if allowed) | All screens unlock immediately. A "Help" button in Puerto offers tutorial replay. |

## Dependencies

### Upstream Dependencies

None. Game Flow is a foundation system with no upstream dependencies.

### Downstream Dependents

| System | Dependency Type | Interface |
|--------|----------------|-----------|
| **Stage System** (#8) | Hard | Stage System lives within the Aventura tab. Uses Game Flow's scene transition API to launch combat and return to stage select. |
| **Narrative System** (#21) | Hard | Narrative scenes are managed by Game Flow (when to show, how to transition). Content comes from Narrative System. |
| **Menus & Navigation UI** (#19) | Hard | Implements the visual layer of everything Game Flow defines: nav bar, tab content, transitions, popups. |
| **Currency System** (#4) | Soft | Currency top bar widget is rendered on Hub screens. Game Flow defines which screens show it; Currency System provides the data. |
| **Tutorial/Onboarding** (#23) | Soft | Tutorial overrides Game Flow's normal navigation to guide first-time players. |

## Tuning Knobs

| Knob | Current Value | Range | What It Affects |
|------|--------------|-------|----------------|
| `LOADING_SCREEN_THRESHOLD` | 1,000ms | 500-2,000ms | When to show a loading screen vs. direct transition. Too low = flash of loading screen. Too high = perceived lag. |
| `SPLASH_DURATION` | 2,000ms | 1,000-3,000ms | How long the splash screen shows. Must meet platform requirements (some stores mandate minimum). |
| `TRANSITION_ANIM_DURATION` | 200ms | 100-400ms | Sub-screen push/pop animation speed. Too fast = jarring. Too slow = sluggish. |
| `NAV_STACK_MAX_DEPTH` | 10 | 5-20 | Maximum screens in a tab's navigation stack before oldest is discarded. Prevents memory bloat. |
| `DAILY_LOGIN_POPUP_DELAY` | 500ms | 0-2,000ms | Delay after Puerto loads before daily login popup appears. Gives the player a moment to orient. |
| `BADGE_REFRESH_INTERVAL` | 30s | 10-60s | How often notification badge counts are recalculated. Lower = more responsive but more CPU. |

### Knob Interactions

| Knob A | Knob B | Interaction |
|--------|--------|-------------|
| `NAV_STACK_MAX_DEPTH` | Memory budget | Deeper stacks = more cached screens = more memory. On low-end devices, reduce depth. |
| `TRANSITION_ANIM_DURATION` | `LOADING_SCREEN_THRESHOLD` | If anim is slow + loading threshold is low, player sees loading screen inside the animation. Keep anim < threshold. |

## Visual/Audio Requirements

### Visual

- **Bottom nav bar**: Pirate-themed wood/metal bar. Each icon is hand-drawn pixel
  art style. Active tab glows or has anchor highlight. Notification badges are red
  with white number.
- **Loading screens**: Full-screen pirate art (ship at sea, port town, treasure map)
  with a gameplay tip at the bottom. Rotating set of 5-10 images.
- **Splash screen**: Studio logo with pirate motif. Quick dissolve to login.
- **Transitions**: Slide left/right for sub-screen push/pop. Fade for full scene
  changes (hub ↔ combat). Gacha reveal has its own special transition (handled by
  Gacha System).
- **Puerto (Home)**: Animated port scene with the player's docked ship. The player's
  **selected lead unit** is displayed on the ship deck, facing the camera (sprite
  art, like FFBE/OPTC home character display). Waves, seagulls, harbor ambience.

### Audio

- **Puerto BGM**: Calm port town theme (acoustic guitar, waves, harbor ambience).
  Loops seamlessly.
- **Aventura BGM**: Adventure map theme (more energetic, drum-driven).
- **Tab switch SFX**: Subtle wooden "click" — non-intrusive, pirate-themed.
- **Back button SFX**: Soft "whoosh" or canvas flap.
- **Notification badge SFX**: None (visual only — audio on notification arrival
  would be annoying).
- **Loading screen**: Continue current BGM (no silence gap).

## UI Requirements

- **Top bar** (persistent on all Hub screens): Player rank, player name, currency
  display (DOB + GDC with "+" buttons), energy display (if energy system active).
- **Bottom nav bar**: 5 equally-spaced tabs. Each tab: icon + label text below.
  Active tab: highlighted color + slightly larger icon. Tabs are not customizable
  or reorderable.
- **Back button**: Top-left corner on all sub-screens. Consistent icon (arrow left).
  Not shown on tab root screens (nav bar handles navigation instead).
- **Popup overlays**: Centered card with dimmed backdrop (50% opacity black). Close
  button top-right + tap-outside-to-dismiss. Confirmation popups have two buttons
  (confirm/cancel).
- **Loading indicator**: For transitions > 500ms but < threshold: small spinner in
  center. For transitions > threshold: full loading screen.
- **Offline indicator**: Small "No connection" bar at the top (below the top bar),
  yellow/amber background. Dismisses automatically on reconnect.
- **Orientation**: Portrait only (locked). Landscape support planned for future
  updates — architecture should not preclude it, but it is not a demo requirement.

## Acceptance Criteria

### Navigation Validation

- [ ] All 5 nav bar tabs are visible and tappable from Puerto
- [ ] Tapping each tab navigates to the correct root screen
- [ ] Tapping the active tab resets its navigation stack to root
- [ ] Back button on each sub-screen returns to the parent screen
- [ ] Back button on Puerto root shows "Exit game?" dialog
- [ ] Android hardware back button behaves identically to on-screen back

### State Preservation

- [ ] Switching from Tab A → Tab B → Tab A preserves Tab A's sub-screen position
- [ ] App background → resume restores exact screen state (< 500ms)
- [ ] Full app restart loads Puerto as default tab

### Combat Flow

- [ ] Selecting a stage from Aventura transitions to combat within 2 seconds
- [ ] Nav bar is hidden during combat and narrative scenes
- [ ] Results screen offers "Next", "Stage Select", or "Retry"/"Edit Team" on defeat
- [ ] Defeat does not consume energy
- [ ] Returning from Results to Hub restores the Aventura tab at stage select

### Performance

- [ ] Tab switching completes in < 100ms (no perceivable delay)
- [ ] Sub-screen transitions complete in < 200ms
- [ ] Cold launch to Puerto in < 5 seconds on target hardware
- [ ] No frame drops during transition animations (maintain 60fps)

### Unlock / Lock System

- [ ] Locked screens show the unlock condition text, not just a lock icon
- [ ] Unlocking a screen immediately makes it accessible (no restart required)
- [ ] Deep link to a locked screen shows the lock message instead of crashing

### Edge Cases

- [ ] Rapid tab switching does not crash or leave UI in broken state
- [ ] Losing internet mid-hub shows "Offline" indicator and blocks online-only actions
- [ ] Losing internet mid-combat does not interrupt combat
- [ ] Notification badges update within `BADGE_REFRESH_INTERVAL` of state changes

## Open Questions

| Question | Owner | Target Resolution |
|----------|-------|-------------------|
| Exact chapter/stage count in "first chapter" that unlocks Naval + Eventos | Game Designer | Stage System GDD |
| How many free units does the player receive during Chapter 1 before Naval unlocks? | Economy Designer | Rewards System / Stage System GDD |
| Should loading screen tips be static or context-aware (show tips about the next system the player will encounter)? | UX Designer | Menus & Navigation UI GDD |

### Resolved During Design

| Question | Resolution |
|----------|-----------|
| Puerto character display | Lead unit sprite displayed on ship deck, facing camera |
| Nav bar customization | No — tabs are fixed, not reorderable |
| Tutorial skippable? | Yes — tutorial is optional, skippable. "Help" button on Puerto for replay. |
| Naval + Eventos unlock | After completing Chapter 1 of the story. Gives time for free units + enough GDC for a multi-pull. |
| Combat pause/quit | "Pause and abandon" with energy refunded (consistent with defeat = no energy loss). |
| Screen orientation | Portrait locked for demo. Landscape planned for future — architecture should not preclude it. |
