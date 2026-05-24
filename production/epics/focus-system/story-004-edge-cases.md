# Story 004: Edge Cases

> **Epic**: 专注值系统
> **Status**: Ready
> **Layer**: Core
> **Type**: Logic
> **Estimate**: S
> **Manifest Version**: 2026-05-24
> **Last Updated**: 2026-05-24

## Context

**GDD**: `design/gdd/focus-system.md`
**Requirement**: `TR-FOC-022~023`
**ADR Governing Implementation**: ADR-0009: Focus & Skill Draw Pipeline
**ADR Decision Summary**: MaxSkillsPerMatch=4 达到后不再触发解锁；FocusCap 防止囤积；FocusPoints 浮点精度钳制；同帧互命中各自独立处理；不连续触发两次解锁。
**Engine**: Unity 2022.3.51 LTS | **Risk**: LOW

**Control Manifest Rules (Core)**:
- Required: FocusSystem uses ClampFocus to enforce FocusCap
- Required: FocusPoints reset to zero between rounds
- Guardrail: FocusSystem per-hit processing < 0.01ms

---

## Acceptance Criteria

- [ ] UnlockedCount >= MaxSkillsPerMatch (4) 时：专注值继续积累但不再触发 OnFocusReady
- [ ] UnlockedCount >= MaxSkillsPerMatch 且 FocusPoints >= FocusCap：钳制到 FocusCap，不触发解锁
- [ ] 一次命中增量导致 FocusPoints 同时超过 UnlockThreshold 和 FocusCap：先钳制到 FocusCap，再判定解锁
- [ ] 解锁后剩余 FocusPoints 超过下一次阈值：不连续触发，等下一帧的下一个命中事件
- [ ] FocusPoints 因浮点精度超过 FocusCap：每次更新后强制 ClampFocus
- [ ] 两个角色同一帧互相命中，都达到解锁阈值：各自独立触发 OnFocusReady
- [ ] CharacterId 无效或不存在：忽略事件，记录警告
- [ ] UnlockedCount 超过 MaxSkillsPerMatch（不应发生）：钳制到 MaxSkillsPerMatch，记录警告

---

## Implementation Notes

- 在专注值更新和解锁判定的关键路径中加入防御性检查
- MaxSkillsPerMatch 检查在解锁判定之前：if (UnlockedCount >= MaxSkillsPerMatch) return
- ClampFocus 在每次 FocusPoints 变更后调用
- 同帧互命中：两个 OnAttackHit 事件独立处理，互不干扰
- 不连续触发：解锁处理是原子操作，一个命中事件只触发一次
- Invalid CharacterId 检查在 Dictionary 查找时自然处理

---

## Out of Scope

- 专注值积累核心逻辑（Story 001）
- 解锁阈值和事件（Story 002）
- 重置逻辑（Story 003）
- HUD 满条/灰色显示（Presentation 层）
- 技能抽取系统的空池处理（skill-draw epic）

---

## QA Test Cases

- **AC-1**: 解锁上限
  - Given: UnlockedCount=4（已达 MaxSkillsPerMatch）
  - When: FocusPoints 积累到 FocusCap
  - Then: 不触发 OnFocusReady，FocusPoints 钳制到 FocusCap
  - Edge cases: UnlockedCount=3 时正常触发第 4 次解锁

- **AC-2**: 同时超过阈值和上限
  - Given: FocusPoints=38.0, FocusCap=55.0, UnlockThreshold=40.0
  - When: FocusGain=20.0
  - Then: FocusPoints_new=Min(58.0, 55.0)=55.0, 触发解锁, FocusPoints_final=15.0
  - Edge cases: 确认先钳制再判定

- **AC-3**: 不连续触发
  - Given: FocusPoints_final=45.0（解锁后）, 下一阈值=45.0
  - When: 同一命中事件处理
  - Then: 不触发第二次解锁
  - Edge cases: 下一次命中时 FocusPoints=45.0 >= 45.0 → 触发

- **AC-4**: 同帧互命中
  - Given: P1 和 P2 同一帧互相命中，P1 BaseDamage=12.0, P2 BaseDamage=4.0
  - When: 两个 OnAttackHit 处理完成
  - Then: P1 FocusPoints += (3.6 + 0.4) = 4.0, P2 FocusPoints += (1.2 + 1.2) = 2.4
  - Edge cases: 两者独立触发解锁互不干扰

- **AC-5**: 无效 CharacterId
  - Given: OnAttackHit 中 CharacterId 不存在
  - When: 专注值系统处理该事件
  - Then: 忽略，不更新专注值，记录警告
  - Edge cases: 确认不影响其他角色

---

## Test Evidence

**Story Type**: Logic
**Required evidence**: `tests/unit/focus-system/focus_edge_cases_test.cs`
**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: Story focus-system/001 (积累), Story focus-system/002 (解锁), Story focus-system/003 (重置)
- Unlocks: skill-draw epic (专注值解锁后触发技能抽取)
