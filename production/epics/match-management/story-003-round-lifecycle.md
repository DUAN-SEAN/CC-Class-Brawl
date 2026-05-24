# Story 003: 回合生命周期 — OnKO → 回合结束 → 重置协调 → 下一回合

## Epic
match-management

## Status
Ready

## Layer
Feature

## Type
Integration

## Estimate
3 hours

## Context
- **GDD**: design/gdd/match-management-system.md — Core Rules 2-5 (回合生命周期, KO 处理, 局间流转)
- **ADR**: ADR-0010 Section 3-4 (KO 处理 + CoordinateRoundReset)
- **TR-IDs**: TR-MCH-009 (回合开始重置级联), TR-MCH-012 (SignalRoundEnd)
- **Control Manifest**: 2026-05-24

## Acceptance Criteria
- **GIVEN** Bo3 比赛第 1 局, **WHEN** GameState 进入 Countdown, **THEN** 伤害百分比为 0%，专注值为 0，角色在出生点，已装备技能保持不变
- **GIVEN** Phase = RoundResolved 且 matchOver=false, **WHEN** GameState 从 BattleEnd 进入 Countdown, **THEN** CoordinateRoundReset() 被调用，Phase = WaitingForBattle
- **GIVEN** Phase = RoundResolved 且 matchOver=true, **WHEN** GameState 从 BattleEnd 进入 Results, **THEN** Phase = MatchComplete，不执行回合重置
- **GIVEN** CoordinateRoundReset 执行, **WHEN** 重置各系统, **THEN** 调用顺序: ResetDamage → ResetFocus(forNewRound) → ResetSkillDraw(forNewRound) → [跳过 SkillEquipment] → ResetPosition → SetVelocity(0) → ResetToIdle → ResetKnockback
- **GIVEN** 第 1 局结束时玩家 1 有 2 个已装备技能、85% 伤害、专注值 42.0, **WHEN** 第 2 局开始, **THEN** 伤害% = 0, 专注值 = 0, 已装备技能保持 2 个不变
- **GIVEN** 某系统 Reset 调用失败, **WHEN** CoordinateRoundReset 执行, **THEN** 记录错误但不中断后续重置（try-catch 保护每个 Reset 调用）

## Implementation Notes
- HandleStateChanged 是核心方法，监听 IGameState.OnStateChanged
- Countdown + RoundResolved → CoordinateRoundReset() → WaitingForBattle
- Battle + WaitingForBattle → RoundInProgress
- Results + RoundResolved → MatchComplete
- CoordinateRoundReset 遍历所有玩家，依次调用各系统 Reset 方法
- 重置方法映射（ADR-0010 Section 4）:
  - _damageSystem.ResetDamage(i)
  - _focusSystem.ResetForNewRound(i) — FocusPoints=0, UnlockedCount 保留
  - _skillDrawSystem.ResetForNewRound(i) — 取消待选, AlreadyDrawn 保留
  - [不调用 SkillEquipment.Reset — 技能跨局保留]
  - _movementControllers[i].ResetPosition(spawnPoint)
  - _movementControllers[i].SetVelocity(Vector2.zero)
  - _combatFSMs[i].ResetToIdle(i)
  - _knockbackSystem.ResetKnockback(i)
- 每个 Reset 调用用 try-catch 包裹，防止一个系统失败阻塞后续
- MatchManager 需要注入 6+ 个系统引用（在 Initialize 时设置）

## Out of Scope
- FSM 状态管理（Story 001）
- 比分逻辑（Story 002）
- 对局间全量重置（Story 004 隐含在 Reset() 方法中）

## QA Test Cases
- test_round_reset_damage: 第 2 局开始 → 伤害 = 0
- test_round_reset_focus: 第 2 局开始 → 专注值 = 0
- test_round_preserve_skills: 第 2 局开始 → 已装备技能不变
- test_round_reset_position: 第 2 局开始 → 角色在出生点
- test_round_reset_velocity: 第 2 局开始 → 速度 = 0
- test_round_reset_fsm: 第 2 局开始 → FSM = Idle
- test_round_no_reset_on_match_end: 比赛结束 → 不执行回合重置
- test_round_partial_failure: 某 Reset 失败 → 后续仍执行

## Test Evidence
- 自动化集成测试（Integration story — BLOCKING）
- 测试文件: tests/integration/match-management/RoundLifecycleTests.cs

## Dependencies
- IFocusSystem: ResetForNewRound
- ISkillDrawSystem: ResetForNewRound
- IDamageSystem: ResetDamage
- IMovementController[]: ResetPosition, SetVelocity
- ICombatStateProvider[]: ResetToIdle
- IKnockbackSystem: ResetKnockback
- Story 001/002（本 Epic）: FSM + 比分
