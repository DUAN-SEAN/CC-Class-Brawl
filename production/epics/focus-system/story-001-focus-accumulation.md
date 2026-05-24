# Story 001: Focus Accumulation

> **Epic**: 专注值系统
> **Status**: Ready
> **Layer**: Core
> **Type**: Logic
> **Estimate**: M
> **Manifest Version**: 2026-05-24
> **Last Updated**: 2026-05-24

## Context

**GDD**: `design/gdd/focus-system.md`
**Requirement**: `TR-FOC-001~010`
**ADR Governing Implementation**: ADR-0009: Focus & Skill Draw Pipeline
**ADR Decision Summary**: FocusSystem 订阅 OnAttackHit 事件，攻击者获得 BaseDamage * FocusGainRate_Attacker 专注值，被击者获得 BaseDamage * FocusGainRate_Defender 专注值。两者在同一回调中同步处理。FocusFormulas 为纯静态类。
**Engine**: Unity 2022.3.51 LTS | **Risk**: LOW

**Control Manifest Rules (Core)**:
- Required: FocusFormulas must be pure static class, 100% unit-testable
- Required: Both attacker and defender focus updates occur in same OnAttackHit callback, same frame
- Required: FocusSystem uses ClampFocus to enforce FocusCap
- Guardrail: FocusSystem per-hit processing < 0.01ms

---

## Acceptance Criteria

- [ ] FocusSystem 订阅攻击系统的 OnAttackHit(AttackData, AttackerId, TargetId) 事件
- [ ] 攻击者获得 FocusGain_Attacker = BaseDamage * FocusGainRate_Attacker
- [ ] 被击者获得 FocusGain_Defender = BaseDamage * FocusGainRate_Defender
- [ ] Warrior GroundAttack (BaseDamage=12.0) 命中：攻击者获得 12.0 * 0.30 = 3.6 专注值
- [ ] Rogue GroundAttack (BaseDamage=4.0) 命中：攻击者获得 4.0 * 0.30 = 1.2 专注值
- [ ] 被击者补偿约为攻击者的 1/3（FocusGainRate_Defender=0.10 vs Attacker=0.30）
- [ ] FocusPoints 钳制到 [0, FocusCap]，使用 ClampFocus
- [ ] BaseDamage=0 时 FocusGain=0.0（纯击退攻击不提供专注值）
- [ ] AttackData=null 时忽略该事件，记录警告
- [ ] 每次专注值变化触发 OnFocusChanged(CharacterId, FocusPoints, UnlockThreshold) 事件

---

## Implementation Notes

- FocusSystem 实现 IFocusSystem 接口
- 使用 Dictionary<CharacterId, FocusRuntimeState> 存储每个角色的专注值状态
- FocusRuntimeState 包含 FocusPoints (float), UnlockedCount (int)
- FocusGainRate_Attacker 和 FocusGainRate_Defender 从配置加载（FocusConfig SO 或 Constants）
- FocusCap 默认 55.0
- OnEnable 订阅、OnDisable 取消订阅（ADR-0008）
- FocusFormulas.CalculateFocusGain 已存在，本 story 集成到 MonoBehaviour 运行时

---

## Out of Scope

- 解锁阈值判定和解锁事件（Story 002）
- 重置逻辑（Story 003）
- 解锁上限和溢出处理（Story 004）
- HUD 进度条显示（Presentation 层）
- 专注值视觉特效（Presentation 层）

---

## QA Test Cases

- **AC-1**: 攻击者专注值获取
  - Given: 角色 FocusPoints=10.0, FocusGainRate_Attacker=0.30
  - When: 攻击命中对手（BaseDamage=12.0）
  - Then: FocusPoints = 10.0 + 3.6 = 13.6
  - Edge cases: FocusPoints=0 命中后 = 3.6

- **AC-2**: 被击者专注值获取
  - Given: 角色 FocusPoints=5.0, FocusGainRate_Defender=0.10
  - When: 被 BaseDamage=12.0 的攻击命中
  - Then: FocusPoints = 5.0 + 1.2 = 6.2
  - Edge cases: FocusGainRate_Defender=0 → 被击者不获取

- **AC-3**: 双方独立积累
  - Given: P1 命中 P2（BaseDamage=8.0）
  - When: 命中处理完成
  - Then: P1 FocusPoints += 2.4, P2 FocusPoints += 0.8
  - Edge cases: 两者在同一回调中同步处理

- **AC-4**: BaseDamage=0
  - Given: 攻击 BaseDamage=0.0
  - When: 命中处理
  - Then: 攻击者和被击者 FocusPoints 均不变
  - Edge cases: FocusGain = 0.0 * Rate = 0.0

- **AC-5**: FocusCap 钳制
  - Given: FocusPoints=53.0, FocusCap=55.0, FocusGain=4.0
  - When: 专注值更新
  - Then: FocusPoints = Min(57.0, 55.0) = 55.0
  - Edge cases: FocusPoints 已达 FocusCap 时不变

---

## Test Evidence

**Story Type**: Logic
**Required evidence**: `tests/unit/focus-system/focus_accumulation_test.cs`
**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: 攻击系统 (OnAttackHit 事件), FocusFormulas (已有)
- Unlocks: Story 002 (阈值解锁), Story 003 (重置), Story 004 (边界情况)
