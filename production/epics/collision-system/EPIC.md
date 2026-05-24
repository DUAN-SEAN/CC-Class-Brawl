# Epic: 碰撞判定系统

> **Layer**: Core
> **GDD**: design/gdd/collision-system.md
> **Architecture Module**: 碰撞判定系统 (OnHitDetected event)
> **Status**: Ready
> **Stories**: 6 stories

## Overview

实现 hitbox/hurtbox 的物理检测与配对。使用 Unity BoxCollider2D (IsTrigger) + Layer Collision Matrix 实现帧精确的碰撞检测。攻击发起时启用 hitbox，碰撞事件通过 OnHitDetected(HitEvent) 广播，包含攻击者、目标、伤害、击退等完整信息。支持 hurtbox 启用/禁用控制（受击时禁用以避免连续命中）。

## Governing ADRs

| ADR | Decision Summary | Engine Risk |
|-----|-----------------|-------------|
| ADR-0003: Hitbox/Hurtbox Detection | Trigger 检测、Layer Matrix、HitEvent 结构、碰撞管线全流程 | LOW |

## GDD Requirements

| TR Range | Count | ADR Coverage |
|----------|-------|--------------|
| TR-COL-001 ~ TR-COL-037 | 37 | ADR-0003 |

Full requirement list: `docs/architecture/tr-registry.yaml` (COL section)

## Existing Code

| File | Type |
|------|------|
| Core/Data/HitEvent.cs | Data struct |

**Status**: 数据结构已定义，hitbox/hurtbox 检测逻辑和 Layer Matrix 配置待开发。

## Stories

| # | Story | Type | Estimate | Status | Depends On |
|---|-------|------|----------|--------|------------|
| 001 | [Layer Collision Matrix](story-001-layer-collision-matrix.md) | Integration | M | Ready | Foundation epics |
| 002 | [HitEvent Construction](story-002-hitevent-construction.md) | Logic | M | Ready | 001 |
| 003 | [Self-Hit & Multi-Hit Prevention](story-003-self-hit-multi-hit-prevention.md) | Logic | S | Ready | 001, 002 |
| 004 | [Projectile Collision Routing](story-004-projectile-collision-routing.md) | Integration | M | Ready | 001, 002, 003 |
| 005 | [Hurtbox Management](story-005-hurtbox-management.md) | Logic | S | Ready | 001 |
| 006 | [HitPoint Calculation & Validation](story-006-hitpoint-calculation-validation.md) | Logic | S | Ready | 001, 002, 005 |

## Definition of Done

This epic is complete when:
- All stories are implemented, reviewed, and closed via `/story-done`
- All acceptance criteria from `design/gdd/collision-system.md` are verified
- All Logic and Integration stories have passing test files in `tests/`
- All Visual/Feel and UI stories have evidence docs with sign-off in `production/qa/evidence/`

## Next Step

Run `/create-stories collision-system` to break this epic into implementable stories.
