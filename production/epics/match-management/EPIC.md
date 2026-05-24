# Epic: 对局管理系统

> **Layer**: Feature
> **GDD**: design/gdd/match-management-system.md
> **Architecture Module**: 对局管理系统 (IMatchManager)
> **Status**: Ready
> **Stories**: Not yet created — run `/create-stories match-management`

## Overview

实现对局生命周期管理：回合追踪、比分记录、胜利条件判定。支持 Bo1/Bo3/Bo5 格式。当击退系统触发 KO 时，对局管理接收事件，判定回合胜负，决定是继续下一回合（重置双方状态）还是结束对局（通知游戏状态管理进入 Results）。回合重置需要协调多个系统的状态清理。

## Governing ADRs

| ADR | Decision Summary | Engine Risk |
|-----|-----------------|-------------|
| ADR-0010: Match & Round Lifecycle | MatchPhase FSM、Bo1/3/5 格式、SignalRoundEnd 接口、回合重置协调 | LOW |

## GDD Requirements

| TR Range | Count | ADR Coverage |
|----------|-------|--------------|
| TR-MCH-001 ~ TR-MCH-022 | 22 | ADR-0010 |

Full requirement list: `docs/architecture/tr-registry.yaml` (MCH section)

## Existing Code

| File | Type |
|------|------|
| Feature/Enums/MatchPhase.cs | Enum |
| Feature/Data/MatchConfig.cs | Data struct |
| Feature/Data/MatchState.cs | Data struct |
| Feature/Formulas/MatchFormulas.cs | Formula implementation |
| Feature/Interfaces/IMatchManager.cs | Interface |
| Tests/Feature/MatchFormulasTests.cs | Unit tests |

**Status**: 公式逻辑和单元测试已完成，运行时 FSM 和跨系统协调待开发。

## Definition of Done

This epic is complete when:
- All stories are implemented, reviewed, and closed via `/story-done`
- All acceptance criteria from `design/gdd/match-management-system.md` are verified
- All Logic and Integration stories have passing test files in `tests/`
- All Visual/Feel and UI stories have evidence docs with sign-off in `production/qa/evidence/`

## Stories

| # | Story | Type | Layer | Estimate | Status | Dependencies |
|---|-------|------|-------|----------|--------|--------------|
| 1 | [MatchPhase FSM](story-001-match-phase-fsm.md) | Logic | Feature | 3h | Ready | IGameState, MatchFormulas |
| 2 | [比分追踪 + 胜利条件](story-002-score-tracking.md) | Logic | Feature | 3h | Ready | Story 1, MatchFormulas |
| 3 | [回合生命周期](story-003-round-lifecycle.md) | Integration | Feature | 3h | Ready | Stories 1-2, 各子系统 |
| 4 | [局间重置协调](story-004-inter-round-reset.md) | Integration | Feature | 2h | Ready | Stories 1-3 |

**Total Estimate**: 11 hours

## Next Step

Stories created. Begin implementation with Story 001 (MatchPhase FSM).
