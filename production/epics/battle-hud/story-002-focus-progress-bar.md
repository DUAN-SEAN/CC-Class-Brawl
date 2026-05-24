# Story 002: 专注值进度条 — 订阅 OnFocusChanged + 脉动动画

## Epic
battle-hud

## Status
Ready

## Layer
Presentation

## Type
UI

## Estimate
3 hours

## Context
- **GDD**: design/gdd/battle-hud.md — Core Rules 3 (专注值进度条), Formulas 3-4 (填充比例 + 脉动频率)
- **ADR**: ADR-0014 Section 5 (OnFocusChanged/OnFocusReady 处理器), Section 6 (USS 脉动策略)
- **Existing Code**: Presentation/HUD/HUDController.cs (OnFocusChanged/OnFocusReady handlers), Presentation/HUD/PlayerHUDView.cs (UpdateFocusBar/PlayFocusUnlockFlash)
- **TR-IDs**: TR-HUD-001, TR-HUD-019
- **Control Manifest**: 2026-05-24

## Acceptance Criteria
- **GIVEN** FocusPoints=0, UnlockThreshold=40.0, **WHEN** HUD 初始化, **THEN** 进度条空（0%），职业色填充
- **GIVEN** FocusPoints=32.0, UnlockThreshold=40.0, **WHEN** OnFocusChanged 到达, **THEN** 进度条填充 80%，基础脉动 1.0Hz
- **GIVEN** FocusPoints=36.0, UnlockThreshold=40.0（>80%）, **WHEN** OnFocusChanged 到达, **THEN** 进度条填充 90%，脉动加速 2.0Hz
- **GIVEN** FocusPoints=41.6, UnlockThreshold=40.0（触发解锁）, **WHEN** OnFocusReady 到达, **THEN** 进度条闪白(0.1s) → 清空 → 1.6/45.0≈3.6% 重新填充
- **GIVEN** UnlockedCount=4 (MaxSkillsPerMatch), **WHEN** OnFocusChanged 到达, **THEN** 进度条变灰 #666666，显示 "MAX"
- **GIVEN** 进度条有阈值标记线, **WHEN** UnlockThreshold 变化, **THEN** 标记线滑动动画到新位置

## Implementation Notes
- HUDController.OnFocusChanged 已存在，委托给 PlayerHUDView.UpdateFocusBar
- HUDController.OnFocusReady 已存在，委托给 PlayerHUDView.PlayFocusUnlockFlash
- PlayerHUDView.UpdateFocusBar 需实现:
  (1) 填充比例 = FocusPoints / UnlockThreshold（钳制 0.0-1.0）
  (2) 职业色: 从 Initialize 时传入的 playerColor 设置
  (3) 脉动: FocusFillRatio > 0.8 时脉动加速，通过 schedule.Execute 循环 + opacity 调制
  (4) MAX 状态: UnlockedCount >= MaxSkillsPerMatch → 变灰 + "MAX" 标签
- PlayerHUDView.PlayFocusUnlockFlash 需实现:
  (1) 进度条闪白（0.1s）
  (2) 清空到 0%
  (3) 剩余 FocusPoints / 新 Threshold 填充
- 阈值标记线: USS 中的 threshold-marker 元素，宽度位置按 UnlockThreshold / MaxThreshold 计算
- 脉动频率公式: FillRatio > 0.8 时 PulseFreq = Lerp(1.0, 3.0, (ratio-0.8)/0.2)
- 背景色暗灰 #333333, MAX 灰 #666666

## Out of Scope
- 伤害百分比（Story 001）
- 技能槽位（Story 003）
- KO 通知（Story 004）

## QA Test Cases
- test_focus_bar_empty: 初始 → 0% 填充
- test_focus_bar_fill: 32/40 → 80% 填充
- test_focus_bar_pulse: >80% → 脉动加速
- test_focus_bar_unlock_flash: 解锁 → 闪白→清空→重新填充
- test_focus_bar_max: 4/4 已装备 → 灰色 + "MAX"
- test_focus_bar_threshold_marker: 阈值变化 → 标记线滑动

## Test Evidence
- 手动 walkthrough doc + 截图（UI story — ADVISORY）
- 证据路径: production/qa/evidence/hud-focus-bar.md

## Dependencies
- IFocusSystem（上游）: OnFocusChanged + OnFocusReady 事件
- HUDController（已有）: 事件订阅
- PlayerHUDView: 需完善 UpdateFocusBar/PlayFocusUnlockFlash
