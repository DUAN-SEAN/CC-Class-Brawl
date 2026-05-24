# Story 001: SkillSlot 管理 — 4 槽位数组 + SlotState

## Epic
skill-equipment

## Status
Ready

## Layer
Feature

## Type
Logic

## Estimate
2 hours

## Context
- **GDD**: design/gdd/skill-equipment-management.md — Core Rules 1 (技能槽系统)
- **ADR**: ADR-0009 Section 4 (SkillEquipmentManager), ADR-0002 (StateDefinition)
- **Existing Code**: Feature/Enums/SlotState.cs (Empty/Equipped), Feature/Data/SkillSlot.cs (State + SkillData)
- **TR-IDs**: TR-SEQ-001 (4 SkillSlots, fill order 1→2→3→4)
- **Control Manifest**: 2026-05-24

## Acceptance Criteria
- **GIVEN** 角色初始化, **WHEN** SkillEquipmentManager 初始化完成, **THEN** 4 个 SkillSlot 均为 SlotState.Empty，SkillData = null
- **GIVEN** 所有槽位为空, **WHEN** 查询 GetEquippedCount(playerIndex), **THEN** 返回 0
- **GIVEN** Slot 1-3 已装备, **WHEN** 查询 GetEquippedCount(playerIndex), **THEN** 返回 3
- **GIVEN** Slot 1 已装备 Fireball, **WHEN** 查询 GetSkillInSlot(playerIndex, 1), **THEN** 返回 Fireball 的 SkillData
- **GIVEN** Slot 2 为空, **WHEN** 查询 GetSkillInSlot(playerIndex, 2), **THEN** 返回 null
- **GIVEN** P1 装备了 2 个技能，P2 装备了 3 个技能, **WHEN** 各自独立查询, **THEN** P1 EquippedCount=2, P2 EquippedCount=3
- **GIVEN** 2 人对战、每人 4 个技能, **WHEN** 执行槽位查询, **THEN** 处理时间 < 0.05ms（纯数组遍历）

## Implementation Notes
- SlotState enum 和 SkillSlot struct 已存在
- 创建 SkillEquipmentManager MonoBehaviour 实现 ISkillEquipmentManager
- 持有 SkillSlot[2][4]（2 玩家 x 4 槽位），使用二维数组或 Dictionary<int, SkillSlot[]>
- 槽位索引从 1 开始（GDD 定义），但内部数组从 0 开始，需注意映射
- GetEquippedSkills 返回按槽位顺序排列的 SkillData 列表（空槽位跳过或返回 null）
- 所有查询方法为 O(n) n=4，性能无顾虑

## Out of Scope
- 装备逻辑（Story 002）
- FSM 注册（Story 003）
- 输入映射（Story 004）
- 重置逻辑（Story 005）

## QA Test Cases
- test_slot_initial_state: 初始化 → 4 个 Empty 槽位
- test_slot_equipped_count: 装备后 count 正确
- test_slot_get_skill: 查询已装备槽位 → 正确 SkillData
- test_slot_get_empty: 查询空槽位 → null
- test_slot_independent_players: P1/P2 独立计数

## Test Evidence
- 自动化单元测试（Logic story — BLOCKING）
- 测试文件: tests/unit/skill-equipment/SkillSlotTests.cs

## Dependencies
- SkillData（Core 层）: 槽位引用的技能数据
