# Combat Prototype v1 (S1-07)

> **Status**: Concluded
> **Created**: 2026-04-05
> **Concluded**: 2026-04-06

## Hypothesis

The SPD-based initiative bar turn order system combined with the master damage
formula (ATK×1.8 - DEF×1.0, elemental modifiers, crits) produces a satisfying
turn-based combat loop where player choices (attack/ability/guard/pass) and
team composition matter.

## How to Run

1. Open the Unity project in Unity 6.3 LTS
2. Open any scene (or create a new Basic scene)
3. Create an empty GameObject in the Hierarchy
4. Add Component > Prototypes > Combat Prototype v1
5. Press Play

The prototype uses IMGUI (OnGUI) — no scene setup or prefabs required.

## What It Tests

- **3v2 combat**: 3 allies (Elena Storm, Bones McCoy, Red Molly) vs 2 enemies
  (Pirata Esqueleto, Capitán Fantasma boss)
- **Initiative Bar**: SPD-based turn ordering with boss tie-breaking
- **Actions**: Attack (neutral physical), Ability (elemental, varied power),
  Guard (50% damage reduction), Pass
- **Damage formula**: Full master formula from DSE GDD — ATK/DEF multipliers,
  elemental advantage (pentagonal + Luz/Sombra), crits, variance
- **Victory/defeat**: Battle ends when all enemies or all allies are KO'd
- **Restart**: Button to replay after battle ends

## Findings

1. **Core loop works**: Turn order → choose action → resolve damage → check
   victory is satisfying and functional
2. **Elemental advantage matters**: 1.25x/0.75x is noticeable without being
   overwhelming — players will want to consider team composition
3. **Guard is useful**: 50% reduction makes it a real tactical choice against
   bosses or when a unit is low HP
4. **Boss tie-breaking feels fair**: The boss acting first on SPD ties adds
   tension without feeling unfair
5. **Damage scaling is correct**: Verified against all GDD worked examples
   (early game, late game, naval)

## Known Limitations

- No MP cost for abilities
- No status effects in combat actions (stun/sleep exist in InitiativeBar but
  aren't triggered by abilities)
- Enemy AI is random (40% ability / 60% attack, random target)
- No Limit Break activation in prototype
- IMGUI rendering — not representative of final UI
- No sound or visual feedback beyond the battle log
