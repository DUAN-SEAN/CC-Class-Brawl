# Story 001: Knockback Vector Calculation

> **Epic**: 击退与击飞系统
> **Status**: Ready
> **Layer**: Core
> **Type**: Logic
> **Estimate**: M
> **Manifest Version**: 2026-05-24
> **Last Updated**: 2026-05-24

## Context

**GDD**: `design/gdd/knockback-launch-system.md`
**Requirement**: `TR-KBL-001~010`
**ADR Governing Implementation**: ADR-0006: Damage & Knockback Pipeline
**ADR Decision Summary**: 击退方向由命中点到目标中心方向决定，KnockbackVector = normalize(horizontalDir, KnockbackLaunchRatio) * KnockbackMagnitude * KnockbackSpeedMultiplier。击退系统通过 IMovementController.SetVelocity 施加，不直接操作 Rigidbody2D。
**Engine**: Unity 2022.3.51 LTS | **Risk**: LOW

**Control Manifest Rules (Core)**:
- Required: Knockback system must not directly operate on Rigidbody2D; delegate through IMovementController.SetVelocity
- Required: All velocity via Rigidbody2D.velocity direct assignment
- Guardrail: KnockbackSystem per-frame (2 players) < 0.1ms

---

## Acceptance Criteria

- [ ] 击退水平方向 horizontalDir = sign(target.position.x - attacker.position.x)
- [ ] 攻击者和被击者 x 坐标相同时，使用攻击者面朝方向作为 horizontalDir
- [ ] 击退方向 knockbackDir = normalize(Vector2(horizontalDir, KnockbackLaunchRatio))
- [ ] 击退速度 KnockbackSpeed = KnockbackMagnitude * KnockbackSpeedMultiplier
- [ ] 最终向量 KnockbackVector = knockbackDir * KnockbackSpeed
- [ ] 攻击者(-2, 0.75)、被击者(2, 0.75)、Magnitude=8.4、Multiplier=2.0 → KnockbackVector=(11.88, 11.88) u/s
- [ ] 攻击者在右侧时 horizontalDir=-1（向左击退）

---

## Implementation Notes

- KnockbackFormulas.CalculateKnockbackVector 已存在（需确认签名），本 story 实现运行时集成
- KnockbackSystem 订阅 DamageSystem 的 OnHitProcessed 事件获取 KnockbackMagnitude
- 需要从 HitEvent 获取 attacker 和 target 的位置信息
- 需要查询攻击者的面朝方向（通过 IMovementController.GetFacing()）
- KnockbackLaunchRatio 和 KnockbackSpeedMultiplier 来自 AttackData 或全局配置
- 击退向量传递给格斗状态机（CombatFSM），由 FSM 委托 3C 施加 SetVelocity

---

## Out of Scope

- 击退物理模拟（速度衰减、重力）（Story 002）
- KO 检测（Story 003）
- 击退状态生命周期管理（Story 004）
- 击退视觉特效（Presentation 层）

---

## QA Test Cases

- **AC-1**: 标准击退向量
  - Given: attacker=(-2, 0.75), target=(2, 0.75), KnockbackMagnitude=8.4, KnockbackSpeedMultiplier=2.0, KnockbackLaunchRatio=1.0
  - When: 计算击退向量
  - Then: KnockbackVector = (11.88, 11.88) u/s
  - Edge cases: 精度 ±0.01

- **AC-2**: 反向击退
  - Given: attacker=(2, 0.75), target=(-2, 0.75)
  - When: 计算击退方向
  - Then: horizontalDir = -1
  - Edge cases: 确认向左击退

- **AC-3**: x 坐标相同
  - Given: attacker=(0, 0.75), target=(0, 0.75), attacker facing right
  - When: 计算击退方向
  - Then: horizontalDir = 1（使用面朝方向）
  - Edge cases: attacker facing left → horizontalDir = -1

---

## Test Evidence

**Story Type**: Logic
**Required evidence**: `tests/unit/knockback-launch/knockback_vector_calculation_test.cs`
**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: Story damage-calculation/002 (OnHitProcessed 事件提供 KnockbackMagnitude)
- Unlocks: Story 002 (物理模拟), Story 003 (KO 检测), Story 004 (状态管理)
