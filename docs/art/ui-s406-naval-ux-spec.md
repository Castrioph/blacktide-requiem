# S4-06 UX Spec — Pantalla de Combate Naval
> Author: UX Designer agent
> Date: 2026-06-12 (actualizado 2026-06-12 — decisiones usuario aplicadas)
> Sprint: S4-06 (spec-only — NO code)
> Reference resolution: 1080×1920 portrait (same CanvasScaler as S3-11)
> Input scope: Mouse + keyboard primary. Gamepad: viability notes only.
> Pattern: spec-first following S3-11 audit method
>
> DECISIONES RESUELTAS (usuario, 2026-06-12):
> - D1: Abordaje → Targeting directo en sprite via marcadores/chips overlay (no panel modal)
> - D2: LB → Barra binaria cosmética (llena/vacía), consistente con combate terrestre
> - D3: Inspección crew enemiga → Incluida en S4-06; reutiliza los mismos marcadores del Abordaje

---

## Nota Preliminar: Divergencias GDD vs Código

El GDD (combate-naval.md §3) documenta **5 acciones** ("Cañonazo, Habilidad Naval,
Maniobra Evasiva, Abordaje, Reparar") más "Pasar Turno" como "+1". El código
`NavalTurnResolver.cs` lo trata igual: `ActionType.Pass` es una sexta case en el switch.
Conclusión: **son 6 acciones totales** (las 5 + Pasar). El spec usa "6 acciones" en
todo momento.

El GDD §3 dice que Abordaje es gratis. El código confirma: ningún `ConsumeMP` en la
rama `ActionType.Boarding`. Consistente.

El GDD dice "Reparar sí disponible bajo Silencio (Edge Case 14)." El código confirma:
`IsAbilityReady()` en `ShipCombatant` aplica el check de Silencio solo a habilidades
(`AbilityData`), y `ResolveRepair()` no pasa por `IsAbilityReady()`. Consistente.

`ShipCombatant.MaxHHP` es fijo en construcción — crew deaths no reducen el MaxHHP,
solo las contribuciones futuras. La barra de HHP enemiga tampoco colapsa su máximo
cuando muere crew. Este detalle afecta el diseño de la barra (usar CurrentHHP/MaxHHP
fijos, no una barra que se encoja).

---

## 1. Flujo de Interacción — Diagrama de Estados

```
┌─────────────────────────────────────────────────────────────────────┐
│                         ESTADOS UI NAVAL                            │
└─────────────────────────────────────────────────────────────────────┘

  [Idle / EnemyTurn]
       │
       │  GameEvents.OnTurnStart(ShipCombatant = barco aliado)
       │  → PlayerNavalInput.OnInputRequested
       ▼
  [ActionSelect]  ◄─────────────────────────────────────────────────────┐
       │                                                                  │
       ├─ Cañonazo ──────────────────────────────────────────────────────►│  [TargetShip]
       │                                                                   │       │
       ├─ Habilidad Naval ──────► [AbilitySelect]                         │       │ Click barco vivo
       │                               │                                   │       │ → SubmitAttack(target)
       │                               │ Click habilidad                   │       │
       │                               │ TargetType = AoeEnemy/Self        │       │ Escape / click fondo
       │                               │   → SubmitAbility(ability, null) ─┼───────┼─► [ActionSelect]
       │                               │ TargetType = SingleEnemy ─────────►│       │
       │                               │ TargetType = SingleCrewEnemy ──────────────►│  [TargetCrewForAbility]
       │                               │                                   │       │       │ Click crew
       │                               │ Escape / Btn Volver ──────────────┼───────┘       │ → SubmitAbility(ab, crew)
       │                               │   → [ActionSelect]                │               │ Escape → [ActionSelect]
       │                               ▼                                   │
       ├─ Maniobra Evasiva ──────────────────────────────────────────────────────────────────────────────────►
       │                               → SubmitManeuver()                  │
       │                               → [Idle / EnemyTurn]                │
       │                                                                    │
       ├─ Abordaje ──────────────────────────────────────────────────────────►│  [TargetCrew]
       │                (solo si hay barco enemigo con crew viva)          │       │
       │                                                                   │       │ Si >1 barco abordable:
       │                                                                   │       │   borde dorado en barcos abordables
       │                                                                   │       │   click barco → expande marcadores
       │                                                                   │       │ Si 1 barco abordable:
       │                                                                   │       │   marcadores aparecen directamente
       │                                                                   │       │
       │                                                                   │       │ Marcadores crew (chips overlay):
       │                                                                   │       │   hover chip → tooltip rol/HP
       │                                                                   │       │   click chip vivo → SubmitBoarding
       │                                                                   │       │   → [Idle / EnemyTurn]
       │                                                                   │       │ Flechas: ciclan chips vivos
       │                                                                   │       │ Enter: confirma chip seleccionado
       │                                                                   │       │ Escape → [ActionSelect]
       │                                                                   │
       ├─ Reparar ───────────────────────────────────────────────────────────────────────────────────────────►
       │                (si CurrentMP >= 20; disponible bajo Silencio)     │
       │                → SubmitRepair()                                    │
       │                → [Idle / EnemyTurn]                               │
       │                                                                    │
       └─ Pasar Turno ──────────────────────────────────────────────────────────────────────────────────────►
                        → SubmitPass()
                        → [Idle / EnemyTurn]


  [Idle / EnemyTurn]
       │  GameEvents.OnWaveComplete → WaveTransition overlay
       │  GameEvents.OnBattleEnd(Victory/Defeat)
       ▼
  [BattleOver]  → ResultOverlay (no bloquea la IB visual)
```

### Reglas de Cancelación

- **Escape** (teclado) o **click derecho** en cualquier estado de targeting o
  submenú: retrocede al estado anterior (TargetShip/TargetCrew → ActionSelect;
  AbilitySelect → ActionSelect).
- **No** hay cancelación desde ActionSelect hacia atrás — el jugador no puede salir
  del combate desde el panel de acciones.
- La cancelación no consume turno ni MP.

---

## 2. Wireframes ASCII

> Resolución de referencia: 1080 × 1920 px portrait.
> Paleta: colores canónicos de coplay-unity-lessons.md §4.
> Nota: el estilo visual (fondos, texturas, sprites de barcos) es competencia del
> art-director. Este wireframe define layout, tamaños mínimos y jerarquía de
> información — no color final ni arte.

---

### 2.1 Pantalla Completa — Estado ActionSelect (turno aliado)

```
┌──────────────────────────────────────────────┐  1080px
│  INITIATIVE BAR  (altura 56px)               │
│  [SHIP_ALIADO▲][ENE_A][ENE_B][ENE_A]         │  iconos barco 44×44, círculo
│  ← turno activo marcado con borde dorado     │
├──────────────────────────────────────────────┤
│  WAVE LABEL   "OLEADA 2/3"   (altura 28px)   │
│  Centrado, cream 14sp. Fase jefe si aplica   │
├──────────────────────────────────────────────┤
│                                              │
│   CAMPO DE BATALLA  (~620px de alto)         │
│                                              │
│  ┌────────────────┐   ┌────────────────┐     │
│  │  BARCO ALIADO  │   │ BARCO ENEMIGO A│     │
│  │  [sprite]      │   │  [sprite]      │     │
│  │  Escudo azul   │   │  Borde rojo    │     │
│  │  si Maniobra   │   │  si Maniobra   │     │
│  │                │   │                │     │
│  │  DoT icons:    │   │  DoT icons:    │     │
│  │  [Q][V][S]     │   │  [Q][V][S]     │     │
│  └────────────────┘   └────────────────┘     │
│                            ┌────────────┐    │
│                            │ CRIATURA B │    │
│                            │  [sprite]  │    │
│                            └────────────┘    │
│                                              │
├──────────────────────────────────────────────┤
│  STATS PANEL BARCO ALIADO  (altura 100px)    │
│  ┌──────────────────────────────────────┐    │
│  │ "La Perdición"  ELE: [icono]         │    │
│  │ HHP [████████████░░] 2800/3200       │    │  barra verde→amarillo→rojo
│  │  MP [████████░░░░░░]  80/150         │    │  barra azul
│  │  LB [████████████████]               │    │  barra binaria: llena si LB ready, vacía si no (ver D2)
│  │ Stats: FPW 193 HDF 150 SPD 88        │    │  flash rojo en stat que baja
│  └──────────────────────────────────────┘    │
├──────────────────────────────────────────────┤
│  CREW PANEL ALIADO  (altura 160px, scroll)   │
│  ┌─────────────────────────────────────┐     │
│  │[CAP][INT][ART][NAV][CAR][CIR][CON]  │     │  7 slots horizontales, 80px c/u
│  │ Vivo/Muerto     minibar HP           │     │  ver §2.5
│  │ Sinergias: [SINERGIA 1][SINERGIA 2]  │     │  badges activos
│  └─────────────────────────────────────┘     │
├──────────────────────────────────────────────┤
│  ACTION PANEL  (altura 280px)                │
│  ┌──────────────────────────────────────┐    │
│  │ ┌────────────┐  ┌────────────┐       │    │
│  │ │  CAÑONAZO  │  │ HAB.NAVAL  │       │    │  btns 240×80px (2 columnas)
│  │ │  [icono]   │  │  [icono]   │       │    │
│  │ └────────────┘  └────────────┘       │    │
│  │ ┌────────────┐  ┌────────────┐       │    │
│  │ │ MANIOBRA   │  │  ABORDAJE  │       │    │  ABORDAJE puede estar disabled
│  │ │  [icono]   │  │  [icono]   │       │    │
│  │ └────────────┘  └────────────┘       │    │
│  │ ┌────────────┐  ┌────────────┐       │    │
│  │ │  REPARAR   │  │   PASAR   │       │    │
│  │ │  [icono]   │  │  [icono]  │       │    │
│  │ └────────────┘  └────────────┘       │    │
│  └──────────────────────────────────────┘    │
├──────────────────────────────────────────────┤
│  BATTLE LOG  (altura 120px, scroll)          │
│  ← 50 entradas max, auto-scroll             │
└──────────────────────────────────────────────┘
```

**Notas de layout:**
- Barco aliado: izquierda. Enemigos: derecha. Consistente con combate terrestre.
- Múltiples enemigos simultáneos: apilados verticalmente en la mitad derecha.
  Con 3 enemigos (máx esperado en demo): cada sprite ~180px de alto.
- DoT icons: iconos pequeños (24×24) encima del sprite del barco. [Q]=Quemadura
  (llama), [V]=Veneno (calavera), [S]=Sangrado (gota roja). Siempre acompañados de
  un número de turnos restantes. NO dependen de color solo — cada uno tiene icono
  distinto (criterio accesibilidad §6).

---

### 2.2 Submenú: Selector de Habilidades Navales

Reemplaza el Action Panel cuando el jugador pulsa "Habilidad Naval".

```
┌──────────────────────────────────────────────┐
│  [Stats Panel — visible, no interactuable]   │
│  [Crew Panel — visible, no interactuable]    │
├──────────────────────────────────────────────┤
│  HABILIDADES NAVALES  (mismo espacio que AP) │
│  ┌──────────────────────────────────────┐    │
│  │ ScrollView (VLG + ContentSizeFitter) │    │
│  │ ┌────────────────────────────────┐   │    │
│  │ │ [LB] Cañón de Tormenta         │   │    │  card 900×60px
│  │ │      Elem: Agua  [40 MP]  AoE  │   │    │  LB badge si CanLimitBreak
│  │ │      Daño a todos los enemigos │   │    │  disabled + razon si sin MP
│  │ └────────────────────────────────┘   │    │  o cooldown activo
│  │ ┌────────────────────────────────┐   │    │
│  │ │      Oleada de Fuego           │   │    │
│  │ │      Elem: Fuego [25 MP] Single│   │    │
│  │ │      Daño a 1 enemigo          │   │    │
│  │ └────────────────────────────────┘   │    │
│  │ ┌────────────────────────────────┐   │    │
│  │ │ [sin MP] Bálsamo del Mar       │   │    │  gris, no interactuable
│  │ │      Heal  [60 MP]             │   │    │  razón en tooltip/label
│  │ └────────────────────────────────┘   │    │
│  │   ... más habilidades                │    │
│  └──────────────────────────────────────┘    │
│  ┌─────────────────┐                         │
│  │   ← VOLVER      │  44px de alto           │
│  └─────────────────┘                         │
└──────────────────────────────────────────────┘
```

**Campos mostrados por habilidad:**
- Nombre, Elemento (icono), Costo MP, TargetType (etiqueta textual: "Único", "Todos",
  "Crew enemiga", "Aliado", "Propio").
- Badge [LB] si `AbilityEntry.CanLimitBreak == true` (dorado).
- Si `ship.GetCooldownRemaining(ability) > 0`: badge "[CD: X]", button disabled.
- Si `ship.CurrentMP < ability.MPCost`: badge "[sin MP]", button disabled.
- Si `ship.HasStatus(StatusEffect.Silencio)`: toda la lista disabled con banner
  "SILENCIO — Habilidades bloqueadas" en la parte superior. Reparar NO aparece
  aquí (es acción base, no habilidad).

---

### 2.3 Estado de Targeting: Barco Enemigo (Cañonazo / habilidad SingleEnemy)

El Action Panel se oculta. El campo de batalla recibe la interacción.

```
┌──────────────────────────────────────────────┐
│  [IB] [WaveLabel]                            │
├──────────────────────────────────────────────┤
│                                              │
│  BARCO ALIADO               BARCO ENEMIGO A  │
│  (opacidad normal)          ┌─────────────┐  │
│                             │ HHP 2400    │  │  card de info aparece al hover
│                             │ ELE: Fuego  │  │  (o siempre visible en targeting)
│                             │ [Q][S]      │  │
│                             └─────────────┘  │
│                             BORDE VERDE ▲    │  borde pulsante verde = targetable
│                                              │
│                             CRIATURA B       │
│                             BORDE VERDE ▲    │
│                                              │
├──────────────────────────────────────────────┤
│  HINT BAR  (44px)                            │
│  "Selecciona un enemigo  |  [ESC] Cancelar"  │
├──────────────────────────────────────────────┤
│  [Crew Panel — visible]                      │
└──────────────────────────────────────────────┘
```

**Comportamiento:**
- Todos los barcos/criaturas enemigos vivos reciben borde verde pulsante.
- Hover: card compacta sobre el sprite: HHP actual/max (barra), Elemento, DoTs activos.
- Click: confirma target → SubmitAttack o SubmitAbility.
- NO se muestra panel de crew enemiga en este modo (solo targeting de casco).
- Escape o click en zona vacía: cancela → ActionSelect.

---

### 2.4 Estado de Targeting: Crew Enemiga (Abordaje)

> DECISION D1 RESUELTA (usuario, 2026-06-12): Targeting directo en sprite via
> marcadores/chips overlay. El panel modal fue rechazado. Diseño completo a continuación.

La crew enemiga NO se renderiza como figuras individuales sobre el sprite del barco.
En su lugar se usan **chips overlay** — elementos UI anclados sobre el sprite, uno
por rol vivo, que sirven tanto para el Abordaje como para la Inspección (ver §2.6).

**Paso A — Selección de barco (si hay >1 barco enemigo abordable):**

Igual que antes: borde dorado pulsante en cada barco abordable. Criaturas sin borde
+ icono [X]. Click en barco abordable → activa los marcadores sobre ese barco (Paso B).
Si hay solo 1 barco abordable, Paso A se omite.

**Paso B — Marcadores de Crew sobre el barco seleccionado:**

Al entrar en modo Abordaje (o al seleccionar barco en Paso A), los chips de crew
se despliegan sobre el sprite del barco. El barco objetivo recibe un ligero zoom-in
(escala ~1.3×, animación 0.15 s ease-out) para garantizar espacio suficiente y
claridad de los chips.

```
┌──────────────────────────────────────────────┐
│  HINT BAR: "Selecciona tripulante  [ESC]"    │
├──────────────────────────────────────────────┤
│                                              │
│  BARCO ALIADO          BARCO ENEMIGO A       │
│  (opacidad 60%,        (zoom-in ×1.3)        │
│   no interactuable)    ┌──────────────────┐  │
│                        │   [sprite ×1.3]  │  │
│                        │                  │  │
│                        │  ┌──┐ ┌──┐ ┌──┐ │  │  chips overlay, fila superior
│                        │  │CA│ │IN│ │AR│ │  │  44×56px c/u (icono 20px + bar 8px)
│                        │  │██│ │██│ │XX│ │  │  CA/IN: vivos | AR: muerto (gris+X)
│                        │  └──┘ └──┘ └──┘ │  │
│                        │  ┌──┐ ┌──┐ ┌──┐ │  │  chips fila inferior (si >3 crew)
│                        │  │NA│ │CA│ │CI│ │  │
│                        │  │██│ │██│ │██│ │  │
│                        │  └──┘ └──┘ └──┘ │  │
│                        │     ┌──┐        │  │  chip 7 centrado si crew impar
│                        │     │CO│        │  │
│                        │     │██│        │  │
│                        │     └──┘        │  │
│                        └──────────────────┘  │
│                                              │
│  [chip con focus actual: borde blanco 2px]   │  teclado: flechas ciclan chips vivos
└──────────────────────────────────────────────┘
```

**Anatomía de cada chip (44×56px mínimo — cumple 44px touch target):**

```
┌──────────────┐
│  [icono rol] │  20×20px, icono de rol (mismo que panel aliado §2.5)
│  [minibar HP]│  barra 36×6px, colores estándar (verde/amarillo/rojo)
│  2-letter ID │  "CA" "IN" "AR" etc., 10sp, cream
└──────────────┘

Muerto:
┌──────────────┐
│  [icono rol] │  opacidad 40%, overlay gris semitransparente
│  [   ×   ]  │  icono X centrado, 16px, blanco
│   CAÍDO      │  10sp, cream, etiqueta textual — no depende solo de color
└──────────────┘
```

**Hover sobre chip vivo:**
- Chip escala a 1.15× (feedback de hover inmediato).
- Tooltip flotante aparece adyacente: nombre completo, rol, HP actual/máx, DEF.
- Borde del chip: verde #4CAF50, 2px.

**Chip con focus por teclado:**
- Borde blanco #FFFFFF, 2px. Misma información que hover.

**Click / Enter sobre chip vivo:**
- Confirma → SubmitBoarding(ship, crew). Barco vuelve a escala 1×.

**Escape:**
- Cancela modo Abordaje. Barco vuelve a escala 1×. → ActionSelect.

**Comportamiento si los 7 chips no caben legibles (barco muy pequeño o lejano):**
- El zoom-in ×1.3 es la solución primaria. El barco se escala y re-posiciona
  dentro del campo de batalla (no sale del área de juego).
- Si tras el zoom los chips se superponen (caso extremo con sprites muy pequeños):
  los chips se distribuyen en abanico semicircular alrededor del sprite del barco,
  manteniendo sus anclas visuales con líneas delgadas (2px, opacidad 60%) hacia
  el sprite. Radio del abanico calculado para garantizar que los chips no se solapen.
- En ambos casos el hit target mínimo de 44px se mantiene — el chip es el elemento
  interactuable, no el sprite de crew.

**Navegación por teclado:**
- Flechas izquierda/derecha (o arriba/abajo): ciclan entre chips vivos (los muertos
  se saltan automáticamente).
- Enter: confirma el chip con focus actual.
- Escape: cancela → ActionSelect.
- Tab: también navega chips (consistente con el resto del HUD).

**Navegación por gamepad (nota de viabilidad):**
- D-pad: misma lógica que teclado.
- South (A/X): confirma. East (B/O): cancela.

---

### 2.5 Panel de Crew Aliada (siempre visible, no interactuable)

```
┌──────────────────────────────────────────────┐
│  TRIPULACIÓN                                 │  label 12sp, cream
│  ┌──┐ ┌──┐ ┌──┐ ┌──┐ ┌──┐ ┌──┐ ┌──┐        │
│  │CA│ │IN│ │AR│ │NA│ │CA│ │CI│ │CO│        │  slots 80×80px c/u
│  │[X│ │[█│ │[█│ │[█│ │[█│ │[█│ │[█│        │  [X = muerto, gris]
│  │X]│ │] │ │] │ │] │ │] │ │] │ │] │        │  [█ = vivo]
│  │  │ │HP│ │HP│ │HP│ │HP│ │HP│ │HP│        │  minibar HP bajo el icono
│  └──┘ └──┘ └──┘ └──┘ └──┘ └──┘ └──┘        │
│  Sinergias: [Piratas x3] [Artilleros x2]     │  badges, gris si cap. muerto
└──────────────────────────────────────────────┘
```

**Roles mostrados (en orden fijo):**
CA=Capitán, IN=Intendente, AR=Artillero, NA=Navegante, CA=Carpintero,
CI=Cirujano, CO=Contramaestre. (2 letras para que quepan en 80px)

**Datos por slot:**
- Icono de rol (imagen, no solo texto).
- Minibar HP: `CrewMemberState.CurrentHP / crew.MaxHP` (HP fijo por rol según GDD §6).
- Estado muerto: slot oscurecido (overlay semitransparente) + icono "X" encima.
  NO se usa solo color: el "X" es la señal primaria. El color es refuerzo.
- Al morir el Capitán: badges de sinergias pasan a gris con tachado.
- Tooltip al hover (mouse): nombre completo del crew member + rol + HP actual.

---

### 2.6 Cards de Barco Enemigo (siempre visibles en campo de batalla)

```
┌─────────────────────────────┐
│  [sprite barco]             │  sprite del barco
│  ┌─────────────────────────┐│
│  │ HHP [███████░░░] 2100/  ││  barra HHP, color igual que barco aliado
│  │     /3000               ││  (verde→amarillo→rojo)
│  │  MP [████░░░░░]  60/120 ││  barra MP azul (si tiene MP)
│  │ ELE: [icono]  [Q][S]    ││  DoTs activos
│  │ Escudo: [icono azul]    ││  si IsManeuvering
│  └─────────────────────────┘│
└─────────────────────────────┘
```

**Hover / tap en barco enemigo (fuera de modo Abordaje) — Inspección de Crew:**

> DECISION D3 RESUELTA (usuario, 2026-06-12): Inspección incluida en S4-06.
> Reutiliza los mismos chips overlay del Abordaje (§2.4) para consistencia y ahorro
> de implementación.

Al hacer hover sobre un barco enemigo (estado ActionSelect, no en targeting activo),
los chips de crew aparecen sobre el sprite del barco en modo lectura: misma
anatomía visual (icono rol + minibar HP + 2-letter ID + "CAÍDO" si muerto), pero
SIN zoom-in y SIN interacción de click.

```
HOVER en barco enemigo (estado ActionSelect):

  BARCO ENEMIGO A
  ┌──────────────────┐
  │   [sprite]       │  sin zoom, sin borde de targeting
  │                  │
  │  ┌──┐ ┌──┐ ┌──┐ │  chips overlay, modo lectura
  │  │CA│ │IN│ │AR│ │  aparecen con fade-in 0.1s
  │  │██│ │██│ │XX│ │  no clickeables en este estado
  │  └──┘ └──┘ └──┘ │
  │  ┌──┐ ┌──┐ ┌──┐ │
  │  │NA│ │CA│ │CI│ │
  │  │██│ │██│ │░░│ │  barra roja = HP bajo
  │  └──┘ └──┘ └──┘ │
  │     ┌──┐        │
  │     │CO│        │
  │     │██│        │
  │     └──┘        │
  └──────────────────┘
  Tooltip tarjeta adicional (aparece junto al barco):
  ┌────────────────────────┐
  │ Barco Enemigo A        │
  │ HHP: 2100/3000         │
  │ Crew viva: 6/7         │
  │ [Tip] Abordaje: 1 caído│
  └────────────────────────┘
```

**Diferencias entre modo Inspección y modo Abordaje (chips):**

| Propiedad | Inspección (hover) | Abordaje (targeting) |
|-----------|-------------------|---------------------|
| Zoom del barco | No | Sí (×1.3) |
| Chips clickeables | No | Sí (solo vivos) |
| Borde de chip en hover | Gris #888, 1px (cursor informa) | Verde #4CAF50, 2px |
| Tooltip chip | Solo nombre + rol + HP | Nombre + rol + HP + DEF |
| Borde del barco | Ninguno adicional | Dorado #FFD700 pulsante |
| Navegación teclado | No (solo hover mouse) | Flechas + Enter + Escape |

**Cierre del panel de Inspección:**
- Los chips desaparecen con fade-out 0.1 s cuando el cursor sale del área del barco.
- Escape también cierra (aunque el jugador no esté en modo targeting).
- Click fuera del barco: cierra.

**Gamepad (nota de viabilidad):**
- D-pad sobre un barco enemigo en ActionSelect: botón "Inspeccionar" dedicado
  (sugerido: Left Bumper) despliega los chips en modo lectura sobre el barco
  con focus activo. Mismo botón cierra.

---

### 2.7 Overlays de Estado

**WaveTransition (entre oleadas):**
```
┌──────────────────────────────────────────────┐
│                                              │
│         OLEADA COMPLETADA                    │  texto grande, cream
│         Nueva oleada en...                  │
│         [3] [2] [1]                          │  countdown 3s o botón saltar
│                                              │
│  [Estado del barco persistido]               │  reminder: "Tu barco retiene
│   HHP/MP actuales visibles                  │  daño y MP entre oleadas"
│                                              │
└──────────────────────────────────────────────┘
```

**BattleOver — Victoria:**
```
┌──────────────────────────────────────────────┐
│        ¡VICTORIA!  (verde dorado)            │
│   "La Perdición" ha triunfado                │
│                                              │
│   HHP final: 1840/3200                       │  datos de la batalla
│   Crew sobreviviente: 5/7                    │
│   MP restante: 40/150                        │
│   Rondas: 12                                 │
│                                              │
│         ┌───────────────────┐                │
│         │    CONTINUAR      │                │
│         └───────────────────┘                │
└──────────────────────────────────────────────┘
```

**BattleOver — Derrota:**
```
┌──────────────────────────────────────────────┐
│         DERROTA  (rojo)                      │
│   El barco se ha hundido                     │
│                                              │
│   ┌────────────────┐  ┌────────────────┐     │
│   │   REINTENTAR   │  │     SALIR      │     │
│   └────────────────┘  └────────────────┘     │
└──────────────────────────────────────────────┘
```

---

## 3. Mapa de Datos

Cada elemento UI referenciado a su fuente en el runtime.

| Elemento UI | Fuente en código | Campo / Método |
|-------------|-----------------|----------------|
| Nombre del barco aliado | `ShipCombatant.Ship` | `ShipData.DisplayName` |
| HHP aliado (barra) | `ShipCombatant` | `CurrentHHP` / `MaxHHP` |
| MP aliado (barra) | `ShipCombatant` | `CurrentMP` / `MaxMP` |
| LB bar (binaria cosmética) | `ShipCombatant` | `LBUsedThisRound` (bool) + `AbilityPool.Any(e => e.CanLimitBreak)` — ver nota* |
| Maniobra activa (escudo) | `ShipCombatant` | `IsManeuvering` |
| Stats del barco (FPW, HDF, SPD) | `ShipCombatant` | `GetEffectiveShipStat(ShipStatType.FPW/HDF/SPD)` |
| Elemento del barco | `ShipCombatant` | `Element` (via `ShipData.Element`) |
| DoTs activos | `ShipCombatant` | `StatusEffects` (lista de `StatusInstance`) |
| Crew: slot y rol | `ShipCombatant.Crew` | `CrewMemberState.Role` (NavalRole enum) |
| Crew: nombre | `CrewMemberState.Unit` | `CharacterData.DisplayName` |
| Crew: HP actual | `CrewMemberState` | `CurrentHP` (campo propio — no es el del barco) |
| Crew: HP máximo | `CrewMemberState` | `MaxHP` (fijo por rol, definido en constructor) |
| Crew: estado vivo/muerto | `CrewMemberState` | `IsDead` |
| Sinergias activas | `ShipCombatant` | `CrewSynergies` (`IReadOnlyList<ActiveSynergy>`) |
| Capitán vivo (sinergias on/off) | `ShipCombatant` | `CaptainAlive` |
| Pool de habilidades | `ShipCombatant` | `AbilityPool` (`IReadOnlyList<AbilityData>`) |
| Habilidad: costo MP | `AbilityData` | `MPCost` |
| Habilidad: disponible | `ShipCombatant` | `IsAbilityReady(AbilityData)` — incluye MP, cooldown, Silencio |
| Habilidad: cooldown restante | `ShipCombatant` | `GetCooldownRemaining(AbilityData)` |
| Habilidad: LB flag | `AbilityEntry` | `CanLimitBreak` |
| Habilidad: tipo de target | `AbilityData` | `TargetType` |
| Habilidades: ¿pool vacío? | `ShipCombatant.AbilityPool` | `.Count == 0` (tras muerte de crew que aportaba habilidades) |
| HHP enemigo (barra) | `ShipCombatant` (enemigo) | `CurrentHHP` / `MaxHHP` |
| MP enemigo | `ShipCombatant` (enemigo) | `CurrentMP` / `MaxMP` |
| Maniobra enemiga | `ShipCombatant` (enemigo) | `IsManeuvering` |
| Elemento enemigo | `ShipCombatant` (enemigo) | `Element` |
| DoTs enemigo | `ShipCombatant` (enemigo) | `StatusEffects` |
| Crew enemiga: lista | `ShipCombatant.Crew` | `IReadOnlyList<CrewMemberState>` |
| Crew enemiga: HP (chips overlay) | `CrewMemberState` | `CurrentHP` / `MaxHP` |
| Crew enemiga: DEF (tooltip chip) | `CrewMemberState.Unit` | `CharacterData.BaseStats[(int)StatType.DEF]` |
| Crew enemiga: viva / muerta (chip) | `CrewMemberState` | `IsDead` — chip muerto: overlay gris + icono X + label "CAÍDO" |
| Crew enemiga: rol (chip label) | `CrewMemberState` | `Role` (NavalRole enum → 2-letter string) |
| Crew enemiga: nombre (chip tooltip) | `CrewMemberState.Unit` | `CharacterData.DisplayName` |
| Chips: count vivos (inspección) | Lógica UI | `ship.GetLivingCrew().Count` — mostrado en tooltip de barco "Crew viva: X/7" |
| Chips: modo Abordaje vs Inspección | Lógica UI | Estado UI activo (TargetCrew vs hover en ActionSelect) |
| Abordaje disponible (acción) | Lógica UI | `target is ShipCombatant ts && ts.GetLivingCrew().Count > 0` |
| Abordaje vs criatura (disabled) | Lógica UI | Enemigo en campo sin `Crew` (criaturas: `_crew` vacío) |
| Número de oleada | `CombatManager` | `TotalWaves` + índice actual (patrón de `HandleWaveStart`) |
| Initiative Bar | `CombatManager.Bar` | `Bar.Entries` (lista de `InitiativeEntry`) |

*Nota LB bar (DECISION D2 RESUELTA — usuario, 2026-06-12): Barra binaria cosmética,
consistente visualmente con la barra LB del combate terrestre. Lógica de relleno:
  - LLENA (dorada) si: `AbilityPool.Any(e => e.CanLimitBreak) == true` Y `LBUsedThisRound == false`
  - VACÍA si: no hay habilidades LB en el pool, O `LBUsedThisRound == true`
No requiere acumulador nuevo en el backend. El UI-programmer combina los dos bools
existentes. No hay gap de datos — se cierra el riesgo identificado en el spec original.

---

## 4. Tabla de Estados de Botones de Acción

| Acción | Condición Enabled | Condición Disabled | Razón (texto de feedback) | Feedback Visual Adicional |
|--------|------------------|-------------------|--------------------------|--------------------------|
| **Cañonazo** | Siempre (hay enemigos vivos) | Nunca | — | Icono de cañón activo |
| **Habilidad Naval** | `AbilityPool.Count > 0` Y al menos 1 habilidad lista | `AbilityPool.Count == 0` O todas sin MP/cooldown/Silencio | "Sin habilidades disponibles" / "Silencio activo" | Si Silencio: icono de boca cruzada en el botón. Si pool vacío: icono diferente |
| **Maniobra Evasiva** | Siempre | Nunca* | — | Si ya activa: badge "ACTIVA" en el botón (no bloquea re-selección — el resolver la resetea) |
| **Abordaje** | Al menos 1 barco enemigo vivo con `GetLivingCrew().Count > 0` | Todos los enemigos vivos son criaturas (`_crew` vacío) O todos los crew enemigos están muertos | "Imposible: no hay tripulación que abordar" | Icono de garfio con X roja. Tooltip explica la razón |
| **Reparar** | `CurrentMP >= REPAIR_MP_COST (20)` | `CurrentMP < 20` | "Sin MP (necesitas 20)" | Barra de MP parpadea brevemente cuando se intenta sin MP. Costo "20 MP" visible en el botón siempre |
| **Pasar Turno** | Siempre | Nunca | — | Botón secundario, visualmente más pequeño / menos prominente que las 5 acciones principales |

*Maniobra Evasiva podría considerarse "inútil" si ya está activa, pero el código
no la bloquea (simplemente resetea `IsManeuvering = true` de nuevo). Dejar enabled
es correcto — no confundir al jugador con un disabled que no tiene razón mecánica.

**Nota sobre Silencio + Reparar:** Reparar es acción base (no `AbilityData`) — el
check de Silencio en `IsAbilityReady()` no aplica. Reparar SIEMPRE es enabled si
hay MP >= 20, incluso bajo Silencio. El UI-programmer debe implementar el check de
Reparar separado del check de habilidades (ver Edge Case 14 del GDD).

**Nota sobre Ceguera:** No bloquea acciones, pero Cañonazo y Abordaje pueden producir
MISS. El botón permanece enabled, pero si Ceguera está activa, mostrar icono de
advertencia [!] en Cañonazo y Abordaje con tooltip "Ceguera activa: 50% de fallo".

---

## 5. Riesgos UX — Estado de Decisiones

### Riesgo 1 — Targeting de Crew en Abordaje

**RESUELTO (usuario, 2026-06-12) — Decisión: Targeting directo en sprite via chips overlay.**

El panel modal fue rechazado. El spec adopta chips overlay anclados sobre el sprite
del barco (anatomía y comportamiento completos en §2.4). El riesgo original de
hit targets demasiado pequeños se mitiga con dos mecanismos:

1. Zoom-in ×1.3 del barco objetivo al entrar en modo Abordaje (solución primaria).
2. Abanico semicircular con líneas de anclaje si los chips se superponen tras el zoom
   (fallback para sprites excepcionalmente pequeños).

Hit target mínimo garantizado: 44×56px por chip. El chip es el elemento interactuable;
no se hace click en el sprite de crew (que no existe como entidad visual individual).

Riesgo residual (P2, post-demo): en dispositivos con pantallas muy pequeñas o con
barcos que tengan sprites de alta complejidad visual, el abanico puede verse
desordenado. Solución a largo plazo: permitir al art-director definir un punto de
anclaje por sprite para el origen del abanico.

---

### Riesgo 2 — Barra de Limit Break

**RESUELTO (usuario, 2026-06-12) — Decisión: Barra binaria cosmética.**

La barra LB se renderiza llena (dorada) o vacía según la combinación de
`AbilityPool.Any(e => e.CanLimitBreak)` y `!LBUsedThisRound`. No se requiere
acumulador nuevo en el backend. El gap de datos identificado en el spec original
queda cerrado — la lógica completa está en §3 (nota de LB bar).

Consistencia visual con combate terrestre: la barra usa la misma paleta dorada y
el mismo componente de barra que el HUD terrestre. El UI-programmer debe reutilizar
el mismo prefab de barra con relleno controlado por bool en lugar de float.

---

### Riesgo 3 — Inspección de Crew Enemiga

**RESUELTO (usuario, 2026-06-12) — Decisión: Incluida en S4-06, reutiliza chips del Abordaje.**

La inspección al hover reutiliza exactamente los mismos chips overlay de §2.4
en modo lectura (sin zoom, sin interacción de click). El UI-programmer implementa
un único componente `CrewChipOverlay` con dos modos: `Inspect` y `Target`. Esto
reduce el scope de implementación respecto a tener dos sistemas separados.

El HP exacto SÍ es visible en modo Inspección (a diferencia de lo que proponía
la Opción A original) — los chips ya muestran la minibar de HP completa. El tooltip
de barco añade el resumen "Crew viva: X/7".

Riesgo residual (P3): en gamepad, el hover no existe — se necesita un botón de
inspección explícito (Left Bumper sugerido). Esto es una nota de implementación
para el UI-programmer, no un bloqueante del demo PC.

---

## 6. Accesibilidad

### Checklist por criterio

| Criterio | Estado en este spec | Notas |
|----------|--------------------|----|
| Usable con teclado solo | DISEÑADO — Tab navega entre botones; Escape cancela targeting; Enter confirma | UI-programmer debe implementar `m_FirstSelected` en EventSystem (P0 de S3-11 aún abierto) |
| Usable con gamepad solo | NOTAS DE VIABILIDAD — no bloqueante para demo PC | D-pad para navegar botones; South (A/X) para confirmar; East (B/O) para cancelar. Panel de crew en Abordaje es scroll-navegable con D-pad |
| Texto legible a tamaño mínimo | DISEÑADO — mínimo 12sp en minibarras de crew; 14sp en battle log; 16sp en botones | Verificar con CanvasScaler Scale With Screen Size (P0 de S3-11) |
| Sin dependencia de color solo | DISEÑADO — cada estado usa icono + texto + color | DoT icons: forma distinta por tipo. Crew muerta: etiqueta "CAÍDO" + overlay + icono X. Disabled buttons: texto de razón visible |
| Sin flashes | DISEÑADO — animaciones de daño son flashes cortos de números flotantes | No hay flashes >= 3Hz. La animación de hundimiento debe tener aviso si incluye flash |
| Subtítulos para diálogo | N/A — combate naval no tiene diálogo en demo | Si se añade narración de jefe, subtítulos son obligatorios |
| UI escala en todas las resoluciones | HEREDADO DE S3-11 — requiere CanvasScaler Scale With Screen Size (P0 ya identificado) | El naval HUD debe crearse con `m_UiScaleMode: 1`, ref 1080×1920, match height |
| Touch targets >= 44px mínimo | DISEÑADO — botones de acción 240×80px; chips de crew overlay 44×56px; botón Volver 44px | Chips en Abordaje e Inspección: 44px garantizado por diseño de chip, independiente del zoom del sprite |

### Casos específicos de accesibilidad naval

**Crew muerta — señal no-color:**
El slot de crew muerta usa: overlay semitransparente gris (color), icono "X" negro
(forma), etiqueta textual "CAÍDO" (texto). Tres señales independientes del color.

**Botón Abordaje disabled — razón siempre visible:**
El texto de razón ("Imposible: criaturas no tienen tripulación") debe ser legible
incluso en estado disabled. Usar `DisabledLabel` (#A08040 a180) de la paleta
canónica — contraste ~2.1:1 sobre `BtnDisabledBg`. NOTA: este contraste falla
WCAG AA (encontrado en S3-11 §3.2). Para la demo se acepta como P2; solución a
largo plazo: usar contraste >= 4.5:1 en el label de razón, o mostrar la razón
fuera del botón (en la hint bar).

**Habilidades bajo Silencio:**
Banner textual "SILENCIO ACTIVO — Habilidades bloqueadas" en la parte superior del
panel de habilidades. NO solo oscurecer los botones.

**Targeting de crew con HP bajo (chips overlay):**
La minibar de HP dentro de cada chip usa los mismos colores que el HHP del barco
(verde/amarillo/rojo con umbrales al 50% y 25%). La barra es la señal primaria;
el tooltip del chip incluye el valor numérico HP actual/máx como respaldo.
El chip muerto añade icono X + label "CAÍDO" — no depende solo del color gris.

**Multi-enemigo en campo:**
Con 3+ enemigos en pantalla, los sprites deben tener suficiente espacio para que
los bordes de targeting (verde/dorado) sean distinguibles. Mínimo 8px de separación
entre sprites. Si el arte de los barcos es muy detallado, el borde de targeting
necesita ser al menos 3px de ancho (igual que el Outline del combate terrestre:
`effectDistance = (3,3)`).

---

## 7. Eventos a Suscribir (para UI-Programmer)

Extensión del patrón de `CombatHUDCanvas.SubscribeEvents()` para el HUD naval.
Eventos nuevos que el combate naval emite y que la UI naval debe escuchar:

| Evento | Fuente en código | Qué actualiza en UI |
|--------|-----------------|---------------------|
| `GameEvents.OnCrewDamaged` | `NavalTurnResolver.DamageCrew` → `GameEvents.PublishCrewDamaged` | Minibar HP del crew member en el panel aliado o en el panel de targeting |
| `GameEvents.OnCrewDied` | `NavalTurnResolver.DamageCrew` → `GameEvents.PublishCrewDied` | Slot del crew: overlay gris + icono X + etiqueta "CAÍDO". Si era Capitán: badges de sinergias → gris |
| `GameEvents.OnShipStatsRecalculated` | `NavalTurnResolver.DamageCrew` → `GameEvents.PublishShipStatsRecalculated` | Panel de stats del barco aliado: flash rojo en stats que bajaron. Actualizar valores mostrados |
| `GameEvents.OnManeuverActivated` | `NavalTurnResolver.ResolveAction` | Escudo azul en sprite del barco. Badge "MANIOBRA" en el botón correspondiente |
| `GameEvents.OnLimitBreakActivated` | `NavalTurnResolver.TryLimitBreak` | Efecto visual LB. Badge "LB" en IB (turno extra insertado) |
| `GameEvents.OnWaveStart` | `CombatHUDCanvas` ya tiene `HandleWaveStart` | WaveLabel actualizado. RebuildEnemyCards equivalente naval |
| `GameEvents.OnStatusApplied` | `NavalTurnResolver.ApplySecondaryEffects` | Añadir icono DoT al barco target. El tipo de DoT determina el icono |
| `GameEvents.OnDamageDealt` (DamageSource.Burn) | `NavalTurnResolver.ApplyPostActionDoTs` | Actualizar barra HHP del barco. Número flotante rojo sobre el sprite |
| `GameEvents.OnDamageDealt` (DamageSource.Bleed/Poison) | `NavalTurnResolver.ApplyCrewDoT` | Número flotante rojo sobre el slot del crew afectado |

Los eventos `OnBattleStart`, `OnRoundStart`, `OnTurnStart`, `OnTurnEnd`,
`OnActionChosen`, `OnDamageDealt`, `OnHealApplied`, `OnUnitDied`, `OnWaveComplete`,
`OnBattleEnd` ya existen en `GameEvents` y siguen el mismo patrón del HUD terrestre.

---

## 8. Notas de Implementación para el UI-Programmer

(Solo notas de scope y viabilidad — el diseño final de código lo define el
UI-programmer.)

- El `NavalHUDCanvas` debe detectar si el `ICombatant` en turno es un
  `ShipCombatant` (similar al check `if (actor is not CombatantState combatant)` en
  `HandleTurnStart` del HUD terrestre) y delegar al HUD naval.
- `ShipCombatant` implementa `ICombatant` — el HUD terrestre ya filtra los
  `ShipCombatant` con `if (entry.Combatant is not CombatantState unit) continue`.
  El HUD naval hace el inverso: actúa solo sobre `ShipCombatant`.
- `CrewMemberState` no es un `ICombatant` — no toma turnos y no aparece en la IB.
  La UI accede a él solo a través de `ShipCombatant.Crew`.
- `ShipCombatant.GetLivingCrew()` devuelve una nueva `List<CrewMemberState>` cada
  llamada — para performance en UI, cachear el resultado por frame si es necesario.
- El sistema de cooldowns en `ShipCombatant` usa `string` (ability ID) como clave.
  `GetCooldownRemaining(ability)` devuelve turnos restantes (int >= 0).
- `AbilityPool` puede contener duplicados (GDD: si dos crew members tienen la misma
  habilidad, aparece dos veces). El panel de habilidades debe mostrar ambas instancias
  por separado (son instancias distintas con cooldowns independientes potencialmente).

---

## Quick Reference: Archivos Leídos

- `design/gdd/combate-naval.md`
- `Assets/Scripts/UI/Combat/CombatHUDCanvas.cs`
- `Assets/Scripts/Core/Combat/NavalTurnResolver.cs`
- `Assets/Scripts/Core/Combat/ShipCombatant.cs`
- `Assets/Scripts/Core/Data/ShipData.cs`
- `Assets/Scripts/Core/Data/NavalRole.cs`
- `docs/art/ui-s311-ux-audit.md`
- `.claude/docs/coplay-unity-lessons.md` §4
