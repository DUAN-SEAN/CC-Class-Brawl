# Story 006: HUD 性能优化 — 确保 < 0.8ms 渲染预算

## Epic
battle-hud

## Status
Ready

## Layer
Presentation

## Type
Logic

## Estimate
2 hours

## Context
- **GDD**: design/gdd/battle-hud.md — 性能验收标准（< 0.8ms/帧）
- **ADR**: ADR-0014 Section 9 (性能预算分析)
- **Existing Code**: Presentation/HUD/HUDController.cs, PlayerHUDView.cs, HUDAnimator.cs
- **TR-IDs**: TR-HUD-021 (HUD 性能预算 < 0.8ms)
- **Control Manifest**: 2026-05-24

## Acceptance Criteria
- **GIVEN** 2 人对战进行中且 HUD 所有组件激活, **THEN** HUD 每帧渲染开销 < 0.8ms（帧预算 16.6ms 的 5%）
- **GIVEN** 同一帧收到 3 个事件更新, **THEN** HUD 在同一帧内完成所有视觉更新，不丢帧
- **GIVEN** HUD 正常运行, **THEN** Update 轮询（边缘警告+方向箭头）耗时 < 0.1ms
- **GIVEN** HUD 正常运行, **THEN** 事件处理器总耗时 < 0.3ms（不含 Update 轮询）
- **GIVEN** 15+ 事件订阅全部激活, **THEN** 总处理时间在可接受范围内

## Implementation Notes
- 使用 Unity Profiler 验证 HUD 帧耗时
- 主要性能关注点:
  (1) 事件处理器不应产生 GC 分配（避免 LINQ、lambda 闭包、new List）
  (2) Update 轮询应最小化: 只在 Battle/BattleEnd 阶段执行，提前退出
  (3) USS class 切换不产生分配（UI Toolkit 内部管理）
  (4) 动画 tween 使用 schedule 而非协程
- 可能的优化策略:
  (1) dirty-flag 批量更新: 同一帧多个事件只触发一次 DOM 更新
  (2) 缓存 UI 元素引用（已在 CacheUIReferences 中实现）
  (3) 避免每帧 string 拼接（百分比文字）——仅在值变化时更新
  (4) 方向箭头位置计算使用整数运算
- 验证方法: Unity Profiler 标记 HUDController.Update 和各事件处理器，采集 60 秒数据
- 目标: 事件处理 < 0.3ms, Update 轮询 < 0.1ms, 总计 < 0.8ms

## Out of Scope
- 具体视觉组件实现（Story 001-005）
- 非 HUD 系统的性能优化

## QA Test Cases
- test_profiler_frame_budget: Profiler 采集 60s → 平均 HUD 帧耗时 < 0.8ms
- test_profiler_event_handling: 单事件处理 < 0.02ms
- test_profiler_update_polling: Update 轮询 < 0.1ms
- test_multi_event_same_frame: 3 事件同帧 → 不丢帧
- test_no_gc_in_event_handlers: 事件处理器无 GC.Alloc

## Test Evidence
- Profiler 截图 + 数据报告（Visual/Feel story — ADVISORY）
- 证据路径: production/qa/evidence/hud-performance.md
- 证据内容: Unity Profiler 截图显示 HUDController 帧耗时数据

## Dependencies
- Story 001-005（本 Epic）: 所有视觉组件实现完成后才能进行完整性能测试
- Unity Profiler: 性能数据采集工具
