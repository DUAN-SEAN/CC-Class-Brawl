# Story 002: 装备逻辑 — 首空槽装备 + 空槽检查

## Epic
skill-equipment

## Status
Ready

## Layer
Feature

## Type
Logic

## Estimate
3 hours

## Context
- **GDD**: design/gdd/skill-equipment-management.md — Core Rules 2 (装备流程), Edge Cases (装备流程)
- **ADR**: ADR-0009 Section 4 (HandleSkillDrawn)
- **Existing Code**: Feature/Interfaces/ISkillEquipmentManager.cs
- **TR-IDs**: TR-SEQ-004 (装备流程: 空槽→SkillInstance→RegisterState)
- **Control Manifest**: 2026-05-24

## Acceptance Criteria
- **GIVEN** 角色开局（所有槽位 Empty）, **WHEN** 收到 OnSkillDrawn(P1, Fireball[Common]), **THEN** Slot 1 装备 Fireball，发出 OnSkillEquipped(P1, 1, Fireball)
- **GIVEN** Slot 1 已装备 Fireball, **WHEN** 收到 OnSkillDrawn(P1, ShieldBash[Rare]), **THEN** Slot 2 装备 ShieldBash
- **GIVEN** Slot 1-4 全部已装备, **WHEN** 收到 OnSkillDrawn（不应发生）, **THEN** 返回 SlotIndex=0，不执行装备，记录警告
- **GIVEN** 收到 OnSkillDrawn(P1, null), **THEN** 装备跳过，所有槽位保持不变，记录错误
- **GIVEN** P1 和 P2 都收到 OnSkillDrawn(Fireball), **WHEN** 两个装备完成, **THEN** 各自独立装备到各自 Slot 1，互不影响
- **GIVEN** 装备成功, **WHEN** 装备流程完成, **THEN** 已选中技能加入 AlreadyDrawnSkillIds（由 SkillDrawSystem 处理），未被选中的候选返回牌池

## Implementation Notes
- SkillEquipmentManager 在 OnEnable 中订阅 ISkillDrawSystem.OnSkillDrawn
- HandleSkillDrawn: (1) 验证 SkillData 非 null → (2) FindFirstEmptySlot(1→2→3→4) → (3) 设置 SlotState + SkillData → (4) 创建 StateDefinition → (5) RegisterState → (6) 发出 OnSkillEquipped
- FindFirstEmptySlot 遍历 slots[0..3]，返回第一个 Empty 的索引（+1 转为 1-based），或 0 表示无可用槽
- OnSkillEquipped 事件签名: Action<int, int, SkillData> (playerIndex, slotIndex, skillData)
- OnSkillUnequipped 事件签名: Action<int, int> (playerIndex, slotIndex)
- RegisterState 调用可能失败（如状态名重复），需 try-catch 并回滚槽位状态
- 双人独立: 每人有自己的槽位数组，OnSkillDrawn 带有 playerIndex 参数

## Out of Scope
- 槽位管理基础（Story 001）
- FSM 注册细节（Story 003）
- 输入映射（Story 004）

## QA Test Cases
- test_equip_first_slot: 首次装备 → Slot 1
- test_equip_second_slot: 第二次装备 → Slot 2
- test_equip_all_full: 4 槽满 → 返回 0 + 警告
- test_equip_null_skill: SkillData=null → 跳过 + 错误
- test_equip_dual_player: P1+P2 各自独立装备
- test_equip_event_fired: 装备成功 → OnSkillEquipped 事件触发

## Test Evidence
- 自动化单元测试（Logic story — BLOCKING）
- 测试文件: tests/unit/skill-equipment/EquipmentLogicTests.cs

## Dependencies
- ISkillDrawSystem（上游）: OnSkillDrawn 事件
- ICombatStateProvider（下游）: RegisterState（Story 003 实现细节）
- Story 001（本 Epic）: 槽位数组管理
