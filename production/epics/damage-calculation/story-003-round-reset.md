# Story 003: Round Reset

> **Epic**: 伤害计算系统
> **Status**: Ready
> **Layer**: Core
> **Type**: Logic
> **Estimate**: S
> **Manifest Version**: 2026-05-24
> **Last Updated**: 2026-05-24

## Context

**GDD**: `design/gdd/damage-calculation-system.md`
**Requirement**: `TR-DMG-021~025`
**ADR Governing Implementation**: ADR-0006: Damage & Knockback Pipeline
**ADR Decision Summary**: DamageSystem 提供 ResetDamage(CharacterId) 和 ResetAll() 接口，由对局管理系统在 OnRoundStart 时调用。KO 后重生也触发重置。
**Engine**: Unity 2022.3.51 LTS | **Risk**: LOW

**Control Manifest Rules (Core)**:
- Required: CoordinateRoundReset order: DamageSystem first
- Required: DamagePercent stored as float
- Guardrail: CoordinateRoundReset < 0.5ms (6 system resets x 2 players)

---

## Acceptance Criteria

- [ ] ResetDamage(CharacterId) 将指定角色的 DamagePercent 重置为 0.0
- [ ] ResetAll() 将所有角色的 DamagePercent 重置为 0.0
- [ ] 重置后触发 OnDamagePercentChanged 事件（值=0.0），通知 HUD 更新显示
- [ ] 对局管理系统的 OnRoundStart 事件触发 ResetAll()
- [ ] 角色 KO 后重生时调用 ResetDamage(CharacterId)
- [ ] 重置操作幂等——连续调用不会产生副作用

---

## Implementation Notes

- DamageSystem 订阅对局管理系统的 OnRoundStart 事件（或由 MatchManager 在 CoordinateRoundReset 中直接调用）
- 按 ADR-0010，CoordinateRoundReset 顺序为 DamageSystem 优先
- ResetAll 遍历 Dictionary<CharacterId, float> 中所有条目设为 0.0
- 重置后对每个角色触发 OnDamagePercentChanged(CharacterId, 0.0f)
- 无需记录重置前的 DamagePercent（MVP 无回放/统计需求）

---

## Out of Scope

- 对局管理系统本身的实现（match-management epic）
- 专注值系统重置（focus-system epic）
- 技能系统重置（skill-draw / skill-equipment epic）

---

## QA Test Cases

- **AC-1**: 单角色重置
  - Given: 角色 DamagePercent=150.0
  - When: 调用 ResetDamage(CharacterId)
  - Then: DamagePercent=0.0
  - Edge cases: DamagePercent 已经是 0.0 时不报错

- **AC-2**: 全角色重置
  - Given: P1 DamagePercent=80.0, P2 DamagePercent=120.0
  - When: 调用 ResetAll()
  - Then: 两个角色 DamagePercent 均为 0.0
  - Edge cases: 无角色时不报错

- **AC-3**: 重置事件通知
  - Given: HUD 已订阅 OnDamagePercentChanged
  - When: 调用 ResetAll()
  - Then: 每个角色均触发一次 OnDamagePercentChanged(CharacterId, 0.0f)
  - Edge cases: 无订阅者时不报错

- **AC-4**: 幂等性
  - Given: 角色 DamagePercent=0.0
  - When: 连续调用 ResetDamage(CharacterId) 3 次
  - Then: DamagePercent 仍为 0.0，无异常
  - Edge cases: 确认无多余事件触发

---

## Test Evidence

**Story Type**: Logic
**Required evidence**: `tests/unit/damage-calculation/round_reset_test.cs`
**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: Story 001 (DamageSystem runtime with ResetDamage/ResetAll interface)
- Unlocks: Story 004 (边界情况)
