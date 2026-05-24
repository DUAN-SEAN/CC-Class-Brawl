# Story 005: 对局重置协调 — 清空槽位 + 注销状态

## Epic
skill-equipment

## Status
Ready

## Layer
Feature

## Type
Integration

## Estimate
2 hours

## Context
- **GDD**: design/gdd/skill-equipment-management.md — Core Rules 6 (对局生命周期), Edge Cases (对局重置)
- **ADR**: ADR-0009 Section 5 (回合重置 vs 对局重置), ADR-0010 Section 4 (CoordinateRoundReset)
- **Existing Code**: Feature/Interfaces/ISkillEquipmentManager.cs (ResetForNewMatch, ResetAll)
- **TR-IDs**: TR-SEQ-015 (OnMatchEnd: DeregisterAllSkillStates + clear slots)
- **Control Manifest**: 2026-05-24

## Acceptance Criteria
- **GIVEN** P1 已装备 3 个技能（Slot 1-3）, **WHEN** 收到 OnMatchEnd（对局间重置）, **THEN** 所有槽位清空为 Empty，FSM 注销所有 3 个技能状态，发出 OnSkillUnequipped(P1, 1/2/3)
- **GIVEN** P1 已装备 3 个技能（Slot 1-3）, **WHEN** 收到 OnRoundStart（回合间）, **THEN** 槽位不变，技能保留，FSM 状态保留
- **GIVEN** P1 正在执行技能（FSM 在 Attacking 状态）, **WHEN** 收到 OnMatchEnd, **THEN** FSM 强制回到 Idle，技能状态注销
- **GIVEN** 装备管理器在 Equipping 状态时收到 OnMatchEnd, **WHEN** 装备中断, **THEN** 执行重置，未完成的装备被丢弃
- **GIVEN** ResetAll() 被调用, **WHEN** 执行全量重置, **THEN** 所有玩家（P1+P2）的槽位清空，所有 FSM 注销技能状态

## Implementation Notes
- ISkillEquipmentManager 定义了 ResetForNewMatch(playerIndex) 和 ResetAll()
- 注意: 无 ResetForNewRound — 技能跨局保留（ADR-0009 确立）
- ResetForNewMatch: (1) 遍历所有槽位 → (2) 对每个 Equipped 槽位发出 OnSkillUnequipped → (3) 调用 ICombatStateProvider.DeregisterAllSkillStates() → (4) 所有槽位设为 Empty + SkillData=null
- ResetAll: 对所有玩家执行 ResetForNewMatch
- 回合间（OnRoundStart）不触发任何重置 — 由对局管理系统协调
- FSM 强制回 Idle: DeregisterAllSkillStates 后 CombatFSM 应自动回到 Idle（如果当前在技能状态）
- 执行顺序: 先 DeregisterAllSkillStates（清理 FSM），再清理槽位数组（避免中间状态）
- Reset 调用方: MatchManager.CoordinateRoundReset 或 GameStateManager 的状态转换

## Out of Scope
- 槽位管理基础（Story 001）
- 装备逻辑（Story 002）
- FSM 注册细节（Story 003）
- 输入映射（Story 004）
- MatchManager 协调逻辑（match-management epic）

## QA Test Cases
- test_reset_match_clears_slots: OnMatchEnd → 3 个槽位清空
- test_reset_match_deregisters_fsm: OnMatchEnd → FSM 字典无技能
- test_reset_match_events: OnMatchEnd → OnSkillUnequipped 每个槽位触发
- test_reset_round_preserves: OnRoundStart → 槽位/FSM 不变
- test_reset_during_execution: Attacking + OnMatchEnd → FSM 回 Idle
- test_reset_interrupt_equip: Equipping + OnMatchEnd → 装备丢弃
- test_reset_all_players: ResetAll → P1+P2 全部清空

## Test Evidence
- 自动化集成测试（Integration story — BLOCKING）
- 测试文件: tests/integration/skill-equipment/ResetCoordinationTests.cs

## Dependencies
- ICombatStateProvider（下游）: DeregisterAllSkillStates
- MatchManager（上游协调）: 调用 ResetForNewMatch/ResetAll
- Story 001/002/003（本 Epic）: 槽位管理 + 装备 + FSM 注册
