# Git Conventions

## Commits — Conventional Commits v1.0.0

Referencia: https://www.conventionalcommits.org/en/v1.0.0/

### Formato

```
<type>(<scope>): <description>

[body]

[footer(s)]
```

### Types

| Type | Cuando usar | SemVer |
|------|------------|--------|
| `feat` | Nueva funcionalidad | MINOR |
| `fix` | Correccion de bug | PATCH |
| `docs` | Solo documentacion (GDDs, ADRs, README) | — |
| `style` | Formato, espacios, sin cambio de logica | — |
| `refactor` | Reestructuracion sin cambiar comportamiento | — |
| `perf` | Mejora de rendimiento | — |
| `test` | Anadir o corregir tests | — |
| `build` | Build system, dependencias, configuracion Unity | — |
| `ci` | CI/CD, pipelines | — |
| `chore` | Tareas internas, limpieza, mantenimiento | — |
| `design` | Documentos de diseno de juego (GDDs, balance) | — |

### Scopes del proyecto

| Scope | Alcance |
|-------|---------|
| `combat` | Combate terrestre y naval |
| `gacha` | Sistema gacha, banners, pity |
| `units` | Unit Data Model, progresion, roster |
| `ships` | Ship Data Model, naval |
| `economy` | Currencies, rewards, tienda |
| `ui` | Menus, HUD, navigation |
| `narrative` | Sistema narrativo, dialogos |
| `equipment` | Sistema de equipamiento |
| `save` | Save/Load, persistencia |
| `stage` | Stage system, contenido |
| `ai` | Enemy AI, auto-battle |
| `audio` | Musica, SFX |
| `art` | Assets visuales, shaders |
| `engine` | Configuracion Unity, URP, project settings |
| `prod` | Sprint plans, milestones, production docs |

### Reglas

1. Description en **imperativo presente** y en **ingles**: "add damage formula",
   no "added" ni "adds"
2. Primera letra de description en **minuscula**
3. Sin punto final en la description
4. Body opcional — explica el **por que**, no el **que**
5. Breaking changes: `!` despues del scope o footer `BREAKING CHANGE:`
6. Maximo 72 caracteres en la primera linea
7. Cada commit referencia la task o GDD relevante cuando aplique

### Ejemplos

```
feat(combat): add initiative bar turn ordering

Implements SPD-based turn calculation with tie-breaking rules
from the Initiative Bar GDD.

Refs: design/gdd/initiative-bar.md
```

```
docs(design): add equipment system GDD
```

```
fix(economy): correct first-clear reward from DOB to GDC
```

```
build(engine): configure URP 2D renderer for Unity 6.3
```

---

## Branches — Conventional Branches

Referencia: https://conventional-branch.github.io/

### Formato

```
<type>/<description>
```

### Types

| Type | Cuando usar |
|------|------------|
| `feat/` | Nueva funcionalidad |
| `fix/` | Correccion de bug |
| `hotfix/` | Fix urgente en produccion |
| `release/` | Preparacion de release |
| `chore/` | Mantenimiento, dependencias, docs |
| `refactor/` | Reestructuracion de codigo |
| `test/` | Tests |

### Reglas

1. Solo **minusculas**, numeros y guiones (`a-z`, `0-9`, `-`)
2. Sin guiones ni puntos consecutivos, ni al inicio/final
3. Branches de release pueden usar puntos para version: `release/v1.2.0`
4. Incluir ID de task cuando aplique: `feat/s1-04-unit-data-model`
5. Nombres descriptivos pero breves

### Ejemplos

```
feat/s1-04-unit-data-model
feat/s1-07-combat-prototype
fix/gacha-pity-counter-reset
chore/update-gitignore-unity
release/v0.1.0-demo
```

### Branch principal

- `main` — rama principal, siempre estable
