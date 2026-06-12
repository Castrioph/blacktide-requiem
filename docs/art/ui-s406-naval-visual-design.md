# S4-06 Visual Design Spec — Pantalla de Combate Naval
> Author: Art Director agent
> Date: 2026-06-12
> Sprint: S4-06 Phase 2 — Visual dress-only (NO code)
> Base UX layout: `docs/art/ui-s406-naval-ux-spec.md` (layout is FROZEN — this doc
>   only applies color, typography, texture, animation, and asset direction on top)
> Palette source: `docs/art/ui-s311-visual-design.md` §1.2 +
>   `.claude/docs/coplay-unity-lessons.md` §4
> Reference resolution: 1080×1920 portrait, CanvasScaler Scale With Screen Size,
>   match height 1.0

---

## Quick Reference — All Color Tokens Used in This Document

Only the tokens actually used in this spec are listed here for fast lookup.
Full canonical table: `ui-s311-visual-design.md` §1.2.

| Token | Hex | Unity Color(r,g,b) | Primary use in this screen |
|-------|-----|--------------------|---------------------------|
| `BgDark` | `#140F24` | 0.08, 0.06, 0.14 | Screen background |
| `PanelDark` | `#1F172E` | 0.12, 0.09, 0.18 | Panels, stat box |
| `HeaderBase` | `#1A0D00` | 0.10, 0.05, 0.00 | Header/footer fills |
| `Gold` | `#D4A017` | 0.83, 0.63, 0.09 | Primary borders, LB bar full |
| `GoldBright` | `#FFD700` | 1.00, 0.84, 0.00 | Catch-lights, active targeting border |
| `GoldMid` | `#E8B420` | 0.91, 0.71, 0.13 | Button hover, IB active icon ring |
| `GoldDark` | `#B8880F` | 0.72, 0.53, 0.06 | Button pressed |
| `Cream` | `#EDD9A3` | 0.93, 0.85, 0.64 | Secondary text everywhere |
| `CreamMuted` | `#F5E6C8` | 0.96, 0.90, 0.78 | Crew names, ability names |
| `WoodBase` | `#3D2810` | 0.24, 0.16, 0.06 | Panel fills (action panel, crew panel) |
| `WoodBorder` | `#5C3D1E` | 0.36, 0.24, 0.12 | Panel borders |
| `WoodLight` | `#4A3018` | 0.29, 0.19, 0.09 | Hovered card/row fill |
| `BtnDisabledBg` | `#3D3020` | 0.24, 0.19, 0.13 | Disabled buttons |
| `DisabledLabel` | `#A08040` | 0.63, 0.50, 0.25 | Disabled button text (WCAG AA) |
| `Shadow` | `#050D14` | 0.02, 0.05, 0.08 | Drop shadows, bottom gradients |

**New tokens for combat naval (additions — consistent with existing palette):**

| Token | Hex | Unity Color(r,g,b) | Primary use |
|-------|-----|--------------------|-------------|
| `HpHigh` | `#4CAF50` | 0.30, 0.69, 0.31 | HHP/crew HP bar > 50% |
| `HpMid` | `#FFCA28` | 1.00, 0.79, 0.16 | HHP/crew HP bar 25%–50% |
| `HpLow` | `#EF5350` | 0.94, 0.33, 0.31 | HHP/crew HP bar < 25% |
| `MpBlue` | `#1E88E5` | 0.12, 0.53, 0.90 | MP bar fill |
| `MpBlueDark` | `#0D47A1` | 0.05, 0.28, 0.63 | MP bar bg |
| `LbGold` | `#D4A017` | 0.83, 0.63, 0.09 | LB bar fill (full state) = Gold |
| `LbEmpty` | `#2A2010` | 0.16, 0.13, 0.06 | LB bar fill (empty state) |
| `TargetGreen` | `#4CAF50` | 0.30, 0.69, 0.31 | Targetable border (= HpHigh) |
| `TargetGold` | `#FFD700` | 1.00, 0.84, 0.00 | Boarding target border (= GoldBright) |
| `DotBurn` | `#FF6F00` | 1.00, 0.44, 0.00 | Quemadura icon/tint |
| `DotPoison` | `#7CB342` | 0.49, 0.70, 0.26 | Veneno icon/tint |
| `DotBleed` | `#C62828` | 0.78, 0.16, 0.16 | Sangrado icon/tint |
| `DotSilence` | `#7B1FA2` | 0.48, 0.12, 0.64 | Silencio icon/tint |
| `DotBuff` | `#1565C0` | 0.08, 0.40, 0.75 | Buff positive icon/tint |
| `ManeuverBlue` | `#29B6F6` | 0.16, 0.71, 0.96 | Maneuver shield overlay |
| `ChipDeadOverlay` | `#1A1A1A` | 0.10, 0.10, 0.10 | Dead crew chip overlay |
| `HintBarBg` | `#0D0A14` | 0.05, 0.04, 0.08 | Hint bar / battle log bg |
| `FlashDamage` | `#EF5350` | 0.94, 0.33, 0.31 | HHP flash on damage |
| `FlashHeal` | `#4CAF50` | 0.30, 0.69, 0.31 | HHP flash on repair |

---

## Section 1 — Fondo de Escena Naval

### 1.1 Diseño

El fondo es la diferencia visual más grande respecto al combate terrestre.
El campo de batalla naval debe transmitir: mar abierto, noche o amanecer con bruma,
amenaza y misterio vudú. No puede ser genérico.

**Capa completa de fondo (bottom-to-top):**

| Capa | Objeto | Técnica primitiva | Asset final | Cobertura |
|------|--------|-------------------|-------------|-----------|
| 1 | `BgSolid` | Image `#140F24` alpha 255 | — | fill canvas |
| 2 | `BgSkyGradient` | 2×2 PNG degradado | `env_naval_sky_gradient.png` (2×2) | fill canvas, alpha 200 |
| 3 | `BgSeaSprite` | Image `#0A1A2A` alpha 180 (azul muy oscuro) | `env_naval_sea_bg.png` (1080×640) | zona inferior ~620px zona de batalla |
| 4 | `BgHazeOverlay` | Image `#6A1B9A` alpha 18 (additive vudú) | — (color puro, sin sprite) | fill canvas |
| 5 | `BgVignetteBot` | Image `#050D14` alpha 160, grad. bot | — (2×2 PNG reutiliza `ui_bg_ocean_gradient.png`) | bottom 300px |

**Degradado del cielo (capa 2):** 2×2 PNG pintado a mano:
- Fila superior: `#1A0D2E` (morado noche)
- Fila inferior: `#0A1A2A` (azul oceánico)

La capa 4 (`#6A1B9A` alpha 18) aplica el tono vudú sin que abrume. Es la misma
técnica que el `BgNoise` del StageSelect pero con el acento Voodoo Violet en lugar
del `#4A7FA5` náutico diurno.

### 1.2 Versión primitiva lista para construcción

Con solo Image components de paleta + el degradado 2×2 existente
(`ui_bg_ocean_gradient.png` reutilizado) el fondo es construible en una sesión.
El sprite `env_naval_sea_bg.png` es P1 (polish) — la escena no lo requiere para demo.

---

## Section 2 — Initiative Bar (Barra de Iniciativa)

La IB naval reutiliza el componente terrestre con estas adaptaciones:

### 2.1 Tratamiento visual

| Elemento | Versión primitiva | Versión asset final |
|----------|------------------|---------------------|
| Contenedor IB | Image `#1A0D00` alpha 230, height 56px, top-stretch | Sin cambio (HeaderBase) |
| Borde inferior | Image `#D4A017` alpha 140, 2px, anchored bottom | Sin cambio |
| Iconos de barco aliado | Image 44×44 redonda, fill `#1E88E5` (Corsair Blue) — clase "aliado" | `ui_ib_ship_allied_44.png` |
| Iconos de barco enemigo | Image 44×44 redonda, fill `#BF360C` (Temple Ember) — clase "enemigo" | `ui_ib_ship_enemy_44.png` |
| Iconos de criatura marina | Image 44×44 redonda, fill `#6A1B9A` (Voodoo Violet) — clase "criatura" | `ui_ib_creature_44.png` |
| Borde icono turno activo | Outline 3px `#FFD700` GoldBright pulsante (ver §10 animaciones) | Sin cambio |
| Borde icono turno inactivo | Outline 2px `#3A2A50` alpha 200 | Sin cambio |

**Diseño de los iconos de barco (44×44, forma):**
- Aliado: silueta de galeón de perfil, mirando a la derecha, color `#1E88E5` sobre `#0A1520`
- Enemigo: silueta de galeón de perfil, mirando a la izquierda, color `#BF360C` sobre `#200A0A`
- Criatura: silueta de pulpo/kraken esquematizado, color `#6A1B9A` sobre `#150A20`

La dirección del sprite (aliado mira derecha, enemigo mira izquierda) es consistente
con el layout del campo de batalla y orienta al jugador sin texto.

---

## Section 3 — Indicador de Oleada y Banner de Fase de Jefe

### 3.1 Wave Label (normal)

| Propiedad | Valor |
|-----------|-------|
| Container | `WaveLabel` — Image `#1A0D00` alpha 200, height 28px, stretch horizontal |
| Borde inferior | 1px Image `#D4A017` alpha 120 |
| Texto "OLEADA X/Y" | Pirata One Regular 14sp, `#EDD9A3` alpha 220, center |
| Sombra texto | offset (1, -1), `#000000` alpha 120 |

### 3.2 Banner de Fase de Jefe

Aparece encima del WaveLabel cuando la oleada activa es de tipo Boss.
Ancho completo, height 48px. Reemplaza visualmente al WaveLabel (el label queda
bajo el banner con menor alpha).

| Propiedad | Valor |
|-----------|-------|
| BannerBg | Image `#6A1B9A` (Voodoo Violet) alpha 220 |
| Borde superior + inferior | 2px Image `#FFD700` alpha 200 |
| Texto "FASE DE JEFE — [nombre]" | Pirata One Regular 18sp, `#FFD700` (GoldBright) |
| Efecto | TMP Drop Shadow offset (2, -2) `#000000` alpha 200 |
| Animación aparición | Slide down + fade-in desde y+30, 0.3s ease-out |

El Voodoo Violet del banner señala con claridad: "esto es diferente y peligroso."
No hay otro elemento en pantalla con este color a máxima saturación.

---

## Section 4 — Campo de Batalla: Sprites de Barcos y Criaturas

### 4.1 Tratamiento de sprites de barcos (aliado + enemigos)

Los sprites de barcos son el elemento más importante del campo de batalla.
Deben leerse bien contra el fondo oscuro y distinguirse entre sí.

**Reglas de composición:**
- Barco aliado: posición izquierda del campo, ligeramente más bajo y más grande
  (sugerido 20% mayor en escala que enemigos de tier estándar) para reforzar POV.
- Barcos enemigos: derecha, ligeramente más arriba. Múltiples barcos se apilan
  verticalmente con 8px mínimo de separación.
- Criaturas marinas: misma zona que enemigos pero con silueta orgánica (tentáculos,
  aletas) que las diferencie visualmente de los barcos con casco.

**Outline de targeting (no requiere sprite adicional):**
- En estado normal: sin outline visible.
- Targetable (verde): Unity `Outline` component, `effectDistance (3,3)`,
  color `#4CAF50` (TargetGreen) alpha 220.
- Boarding target (dorado pulsante): `effectDistance (3,3)`, `#FFD700`
  (GoldBright), pulsación ver §10.
- Maniobra activa: overlay shield Image 80×80px `#29B6F6` (ManeuverBlue) alpha 60,
  anclado sobre el sprite, con icono escudo (ver §4.2).

### 4.2 Overlay de Maniobra Evasiva (escudo azul)

| Capa | Descripción | Valor |
|------|-------------|-------|
| Shield tint | Image fill `#29B6F6` alpha 50, mismo size que sprite | primitivo ok |
| Shield icon | Image 40×40 centrada sobre el sprite | `ui_status_maneuver_shield_40.png` |
| Shield icon tint | `#29B6F6` alpha 200 | |
| Animación entrada | Scale 0→1 + fade-in, 0.2s ease-out | |

Versión primitiva: solo el tint azul es suficiente para el demo. El icono de escudo
es P1.

### 4.3 Barras de HHP de barcos enemigos (inline bajo sprite)

Las barras de HHP enemigo se ubican directamente bajo el sprite del barco,
dentro del card de barco enemigo (UX spec §2.6).

| Componente | Versión primitiva | Nota |
|------------|------------------|------|
| Contenedor card | Image `#1F172E` alpha 210, rounded (corner radius 0 sin sprite 9-slice) | PanelDark |
| Borde card | Image `#5C3D1E` alpha 200 (WoodBorder), 1px inset | |
| Barra HHP background | Image `#2A1A1A` alpha 220, height 10px | |
| Barra HHP fill | Image color por umbral (verde/amarillo/rojo), height 10px | ver §5.1 |
| Texto "HHP X/Y" | Noto Sans Regular 11sp `#EDD9A3` alpha 200 | |
| Icono elemento | 20×20, ver §8 | |
| DoT icons | 24×24 cada uno, ver §7 | |

**Diseño del card enemigo:**
- Corner radius: 0 (primitiva). Con asset: `ui_panel_enemycard_9slice.png` (P1).
- El fondo `#1F172E` oscuro hace que las barras de color resalten inmediatamente.
- El borde `#5C3D1E` (madera) conecta visualmente con los demás paneles del HUD.

---

## Section 5 — Panel de Stats del Barco Aliado

### 5.1 Estructura visual

El panel ocupa los 100px definidos en UX spec §2.1. Fondo madera como los cards
de StageSelect — establece consistencia con la "identidad visual de panel" del juego.

**Capas del panel stats (bottom-to-top):**

| Capa | Valor primitivo |
|------|----------------|
| Fondo base | Image `#3D2810` (WoodBase) alpha 230 |
| Borde superior | 2px Image `#D4A017` (Gold) alpha 180 |
| Catch-light top | 6px Image `#8B5E3C` (WoodCatch) alpha 100, anchored top |

**Tipografía y jerarquía del panel:**

| Elemento | Font | Size | Color | Alpha |
|----------|------|------|-------|-------|
| Nombre del barco "La Perdición" | Pirata One | 18sp | `#D4A017` Gold | 255 |
| Elemento del barco (icono) | Image 18×18 + label inline | — | — | — |
| Stats (FPW, HDF, SPD) | Noto Sans Regular | 12sp | `#EDD9A3` Cream | 200 |
| Stats valor | Noto Sans Bold | 13sp | `#F5E6C8` CreamMuted | 255 |
| Label de barra ("HHP", "MP", "LB") | Noto Sans Regular | 11sp | `#EDD9A3` Cream | 180 |
| Valor numérico barra | Noto Sans Bold | 11sp | `#F5E6C8` CreamMuted | 255 |

### 5.2 Barras de recurso

Altura de barra: 12px (ligeramente más gruesa que en crew chips para jerarquía).
Width: ~640px (panel ancho menos márgenes).

**HHP bar (verde → amarillo → rojo):**

| Umbral | Color fill | Token |
|--------|-----------|-------|
| > 50% CurrentHHP/MaxHHP | `#4CAF50` | HpHigh |
| 25%–50% | `#FFCA28` | HpMid |
| < 25% | `#EF5350` | HpLow |

Transición de color: instantánea al cruzar umbral (sin tween de color — el flash
de daño es suficiente feedback; tweens de color lentos confunden).

Fondo barra HHP: `#1A2A1A` alpha 220 cuando fill es verde/amarillo; `#2A1010`
alpha 220 cuando fill es rojo (el fondo cambia en sincronía con el fill para
reforzar el estado crítico).

**MP bar (azul):**

| Estado | Fill color | Fondo color |
|--------|-----------|-------------|
| Siempre | `#1E88E5` MpBlue | `#0D1A2A` alpha 220 |

**LB bar (binaria):**

| Estado | Fill color | Ancho fill | Indicador adicional |
|--------|-----------|-----------|---------------------|
| LB lista (llena) | `#D4A017` Gold | 100% | Pequeño destello (ver §10.5) |
| LB no lista (vacía) | `#2A2010` LbEmpty | 0% (barra vacía) | Sin indicador |

La barra LB en estado lleno tiene además un catch-light: Image 2px height
`#FFD700` alpha 160, anchored top, sobre el fill dorado. Referencia visual
directa con los botones primarios — "el LB es un recurso valioso."

**Flash de daño en stats (OnShipStatsRecalculated):**
El campo del stat que bajó (FPW, HDF o SPD) parpadea en rojo: el Text Color hace
tween de `#EDD9A3` → `#EF5350` → `#EDD9A3` en 0.4s (ease in-out). Ver §10.3.

---

## Section 6 — Panel de Crew Aliada

### 6.1 Contenedor

| Capa | Valor |
|------|-------|
| Fondo | Image `#3D2810` WoodBase alpha 220 |
| Borde superior | 2px Image `#D4A017` Gold alpha 160 |
| Catch-light | 6px `#8B5E3C` alpha 80, anchored top |
| Label "TRIPULACIÓN" | Noto Sans Regular 12sp `#EDD9A3` alpha 180, uppercase |

### 6.2 Slots de crew (80×80px)

Cada slot es un mini-card con tres estados visuales.

**Estado VIVO:**

| Capa | Valor |
|------|-------|
| Fondo slot | Image `#2A1E10` SlotFilled alpha 240 |
| Borde slot | 1px Image `#5C3D1E` WoodBorder alpha 200 |
| Icono de rol | Image 32×32, ver §8 para especificación de iconos |
| Minibar HP | height 6px, color por umbral (HpHigh/HpMid/HpLow), fondo `#1A1A1A` |
| Label 2 letras | Noto Sans Bold 10sp `#EDD9A3`, centered, bajo minibar |

**Estado MUERTO:**

| Capa | Valor |
|------|-------|
| Fondo slot | Image `#1A1A1A` alpha 220 (ChipDeadOverlay — gris frío) |
| Borde slot | 1px Image `#3A2A50` SlotBorderEmpty alpha 160 |
| Icono de rol | Image 32×32, alpha 40% (tintado gris) |
| Overlay gris | Image `#1A1A1A` alpha 140 sobre todo el slot |
| Icono X | Image 20×20 `#FFFFFF` alpha 220, centered — señal NO-COLOR primaria |
| Label "CAÍDO" | Noto Sans Regular 9sp `#EDD9A3` alpha 180, centered, bajo X |

**Nota accesibilidad:** el slot muerto usa tres señales independientes del color:
overlay (oscuridad), icono X (forma), label "CAÍDO" (texto). El color gris es
refuerzo, no señal primaria. Consistente con UX spec §6.

**Estado CAPITÁN MUERTO (efecto sobre sinergias):**
Los badges de sinergia pasan a: Image `#3D3020` BtnDisabledBg + texto
`#A08040` DisabledLabel + overlay tachado (Image 1px height `#A08040` centrada
sobre el texto del badge). Misma señal de "disabled" que en botones.

### 6.3 Badges de sinergia

| Elemento | Valor |
|----------|-------|
| Container badge | Image `#1F172E` PanelDark, corner 0, height 22px, auto-width |
| Borde badge | 1px `#D4A017` Gold alpha 160 |
| Texto | Noto Sans Regular 11sp `#EDD9A3` |
| Badge activo | borde `#D4A017` — señal "sinergia funciona" |
| Badge inactivo (capitán muerto) | borde `#3D3020`, texto `#A08040` DisabledLabel, overlay tachado |

---

## Section 7 — Iconografía de Estados, DoTs y Buffs

Los iconos de estado son 24×24px en el campo de batalla y 20×20px en el panel
de stats del barco. Cada icono tiene: forma distintiva, color de tint, símbolo
accesible (no depende solo de color).

### 7.1 Tabla completa de iconos de estado

| Estado | Nombre ES | Color tint | Forma / símbolo | ID único visual | Versión primitiva |
|--------|-----------|-----------|-----------------|-----------------|-------------------|
| Quemadura | Quemadura | `#FF6F00` DotBurn | Llama con base triangular | Llama + "Q" 8sp | Image 24×24 naranja brillante, letra "Q" Noto Bold 8sp centrada |
| Veneno | Veneno | `#7CB342` DotPoison | Calavera con huesos cruzados (pirata) | Calavera + "V" | Image 24×24 verde oliva, letra "V" |
| Sangrado | Sangrado | `#C62828` DotBleed | Gota de sangre invertida (forma de lágrima) | Gota + "S" | Image 24×24 rojo oscuro, letra "S" |
| Silencio | Silencio | `#7B1FA2` DotSilence | Boca cerrada con X | Boca-X + "SI" | Image 24×24 violeta oscuro, letras "SI" |
| Ceguera | Ceguera | `#4A4A4A` (gris) | Ojo cerrado con línea diagonal | Ojo-tachado | Image 24×24 gris, letra "C" |
| Maniobra | Maniobra | `#29B6F6` ManeuverBlue | Escudo con flecha curva | Escudo | Image 24×24 azul claro, letra "M" |
| Buff genérico | Buff | `#1565C0` DotBuff | Flecha hacia arriba dentro de círculo | Flecha-up | Image 24×24 azul medio, letra "B" |

**Contador de turnos (siempre junto al icono):**
- TextMeshProUGUI 9sp `#F5E6C8` CreamMuted, posición: sup-derecha del icono, offset (+10, +8)
- Fondo del contador: Image `#1F172E` PanelDark alpha 180, 14×14px, esquinas 0
- El número de turnos restantes es la señal primaria; el icono identifica el tipo.
- Accesibilidad: cuando los turnos = 1, el contador hace flash rojo (tween color
  `#F5E6C8` → `#EF5350` → `#F5E6C8`, 0.5s, una vez). Señal de "próximo a expirar."

### 7.2 Iconos de estado: asset requirements

Ver §12 (Asset Table) para dimensiones, formato y prompts completos.

La prioridad es P0 para los 3 DoTs de combate (Quemadura, Veneno, Sangrado) y
Silencio. Ceguera y Maniobra son P1. La versión primitiva (Image color + letra)
es suficiente para el demo.

---

## Section 8 — Iconografía de los 7 Roles de Crew

Fuente: `Assets/Scripts/Core/Data/NavalRole.cs`. Roles en orden canónico:
`Capitan, Intendente, Artillero, Navegante, Carpintero, Cirujano, Contramaestre`.

Tamaño: 32×32px en slots aliados (80px wide), 20×20px en chips overlay (44px wide).

### 8.1 Forma distintiva por rol

Cada icono debe ser reconocible tanto a 32px como a 20px. La forma es la señal
primaria; el color es refuerzo. Ni colores duplicados ni formas duplicadas.

| Rol | ID 2-letras | Color base | Forma / objeto | Elemento visual clave |
|-----|-------------|-----------|----------------|-----------------------|
| Capitan | CA | `#D4A017` Gold | Tricornio (sombrero) con calavera y cruz | La punta del sombrero es inconfundible |
| Intendente | IN | `#EDD9A3` Cream | Balanza equilibrada | Forma de balanza simétrica |
| Artillero | AR | `#EF5350` rojo | Cañón de barco de perfil | Tubo cilíndrico inclinado |
| Navegante | NA | `#1E88E5` azul | Brújula octogonal (rosa de los vientos) | Forma octogonal con N marcada |
| Carpintero | CP | `#8D6E63` marrón medio | Hacha de carpintero (hatchet) | Filo curvo del hacha |
| Cirujano | CI | `#4CAF50` verde | Cruz médica con calavera vudú superpuesta | Cruz + calavera — dos formas en una |
| Contramaestre | CO | `#FFB300` ámbar | Cuerno de mando / bocina | Forma cónica curvada |

**Nota CA vs CP:** La diferencia entre "CA" (Capitán) y "CA" (Carpintero) es un
problema de ID duplicado en el UX spec. El código usa `Capitan` y `Carpintero`.
Este spec resuelve el conflicto: Carpintero usa ID "CP" (abreviatura más clara).
Comunicar al UX-designer y UI-programmer para sincronizar.

### 8.2 Color de tint por rol (Image component)

Los iconos primitivos son Image blancas tintadas con el color de rol. El arte final
son sprites con colores integrados, pero el tint primitivo es suficiente para demo.

---

## Section 9 — Panel de Acciones (Action Panel)

### 9.1 Contenedor del panel (280px)

| Capa | Valor |
|------|-------|
| Fondo | Image `#1A0D00` HeaderBase alpha 245 |
| Borde superior | 3px Image `#D4A017` Gold alpha 200 |
| Catch-light sobre borde | 2px Image `#FFD700` GoldBright alpha 100, anchored top |

Consistencia: mismo tratamiento que el footer de StageSelect y TeamSelect.

### 9.2 Botones de acción (240×80px)

Reutilizan el sistema de botón dorado de S3-11 con adaptaciones de icono.

**Estado ENABLED (normal):**

| Capa | Valor |
|------|-------|
| Fondo (Image en Button) | `#3D2810` WoodBase alpha 230 |
| Borde | 1px Image `#5C3D1E` WoodBorder alpha 220 |
| Bevel superior | 3px Image `#8B5E3C` WoodCatch alpha 120, anchored top |
| Icono acción | Image 32×32, ver §9.3 |
| Label texto | Pirata One Regular 15sp `#EDD9A3` Cream |
| Label shadow | offset (1, -1) `#000000` alpha 120 |

**Estado HOVER (Highlighted):**

| Cambio | Valor |
|--------|-------|
| Fondo → | `#4A3018` WoodLight alpha 240 |
| Borde → | `#D4A017` Gold alpha 200 |
| Label → | `#F5E6C8` CreamMuted |
| Icono brightness | +20% (multiplicar color por 1.2) |
| Transición | 0.1s fade (Button.ColorBlock fade duration) |

**Estado PRESSED:**

| Cambio | Valor |
|--------|-------|
| Fondo → | `#2A1A08` (más oscuro que WoodBase) |
| Borde → | `#B8880F` GoldDark |
| Scale | `(0.97, 0.97)` instantáneo — springback 0.1s ease-out |

**Estado DISABLED:**

| Cambio | Valor |
|--------|-------|
| Fondo → | `#3D3020` BtnDisabledBg |
| Borde → | `#3D3020` sin contraste |
| Icono alpha | 40% |
| Label → | `#A08040` DisabledLabel alpha 180 (WCAG AA) |
| Razón (sub-label) | Noto Sans Regular 10sp `#A08040` alpha 160, bajo el label principal |

**ColorBlock para Unity Button component:**

| State | Color | Alpha |
|-------|-------|-------|
| Normal | `#3D2810` | 255 |
| Highlighted | `#4A3018` | 255 |
| Pressed | `#2A1A08` | 255 |
| Disabled | `#3D3020` | 255 |
| Color Multiplier | 1.0 | — |
| Fade Duration | 0.1s | — |

**Diferencia con botón principal gold (BtnStartBattle):** los botones de acción
naval usan WoodBase como color base en lugar de Gold — jerarquía correcta. El Gold
solo aparece en bordes y en la barra LB, reservando su asociación semántica de
"acción primaria de navegación" para los botones de pantalla, no para las 6
acciones que se repiten cada turno.

### 9.3 Iconos de acción (32×32px cada uno)

| Acción | Icono (forma) | Color base | Versión primitiva | Nota visual |
|--------|---------------|-----------|-------------------|-------------|
| Cañonazo | Cañón disparando (destello en boca) | `#EF5350` rojo (fuego) | Image 32×32 roja, letra "C" Bold | Único icono con rojo puro = peligro/ataque |
| Habilidad Naval | Estrella de 6 puntas con espiral vudú | `#D4A017` Gold | Image 32×32 dorada, letra "H" | Dorado = habilidad especial, alineado con LB |
| Maniobra Evasiva | Barco con trayectoria curva + flecha | `#29B6F6` azul cielo | Image 32×32 azul, letra "E" | Azul = defensa/movimiento |
| Abordaje | Garfio con cuerda | `#D4A017` Gold (enabled) / `#3D3020` disabled | Image 32×32 dorada, letra "A" | Con X roja superpuesta en disabled |
| Reparar | Martillo con estrella de curación | `#4CAF50` verde | Image 32×32 verde, letra "R" | Verde = curación, consistente con HpHigh |
| Pasar Turno | Reloj de arena / flecha circular | `#EDD9A3` Cream alpha 160 | Image 32×32 crema semitransparente, letra "P" | Alpha reducido = acción pasiva/secundaria |

**Pasar Turno es visualmente más pequeño:** el botón Pasar usa icono 24×24
(en lugar de 32×32) y label en Noto Sans Regular (en lugar de Pirata One) —
el UX spec indica que es "visualmente más pequeño / menos prominente."

**Badge de Silencio en Habilidad Naval:**
Cuando `HasStatus(Silencio)`: añadir child Image 16×16 de icono boca-X
(`ui_status_silence_16.png` o primitiva `#7B1FA2` con "SI") en esquina sup-derecha
del botón de Habilidad Naval, sobre el icono de estrella.

**Badge de advertencia Ceguera:**
Cuando `HasStatus(Ceguera)`: añadir "!" Image 16×16 `#FFCA28` HpMid en
esquina sup-derecha de los botones Cañonazo y Abordaje. Tooltip: "Ceguera: 50% de
fallo posible."

**Badge MANIOBRA ACTIVA:**
Cuando `IsManeuvering == true`: Image pill 48×16px `#29B6F6` alpha 200, texto
"ACTIVA" Noto Bold 8sp `#FFFFFF`, posicionado en esquina inf-derecha del botón
Maniobra Evasiva. No bloquea el botón (el resolver lo resetea).

**Feedback de MP insuficiente (Reparar):**
Cuando MP < 20 y el jugador intenta click sobre Reparar disabled: la barra de MP
del panel de stats hace un pulso (scale 1→1.04→1, 0.3s ease in-out + alpha
`#1E88E5` → `#29B6F6` → `#1E88E5`). El botón de Reparar tiene siempre visible
"20 MP" como sub-label (Noto Regular 10sp `#A08040` alpha 160 en estado disabled).

---

## Section 10 — Selector de Habilidades Navales (AbilitySelect Panel)

Reemplaza el Action Panel. Mismo contenedor visual.

### 10.1 Fondo y estructura

Mismo fondo que Action Panel: `#1A0D00` con borde dorado superior 3px.
La transición entre ActionPanel → AbilityPanel es un crossfade de 0.15s.

### 10.2 Cards de habilidad (900×60px)

| Capa | Versión primitiva | Versión final |
|------|------------------|---------------|
| Fondo card | `#3D2810` WoodBase alpha 220 | `ui_ability_card_9slice.png` (P1) |
| Borde card | 1px `#5C3D1E` WoodBorder | — |
| Hover state | `#4A3018` WoodLight alpha 230 + borde `#D4A017` | |
| Fondo disabled | `#3D3020` BtnDisabledBg alpha 200 | |

**Layout de cada card (izquierda a derecha):**
1. Badge LB (si aplica): Image 32×16px `#D4A017` Gold, texto "LB" Pirata One 10sp
   `#1A0D00` HeaderBase — consistente con Gold = recurso especial.
2. Nombre habilidad: Pirata One 15sp `#F5E6C8` CreamMuted (enabled) /
   `#A08040` DisabledLabel (disabled).
3. Elemento: Image 18×18 del elemento (ver §11).
4. Costo MP: Noto Sans Bold 12sp `#1E88E5` MpBlue con símbolo "MP" — color azul
   establece inmediatamente la asociación costo/recurso.
5. Badge tipo target: Image pill pequeño (36×14px), texto 9sp:
   - "AoE": fill `#6A1B9A` VoodooViolet alpha 180, texto `#F5E6C8`
   - "Único": fill `#1A0D00` alpha 180, borde `#5C3D1E`, texto `#EDD9A3`
   - "Crew": fill `#BF360C` TempleEmber alpha 160, texto `#F5E6C8`
   - "Propio": fill `#1565C0` DotBuff alpha 160, texto `#F5E6C8`
6. Badge cooldown (si activo): Image pill `#2A1020` alpha 200, texto "[CD: X]"
   Noto Bold 10sp `#EF5350` HpLow.
7. Descripción corta: Noto Sans Regular 11sp `#EDD9A3` alpha 180, truncada con
   ellipsis si > 1 línea.

**Banner de Silencio (top del panel):**
Cuando Silencio activo: Image `#7B1FA2` DotSilence alpha 230, height 32px,
stretch horizontal. Texto "SILENCIO ACTIVO — Habilidades bloqueadas" Noto Sans
Bold 13sp `#F5E6C8` CreamMuted, con icono boca-X 20×20 a la izquierda. NO solo
oscurece los botones.

### 10.3 Botón "Volver"

| Propiedad | Valor |
|-----------|-------|
| SizeDelta | fill width, height 44px |
| Fondo | Image `#1A0D00` HeaderBase alpha 200 |
| Borde superior | 1px `#D4A017` Gold alpha 120 |
| Label "< VOLVER" | Noto Sans Regular 15sp `#EDD9A3` Cream alpha 200 |
| Hover | Label → `#FFD700` GoldBright |
| Comportamiento | Misma spec que BtnBack de screens anteriores — texto sin fondo sólido de color |

---

## Section 11 — Chips Overlay de Crew Enemiga

Los chips (44×56px) son el elemento más específico de la pantalla naval.
Deben funcionar sobre fondos variados (sprite de barco enemigo) y ser legibles
a tamaño pequeño.

### 11.1 Chip vivo — versión primitiva

| Capa | Objeto | Valor |
|------|--------|-------|
| Fondo chip | Image | `#1F172E` PanelDark alpha 230 |
| Borde chip | Image inset 1px | `#5C3D1E` WoodBorder alpha 200 |
| Icono rol | Image 20×20 | Color por rol (ver §8), alpha 220 |
| Minibar HP bg | Image 36×6px | `#1A1A1A` alpha 200 |
| Minibar HP fill | Image 36×6px | HpHigh/HpMid/HpLow según umbral |
| Label 2 letras | TMP 10sp Noto Bold `#EDD9A3` | centered bajo bar |

### 11.2 Chip muerto — versión primitiva

| Capa | Objeto | Valor |
|------|--------|-------|
| Fondo chip | Image | `#1A1A1A` ChipDeadOverlay alpha 220 |
| Borde chip | Image inset 1px | `#3A2A50` SlotBorderEmpty alpha 120 |
| Icono rol | Image 20×20 | alpha 40% (desaturado por tint gris `#3A3A3A`) |
| Overlay gris | Image fill chip | `#1A1A1A` alpha 100 |
| Icono X | Image 16×16 `#FFFFFF` | centered sobre chip — SEÑAL PRIMARIA |
| Label "CAÍDO" | TMP 9sp Noto Regular `#EDD9A3` alpha 180 | centered bajo X |

### 11.3 Modo Abordaje vs Modo Inspección

| Estado del chip | Abordaje (targeting) | Inspección (hover read-only) |
|-----------------|---------------------|------------------------------|
| Borde chip hover (vivo) | 2px `#4CAF50` TargetGreen | 1px `#888888` gris (cursor informa no-acción) |
| Hover scale | 1.15× en 0.08s | Sin scale |
| Cursor | `Pointer` (mano) | `Default` (flecha) |
| Borde chip muerto | Sin cambio en hover | Sin cambio en hover |
| Chip clickeable | Sí (vivos) | No |

### 11.4 Animación de aparición de chips

**Modo Abordaje:** los chips aparecen con stagger secuencial:
- Chip 0: fade-in + translate Y+8px→0 en 0.12s ease-out
- Chip 1: misma animación con delay 0.04s
- Chip 2: delay 0.08s
- ... cada chip añade 0.04s de delay.
- El barco objetivo hace zoom-in simultáneo (ver §13.2).

**Modo Inspección (hover):** todos los chips aparecen juntos con un
fade-in simple de 0.1s — más rápido, más sutil, no hay zoom del barco.

**Desaparición:** fade-out 0.1s en ambos modos.

### 11.5 Líneas de anclaje (abanico semicircular — fallback)

Si el abanico se activa (sprites muy pequeños tras zoom):
- Líneas: Image 1px, color `#D4A017` Gold alpha 100, origin en sprite barco,
  destination en centro del chip. Aparecen con los chips (fade-in simultáneo).
- No hay asset adicional: las líneas son Image components rotadas/escaladas por script.

---

## Section 12 — Hint Bar y Battle Log

### 12.1 Hint Bar (44px)

Aparece entre el campo de batalla y el panel de crew cuando hay un estado de
targeting activo.

| Capa | Valor |
|------|-------|
| Fondo | Image `#0D0A14` HintBarBg alpha 230 |
| Borde superior | 1px `#D4A017` Gold alpha 100 |
| Borde inferior | 1px `#D4A017` Gold alpha 100 |
| Texto | Noto Sans Regular 13sp `#EDD9A3` Cream alpha 220, centered |
| [ESC] indicator | Noto Sans Bold 12sp `#F5E6C8` con fondo pill `#3D3020` 4px padding |

### 12.2 Battle Log (120px, scroll)

| Capa | Valor |
|------|-------|
| Fondo | Image `#0D0A14` HintBarBg alpha 210 |
| Borde superior | 2px `#D4A017` Gold alpha 140 |
| Texto log | Noto Sans Regular 12sp `#EDD9A3` alpha 180 |
| Texto daño recibido | Noto Sans Regular 12sp `#EF5350` HpLow alpha 200 |
| Texto daño causado | Noto Sans Regular 12sp `#4CAF50` HpHigh alpha 200 |
| Texto curación | Noto Sans Regular 12sp `#4CAF50` HpHigh alpha 200 |
| Texto muerte crew | Noto Sans Bold 12sp `#EF5350` HpLow alpha 255 |
| Auto-scroll | al último entry, instantáneo |

**Números flotantes sobre sprites (OnDamageDealt):**
- Size: Noto Sans ExtraBold (o Bold) 22sp
- Damage al barco: `#EF5350` HpLow, outline negro 1px
- Damage al crew: `#EF5350` HpLow + icono de rol del crew dañado 16×16 a la derecha
- Curación: `#4CAF50` HpHigh con "+" prefijo
- Miss: `#EDD9A3` Cream alpha 140, texto "FALLO"
- Animación: translate Y−40px en 0.8s ease-out + fade-out últimos 0.3s

---

## Section 13 — Overlays de Estado de Combate

### 13.1 WaveTransition Overlay

| Capa | Valor |
|------|-------|
| Bg overlay | Image `#140F24` BgDark alpha 220 (no opaco total — barcos visibles al fondo) |
| Texto "OLEADA COMPLETADA" | Pirata One 36sp `#D4A017` Gold, TMP Drop Shadow (2,-2) negro a160 |
| Texto "Nueva oleada en..." | Noto Sans Italic 16sp `#EDD9A3` Cream alpha 180 |
| Countdown [3][2][1] | Pirata One 48sp `#FFD700` GoldBright, scale pulse cada segundo |
| Reminder box | Image `#1F172E` PanelDark alpha 200, border `#5C3D1E`, texto Noto 13sp Cream |

Animación de entrada: fade-in overlay 0.3s + slide-up texto desde y−20, 0.4s ease-out.
Animación de salida: fade-out overlay 0.5s.

### 13.2 BattleOver — Victoria

| Capa | Valor |
|------|-------|
| Bg overlay | Image `#140F24` BgDark alpha 235 |
| Texto "¡VICTORIA!" | Pirata One 48sp `#D4A017` Gold + TMP Glow (optional P1) |
| Flavor text | Pirata One 18sp `#EDD9A3` Cream |
| Stats box | Panel madera (`#3D2810` WoodBase, borde `#D4A017` Gold) |
| Stats texto | Noto Sans Regular 14sp `#EDD9A3` Cream |
| Stats valores | Noto Sans Bold 14sp `#F5E6C8` CreamMuted |
| BtnContinuar | Spec idéntica a BtnStartBattle S3-11: Gold 540×88px, Pirata One 24sp |

### 13.3 BattleOver — Derrota

| Capa | Valor |
|------|-------|
| Bg overlay | Image `#0D0A14` HintBarBg alpha 245 (más oscuro que victoria) |
| Overlay gris encima | Image `#1A1A1A` alpha 60 (aplana colores — todo más frío) |
| Texto "DERROTA" | Pirata One 48sp `#EF5350` HpLow rojo |
| Flavor text "El barco se ha hundido" | Noto Sans Italic 16sp `#EDD9A3` alpha 160 |
| BtnReintentar | Gold spec estándar 400×80px |
| BtnSalir | Wood spec (fondo `#3D2810`, borde `#5C3D1E`, label Cream), 400×80px |

**Señal DERROTA vs VICTORIA sin depender de color:** el overlay oscuro (frío vs
cálido) + el tamaño del texto (idéntico) permiten leerlos con baja visión del
color. Las dos pantallas también tienen copies distintas y botones distintos.

---

## Section 14 — Animaciones y Feedback Visual

### 14.1 Hover sobre botón de acción

- Duración: 0.1s (Unity Button.ColorBlock `fadeDuration`)
- Easing: lineal (Unity default — no hay tween personalizado)
- Cambio: WoodBase → WoodLight, borde → Gold (via ColorBlock)
- Icono: brightness +20% (multiplicar `Image.color` por `new Color(1.2, 1.2, 1.2)`)

### 14.2 Pressed sobre botón de acción

- Duración: instantáneo en entrada, 0.1s ease-out en salida (springback)
- Scale: `(0.97, 0.97)` via `AnimationTrigger` o DOTween shortcut
- Color: per ColorBlock Pressed state
- Recomendación: usar `EventTrigger.PointerDown/Up` para el scale — Unity Button
  ColorBlock no controla scale.

### 14.3 Flash de daño en barras HHP

Evento: `OnDamageDealt` cuando target es `ShipCombatant`.

- El fill de la barra HHP parpadea: tween de color fill → `#FFFFFF` alpha 200 →
  color correcto (HpHigh/HpMid/HpLow según el nuevo valor), duración 0.25s ease in-out.
- El fondo de la barra hace pulso alpha: alpha 220 → 255 → 220, 0.25s.
- No se usa flash de la pantalla completa — solo la barra afectada.

### 14.4 Flash de curación en barras HHP

Evento: `OnHealApplied` → `SubmitRepair`.

- Fill: tween actual → `#4CAF50` HpHigh (aunque el umbral no haya cambiado) →
  color correcto, 0.3s.
- Número flotante verde "+X" sobre el sprite del barco aliado.

### 14.5 Destello de LB listo

Cuando la barra LB transiciona de vacía a llena (turno nuevo, `LBUsedThisRound`
vuelve a false):

- La barra LB hace un sweep de izquierda a derecha: un Image blanco alpha 180 de
  width 8px que se traslada de x=0 a x=barra_width en 0.4s ease-out.
- El borde de la barra (1px `#FFD700`) hace flash: alpha 255 → 80 → 255 en 0.5s.
- No hay sonido en esta spec (fuera de scope art-director).

### 14.6 Zoom-in de barco para Abordaje

Definido en UX spec como "escala ×1.3, animación 0.15s ease-out."

- **Easing:** ease-out cúbico (`AnimationCurve` evaluado: in=0, out=1 con tangente
  out suave). En DOTween: `Ease.OutCubic`.
- **Punto de pivote:** el barco escala desde su centro (pivot 0.5, 0.5). Si el barco
  está en la mitad derecha de la pantalla, el zoom puede empujar el sprite hacia el
  centro — esto es deseable: el barco objetivo se "acerca" al jugador.
- **Re-posición:** si tras el zoom el sprite cruza el borde del área de batalla
  (1080px), el sprite se reposiciona (translate X) para que quede contenido.
  Duración del translate: igual que el scale (0.15s, misma curva).
- **Acompañamiento visual:**
  - Barco aliado: alpha tween a 60% en 0.15s (sale del foco).
  - Otros enemigos (si >1): alpha tween a 40% en 0.15s.
  - Borde del barco objetivo: aparece `#FFD700` GoldBright 3px pulsante
    simultáneamente con el inicio del zoom.
- **Reverso (cancel/confirm):** scale ×1.3→×1.0 en 0.15s ease-in-cubic.
  Alphas de otros elementos vuelven a 100% en paralelo.

### 14.7 Aparición de chips de crew (Abordaje)

Ver §11.4 — stagger 0.04s por chip, fade-in + translate Y+8→0, 0.12s ease-out.

### 14.8 Muerte de crew (chip overlay → chip muerto)

Evento: `OnCrewDied`.

1. El chip vivo hace scale 1→1.2 en 0.1s (impacto).
2. Scale 1.2→1 en 0.1s con tween hacia el estado visual de muerto (fade overlay gris,
   swap icono X).
3. El label cambia de "2-letras" a "CAÍDO" durante el step 2.
4. Duración total: 0.2s.

### 14.9 Pulsación de borde de targeting

Bordes verdes (TargetGreen) y dorados (GoldBright) que "pulsean" en modo targeting:

- Ciclo: alpha 220 → 80 → 220, periodo 1.2s, loop infinito, ease sin.
- Implementación: animar `Image.color.a` del objeto borde (no del sprite del barco).
- Se detiene inmediatamente al salir del estado de targeting.

---

## Section 15 — Consistencia con S3-11: Qué Reutilizar Tal Cual

Esta sección es la referencia para el UI-programmer sobre qué no re-inventar.

| Elemento | Spec de origen | Reutilizar exactamente |
|----------|---------------|------------------------|
| Fondo `#140F24` + degradado 2×2 | S3-11 §2.2 | Sí — mismos objetos (BgBase + BgGradient) |
| Header `#1A0D00` + borde dorado 4px | S3-11 §4.3 | Sí — misma Height 180px, mismos valores |
| Footer `#1A0D00` + borde dorado | S3-11 §4.6 | Sí para el Action Panel (borde superior 3px) |
| Botón gold (BtnStartBattle style) | S3-11 §2.7 | Reutilizar para BtnContinuar/BtnReintentar en overlays de resultado |
| Back button text-only (Cream) | S3-11 §3.2 | Reutilizar para BtnVolver en AbilityPanel |
| Slots madera filled (SlotFilled/SlotBorderFilled) | S3-11 §4.4 | Reutilizar para slots de crew aliada viva |
| Slots vacíos/muertos (SlotEmpty/SlotBorderEmpty) | S3-11 §4.4 | Reutilizar para slots de crew muerta |
| DisabledLabel `#A08040` alpha 180 | S3-11 §1.2 | Sí — todos los disabled labels de acciones |
| Fuente Pirata One / Noto Sans (assets .asset) | S3-11 §5.2 | Sí — mismos .asset files |
| ColorBlock del botón gold | S3-11 §2.7 | Reutilizar para BtnContinuar/BtnReintentar |
| Borde de targeting (Outline 3px, effectDistance 3,3) | UX spec §6 | Sí — misma configuración |
| AccentStripe técnica 8px | S3-11 §3.6 / stageselect §10 | Reutilizar para accent stripe de chip (color por rol) |

**Qué NO reutilizar sin modificación:**
- El botón dorado puro `#D4A017` como fondo de los 6 botones de acción — aquí
  se usa WoodBase para evitar que 6 botones dorados compitan con el Gold semántico.
- La card de StageSelect (900×340px) — las cards de habilidad son 900×60px.

---

## Section 16 — Iconos de Elementos Navales

Los barcos tienen un elemento (`ShipData.Element`). Se mostrará como icono 18×18px
en el panel de stats y en la card del barco enemigo.

| Elemento | Español | Color | Forma primitiva | Nota temática |
|----------|---------|-------|-----------------|---------------|
| Fire | Fuego | `#BF360C` TempleEmber | Image 18×18 roja, llama | Cañones de pólvora, vudú de fuego |
| Water | Agua | `#1E88E5` CorsairBlue | Image 18×18 azul, ola | Mar, tormentas |
| Thunder | Trueno | `#E8B420` GoldMid | Image 18×18 ámbar, rayo | Tormentas eléctricas |
| (None/Neutral) | Neutro | `#EDD9A3` Cream alpha 180 | Image 18×18 crema, estrella | Sin afinidad elemental |

Mismos colores que los acentos de stage (`ui-stageselect-visual-direction.md §7`)
— consistencia temática: Bahía Corsaria usa Corsair Blue (agua), Templo Vudú usa
Temple Ember (fuego).

---

## Section 17 — Asset Requirements Table

Nombre de archivo siguiendo la convención: `[category]_[name]_[variant]_[size].[ext]`

### P0 — Bloqueantes para demo funcional con arte

| Nombre de archivo | Dimensiones | Formato | Descripción funcional | Prompt IA sugerido | Prioridad |
|-------------------|-------------|---------|----------------------|-------------------|-----------|
| `ui_ib_ship_allied_44.png` | 44×44 | PNG, transparente | Icono barco aliado en Initiative Bar | "pirate galleon ship silhouette icon, facing right, blue highlight, clean minimal, game UI icon style, transparent bg, 44x44" | P0 |
| `ui_ib_ship_enemy_44.png` | 44×44 | PNG, transparente | Icono barco enemigo en Initiative Bar | "pirate galleon ship silhouette icon, facing left, red-orange highlight, menacing, clean minimal, game UI icon style, transparent bg, 44x44" | P0 |
| `ui_ib_creature_44.png` | 44×44 | PNG, transparente | Icono criatura marina en Initiative Bar | "sea creature tentacle kraken icon, purple highlight, minimal silhouette, game UI icon style, transparent bg, 44x44" | P0 |
| `ui_role_capitan_32.png` | 32×32 | PNG, transparente | Icono Capitán (tricornio con calavera) | "pirate captain hat tricorn icon, skull emblem, gold color, clean pixel art, game UI icon, transparent bg, 32x32" | P0 |
| `ui_role_intendente_32.png` | 32×32 | PNG, transparente | Icono Intendente (balanza) | "balance scale icon, cream colored, minimal pirate theme, game UI icon, transparent bg, 32x32" | P0 |
| `ui_role_artillero_32.png` | 32×32 | PNG, transparente | Icono Artillero (cañón) | "cannon side view icon, red highlight, pirate ship cannon, minimal, game UI icon, transparent bg, 32x32" | P0 |
| `ui_role_navegante_32.png` | 32×32 | PNG, transparente | Icono Navegante (brújula) | "compass rose icon, blue highlight, nautical, minimal, game UI icon, transparent bg, 32x32" | P0 |
| `ui_role_carpintero_32.png` | 32×32 | PNG, transparente | Icono Carpintero (hacha de mano) | "hatchet axe icon, brown highlight, carpenter tool, minimal, game UI icon, transparent bg, 32x32" | P0 |
| `ui_role_cirujano_32.png` | 32×32 | PNG, transparente | Icono Cirujano (cruz con calavera vudú) | "medical cross with voodoo skull icon, green highlight, dark mystical, minimal, game UI icon, transparent bg, 32x32" | P0 |
| `ui_role_contramaestre_32.png` | 32×32 | PNG, transparente | Icono Contramaestre (cuerno de mando) | "speaking trumpet boatswain horn icon, amber highlight, nautical command, minimal, game UI icon, transparent bg, 32x32" | P0 |
| `ui_dot_burn_24.png` | 24×24 | PNG, transparente | Icono DoT Quemadura | "flame fire icon, orange glow, status effect, game UI icon, transparent bg, 24x24" | P0 |
| `ui_dot_poison_24.png` | 24×24 | PNG, transparente | Icono DoT Veneno | "skull crossbones icon, green tint, poison status, pirate style, game UI icon, transparent bg, 24x24" | P0 |
| `ui_dot_bleed_24.png` | 24×24 | PNG, transparente | Icono DoT Sangrado | "blood drop teardrop icon, dark red, bleed status effect, minimal, game UI icon, transparent bg, 24x24" | P0 |
| `ui_dot_silence_24.png` | 24×24 | PNG, transparente | Icono Silencio | "mouth closed with X mark icon, purple tint, silence debuff, minimal, game UI icon, transparent bg, 24x24" | P0 |
| `ui_action_cannon_32.png` | 32×32 | PNG, transparente | Icono acción Cañonazo | "cannon firing muzzle flash icon, red orange, action button, minimal, game UI icon, transparent bg, 32x32" | P0 |
| `ui_action_ability_32.png` | 32×32 | PNG, transparente | Icono acción Habilidad Naval | "six point star with spiral voodoo pattern, gold color, ability button, minimal, game UI icon, transparent bg, 32x32" | P0 |
| `ui_action_maneuver_32.png` | 32×32 | PNG, transparente | Icono acción Maniobra Evasiva | "ship with curved trajectory arrow, light blue, evasion action, minimal, game UI icon, transparent bg, 32x32" | P0 |
| `ui_action_boarding_32.png` | 32×32 | PNG, transparente | Icono acción Abordaje (enabled) | "grappling hook with rope, gold color, pirate boarding action, minimal, game UI icon, transparent bg, 32x32" | P0 |
| `ui_action_repair_32.png` | 32×32 | PNG, transparente | Icono acción Reparar | "hammer with healing star, green highlight, repair action, minimal, game UI icon, transparent bg, 32x32" | P0 |
| `ui_action_pass_32.png` | 32×32 | PNG, transparente | Icono acción Pasar Turno | "hourglass with circular arrow, cream muted color, pass action, minimal, game UI icon, transparent bg, 32x32" | P0 |

### P1 — Polish (no bloqueante para demo)

| Nombre de archivo | Dimensiones | Formato | Descripción funcional | Prompt IA sugerido | Prioridad |
|-------------------|-------------|---------|----------------------|-------------------|-----------|
| `env_naval_sea_bg.png` | 1080×640 | PNG, sin transparencia | Fondo del campo de batalla naval | "dark ocean sea at night, storm clouds, voodoo mystical atmosphere, pirate ships implied horizon, deep purple-blue color palette, game background art, no characters, photorealistic digital painting" | P1 |
| `env_naval_sky_gradient.png` | 2×2 | PNG | Degradado cielo nocturno (reemplaza versión primitiva BgSolid) | Procedural — fila top `#1A0D2E`, fila bot `#0A1A2A` | P1 |
| `ui_ship_allied_combat.png` | 320×240 | PNG, transparente | Sprite barco aliado en campo de batalla | "pirate galleon ship side view, left facing, worn sails, dark wood hull, gold trim, ocean combat, painterly, 2D game sprite, transparent bg, blue lit" | P1 |
| `ui_ship_enemy_a_combat.png` | 280×200 | PNG, transparente | Sprite barco enemigo tipo A | "enemy pirate ship side view, right facing, battle-damaged, red/black sails, menacing, painterly, 2D game sprite, transparent bg, red lit" | P1 |
| `ui_creature_seabeast_combat.png` | 280×200 | PNG, transparente | Sprite criatura marina tipo base | "sea beast kraken tentacles rising from ocean, purple voodoo glow, dark and menacing, 2D game sprite, painterly, transparent bg" | P1 |
| `ui_status_maneuver_shield_40.png` | 40×40 | PNG, transparente | Icono escudo de Maniobra | "shield with wave pattern, light blue, defensive aura, minimal, game UI icon, transparent bg, 40x40" | P1 |
| `ui_dot_blind_24.png` | 24×24 | PNG, transparente | Icono Ceguera | "closed eye with diagonal line through it, gray, blindness debuff, minimal, game UI icon, transparent bg, 24x24" | P1 |
| `ui_dot_buff_24.png` | 24×24 | PNG, transparente | Icono Buff genérico | "upward arrow inside circle, blue, positive buff status, minimal, game UI icon, transparent bg, 24x24" | P1 |
| `ui_elem_fire_18.png` | 18×18 | PNG, transparente | Elemento Fuego (inline) | "small flame icon, orange-red, element symbol, minimal, game UI, transparent bg, 18x18" | P1 |
| `ui_elem_water_18.png` | 18×18 | PNG, transparente | Elemento Agua (inline) | "small wave droplet icon, blue, element symbol, minimal, game UI, transparent bg, 18x18" | P1 |
| `ui_elem_thunder_18.png` | 18×18 | PNG, transparente | Elemento Trueno (inline) | "small lightning bolt icon, amber yellow, element symbol, minimal, game UI, transparent bg, 18x18" | P1 |
| `ui_panel_enemycard_9slice.png` | 32×32 | PNG | 9-slice panel card bordes redondeados | Procedural — borde 2px `#5C3D1E`, fill transparent, corner radius 4px | P1 |
| `ui_ability_card_9slice.png` | 32×32 | PNG | 9-slice para cards de habilidad | Procedural — borde 1px `#5C3D1E`, fill `#3D2810` alpha 220, esquinas 0 | P1 |
| `ui_boss_banner_bg.png` | 1080×48 | PNG | Fondo texturado para banner de jefe | "dark purple voodoo banner texture, ritual mystical, horizontal strip, no text, game UI" | P1 |

---

## Section 18 — Tipografía Completa de la Pantalla

Reutiliza los TMP assets existentes de S3-11 sin modificación.

| Elemento | Font | Size | Color (token) | Hex | Alpha |
|----------|------|------|---------------|-----|-------|
| Nombre del barco | Pirata One Regular | 18sp | Gold | `#D4A017` | 255 |
| Títulos de panel ("TRIPULACIÓN", "ACCIONES") | Noto Sans Regular | 12sp | Cream | `#EDD9A3` | 180 |
| Texto botón de acción (label principal) | Pirata One Regular | 15sp | Cream | `#EDD9A3` | 255 |
| Sub-label razón disabled | Noto Sans Regular | 10sp | DisabledLabel | `#A08040` | 160 |
| Nombre habilidad (enabled) | Pirata One Regular | 15sp | CreamMuted | `#F5E6C8` | 255 |
| Nombre habilidad (disabled) | Noto Sans Regular | 15sp | DisabledLabel | `#A08040` | 200 |
| Costo MP en habilidad | Noto Sans Bold | 12sp | MpBlue | `#1E88E5` | 255 |
| Stats del barco (labels) | Noto Sans Regular | 12sp | Cream | `#EDD9A3` | 200 |
| Stats del barco (valores) | Noto Sans Bold | 13sp | CreamMuted | `#F5E6C8` | 255 |
| Valores de barra (HHP "2800/3200") | Noto Sans Bold | 11sp | CreamMuted | `#F5E6C8` | 255 |
| Labels de barra ("HHP", "MP", "LB") | Noto Sans Regular | 11sp | Cream | `#EDD9A3` | 180 |
| Label 2-letras en chip crew | Noto Sans Bold | 10sp | Cream | `#EDD9A3` | 220 |
| Label "CAÍDO" en chip muerto | Noto Sans Regular | 9sp | Cream | `#EDD9A3` | 180 |
| Battle Log (texto general) | Noto Sans Regular | 12sp | Cream | `#EDD9A3` | 180 |
| Battle Log (daño recibido) | Noto Sans Regular | 12sp | HpLow | `#EF5350` | 200 |
| Battle Log (daño causado) | Noto Sans Regular | 12sp | HpHigh | `#4CAF50` | 200 |
| Hint Bar | Noto Sans Regular | 13sp | Cream | `#EDD9A3` | 220 |
| Wave Label | Pirata One Regular | 14sp | Cream | `#EDD9A3` | 220 |
| Boss Banner | Pirata One Regular | 18sp | GoldBright | `#FFD700` | 255 |
| Texto overlay Victoria | Pirata One Regular | 48sp | Gold | `#D4A017` | 255 |
| Texto overlay Derrota | Pirata One Regular | 48sp | HpLow | `#EF5350` | 255 |
| Número flotante daño | Noto Sans Bold | 22sp | HpLow | `#EF5350` | 255 |
| Número flotante curación | Noto Sans Bold | 22sp | HpHigh | `#4CAF50` | 255 |
| Número flotante FALLO | Noto Sans Regular | 16sp | Cream | `#EDD9A3` | 140 |
| Tooltip (título) | Noto Sans Bold | 13sp | CreamMuted | `#F5E6C8` | 255 |
| Tooltip (contenido) | Noto Sans Regular | 12sp | Cream | `#EDD9A3` | 200 |

**Regla de tipografía:** Pirata One = nombres propios, acciones con personalidad,
títulos de pantalla. Noto Sans = datos, stats, tooltips, logs — todo lo funcional.
Esta dicotomía está establecida desde S3-11 y se mantiene aquí sin excepción.

---

## Section 19 — Tooltips

Los tooltips aparecen en hover (chip de crew, barco enemigo, botón de acción
disabled). Diseño consistente en todos los casos.

| Capa | Valor |
|------|-------|
| Fondo | Image `#1F172E` PanelDark alpha 235 |
| Borde | 1px `#D4A017` Gold alpha 160 |
| Bevel superior | 2px `#8B5E3C` WoodCatch alpha 80 |
| Sombra drop shadow del panel | Shadow `#050D14` alpha 100, offset (3, -3) |
| Padding interior | 8px todos los lados |
| Max width | 280px |
| Título | Noto Sans Bold 13sp `#F5E6C8` CreamMuted |
| Contenido | Noto Sans Regular 12sp `#EDD9A3` Cream alpha 200 |
| Separador título/contenido | 1px Image `#D4A017` Gold alpha 100 |

**Posicionamiento:** aparece adyacente al elemento hover, evitando bordes de
pantalla (lógica de clamp). Delay de aparición: 0.3s de hover antes de mostrar.
Fade-in: 0.1s. Fade-out: inmediato al salir del hover.

---

## Section 20 — Notas de Implementación Prioritarias

Estas notas no son código — son restricciones y decisiones que el art-director
comunica al UI-programmer antes de construir.

1. **ID de Carpintero "CP":** El UX spec usa "CA" para Carpintero, duplicando el
   Capitán. Este spec define "CP" como ID de 2 letras canónico para Carpintero.
   Actualizar la tabla de mapeo `NavalRole → string` en el código UI (no en
   `NavalRole.cs` — el enum no cambia).

2. **Chips y el WoodBase:** Los chips overlay van sobre sprites de barco cuyo fondo
   es desconocido (arte IA variable). El fondo `#1F172E` PanelDark con border
   `#5C3D1E` crea un "marco de panel" que aísla visualmente el chip del fondo.
   No usar fondo transparente en chips — siempre fondo sólido.

3. **Barra HHP: MaxHHP fijo:** Según UX spec §Nota preliminar, `MaxHHP` no cambia
   cuando muere crew. La barra usa `CurrentHHP / MaxHHP` donde MaxHHP es fijo en
   construcción. No animar el máximo de la barra.

4. **Zoom del barco y RectTransform:** El zoom-in ×1.3 del Abordaje se implementa
   como `transform.localScale` en el Image del sprite, NO como `sizeDelta`.
   Cambiar `sizeDelta` rompería el layout. El punto de pivote del RectTransform
   del sprite debe ser (0.5, 0.5) para que el zoom sea desde el centro.

5. **AbilityPanel transition:** La transición ActionPanel → AbilityPanel es un
   crossfade (`CanvasGroup.alpha` 1→0 en ActionPanel mientras 0→1 en AbilityPanel),
   0.15s, lineal. No destruir ni instanciar — solo activar/desactivar los
   CanvasGroups y controlar `interactable` en el CanvasGroup inactivo.

6. **Tooltips sobre chips en Abordaje:** Los tooltips de chip incluyen "DEF" del
   crew member (`CharacterData.BaseStats[(int)StatType.DEF]`) solo en modo Abordaje.
   En modo Inspección no se muestra DEF — simplificar el tooltip.

7. **Números flotantes:** usar un pool de TMP labels para los floating numbers
   (pool size 10–15 suficiente para el demo). No instanciar en tiempo de combate.
