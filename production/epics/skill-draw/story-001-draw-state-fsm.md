# Story 001: DrawState FSM — Idle → Drawing → AwaitingSelection → Complete

## Epic
skill-draw

## Status
Ready

## Layer
Feature

## Type
Logic

## Estimate
3 hours

## Context
- **GDD**: design/gdd/skill-draw-system.md — States and Transitions, Core Rules
- **ADR**: ADR-0009 (Focus & Skill Draw Pipeline) — Section 3: SkillDrawSystem
- **Existing Code**: Feature/Enums/DrawPhase.cs, Feature/Data/DrawRuntimeState.cs, Feature/Interfaces/ISkillDrawSystem.cs
- **TR-IDs**: TR-SKW-001 (DrawState FSM 4 states)
- **Control Manifest**: 2026-05-24

## Acceptance Criteria
- **GIVEN** SkillDrawSystem 已初始化, **WHEN** 初始化完成, **THEN** 所有角色 DrawPhase = Idle，AlreadyDrawnSkillIds 为空集合
- **GIVEN** DrawPhase = Idle, **WHEN** 收到 OnFocusReady(playerIndex, unlockedCount), **THEN** 转入 Drawing 状态
- **GIVEN** DrawPhase = Idle, **WHEN** 收到 OnFocusReady 但 Phase != Idle, **THEN** 忽略事件（队列化处理，等当前抽取完成后再执行）
- **GIVEN** Drawing 阶段候选列表 > 1, **WHEN** 候选生成完成, **THEN** 转入 AwaitingSelection，发出 OnDrawReady
- **GIVEN** Drawing 阶段候选列表 = 1, **WHEN** 牌池仅 1 个技能, **THEN** 跳过 AwaitingSelection，直接 Complete + OnSkillDrawn
- **GIVEN** Drawing 阶段牌池为空, **WHEN** 无技能可抽, **THEN** 回到 Idle，不消耗解锁次数，不发 OnSkillDrawn
- **GIVEN** AwaitingSelection 状态, **WHEN** 调用 SelectCandidate(playerIndex, index), **THEN** 选中技能加入 AlreadyDrawnSkillIds，发出 OnSkillDrawn，回到 Idle
- **GIVEN** 每个角色的 DrawPhase 独立, **WHEN** P1 在 AwaitingSelection, **THEN** P2 状态不受影响
- **GIVEN** 2 人对战，**WHEN** 执行 FSM 状态管理, **THEN** 每帧处理耗时 < 0.05ms

## Implementation Notes
- DrawPhase enum 已存在于 Feature/Enums/DrawPhase.cs
- DrawRuntimeState struct 已存在于 Feature/Data/DrawRuntimeState.cs，包含 Phase、AlreadyDrawnSkillIds、CurrentCandidates、RemainingTimeoutFrames
- ISkillDrawSystem 接口已定义（GetDrawPhase, GetCurrentCandidates, SelectCandidate, ResetForNewRound/Match, ResetAll, OnDrawReady, OnSkillDrawn）
- 创建 SkillDrawSystem MonoBehaviour 实现 ISkillDrawSystem，持有 DrawRuntimeState[]（每角色一份）
- 在 FixedUpdate 中处理超时倒计时（每帧 RemainingTimeoutFrames--）
- 状态转换需在 FixedUpdate 末尾统一处理，避免中间状态不一致
- OnFocusReady 订阅 FocusSystem 事件；OnDrawReady/OnSkillDrawn 作为 C# event 发布
- 队列化处理：如果 Phase != Idle 时收到 OnFocusReady，记录 _pendingFocusReady 标志，当前抽取完成后自动触发下一次

## Out of Scope
- 牌池构建逻辑（Story 002）
- 加权随机算法（Story 003）
- 抽取触发逻辑（Story 004）
- 超时安全机制（Story 005）
- UI 展示（由 battle-hud epic 负责）

## QA Test Cases
- test_draw_phase_initial_state: 初始化后所有角色 Phase = Idle
- test_draw_phase_idle_to_drawing: OnFocusReady → Phase = Drawing
- test_draw_phase_drawing_to_awaiting: 候选 > 1 → Phase = AwaitingSelection + OnDrawReady 发出
- test_draw_phase_drawing_auto_select: 候选 = 1 → 直接 Complete + OnSkillDrawn
- test_draw_phase_empty_pool: 牌池空 → 回到 Idle，不消耗
- test_draw_phase_awaiting_to_complete: SelectCandidate → OnSkillDrawn + Phase = Idle
- test_draw_phase_ignore_when_not_idle: Phase != Idle 时 OnFocusReady 被队列化
- test_draw_phase_independent_players: P1/P2 状态互不影响

## Test Evidence
- 自动化单元测试（Logic story — BLOCKING）
- 测试文件: tests/unit/skill-draw/DrawStateFSMTests.cs

## Dependencies
- FocusSystem（上游）: OnFocusReady 事件
- ISkillDatabase（上游）: 牌池构建（Story 002 实现具体逻辑，本 Story 仅定义接口调用点）
- SkillEquipmentManager（下游）: 接收 OnSkillDrawn 事件
