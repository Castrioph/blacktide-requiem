# Team Composition

> **Status**: Approved
> **Author**: user + agents
> **Last Updated**: 2026-03-27
> **Implements Pillar**: Pillar 1 (Combate Táctico), Pillar 3 (Colección con Propósito)

## Overview

Team Composition es el sistema que permite al jugador organizar sus unidades para combate. Abarca dos contextos: **equipos terrestres** (5 unidades propias + 1 amigo, con un Capitán designado) y **tripulaciones navales** (unidades asignadas a los role slots de un barco). El jugador interactúa con este sistema antes de cada batalla, seleccionando qué unidades llevar, quién es el Capitán (tierra), y qué unidad ocupa cada rol del barco (naval). Es el puente entre la colección de unidades y el combate — sin este sistema, tener muchas unidades no tendría propósito táctico. Las decisiones de composición definen qué sinergias se activan, qué elementos cubre el equipo, y qué habilidades están disponibles en batalla.

## Player Fantasy

**Fantasía**: Eres un capitán pirata que arma equipos especializados para cada situación. Desde tu cuartel, preparas varias formaciones — una para stages de Pólvora, otra para jefes de Maldición, una tripulación naval para rutas peligrosas. Antes de zarpar, eliges el equipo adecuado para la misión.

**Emoción objetivo**: Satisfacción del puzzle resuelto. El jugador mira su roster, analiza qué contenido tiene por delante, y arma equipos que *encajan*. La satisfacción viene de tener el equipo correcto preparado, y de ajustarlo cuando consigue una nueva unidad que cambia todo.

**Referencia**: FFBE (party presets con nombre, selección rápida pre-batalla, amigo de otros jugadores).

**Tipo de sistema**: Activo en menú principal (crear/editar presets) + selección rápida pre-combate (elegir preset + amigo).

## Detailed Design

### Core Rules

**1. Tipos de Preset**

| Tipo | Slots | Capacidad | Notas |
|------|-------|-----------|-------|
| **Equipo Terrestre** | 5 propias + 1 amigo | 10 presets | Slot amigo se llena al entrar en batalla |
| **Tripulación Naval** | Variable por barco (5-7 demo) + 1 guest | 5 presets | Cada preset vinculado a un barco. Slots con roles |

**2. Equipo Terrestre — Reglas**

1. Cada preset tiene 5 slots para unidades propias (slots 1-5) y 1 slot de amigo (slot 6)
2. El jugador asigna unidades de su roster a los slots 1-5. No hay restricciones de rareza, elemento, o tipo
3. **No se permite duplicar la misma unidad** dentro del mismo preset (cada slot debe ser una unidad diferente)
4. Una misma unidad puede aparecer en múltiples presets sin restricción
5. Los slots pueden dejarse vacíos — un equipo con menos de 5 unidades es válido (aunque subóptimo)
6. **Capitán principal**: El jugador designa uno de los slots 1-5 como Capitán. Por defecto es el slot 1. El Capitán determina qué sinergias se activan (ver Traits/Sinergias GDD)
7. **Slot de amigo (slot 6)**: Se selecciona al entrar en batalla, de una lista de unidades de amigos. Puede repetir una unidad que ya esté en el equipo (es la copia de otro jugador). No puede ser designado como Capitán principal
8. **Amigo como segundo Capitán**: Si el slot 6 es una **unidad de amigo** (de otro jugador), funciona como un segundo Capitán para sinergias — sus traits también se evalúan para activación, acumulándose con las del Capitán principal. Si el slot 6 es una **unidad propia** (porque no hay amigos disponibles), NO cuenta como segundo Capitán
9. El preset tiene un **nombre editable** (por defecto: "Equipo 1", "Equipo 2", etc.)
10. **Orden de slots**: El orden de los slots 1-5 no afecta mecánicas de combate. Solo afecta el tie-breaking de la Initiative Bar (slot menor actúa antes en empate)

**3. Tripulación Naval — Reglas**

1. Cada preset naval está **vinculado a un barco** del inventario del jugador
2. Los slots disponibles los define el barco (RoleSlot[] del ShipData). Cada slot tiene un NavalRole fijo
3. El jugador asigna unidades de su roster a los role slots
4. **No se permite duplicar la misma unidad** dentro de una misma tripulación
5. Una misma unidad puede estar en presets terrestres Y navales sin restricción
6. **Rol matching**: Si NavalRoleAffinity de la unidad coincide con el rol del slot → bonus completo. Si no coincide → penalización (ver SDM Formulas). Cualquier unidad puede ocupar cualquier slot
7. **El Capitán naval es automático**: la unidad en el slot con Role = Capitán es el Capitán
8. **Guest slot**: Un slot por barco puede ser guest (IsGuestSlot = true). Se llena al entrar en batalla con unidad de amigo. Funciona como segundo Capitán naval bajo las mismas reglas que el terrestre (amigo = segundo Capitán, unidad propia = no)
9. Los slots pueden dejarse vacíos (contribuyen 0 stats, 0 habilidades, barco sigue desplegable)
10. El preset tiene un **nombre editable** (por defecto: nombre del barco)

**4. Selección Pre-Batalla**

1. El sistema detecta el tipo de combate del stage (terrestre o naval)
2. Se muestra la lista de presets del tipo correspondiente
3. El jugador selecciona un preset o crea/edita uno rápidamente
4. Selecciona una unidad amigo/guest de la lista de amigos (o una unidad propia si no hay amigos)
5. Se muestra el preview de sinergias activas (contando ambos Capitanes si aplica)
6. Confirma y entra en combate

**5. Restricciones**

- No hay restricciones de elemento, rareza, o tipo para ningún slot
- La única restricción es **no duplicar unidades dentro del mismo preset** (excepción: la unidad amiga sí puede repetir una unidad del equipo, ya que es la copia de otro jugador)
- Presets incompletos (slots vacíos) son válidos pero se muestra advertencia visual
- **Protección de venta**: Una unidad que esté en cualquier preset (terrestre o naval) **no puede venderse**. Al intentar vender, el sistema muestra un aviso indicando en qué presets está asignada. El jugador debe quitarla de todos los presets antes de poder venderla

### States and Transitions

**Estados del Preset**

| Estado | Descripción | Transiciones |
|--------|-------------|-------------|
| `Empty` | Preset recién creado, sin unidades asignadas | → `Partial` (al asignar primera unidad) |
| `Partial` | Tiene al menos 1 unidad pero no todos los slots llenos | → `Complete` (todos los slots propios llenos), → `Empty` (todas las unidades removidas) |
| `Complete` | Todos los slots propios llenos (5/5 terrestre, todos roles llenos naval) | → `Partial` (una unidad removida o vendida) |
| `Ready` | Complete + amigo/guest seleccionado (solo en contexto pre-batalla) | → (entra en combate) |

**Flujo del Sistema**

```
Menú Principal                      Pre-Batalla
     │                                   │
     ├── Crear preset ──► Empty           ├── Seleccionar preset
     ├── Editar preset                    ├── Editar rápido (opcional)
     ├── Renombrar preset                 ├── Seleccionar amigo/guest
     ├── Duplicar preset                  ├── Preview sinergias
     └── Eliminar preset                  └── Confirmar ──► Combate
```

### Interactions with Other Systems

| Sistema | Dirección | Datos que fluyen | Interfaz |
|---------|-----------|-----------------|----------|
| **Unit Data Model** | UDM → TC | Lista de unidades del roster del jugador, CharacterData (stats, elemento, traits, NavalRoleAffinity) | TC lee el roster para poblar la UI de selección. Muestra stats y traits de cada unidad para ayudar al jugador a decidir |
| **Ship Data Model** | SDM → TC | Lista de barcos del jugador, ShipData (RoleSlots, stats base) | TC lee los barcos y sus role slots para construir presets navales. Los slots disponibles y sus roles vienen del SDM |
| **Traits/Sinergias** | TC → TS | Equipo completo (unidades + Capitán + amigo) para evaluación de sinergias | TC envía la composición; TS calcula qué sinergias se activan. TC muestra el preview en la UI pre-batalla. **Nota**: El amigo de otro jugador cuenta como segundo Capitán para TS |
| **Combate Terrestre** | TC → CT | Equipo terrestre final (5 unidades + amigo, Capitán designado, flag de segundo Capitán) | CT recibe el equipo como input en PreCombat. TC es responsable de validar el equipo antes de enviarlo |
| **Combate Naval** | TC → CN | Tripulación naval final (barco + unidades asignadas a roles + guest) | CN recibe la tripulación como input. Los role matchings y stat contributions se calculan a partir de la asignación de TC |
| **Save/Load System** | TC ↔ S/L | Presets guardados (terrestres y navales) | Los presets se persisten en el save file. Al cargar, TC reconstruye los presets y valida que las unidades/barcos aún existen |
| **Unit Roster/Inventory** | URI → TC | Roster de unidades disponibles, estado de cada unidad | TC consulta el roster para saber qué unidades tiene el jugador. **Protección de venta**: URI consulta TC antes de permitir venta — si la unidad está en algún preset, bloquea la venta |
| **Menus & Navigation UI** | TC → MUI | Datos de presets para renderizar la UI de gestión de equipos | MUI renderiza la pantalla de equipos del menú principal. TC proporciona los datos; MUI los presenta |
| **Stage System** | SS → TC | Tipo de combate del stage (terrestre/naval) para filtrar presets | TC usa esta info para mostrar solo presets del tipo relevante en la selección pre-batalla |

## Formulas

**Preview de Sinergias (cálculo pre-batalla)**

El preview evalúa qué sinergias se activarían sin entrar en combate:

```
Para cada trait T del Capitán principal:
  count = número de unidades en el equipo (incluido Capitán) que tienen T
  Si amigo es de otro jugador (segundo Capitán) Y amigo tiene T:
    count += 1 (el amigo cuenta para el threshold si comparte el trait)
  Si count ≥ SYNERGY_THRESHOLD (3):
    Sinergia T activa → mostrar en preview

Para cada trait T del segundo Capitán (amigo de otro jugador):
  Si T no fue ya evaluado por el Capitán principal:
    count = número de unidades en el equipo que tienen T (incluido el amigo)
    Si count ≥ SYNERGY_THRESHOLD (3):
      Sinergia T activa → mostrar en preview
```

| Variable | Definición | Valor |
|----------|-----------|-------|
| `SYNERGY_THRESHOLD` | Mínimo de unidades compartiendo trait para activar sinergia | 3 (de Traits/Sinergias GDD) |

**Nota**: Este preview es informativo. El cálculo real lo hace el sistema de Traits/Sinergias al entrar en combate. El preview debe usar la misma lógica para ser consistente.

**Stat Contribution Naval**: Las fórmulas de cómo las unidades contribuyen stats al barco están definidas en el Ship Data Model GDD (ver SDM Formulas). TC no recalcula — delega al SDM.

## Edge Cases

1. **Jugador no tiene suficientes unidades para llenar un equipo terrestre**: Presets incompletos son válidos. El jugador puede entrar en combate con 1-4 unidades. Se muestra advertencia visual pero no se bloquea.

2. **Jugador no tiene amigos disponibles en la lista**: Se permite usar una unidad propia en el slot 6 como reemplazo. Esta unidad propia NO cuenta como segundo Capitán ni puede repetir una unidad del equipo. Si el jugador no tiene unidades propias adicionales fuera del equipo, el slot 6 queda vacío.

3. **Amigo tiene la misma unidad que el Capitán (ej: dos Barbosa)**: Permitido. La unidad amiga es una copia independiente. Ambos Barbosa funcionan como Capitán: el principal y el segundo. Si ambos comparten un trait, la sinergia se **activa dos veces** (doble bono). Cada Barbosa cuenta para el threshold de ambas evaluaciones.

4. **Barco en preset no puede venderse**: Igual que las unidades, un barco asignado a un preset naval **no puede venderse**. Se muestra aviso indicando en qué presets está. El jugador debe quitarlo de todos los presets antes de vender.

5. **Unidad asignada a rol naval sin NavalRoleAffinity matching**: Permitido con penalización de stats (ver SDM Formulas). Se muestra indicador visual de mismatch.

6. **Jugador intenta vender una unidad/barco en presets**: La venta se bloquea. El aviso lista los presets por nombre. El jugador debe remover la unidad/barco de todos los presets antes de vender.

7. **Preset con una sola unidad que es el Capitán**: Válido para terrestre. Las sinergias no se activarán (count < threshold), pero el equipo funciona.

8. **Slot Capitán del barco vacío**: El jugador **no puede entrar en combate naval**. Se muestra error: "Asigna un Capitán al barco para poder zarpar". El amigo/guest nunca puede ocupar el slot de Capitán naval ni sustituirlo como Capitán principal.

9. **Dos Capitanes (principal + amigo) comparten el mismo trait**: La sinergia se evalúa **dos veces**, una por cada Capitán. Ambas activaciones generan bonos independientes que se acumulan. Ejemplo: Capitán y amigo ambos tienen "Hijos del Mar" → la sinergia "Hijos del Mar" se activa dos veces → las unidades con ese trait reciben doble SynergyBonus.

10. **Jugador edita un preset en pantalla de selección pre-batalla**: Los cambios se guardan inmediatamente. El preview de sinergias se actualiza en tiempo real.

11. **Preset duplicado**: Se crea copia independiente con nombre "[original] (copia)". Cambios en uno no afectan al otro.

12. **Amigo de otro jugador en guest slot naval que tiene Role = Capitán en su RoleAffinity**: El amigo ocupa el guest slot, no el slot Capitán. Su NavalRoleAffinity no importa para la mecánica de Capitán — sigue siendo segundo Capitán de sinergias, no Capitán principal del barco.

## Dependencies

### Dependencias Upstream (TC depende de)

| Sistema | Tipo | Interfaz | GDD |
|---------|------|----------|-----|
| **Unit Data Model** | Hard | Roster de unidades, CharacterData (stats, traits, NavalRoleAffinity, elemento) | ✅ Approved |
| **Ship Data Model** | Hard | Barcos del jugador, ShipData (RoleSlots, stats base) | ✅ Approved |

### Dependencias Downstream (dependen de TC)

| Sistema | Tipo | Qué necesita de TC | GDD |
|---------|------|---------------------|-----|
| **Combate Terrestre** | Hard | Equipo final: 5 unidades + amigo, Capitán designado, flag segundo Capitán | ✅ Approved |
| **Combate Naval** | Hard | Tripulación final: barco + unidades en roles + guest, Capitán naval + flag segundo Capitán | ⬜ Not Started |
| **Menus & Navigation UI** | Hard | Datos de presets para la pantalla de gestión de equipos | ⬜ Not Started |
| **Save/Load System** | Hard | Presets para persistencia | ⬜ Not Started |
| **Unit Roster/Inventory** | Soft | Consulta de protección de venta (¿está la unidad/barco en algún preset?) | ⬜ Not Started |

### Cross-System Updates Necesarios

- **Traits/Sinergias GDD**: Actualizar para documentar la mecánica de segundo Capitán (amigo) y la doble activación cuando ambos Capitanes comparten trait
- **Combate Terrestre GDD**: Actualizar para documentar que el amigo puede funcionar como segundo Capitán

## Tuning Knobs

| Knob | Valor Actual | Rango Seguro | Afecta a | Notas |
|------|-------------|-------------|----------|-------|
| `MAX_LAND_PRESETS` | 10 | 5–20 | Cantidad de equipos terrestres guardados. Muy pocos frustra a jugadores organizados. Demasiados satura la UI | Ampliable con progresión futura si se desea |
| `MAX_NAVAL_PRESETS` | 5 | 3–10 | Cantidad de tripulaciones navales guardadas. Menos que terrestres porque hay menos barcos | Escala con cantidad de barcos disponibles |
| `LAND_TEAM_SIZE` | 5 + 1 amigo | 4–6 + amigo | Tamaño del equipo terrestre. Cambiar afecta balance de sinergias (threshold de 3), Initiative Bar, y dificultad general | Definido en múltiples GDDs — cambio requiere propagación |
| `MIN_UNITS_TO_ENTER_COMBAT` | 1 (terrestre), Capitán obligatorio (naval) | — | Mínimo para entrar en combate. Terrestre es flexible; naval requiere Capitán | Naval no puede ser 0 por diseño (el barco necesita capitán) |
| `PRESET_NAME_MAX_LENGTH` | 20 caracteres | 10–30 | Límite del nombre editable del preset | Suficiente para nombres descriptivos sin desbordar la UI |

## Visual/Audio Requirements

**Visual**
- Pantalla de gestión de equipos accesible desde el menú principal
- Vista de slots con retrato de unidad, nombre, elemento (icono), nivel, y rareza (estrellas)
- Indicador visual de Capitán: **sombrero pirata** sobre el slot designado
- Indicador de sinergias activas en tiempo real (al añadir/quitar unidades, las sinergias se recalculan y se muestran)
- Para presets navales: vista del barco con role slots alrededor, cada slot muestra el rol requerido y la unidad asignada
- Indicador de role matching: verde (match), naranja (mismatch) en cada slot naval
- Pantalla pre-batalla: lista compacta de presets con nombre y vista rápida de unidades
- Aviso visual para presets incompletos (borde rojo/amarillo, texto de advertencia)
- Aviso de protección de venta: popup con lista de presets afectados

**Audio**
- SFX de asignación de unidad a slot (satisfactorio, tipo "encajar pieza")
- SFX de sinergia activada (al completar threshold, feedback positivo)
- SFX de error (intentar duplicar unidad, intentar vender unidad protegida)
- SFX de cambio de Capitán

## UI Requirements

- **Pantalla de gestión (menú principal)**: Lista de presets con tabs terrestres/navales. Cada preset muestra nombre + miniatura de las unidades asignadas. Botones: Crear, Editar, Duplicar, Renombrar, Eliminar. Los presets son **reordenables** (drag o flechas)
- **Editor de preset terrestre**: 5 slots en fila + slot 6 (amigo, atenuado/grisado porque se llena en pre-batalla). Tap en slot vacío abre selector de roster. Tap en slot lleno permite reemplazar o vaciar. Botón de sombrero pirata para designar Capitán. Panel lateral con preview de sinergias activas y stats totales del equipo
- **Editor de preset naval**: Vista del barco con slots distribuidos por rol. Cada slot muestra: rol requerido (icono + nombre), unidad asignada (retrato), indicador de match. Guest slot marcado diferente. Panel lateral con stats del barco (base + contribuciones de crew)
- **Selector de roster**: Lista filtrable/ordenable de unidades disponibles. Filtros: elemento, rareza, trait, NavalRoleAffinity (naval). Ordenar por: nivel, stat específico, rareza. Las unidades ya asignadas al preset actual se marcan (no seleccionables)
- **Auto-fill simple**: Botón que sugiere unidades para los slots vacíos. Prioriza: matching de rol naval, traits que activen sinergias con el Capitán, cobertura elemental. Es una sugerencia rápida, no un optimizador — la composición del equipo debe ser parte del disfrute del jugador
- **Pantalla pre-batalla**: Preset seleccionado con vista de unidades + selector de amigo. Preview de sinergias contando amigo. Botón de edición rápida. Botón de confirmar
- **Selector de amigo**: Lista de unidades de amigos disponibles. Muestra: nombre, nivel, elemento, traits. Indica si el amigo activaría sinergias adicionales como segundo Capitán

## Acceptance Criteria

**Presets Terrestres**
1. El jugador puede crear, editar, renombrar, duplicar y eliminar hasta 10 presets terrestres
2. Cada preset permite asignar unidades a 5 slots sin restricciones de rareza/elemento
3. No se puede asignar la misma unidad a dos slots dentro del mismo preset
4. Una misma unidad puede estar en múltiples presets
5. El jugador puede designar cualquier slot 1-5 como Capitán (por defecto slot 1)
6. Presets incompletos son válidos y permiten entrar en combate con advertencia visual

**Presets Navales**
7. El jugador puede crear hasta 5 presets navales, cada uno vinculado a un barco
8. Los slots muestran el NavalRole requerido y el indicador de matching/mismatch
9. El slot Capitán del barco debe estar ocupado para poder entrar en combate naval
10. El guest slot acepta amigos o unidades propias

**Amigo / Segundo Capitán**
11. Al entrar en combate, el jugador puede seleccionar una unidad amiga de la lista
12. La unidad amiga puede repetir una unidad del equipo (es copia de otro jugador)
13. Si la unidad del slot 6/guest es de un amigo, funciona como segundo Capitán para sinergias
14. Si es unidad propia, NO funciona como segundo Capitán
15. Cuando ambos Capitanes comparten un trait, la sinergia se activa dos veces con bonos acumulados
16. El preview pre-batalla muestra correctamente las sinergias contando ambos Capitanes

**Protección de Venta**
17. Una unidad en cualquier preset no puede venderse; se muestra aviso con lista de presets afectados
18. Un barco en cualquier preset no puede venderse; misma protección que las unidades
19. Tras quitar la unidad/barco de todos los presets, la venta se desbloquea

**Selección Pre-Batalla**
20. El sistema filtra presets por tipo de combate del stage (terrestre/naval)
21. El jugador puede editar rápidamente un preset desde la pantalla pre-batalla
22. Los cambios en pre-batalla se guardan inmediatamente al preset

**Persistencia**
23. Los presets se guardan correctamente al save file y se restauran al cargar

## Open Questions

1. **Sistema de amigos**: La lista de amigos y unidades compartidas se definirá en un futuro Social System GDD. TC asume que existe esta funcionalidad. *Owner: Game Designer*

2. **Stages mixtos tierra + naval**: Idea para el futuro — stages que combinen ambos tipos de combate, donde el mismo pool de unidades se repartiría entre equipo terrestre y tripulación naval. No es necesario para la demo. Si se implementa, TC necesitaría validación de no-conflicto entre ambos presets. *Owner: Game Designer*
