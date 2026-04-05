# ADR-002: Rendering Pipeline — URP 2D Renderer

> **Status**: Accepted
> **Date**: 2026-03-28
> **Deciders**: User + Technical Director agent
> **Sprint**: S1-03

## Context

The game is a 2D turn-based RPG gacha with pixel art chibi sprites (gameplay)
and anime-style portraits (visual novel scenes). Target platforms are mobile
(iOS/Android) and WebGL. The art style is sprite-based with particle effects
for combat abilities.

Unity 6.3 LTS offers three render pipelines: Built-in, URP, and HDRP.

## Decision

**Use Universal Render Pipeline (URP) with the 2D Renderer.**

### Configuration

- **Render Pipeline**: URP (Universal Render Pipeline)
- **Renderer**: 2D Renderer (not Forward/Deferred)
- **Color Space**: Linear (better blending for 2D lighting)
- **Target Resolution**: 1080×1920 (portrait, 9:16) as reference
- **Sprite Atlas**: Enabled (auto-packing for draw call reduction)
- **2D Lighting**: URP 2D lights for ambient and combat effects
- **Post-Processing**: Minimal — bloom for ability effects, vignette for boss phases
- **Anti-Aliasing**: None (pixel art benefits from sharp edges)

### Why URP 2D Renderer specifically

Unity 6.3 LTS URP 2D Renderer now supports mixed 2D+3D rendering (Mesh Renderer
and Skinned Mesh Renderer alongside sprites). This gives us future flexibility
for 3D ship models or particle effects without pipeline changes.

### Sprite Workflow

- **Format**: PNG with transparency
- **Pixels Per Unit**: 16 or 32 (to be determined by art style test)
- **Filter Mode**: Point (no filtering — crisp pixel art)
- **Compression**: ASTC 4×4 (mobile), DXT5 (WebGL fallback)
- **Sprite Atlas**: One atlas per category (units, enemies, ships, UI, effects)

## Alternatives Considered

### Built-in Render Pipeline

- **Rejected**: No 2D lighting system, no SRP Batcher optimizations, no future
  upgrade path. Unity is deprecating Built-in RP for new projects.

### HDRP

- **Rejected**: Designed for high-end 3D. Does not support 2D Renderer. Massive
  overkill for a sprite-based game. Poor mobile/WebGL performance.

### URP Forward Renderer (3D) with sprite workarounds

- **Rejected**: The 2D Renderer is purpose-built for sprite games. Using the
  Forward Renderer for 2D requires manual sorting, no 2D lights, and more
  draw calls. No benefit for a purely 2D game.

## Consequences

### Positive

- **Optimized for our use case**: 2D Renderer handles sprite sorting, 2D lights,
  and batching automatically
- **Mobile-ready**: URP is Unity's recommended pipeline for mobile
- **WebGL-ready**: URP supports WebGL builds with good performance
- **Future-proof**: Mixed 2D+3D support in Unity 6.3 if we need 3D elements later
- **SRP Batcher**: Automatic draw call batching for materials sharing the same shader

### Negative

- **Limited 3D**: If we ever need heavy 3D rendering, URP 2D Renderer has
  limitations. Unlikely for this project.
- **Custom shaders**: Must use Shader Graph or URP-compatible shaders. Built-in
  shaders won't work. Minor constraint since we're mostly using default 2D sprites.

## Compliance

- **Technical Preferences**: URP confirmed as rendering pipeline ✅
- **Game Concept**: Pixel art chibi + mobile/web target ✅
- **Performance**: URP is lightest pipeline, within mobile budgets ✅
