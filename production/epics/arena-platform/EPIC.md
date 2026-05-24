# Epic: 场地/平台系统

> **Layer**: Foundation
> **GDD**: design/gdd/arena-platform-system.md
> **Architecture Module**: 场地/平台系统 (IArenaDataProvider)
> **Status**: Ready
> **Stories**: Not yet created — run `/create-stories arena-platform`

## Overview

实现多层平台场地的数据提供和物理配置。包括平台碰撞体（含单向穿越的 PlatformEffector2D）、Blast Zone 边界定义、摄像机活动范围、角色出生点。所有场地数据通过 ArenaConfig ScriptableObject 配置，支持热切换场地。系统为纯数据提供者，无上游依赖。

## Governing ADRs

| ADR | Decision Summary | Engine Risk |
|-----|-----------------|-------------|
| ADR-0003: Hitbox/Hurtbox Detection | SolidPlatform Layer 定义，平台碰撞层矩阵 | LOW |
| ADR-0004: Skill System Data-Driven | ArenaConfig SO 数据结构 | LOW |
| ADR-0011: Arena Platform Architecture | 场地加载/卸载生命周期、平台数据验证 | LOW |

## GDD Requirements

| TR Range | Count | ADR Coverage |
|----------|-------|--------------|
| TR-ARE-001 ~ TR-ARE-025 | 25 | ADR-0003, 0004, 0011 |

Full requirement list: `docs/architecture/tr-registry.yaml` (ARE section)

## Existing Code

| File | Type |
|------|------|
| Foundation/Data/BoundsData.cs | Data struct |
| Foundation/Data/PlatformData.cs | Data struct |
| Foundation/Data/SpawnPointData.cs | Data struct |
| Foundation/Data/ArenaState.cs | Data struct |
| Foundation/Interfaces/IArenaDataProvider.cs | Interface |

**Status**: 数据结构和接口已定义，运行时 MonoBehaviour 实现待开发。

## Definition of Done

This epic is complete when:
- All stories are implemented, reviewed, and closed via `/story-done`
- All acceptance criteria from `design/gdd/arena-platform-system.md` are verified
- All Logic and Integration stories have passing test files in `tests/`
- All Visual/Feel and UI stories have evidence docs with sign-off in `production/qa/evidence/`

## Next Step

Run `/create-stories arena-platform` to break this epic into implementable stories.
