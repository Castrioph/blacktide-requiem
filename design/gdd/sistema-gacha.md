# Sistema Gacha

> **Status**: Approved
> **Author**: user + agents
> **Last Updated**: 2026-04-01 (TIE/TIF integration, standard banner pity)
> **Implements Pillar**: Pillar 3 (Recompensa a la Paciencia), Pillar 2 (Personajes con Alma)

## Overview

El Sistema Gacha es el mecanismo principal de adquisición de unidades. El jugador gasta Gemas de Calavera (GDC, la moneda premium) para realizar invocaciones que producen unidades aleatorias de distintas rarezas (3★, 4★, 5★). El sistema ofrece dos tipos de banners: un **banner permanente** (pool estándar con todas las unidades) y un **banner destacado** (rate-up temporal para 1-2 unidades específicas). Para proteger al jugador contra la mala suerte, el sistema implementa un modelo de pity dual: **soft pity** (la probabilidad de obtener un 5★ aumenta gradualmente después de cierto número de pulls sin obtenerlo) y **hard pity** (garantía absoluta de un 5★ tras un máximo de pulls). Cada unidad obtenida se integra en el roster del jugador; los duplicados proporcionan un bonus inmediato de stats (+5% por copia) y sirven como material de awakening para desbloquear tiers de progresión avanzada. En la demo, el gacha es exclusivamente de unidades (8-12 en el roster) — el gacha de barcos se pospone al juego completo cuando el roster naval alcance 20+ barcos. Sin este sistema, no hay adquisición de personajes nuevos, no hay emoción de descubrimiento, y el core loop de colección no funciona.

## Player Fantasy

**Fantasía**: Eres un capitán pirata que lanza su red al destino — cada invocación es una botella al mar que puede traer desde un marinero novato hasta un legendario bucanero. No estás comprando poder; estás reclutando tripulantes, cada uno con nombre, historia y un lugar donde encajar en tu flota.

**Emoción objetivo**: El gacha debe generar **tres momentos emocionales distintos**:

1. **La anticipación del pull**: El momento antes de ver el resultado. La animación de invocación debe construir tensión — el jugador sabe que puede cambiar su roster para siempre. Esto es puro entretenimiento, no ansiedad.

2. **El descubrimiento del valor oculto** (Pillar 3): El jugador obtiene un 3★ que inicialmente ignora, pero semanas después descubre que su trait "Malditos" completa una sinergia naval devastadora. El gacha no solo recompensa la suerte — recompensa la curiosidad y la paciencia. El sistema de duplicados refuerza esto: incluso obtener una copia extra de una unidad que ya tienes tiene valor inmediato (stat boost) y valor a largo plazo (awakening).

3. **El jackpot justo**: Cuando el pity se acumula y el jugador finalmente obtiene su 5★, debe sentirse como una recompensa ganada, no como caridad del sistema. El soft pity crea una narrativa de "cada pull me acerca más" que transforma la frustración en anticipación creciente.

**Referencia**: FFBE (la emoción del rainbow crystal, step-up banners con progresión tangible), Genshin Impact (soft pity que crea esperanza creciente, banner featured vs permanente como decisión estratégica).

**Tipo de sistema**: Activo — el jugador decide cuándo gastar, en qué banner, y si hacer single o multi-pull. Es una de las decisiones económicas más importantes del juego.

**El sistema falla si**: el jugador siente que los 3★ son basura automática (Pillar 3 roto), si la pity no se comunica claramente (frustración opaca), si los duplicados se sienten como castigo en vez de progreso, o si el gacha se siente como la única forma de progresar (pay-to-win).

## Detailed Design

### Core Rules

**1. Pools de Invocación (Banners)**

El sistema tiene dos banners simultáneos:

| Banner | Tipo | Pool | Moneda | Pity | Duración |
|--------|------|------|--------|------|----------|
| **Banner Estándar** | Permanente | Todas las unidades del juego | TIE (Ticket de Invocación Estándar) | Pity propio (soft 60, hard 90). Sin 50/50 | Siempre disponible |
| **Banner Destacado (GDC)** | Rate-up | Todas las unidades, con 1-2 featured a rate-up | GDC (Gemas de Calavera) | Pity propio + garantía 50/50 | Rotativo |
| **Banner Destacado (TIF)** | Rate-up | Mismo pool que Banner Destacado | TIF (Ticket de Invocación Featured) | Pity propio independiente + garantía 50/50 | Mismo que Banner Destacado |

**3 contadores de pity independientes:**
- **Estándar (TIE)**: Avanza con cada TIE pull. No hay 50/50 — todas las unidades
  dentro de cada rareza son equiprobables. Soft pity a 60, hard pity a 90.
- **Destacado (GDC)**: Avanza solo con pulls pagados con GDC. Incluye mecánica
  50/50 para 5★. Se conserva al cambiar de banner; estado 50/50 se reinicia.
- **Destacado (TIF)**: Avanza solo con pulls pagados con TIF. Incluye mecánica
  50/50. Pity independiente del contador GDC. Con ~1 TIF/mes, este pity rara vez
  se activa, pero está implementado para consistencia y escalabilidad futura.

Hacer pulls en un banner/moneda no afecta al pity de los otros. El banner
destacado rota periódicamente. Al cambiar el banner, los **pity counters (GDC y
TIF) se conservan** (el progreso hacia el 5★ no se pierde). El estado de la
garantía 50/50 **se reinicia** a FiftyFifty en ambos counters (la unidad featured
cambió). En la demo, la rotación es manual (no automática).

**2. Costos de Invocación**

| Tipo | Costo | Banner | Notas |
|------|-------|--------|-------|
| Single pull (GDC) | 300 GDC | Destacado | 1 unidad aleatoria. Avanza pity GDC |
| Multi-pull 10x (GDC) | 2,700 GDC | Destacado | 10% descuento vs 10 singles (3,000). **Garantiza 1x 4★+** |
| Single pull (TIF) | 1 TIF | Destacado | Mismo pool/rates que GDC. Avanza pity TIF (independiente) |
| Multi-pull 10x (TIF) | 10 TIF | Destacado | Sin descuento (10 tickets = 10 pulls). **Garantiza 1x 4★+** |
| Single pull (TIE) | 1 TIE | Estándar | 1 unidad aleatoria. Avanza pity TIE |
| Multi-pull 10x (TIE) | 10 TIE | Estándar | Sin descuento (10 tickets = 10 pulls). **Garantiza 1x 4★+** |

- La garantía del multi-pull funciona así: se generan las 10 unidades normalmente.
  Si ninguna es 4★ o superior, la última (posición 10) se reemplaza por una 4★
  aleatoria del pool. Esta garantía aplica independientemente del método de pago.
- **Tickets no tienen descuento** en multi-pull: 10 tickets = 10 pulls at face
  value. Los tickets ya representan valor gratuito; el descuento del 10% es
  exclusivo de GDC para incentivar el ahorro de premium currency.
- **No se acepta Doblones** para pulls. Solo GDC (featured), TIF (featured), o
  TIE (estándar).

**3. Rates Base**

| Rareza | Rate base | Notas |
|--------|-----------|-------|
| 5★ | 2% | ~1 de cada 50 pulls |
| 4★ | 15% | ~1 de cada 7 pulls |
| 3★ | 83% | La mayoría de pulls |

**4. Sistema de Pity (Soft + Hard)**

El pity protege contra rachas de mala suerte:

- **Soft pity** (pull 61-89): A partir del pull 60 sin obtener un 5★, la probabilidad de 5★ **aumenta linealmente** con cada pull. La fórmula: `EffectiveRate = BASE_5STAR_RATE + (PullCount - SOFT_PITY_START) × SOFT_PITY_INCREMENT`. Nota: en pull 60 el delta es 0 (el rate no cambia aún); el primer incremento real ocurre en pull 61
- **Hard pity** (pull 90): Si el jugador llega al pull 90 sin un 5★, el pull 90 es **un 5★ garantizado** (100%).
- **Reset**: Al obtener cualquier 5★ (por suerte normal, soft pity, o hard pity), el contador de pity se reinicia a 0.
- El pity counter se muestra al jugador en la UI del banner ("X/90 hasta garantía").

**5. Garantía 50/50 (Solo Banner Destacado)**

Cuando el jugador obtiene un 5★ en el banner destacado:

- **Primera vez**: 50% de probabilidad de que sea la unidad featured, 50% de que sea un 5★ off-banner (aleatorio del pool general de 5★).
- **Si pierde el 50/50** (obtiene off-banner): La **siguiente vez** que obtenga un 5★ en ese mismo banner, la unidad featured está **garantizada** (100%).
- **Reset**: Ganar el 50/50 O obtener el featured garantizado resetea el flag. La próxima vez vuelve a ser 50/50.
- **Cambio de banner**: El estado 50/50 se reinicia al cambiar el banner destacado. El pity counter se conserva.
- En el banner permanente **no hay 50/50** — no hay unidad featured.

**6. Resultado del Pull**

Cada pull produce exactamente 1 unidad:

1. El sistema determina la rareza (según rates + pity).
2. Dentro de esa rareza, selecciona una unidad aleatoria del pool del banner con distribución uniforme (todas las unidades de la misma rareza tienen la misma probabilidad), excepto la unidad featured que tiene el split 50/50 descrito en §5.
3. Si la unidad ya existe en el roster del jugador → es un **duplicado** (ver §7).
4. Si es nueva → se añade al roster con nivel 1, sin equipo, 0 duplicados.

**7. Sistema de Duplicados**

Cuando el jugador obtiene una unidad que ya posee:

**Demo (implementación actual):**
- **Stat bonus inmediato**: +5% a todos los stats base de esa unidad. Acumulativo, máximo 4 duplicados = +20%.
- **Después del cap (5+ copias extra)**: Se convierten automáticamente en **Fragmentos de Alma** según rareza del dupe (3★=3, 4★=10, 5★=50 fragmentos). Los fragmentos son un item de inventario acumulable, canjeable en la **Tienda de Almas** por una **unidad 5★ garantizada** del pool permanente rotativo (costo: 300 Fragmentos). Los fragmentos NO son moneda (no pasan por Currency System); se gestionan como item stackeable en el inventario del jugador. *Diseño detallado en Progresión de Unidades GDD.*

| Copia # | Bonus acumulado |
|---------|-----------------|
| 1ª (original) | — |
| 2ª (1er dupe) | +5% all stats |
| 3ª (2do dupe) | +10% all stats |
| 4ª (3er dupe) | +15% all stats |
| 5ª (4to dupe) | +20% all stats (cap) |
| 6ª+ | Convertido a Fragmentos de Alma |

**Full game (visión futura — NO implementar en demo):**
Los duplicados desbloquearán un **árbol de recompensas por unidad** que incluirá:
- Habilidades activas y pasivas exclusivas (no obtenibles de otra forma)
- Equipamiento especial (armas, armaduras) vinculado a esa unidad pero **usable por cualquier personaje**
- Stat boosts adicionales más allá del cap de +20%

Esto creará un incentivo para subir duplicados de **todas las rarezas**: un 3★ con 4 dupes puede desbloquear una espada que tu 5★ principal va a usar. El jugador quiere cada copia, de cualquier unidad, porque las recompensas de dupes tienen valor cross-roster. *Decisión pendiente: incluir antes o después de la demo.*

**8. Pool Composition (Demo)**

| Rareza | Unidades en pool (demo) | Notas |
|--------|------------------------|-------|
| 5★ | 2-3 unidades | Featured (1) + off-banner (1-2). Pool pequeño pero pity garantiza acceso |
| 4★ | 3-4 unidades | Unidades sólidas con sinergias útiles |
| 3★ | 3-5 unidades | Unidades base. Deben tener traits que complementen a las 4-5★ (Pillar 3) |

- La composición exacta del roster se define en el contenido de la demo, no en este GDD.
- Las 3★ DEBEN tener valor: traits que completen sinergias, habilidades navales útiles, o roles de nicho. "Personajes con Alma" aplica a todas las rarezas.

### States and Transitions

**Estados del Banner**

| Estado | Descripción | Transiciones |
|--------|-------------|-------------|
| `Active` | Banner disponible para pulls. Muestra pool, rates, pity counter | → `Expired` (duración termina, solo banner destacado) |
| `Expired` | Banner destacado ya no disponible. Pity counter se **conserva** para el siguiente banner destacado. Estado 50/50 se **reinicia** a FiftyFifty | → (eliminado de UI) |
| `Permanent` | Banner permanente. Siempre en estado Active | — (no transiciona) |

**Estados del Pull (por transacción)**

| Estado | Descripción | Transiciones |
|--------|-------------|-------------|
| `Idle` | Jugador está en la pantalla del banner, no ha iniciado pull | → `Confirming` (tap en Single o Multi) |
| `Confirming` | Diálogo de confirmación. Para GDC: diálogo premium con énfasis ("¿Gastar X GDC?"). Para TIE/TIF: diálogo simple ("¿Usar X tickets?") | → `Resolving` (confirma), → `Idle` (cancela) |
| `Resolving` | Sistema calcula resultado: verifica recurso (GDC balance o ticket count), deduce costo, genera unidad(es) según rates + pity del counter correspondiente. Selecciona variante de animación (directa o fake-out) | → `Animating` (éxito), → `InsufficientFunds` (fallo) |
| `Animating` | Animación "Botella en la Playa". La variante depende de la rareza más alta del resultado Y del roll de fake-out (ver tabla abajo). Skip disponible con tap | → `Revealing` (animación completa o skip) |
| `Revealing` | Muestra resultado(s): unidad(es) con rareza, nombre, indicador nuevo/duplicado. En multi-pull, las 10 unidades se muestran en secuencia rápida con opción de skip a vista completa | → `Idle` (jugador cierra) |
| `InsufficientFunds` | Recurso insuficiente. Para GDC: prompt para comprar más (→ Shop). Para tickets: prompt informativo mostrando cómo obtener más (login, misiones, logros) | → `Idle` (cierra), → Shop (solo si GDC, navega a tienda) |

**Animación "Botella en la Playa" — Variantes**

Al resolverse el pull, el sistema elige la variante de animación:
- **Animación directa** (70% de las veces para 4★/5★, 100% para 3★): La animación corresponde directamente a la rareza del resultado.
- **Fake-out** (30% de las veces cuando el resultado es 4★ o 5★): La animación **empieza como una rareza inferior** y escala dramáticamente.

| Variante | Rareza real | Animación |
|----------|-------------|-----------|
| **3★ directa** | 3★ | Botella llega con marea suave. Manos la recogen, pop simple. Pergamino se desenrolla revelando la silueta |
| **4★ directa** | 4★ | Botella llega con ola fuerte. Al abrirla, destello dorado — botella cae de las manos y brilla en la arena. Pergamino con chispas |
| **4★ fake-out** | 4★ | **Empieza como 3★**: marea suave, manos la recogen, pop simple → **la botella vibra**, destello dorado repentino, cae y brilla. Transición inesperada |
| **5★ directa** | 5★ | Botella llega con ola masiva. Al tocarla, **explota** en fragmentos de cristal brillante que flotan en el aire. Trueno + relámpago. Silueta emerge con aura legendaria |
| **5★ fake-out (desde 4★)** | 5★ | **Empieza como 4★**: ola fuerte, destello dorado al caer → **el suelo tiembla**, la botella se levanta y **explota** en cristales. Relámpago |
| **5★ fake-out (desde 3★)** | 5★ | **Empieza como 3★**: marea suave, pop simple → **vibración** (parece 4★ fake-out) → destello dorado → **el suelo tiembla**, botella explota. Doble escalación. Momento más raro y épico del gacha |

Para 5★ fake-out: 50% de probabilidad de empezar desde 3★ (doble escalación) vs. 50% desde 4★. Esto significa que el ~15% de los 5★ totales tendrán doble escalación (el momento más raro del gacha).

**Multi-pull**: La animación principal se determina por la **rareza más alta** del resultado de 10 pulls. Si hay un 5★, se usa animación de 5★ (con chance de fake-out). Las 10 unidades se revelan después en secuencia.

**Estados del Pity Counter (3 counters independientes)**

Cada counter (Estándar/TIE, Destacado/GDC, Destacado/TIF) tiene los mismos
estados internos:

| Estado | Descripción | Transiciones |
|--------|-------------|-------------|
| `Counting` | Acumulando pulls sin 5★. Counter visible: X/90 | → `SoftPity` (counter ≥ 60) |
| `SoftPity` | Rate de 5★ incrementando con cada pull. UI indica "rates aumentadas" | → `HardPity` (counter = 89, siguiente es 90) |
| `HardPity` | Siguiente pull es 5★ garantizado (100%) | → `Reset` (pull ejecutado) |
| `Reset` | 5★ obtenido. Counter vuelve a 0 | → `Counting` |

Un pull con GDC solo avanza el counter GDC. Un pull con TIF solo avanza el
counter TIF. Un pull con TIE solo avanza el counter TIE. No hay interacción
cruzada entre counters.

**Estado de Garantía 50/50 (banner destacado — GDC y TIF independientes)**

Cada método de pago en el banner destacado (GDC, TIF) tiene su propio estado
50/50 independiente:

| Estado | Descripción | Transiciones |
|--------|-------------|-------------|
| `FiftyFifty` | Próximo 5★ tiene 50% de ser featured | → `Guaranteed` (pierde 50/50), → `FiftyFifty` (gana 50/50) |
| `Guaranteed` | Próximo 5★ es featured garantizado (100%) | → `FiftyFifty` (featured obtenido) |

El banner estándar (TIE) **no tiene 50/50** — no hay unidad featured.

### Interactions with Other Systems

| Sistema | Dirección | Datos que fluyen | Interfaz |
|---------|-----------|-----------------|----------|
| **Currency System** | CS → SG | Balance de GDC, confirmación de gasto | SG llama `TrySpend(GDC, cost)` para pulls con GDC. Si falla, transiciona a InsufficientFunds |
| **Unit Roster/Inventory** | UR → SG | Balance de TIE/TIF (items de inventario) | SG consume tickets via inventario (no Currency System). Verifica count ≥ cost antes de resolver |
| **Rewards System** | RS → SG | TIE y TIF como rewards de login/misiones/logros | RS otorga tickets que el jugador gasta en SG. Conexión indirecta via inventario |
| **Unit Data Model** | UDM → SG | Pool de unidades disponibles: Id, Rarity, DisplayName, GachaPool. Definición de DuplicateBonus | SG lee la lista de unidades filtrada por banner/rareza. Al obtener duplicado, aplica +5% stats vía UDM |
| **Ship Data Model** | SDM → SG (futuro) | Pool de barcos para gacha naval (full game, no demo) | No implementado en demo. Interfaz futura: misma estructura que unidades pero con ShipData |
| **Save/Load System** | SG ↔ S/L | 3 pity counters (TIE, GDC, TIF), 2 estados 50/50 (GDC, TIF), historial de pulls, TIE/TIF balances (via inventario) | S/L persiste todo el estado del gacha. Al cargar, restaura counters y flags |
| **Menus & Navigation UI** | SG → UI | Eventos: pull resuelto (unidad + rareza + nuevo/dupe), pity counter actualizado, banner info (pool, rates, tiempo restante) | UI renderiza banners, animaciones, resultados. SG provee los datos |
| **Progresión de Unidades** | SG → PU | Duplicados como material de awakening. Fragmentos de Alma como moneda de progresión | SG notifica a PU cuando un duplicado se obtiene (+1 awakening mat) o cuando se genera Fragmentos de Alma (copia 6+) |

## Formulas

**1. Effective 5★ Rate (con Soft Pity)**

```
Si PullCount < SOFT_PITY_START:
    EffectiveRate = BASE_5STAR_RATE

Si PullCount >= SOFT_PITY_START y PullCount < HARD_PITY:
    EffectiveRate = BASE_5STAR_RATE + (PullCount - SOFT_PITY_START) × SOFT_PITY_INCREMENT

Si PullCount >= HARD_PITY:
    EffectiveRate = 1.0 (100%)
```

| Variable | Valor | Descripción |
|----------|-------|-------------|
| `BASE_5STAR_RATE` | 0.02 (2%) | Probabilidad base de 5★ en cada pull |
| `BASE_4STAR_RATE` | 0.15 (15%) | Probabilidad base de 4★ en cada pull |
| `BASE_3STAR_RATE` | 0.83 (83%) | Probabilidad base de 3★ |
| `SOFT_PITY_START` | 60 | Pull a partir del cual el rate de 5★ empieza a subir |
| `HARD_PITY` | 90 | Pull que garantiza 5★ al 100% |
| `SOFT_PITY_INCREMENT` | 0.0327 (~3.27%) | Incremento por pull durante soft pity. Calibrado para que pull 89 ≈ 97% y pull 90 = 100% |

**Tabla de rates durante soft pity:**

| Pull # | EffectiveRate 5★ | Acumulado (prob. de haber obtenido 5★ al menos una vez) |
|--------|-----------------|-------------------------------------------------------|
| 1-59 | 2.0% | ~70% al pull 59 |
| 60 | 2.0% | ~70.6% |
| 65 | 18.4% | ~81% |
| 70 | 34.7% | ~92% |
| 75 | 51.1% | ~97.5% |
| 80 | 67.4% | ~99.5% |
| 85 | 83.8% | ~99.97% |
| 89 | 96.8% | ~99.999% |
| 90 | 100% | 100% (hard pity) |

**Nota**: La mayoría de jugadores obtendrán su 5★ entre pull 60-80 gracias al soft pity. Llegar a hard pity (90) es extremadamente raro (~0.5% de los casos).

**Caso promedio F2P**: Con soft pity, el promedio esperado está en ~65-75 pulls para un 5★. A 300 GDC/pull = 19,500-22,500 GDC. Con ~1,800 GDC/semana (de Currency System), un F2P promedio tarda **~11-13 semanas** en obtener un 5★. El worst-case (hard pity a 90 pulls = 27,000 GDC) son ~15 semanas.

**2. Selección de Unidad dentro de Rareza**

```
Para banner permanente:
    Unidad = random_uniform(pool[rareza])    // todas equiprobables

Para banner destacado, si rareza = 5★:
    Si estado = FiftyFifty:
        roll = random(0, 1)
        Si roll < 0.5: Unidad = featured_unit
        Si roll >= 0.5: Unidad = random_uniform(pool_5star_sin_featured)
    Si estado = Guaranteed:
        Unidad = featured_unit

Para banner destacado, si rareza = 4★ o 3★:
    Unidad = random_uniform(pool[rareza])    // sin rate-up para 4★/3★ en demo
```

**3. Garantía Multi-Pull**

```
resultados = []
para i en 1..10:
    resultados[i] = resolver_pull_normal(rates, pity)

si ningún resultado tiene rareza >= 4★:
    resultados[10].rareza = 4★
    resultados[10].unidad = random_uniform(pool_4star)
```

**4. Duplicate Stat Bonus**

```
DupeBonus = min(DupeCount, MAX_DUPES) × DUPE_STAT_PERCENT
EffectiveBaseStat = BaseStat × (1.0 + DupeBonus)
```

| Variable | Valor | Descripción |
|----------|-------|-------------|
| `MAX_DUPES` | 4 | Máximo de duplicados que otorgan bonus |
| `DUPE_STAT_PERCENT` | 0.05 (5%) | Bonus por duplicado a todos los stats base |

**5. Fragmentos de Alma (copia 6+)**

```
Si DupeCount > MAX_DUPES:
    Fragmentos ganados = FRAGMENTS_PER_EXTRA_DUPE[rareza]
```

| Rareza de la unidad duplicada | Fragmentos por copia extra |
|-------------------------------|---------------------------|
| 3★ | 5 Fragmentos |
| 4★ | 15 Fragmentos |
| 5★ | 50 Fragmentos |

**6. Selección de Animación (Fake-out)**

```
Si rareza_resultado = 3★:
    animación = "3★ directa"

Si rareza_resultado = 4★:
    roll = random(0, 1)
    Si roll < FAKEOUT_CHANCE: animación = "4★ fake-out (desde 3★)"
    Si no: animación = "4★ directa"

Si rareza_resultado = 5★:
    roll = random(0, 1)
    Si roll < FAKEOUT_CHANCE:
        sub_roll = random(0, 1)
        Si sub_roll < 0.5: animación = "5★ fake-out (desde 3★)"  // doble escalación
        Si no: animación = "5★ fake-out (desde 4★)"
    Si no: animación = "5★ directa"
```

| Variable | Valor | Descripción |
|----------|-------|-------------|
| `FAKEOUT_CHANCE` | 0.30 (30%) | Probabilidad de animación fake-out para 4★ y 5★ |

**7. Worked Example**

```
Jugador hace multi-pull en banner destacado. Estado: FiftyFifty, pity counter = 58.

Pull 1 (counter 59): Rate 2% → roll 0.87 → 3★. Pool 3★: [Marina, Pedro, Sol, Luca]. Roll → Sol
Pull 2 (counter 60): Soft pity! Rate 2% + 0×3.27% = 2% → roll 0.44 → 3★. Roll → Marina
Pull 3 (counter 61): Rate 2% + 1×3.27% = 5.27% → roll 0.91 → 3★. Roll → Pedro
Pull 4 (counter 62): Rate 5% + 2×3.27% = 8.54% → roll 0.03 → ★★★ 5★! ★★★
  Estado FiftyFifty: roll 0.62 → pierde 50/50. Unidad = off-banner 5★ "Doña Morgana"
  Estado → Guaranteed. Pity counter → 0
Pull 5 (counter 1): Rate 2% → roll 0.78 → 3★. Roll → Luca (ya la tiene: +5% stats, dupe #1)
Pull 6 (counter 2): Rate 2% → roll 0.12 → 4★. Pool 4★: [Roque, Ana, Félix]. Roll → Roque
Pull 7-10: 3★, 3★, 3★, 3★

Garantía multi-pull check: Pull 6 fue 4★ → garantía cumplida, no se reemplaza nada.

Resultado final: 1× 5★ (Doña Morgana, off-banner), 1× 4★ (Roque), 8× 3★ (1 dupe)
Animación: rareza más alta = 5★. Roll fake-out 0.22 < 0.30 → fake-out! Sub-roll 0.71 → desde 4★.
→ Animación: empieza como 4★ (ola fuerte, destello dorado) → suelo tiembla → explosión de cristal

Pity counter final: 6 (pulls 5-10 después del reset)
Estado 50/50: Guaranteed (perdió el 50/50, próximo 5★ en este banner será featured)
```

## Edge Cases

1. **Jugador hace pull con exactamente el GDC justo**: Transacción procede normalmente. Balance queda en 0. No hay problemas — balance 0 es válido.

2. **Multi-pull donde los 10 resultados son 3★ (antes de garantía)**: La posición 10 se reemplaza por una 4★ aleatoria. El pity counter avanza +10. La animación usa la variante de 4★ (con chance de fake-out).

3. **Pull #90 en banner destacado con estado Guaranteed**: El jugador obtiene la unidad featured al 100% (hard pity + garantía 50/50 se combinan). Ambos estados se resetean.

4. **Pull #90 en banner destacado con estado FiftyFifty**: Hard pity garantiza 5★, pero la selección 50/50 sigue aplicando. Puede obtener off-banner. Si pierde el 50/50, el siguiente 5★ será guaranteed.

5. **Banner destacado expira mientras el jugador tiene pity acumulado**: El pity counter se **conserva** para el siguiente banner destacado. El estado 50/50 **se reinicia** a FiftyFifty (la unidad featured cambió). UI muestra al jugador su pity actual al entrar al nuevo banner ("Llevas X/90 pulls").

6. **Duplicado de unidad ya en cap (+20%)**: Se convierte en Fragmentos de Alma (5/15/50 según rareza). No se añade stat bonus adicional. UI muestra "Fragmentos de Alma ×N obtenidos".

7. **Pool del banner tiene solo 1 unidad de 5★ y el jugador pierde el 50/50 en featured**: El off-banner 5★ se elige del pool general de 5★ **excluyendo** la unidad featured. Si solo hay 1 unidad 5★ en todo el juego → el 50/50 siempre da la featured (no hay off-banner posible). *Nota demo: con 2-3 unidades 5★, perder el 50/50 da una de las 1-2 off-banner.*

8. **Jugador desconecta durante la animación de pull**: El resultado ya fue determinado y guardado en `Resolving`. GDC ya deducido. Al reconectar, se muestra la pantalla de `Revealing` con los resultados. No hay pérdida.

9. **Jugador hace single pull con pity en 89 (hard pity)**: El pull es 5★ garantizado. Se aplica la regla 50/50 si es banner destacado. Pity se resetea.

10. **Soft pity rate + base excede 100%**: Clamp a 100%. En la práctica, con los valores actuales, pull 89 ≈ 97% y pull 90 = hard pity (100%), así que no se excede antes de hard pity.

11. **Multi-pull cruza el hard pity (ej: pity en 85, hace multi de 10)**: Cada pull dentro del multi se resuelve secuencialmente. Pull 90 (posición 5 del multi) es 5★ garantizado. Los pulls 6-10 del multi continúan con pity reseteado a 0. Es posible obtener **dos 5★** en un mismo multi-pull si hay suerte en los pulls post-reset.

12. **Jugador obtiene la unidad featured en el banner permanente**: No es posible. El banner permanente no tiene featured — todas las unidades de cada rareza son equiprobables. La unidad puede estar en ambos pools, pero no tiene rate-up en el permanente.

13. **Fragmentos de Alma insuficientes para canjear algo**: Los fragmentos se acumulan sin límite. La tienda de fragmentos muestra claramente el costo y el balance actual. No se puede canjear parcialmente.

14. **Fake-out animation + skip**: Si el jugador hace tap para skip durante una animación fake-out, se salta directamente a `Revealing`. El jugador ve el resultado final sin la escalación. El skip es siempre instantáneo.

15. **Dos multi-pulls consecutivos muy rápidos**: Las transacciones se serializan (heredado de Currency System). El segundo multi-pull espera a que el primero complete `Resolving` antes de iniciar `Confirming`.

16. **Jugador tiene 8 TIE e intenta multi-pull de 10**: Insuficiente. Se necesitan exactamente 10 tickets para un multi-pull. No hay pulls parciales (ej: 8 tickets + GDC para completar). El jugador puede hacer 8 singles.

17. **Jugador usa TIF en banner destacado — ¿comparte pity con GDC?**: No. El pity de TIF es independiente del pity de GDC. Ambos se muestran por separado en la UI del banner. El jugador ve "GDC: X/90" y "Tickets: Y/90".

18. **Banner destacado cambia — ¿qué pasa con pity TIF?**: El pity counter TIF se conserva (igual que GDC). El estado 50/50 de TIF se reinicia a FiftyFifty.

19. **Jugador usa TIE en banner estándar — ¿hay 50/50?**: No. El banner estándar no tiene unidad featured. Todas las unidades dentro de cada rareza son equiprobables. El pity TIE funciona igual que el de GDC (soft 60, hard 90) pero sin mecánica 50/50.

## Dependencies

### Dependencias Upstream (SG depende de)

| Sistema | Tipo | Interfaz | GDD |
|---------|------|----------|-----|
| **Unit Data Model** | Hard | Pool de unidades (Id, Rarity, GachaPool, DisplayName), DuplicateBonus, NavalRoleAffinity para mostrar info | ✅ Approved |
| **Ship Data Model** | Soft (futuro) | Pool de barcos para gacha naval. No usado en demo | ✅ Approved |
| **Currency System** | Hard | `TrySpend(GDC, cost)` para deducir GDC. `GetBalance(GDC)` para mostrar balance | ✅ Approved (updated 2026-04-01: TIE/TIF section added) |
| **Unit Roster/Inventory** | Hard | TIE/TIF balance (items de inventario). SG consume tickets via inventario | ⬜ Not Started |
| **Rewards System** | Soft | Otorga TIE y TIF al jugador. SG los consume. Conexión indirecta via inventario | ✅ Designed |

### Dependencias Downstream (dependen de SG)

| Sistema | Tipo | Qué necesita de SG | GDD |
|---------|------|---------------------|-----|
| **Progresión de Unidades** | Hard | Duplicados como material de awakening, Fragmentos de Alma como moneda de progresión | ✅ Approved |
| **Save/Load System** | Hard | 3 pity counters (TIE, GDC, TIF), 2 estados 50/50 (GDC, TIF), Fragmentos de Alma, historial de pulls, TIE/TIF balances | ⬜ Not Started |
| **Menus & Navigation UI** | Hard | Datos de banners activos, pool info, pity display, resultados de pull para renderizar | ⬜ Not Started |
| **Unit Roster/Inventory** | Soft | Nuevas unidades añadidas al roster tras pull. Duplicados notificados para actualizar stats | ⬜ Not Started |

### Cross-System Updates Necesarios

- **Unit Data Model**: El campo `DuplicateBonus` en UnitData necesita implementarse como: `DupeCount` (int, 0-4+) y `DupeStatPercent` (0.05 por dupe). Actualmente UDM lo menciona pero no define la estructura exacta
- ~~**Currency System**: Fragmentos de Alma se gestionan como **item de inventario**, no como moneda. Currency System no necesita cambios~~ → **Resuelto 2026-04-01**: Currency System actualizado con sección TIE/TIF. Tickets son items de inventario, no currencies.
- **Unit Roster/Inventory**: Debe implementar balance de TIE/TIF como items stackeables. SG consume tickets via interfaz de inventario

## Tuning Knobs

| Knob | Valor Actual | Rango Seguro | Afecta a | Notas |
|------|-------------|-------------|----------|-------|
| `BASE_5STAR_RATE` | 0.02 (2%) | 0.01–0.05 | Frecuencia de 5★ sin pity. Muy bajo: jugador nunca ve 5★ fuera de pity. Muy alto: 5★ pierde valor | Rates son visibles al jugador — cambios post-launch generan desconfianza |
| `BASE_4STAR_RATE` | 0.15 (15%) | 0.10–0.25 | Frecuencia de 4★. Muy bajo: multi-pulls deprimentes. Muy alto: 4★ pierde valor | La garantía multi-pull mitiga rates bajos |
| `SOFT_PITY_START` | 60 | 40–75 | Cuándo empieza la escalación de rates. Más bajo: más generoso. Más alto: más tiempo en rates base | Interactúa directamente con HARD_PITY — la rampa debe sentirse gradual |
| `HARD_PITY` | 90 | 60–120 | Máximo de pulls sin 5★. Directamente define el worst-case del jugador | 90 es estándar de industria (Genshin). Menor = más generoso, mayor = más lucrativo |
| `SOFT_PITY_INCREMENT` | 0.0327 | 0.01–0.06 | Velocidad de escalación durante soft pity. Muy bajo: soft pity se siente irrelevante. Muy alto: salta de 2% a 100% en pocos pulls | Debe calibrarse para que la curva sea suave entre SOFT_PITY_START y HARD_PITY |
| `SINGLE_PULL_COST_GDC` | 300 | 200–500 | Costo por pull. Define la accesibilidad F2P. Heredado de Currency System | Cambiar aquí requiere actualizar Currency System |
| `MULTI_PULL_DISCOUNT` | 10% | 0–20% | Incentivo de multi vs single. Muy bajo: no hay razón para esperar. Muy alto: single se siente como tirar GDC | Heredado de Currency System |
| `DUPE_STAT_PERCENT` | 0.05 (5%) | 0.03–0.10 | Bonus por duplicado. Muy bajo: dupes irrelevantes. Muy alto: whale advantage excesivo (4 dupes = +40%) | Con cap de 4 dupes, el rango efectivo es 12%-40% bonus total |
| `MAX_DUPES` | 4 | 3–6 | Cuántos dupes son útiles. Más alto: más razón para seguir pullando la misma unidad. Más bajo: cap rápido → Fragmentos | Interactúa con DUPE_STAT_PERCENT para el bonus total |
| `FRAGMENTS_PER_EXTRA_DUPE` | 5/15/50 (3★/4★/5★) | 3-10/10-30/30-100 | Valor de dupes post-cap. Muy bajo: dupes se sienten desperdiciados. Muy alto: infla la economía de fragmentos | Debe balancearse con los costos de la tienda de fragmentos |
| `FAKEOUT_CHANCE` | 0.30 (30%) | 0.15–0.50 | Frecuencia de fake-out en animación. Muy bajo: raramente se ve, pierde impacto. Muy alto: jugadores esperan el fake-out, pierde sorpresa | No afecta gameplay, solo experiencia emocional |
| `FEATURED_RATE_SPLIT` | 0.50 (50%) | 0.40–0.75 | Probabilidad de featured vs off-banner al obtener 5★. Más alto: más fácil obtener featured. Más bajo: más frustrante | La garantía del siguiente 5★ mitiga rates bajos |

### Knob Interactions

| Knob A | Knob B | Interacción |
|--------|--------|-------------|
| BASE_5STAR_RATE | SOFT_PITY_START | Si base rate sube Y soft pity empieza antes, la probabilidad acumulada sube dramáticamente. Ajustar como grupo |
| HARD_PITY | SINGLE_PULL_COST_GDC | Worst-case GDC para un 5★ = HARD_PITY × SINGLE_PULL_COST. Con 90 × 300 = 27,000 GDC. Si el costo sube, el worst-case se vuelve brutal |
| DUPE_STAT_PERCENT | MAX_DUPES | Bonus total = PERCENT × MAX. Ambos altos = ventaja masiva de whales. Ambos bajos = dupes irrelevantes |
| FRAGMENTS_PER_EXTRA_DUPE | Costos tienda fragmentos | Si fragmentos/dupe es alto Y costos son bajos, whales acceden a todo rápido. Balance como ratio |
| F2P GDC semanal (Currency System) | SINGLE_PULL_COST_GDC × HARD_PITY | Define cuántas semanas tarda un F2P en alcanzar hard pity. Con 1,800 GDC/semana y 27,000 worst-case = 15 semanas. Ajustar si el ciclo es demasiado largo |

## Visual/Audio Requirements

**Visual**
- **Escena de invocación**: Playa al atardecer/amanecer con olas. Parallax de fondo con cielo, nubes, horizonte marino. Arena en primer plano donde llega la botella
- **Botella**: Sprite detallado con pergamino visible dentro. Brillo sutil en idle. 3 variantes de color de contenido según rareza: blanco (3★), dorado (4★), púrpura/arcoíris (5★) — visible solo en animación directa, NO en fake-out (para mantener la sorpresa)
- **Efectos de ola**: 3 niveles de intensidad: marea suave (3★), ola fuerte con espuma (4★), ola masiva con spray (5★)
- **Efecto fake-out**: La botella vibra/tiembla con partículas de energía antes de escalar. Efecto de "crack" visual en el cristal antes de explotar (5★)
- **Explosión de cristal (5★)**: Fragmentos reflectantes flotando en el aire con light rays. Relámpago en el cielo. Silueta de la unidad emerge con aura elemental (color del elemento de la unidad)
- **Pantalla de reveal**: Fondo oscuro con la unidad en pose signature. Nombre, rareza (estrellas), e indicador NUEVO/DUPE. Para 5★: efecto de partículas continuo + border animado
- **Multi-pull reveal**: 10 cartas boca abajo que se voltean en secuencia (rápida). Las 4★+ tienen borde dorado, las 5★ tienen borde arcoíris. Vista resumen al final
- **Banner UI**: Artwork de la unidad featured en pose dinámica. Fondo temático pirata. Timer de expiración. Botones de Single/Multi prominentes
- **Indicador de pity**: Barra de progreso estilizada como "distancia al tesoro" (mapa con camino). Marca visual en el punto de soft pity (60) y hard pity (90)

**Audio**
- **Música de invocación**: Tema propio — empieza suave y misterioso (olas, viento). Escalation musical si hay 4★+ (percusión). Para 5★: crescendo épico con coro/cuernos
- **SFX ola**: 3 niveles de intensidad (calm splash → wave crash → tsunami roar)
- **SFX botella**: Pop de corcho (3★), crack + destello (4★), shatter explosivo + trueno (5★)
- **SFX fake-out**: Sonido tenso de vibración/energía acumulándose antes de la escalación. "Whoosh" al transicionar
- **SFX reveal**: Chime melódico por unidad revelada. Más impactante para rarezas altas. 5★: fanfare corta
- **SFX duplicado**: Sonido de "fusión" o "absorción" — claramente diferente al de unidad nueva
- **SFX Fragmentos de Alma**: Sonido cristalino (los fragmentos de la botella se transforman en la moneda)

## UI Requirements

- **Pantalla principal de gacha**: Accesible desde el menú principal. Muestra los banners activos como "cartas" deslizables (swipe horizontal): Banner Estándar (siempre primero) + Banner Destacado (con artwork del featured, timer). Cada carta muestra nombre del banner, botón "Detalles"
- **Pantalla de banner destacado (al entrar)**: Artwork grande de la unidad featured. Sección GDC: botones "Single (300 GDC)" y "Multi ×10 (2,700 GDC)". Sección TIF: botones "Single (1 TIF)" y "Multi ×10 (10 TIF)" — visibles solo si el jugador tiene ≥1 TIF. Balances de GDC y TIF visibles. Dos pity counters mostrados: "GDC: X/90" y "Tickets: Y/90"
- **Pantalla de banner estándar (al entrar)**: Sin artwork featured (todas las unidades equiprobables). Botones "Single (1 TIE)" y "Multi ×10 (10 TIE)". Balance de TIE visible. Pity counter: "TIE: X/90"
- **Botón de rates/detalles**: Abre popup con tabla completa de rates por rareza y lista de unidades en el pool. Obligatorio por regulación de gacha en muchos mercados
- **Confirmación de pull**: Diálogo premium (heredado de Currency System): borde dorado, "¿Gastar X GDC?", botones Confirmar/Cancelar. Muestra balance actual y balance después del gasto
- **Pantalla de animación**: Fullscreen. Escena de playa. Sin UI excepto botón "Skip" semi-transparente en esquina. La animación ocupa toda la pantalla
- **Pantalla de reveal (single)**: Unidad centrada con pose, nombre, rareza (estrellas animadas), elemento, indicador NUEVO (verde) o DUPE #X (azul con "+5% stats"). Botón "OK" para cerrar
- **Pantalla de reveal (multi)**: 10 cartas en grid (2 filas × 5). Se voltean en secuencia rápida o skip a todas reveladas. Las nuevas tienen badge "NEW". Botón "Detalles" para ver cada unidad individualmente. Botón "OK" para cerrar
- **Indicador de pity en banner**: Siempre visible. En banner destacado: dos barras (GDC y TIF) con marcas en 60/90. En banner estándar: una barra (TIE). Texto: "X/90". Al entrar en soft pity (≥60), la barra cambia de color (dorado) con texto "Probabilidad aumentada"
- **Indicador de 50/50**: En banner destacado, badge junto a cada pity counter (GDC y TIF por separado): "50/50" o "Garantizado". Tooltip al tap explicando la mecánica. No aparece en banner estándar
- **Historial de pulls**: Botón en la pantalla de gacha que abre lista scrolleable de últimos N pulls con: fecha, banner, unidad obtenida, rareza, nuevo/dupe
- **Fragmentos de Alma**: Balance visible en la pantalla de gacha o en perfil. Botón "Tienda de Fragmentos" accesible desde la pantalla de gacha

## Acceptance Criteria

**Rates y Resolución**
1. Single pull (GDC) deduce 300 GDC y produce exactamente 1 unidad
2. Multi-pull (GDC) deduce 2,700 GDC y produce exactamente 10 unidades
3. Single pull (TIE) deduce 1 TIE y produce exactamente 1 unidad (banner estándar)
4. Multi-pull (TIE) deduce 10 TIE y produce exactamente 10 unidades (sin descuento)
5. Single/Multi pull (TIF) deduce 1/10 TIF y produce 1/10 unidades (banner featured, sin descuento)
6. Rates base se cumplen estadísticamente: ~2% 5★, ~15% 4★, ~83% 3★ (verificar con 10,000+ pulls simulados)
7. Multi-pull siempre contiene al menos 1 unidad de 4★ o superior (independientemente del método de pago)
8. Recurso insuficiente → transacción denegada, balance/tickets intactos

**Pity System (3 counters independientes)**
9. Cada método de pago (TIE, GDC, TIF) tiene su propio pity counter
10. Pity counter incrementa +1 por cada pull sin 5★ en ese counter
11. Pity counter se resetea a 0 al obtener un 5★ en ese counter
12. Soft pity: a partir del pull 60, la rate efectiva de 5★ aumenta según fórmula §1
13. Hard pity: pull 90 siempre produce un 5★ (100%)
14. Pity counters GDC y TIF se conservan al cambiar de banner destacado
15. Los 3 pity counters no interactúan entre sí

**Garantía 50/50**
16. En banner destacado (GDC), un 5★ tiene 50% de ser featured y 50% off-banner
17. En banner destacado (TIF), aplica la misma mecánica 50/50 con estado independiente
18. Si el jugador pierde el 50/50, el siguiente 5★ en ese counter es featured garantizado
19. El estado 50/50 de ambos counters (GDC, TIF) se reinicia a FiftyFifty al cambiar de banner
20. El banner estándar (TIE) no tiene mecánica 50/50

**Duplicados**
16. Obtener una unidad ya poseída aplica +5% all stats (hasta 4 dupes = +20%)
17. La 6ª+ copia se convierte en Fragmentos de Alma (5/15/50 por rareza)
18. El bonus de stats de duplicados se refleja correctamente en todas las pantallas de stats

**Animación**
19. La animación de pull corresponde a la rareza más alta del resultado
20. ~30% de los pulls 4★/5★ usan animación fake-out (verificar estadísticamente)
21. Skip (tap) durante animación lleva inmediatamente a la pantalla de resultados
22. Desconexión durante animación → resultado recuperado al reconectar

**Banners**
23. Banner permanente siempre disponible con pool completo
24. Banner destacado muestra: unidad featured, rates, pity counter, tiempo restante
25. Rates publicadas en UI coinciden con rates reales del sistema

**Persistencia**
28. Los 3 pity counters (TIE, GDC, TIF), 2 estados 50/50 (GDC, TIF), y Fragmentos de Alma sobreviven save/load sin pérdida
29. TIE/TIF balances sobreviven save/load sin pérdida
30. Resultado del pull se persiste antes de la animación (resistente a crash)

## Open Questions

[To be designed]
