# Epic: 战斗HUD

> **Layer**: Presentation
> **GDD**: design/gdd/battle-hud.md
> **Architecture Module**: 战斗HUD (pure consumer)
> **Status**: Ready
> **Stories**: Not yet created — run `/create-stories battle-hud`

## Overview

实现战斗界面的所有 HUD 元素：伤害百分比显示、专注值进度条、已装备技能图标、KO 通知、边缘警告。HUD 为纯消费者，不写入任何游戏逻辑，仅订阅事件并渲染。使用 UI Toolkit (UXML/USS) 实现，所有动画和视觉反馈遵循 UX 视觉设计规格。性能预算 < 0.8ms。

## Governing ADRs

| ADR | Decision Summary | Engine Risk |
|-----|-----------------|-------------|
| ADR-0014: UI Architecture Battle HUD | UI Toolkit 框架选择、事件订阅模式、被动渲染架构 | MEDIUM |

## GDD Requirements

| TR Range | Count | ADR Coverage |
|----------|-------|--------------|
| TR-HUD-001 ~ TR-HUD-028 | 28 | ADR-0014 |

Full requirement list: `docs/architecture/tr-registry.yaml` (HUD section)

## Existing Code

| File | Type |
|------|------|
| Presentation/HUD/HUDController.cs | Controller |
| Presentation/HUD/PlayerHUDView.cs | View |
| Presentation/HUD/HUDAnimator.cs | Animator |
| Presentation/HUD/HUDTuningData.cs | Tuning data |

**Status**: HUD 基础框架已实现，事件订阅和视觉动画集成待完善。

## Definition of Done

This epic is complete when:
- All stories are implemented, reviewed, and closed via `/story-done`
- All acceptance criteria from `design/gdd/battle-hud.md` are verified
- All Logic and Integration stories have passing test files in `tests/`
- All Visual/Feel and UI stories have evidence docs with sign-off in `production/qa/evidence/`

## Stories

| # | Story | Type | Layer | Estimate | Status | Dependencies |
|---|-------|------|-------|----------|--------|--------------|
| 1 | [伤害百分比显示](story-001-damage-percent-display.md) | UI | Presentation | 2h | Ready | IDamageSystem |
| 2 | [专注值进度条](story-002-focus-progress-bar.md) | UI | Presentation | 3h | Ready | IFocusSystem |
| 3 | [技能槽位图标](story-003-skill-slot-icons.md) | UI | Presentation | 3h | Ready | ISkillEquipmentManager |
| 4 | [KO 通知](story-004-ko-notification.md) | UI | Presentation | 2h | Ready | IKnockbackSystem |
| 5 | [边缘警告](story-005-edge-warning.md) | UI | Presentation | 2h | Ready | IArenaDataProvider |
| 6 | [HUD 性能优化](story-006-hud-performance-optimization.md) | Logic | Presentation | 2h | Ready | Stories 1-5 |

**Total Estimate**: 14 hours

## Next Step

Stories created. Begin implementation with Story 001 (伤害百分比显示).
