# Story 002: 比分追踪 + 胜利条件 — Bo1/Bo3/Bo5 格式

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
- **GDD**: design/gdd/match-management-system.md — Core Rules 3 (KO 处理), 4 (比赛胜负判定), Formulas 1-4
- **ADR**: ADR-0010 Section 3 (KO 处理与比分更新), Section 6 (IMatchManager 接口)
- **Existing Code**: Feature/Formulas/MatchFormulas.cs（已有公式 + 测试）
- **TR-IDs**: TR-MCH-012 (SignalRoundEnd 接口)
- **Control Manifest**: 2026-05-24

## Acceptance Criteria
- **GIVEN** Bo3 比赛且 scores=[0,0], **WHEN** 收到 OnKO(player2Index), **THEN** scores=[1,0], currentRound=2, SignalRoundEnd(0, false)
- **GIVEN** Bo3 比赛, **WHEN** 赛制格式=3, **THEN** WinsNeeded=2, MaxRounds=3
- **GIVEN** Bo1 比赛, **WHEN** 赛制格式=1, **THEN** WinsNeeded=1, MaxRounds=1
- **GIVEN** Bo3 比赛且 scores=[1,1], **WHEN** 玩家 1 赢得第 3 局, **THEN** scores=[2,1], IsMatchOver=true, SignalRoundEnd(0, true)
- **GIVEN** Bo3 比赛且 scores=[1,1], **WHEN** 双 KO 发生, **THEN** scores=[2,2], IsDraw=true, SignalRoundEnd(-1, true)
- **GIVEN** Bo1 比赛, **WHEN** 玩家 1 被 KO, **THEN** scores=[0,1], IsMatchOver=true
- **GIVEN** 回合结束, **THEN** OnRoundEnd(winnerIndex, scores) 事件触发
- **GIVEN** 比赛结束（胜利或平局）, **THEN** OnMatchEnd(winnerIndex or null) 事件触发
- **GIVEN** Bo3 比分 1-0 时双 KO, **WHEN** scores=[2,1], **THEN** 玩家 1 赢得比赛（非平局）
- **GIVEN** 赛制格式不是 {1,3,5}, **WHEN** 初始化, **THEN** 钳制到最近合法值（0→1, 2→3, >5→5）

## Implementation Notes
- MatchFormulas 已有 CalculateWinsNeeded/MaxRounds/IsMatchOver/IsDraw/ClampMatchFormat + 单元测试
- KO 处理在 HandleKO 中: 被 KO 玩家的对手获胜，winnerIndex = 1 - koPlayerIndex
- 双 KO: 同帧两个 OnKO → _koCountThisFrame >= 2 → scores 双方各 +1
- 帧末统一处理: FixedUpdate 末尾检查 _koCountThisFrame > 0，执行比分更新 + SignalRoundEnd
- SignalRoundEnd 签名: (int winnerIndex, bool matchOver)
  - 单 KO + matchOver=false: winnerIndex = 胜者
  - 双 KO + matchOver=false: winnerIndex = 0（任意值）
  - matchOver=true + 有胜者: winnerIndex = 胜者
  - matchOver=true + 平局: winnerIndex = -1
- OnRoundEnd 和 OnMatchEnd 事件在比分更新后触发
- 赛点判定: scores[i] == WinsNeeded - 1（HUD 使用，不在本 Story 实现但数据需可查）

## Out of Scope
- FSM 状态管理（Story 001）
- 回合生命周期细节（Story 003）
- 重置协调（Story 004）

## QA Test Cases
- test_score_single_ko: 单 KO → 正确比分 + SignalRoundEnd
- test_score_bo3_init: Bo3 → WinsNeeded=2, MaxRounds=3
- test_score_bo1_init: Bo1 → WinsNeeded=1
- test_score_match_over_win: scores=[2,1] → IsMatchOver=true
- test_score_match_draw: Bo3 双 KO → scores=[2,2] → IsDraw=true
- test_score_bo1_ko: Bo1 单 KO → IsMatchOver=true
- test_score_events: OnRoundEnd + OnMatchEnd 正确触发
- test_score_lead_double_ko: Bo3 [1,0] + 双 KO → [2,1] 非平局
- test_score_clamp_format: 非法格式钳制

## Test Evidence
- 自动化单元测试（Logic story — BLOCKING）
- 测试文件: tests/unit/match-management/ScoreTrackingTests.cs

## Dependencies
- MatchFormulas（已有）: 纯计算公式
- IGameState（下游）: SignalRoundEnd 调用
- IKnockbackSystem（上游）: OnKO 事件
- Story 001（本 Epic）: FSM 状态守卫
