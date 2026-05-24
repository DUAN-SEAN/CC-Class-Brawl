# Story 002: Knockback Magnitude Integration

> **Epic**: 伤害计算系统
> **Status**: Ready
> **Layer**: Core
> **Type**: Integration
> **Estimate**: S
> **Manifest Version**: 2026-05-24
> **Last Updated**: 2026-05-24

## Context

**GDD**: `design/gdd/damage-calculation-system.md`
**Requirement**: `TR-DMG-011~020`
**ADR Governing Implementation**: ADR-0006: Damage & Knockback Pipeline
**ADR Decision Summary**: DamagePercent 驱动 KnockbackMagnitude 计算，KnockbackMagnitude 通过 OnHitProcessed 事件传递给击退与击飞系统。公式为 BaseKnockbackGrowth * (DamagePercent/100) * BaseKnockback + BaseKnockback。
**Engine**: Unity 2022.3.51 LTS | **Risk**: LOW

**Control Manifest Rules (Core)**:
- Required: DamagePercent update and KnockbackMagnitude calculation synchronous in same frame
- Required: Knockback system must not directly operate on Rigidbody2D; delegate through IMovementController.SetVelocity
- Guardrail: DamageFormulas + KnockbackFormulas per-hit < 1 microsecond

---

## Acceptance Criteria

- [ ] KnockbackMagnitude = BaseKnockbackGrowth * (TargetDamagePercent / 100) * BaseKnockback + BaseKnockback
- [ ] DamagePercent=0 时 KnockbackMagnitude = BaseKnockback（纯基础击退）
- [ ] KnockbackMagnitude 通过 OnHitProcessed 事件传递给击退系统（含 HitPoint、AttackerId、TargetId）
- [ ] 不同职业攻击产生不同的 KnockbackMagnitude（验证 Warrior vs Rogue vs Mage 值差异）
- [ ] 计算使用更新后的 DamagePercent（含本次伤害增量）

---

## Implementation Notes

- KnockbackFormulas.CalculateKnockbackMagnitude 已存在并通过测试
- 本 story 确保 DamageSystem 在命中处理管线中正确调用该公式，并将结果通过事件传递
- OnHitProcessed 事件需包含 KnockbackMagnitude + HitEvent 原始数据（AttackerId, TargetId, HitPoint）
- 击退系统收到 KnockbackMagnitude 后自行计算方向和速度（属于 knockback-launch epic）
- BaseKnockbackGrowth=0.15 来自共享 Constants 或 ArenaConfig

---

## Out of Scope

- 击退向量计算和物理模拟（knockback-launch epic）
- KO 检测（knockback-launch epic）
- BaseKnockback=0 等边界情况处理（Story 004）
- BaseKnockbackGrowth 旋钮 UI（Presentation 层）

---

## QA Test Cases

- **AC-1**: Warrior GroundAttack 击退力度
  - Given: BaseKnockback=8.0, BaseKnockbackGrowth=0.15, DamagePercent=100.0
  - When: 计算击退力度
  - Then: KnockbackMagnitude=9.2
  - Edge cases: DamagePercent=0 → 8.0; DamagePercent=150 → 9.8

- **AC-2**: Rogue GroundAttack 击退力度
  - Given: BaseKnockback=2.0, BaseKnockbackGrowth=0.15, DamagePercent=100.0
  - When: 计算击退力度
  - Then: KnockbackMagnitude=2.3
  - Edge cases: DamagePercent=0 → 2.0; DamagePercent=150 → 2.45

- **AC-3**: 同帧同步计算
  - Given: 命中事件到达
  - When: DamagePercent 更新后
  - Then: KnockbackMagnitude 使用更新后的 DamagePercent 计算
  - Edge cases: 确保使用 DamagePercent_old + BaseDamage 作为 TargetDamagePercent

---

## Test Evidence

**Story Type**: Integration
**Required evidence**: `tests/unit/damage-calculation/knockback_magnitude_integration_test.cs`
**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: Story 001 (DamageSystem runtime)
- Unlocks: knockback-launch epic (KnockbackMagnitude → 击退向量)
