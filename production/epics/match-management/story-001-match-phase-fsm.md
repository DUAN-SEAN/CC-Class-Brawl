# Story 001: MatchPhase FSM — Inactive → WaitingForBattle → RoundInProgress → RoundResolved → MatchComplete

## Epic
match-management

## Status
Ready

## Layer
Feature

## Type
Logic

## Estimate
3 hours

## Context
- **GDD**: design/gdd/match-management-system.md — States and Transitions (内部 FSM)
- **ADR**: ADR-0010 Section 2 (MatchPhase enum + 状态转换)
- **Existing Code**: Feature/Enums/MatchPhase.cs (Inactive/WaitingForBattle/RoundInProgress/RoundResolved/MatchComplete), Feature/Data/MatchState.cs
- **TR-IDs**: TR-MCH-001 (内部 FSM 5 状态)
- **Control Manifest**: 2026-05-24

## Acceptance Criteria
- **GIVEN** MatchManager 未初始化, **WHEN** 创建后, **THEN** Phase = Inactive
- **GIVEN** Phase = Inactive, **WHEN** Initialize(MatchConfig{format=3}), **THEN** Phase = WaitingForBattle, scores=[0,0], WinsNeeded=2, currentRound=1
- **GIVEN** Phase = WaitingForBattle, **WHEN** GameState 进入 Battle, **THEN** Phase = RoundInProgress
- **GIVEN** Phase = RoundInProgress, **WHEN** 收到 OnKO, **THEN** Phase = RoundResolved
- **GIVEN** Phase = RoundResolved 且 matchOver=false, **WHEN** GameState 进入 Countdown, **THEN** Phase = WaitingForBattle
- **GIVEN** Phase = RoundResolved 且 matchOver=true, **WHEN** GameState 进入 Results, **THEN** Phase = MatchComplete
- **GIVEN** Phase = RoundResolved, **WHEN** 收到额外 OnKO, **THEN** 事件被忽略，不改变已锁定结果
- **GIVEN** Phase = MatchComplete, **WHEN** 收到任何事件, **THEN** 全部忽略
- **GIVEN** Phase = WaitingForBattle, **WHEN** 收到 OnKO, **THEN** 事件被忽略

## Implementation Notes
- MatchPhase enum 已存在: Inactive, WaitingForBattle, RoundInProgress, RoundResolved, MatchComplete
- MatchState struct 已存在: Phase, Scores, CurrentRound, WinsNeeded, MaxRounds, PlayerCount
- 创建 MatchManagerBehaviour MonoBehaviour 实现 IMatchManager
- Initialize(MatchConfig): (1) ClampMatchFormat → (2) CalculateWinsNeeded + MaxRounds → (3) 初始化 scores/currentRound → (4) Phase = WaitingForBattle
- 状态转换通过 GameState.OnStateChanged 事件驱动
- HandleKO 方法有 Phase 守卫: if (_phase != MatchPhase.RoundInProgress) return
- 所有事件处理方法都有 Phase 检查，防止异常状态下的非法操作
- MatchFormulas.CalculateWinsNeeded/MaxRounds/IsMatchOver/IsDraw 已存在并经过测试

## Out of Scope
- 比分追踪和胜负判定（Story 002）
- 回合生命周期（Story 003）
- 重置协调（Story 004）

## QA Test Cases
- test_fsm_initial_state: 创建后 Phase = Inactive
- test_fsm_initialize: Initialize(Bo3) → Phase = WaitingForBattle
- test_fsm_waiting_to_inprogress: GameState=Battle → Phase = RoundInProgress
- test_fsm_inprogress_to_resolved: OnKO → Phase = RoundResolved
- test_fsm_resolved_to_waiting: matchOver=false + Countdown → Phase = WaitingForBattle
- test_fsm_resolved_to_complete: matchOver=true + Results → Phase = MatchComplete
- test_fsm_ignore_ko_in_resolved: RoundResolved + OnKO → 忽略
- test_fsm_ignore_all_in_complete: MatchComplete + 任何事件 → 忽略
- test_fsm_ignore_ko_in_waiting: WaitingForBattle + OnKO → 忽略

## Test Evidence
- 自动化单元测试（Logic story — BLOCKING）
- 测试文件: tests/unit/match-management/MatchPhaseFSMTests.cs

## Dependencies
- IGameState（上游）: OnStateChanged 事件
- IKnockbackSystem（上游）: OnKO 事件
- MatchFormulas（已有）: CalculateWinsNeeded, IsMatchOver, IsDraw
