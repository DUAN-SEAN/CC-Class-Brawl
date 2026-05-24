# Story 005: 抽取超时安全 — 自动选择

## Epic
skill-draw

## Status
Ready

## Layer
Feature

## Type
Logic

## Estimate
2 hours

## Context
- **GDD**: design/gdd/skill-draw-system.md — Core Rules 4 (超时 5 秒), Edge Cases (选择流程)
- **ADR**: ADR-0009 Section 3 (超时处理)
- **Existing Code**: Feature/Data/DrawRuntimeState.cs (RemainingTimeoutFrames)
- **TR-IDs**: TR-SKW-009 (5 秒超时自动选择第一个)
- **Control Manifest**: 2026-05-24

## Acceptance Criteria
- **GIVEN** AwaitingSelection 状态且有 3 个候选, **WHEN** 5.0 秒内无输入, **THEN** 自动选择候选[0]，状态转 Complete，发出 OnSkillDrawn
- **GIVEN** AwaitingSelection 状态, **WHEN** 超时倒计时从 OnDrawReady 事件发出时开始, **THEN** 精确计时 300 帧（5s * 60fps）
- **GIVEN** AwaitingSelection 状态且有 2 个候选, **WHEN** 5.0 秒超时, **THEN** 同样自动选择候选[0]
- **GIVEN** 超时发生时选择 UI 尚未完全渲染, **WHEN** 超时触发, **THEN** 仍然执行自动选择（不依赖 UI 渲染完成）
- **GIVEN** P1 和 P2 都在 AwaitingSelection, **WHEN** 仅 P1 超时, **THEN** P1 自动选择，P2 超时计时器独立继续
- **GIVEN** 玩家在 KO 状态下处于 AwaitingSelection, **WHEN** 超时触发, **THEN** 自动选择正常执行
- **GIVEN** 对局在 AwaitingSelection 状态下结束（OnRoundStart 到达）, **WHEN** 收到重置信号, **THEN** 取消超时计时器，抽取被丢弃，候选不计入 AlreadyDrawnSkillIds

## Implementation Notes
- 在 FixedUpdate 中每帧检查所有角色的 DrawRuntimeState
- 如果 Phase == AwaitingSelection: RemainingTimeoutFrames--
- 当 RemainingTimeoutFrames <= 0: 调用 SelectCandidate(playerIndex, 0)
- SelectionTimeout 配置值: 5.0 秒 = 300 帧（60fps），应从 HUDTuningData 或 DrawSystem 配置读取
- 超时倒计时从 OnDrawReady 发出时开始（设置 RemainingTimeoutFrames = SelectionTimeout * 60）
- ResetForNewRound/ResetForNewMatch 需要取消正在进行的超时（将 Phase 设回 Idle）
- 双人独立超时: 每人有自己的 RemainingTimeoutFrames，互不干扰

## Out of Scope
- 超时倒计时的 UI 展示（由 battle-hud epic 负责）
- 牌池/随机算法逻辑

## QA Test Cases
- test_timeout_auto_select: 5 秒无输入 → 自动选候选[0]
- test_timeout_frame_precision: 超时精确在 300 帧时触发
- test_timeout_independent_players: P1 超时不影响 P2
- test_timeout_ko_state: KO 状态下超时正常执行
- test_timeout_cancel_on_reset: OnRoundStart 取消超时
- test_timeout_ui_not_ready: UI 未渲染完成时超时仍触发

## Test Evidence
- 自动化单元测试（Logic story — BLOCKING）
- 测试文件: tests/unit/skill-draw/DrawTimeoutTests.cs

## Dependencies
- Story 001（本 Epic）: DrawState FSM
- 对局管理系统（上游）: OnRoundStart 重置信号
