# Story: HitStun + Knockback State Entry

> **Epic**: combat-state-machine
> **Status**: Ready
> **Layer**: Core
> **Type**: Logic
> **Estimate**: M (3-4 hours)
> **Control Manifest Version**: 2026-05-24

## Context

- **GDD**: `design/gdd/combat-state-machine.md`
- **TR Range**: TR-CBT-008, TR-CBT-012, TR-CBT-013, TR-CBT-014, TR-CBT-015, TR-CBT-019, TR-CBT-020, TR-CBT-021, TR-CBT-022, TR-CBT-023, TR-CBT-024, TR-CBT-025, TR-CBT-038, TR-CBT-039
- **Governing ADR**: ADR-0002 (Dual FSM Architecture)
- **Engine**: Unity 2022.3.51 LTS, LOW risk

## Summary

实现受击后的状态转换逻辑：根据击退力度（KnockbackMagnitude）与阈值（KnockbackThreshold=9.0）的比较，决定进入 HitStun 还是 Knockback。包括 HitStun 计时器、KnockbackHitstunFrames 公式计算、HitStun 中再次被击中的重置逻辑、Knockback 着地结束、以及强制取消攻击的优先级。

## Acceptance Criteria (from GDD)

### Knockback vs HitStun 判定
- **GIVEN** KnockbackThreshold = 9.0, **WHEN** 击退力度 = 2.0, **THEN** 进入 HitStun
- **GIVEN** KnockbackThreshold = 9.0, **WHEN** 击退力度 = 9.0, **THEN** 进入 HitStun（严格大于判定）
- **GIVEN** KnockbackThreshold = 9.0, **WHEN** 击退力度 = 25.0, **THEN** 进入 Knockback

### HitStun 持续
- **GIVEN** 攻击定义 HitStunFrames = 8, **WHEN** 角色进入 HitStun, **THEN** HitStun 持续恰好 8 帧后回到 Idle
- **GIVEN** 攻击定义 HitStunFrames = 0（数据错误）, **WHEN** 角色进入 HitStun, **THEN** 强制最少 1 帧硬直
- **GIVEN** 角色在 HitStun（剩余 5 帧）, **WHEN** 再次被击中（力度 <= 9.0）, **THEN** HitStun 计时器重置为新攻击的 HitStunFrames
- **GIVEN** 角色在 HitStun, **WHEN** 再次被击中（力度 > 9.0）, **THEN** 进入 Knockback

### Knockback 公式验证
- **GIVEN** KnockbackMagnitude = 15.0, **THEN** KnockbackHitstunFrames = Floor(3 + 0.5 x 15) = 10 帧
- **GIVEN** KnockbackMagnitude = 100.0, **THEN** KnockbackHitstunFrames = Min(Floor(53), 50) = 50 帧（触及上限）

### Knockback 行为
- **GIVEN** 角色在 Knockback, **WHEN** KnockbackHitstunFrames 耗尽, **THEN** 回到 Idle（可操作）
- **GIVEN** 角色在 Knockback 可操作期, **WHEN** 再次被击中, **THEN** 正常判定 HitStun/Knockback，新击退速度覆盖当前
- **GIVEN** 角色在 Knockback 状态, **WHEN** 角色着地, **THEN** Knockback 结束，CombatState 回到 Idle

### 强制取消
- **GIVEN** 角色在 Attacking.Startup, **WHEN** 受击, **THEN** 攻击取消，hitbox 关闭，进入 HitStun 或 Knockback
- **GIVEN** 角色在 Attacking.Active, **WHEN** 命中对手, **THEN** 不打断自身攻击

### 3C 协调
- **GIVEN** 角色进入 HitStun, **THEN** 3C 移动被冻结
- **GIVEN** 角色进入 Knockback, **THEN** 格斗状态机调用 SetVelocity(击退向量)

## Implementation Notes (from ADR-0002)

- HitStun 可以打断任何其他战斗状态——唯一的硬编码优先级
- 状态优先级（同一帧多个触发时）: 受击 > 攻击推进 > 取消触发 > 自然结束 (TR-CBT-019)
- Knockback vs HitStun 判定使用严格大于（`>`），等于阈值时进入 HitStun
- KnockbackHitstunFrames 公式: `Floor(BaseKnockbackHitstun + KnockbackHitstunGrowth * KnockbackMagnitude)`，钳制到 Cap
- Knockback 时委托 3C 施加物理力（`SetVelocity()`），hitstun 期内冻结移动

## Out of Scope

- 投射物碰撞（attack-system epic）
- 伤害计算公式（damage-calculation epic）
- 击退向量计算（knockback-launch epic）
- 视觉/音频反馈

## Dependencies

- Story 001 (CombatFSM Core) must be DONE
- Story 002 (Attack Phase Progression) must be DONE
- `IMovementController.SetVelocity()` 接口可用
- KnockbackThreshold 校准值 9.0（来自 knockback-launch-system GDD）

## QA Test Cases

### Logic Tests (Given/When/Then)

**Test: KnockbackThreshold 判定**
- Given: KnockbackThreshold = 9.0
- When: OnHitReceived(knockbackMagnitude=2.0)
- Then: NextState = HitStun
- When: OnHitReceived(knockbackMagnitude=9.0)
- Then: NextState = HitStun（严格大于）
- When: OnHitReceived(knockbackMagnitude=25.0)
- Then: NextState = Knockback

**Test: KnockbackHitstunFrames 计算**
- Given: BaseKnockbackHitstun=3, KnockbackHitstunGrowth=0.5, KnockbackHitstunCap=50
- When: KnockbackMagnitude=15.0
- Then: KnockbackHitstunFrames = 10
- When: KnockbackMagnitude=100.0
- Then: KnockbackHitstunFrames = 50（触及上限）

**Test: HitStun 中再次被击中**
- Given: 角色在 HitStun（剩余 5 帧），新攻击 HitStunFrames=12
- When: OnHitReceived(knockbackMagnitude=2.0, hitStunFrames=12)
- Then: HitStun 计时器重置为 12 帧

**Test: HitStunFrames=0 强制最少 1 帧**
- Given: 攻击 HitStunFrames=0
- When: 进入 HitStun
- Then: 持续 1 帧后回到 Idle

**Test: 攻击中被击中强制取消**
- Given: 角色在 Attacking.Active（hitbox 已激活）
- When: OnHitReceived()
- Then: CombatState → HitStun/Knockback，OnCombatStateChanged 触发

**Test: Knockback 着地结束**
- Given: 角色在 Knockback 状态
- When: 收到着地事件
- Then: CombatState → Idle

## Test Evidence

- Automated unit tests: `tests/unit/combat/hitstun_knockback_test.cs`
- Test type: Logic (BLOCKING)

## Files to Create/Modify

- `Assets/Scripts/Core/CombatFSM.cs` (modify — add HitStun/Knockback state handling)
- `Assets/Scripts/Core/Formulas/CombatFormulas.cs` (modify — add KnockbackHitstunFrames formula)
