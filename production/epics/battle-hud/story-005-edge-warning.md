# Story 005: 边缘警告 — 玩家接近 Blast Zone 边界警告

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
- **GDD**: design/gdd/battle-hud.md — UXML 结构中 EdgeWarning 元素 + 方向指示箭头
- **ADR**: ADR-0014 Section 4 (EdgeWarning + DirectionArrows), Section 8 (Update 轮询)
- **Existing Code**: Presentation/HUD/HUDController.cs (UpdateEdgeWarnings + UpdateDirectionArrows 已实现)
- **TR-IDs**: TR-HUD-001, TR-HUD-021 (< 0.8ms 帧预算)
- **Control Manifest**: 2026-05-24

## Acceptance Criteria
- **GIVEN** 玩家距 Blast Zone 边缘 > 3 单位, **WHEN** Update 执行, **THEN** 边缘警告不可见（opacity=0, display=none）
- **GIVEN** 玩家距 Blast Zone 边缘 < 3 单位, **WHEN** Update 执行, **THEN** 边缘警告可见，opacity 按距离线性插值（0.0=边缘→0.4=阈值）
- **GIVEN** 玩家超出摄像机可视范围, **WHEN** Update 执行, **THEN** 方向箭头显示，指向玩家位置，颜色为玩家职业色
- **GIVEN** 玩家回到摄像机范围内, **WHEN** Update 执行, **THEN** 方向箭头隐藏
- **GIVEN** 两个玩家同时接近边缘, **WHEN** Update 执行, **THEN** 边缘警告 alpha 取两者最大值
- **GIVEN** HUD 未初始化或不在 Battle/BattleEnd 阶段, **WHEN** Update 执行, **THEN** 跳过边缘警告和方向箭头更新

## Implementation Notes
- HUDController.UpdateEdgeWarnings 已实现（Lines 513-559）:
  (1) 从 IArenaDataProvider.GetBlastZone() 获取 Blast Zone 边界
  (2) 遍历每个玩家，计算到四个边缘的最小距离
  (3) warningThreshold = 3.0 单位，alpha = Lerp(0.4, 0, dist/threshold)
  (4) 取两玩家最大 alpha，设置 EdgeWarning 元素 opacity
- HUDController.UpdateDirectionArrows 已实现（Lines 563-633）:
  (1) 从 IArenaDataProvider.GetCameraBounds() 获取可视范围
  (2) 判断玩家是否出界，如果是则显示箭头并定位到对应屏幕边缘
  (3) 箭头颜色设为玩家职业色
- 需要验证现有实现是否符合 GDD 验收标准，补充任何缺失细节
- Update 方法仅在 _initialized 且 Battle/BattleEnd 阶段执行（已实现守卫）
- 边缘警告使用 VisualElement + opacity 控制（非动画，纯数值更新）
- 方向箭头位置计算需要正确映射世界坐标到屏幕坐标

## Out of Scope
- 伤害百分比（Story 001）
- 专注值进度条（Story 002）
- 技能槽位（Story 003）
- KO 通知（Story 004）
- 性能优化（Story 006 — 但需确保边缘警告不超标）

## QA Test Cases
- test_edge_warning_hidden: 远离边缘 → 不可见
- test_edge_warning_visible: 接近边缘 → 可见 + 正确 alpha
- test_edge_warning_max_alpha: 两玩家 → 取最大
- test_direction_arrow_show: 出界 → 箭头显示 + 正确位置
- test_direction_arrow_hide: 回到范围内 → 箭头隐藏
- test_edge_warning_phase_guard: 非 Battle 阶段 → 跳过

## Test Evidence
- 手动 walkthrough doc + 截图（UI story — ADVISORY）
- 证据路径: production/qa/evidence/hud-edge-warning.md

## Dependencies
- IArenaDataProvider（上游）: GetBlastZone + GetCameraBounds
- IMovementController[]（上游）: GetPosition 轮询
- HUDController（已有）: UpdateEdgeWarnings + UpdateDirectionArrows 实现
