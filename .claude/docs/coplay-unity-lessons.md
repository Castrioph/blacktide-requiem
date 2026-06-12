# Coplay + Unity UI: Lecciones Aprendidas

> Sesión 2026-04-21 — S3-06 + S3-07. Flujo completo confirmado por el usuario.

---

## 1. Límites de Coplay

| Situación | Problema | Solución |
|-----------|----------|----------|
| `capture_ui_canvas` en Edit Mode | Canvas ScreenSpaceOverlay no se renderiza vía cámara → imagen negra | Solo capturar en **Play Mode** |
| `execute_script` accede a singletons | Assembly separado → `.Instance` siempre null | `Object.FindFirstObjectByType<T>()` |
| Reflection cross-assembly con `as T` | El tipo del juego ≠ tipo del executor → devuelve null aunque el campo esté asignado | Usar `f.GetValue(obj)` raw; para diagnosticar, añadir `Debug.Log` directo en el método del juego |
| `GetPersistentEventCount() == 0` | Solo cuenta listeners del Inspector, no los de `AddListener()` | Count 0 es normal para botones cableados en runtime |
| `EditorSceneManager.OpenScene` en Play | Lanza excepción | `stop_game` → script editor → `play_game` |
| `FindFirstObjectByType<ScriptableObject>` | SOs no están en escena | `AssetDatabase.LoadAssetAtPath<T>("Assets/...")` |

---

## 2. Unity UI — Configuración Correcta

**Scroll lists (VLG + prefabs):**
```csharp
// VerticalLayoutGroup en Content
vlg.childControlWidth = true; vlg.childControlHeight = true;
vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false;

// ContentSizeFitter en Content
csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

// Content pivot — evita el hueco vacío al inicio del scroll
contentRT.pivot = new Vector2(0.5f, 1f);

// Viewport Image — casi transparente para no tapar el contenido
viewportImage.color = new Color(1f, 1f, 1f, 0.004f);

// Cada prefab hijo necesita LayoutElement
le.preferredHeight = 90f;
```

**Anchors corruptos (valor > 1.0):** elementos quedan fuera de pantalla. Verificar siempre tras scripts de creación automática de escenas. Fix: asignar valores explícitos por elemento, no resetear todo a `(0.5, 0.5)`.

**Editar prefabs desde editor scripts:** usar `PrefabUtility.EditPrefabContentsScope`. `AssetDatabase.LoadAssetAtPath` + modificar directo no serializa correctamente.

---

## 3. Flujo de Verificación Visual

```
stop_game → open_scene(MainMenu) → play_game
→ execute_script(SimulateXxx.cs) → capture_ui_canvas → verificar
→ stop_game si hay que corregir
```

Scripts disponibles en `Assets/Editor/`:
- `SimulateClick.cs` → MainMenu → StageSelect
- `SimulateLaunch.cs` → StageSelect → TeamSelect
- `SimulateCombat.cs` → TeamSelect → Combat
- `SimulateResults.cs` → Combat → Results
- `SimulateReturnMenu.cs` → Results → MainMenu

---

## 4. Paleta Visual — CANÓNICA S3-11 (2026-06-12)

> Fuente de verdad: `docs/art/ui-s311-visual-design.md` §1.2. Reemplaza valores previos.

```csharp
// Fondos
Color BgDark       = new Color(0.08f, 0.06f, 0.14f);   // #140F24 — fondo pantalla
Color PanelDark    = new Color(0.12f, 0.09f, 0.18f);   // #1F172E — paneles, cards
Color HeaderBase   = new Color(0.10f, 0.05f, 0.00f);   // #1A0D00 — header/footer

// Familia oro
Color Gold         = new Color(0.83f, 0.63f, 0.09f);   // #D4A017 — títulos, btn activo
Color GoldBright   = new Color(1.00f, 0.84f, 0.00f);   // #FFD700 — bisel catch-light
Color GoldMid      = new Color(0.91f, 0.71f, 0.13f);   // #E8B420 — btn highlighted
Color GoldDark     = new Color(0.72f, 0.53f, 0.06f);   // #B8880F — btn pressed

// Texto
Color Cream        = new Color(0.93f, 0.85f, 0.64f);   // #EDD9A3 — texto secundario
Color CreamMuted   = new Color(0.96f, 0.90f, 0.78f);   // #F5E6C8 — nombres destacados

// Botones
Color BtnDisabledBg = new Color(0.24f, 0.19f, 0.13f);             // #3D3020
Color DisabledLabel = new Color(0.63f, 0.50f, 0.25f, 0.70f);      // #A08040 a180 (WCAG AA)

// Cards madera
Color WoodBase     = new Color(0.24f, 0.16f, 0.06f, 0.902f);      // #3D2810 a230
Color WoodBorder   = new Color(0.36f, 0.24f, 0.12f);              // #5C3D1E

// Estados de slot (TeamSelect)
Color SlotEmpty        = new Color(0.12f, 0.09f, 0.18f);  // #1F172E
Color SlotFilled       = new Color(0.16f, 0.12f, 0.06f);  // #2A1E10
Color SlotBorderEmpty  = new Color(0.23f, 0.16f, 0.31f);  // #3A2A50
Color SlotBorderFilled = new Color(0.83f, 0.63f, 0.09f);  // = Gold

// Acentos por stage (stripe izq / dots dificultad) — ver StageAccentPalette.cs
// stage_001 Bahía Corsaria: #1E88E5 / #4FC3F7
// stage_002 Muelle Maldito: #6A1B9A / #CE93D8
// stage_003 Templo Vudú:    #BF360C / #FF8A65

// Selección (gestionado por script):
Color BorderUnselected = new Color(0.361f, 0.239f, 0.118f);
Color BorderSelected   = new Color(0.831f, 0.627f, 0.090f);
```

---

## 5. Checklist UI — Antes de Marcar Done

- [ ] **Game view en portrait 1080×1920** antes de playtest (Free Aspect apaisado
      deforma la UI en barras gigantes). Fix: `SetPortraitGameView.Execute`
      (Assets/Editor/) o elegir "Blacktide 1080x1920" en el dropdown de aspecto.
- [ ] EventSystem usa `InputSystemUIInputModule` (proyecto es Input System-only;
      `StandaloneInputModule` lanza excepción por frame y mata el input real.
      Fix masivo: `FixEventSystemModules.Execute`)
- [ ] Anchors en rango [0..1] en todos los RectTransforms
- [ ] Scroll: VLG `childControlWidth/Height=true`, `ContentSizeFitter`, pivot `(0.5,1)`, Viewport alpha 0.004
- [ ] Prefabs en scroll: tienen `LayoutElement.preferredHeight`
- [ ] Colores de la paleta de sección 4
- [ ] `capture_ui_canvas` verificado en Play Mode
- [ ] Playtest manual confirmado por el usuario
