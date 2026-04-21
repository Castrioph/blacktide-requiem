# Coplay + Unity Editor: Lecciones Aprendidas

> Derivado de la sesión 2026-04-21 (S3-06 + S3-07). Flujo completo verificado y
> confirmado por el usuario. Este doc recoge lo que funcionó y lo que NO, para
> no repetir los mismos errores.

---

## 1. Limitaciones de Coplay — Reglas Firmes

### 1.1 `capture_ui_canvas` solo funciona en Play Mode

**Por qué falla en Edit Mode:** la herramienta usa una cámara con render texture.
Los Canvas en modo `ScreenSpaceOverlay` no se renderizan vía cámara: el resultado
es una imagen negra o gris vacía.

**Regla:** nunca captures para verificar visuals en Edit Mode. Siempre:
1. `play_game`
2. Si la escena activa no es la correcta, usa un script `SimulateXxx.cs` para navegar
3. Captura

---

### 1.2 `execute_script` vive en un assembly separado del juego

El executor de Coplay compila el script en su propio assembly. Esto tiene tres
consecuencias:

**a) Singletons estáticos aparecen null**
```csharp
// ❌ Siempre null desde Coplay executor
GameFlowManager.Instance

// ✅ Funciona
Object.FindFirstObjectByType<GameFlowManager>()
```

**b) Reflection con `as T` falla para tipos del juego**
```csharp
// ❌ Devuelve null aunque el campo esté asignado en el juego
var cb = GetField<Action<StageData>>(entry, "_onSelected");

// ✅ Usa GetValue raw y comprueba nullidad
var f = entry.GetType().GetField("_onSelected", BindingFlags.NonPublic | BindingFlags.Instance);
var raw = f?.GetValue(entry);   // non-null si realmente está asignado
Debug.Log(raw?.GetType().FullName ?? "NULL");
```
El cast `as T` falla porque `StageData` en el executor context es un tipo distinto
al del assembly del juego aunque tengan el mismo nombre completo.

**c) `GetPersistentEventCount() == 0` es normal para listeners de runtime**
`AddListener()` crea listeners de runtime, no persistentes. `GetPersistentEventCount`
solo cuenta los asignados en el Inspector. Un count de 0 NO significa que el botón
no tenga listeners.

**Regla diagnóstica:** si sospechas que `Initialize()` no se llamó, añade un
`Debug.Log` temporal directamente en el método del juego. Es más fiable que
reflection cross-assembly.

---

### 1.3 `EditorSceneManager.OpenScene` no funciona en Play Mode

Unity lanza: *"This cannot be used during play mode"*.

**Regla:** los scripts que abren escenas con `EditorSceneManager` deben ejecutarse
SIEMPRE con el juego parado. Secuencia correcta:

```
stop_game → execute_script (editor) → open_scene (si hace falta) → play_game
```

---

### 1.4 ScriptableObjects no se encuentran con `FindFirstObjectByType`

`FindObjectsByType<T>` / `FindFirstObjectByType<T>` solo encuentra MonoBehaviours
activos en escena. Los ScriptableObjects (StageData, CharacterData, StageRegistry…)
no están en escena.

**Regla:** carga SOs desde Coplay scripts con:
```csharp
var stage = AssetDatabase.LoadAssetAtPath<StageData>("Assets/Data/Stages/stage_001_bahia_corsaria.asset");
```

---

## 2. Unity UI — Problemas Comunes y Soluciones

### 2.1 Anchors corruptos (valor > 1.0)

Los scripts de creación automática de escenas a veces generan anchorMin/Max con
valores como `(5.0, 5.0)`. Un elemento con anchorMin > 1 queda 5× el ancho del
canvas fuera de pantalla → pantalla negra o vacía.

**Diagnóstico:**
```csharp
// Detectar en editor script
bool corrupt = rt.anchorMin.x > 1 || rt.anchorMax.x > 1
            || rt.anchorMin.y > 1 || rt.anchorMax.y > 1;
```

**Fix:** resetear a valores correctos con un editor script explícito por elemento
(no resetear todo a 0.5/0.5 porque eso da tamaño cero).

---

### 2.2 VerticalLayoutGroup: childControlHeight debe ser true

Por defecto los VLG se crean con `childControlWidth=false, childControlHeight=false`.
En ese modo el VLG NO lee `LayoutElement.preferredHeight` → los hijos quedan con
sizeDelta.y = 0 → invisibles.

**Configuración correcta para scroll lists:**
```csharp
vlg.childControlWidth      = true;
vlg.childControlHeight     = true;
vlg.childForceExpandWidth  = true;
vlg.childForceExpandHeight = false;  // usar preferredHeight, no expandir
vlg.spacing                = 4f;
```

Y añadir `ContentSizeFitter` al Content para que crezca con los hijos:
```csharp
csf.verticalFit   = ContentSizeFitter.FitMode.PreferredSize;
csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
```

---

### 2.3 Content pivot para scroll views

Con `childControlHeight=true` en el VLG y un `ContentSizeFitter`, el Content
crece hacia abajo desde su pivot. Si el pivot es `(0.5, 0.5)` (defecto), la
mitad del contenido queda por encima del viewport → hueco vacío al inicio del scroll.

**Fix:** pivot = `(0.5, 1.0)` para que el Content crezca hacia abajo desde su
borde superior.

```csharp
contentRT.pivot = new Vector2(0.5f, 1f);
contentRT.anchoredPosition = Vector2.zero;
```

---

### 2.4 Viewport Image en ScrollRect

El Viewport necesita un componente `Image` para que `Mask` funcione. Ese Image se
renderiza como un rectángulo blanco sólido encima del contenido si no se controla.

**Fix:** dejar el Image visible pero casi transparente (el Mask sigue funcionando):
```csharp
viewportImage.color = new Color(1f, 1f, 1f, 0.004f);
```

---

### 2.5 Prefabs: usar `PrefabUtility.EditPrefabContentsScope`

Para editar prefabs desde editor scripts (añadir componentes, cambiar colores):

```csharp
using (var scope = new PrefabUtility.EditPrefabContentsScope("Assets/Prefabs/UI/MyPrefab.prefab"))
{
    var root = scope.prefabContentsRoot;
    // modificar root y sus hijos
    EditorUtility.SetDirty(root);
}
// prefab se guarda automáticamente al salir del scope
```

NO usar `AssetDatabase.LoadAssetAtPath` + modificar directamente: los cambios no
se serializan correctamente en el archivo `.prefab`.

---

### 2.6 LayoutElement en prefabs de scroll lists

Cada prefab que se instancie dentro de un VLG con `childControlHeight=true` DEBE
tener un `LayoutElement` con `preferredHeight` asignado. Sin él, el VLG le da
altura 0.

```csharp
var le = root.GetComponent<LayoutElement>() ?? root.AddComponent<LayoutElement>();
le.preferredHeight = 90f;  // o lo que corresponda al diseño
```

---

## 3. Flujo de Verificación Visual Recomendado

Para verificar una pantalla UI con Coplay:

```
1. stop_game (si está en play)
2. open_scene("Assets/MainMenu.unity")   ← siempre arrancar desde MainMenu
3. play_game
4. execute_script(SimulateClick.cs)      ← navegar a la escena objetivo
   - usa FindFirstObjectByType, no .Instance
   - usa AssetDatabase para SOs
5. capture_ui_canvas                     ← captura en play mode
6. verificar visualmente
7. stop_game si necesitas corregir algo en el editor
```

**Scripts de simulación disponibles** en `Assets/Editor/`:
- `SimulateClick.cs` — navega MainMenu → StageSelect
- `SimulateLaunch.cs` — navega StageSelect → TeamSelect (con StageData real)
- `SimulateCombat.cs` — navega TeamSelect → Combat (con equipo completo)
- `SimulateResults.cs` — navega Combat → Results
- `SimulateReturnMenu.cs` — navega Results → MainMenu

---

## 4. Paleta Visual del Proyecto

Colores usados en todas las pantallas UI. Referencia para futuras escenas.

```csharp
static readonly Color BgDark    = new Color(0.08f, 0.06f, 0.14f, 1f);  // fondo principal
static readonly Color PanelDark = new Color(0.12f, 0.09f, 0.18f, 1f);  // paneles/cards
static readonly Color Gold      = new Color(0.95f, 0.78f, 0.25f, 1f);  // títulos, estrellas
static readonly Color Cream     = new Color(0.92f, 0.88f, 0.72f, 1f);  // texto secundario
static readonly Color BtnActive = new Color(0.60f, 0.42f, 0.10f, 1f);  // botones habilitados
static readonly Color BtnOff    = new Color(0.24f, 0.19f, 0.13f, 1f);  // botones deshabilitados
// Selection states (gestionados por script, no hardcodeados en prefab):
static readonly Color BorderUnselected = new Color(0.361f, 0.239f, 0.118f);
static readonly Color BorderSelected   = new Color(0.831f, 0.627f, 0.090f);
```

---

## 5. Checklist para Nuevas Escenas UI

Antes de marcar una tarea UI como Done:

- [ ] Todos los RectTransforms tienen anchorMin/Max en rango [0..1]
- [ ] Scroll lists: VLG con `childControlWidth=true, childControlHeight=true`
- [ ] Scroll lists: `ContentSizeFitter` con `verticalFit=PreferredSize` en Content
- [ ] Scroll lists: Content pivot = `(0.5, 1)`
- [ ] Scroll lists: Viewport Image color alpha ≈ 0.004
- [ ] Prefabs con VLG: tienen `LayoutElement.preferredHeight` en el root
- [ ] Todos los colores usan la paleta de la sección 4
- [ ] Verificado con `capture_ui_canvas` en Play Mode (no Edit Mode)
- [ ] Navegación probada con scripts de simulación
- [ ] Playtest manual confirmado por el usuario
