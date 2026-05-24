# Directory Structure

```text
/
├── CLAUDE.md                    # Master configuration
├── .claude/                     # Agent definitions, skills, hooks, rules, docs
├── Packages/                    # Unity package manifest (URP, Input System, Test Framework)
├── ProjectSettings/             # Unity project settings (auto-generated on first open)
├── Assets/                      # Unity asset root
│   ├── Scripts/                 # Game source code — 4-layer architecture
│   │   ├── Foundation/          # Layer 0: Enums, Data structs, Constants, Interfaces
│   │   │   └── ClassBrawl.Foundation.asmdef
│   │   ├── Core/                # Layer 1: Combat data, Formulas, System interfaces
│   │   │   └── ClassBrawl.Core.asmdef
│   │   ├── Feature/             # Layer 2: Skill draw, Match management, Equipment
│   │   │   └── ClassBrawl.Feature.asmdef
│   │   └── Tests/               # Edit-mode unit tests (NUnit)
│   │       └── ClassBrawl.Tests.asmdef
│   ├── Scenes/                  # MenuScene.unity, GameScene.unity
│   ├── Art/                     # Sprites, animations
│   ├── Audio/                   # SFX, BGM
│   ├── Data/                    # ScriptableObjects (ClassData, SkillData, ArenaConfig)
│   ├── Prefabs/                 # Character prefabs, UI prefabs
│   ├── Animations/              # Animator controllers
│   ├── Input/                   # Input action assets
│   └── Settings/                # URP pipeline asset
├── design/                      # Game design documents (gdd, narrative, levels, balance)
├── docs/                        # Technical documentation (architecture, api, postmortems)
│   └── engine-reference/        # Curated engine API snapshots (version-pinned)
├── tools/                       # Build and pipeline tools (ci, build, asset-pipeline)
├── prototypes/                  # Throwaway prototypes (isolated from Assets/Scripts/)
└── production/                  # Production management (sprints, milestones, releases)
    ├── session-state/           # Ephemeral session state (active.md — gitignored)
    └── session-logs/            # Session audit trail (gitignored)
```
