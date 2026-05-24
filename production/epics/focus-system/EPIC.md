# Epic: 专注值系统

> **Layer**: Core
> **GDD**: design/gdd/focus-system.md
> **Architecture Module**: 专注值系统 (IFocusSystem)
> **Status**: Ready
> **Stories**: Not yet created — run `/create-stories focus-system`

## Overview

实现专注值积累和解锁阈值系统。每次攻击命中获得专注值，积累到阈值时触发 OnFocusReady 事件，通知技能抽取系统。阈值随已解锁技能数递增（公式驱动）。每局重置。专注值进度通过 OnFocusChanged 事件通知 HUD 显示进度条。纯计算模块，无引擎 API 依赖。

## Governing ADRs

| ADR | Decision Summary | Engine Risk |
|-----|-----------------|-------------|
| ADR-0009: Focus & Skill Draw Pipeline | 专注值积累、阈值公式、解锁事件链、每局重置 | LOW |

## GDD Requirements

| TR Range | Count | ADR Coverage |
|----------|-------|--------------|
| TR-FOC-001 ~ TR-FOC-023 | 23 | ADR-0009 |

Full requirement list: `docs/architecture/tr-registry.yaml` (FOC section)

## Existing Code

| File | Type |
|------|------|
| Core/Data/FocusRuntimeState.cs | Data struct |
| Core/Formulas/FocusFormulas.cs | Formula implementation |
| Core/Interfaces/IFocusSystem.cs | Interface |
| Tests/Core/FocusFormulasTests.cs | Unit tests |

**Status**: 公式逻辑和单元测试已完成，运行时系统集成待开发。

## Stories

| # | Story | Type | Estimate | Status | Test File |
|---|-------|------|----------|--------|-----------|
| 1 | [Focus Accumulation](story-001-focus-accumulation.md) | Logic | M | Ready | focus_accumulation_test.cs |
| 2 | [Threshold Unlock Event](story-002-threshold-unlock-event.md) | Logic | M | Ready | threshold_unlock_event_test.cs |
| 3 | [Round Reset State](story-003-round-reset-state.md) | Logic | S | Ready | round_reset_state_test.cs |
| 4 | [Edge Cases](story-004-edge-cases.md) | Logic | S | Ready | focus_edge_cases_test.cs |

**Total**: 4 stories | Estimate: 2M + 2S

## Definition of Done

This epic is complete when:
- All stories are implemented, reviewed, and closed via `/story-done`
- All acceptance criteria from `design/gdd/focus-system.md` are verified
- All Logic and Integration stories have passing test files in `tests/`
- All Visual/Feel and UI stories have evidence docs with sign-off in `production/qa/evidence/`

## Next Step

Run `/create-stories focus-system` to break this epic into implementable stories.
