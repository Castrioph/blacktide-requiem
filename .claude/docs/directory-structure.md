# Directory Structure

```text
/
├── CLAUDE.md                    # Master configuration
├── .claude/                     # Agent definitions, skills, hooks, rules, docs
├── Assets/                      # Unity Assets (Scripts, Art, Audio, Prefabs, Scenes)
│   ├── Scripts/                 # Game source code (core, gameplay, ai, ui, tools)
│   ├── Art/                     # Sprites, textures, animations
│   ├── Audio/                   # Music, SFX
│   ├── Prefabs/                 # Prefab assets
│   ├── Scenes/                  # Unity scenes
│   ├── Settings/                # URP and render pipeline settings
│   └── Data/                    # ScriptableObjects, config files
├── Packages/                    # Unity package manifest
├── ProjectSettings/             # Unity project settings (tracked)
├── design/                      # Game design documents (gdd, narrative, levels, balance)
├── docs/                        # Technical documentation (architecture, api, postmortems)
│   └── engine-reference/        # Curated engine API snapshots (version-pinned)
├── tests/                       # Test suites (unit, integration, performance, playtest)
├── tools/                       # Build and pipeline tools (ci, build, asset-pipeline)
├── prototypes/                  # Throwaway prototypes (isolated from Assets/)
└── production/                  # Production management (sprints, milestones, releases)
    ├── session-state/           # Ephemeral session state (active.md — gitignored)
    └── session-logs/            # Session audit trail (gitignored)
```

> **Note**: `Library/`, `Temp/`, `Logs/`, `UserSettings/` are Unity-generated
> folders excluded via `.gitignore`. Do not track them.
