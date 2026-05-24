# Story 004: 输入映射 — Skill1-4 绑定到已装备技能

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
- **GDD**: design/gdd/skill-equipment-management.md — Core Rules 3 (技能激活), Table (键盘/手柄映射)
- **ADR**: ADR-0002 (输入缓冲 8 帧), ADR-0009 Section 4 (技能输入通过 FSM 标准输入缓冲)
- **Existing Code**: 无直接现有代码（依赖 CombatFSM 输入系统）
- **TR-IDs**: TR-SEQ-001 (输入映射 Slot→键位)
- **Control Manifest**: 2026-05-24

## Acceptance Criteria
- **GIVEN** Slot 1 已装备 Fireball 且 FSM 在 Idle, **WHEN** 玩家按下技能键 1（或 RB）, **THEN** 角色进入 Fireball 的 Startup 帧
- **GIVEN** Slot 2 未装备（Empty）, **WHEN** 玩家按下技能键 2（或 RT）, **THEN** 输入被忽略，角色不执行任何操作
- **GIVEN** Slot 1 已装备且 FSM 在 Attacking.Recovery（CancelTable 允许技能）, **WHEN** 玩家按下技能键 1, **THEN** 取消当前攻击到 Fireball 的 Startup
- **GIVEN** Slot 1 已装备且 FSM 在 HitStun（剩余 5 帧）, **WHEN** 玩家按下技能键 1, **THEN** 输入写入缓冲，等待 HitStun 结束后执行
- **GIVEN** Slot 1 已装备且 FSM 在 HitStun（剩余 10 帧，InputBufferFrames=8）, **WHEN** 玩家按下技能键 1, **THEN** 输入在 HitStun 结束前过期，不执行
- **GIVEN** 同一帧多个技能键按下, **WHEN** Slot 1 > Slot 2 > Slot 3 > Slot 4 优先级, **THEN** 只有优先级最高的技能被接受

## Implementation Notes
- 输入映射不经过 SkillEquipmentManager — 输入直接写入 CombatFSM 的输入缓冲
- SkillEquipmentManager 只负责装备/卸载时的 StateDefinition 注册（包含 InputMapping 字段）
- InputType 枚举需包含 Skill1, Skill2, Skill3, Skill4
- 键盘映射: 1→Skill1, 2→Skill2, 3→Skill3, 4→Skill4
- 手柄映射: RB→Skill1, RT→Skill2, LB→Skill3, LT→Skill4
- 空槽位: CombatFSM 检查 InputMapping 对应的 StateDefinition 是否存在，不存在则忽略输入
- 输入缓冲: 复用 CombatFSM 已有的 8 帧环形缓冲区
- 优先级: 同帧多技能输入时，按 Slot 1 > 2 > 3 > 4 处理（FSM 输入处理逻辑）
- Unity Input System 配置: 在 InputActionAsset 中定义 Skill1-4 action，绑定键盘和手柄按键

## Out of Scope
- 装备/卸载逻辑（Story 002/003）
- 重置逻辑（Story 005）
- Input System 资产创建（Foundation 层）

## QA Test Cases
- test_input_skill1_idle: Idle + Skill1 → 执行技能
- test_input_empty_slot: Empty + Skill2 → 忽略
- test_input_recovery_cancel: Recovery + Skill1 + CancelTable 允许 → 取消到技能
- test_input_hitstun_buffer: HitStun(5帧) + Skill1 → 缓冲等待执行
- test_input_hitstun_expire: HitStun(10帧) + Skill1(buffer=8) → 过期
- test_input_priority: 同帧 Skill1+Skill2 → Skill1 优先

## Test Evidence
- 自动化单元测试（Logic story — BLOCKING）
- 测试文件: tests/unit/skill-equipment/InputMappingTests.cs

## Dependencies
- CombatFSM（Core 层）: 输入缓冲 + 状态转换逻辑
- Unity Input System: Skill1-4 action 定义
- Story 003（本 Epic）: StateDefinition 注册后输入才有效
