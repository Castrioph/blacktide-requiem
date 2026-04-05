# Game Concept: Pirate Gacha RPG (Working Title)

*Created: 2026-03-25*
*Status: Draft*

---

## Elevator Pitch

> Un RPG gacha por turnos ambientado en un mundo de piratas donde lideras una
> tripulación tanto en combate terrestre como naval. Colecciona personajes y
> barcos, construye sinergias entre tu tripulacion mediante roles y traits,
> y domina dos campos de batalla completamente distintos.

---

## Core Identity

| Aspect | Detail |
| ---- | ---- |
| **Genre** | RPG por turnos + Gacha |
| **Platform** | Mobile / Web |
| **Target Audience** | Jugadores mid-core de gacha RPGs (perfil FFBE) |
| **Player Count** | Single-player con sistema de amigos (unidad prestada) |
| **Session Length** | 15-60 minutos |
| **Monetization** | F2P con gacha (a definir modelo concreto) |
| **Estimated Scope** | Demo: Small (3-6 meses) / Juego completo: Large (12+ meses) |
| **Comparable Titles** | Final Fantasy Brave Exvius, Summoners War, Epic Seven |

---

## Core Fantasy

Eres un capitán pirata que recluta tripulantes de todos los rincones del
mundo, cada uno con su propia historia y habilidades. Tu poder no está solo
en la fuerza bruta de tus personajes, sino en cómo los combinas: la
tripulación correcta en el barco correcto convierte un grupo de desconocidos
en una fuerza imparable. En tierra luchas cuerpo a cuerpo con tu equipo de
elite; en el mar, tu barco es tu arma y tu tripulación lo potencia.

La fantasía es ser el capitán estratega que ve potencial donde otros ven
unidades descartables — el que descubre que aquel marinero olvidado era la
pieza clave para desbloquear una sinergia devastadora.

---

## Unique Hook

"Como Final Fantasy Brave Exvius, Y ADEMAS tiene un sistema de combate
naval donde tu barco tiene slots de rol (capitan, artillero, navegante...)
y las habilidades y traits de tu tripulacion modifican las capacidades del
barco, creando un segundo layer estrategico completamente distinto al
combate terrestre."

Cada personaje tiene dos sets de habilidades — uno para tierra y otro para
mar — lo que significa que una unidad mediocre en combate terrestre puede
ser la mejor artillera del juego. Esto duplica el valor de cada personaje
y hace que ninguna unidad sea realmente "basura".

---

## Player Experience Analysis (MDA Framework)

### Target Aesthetics (What the player FEELS)

| Aesthetic | Priority | How We Deliver It |
| ---- | ---- | ---- |
| **Sensation** (sensory pleasure) | 5 | Pixel art chibi con juice en combate, cinemáticas anime |
| **Fantasy** (make-believe, role-playing) | 2 | Ser un capitán pirata, construir tu tripulación y barco |
| **Narrative** (drama, story arc) | 4 | Historia concisa y visual, personajes con personalidad |
| **Challenge** (obstacle course, mastery) | 1 | Desafíos manuales que exigen composición y estrategia |
| **Fellowship** (social connection) | 6 | Sistema de amigos, unidad prestada |
| **Discovery** (exploration, secrets) | 3 | Sinergias ocultas de traits, habilidades que revelan valor con el tiempo |
| **Expression** (self-expression, creativity) | 7 | Composición de equipo terrestre + tripulación naval |
| **Submission** (relaxation, comfort zone) | 8 | Auto-battle para farming de contenido fácil |

### Key Dynamics (Emergent player behaviors)

- Los jugadores experimentarán con combinaciones de tripulación naval para
  descubrir sinergias de traits que potencien el barco de formas inesperadas
- Los jugadores conservarán unidades de baja rareza porque sus traits o
  habilidades navales pueden ser clave en ciertas composiciones
- Los jugadores alternarán entre farming en auto y engagement manual en
  contenido difícil, creando un ritmo de juego natural
- Los jugadores revaluarán unidades constantemente al descubrir nuevos
  contextos donde brillan (tierra vs. mar, traits específicos)

### Core Mechanics (Systems we build)

1. **Combate terrestre por turnos** — Barra de iniciativa, equipo de 5+1
   unidades, habilidades terrestres con posibilidad de acción múltiple
2. **Combate naval por turnos** — Barco con stats propios, slots de rol
   (capitán, artillero, etc.), traits y sinergias de tripulación
3. **Sistema gacha dual** — Invocación de unidades e invocación de barcos
4. **Progresión de unidades** — Niveles, awakening, duplicados
5. **Sistema de traits y sinergias** — Etiquetas compartidas entre
   personajes que activan bonificaciones (ej: "Los de la Negra")

---

## Player Motivation Profile

### Primary Psychological Needs Served

| Need | How This Game Satisfies It | Strength |
| ---- | ---- | ---- |
| **Autonomy** (freedom, meaningful choice) | Libertad para componer equipos terrestres y tripulaciones navales; elegir qué contenido atacar y cómo | Core |
| **Competence** (mastery, skill growth) | Dominar el sistema de iniciativa, descubrir sinergias, superar desafíos difíciles con estrategia | Core |
| **Relatedness** (connection, belonging) | Conexión emocional con personajes, sistema de amigos | Supporting |

### Player Type Appeal (Bartle Taxonomy)

- [x] **Achievers** (goal completion, collection, progression) — Coleccionar
  unidades y barcos, completar stages, maximizar progresión
- [x] **Explorers** (discovery, understanding systems, finding secrets) —
  Descubrir sinergias de traits, encontrar valor oculto en unidades "débiles"
- [ ] **Socializers** (relationships, cooperation, community) — Limitado al
  sistema de amigos en la demo
- [ ] **Killers/Competitors** (domination, PvP, leaderboards) — No es foco
  del juego

### Flow State Design

- **Onboarding curve**: Los primeros stages enseñan combate terrestre básico.
  Se introduce la barra de iniciativa gradualmente. El combate naval se
  desbloquea después de familiarizarse con el terrestre.
- **Difficulty scaling**: Los stages avanzan en dificultad. El modo auto
  funciona para stages fáciles pero falla en contenido difícil, forzando
  engagement manual. Los desafíos opcionales exigen composición optimizada.
- **Feedback clarity**: Barras de vida, números de daño, indicadores visuales
  de sinergias activas, resumen post-combate de rendimiento.
- **Recovery from failure**: Sin penalización por perder — se conserva la
  energía/intento. El jugador puede recomponer su equipo y reintentar
  inmediatamente.

---

## Core Loop

### Moment-to-Moment (30 seconds)

Leer la barra de iniciativa para anticipar el orden de turnos. Seleccionar
habilidades considerando posición, tipo de enemigo y sinergias con el resto
del equipo. En naval: decidir qué habilidades del barco usar y cómo la
tripulación las potencia. La satisfacción viene de ejecutar una secuencia
planeada y ver los números de sinergia activarse.

### Short-Term (5-15 minutes)

Completar un stage (3-5 oleadas de combate) -> pantalla de recompensas
(materiales, moneda, experiencia) -> decisión: repetir en auto para farmear
o avanzar al siguiente stage. Los stages difíciles rompen el auto y exigen
recomposición manual del equipo.

### Session-Level (30-60 minutes)

Avanzar en stages de historia -> acumular recursos -> hacer pulls en el
gacha -> obtener nueva unidad o barco -> subirla de nivel/despertar ->
probar nuevas composiciones -> enfrentarse al contenido más difícil
disponible. La sesión termina naturalmente cuando se agota la energía
o se completa el objetivo del día.

### Long-Term Progression

Coleccionar unidades y barcos -> descubrir combinaciones de traits ->
construir equipos especializados para tierra y mar -> awakening de unidades
favoritas -> enfrentarse a desafíos end-game que exigen optimización
completa de equipo + tripulación + barco.

### Retention Hooks

- **Curiosity**: Qué sinergias desbloqueará la próxima unidad? Qué viene
  en el siguiente capítulo de la historia?
- **Investment**: Tripulación construida con cariño, barco optimizado,
  progreso de awakening
- **Social**: Amigos que prestan unidades útiles, descubrir builds de
  otros jugadores (futuro)
- **Mastery**: Desafíos difíciles que exigen repensar la composición
  completa

---

## Game Pillars

### Pillar 1: Profundidad Estratégica Dual

Dos campos de batalla, dos formas de pensar. El combate terrestre y naval
son experiencias estratégicas distintas que se complementan.

*Design test*: Si una mecánica solo funciona en tierra O en mar pero no
añade profundidad al sistema general, cuestionarla. Cada unidad debe
sentirse valiosa en al menos un modo de combate.

### Pillar 2: Personajes con Alma

Cada unidad es un personaje, no una estadística. Incluso los personajes
de baja rareza tienen personalidad y razón para existir en el mundo.

*Design test*: Si un personaje no tiene personalidad, historia mínima, ni
razón para existir en el mundo pirata, no entra al roster.

### Pillar 3: Recompensa a la Paciencia

Las unidades simples de hoy son las piezas clave del mañana. El sistema
de traits y sinergias hace que personajes aparentemente débiles se vuelvan
esenciales en ciertas composiciones.

*Design test*: Si el jugador nunca tiene razón para conservar una unidad
de baja rareza, el sistema de traits/sinergias está fallando.

### Pillar 4: Respeto al Tiempo del Jugador

Auto para lo fácil, estrategia para lo que importa. El jugador nunca
debería sentir que pierde el tiempo en contenido trivial, pero el contenido
difícil debe exigir su atención completa.

*Design test*: Si el jugador necesita farmear manualmente algo repetitivo,
falta un auto. Si el contenido difícil se puede hacer en auto, falta
dificultad.

### Anti-Pillars (What This Game Is NOT)

- **NOT pay-to-win**: Los desafíos se diseñan para ser vencibles con
  unidades accesibles. Gastar dinero acelera la progresión pero no
  desbloquea poder inalcanzable.
- **NOT un muro de texto**: La narrativa debe ser concisa y visual. Si una
  escena necesita más de 2 minutos de lectura, hay que recortarla o hacerla
  interactiva.
- **NOT coleccionismo vacío**: Cada personaje tiene un rol mecánico claro en
  tierra, en mar, o en ambos. No hay unidades decorativas.

---

## Inspiration and References

| Reference | What We Take From It | What We Do Differently | Why It Matters |
| ---- | ---- | ---- | ---- |
| Final Fantasy Brave Exvius | Combate por turnos profundo, sistema de habilidades extenso, estructura de stages, eventos semanales | Combate naval como segundo layer, historia más concisa y visual, sinergias por traits | Valida que el core loop de RPG gacha por turnos funciona a largo plazo |
| Summoners War | Sinergias entre unidades, unidades de baja rareza viables | Sistema de roles navales, doble set de habilidades por unidad | Valida que las sinergias mantienen el engagement del coleccionismo |
| Epic Seven | Presentación visual de alta calidad, animaciones de habilidades impactantes | Pixel art chibi en lugar de anime HD, combate naval como diferenciador | Valida que la presentación visual eleva la experiencia gacha |

**Non-game inspirations**: One Piece (fantasía pirata con tripulaciones
diversas y carismáticas), Piratas del Caribe (batallas navales épicas),
la era dorada de la piratería (ambientación y facciones).

---

## Target Player Profile

| Attribute | Detail |
| ---- | ---- |
| **Age range** | 18-35 |
| **Gaming experience** | Mid-core (familiarizado con gachas y RPGs) |
| **Time availability** | 15-60 min por sesión, sesiones diarias cortas entre semana, más largas en fin de semana |
| **Platform preference** | Mobile como plataforma principal, web como secundaria |
| **Current games they play** | FFBE, Summoners War, Epic Seven, Genshin Impact |
| **What they're looking for** | Un gacha con profundidad estratégica real que no sea solo auto-battle, donde los personajes importen más allá de sus stats |
| **What would turn them away** | Pay-to-win agresivo, historia excesivamente larga/aburrida, falta de contenido desafiante |

---

## Technical Considerations

| Consideration | Assessment |
| ---- | ---- |
| **Recommended Engine** | Unity — familiaridad del developer, excelente soporte mobile/WebGL, ecosistema maduro para 2D |
| **Key Technical Challenges** | Sistema de sinergias por traits (combinatoria compleja), doble set de habilidades por unidad, barra de iniciativa dinámica |
| **Art Style** | Pixel art chibi (gameplay) + Estilo anime (cinemáticas/retratos) |
| **Art Pipeline Complexity** | Medium — pixel art sprites + animaciones, retratos anime para cinemáticas estilo visual novel |
| **Audio Needs** | Moderate — OST temática pirata, SFX de combate, voicelines mínimos |
| **Networking** | Mínimo — lista de amigos y unidad prestada (cliente-servidor ligero) |
| **Content Volume** | Demo: 5-10 stages terrestres, 2-3 stages navales, 8-12 unidades, 2-3 barcos |
| **Procedural Systems** | Ninguno en la demo. Potencial futuro para generación de stages de eventos |

---

## Risks and Open Questions

### Design Risks

- El combate naval podría sentirse como un "mini-juego" separado en vez de
  un pilar del juego si no se integra bien con la progresión general
- Las sinergias por traits podrían ser demasiado opacas para jugadores
  nuevos si no se comunican claramente en la UI
- El balance entre auto-battle y contenido manual es delicado: demasiado
  auto aburre, demasiado manual agota

### Technical Risks

- El sistema de traits con múltiples bonificaciones condicionales puede
  ser difícil de escalar y debuggear
- La compilación WebGL de Unity puede tener limitaciones de rendimiento
  para los efectos visuales deseados
- Es el primer proyecto de gamedev del developer — la curva de
  aprendizaje de Unity es un riesgo de timeline

### Market Risks

- El mercado gacha está dominado por títulos establecidos con presupuestos
  masivos (Genshin, Star Rail)
- La temática pirata en gacha no tiene muchos precedentes exitosos —
  puede ser diferenciador o nicho demasiado pequeño

### Scope Risks

- El doble sistema de combate (tierra + naval) duplica el trabajo de
  balance, UI, y testing
- Las cinemáticas estilo anime requieren assets de alta calidad que un
  developer solo puede no poder producir

### Open Questions

- Cómo resolver la narrativa para que no se skipee? (Prototipar formatos:
  visual novel, narrativa integrada en gameplay, storytelling ambiental)
- Cuál es el modelo de monetización específico del gacha? (Rates, pity
  system, monedas premium vs. gratis)
- Cómo funciona exactamente la barra de iniciativa con la mecánica de
  "romper el límite de acciones"? (Prototipar en combate terrestre primero)
- Cuántos roles navales habrá y cómo se balancean? (Empezar con 3-4 roles
  básicos en la demo)

---

## MVP Definition

**Core hypothesis**: "El combate por turnos con barra de iniciativa es
divertido y las sinergias de traits en el combate naval añaden una capa
estratégica que diferencia este juego de otros gachas RPG."

**Required for MVP (Demo)**:

1. Combate terrestre por turnos con barra de iniciativa (5+1 unidades)
2. Combate naval con barco, slots de rol y sinergias de traits
3. Sistema gacha funcional (invocación de unidades y barcos)
4. Progresión básica (niveles, awakening, duplicados)
5. 5-10 stages terrestres + 2-3 stages navales
6. 8-12 unidades con doble set de habilidades (tierra + mar)
7. 2-3 barcos con stats y habilidades propias
8. Historia placeholder (concisa, estilo visual novel)
9. Auto-battle para stages completados

**Explicitly NOT in MVP** (defer to later):

- Eventos semanales / contenido recurrente
- PvP
- Historia definitiva / narrativa profunda
- Marina como facción enemiga
- Networking / sistema de amigos real (simular con unidad prestada estática)
- Cinemáticas anime completas (usar formato visual novel)

### Scope Tiers (if budget/time shrinks)

| Tier | Content | Features | Notes |
| ---- | ---- | ---- | ---- |
| **MVP** | 5 stages terrestres, 6 unidades, 1 barco | Combate terrestre + gacha de unidades | Valida si el core loop terrestre es divertido |
| **Demo v1** | +3 stages navales, +4 unidades, +2 barcos | + Combate naval + gacha de barcos | Valida el diferenciador naval |
| **Demo v2** | +2 stages terrestres, +2 unidades | + Awakening, duplicados, auto-battle | Valida la progresión |
| **Full Vision** | Cientos de stages, 100+ unidades, 20+ barcos | Eventos, PvP, historia completa, guilds | Juego completo post-demo |

---

## Next Steps

- [ ] Configurar Unity como motor del proyecto (`/setup-engine unity`)
- [ ] Validar completitud del concepto (`/design-review design/gdd/game-concept.md`)
- [ ] Descomponer el concepto en sistemas individuales (`/map-systems`)
- [ ] Diseñar cada sistema en detalle (`/design-system`)
- [ ] Crear primera decisión arquitectónica (`/architecture-decision`)
- [ ] Prototipar el combate terrestre (`/prototype combat-terrestre`)
- [ ] Validar el prototipo con playtest (`/playtest-report`)
- [ ] Planificar el primer sprint (`/sprint-plan new`)
