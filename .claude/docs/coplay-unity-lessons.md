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

## 4. Paleta Visual

```csharp
Color BgDark    = new Color(0.08f, 0.06f, 0.14f, 1f);  // fondo
Color PanelDark = new Color(0.12f, 0.09f, 0.18f, 1f);  // cards
Color Gold      = new Color(0.95f, 0.78f, 0.25f, 1f);  // títulos
Color Cream     = new Color(0.92f, 0.88f, 0.72f, 1f);  // texto
Color BtnActive = new Color(0.60f, 0.42f, 0.10f, 1f);  // botón on
Color BtnOff    = new Color(0.24f, 0.19f, 0.13f, 1f);  // botón off
// Selección (gestionado por script):
Color BorderUnselected = new Color(0.361f, 0.239f, 0.118f);
Color BorderSelected   = new Color(0.831f, 0.627f, 0.090f);
```

---

## 5. Checklist UI — Antes de Marcar Done

- [ ] Anchors en rango [0..1] en todos los RectTransforms
- [ ] Scroll: VLG `childControlWidth/Height=true`, `ContentSizeFitter`, pivot `(0.5,1)`, Viewport alpha 0.004
- [ ] Prefabs en scroll: tienen `LayoutElement.preferredHeight`
- [ ] Colores de la paleta de sección 4
- [ ] `capture_ui_canvas` verificado en Play Mode
- [ ] Playtest manual confirmado por el usuario
