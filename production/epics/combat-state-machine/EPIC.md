# Epic: 格斗状态机

> **Layer**: Core
> **GDD**: design/gdd/combat-state-machine.md
> **Architecture Module**: 格斗状态机 (ICombatStateProvider)
> **Status**: Ready
> **Stories**: Not yet created — run `/create-stories combat-state-machine`

## Overview

实现角色战斗状态 FSM（Idle/Attacking/HitStun/Knockback/LandingLag/Dashlag/Shielding/Grabbed），与 3C 的 Movement FSM 并行运行。管理攻击帧计数器、8 帧输入缓冲、取消规则表、以及动态技能状态注册/注销。是战斗系统的核心调度器，所有攻击和技能的状态流转都由它驱动。

## Governing ADRs

| ADR | Decision Summary | Engine Risk |
|-----|-----------------|-------------|
| ADR-0002: Dual FSM Architecture | Movement + Combat 双 FSM 并行，CombatState 枚举，状态转换优先级 | LOW |
| ADR-0005: Input System | Input buffer 8 帧，技能输入映射 Skill1-4 | LOW |

## GDD Requirements

| TR Range | Count | ADR Coverage |
|----------|-------|--------------|
| TR-CBT-001 ~ TR-CBT-044 | 44 | ADR-0002, 0005 |

Full requirement list: `docs/architecture/tr-registry.yaml` (CBT section)

## Existing Code

| File | Type |
|------|------|
| Core/Enums/CombatState.cs | Enum |
| Core/Enums/AttackPhase.cs | Enum |
| Core/Data/CancelEntry.cs | Data struct |
| Core/Data/StateDefinition.cs | readonly struct |
| Core/Interfaces/ICombatStateProvider.cs | Interface |

**Status**: 数据结构和接口已定义，运行时 FSM 实现待开发。

## Definition of Done

This epic is complete when:
- All stories are implemented, reviewed, and closed via `/story-done`
- All acceptance criteria from `design/gdd/combat-state-machine.md` are verified
- All Logic and Integration stories have passing test files in `tests/`
- All Visual/Feel and UI stories have evidence docs with sign-off in `production/qa/evidence/`

## Next Step

Run `/create-stories combat-state-machine` to break this epic into implementable stories.
