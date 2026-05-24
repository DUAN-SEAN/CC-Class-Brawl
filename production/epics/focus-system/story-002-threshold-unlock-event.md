# Story 002: Threshold Unlock Event

> **Epic**: 专注值系统
> **Status**: Ready
> **Layer**: Core
> **Type**: Logic
> **Estimate**: M
> **Manifest Version**: 2026-05-24
> **Last Updated**: 2026-05-24

## Context

**GDD**: `design/gdd/focus-system.md`
**Requirement**: `TR-FOC-011~018`
**ADR Governing Implementation**: ADR-0009: Focus & Skill Draw Pipeline
**ADR Decision Summary**: 解锁阈值递增——UnlockThreshold_n = FocusBaseThreshold + n * FocusThresholdGrowth。FocusPoints >= UnlockThreshold 且 UnlockedCount < MaxSkillsPerMatch 时自动触发 OnFocusReady，无需玩家确认。解锁后 FocusPoints 扣减阈值、UnlockedCount 递增。
**Engine**: Unity 2022.3.51 LTS | **Risk**: LOW

**Control Manifest Rules (Core)**:
- Required: FocusFormulas must be pure static class, 100% unit-testable
- Required: Both attacker and defender focus updates occur in same callback, same frame
- Guardrail: Full pipeline triggered by one hit < 0.1ms

---

## Acceptance Criteria

- [ ] UnlockThreshold_n = FocusBaseThreshold + n * FocusThresholdGrowth（n=已解锁数）
- [ ] 第一次解锁：UnlockThreshold = 40.0（FocusBaseThreshold=40.0, n=0）
- [ ] 第二次解锁：UnlockThreshold = 45.0（n=1, Growth=5.0）
- [ ] 第三次解锁：UnlockThreshold = 50.0（n=2）
- [ ] 第四次解锁：UnlockThreshold = 55.0（n=3）
- [ ] FocusPoints >= UnlockThreshold 且 UnlockedCount < MaxSkillsPerMatch 时自动触发 OnFocusReady(CharacterId, UnlockedCount)
- [ ] 解锁后 FocusPoints -= UnlockThreshold_n，UnlockedCount += 1
- [ ] 解锁在同一帧内自动完成，不需要玩家按键
- [ ] FocusPoints=38.0、BaseDamage=12.0、GainRate=0.30 → FocusPoints_new=41.6 → 触发解锁 → FocusPoints_final=1.6, UnlockedCount=1
- [ ] 解锁阈值计算结果为负数或零时：强制最小值 1.0

---

## Implementation Notes

- 在 Story 001 的专注值积累逻辑之后，添加阈值判定和解锁触发
- FocusFormulas.CalculateUnlockThreshold 已存在
- 解锁判定在 ClampFocus 之后执行（先钳制、再判定）
- OnFocusReady 事件通知技能抽取系统执行随机抽取
- 不连续触发——一次命中只触发一次解锁，即使剩余 FocusPoints 超过下一阈值
- 下一阈值的判定在下一个命中事件中处理

---

## Out of Scope

- 专注值积累细节（Story 001）
- 重置逻辑（Story 003）
- MaxSkillsPerMatch 上限和 FocusCap 溢出处理（Story 004）
- 技能抽取系统实现（skill-draw epic）
- 解锁视觉特效（Presentation 层）

---

## QA Test Cases

- **AC-1**: 首次解锁
  - Given: FocusPoints=38.0, UnlockThreshold=40.0, UnlockedCount=0
  - When: 获得专注值 3.6（BaseDamage=12.0 * 0.30）
  - Then: FocusPoints_new=41.6 >= 40.0, 触发 OnFocusReady, FocusPoints_final=1.6, UnlockedCount=1
  - Edge cases: FocusPoints 恰好等于阈值 → 触发

- **AC-2**: 极端增益解锁
  - Given: FocusPoints=38.0, UnlockThreshold=40.0, FocusCap=55.0
  - When: 获得极端 FocusGain=20.0
  - Then: FocusPoints_new=Min(58.0, 55.0)=55.0, 触发解锁, FocusPoints_final=55.0-40.0=15.0
  - Edge cases: FocusCap 钳制不影响解锁触发

- **AC-3**: 递增阈值序列
  - Given: FocusBaseThreshold=40.0, FocusThresholdGrowth=5.0
  - When: n=0,1,2,3
  - Then: 阈值分别为 40.0, 45.0, 50.0, 55.0
  - Edge cases: n=4 超出 MaxSkillsPerMatch 不应触发

- **AC-4**: 阈值负数防护
  - Given: FocusBaseThreshold=-5.0（数据错误）
  - When: 计算阈值
  - Then: 强制最小值 1.0
  - Edge cases: FocusThresholdGrowth 为负数时同理

---

## Test Evidence

**Story Type**: Logic
**Required evidence**: `tests/unit/focus-system/threshold_unlock_event_test.cs`
**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: Story focus-system/001 (专注值积累)
- Unlocks: Story 003 (重置), Story 004 (边界情况), skill-draw epic (OnFocusReady 消费)
