# Story 004: 抽取触发 — OnFocusReady 启动抽取

## Epic
skill-draw

## Status
Ready

## Layer
Feature

## Type
Integration

## Estimate
2 hours

## Context
- **GDD**: design/gdd/skill-draw-system.md — Core Rules 1 (抽取触发机制), 4 (玩家选择流程)
- **ADR**: ADR-0009 Section 3 (HandleFocusReady 流程), Section 2 (FocusSystem.OnFocusReady)
- **Existing Code**: Feature/Interfaces/ISkillDrawSystem.cs, Core/Interfaces/IFocusSystem.cs
- **TR-IDs**: TR-SKW-023 (不暂停游戏)
- **Control Manifest**: 2026-05-24

## Acceptance Criteria
- **GIVEN** UnlockedCount=3 且 AlreadyDrawnSkillIds 有 3 个 ID, **WHEN** OnFocusReady 触发, **THEN** 正常执行第 4 次抽取
- **GIVEN** UnlockedCount=4（已达上限）, **WHEN** OnFocusReady 误触发, **THEN** 抽取系统忽略该事件，保持在 Idle 状态
- **GIVEN** Warrior InitialPoolSize=6, **WHEN** n=0→4, **THEN** RemainingPoolSize 依次为 6→5→4→3→2
- **GIVEN** P1(Warrior) 和 P2(Rogue) 同一帧触发 OnFocusReady, **WHEN** 两个抽取处理完成, **THEN** 各自从各自职业池抽取，独立候选，独立超时
- **GIVEN** AwaitingSelection 状态（第 1 次抽取进行中）, **WHEN** 第 2 个 OnFocusReady 到达, **THEN** 队列化处理，等第 1 次完成后再执行第 2 次
- **GIVEN** P1 和 P2 都抽到同一通用技能（如 skill_counter-strike）, **WHEN** 两个抽取完成, **THEN** P1 的 AlreadyDrawnSkillIds 包含它，P2 的也包含它（按角色独立）
- **GIVEN** 无下游系统订阅 OnSkillDrawn, **WHEN** 抽取完成, **THEN** 事件发布不阻塞，技能仍加入 AlreadyDrawnSkillIds

## Implementation Notes
- SkillDrawSystem 在 OnEnable 中订阅 IFocusSystem.OnFocusReady
- HandleFocusReady 检查: (1) Phase == Idle, (2) UnlockedCount < MaxSkillsPerMatch
- 触发后立即执行: 牌池构建（Story 002）→ 权重计算+候选生成（Story 003）→ 发出 OnDrawReady 或直接完成
- 队列化处理: 维护 _pendingDraw[playerIndex] 布尔标志，当前抽取完成后（回到 Idle）自动检查并触发下一次
- 双人同帧抽取: 因为每人有独立的 DrawRuntimeState，天然支持并行处理
- OnDrawReady 事件签名: Action<int, IReadOnlyList<SkillData>> (playerIndex, candidates)
- OnSkillDrawn 事件签名: Action<int, SkillData> (playerIndex, selectedSkill)
- 游戏不暂停: 不调用任何暂停逻辑，角色仍可移动、攻击、物理模拟继续

## Out of Scope
- 牌池构建细节（Story 002）
- 加权随机细节（Story 003）
- 超时逻辑（Story 005）
- UI 展示

## QA Test Cases
- test_trigger_normal_draw: OnFocusReady → 正常触发抽取流程
- test_trigger_ignore_at_max: UnlockedCount=4 → 忽略
- test_trigger_dual_player_same_frame: P1+P2 同帧触发 → 各自独立处理
- test_trigger_queued: Phase != Idle 时 → 队列化，完成后自动触发
- test_trigger_shared_common_skill: 两人抽到同一通用技能 → 各自独立记录
- test_trigger_no_subscriber: OnSkillDrawn 无订阅者 → 不阻塞

## Test Evidence
- 自动化单元测试（Integration story — BLOCKING）
- 测试文件: tests/integration/skill-draw/DrawTriggerTests.cs

## Dependencies
- IFocusSystem（上游）: OnFocusReady 事件
- ISkillDatabase（上游）: GetAllSkills
- SkillEquipmentManager（下游）: 订阅 OnSkillDrawn
- Story 001/002/003（本 Epic）: FSM + 牌池 + 随机算法
