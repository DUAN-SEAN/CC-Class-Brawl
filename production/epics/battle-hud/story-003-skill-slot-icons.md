# Story 003: 技能槽位图标 — 订阅 OnSkillEquipped/OnSkillUnequipped

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
- **GDD**: design/gdd/battle-hud.md — Core Rules 4 (技能槽位组)
- **ADR**: ADR-0014 Section 5 (OnSkillEquipped/OnSkillUnequipped 处理器), Section 6 (技能装备动画)
- **Existing Code**: Presentation/HUD/HUDController.cs (OnSkillEquipped/OnSkillUnequipped handlers), Presentation/HUD/PlayerHUDView.cs (UpdateSkillSlot)
- **TR-IDs**: TR-HUD-001, TR-HUD-019
- **Control Manifest**: 2026-05-24

## Acceptance Criteria
- **GIVEN** 所有槽位为空, **WHEN** HUD 初始化, **THEN** 显示 4 个灰色半透明轮廓空槽位 #555555，带按键提示（P1: "1"/"2"/"3"/"4"）
- **GIVEN** Slot 1 为空, **WHEN** OnSkillEquipped(P1, 1, Fireball[Common]), **THEN** Slot 1 显示火球图标 + 蓝色边框 #4488FF，播放弹入动画（0→1.2x→1.0x, 0.25s）
- **GIVEN** Slot 1 已装备 Fireball, **WHEN** FSM 进入 Fireball.Startup, **THEN** Slot 1 高亮 + 脉动边框（2Hz）
- **GIVEN** Slot 1 Fireball 执行中被 HitStun 打断, **WHEN** FSM 状态变化, **THEN** Slot 1 短暂闪红（0.2s）
- **GIVEN** Slot 1-4 全部已装备, **WHEN** OnSkillUnequipped(P1, 1/2/3/4) 到达, **THEN** 所有槽位恢复为灰色空槽 + 按键提示
- **GIVEN** OnSkillEquipped 到达时槽位已有图标（不应发生）, **THEN** 覆盖为新图标，重新播放装备动画，记录警告

## Implementation Notes
- HUDController.OnSkillEquipped 已存在，委托给 PlayerHUDView.UpdateSkillSlot(slotIndex, skillData, tuning)
- HUDController.OnSkillUnequipped 已存在，委托给 PlayerHUDView.UpdateSkillSlot(slotIndex, null, tuning)
- PlayerHUDView.UpdateSkillSlot 需实现:
  (1) skillData != null: 设置图标 (SkillData.Icon)、稀有度边框颜色、播放弹入动画
  (2) skillData == null: 恢复为空槽位（灰色轮廓 + 按键提示）
- 稀有度边框颜色: Common #4488FF, Rare #8844CC, Epic #FFB800
- 弹入动画: scale 0→1.2→1.0, 0.25s, EaseOutBack（通过 USS transition）
- 高亮/脉动: FSM 状态变化事件驱动（需订阅 ICombatStateProvider 或 CombatFSM 状态事件）
- 闪红:被打断时通过 USS class "interrupted" 触发 0.2s 红色闪烁
- 每个槽位约 48x48px（1920x1080 基准）
- 技能图标: SkillData.Icon (Sprite) → 转为 BackgroundImage 或单独的 Image 元素

## Out of Scope
- 伤害百分比（Story 001）
- 专注值进度条（Story 002）
- KO 通知（Story 004）
- 边缘警告（Story 005）
- 技能选择叠加层（技能抽取 UI）

## QA Test Cases
- test_skill_slot_empty_init: 初始 → 4 个灰色空槽 + 按键提示
- test_skill_slot_equip: OnSkillEquipped → 图标 + 稀有度边框 + 弹入动画
- test_skill_slot_unequip: OnSkillUnequipped → 灰色空槽
- test_skill_slot_active_highlight: FSM Startup → 高亮 + 脉动
- test_skill_slot_interrupt_flash: HitStun 打断 → 闪红 0.2s
- test_skill_slot_overwrite: 已有图标覆盖 → 重新动画

## Test Evidence
- 手动 walkthrough doc + 截图（UI story — ADVISORY）
- 证据路径: production/qa/evidence/hud-skill-slots.md

## Dependencies
- ISkillEquipmentManager（上游）: OnSkillEquipped/OnSkillUnequipped 事件
- SkillData: Icon + Rarity 字段
- HUDController（已有）: 事件订阅
- CombatFSM（软依赖）: 技能激活/打断状态事件
