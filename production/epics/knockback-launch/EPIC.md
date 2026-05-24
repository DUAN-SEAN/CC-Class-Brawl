# Epic: 击退与击飞系统

> **Layer**: Core
> **GDD**: design/gdd/knockback-launch-system.md
> **Architecture Module**: 击退与击飞系统 (IKnockbackSystem)
> **Status**: Ready
> **Stories**: Not yet created — run `/create-stories knockback-launch`

## Overview

实现击退向量计算、击退物理模拟和 KO 检测。击退方向由命中点到目标中心的方向决定，击退力由伤害计算系统提供。击退期间使用速度衰减曲线（KnockbackDecayRate）。每帧检测角色是否超出 Blast Zone 边界，超出时触发 KO 事件。击退状态由格斗状态机管理。

## Governing ADRs

| ADR | Decision Summary | Engine Risk |
|-----|-----------------|-------------|
| ADR-0006: Damage & Knockback Pipeline | 击退向量公式、KO 检测、速度衰减、Blast Zone 检查 | LOW |

## GDD Requirements

| TR Range | Count | ADR Coverage |
|----------|-------|--------------|
| TR-KBL-001 ~ TR-KBL-035 | 35 | ADR-0006 |

Full requirement list: `docs/architecture/tr-registry.yaml` (KBL section)

## Existing Code

| File | Type |
|------|------|
| Core/Data/KnockbackRuntimeState.cs | Data struct |
| Core/Formulas/KnockbackFormulas.cs | Formula implementation |
| Core/Interfaces/IKnockbackSystem.cs | Interface |
| Tests/Core/KnockbackFormulasTests.cs | Unit tests |

**Status**: 公式逻辑和单元测试已完成，运行时物理模拟和 KO 检测待开发。

## Stories

| # | Story | Type | Estimate | Status | Test File |
|---|-------|------|----------|--------|-----------|
| 1 | [Knockback Vector Calculation](story-001-knockback-vector-calculation.md) | Logic | M | Ready | knockback_vector_calculation_test.cs |
| 2 | [Knockback Physics](story-002-knockback-physics.md) | Logic | M | Ready | knockback_physics_test.cs |
| 3 | [KO Detection](story-003-ko-detection.md) | Logic | S | Ready | ko_detection_test.cs |
| 4 | [Knockback State Management](story-004-knockback-state-management.md) | Integration | M | Ready | knockback_state_management_test.cs |

**Total**: 4 stories | Estimate: 3M + 1S

## Definition of Done

This epic is complete when:
- All stories are implemented, reviewed, and closed via `/story-done`
- All acceptance criteria from `design/gdd/knockback-launch-system.md` are verified
- All Logic and Integration stories have passing test files in `tests/`
- All Visual/Feel and UI stories have evidence docs with sign-off in `production/qa/evidence/`

## Next Step

Run `/create-stories knockback-launch` to break this epic into implementable stories.
