# Epic: 技能装备管理

> **Layer**: Feature
> **GDD**: design/gdd/skill-equipment-management.md
> **Architecture Module**: 技能装备管理 (ISkillEquipmentManager)
> **Status**: Ready
> **Stories**: Not yet created — run `/create-stories skill-equipment`

## Overview

实现 4 个技能槽的管理和技能激活机制。新技能从抽取系统获得后自动装备到空槽位（或替换最旧技能）。装备时将技能的 StateDefinition 动态注册到格斗状态机，映射到 Skill1-4 输入。激活时通过攻击系统发动技能攻击。每局结束或回合重置时清理所有槽位。

## Governing ADRs

| ADR | Decision Summary | Engine Risk |
|-----|-----------------|-------------|
| ADR-0002: Dual FSM Architecture | RegisterState/DeregisterAllSkillStates 动态状态注册 | LOW |
| ADR-0004: Skill System Data-Driven | StateDefinition readonly struct、技能数据驱动 | LOW |

## GDD Requirements

| TR Range | Count | ADR Coverage |
|----------|-------|--------------|
| TR-SEQ-001 ~ TR-SEQ-035 | 35 | ADR-0002, 0004 |

Full requirement list: `docs/architecture/tr-registry.yaml` (SEQ section)

## Existing Code

| File | Type |
|------|------|
| Feature/Enums/SlotState.cs | Enum |
| Feature/Data/SkillSlot.cs | Data struct |
| Feature/Interfaces/ISkillEquipmentManager.cs | Interface |

**Status**: 数据结构和接口已定义，槽位管理和 FSM 注册逻辑待开发。

## Definition of Done

This epic is complete when:
- All stories are implemented, reviewed, and closed via `/story-done`
- All acceptance criteria from `design/gdd/skill-equipment-management.md` are verified
- All Logic and Integration stories have passing test files in `tests/`
- All Visual/Feel and UI stories have evidence docs with sign-off in `production/qa/evidence/`

## Stories

| # | Story | Type | Layer | Estimate | Status | Dependencies |
|---|-------|------|-------|----------|--------|--------------|
| 1 | [SkillSlot 管理](story-001-skill-slot-management.md) | Logic | Feature | 2h | Ready | SkillData |
| 2 | [装备逻辑](story-002-equipment-logic.md) | Logic | Feature | 3h | Ready | Story 1, ISkillDrawSystem |
| 3 | [FSM 注册](story-003-fsm-registration.md) | Integration | Feature | 3h | Ready | Story 2, ICombatStateProvider |
| 4 | [输入映射](story-004-input-mapping.md) | Logic | Feature | 2h | Ready | Story 3, CombatFSM |
| 5 | [对局重置协调](story-005-round-reset-coordination.md) | Integration | Feature | 2h | Ready | Stories 1-3, MatchManager |

**Total Estimate**: 12 hours

## Next Step

Stories created. Begin implementation with Story 001 (SkillSlot 管理).
