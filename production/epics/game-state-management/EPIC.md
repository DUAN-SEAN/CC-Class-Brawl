# Epic: 游戏状态管理

> **Layer**: Foundation
> **GDD**: design/gdd/game-state-management.md
> **Architecture Module**: 游戏状态管理 (IGameState)
> **Status**: Ready
> **Stories**: 5 stories (4 new)

## Overview

实现全局游戏状态 FSM（Menu → CharacterSelect → Countdown → Battle → BattleEnd → Results）和场景管理。使用双场景架构（MenuScene + GameScene），通过 async load/unload 切换。管理 PlayerSlot 数组，处理手柄配对和角色分配。是整个游戏的状态根权威，无上游依赖。

## Governing ADRs

| ADR | Decision Summary | Engine Risk |
|-----|-----------------|-------------|
| ADR-0007: Scene & Game State Management | 双场景架构、GamePhase FSM、PlayerSlot 管理、SignalRoundEnd 接口 | LOW |
| ADR-0008: Event Architecture | C# Event Delegates 跨系统事件通信 | LOW |

## GDD Requirements

| TR Range | Count | ADR Coverage |
|----------|-------|--------------|
| TR-GST-001 ~ TR-GST-020 | 20 | ADR-0007, 0008 |

Full requirement list: `docs/architecture/tr-registry.yaml` (GST section)

## Existing Code

| File | Type |
|------|------|
| Foundation/Enums/GamePhase.cs | Enum |
| Foundation/Data/PlayerSlot.cs | Data struct |
| Foundation/Interfaces/IGameState.cs | Interface |

**Status**: 数据结构和接口已定义，运行时 MonoBehaviour 实现待开发。

## Stories

| # | Story | Type | Estimate | Status | Depends On |
|---|-------|------|----------|--------|------------|
| 001 | [GamePhase FSM](story-001-gamephase-fsm.md) | Logic | M | Ready | None |
| 002 | [Scene Management](story-002-scene-management.md) | Integration | M | Ready | 001 |
| 003 | [Player Slot Management](story-003-player-slot-management.md) | Logic | M | Ready | 001, 002 |
| 004 | [Countdown & Input Freeze](story-004-countdown-input-freeze.md) | Logic | S | Ready | 001, 002, 003 |
| 005 | [BattleEnd & Results Transition](story-005-battleend-results-transition.md) | Integration | M | Ready | 001, 002, 003, 004 |

## Definition of Done

This epic is complete when:
- All stories are implemented, reviewed, and closed via `/story-done`
- All acceptance criteria from `design/gdd/game-state-management.md` are verified
- All Logic and Integration stories have passing test files in `tests/`
- All Visual/Feel and UI stories have evidence docs with sign-off in `production/qa/evidence/`

## Next Step

Run `/create-stories game-state-management` to break this epic into implementable stories.
