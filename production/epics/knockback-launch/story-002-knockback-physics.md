# Story 002: Knockback Physics

> **Epic**: 击退与击飞系统
> **Status**: Ready
> **Layer**: Core
> **Type**: Logic
> **Estimate**: M
> **Manifest Version**: 2026-05-24
> **Last Updated**: 2026-05-24

## Context

**GDD**: `design/gdd/knockback-launch-system.md`
**Requirement**: `TR-KBL-011~020`
**ADR Governing Implementation**: ADR-0006: Damage & Knockback Pipeline
**ADR Decision Summary**: 击退期间速度在 FixedUpdate 60Hz 中更新——水平衰减 Vx *= KnockbackDecayRate，垂直受重力 Vy -= Gravity * dt，终端速度钳制。恢复期使用更快的 KnockbackRecoveryRate 衰减到 MaxAirSpeed。
**Engine**: Unity 2022.3.51 LTS | **Risk**: LOW

**Control Manifest Rules (Core)**:
- Required: All velocity updates use direct Rigidbody2D.velocity assignment, never AddForce
- Required: All movement in FixedUpdate 60Hz
- Required: Gravity 32.0 u/s^2, TerminalVelocity 20.0 u/s from shared Constants
- Required: Knockback system delegates through IMovementController.SetVelocity
- Guardrail: KnockbackSystem per-frame < 0.1ms

---

## Acceptance Criteria

- [ ] 不可操作期：Vx_new = Vx * KnockbackDecayRate, Vy_new = Max(Vy - Gravity * dt, -TerminalVelocity)
- [ ] 初始 KnockbackVector=(11.88, 11.88)、DecayRate=0.99、Gravity=32.0 → 1帧后 Vx=11.76, Vy=11.35
- [ ] 恢复期：如果 |Vx| > MaxAirSpeed 则 Vx *= KnockbackRecoveryRate，否则 3C 正常接管
- [ ] 恢复期 Vx=10.0、MaxAirSpeed=3.5、RecoveryRate=0.92 → 约 13 帧后 |Vx| <= 3.5
- [ ] KnockbackDecayRate > 1.0 时钳制为 1.0（速度不应增加）
- [ ] KnockbackRecoveryRate >= 1.0 时钳制为 0.95（必须衰减）
- [ ] 着地时：垂直速度归零，水平速度由 3C 地面摩擦接管

---

## Implementation Notes

- KnockbackSystem 在 FixedUpdate 中更新处于 Knockback 状态的角色速度
- 使用 KnockbackRuntimeState 跟踪每个角色当前击退阶段（不可操作期/恢复期）和速度
- 不可操作期由 HitstunFrames 控制，帧计数器归零后转入恢复期
- 恢复期检测 |Vx| <= MaxAirSpeed 或着地事件后结束
- 通过 IMovementController.SetVelocity 更新速度
- Gravity=32.0, TerminalVelocity=20.0 来自共享 Constants 类
- dt 使用 Time.fixedDeltaTime (1/60)

---

## Out of Scope

- 击退向量计算（Story 001）
- KO 检测（Story 003）
- 击退状态与格斗状态机的集成（Story 004）
- 拖尾视觉效果（Presentation 层）
- DI（方向影响）机制（MVP 后考虑）

---

## QA Test Cases

- **AC-1**: 不可操作期 1 帧更新
  - Given: Vx=11.88, Vy=11.88, KnockbackDecayRate=0.99, Gravity=32.0
  - When: 执行 1 帧物理更新
  - Then: Vx=11.76, Vy=11.35
  - Edge cases: 精度 ±0.01

- **AC-2**: 多帧 hitstun 物理更新
  - Given: 初始 Vx=11.88, Vy=11.88, DecayRate=0.99
  - When: 执行 9 帧
  - Then: Vx ≈ 10.85, 总位移约 (1.75, 1.65) u
  - Edge cases: 确认每帧速度单调衰减

- **AC-3**: 恢复期衰减
  - Given: Vx=10.0, MaxAirSpeed=3.5, KnockbackRecoveryRate=0.92
  - When: 执行恢复衰减
  - Then: 约 13 帧后 |Vx| <= 3.5
  - Edge cases: Vx 初始已 < MaxAirSpeed 时不衰减

- **AC-4**: DecayRate 钳制
  - Given: KnockbackDecayRate=1.05
  - When: 初始化
  - Then: DecayRate 钳制为 1.0
  - Edge cases: RecoveryRate=1.0 → 钳制为 0.95

---

## Test Evidence

**Story Type**: Logic
**Required evidence**: `tests/unit/knockback-launch/knockback_physics_test.cs`
**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: Story knockback-launch/001 (KnockbackVector 传入)
- Unlocks: Story 003 (KO 检测), Story 004 (状态管理)
