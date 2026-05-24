# Epic: 技能抽取系统

> **Layer**: Feature
> **GDD**: design/gdd/skill-draw-system.md
> **Architecture Module**: 技能抽取系统 (ISkillDrawSystem)
> **Status**: Ready
> **Stories**: Not yet created — run `/create-stories skill-draw`

## Overview

实现专注值满时的随机技能抽取机制。使用 DrawState FSM（Idle → Drawing → Presenting → Done）管理抽取流程。从技能数据库构建合格技能池（排除已有技能），按稀有度权重进行无放回加权随机选择，生成候选列表供玩家选择。抽取过程中游戏进入短暂暂停（仪式感时刻）。

## Governing ADRs

| ADR | Decision Summary | Engine Risk |
|-----|-----------------|-------------|
| ADR-0009: Focus & Skill Draw Pipeline | DrawState FSM、加权随机算法、候选选择、超时机制 | LOW |

## GDD Requirements

| TR Range | Count | ADR Coverage |
|----------|-------|--------------|
| TR-SKW-001 ~ TR-SKW-033 | 33 | ADR-0009 |

Full requirement list: `docs/architecture/tr-registry.yaml` (SKW section)

## Existing Code

| File | Type |
|------|------|
| Feature/Enums/DrawPhase.cs | Enum |
| Feature/Data/DrawRuntimeState.cs | Data struct |
| Feature/Formulas/DrawFormulas.cs | Formula implementation |
| Feature/Interfaces/ISkillDrawSystem.cs | Interface |
| Tests/Feature/DrawFormulasTests.cs | Unit tests |

**Status**: 公式逻辑和单元测试已完成，运行时 FSM 和 UI 集成待开发。

## Definition of Done

This epic is complete when:
- All stories are implemented, reviewed, and closed via `/story-done`
- All acceptance criteria from `design/gdd/skill-draw-system.md` are verified
- All Logic and Integration stories have passing test files in `tests/`
- All Visual/Feel and UI stories have evidence docs with sign-off in `production/qa/evidence/`

## Stories

| # | Story | Type | Layer | Estimate | Status | Dependencies |
|---|-------|------|-------|----------|--------|--------------|
| 1 | [DrawState FSM](story-001-draw-state-fsm.md) | Logic | Feature | 3h | Ready | FocusSystem, ISkillDatabase |
| 2 | [合格牌池构建](story-002-eligible-pool-construction.md) | Logic | Feature | 2h | Ready | ISkillDatabase, Story 1 |
| 3 | [加权随机选择算法](story-003-weighted-random-selection.md) | Logic | Feature | 3h | Ready | DrawFormulas, Story 2 |
| 4 | [抽取触发](story-004-draw-trigger.md) | Integration | Feature | 2h | Ready | Stories 1-3 |
| 5 | [抽取超时安全](story-005-draw-timeout-safety.md) | Logic | Feature | 2h | Ready | Story 1 |

**Total Estimate**: 12 hours

## Next Step

Stories created. Begin implementation with Story 001 (DrawState FSM).
