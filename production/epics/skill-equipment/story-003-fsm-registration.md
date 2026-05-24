# Story 003: FSM 注册 — RegisterState 装备时注册 + DeregisterAllSkillStates 重置

## Epic
skill-equipment

## Status
Ready

## Layer
Feature

## Type
Integration

## Estimate
3 hours

## Context
- **GDD**: design/gdd/skill-equipment-management.md — Core Rules 2/5 (装备流程, 状态注册规则)
- **ADR**: ADR-0002 (Dual FSM — ICombatStateProvider.RegisterState), ADR-0004 (StateDefinition readonly struct), ADR-0009 Section 4
- **Existing Code**: Feature/Data/SkillSlot.cs, Core/Interfaces/ICombatStateProvider（定义在 ADR-0002 中）
- **TR-IDs**: TR-SEQ-004 (RegisterState), TR-SEQ-015 (DeregisterAllSkillStates)
- **Control Manifest**: 2026-05-24

## Acceptance Criteria
- **GIVEN** Slot 1 装备 Fireball, **WHEN** 装备完成, **THEN** FSM 注册状态 "skill_fireball"，包含 StartupFrames/ActiveFrames/RecoveryFrames/CancelTable
- **GIVEN** Slot 1-3 已装备 3 个技能, **WHEN** 收到 OnMatchEnd, **THEN** 所有 3 个技能状态从 FSM 注销，发出 OnSkillUnequipped(P1, 1/2/3)
- **GIVEN** P1 装备 Fireball 且 FSM 注册成功, **WHEN** 玩家按技能键 1 且 FSM 在 Idle, **THEN** 角色进入 Fireball 的 Startup 帧（验证注册成功）
- **GIVEN** FSM RegisterState 调用失败（状态名重复）, **WHEN** 装备流程执行, **THEN** 装备失败，槽位保持 Empty，发出装备失败事件
- **GIVEN** P1 和 P2 都装备 Fireball, **WHEN** 各自注册, **THEN** 各自 FSM 实例独立注册，SkillId 在同一 FSM 内唯一，跨 FSM 允许重复
- **GIVEN** 装备期间对局结束（OnMatchEnd 到达）, **WHEN** 装备流程中断, **THEN** 执行重置，未完成装备被丢弃

## Implementation Notes
- StateDefinition 为 readonly struct（ADR-0004 定义），从 SkillData.AttackData 创建
- 创建映射: StateId = SkillData.SkillId, StartupFrames = AttackData.StartupFrames, ActiveFrames = AttackData.ActiveFrames, RecoveryFrames = AttackData.RecoveryFrames, CancelTable = AttackData.CancelTable, InputMapping = 对应槽位的输入绑定
- ICombatStateProvider.RegisterState(stateDefinition) 将定义加入 CombatFSM 的 Dictionary
- ICombatStateProvider.DeregisterAllSkillStates() 在对局重置时清除所有技能状态（保留基础攻击）
- 每个 CharacterController 有独立的 CombatFSM 实例 → 独立的 Dictionary → 允许跨角色 SkillId 重复
- RegisterState 失败场景: 状态名已存在（不应发生，SkillDrawSystem 保证不重复），需要错误处理
- 需要注入 ICombatStateProvider[]（每角色一个），在 Initialize 时设置

## Out of Scope
- 槽位管理（Story 001）
- 装备逻辑（Story 002）
- 输入映射细节（Story 004）

## QA Test Cases
- test_register_state_on_equip: 装备后 FSM 字典含对应 StateDefinition
- test_deregister_on_match_end: 对局重置后 FSM 字典无技能状态
- test_register_skill_activation: 注册后技能键可触发对应攻击
- test_register_failure_rollback: 注册失败 → 槽位保持 Empty
- test_register_independent_fsm: P1/P2 各自 FSM 独立
- test_register_interrupt_on_reset: 装备中断时正确回滚

## Test Evidence
- 自动化集成测试（Integration story — BLOCKING）
- 测试文件: tests/integration/skill-equipment/FSMRegistrationTests.cs

## Dependencies
- ICombatStateProvider（下游）: RegisterState, DeregisterAllSkillStates
- Story 002（本 Epic）: 装备逻辑调用注册
- CombatFSM（Core 层）: 必须先实现 RegisterState 接口
