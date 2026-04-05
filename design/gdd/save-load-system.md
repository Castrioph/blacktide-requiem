# Save/Load System

> **Status**: Designed
> **Author**: User + Claude Code Game Studios agents
> **Last Updated**: 2026-04-01
> **Implements Pillar**: Pillar 4 (Respeto al Tiempo del Jugador)

## Overview

El Save/Load System gestiona la persistencia de todo el estado del jugador en
almacenamiento local del dispositivo. El sistema opera con un único slot de
guardado automático — el jugador nunca interactúa directamente con "guardar" o
"cargar". Los datos se persisten automáticamente en momentos clave (tras completar
un stage, tras un pull de gacha, tras aplicar una mejora) y al salir de la app.

El sistema serializa y deserializa el estado completo del jugador: roster de
unidades (niveles, awakening, duplicados, equipamiento asignado), flota de barcos
(upgrades, crew assignments), currencies (DOB, GDC), inventario (TIE, TIF, Ron,
Cristales, Fragmentos de Alma, materiales de barco, piezas de equipamiento),
progreso de gacha (3 pity counters, 2 estados 50/50), progreso de stages (clears,
first-clears, misiones), estado de rewards (login streak, calendario mensual,
misiones diarias, logros), composiciones de equipo, y configuración del jugador.

La demo usa almacenamiento local exclusivamente — no hay servidor ni cloud sync.
La arquitectura está diseñada para que un futuro cloud backup o server-authoritative
save pueda añadirse sin reestructurar el modelo de datos.

Sin este sistema, cada sesión empieza desde cero. Es la diferencia entre un juego
y una demo técnica. El sistema es invisible cuando funciona y catastrófico cuando
falla — por eso la integridad de datos es la prioridad #1.

## Player Fantasy

**"Mi progreso es indestructible."** El jugador nunca piensa en el save system —
simplemente cierra la app y al día siguiente todo está exactamente como lo dejó.
La tripulación que construyó, el barco que mejoró, las gemas que ahorró durante
semanas, el pity a 73/90... todo intacto. La confianza en la persistencia es
silenciosa pero absoluta.

Este es un sistema que "no notas" (infraestructura, no espectáculo). La única
emoción que debe generar es **alivio retroactivo**: "menos mal que no perdí nada"
después de un crash, un cierre forzado, o una batería agotada. La ausencia de
ansiedad sobre perder datos ES la emoción objetivo.

El sistema fracasa si el jugador siente la necesidad de "guardar manualmente"
antes de cerrar la app, si alguna vez descubre que perdió progreso tras un crash,
o si el arranque del juego tarda notablemente más por la carga de datos.

## Detailed Design

### Core Rules

#### 1. Save Architecture

- **1 save file** per player: `save.json` stored in Unity's `Application.persistentDataPath`
- **Format**: JSON (human-readable, debuggeable). No encryption in demo
- **Auto-save only**: No manual save/load UI. The system saves automatically at trigger points
- **Single-write pattern**: The entire save state is serialized and written atomically (write to temp file → rename to `save.json`). This prevents corruption from partial writes during crashes
- **Backup file**: Before each write, the previous `save.json` is renamed to `save.backup.json`. If the primary file is corrupted on load, the backup is used automatically

#### 2. Save Data Schema

```
SaveData {
  meta: SaveMeta
  currencies: CurrencyState
  inventory: InventoryState
  units: List<UnitSaveState>
  ships: List<ShipSaveState>
  teams: TeamState
  gacha: GachaState
  stages: StageProgressState
  rewards: RewardsState
  settings: PlayerSettings
}
```

**SaveMeta:**

| Field | Type | Description |
|-------|------|-------------|
| `version` | int | Schema version for migration support |
| `lastSaveTimestamp` | long | UTC timestamp of last save (Unix ms) |
| `playTimeSeconds` | int | Total accumulated play time |
| `playerName` | string | Display name (set on first launch) |

**CurrencyState:**

| Field | Type | Description |
|-------|------|-------------|
| `doblones` | int | DOB balance |
| `gemasDeCalavera` | int | GDC balance |

**InventoryState:**

| Field | Type | Description |
|-------|------|-------------|
| `tickets` | {TIE: int, TIF: int} | Gacha ticket balances |
| `ron` | {anejo: int, capitan: int, legendario: int} | XP items |
| `cristales` | Map\<Element, {t1: int, t2: int, t3: int}\> | Awakening materials per element (7 elements + Universal) |
| `fragmentosDeAlma` | int | Soul Fragment count |
| `shipMaterials` | Map\<MaterialId, int\> | Ship construction/upgrade materials |
| `equipment` | List\<EquipmentSaveState\> | Owned equipment pieces (details TBD — Equipment System #22 not yet designed) |

**UnitSaveState:**

| Field | Type | Description |
|-------|------|-------------|
| `templateId` | string | Reference to UnitData template (e.g., `"elena_storm"`) |
| `level` | int | Current level |
| `currentXP` | int | XP accumulated toward next level |
| `awakeningTier` | int | 0-3 (0 = not awakened) |
| `dupeCount` | int | 0-4+ (stat bonus dupes received) |
| `equippedItems` | List\<EquipmentId?\> | Equipment in each slot (3 slots, null if empty) |

**ShipSaveState:**

| Field | Type | Description |
|-------|------|-------------|
| `shipId` | string | Reference to ShipData template |
| `owned` | bool | Whether the player has acquired this ship |
| `hullLevel` | int | 0-3 |
| `cannonsLevel` | int | 0-3 |
| `sailsLevel` | int | 0-3 |
| `crewAssignments` | Map\<SlotIndex, UnitTemplateId?\> | Which unit is assigned to each role slot |

**TeamState:**

| Field | Type | Description |
|-------|------|-------------|
| `landTeam` | List\<UnitTemplateId?\> | 5 slots + 1 guest slot (6 total) |
| `activeShipId` | string? | Currently selected ship for naval combat |

**GachaState:**

| Field | Type | Description |
|-------|------|-------------|
| `pityCounterTIE` | int | Standard banner pity (0-89) |
| `pityCounterGDC` | int | Featured banner GDC pity (0-89) |
| `pityCounterTIF` | int | Featured banner TIF pity (0-89) |
| `fiftyFiftyStateGDC` | enum | FiftyFifty or Guaranteed |
| `fiftyFiftyStateTIF` | enum | FiftyFifty or Guaranteed |
| `pullHistory` | List\<PullRecord\> | Last N pulls for history screen |

**StageProgressState:**

| Field | Type | Description |
|-------|------|-------------|
| `clearedBattles` | Set\<BattleId\> | All battles the player has cleared at least once |
| `missionCompletion` | Map\<BattleId, List\<bool\>\> | Per-battle mission completion (3 missions per battle) |

**RewardsState:**

| Field | Type | Description |
|-------|------|-------------|
| `loginStreakDay` | int | Current streak day (1-28) |
| `lastLoginDate` | string | ISO date of last login (UTC, `"2026-04-01"`) |
| `monthlyCalendarClaimed` | Set\<int\> | Days of current month already claimed |
| `currentMonth` | int | Month being tracked (1-12) for calendar reset |
| `dailyMissions` | DailyMissionState | Today's 3 missions + progress |
| `missionStreakDays` | int | Consecutive days with all 3 missions completed (0-7) |
| `achievements` | Map\<AchievementId, AchievementStatus\> | Locked/InProgress/Completed/Claimed |

**PlayerSettings:**

| Field | Type | Description |
|-------|------|-------------|
| `musicVolume` | float | 0.0-1.0 |
| `sfxVolume` | float | 0.0-1.0 |
| `language` | string | Locale code |
| `autoBattleSpeed` | int | 1x, 2x, 4x (x4 unlocked after Chapter 1 clear — derived from `clearedBattles`) |

#### 3. Save Triggers (auto-save points)

| Trigger | What Changed | Why Here |
|---------|-------------|----------|
| Stage clear (victory) | Stage progress, currencies, XP, inventory | Most important — rewards are the primary value gain |
| Gacha pull resolved | Gacha state, roster, currencies/tickets, inventory (dupes→fragments) | Pull results are irreplaceable — must persist before animation |
| Awakening/level up applied | Unit state, currencies, inventory | Player invested resources — losing this feels worst |
| Ship upgrade applied | Ship state, inventory | Material investment |
| Equipment change | Unit state, inventory | Gear assignment |
| Crew assignment changed | Ship state | Team composition |
| Team composition changed | Team state | Team composition |
| Login reward claimed | Rewards state, currencies, inventory | Daily progress |
| Mission reward claimed | Rewards state, currencies | Daily progress |
| Achievement claimed | Rewards state, currencies, inventory | One-time progress |
| App pause/background | Full state | Catch-all for anything missed |
| App quit | Full state | Final checkpoint |

#### 4. Load Sequence

1. App launches → check for `save.json` in `persistentDataPath`
2. If exists: deserialize JSON → validate schema version → migrate if needed → populate runtime state
3. If missing but `save.backup.json` exists: use backup, log warning
4. If neither exists: first launch → create default save with starter state (tutorial unit, 0 currencies, etc.)
5. If file exists but fails to parse: attempt backup → if backup also fails → prompt player with "Data corrupted" screen offering "Borrar cuenta e iniciar de nuevo" (with double confirmation, last resort)

#### 5. Schema Versioning

The `version` field in SaveMeta allows data migration when the save format changes between game updates:

```
On load:
  if save.version < CURRENT_VERSION:
    for each version from save.version+1 to CURRENT_VERSION:
      apply migration(version)
    save.version = CURRENT_VERSION
    write updated save
```

Each migration is a function that transforms the save data from version N to N+1. Migrations are additive — new fields get default values, removed fields are ignored. This ensures old saves always work with new game versions.

### States and Transitions

| State | Description | Transitions To |
|-------|-------------|----------------|
| **No Save** | First launch. No save file exists | → Initializing |
| **Initializing** | Creating default save with starter data | → Loaded |
| **Loading** | Reading and deserializing save.json on app launch | → Loaded, → Migrating, → Recovery |
| **Migrating** | Save version < current. Applying sequential migrations | → Loaded |
| **Loaded** | Runtime state is populated from save data. Game is playable | → Saving |
| **Saving** | Serializing and writing save to disk (triggered by auto-save) | → Loaded |
| **Recovery** | Primary save failed to parse. Attempting backup | → Loaded (backup OK), → Corrupted |
| **Corrupted** | Both primary and backup unreadable. Player prompted to start over | → Initializing (new game) |

**Key rules:**
- **Saving is non-blocking**: serialize in memory, write async to disk. The player never sees a loading spinner for saves
- **Loading is blocking**: on app launch only. The game does not start until load completes. Target: <500ms for demo-size save files
- The system never enters **Saving** during combat. In-combat state (current HP, buffs, wave index) is NOT persisted — if the app crashes mid-combat, the player retries the stage (consistent with "no penalty for losing" rule for story stages)

### Interactions with Other Systems

| System | Direction | Data Interface |
|--------|-----------|----------------|
| **Currency System** | CS ↔ S/L | Reads `GetBalance(DOB)`, `GetBalance(GDC)` on save. Restores balances on load via `SetBalance()`. Save triggers after any currency change |
| **Unit Data Model** | UDM ↔ S/L | S/L persists player-owned unit instances (level, XP, awakening, dupes, equipment). UDM provides template data for validation on load |
| **Ship Data Model** | SDM ↔ S/L | S/L persists ship ownership, upgrade levels, crew assignments. SDM provides template for slot validation on load |
| **Sistema Gacha** | SG ↔ S/L | S/L persists 3 pity counters, 2 fifty-fifty states, pull history. Save triggers before gacha animation (pull already resolved) |
| **Progresión de Unidades** | PU ↔ S/L | S/L persists level, XP, awakening tier, dupe count per unit. Also persists inventory (Ron, Cristales, Fragmentos) |
| **Rewards System** | RS ↔ S/L | S/L persists login streak, calendar progress, daily mission progress, achievement status. Save triggers on reward claim |
| **Stage System** | SS ↔ S/L | S/L persists cleared battles and mission completion flags. Save triggers on stage clear |
| **Team Composition** | TC ↔ S/L | S/L persists land team and active ship selection. Save triggers on team change |
| **Unit Roster/Inventory** | UR ↔ S/L | S/L persists the full inventory: TIE, TIF, materials, equipment. Roster reads unit/ship state from S/L on load |
| **Equipment System** | EQ ↔ S/L | S/L persists owned equipment and per-unit equipment assignments. Interface TBD (Equipment System #22 not designed) |
| **Game Flow** | GF → S/L | Game Flow triggers load on app launch and save on app pause/quit |

**Interface ownership**: S/L owns the save file format and read/write logic. Each system owns its own runtime state — S/L only serializes/deserializes, never modifies game state directly. On load, S/L populates each system's state through their public interfaces.

## Formulas

### 1. Save File Size Estimate

```
SaveSize ≈ META_OVERHEAD + (UNIT_COUNT × UNIT_RECORD_SIZE) +
           (SHIP_COUNT × SHIP_RECORD_SIZE) + INVENTORY_SIZE +
           STAGES_SIZE + GACHA_SIZE + REWARDS_SIZE
```

| Component | Est. Size (JSON) | Demo Max |
|-----------|-----------------|----------|
| SaveMeta | ~200 bytes | Fixed |
| CurrencyState | ~50 bytes | Fixed |
| InventoryState | ~800 bytes | 8 elements × 3 tiers + tickets + materials |
| UnitSaveState (×12) | ~150 bytes/unit | 12 units × 150 = ~1,800 bytes |
| ShipSaveState (×3) | ~300 bytes/ship | 3 ships × 300 = ~900 bytes |
| TeamState | ~200 bytes | Fixed |
| GachaState | ~500 bytes + history | ~100 pulls × 50 bytes = ~5,500 bytes |
| StageProgressState | ~50 bytes/battle | ~20 battles × 50 = ~1,000 bytes |
| RewardsState | ~2,000 bytes | 53 achievements + missions + login |
| PlayerSettings | ~100 bytes | Fixed |
| **Total estimate** | | **~13 KB** (demo max) |

Well within mobile storage limits. JSON overhead vs binary is ~2x but irrelevant
at this scale.

### 2. Save/Load Performance Budget

| Operation | Target | Method |
|-----------|--------|--------|
| Serialize (memory) | <5ms | JSON serialization of ~13 KB |
| Write to disk | <50ms | Async file write |
| Deserialize (load) | <10ms | JSON parse of ~13 KB |
| Total load time | <100ms | Including file read + parse + state population |
| Total save time | <60ms | Non-blocking — player never waits |

## Edge Cases

| Edge Case | Resolution |
|-----------|------------|
| **App killed mid-save (partial write)** | Atomic write pattern: write to `save.tmp` first, then rename to `save.json`. If `save.tmp` exists on load, it was an incomplete write — ignore it and use `save.json` (previous good save) |
| **App crashes mid-combat** | Combat state is not persisted. On relaunch, player is at the stage select screen. No penalty — energy is not consumed for story stages (consumed on entry for event stages per Rewards System) |
| **Save file manually deleted by player (outside the game)** | Treated as first launch. New account starts. No recovery possible without cloud backup (future feature). This is equivalent to deleting the account from within the game |
| **Save file manually edited by player (JSON is plain text)** | Best effort: validate on load. If values are within valid ranges, accept. If invalid (negative currency, impossible awakening tier, templateId not found), clamp or discard the invalid entry. Log warning. Demo is single-player — no competitive advantage to cheating |
| **Device storage full — write fails** | Catch write exception. Retry once after short delay. If still fails, show warning toast "Could not save — free up storage space" but keep the game running with last good save intact |
| **Two save triggers fire simultaneously (e.g., stage clear + achievement)** | Saves are queued. Second trigger waits for first write to complete, then saves with the fully updated state. No concurrent writes |
| **Game updated, save version is old** | Migration chain runs: v1→v2→v3→...→current. Each migration adds new fields with defaults. No data loss. Save is rewritten at current version |
| **Game downgraded (save version > code version)** | Reject load. Show "This save was created with a newer version. Please update the game." Player cannot play with old code on new save |
| **Save file from different platform (mobile → WebGL)** | Same JSON format. `persistentDataPath` differs per platform but the schema is identical. Cross-platform transfer is theoretically possible via manual file copy (not officially supported in demo) |
| **Player name contains special characters / emoji** | JSON supports UTF-8 natively. No restrictions on playerName content. Max length: 20 characters |
| **pullHistory grows unbounded** | Cap at MAX_PULL_HISTORY (100 entries). Oldest entries are dropped when cap is exceeded. 100 × ~50 bytes = ~5 KB |
| **Achievement progress tracking for "complete 50 stages"** | Achievement progress counters are stored in RewardsState. On load, counters are restored — no need to recount from stage progress |
| **Login at 23:59 UTC, save, login at 00:01 UTC** | `lastLoginDate` stores ISO date string. Comparison is date-only (not timestamp). Two logins on different UTC dates = two login days, regardless of time gap |
| **IAP purchase succeeds but save fails to write after** | IAP receipts are verified against platform store API on each launch (see Currency System edge cases). Purchased GDC is re-credited if missing from save. Save failure does not lose real-money purchases |
| **First launch with no internet (WebGL)** | Local save works without internet. No cloud dependency in demo. WebGL IndexedDB is used as local storage |

## Dependencies

### Upstream (S/L depende de)

| System | Type | Interface | GDD |
|--------|------|-----------|-----|
| **Unit Data Model** | Hard | Provides template IDs for validation. S/L needs to know valid unit IDs on load | ✅ Approved |
| **Ship Data Model** | Hard | Provides template IDs and slot counts for validation. S/L validates crew assignments against ship slot config | ✅ Approved |
| **Currency System** | Hard | Provides `GetBalance`/`SetBalance` interface for DOB/GDC | ✅ Approved (updated) |

### Downstream (dependen de S/L)

| System | Type | What it needs | GDD |
|--------|------|---------------|-----|
| **Unit Roster/Inventory** | Hard | Full list of owned units with state + inventory items. Cannot display roster without loaded save | ⬜ Not Started |
| **All systems with persistent state** | Soft | Every system that has persisted data reads it from S/L on load. But systems can function with defaults if save is missing (first launch) | Various |

### Bidirectional (S/L reads and writes)

All systems listed in Interactions (§Interactions) have bidirectional data flow:
S/L reads their state on save and populates their state on load.

**Critical path**: Unit Data Model and Ship Data Model must be loaded (as
templates/assets) before save data can be validated. The load order is:
templates first → save data second → runtime state population third.

## Tuning Knobs

| Knob | Current Value | Safe Range | Gameplay Effect |
|------|--------------|------------|-----------------|
| `MAX_PULL_HISTORY` | 100 | 50-500 | Cap on stored pull records. Higher = more history visible but larger save file. 100 × ~50 bytes = ~5 KB |
| `SAVE_DEBOUNCE_MS` | 500 | 100-2000 | Minimum time between auto-saves. Prevents rapid-fire saves when multiple triggers fire quickly (e.g., claiming 3 mission rewards). Too low = excessive disk writes. Too high = risk of losing recent changes |
| `PLAYER_NAME_MAX_LENGTH` | 20 | 10-30 | Max characters for player display name |
| `BACKUP_COUNT` | 1 | 1-3 | Number of backup saves kept. 1 = single backup (save.backup.json). Higher = more safety but more storage used |
| `LOAD_TIMEOUT_MS` | 5000 | 2000-10000 | Maximum time to wait for file read before declaring failure. Handles corrupted files that cause parsing to hang |

### Knob Interactions

| Knob A | Knob B | Interaction |
|--------|--------|-------------|
| `SAVE_DEBOUNCE_MS` | Save triggers | If debounce is high (2s) and player claims a login reward then immediately does a gacha pull, only the later state is saved. Acceptable — the debounce catches the final state |
| `MAX_PULL_HISTORY` | Gacha UI | The history screen shows this many entries. If reduced, old history disappears silently (no notification) |

## Visual/Audio Requirements

**Visual:**
- **Auto-save indicator**: Icono pequeño y discreto en esquina inferior (spinning
  compass/anchor) que aparece brevemente (~0.5s) cuando el auto-save se ejecuta.
  No bloquea input, no requiere atención. Desaparece automáticamente
- **Account delete screen**: Pantalla sobria con icono de advertencia (ancla rota
  o pergamino dañado). Texto explicativo severo + doble confirmación. Sin
  melodrama pero debe ser claro que la acción es irreversible
- **Data corrupted screen**: Misma estética que account delete. Texto: "Los datos
  están dañados." Botón "Borrar cuenta e iniciar de nuevo" con doble confirmación
- **Loading screen (app launch)**: El save load es tan rápido (<100ms) que no
  necesita su propia pantalla. Se integra en el splash/loading screen del Game Flow

**Audio:**
- **Auto-save**: Sin SFX. El save es silencioso y frecuente — un sonido cada vez
  sería intrusivo
- **Data corrupted / account delete**: SFX sutil de error/advertencia (no
  alarmante). Un tono bajo + click mecánico

## UI Requirements

- **No save/load UI**: No hay pantalla de "guardar partida" ni "cargar partida".
  Todo es automático
- **Auto-save icon**: Spinning compass (~24×24px) en esquina inferior derecha.
  Aparece 0.5s en cada auto-save. Opacidad 50% para no distraer. No interactivo
- **No "New Game" option**: El jugador simula tener una cuenta permanente. No hay
  opción de empezar de nuevo desde el menú principal
- **Account delete (Settings)**: Opción "Borrar cuenta" en el menú de settings,
  al final de la lista, en rojo. Requiere doble confirmación:
  1. "¿Estás seguro? Se borrará TODO tu progreso, unidades, y recursos. Esta
     acción es IRREVERSIBLE." → Botón "Sí, borrar todo"
  2. "Escribe BORRAR para confirmar" → Input de texto obligatorio
  - Tras confirmar: save file se elimina, app se reinicia como primer launch
- **Data corrupted popup**: Modal centrado. Texto: "Los datos de guardado están
  dañados y no se pueden recuperar." Botón: "Borrar cuenta e iniciar de nuevo"
  con la misma doble confirmación que el delete manual
- **Settings persistence**: Los settings se guardan con el mismo auto-save. Si el
  jugador cambia el volumen y cierra la app, el cambio persiste

## Acceptance Criteria

**Save/Write**
1. Auto-save triggers after stage clear — save file contains updated stage progress + currencies + XP
2. Auto-save triggers after gacha pull — save file contains updated pity counter, roster, and tickets/GDC
3. Auto-save triggers on app pause/background — full state written
4. Atomic write: if app crashes during write, `save.json` is not corrupted (backup intact)
5. Save completes in <60ms (non-blocking — no visible delay to player)

**Load/Read**
6. On app launch, save file is loaded and all runtime state is populated correctly
7. Load completes in <100ms for demo-size saves (~13 KB)
8. If save.json is missing but backup exists, backup is loaded automatically
9. If no save exists, a default new-game state is created
10. If save file is corrupted beyond repair, player sees "Data corrupted" prompt with "Borrar cuenta e iniciar de nuevo" (double confirmation required)

**Data Integrity**
11. Currency balances survive save/load cycle without loss (DOB, GDC)
12. Unit state survives: level, XP, awakening tier, dupe count, equipped items
13. Ship state survives: ownership, all 3 component upgrade levels, crew assignments
14. Inventory survives: TIE, TIF, Ron (3 tiers), Cristales (7 elements × 3 tiers), Fragmentos, ship materials, equipment
15. Gacha state survives: 3 pity counters, 2 fifty-fifty states, pull history
16. Stage progress survives: cleared battles, per-mission completion flags
17. Rewards state survives: login streak day, last login date, monthly calendar, daily missions, achievements
18. Team compositions survive: land team (6 slots), active ship
19. Player settings survive: volumes, language, auto-battle speed

**Schema Migration**
20. Old save (version N) is successfully migrated to current version (N+M) on load
21. New fields added by migration receive correct default values
22. Save from newer version than code → error message, not crash

**Edge Cases**
23. Save with invalid templateId (unit removed in update) → entry is discarded, rest loads normally
24. Save with negative currency values → clamped to 0
25. Rapid-fire save triggers (3 in <1s) → debounced, only final state written

## Open Questions

| # | Question | Impact | Status / Resolution |
|---|----------|--------|---------------------|
| 1 | ¿Qué pasa con GDC comprado via IAP si el jugador borra su cuenta? | Legal/monetización — si pagó dinero real, perder GDC es su decisión pero debe ser explícito en la doble confirmación | **Resuelto**: La doble confirmación advierte que TODO se pierde. El jugador acepta bajo su responsabilidad. No hay recuperación post-delete |
| 2 | ¿El pull history incluye banner y método de pago por entrada? | UI/data — afecta PullRecord size y lo que muestra el historial | **Resuelto**: Sí. PullRecord incluye: timestamp, bannerId, paymentMethod (GDC/TIF/TIE), unitTemplateId, rarity, isNew/isDupe |
| 3 | ¿Equipment System necesita campos adicionales en el save? (stat rolls, upgrade levels, rarity) | Schema — puede cambiar EquipmentSaveState | **Pendiente**: Se revisará el schema completo tras diseñar Equipment System (#22). Owner: Game Designer |
| 4 | ¿Dev save para testing? | QA/dev workflow | **Resuelto**: No necesario. El JSON es plain text y editable manualmente en la demo |
| 5 | ¿Cloud backup: Google Play Games Save API o solución custom? | Arquitectura futura | **Deferred**: Post-demo, solo si la demo tiene tracción. Owner: Technical Director |
