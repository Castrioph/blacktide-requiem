# Vertical Slice Scope — Pirate Gacha RPG Demo

> **Status**: Approved
> **Created**: 2026-03-28
> **Last Updated**: 2026-03-28

## Objetivo

Demostrar que el core loop funciona: combate terrestre + naval + gacha + progresión
básica. El jugador debe poder jugar 30-60 minutos con una experiencia completa.

## Contenido

### Stages

| Tipo | Cantidad | Estructura |
|------|----------|------------|
| Land (Ch1 — intro) | 5-6 batallas en 2 scenes | 1-2 oleadas, tutorial implícito |
| Land (Ch2 — primer arco) | 4-5 batallas en 2-3 scenes | 3-5 oleadas, incluye boss |
| Naval | 3 batallas | Se desbloquean tras Ch1 |
| **Total** | **~13 batallas** | |

### Unidades (8-12)

| Rareza | Cantidad | Rol |
|--------|----------|-----|
| 5★ | 2-3 | Featured banner (1) + off-banner (1-2) |
| 4★ | 3-4 | Sinergias útiles, roles variados |
| 3★ | 3-5 | Traits complementarios, valor naval. Pillar 3 |

### Barcos (2-3)

| Barco | Obtención | Slots | Identidad |
|-------|-----------|-------|-----------|
| Ship 1 | Reward de historia | 5 | Starter balanceado |
| Ship 2 | Crafteable | 6-7 | Especializado combate/velocidad |
| Ship 3 | Crafteable | 6-7 | Complementa Ship 2 |

### Traits (3 para demo)

- **Hijos del Mar** — ATK (land) / FPW (naval)
- **Malditos** — MST (land) / MST (naval)
- **Hierro Viejo** — DEF (land) / HDF (naval)

### Elementos activos

Todos los 7: Pólvora, Tormenta, Maldición, Bestia, Acero, Luz, Sombra.

## Sistemas requeridos para el Vertical Slice

### MVP Tier (14 sistemas — todos diseñados)

| Sistema | Scope para demo |
|---------|-----------------|
| Unit Data Model | Completo |
| Ship Data Model | Completo |
| Damage & Stats Engine | Completo |
| Currency System | GDC + Doblones (craft materials) |
| Game Flow | Menú → Stage Select → Combat → Rewards |
| Initiative Bar | Completo |
| Enemy System | 3 tiers (Normal, Elite, Jefe), perfiles AI básicos |
| Stage System | 2 chapters + naval mode, sin energía |
| Traits/Sinergias | 3 traits funcionales |
| Combate Terrestre | Completo |
| Combate Naval | Completo |
| Team Composition | 5 presets terrestre + 5 naval |
| Sistema Gacha | 1 banner, pity/50-50 funcional |
| Combat UI | Framework adaptativo completo |

### Vertical Slice Tier (7 sistemas — diseño pendiente)

| Sistema | Scope para demo |
|---------|-----------------|
| Progresión de Unidades | Niveles + awakening básico |
| Rewards System | Drops de stage + first clear |
| Save/Load | Persistencia local |
| Unit Roster/Inventory | Ver/filtrar/equipar unidades |
| Menus & Navigation UI | Stage select, gacha screen, roster |
| Auto-Battle | Toggle on/off en stages ya cleared |
| Narrative System | Visual novel entre stages (placeholder text) |

## Explícitamente FUERA del demo

- Equipment System (diferido)
- Tutorial/Onboarding formal
- Eventos semanales / contenido recurrente
- PvP
- Gacha de barcos
- Networking / sistema de amigos real (simular con unidad prestada estática)
- Energía

## Criterio de éxito

El jugador puede: obtener unidades por gacha → subirlas de nivel → armar equipo
terrestre → completar Ch1 → desbloquear naval → armar tripulación → completar
3 stages navales → sentir que quiere seguir jugando.
