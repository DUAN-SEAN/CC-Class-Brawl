# Story 004: 局间重置协调 — 全量重置（对局间）+ 对局结束流程

## Epic
match-management

## Status
Ready

## Layer
Feature

## Type
Integration

## Estimate
2 hours

## Context
- **GDD**: design/gdd/match-management-system.md — Core Rules 6 (数据重置规则), Edge Cases (初始化异常)
- **ADR**: ADR-0010 Section 5 (对局间重置), Section 6 (IMatchManager.Reset)
- **Existing Code**: Feature/Interfaces/IMatchManager.cs (Reset, ResetForNewMatch)
- **TR-IDs**: TR-MCH-009 (重置级联), TR-MCH-012 (SignalRoundEnd)
- **Control Manifest**: 2026-05-24

## Acceptance Criteria
- **GIVEN** 比赛结束（Results 状态）, **WHEN** GameState 从 Results 回到 CharacterSelect（"再来一局"）, **THEN** MatchManager.Reset() 被调用
- **GIVEN** Reset() 执行, **WHEN** 全量重置, **THEN** scores=[0,0], currentRound=1, Phase=Inactive
- **GIVEN** Reset() 执行, **WHEN** 重置各系统, **THEN** FocusSystem.ResetAllForNewMatch(), SkillDrawSystem.ResetAll(), SkillEquipmentManager.ResetAll()（含 DeregisterAllSkillStates）
- **GIVEN** P1 第 1 局装备了 2 个技能, **WHEN** 全量重置完成, **THEN** P1 所有槽位清空，FSM 无技能状态，角色回到只有基础招式
- **GIVEN** Reset() 后再次 Initialize(MatchConfig), **WHEN** 新对局开始, **THEN** 全部数据从零开始
- **GIVEN** 赛制格式不是 {1,3,5}（如 0、2、6）, **WHEN** Initialize, **THEN** ClampMatchFormat 钳制到合法值

## Implementation Notes
- IMatchManager.Reset() 在 GameStateManager 进入 CharacterSelect 时被调用
- Reset 流程: (1) scores = [0,0] → (2) currentRound = 1 → (3) Phase = Inactive → (4) FocusSystem.ResetAllForNewMatch() → (5) SkillDrawSystem.ResetAll() → (6) SkillEquipmentManager.ResetAll()（DeregisterAllSkillStates + 清空槽位）
- 注意: 对局间重置不同于回合间重置 — 对局间需要清空所有状态包括已装备技能
- Reset 后需再次调用 Initialize 才能开始新对局（Phase = Inactive → Initialize → WaitingForBattle）
- GameStateManager 的 Results → CharacterSelect 流转触发 Reset（由 GameStateManager 调用 IMatchManager.Reset()）
- 各系统 Reset 调用也需 try-catch 保护

## Out of Scope
- FSM 状态管理（Story 001）
- 比分逻辑（Story 002）
- 回合间重置（Story 003）

## QA Test Cases
- test_full_reset_scores: Reset → scores=[0,0]
- test_full_reset_phase: Reset → Phase=Inactive
- test_full_reset_focus: Reset → FocusSystem 全量重置
- test_full_reset_draw: Reset → SkillDrawSystem 全量清空
- test_full_reset_equipment: Reset → SkillEquipment 全部清空 + FSM 注销
- test_full_reset_reinitialize: Reset + Initialize → 正常开始新对局
- test_full_reset_clamp_format: 非法格式钳制

## Test Evidence
- 自动化集成测试（Integration story — BLOCKING）
- 测试文件: tests/integration/match-management/FullResetTests.cs

## Dependencies
- IFocusSystem: ResetAllForNewMatch
- ISkillDrawSystem: ResetAll
- ISkillEquipmentManager: ResetAll（含 DeregisterAllSkillStates）
- IGameState: 触发 Reset 调用
- Story 001/002/003（本 Epic）: FSM + 比分 + 回合重置
