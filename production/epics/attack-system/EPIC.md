# Epic: 攻击系统

> **Layer**: Core
> **GDD**: design/gdd/attack-system.md
> **Architecture Module**: 攻击系统 (IAttackSystem)
> **Status**: Ready
> **Stories**: 7 stories (6 new)

## Overview

实现攻击全生命周期：发动（startup）→ 活跃（active）→ 恢复（recovery），包括 hitbox 启用/禁用、命中目标集合（防重复命中）、Hitstop 暂停帧、投射物对象池。攻击数据统一使用 AttackData struct，支持基础攻击和技能攻击的统一处理。取消规则由格斗状态机管理。

## Governing ADRs

| ADR | Decision Summary | Engine Risk |
|-----|-----------------|-------------|
| ADR-0003: Hitbox/Hurtbox Detection | Hitbox 生命周期、Trigger 检测、命中去重 | LOW |
| ADR-0004: Skill System Data-Driven | AttackData 统一数据结构、技能注入 | LOW |
| ADR-0013: Projectile System | 投射物对象池、独立 GameObject、生命周期管理 | MEDIUM |

## GDD Requirements

| TR Range | Count | ADR Coverage |
|----------|-------|--------------|
| TR-ATK-001 ~ TR-ATK-036 | 36 | ADR-0003, 0004, 0013 |

Full requirement list: `docs/architecture/tr-registry.yaml` (ATK section)

## Existing Code

| File | Type |
|------|------|
| Core/Enums/AttackType.cs | Enum |
| Core/Enums/AttackPhase.cs | Enum |
| Core/Data/AttackData.cs | Data struct |
| Core/Interfaces/IAttackSystem.cs | Interface |

**Status**: 数据结构和接口已定义，hitbox 生命周期和投射物池实现待开发。

## Stories

| # | Story | Type | Estimate | Status | Depends On |
|---|-------|------|----------|--------|------------|
| 001 | [Attack Lifecycle](story-001-attack-lifecycle.md) | Logic | M | Ready | combat-state-machine |
| 002 | [Hitbox Positioning](story-002-hitbox-positioning.md) | Logic | S | Ready | 001 |
| 003 | [Multi-Hit Prevention](story-003-multi-hit-prevention.md) | Logic | S | Ready | 001, 002 |
| 004 | [Hitstop Implementation](story-004-hitstop-implementation.md) | Logic | M | Ready | 001, 002, 003 |
| 005 | [Attack Type Resolution](story-005-attack-type-resolution.md) | Logic | S | Ready | 001 |
| 006 | [Projectile System](story-006-projectile-system.md) | Logic | L | Ready | 001, 002, 005 |
| 007 | [Projectile Collision](story-007-projectile-collision.md) | Integration | M | Ready | 006, collision 001, collision 002 |

## Definition of Done

This epic is complete when:
- All stories are implemented, reviewed, and closed via `/story-done`
- All acceptance criteria from `design/gdd/attack-system.md` are verified
- All Logic and Integration stories have passing test files in `tests/`
- All Visual/Feel and UI stories have evidence docs with sign-off in `production/qa/evidence/`

## Next Step

Run `/create-stories attack-system` to break this epic into implementable stories.
