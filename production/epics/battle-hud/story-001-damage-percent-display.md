# Story 001: 伤害百分比显示 — 订阅 OnDamagePercentChanged

## Epic
battle-hud

## Status
Ready

## Layer
Presentation

## Type
UI

## Estimate
2 hours

## Context
- **GDD**: design/gdd/battle-hud.md — Core Rules 2 (伤害百分比显示), Formulas 1-2 (显示值 + 颜色索引)
- **ADR**: ADR-0014 Section 5 (OnDamagePercentChanged 事件处理器)
- **Existing Code**: Presentation/HUD/HUDController.cs (OnDamagePercentChanged handler 已存在), Presentation/HUD/PlayerHUDView.cs (UpdateDamagePercent 方法)
- **TR-IDs**: TR-HUD-001 (HUD 纯被动渲染), TR-HUD-019 (UI Toolkit)
- **Control Manifest**: 2026-05-24

## Acceptance Criteria
- **GIVEN** 角色当前 DamagePercent=0.0, **WHEN** HUD 初始化, **THEN** 显示 "0%"，白色
- **GIVEN** 角色当前 DamagePercent=42.7, **WHEN** OnDamagePercentChanged 到达, **THEN** 显示 "42%"，白色，触发缩放弹跳动画（1.0x → 1.3x → 1.0x, 0.15s）
- **GIVEN** DamagePercent 从 49.0 变为 52.0, **WHEN** 颜色阈值跨越, **THEN** 颜色从白色平滑过渡到黄色（0.2s）
- **GIVEN** DamagePercent 从 99.0 变为 101.0, **THEN** 颜色从黄色过渡到橙色
- **GIVEN** DamagePercent 从 149.0 变为 152.0, **THEN** 颜色从橙色变为红色闪烁（0.5Hz）
- **GIVEN** DamagePercent=1002.3, **THEN** 显示 "999+"
- **GIVEN** DamagePercent 为负数（上游错误）, **THEN** 钳制显示为 "0%"，白色
- **GIVEN** 同一帧收到多个 OnDamagePercentChanged, **THEN** 依次处理，最终显示最新值

## Implementation Notes
- HUDController.OnDamagePercentChanged 已存在，委托给 PlayerHUDView.UpdateDamagePercent
- PlayerHUDView.UpdateDamagePercent 需实现:
  (1) Floor(newPercent) → 显示整数 + "%"
  (2) 颜色编码: <50 白色, 50-99 黄色, 100-149 橙色, 150+ 红色闪烁
  (3) 弹跳动画: 通过 USS class "bounce" 触发
  (4) 颜色过渡: 通过 USS transition + class 切换
- HUDAnimator 已有 TweenOpacity 等辅助方法，可能需扩展弹跳动画支持
- 颜色值: 白 #FFFFFF, 黄 #FFD700, 橙 #FF8C00, 红 #FF2020
- 999+ 特殊显示: Floor > 999 时显示 "999+"
- 负数钳制: Mathf.Max(0, displayValue)

## Out of Scope
- 专注值进度条（Story 002）
- 技能槽位（Story 003）
- KO 通知（Story 004）
- 边缘警告（Story 005）
- 性能优化（Story 006）

## QA Test Cases
- test_damage_display_zero: 初始 → "0%" 白色
- test_damage_display_fraction: 42.7 → "42%" 白色 + 弹跳
- test_damage_color_transition: 49→52 → 白到黄过渡
- test_damage_color_orange: 99→101 → 黄到橙
- test_damage_color_red_flash: 149→152 → 橙到红闪烁
- test_damage_display_overflow: 1002.3 → "999+"
- test_damage_negative_clamp: 负数 → "0%" 白色
- test_damage_same_frame_multi: 同帧多事件 → 最新值

## Test Evidence
- 手动 walkthrough doc + 截图（UI story — ADVISORY）
- 证据路径: production/qa/evidence/hud-damage-percent.md

## Dependencies
- IDamageSystem（上游）: OnDamagePercentChanged 事件
- HUDController（已有）: 事件订阅框架
- PlayerHUDView: 需实现 UpdateDamagePercent
- HUDAnimator: 弹跳动画辅助
