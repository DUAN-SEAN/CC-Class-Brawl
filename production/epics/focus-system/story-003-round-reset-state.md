# Story 003: Round Reset State

> **Epic**: 专注值系统
> **Status**: Ready
> **Layer**: Core
> **Type**: Logic
> **Estimate**: S
> **Manifest Version**: 2026-05-24
> **Last Updated**: 2026-05-24

## Context

**GDD**: `design/gdd/focus-system.md`
**Requirement**: `TR-FOC-019~021`
**ADR Governing Implementation**: ADR-0009: Focus & Skill Draw Pipeline, ADR-0010: Match & Round Lifecycle
**ADR Decision Summary**: 新一局开始时 FocusPoints 重置为 0，UnlockedCount 重置为 0。按 ADR-0010，CoordinateRoundReset 顺序中 FocusSystem 在 DamageSystem 之后。Reset 方法区分 round-level 和 match-level 重置。
**Engine**: Unity 2022.3.51 LTS | **Risk**: LOW

**Control Manifest Rules (Core)**:
- Required: FocusPoints reset to zero between rounds; UnlockedCount and AlreadyDrawnSkillIds persist across rounds (Feature 层管理)
- Required: CoordinateRoundReset order: DamageSystem -> FocusSystem -> SkillDrawSystem -> ...
- Guardrail: CoordinateRoundReset < 0.5ms (6 system resets x 2 players)

---

## Acceptance Criteria

- [ ] ResetFocus(CharacterId) 将指定角色的 FocusPoints 重置为 0.0，UnlockedCount 重置为 0
- [ ] ResetAll() 将所有角色的 FocusPoints 和 UnlockedCount 重置
- [ ] 重置后触发 OnFocusChanged(CharacterId, 0.0, FocusBaseThreshold) 事件
- [ ] 对局管理系统的 OnRoundStart 事件触发 ResetAll()
- [ ] 角色 KO 后重生时 FocusPoints 保留（不重置）——待对局管理系统定义重生规则
- [ ] 重置操作幂等——连续调用不产生副作用

---

## Implementation Notes

- FocusSystem.ResetAll() 由 MatchManager 在 CoordinateRoundReset 中调用
- 按 ADR-0010 顺序，FocusSystem 重置在 DamageSystem 之后
- ResetFocus 和 ResetAll 只重置 FocusPoints 和 UnlockedCount
- AlreadyDrawnSkillIds 属于 Feature 层（SkillDrawSystem），不在 FocusSystem 范围内
- 重置后对每个角色触发 OnFocusChanged，传入新的 UnlockThreshold（= FocusBaseThreshold，因为 UnlockedCount=0）
- KO 后重生的保留逻辑：FocusSystem 不在 KO 时调用 ResetFocus，由对局管理系统决定

---

## Out of Scope

- 专注值积累（Story 001）
- 解锁事件（Story 002）
- MaxSkillsPerMatch 和溢出处理（Story 004）
- 技能抽取系统重置（skill-draw epic）
- 技能装备管理重置（skill-equipment epic）
- 对局管理系统 CoordinateRoundReset 实现（match-management epic）

---

## QA Test Cases

- **AC-1**: 单角色重置
  - Given: 角色 FocusPoints=30.0, UnlockedCount=2
  - When: 调用 ResetFocus(CharacterId)
  - Then: FocusPoints=0.0, UnlockedCount=0
  - Edge cases: 已为 0 时不报错

- **AC-2**: 全角色重置
  - Given: P1 FocusPoints=40.0, UnlockedCount=1; P2 FocusPoints=20.0, UnlockedCount=0
  - When: 调用 ResetAll()
  - Then: 两者 FocusPoints=0.0, UnlockedCount=0
  - Edge cases: 无角色时不报错

- **AC-3**: 重置事件通知
  - Given: HUD 已订阅 OnFocusChanged
  - When: 调用 ResetAll()
  - Then: 每个角色触发 OnFocusChanged(CharacterId, 0.0, FocusBaseThreshold)
  - Edge cases: 确认 UnlockThreshold 恢复为 FocusBaseThreshold

- **AC-4**: 幂等性
  - Given: 角色 FocusPoints=0.0, UnlockedCount=0
  - When: 连续调用 ResetFocus 3 次
  - Then: 状态不变，无异常
  - Edge cases: 确认无多余事件触发

---

## Test Evidence

**Story Type**: Logic
**Required evidence**: `tests/unit/focus-system/round_reset_state_test.cs`
**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: Story focus-system/001 (FocusSystem runtime), Story focus-system/002 (阈值系统)
- Unlocks: Story 004 (边界情况)
