# Story 004: KO 通知 — 订阅 OnKO + 显示闪光/叠加层

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
- **GDD**: design/gdd/battle-hud.md — UXML 结构中 KONotification 元素
- **ADR**: ADR-0014 Section 4 (UXML: KONotification label)
- **Existing Code**: Presentation/HUD/HUDController.cs (OnKO handler + PlayKOSequence), Presentation/HUD/HUDAnimator.cs (PlayKOSequence)
- **TR-IDs**: TR-HUD-001, TR-HUD-019
- **Control Manifest**: 2026-05-24

## Acceptance Criteria
- **GIVEN** 战斗进行中, **WHEN** 收到 OnKO(playerIndex, koDirection) 事件, **THEN** 屏幕中央显示 "KO!" 文本，配合闪光动画
- **GIVEN** KO 通知显示后, **WHEN** 动画完成（约 1-2 秒）, **THEN** KO 文本淡出消失
- **GIVEN** HUD 隐藏状态, **WHEN** 收到 OnKO, **THEN** KO 通知不显示
- **GIVEN** KO 通知期间, **WHEN** 进入 BattleEnd, **THEN** KO 文本保持显示直到 BattleEnd 冻结帧结束

## Implementation Notes
- HUDController.OnKO 已存在，调用 HUDAnimator.PlayKOSequence(_koNotification)
- HUDAnimator.PlayKOSequence 需实现:
  (1) 设置 KONotification style.display = Flex
  (2) 播放缩放弹入动画（0→2.0x→1.0x, 约 0.3s）
  (3) 可选: 短暂全屏白色闪光（0.05s）
  (4) 保持显示约 1-2s
  (5) 淡出消失（0.3s）
- KONotification 是 UXML 中的 Label 元素（见 ADR-0014 Section 2）
- 默认 style.display = None，KO 时改为 Flex
- 使用 HUDAnimator 的 schedule 或 tween 系统管理动画时序
- _initialized 守卫防止 HUD 未初始化时显示

## Out of Scope
- 伤害百分比（Story 001）
- 专注值进度条（Story 002）
- 技能槽位（Story 003）
- 边缘警告（Story 005）
- 音效（由音效系统负责）

## QA Test Cases
- test_ko_notification_display: OnKO → "KO!" 显示
- test_ko_notification_fadeout: KO 动画完成后 → 淡出消失
- test_ko_notification_hidden_hud: HUD 隐藏 + OnKO → 不显示
- test_ko_notification_battle_end: KO + BattleEnd → 保持显示

## Test Evidence
- 手动 walkthrough doc + 截图（UI story — ADVISORY）
- 证据路径: production/qa/evidence/hud-ko-notification.md

## Dependencies
- IKnockbackSystem（上游）: OnKO 事件
- HUDController（已有）: OnKO handler
- HUDAnimator: PlayKOSequence 实现
