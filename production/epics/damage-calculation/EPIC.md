# Epic: 伤害计算系统

> **Layer**: Core
> **GDD**: design/gdd/damage-calculation-system.md
> **Architecture Module**: 伤害计算系统 (IDamageSystem)
> **Status**: Ready
> **Stories**: Not yet created — run `/create-stories damage-calculation`

## Overview

实现百分比伤害累积和击退力计算。伤害公式为纯计算模块，无引擎 API 依赖。DamagePercent 从 0% 累积，每次命中增加攻击的 BaseDamage。击退力公式使用 BaseKnockbackGrowth × (DamagePercent/100) × BaseKnockback + BaseKnockback。通过 OnDamagePercentChanged 事件通知 HUD 和专注值系统。

## Governing ADRs

| ADR | Decision Summary | Engine Risk |
|-----|-----------------|-------------|
| ADR-0006: Damage & Knockback Pipeline | 伤害公式、击退力计算、累积机制、事件广播 | LOW |

## GDD Requirements

| TR Range | Count | ADR Coverage |
|----------|-------|--------------|
| TR-DMG-001 ~ TR-DMG-030 | 30 | ADR-0006 |

Full requirement list: `docs/architecture/tr-registry.yaml` (DMG section)

## Existing Code

| File | Type |
|------|------|
| Core/Formulas/DamageFormulas.cs | Formula implementation |
| Core/Interfaces/IDamageSystem.cs | Interface |
| Tests/Core/DamageFormulasTests.cs | Unit tests |

**Status**: 公式逻辑和单元测试已完成，运行时系统集成待开发。

## Stories

| # | Story | Type | Estimate | Status | Test File |
|---|-------|------|----------|--------|-----------|
| 1 | [Damage System Runtime](story-001-damage-system-runtime.md) | Logic | M | Ready | damage_system_runtime_test.cs |
| 2 | [Knockback Magnitude Integration](story-002-knockback-magnitude-integration.md) | Integration | S | Ready | knockback_magnitude_integration_test.cs |
| 3 | [Round Reset](story-003-round-reset.md) | Logic | S | Ready | round_reset_test.cs |
| 4 | [Edge Cases](story-004-edge-cases.md) | Logic | S | Ready | edge_cases_test.cs |

**Total**: 4 stories | Estimate: 2M + 2S

## Definition of Done

This epic is complete when:
- All stories are implemented, reviewed, and closed via `/story-done`
- All acceptance criteria from `design/gdd/damage-calculation-system.md` are verified
- All Logic and Integration stories have passing test files in `tests/`
- All Visual/Feel and UI stories have evidence docs with sign-off in `production/qa/evidence/`

## Next Step

Run `/create-stories damage-calculation` to break this epic into implementable stories.
