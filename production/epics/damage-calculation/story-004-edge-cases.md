# Story 004: Edge Cases

> **Epic**: 伤害计算系统
> **Status**: Ready
> **Layer**: Core
> **Type**: Logic
> **Estimate**: S
> **Manifest Version**: 2026-05-24
> **Last Updated**: 2026-05-24

## Context

**GDD**: `design/gdd/damage-calculation-system.md`
**Requirement**: `TR-DMG-026~030`
**ADR Governing Implementation**: ADR-0006: Damage & Knockback Pipeline
**ADR Decision Summary**: AttackId 无效时忽略命中；BaseKnockback=0 时 KnockbackMagnitude=0，角色进入 HitStun；DamagePercent 不为负；999+ 显示处理。
**Engine**: Unity 2022.3.51 LTS | **Risk**: LOW

**Control Manifest Rules (Core)**:
- Required: DamagePercent only ever increases (MVP)
- Required: DamageFormulas pure static, 100% unit-testable
- Guardrail: Full pipeline < 0.2ms per frame

---

## Acceptance Criteria

- [ ] AttackId 无效或未注册：忽略命中，不更新 DamagePercent，记录错误日志
- [ ] BaseKnockback=0（数据错误）：KnockbackMagnitude=0，攻击造成伤害但不产生击退，记录警告
- [ ] BaseKnockbackGrowth=0：KnockbackMagnitude 恒等于 BaseKnockback，不受百分比影响（合法但消除张力曲线）
- [ ] KnockbackMagnitude 计算结果为负数（不应发生）：钳制为 0.0，记录警告
- [ ] DamagePercent 超过 999.0：不钳制，无上限；显示 999+
- [ ] DamagePercent 为负数（数据错误）：钳制为 0.0，记录警告
- [ ] AttackData.BaseDamage=0：DamagePercent 不增加，KnockbackMagnitude 仍正常计算（纯击退攻击）

---

## Implementation Notes

- 在 DamageSystem.OnHitDetected 回调中加入防御性检查
- 使用 UnityEngine.Debug.LogWarning 和 Debug.LogError 记录异常
- DamagePercent < 0 的钳制在每次更新后执行
- KnockbackMagnitude < 0 的钳制在 CalculateKnockbackMagnitude 后执行
- 999+ 显示逻辑：DisplayPercent 使用 int，超过 999 时由 HUD 处理显示（DamageSystem 只提供 float 值）

---

## Out of Scope

- HUD 999+ 显示实现（Presentation 层）
- BaseKnockback=0 的修复（属于数据配置问题）
- 对局管理系统的重置触发（Story 003）

---

## QA Test Cases

- **AC-1**: AttackId 无效
  - Given: HitEvent.AttackId = "nonexistent_attack"
  - When: OnHitDetected 触发
  - Then: DamagePercent 不变，错误日志已记录
  - Edge cases: AttackId = "" (空字符串)

- **AC-2**: BaseKnockback=0
  - Given: AttackData.BaseKnockback = 0
  - When: 命中处理
  - Then: KnockbackMagnitude = 0，DamagePercent 正常增加，警告已记录
  - Edge cases: BaseKnockback 为负数

- **AC-3**: DamagePercent 超过 999
  - Given: DamagePercent = 995.0
  - When: 被 BaseDamage=12.0 的攻击命中
  - Then: DamagePercent = 1007.0（不钳制）
  - Edge cases: DamagePercent = 999.0 + 0.5 = 999.5

- **AC-4**: KnockbackMagnitude 负数钳制
  - Given: 计算结果因浮点误差为 -0.001
  - When: 计算 KnockbackMagnitude
  - Then: KnockbackMagnitude = 0.0，警告已记录
  - Edge cases: 确保正常值不受影响

- **AC-5**: BaseDamage=0 纯击退攻击
  - Given: AttackData.BaseDamage = 0, BaseKnockback = 5.0
  - When: 命中处理
  - Then: DamagePercent 不变，KnockbackMagnitude 正常计算
  - Edge cases: BaseDamage=0 + BaseKnockback=0 → 完全无效攻击

---

## Test Evidence

**Story Type**: Logic
**Required evidence**: `tests/unit/damage-calculation/edge_cases_test.cs`
**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: Story 001 (DamageSystem runtime), Story 002 (KnockbackMagnitude integration)
- Unlocks: None
