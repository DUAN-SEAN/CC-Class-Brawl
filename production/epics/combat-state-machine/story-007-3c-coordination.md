# Story: 3C Coordination

> **Epic**: combat-state-machine
> **Status**: Ready
> **Layer**: Core
> **Type**: Integration
> **Estimate**: M (3-4 hours)
> **Control Manifest Version**: 2026-05-24

## Context

- **GDD**: `design/gdd/combat-state-machine.md`
- **TR Range**: TR-CBT-003, TR-CBT-005
- **Governing ADR**: ADR-0002 (Dual FSM Architecture)
- **Engine**: Unity 2022.3.51 LTS, LOW risk

## Summary

实现格斗状态机与 3C 移动系统的协调交互：攻击/受击时冻结移动（FreezeMovement），Knockback 时委托 3C 施加击退速度（SetVelocity），查询 3C MovementState 和 IsGrounded() 确定攻击类型（地面/空中/冲刺）。集成测试验证双 FSM 的协调行为在各种状态组合下的正确性。

## Acceptance Criteria (from GDD)

- **GIVEN** 角色进入 Attacking, **THEN** 3C 移动被冻结（FreezeMovement(true)）
- **GIVEN** 角色攻击结束回到 Idle, **THEN** 3C 移动恢复（FreezeMovement(false)）
- **GIVEN** 角色进入 HitStun, **THEN** 3C 移动被冻结，角色播放受击动画
- **GIVEN** 角色进入 Knockback, **THEN** 格斗状态机调用 SetVelocity(击退向量) 委托 3C 施加物理力
- **GIVEN** 攻击输入被接受且 MovementState=Idle, **THEN** 使用 GroundAttack 数据 (TR-CBT-005)
- **GIVEN** 攻击输入被接受且 MovementState=Jumping, **THEN** 使用 AirAttack 数据
- **GIVEN** 攻击输入被接受且 MovementState=Dashing, **THEN** 使用 DashAttack 数据

## Implementation Notes (from ADR-0002)

- CombatFSM 通过 `IMovementController` 接口控制 3C，不直接引用具体类
- 解冻逻辑在 CombatFSM FixedUpdate 末尾统一处理——从非 Idle 转入 Idle 时调用 `FreezeMovement(false)`
- 攻击类型解析: 查询 `IMovementController.GetState()` 和 `IsGrounded()`
- Knockback 时调用 `SetVelocity(knockbackVector)`，hitstun 期内同时冻结移动
- 执行顺序: 3C FixedUpdate 先于 CombatFSM（CharacterController 协调器保证）

## Out of Scope

- 3C 系统内部实现（Foundation 层 3c-system epic）
- 击退向量计算（knockback-launch epic）
- 视觉/动画反馈

## Dependencies

- Story 001 (CombatFSM Core) must be DONE
- Story 003 (HitStun + Knockback Entry) must be DONE
- `IMovementController` 接口可用（`Assets/Scripts/Foundation/Interfaces/IMovementController.cs`）
- 3C 系统基础实现（MovementController）可运行

## QA Test Cases

### Integration Tests (Given/When/Then)

**Test: 攻击冻结 3C**
- Given: 角色 Idle + 3C 未冻结
- When: 进入 Attacking
- Then: IMovementController.FreezeMovement(true) 被调用

**Test: 攻击结束解冻 3C**
- Given: 角色在 Attacking.Recovery
- When: 攻击自然结束 → Idle
- Then: IMovementController.FreezeMovement(false) 被调用

**Test: HitStun 冻结 3C**
- Given: 角色在 Idle
- When: 进入 HitStun
- Then: FreezeMovement(true) 被调用
- When: HitStun 结束 → Idle
- Then: FreezeMovement(false) 被调用

**Test: Knockback 调用 SetVelocity**
- Given: 角色在 Idle，击退向量 = (5.0, 3.0)
- When: 进入 Knockback
- Then: SetVelocity((5.0, 3.0)) 被调用

**Test: 攻击类型根据 MovementState 解析**
- Given: IMovementController.GetState() = Idle
- When: 攻击输入被接受
- Then: 使用 GroundAttack AttackData
- Given: GetState() = Jumping
- When: 攻击输入被接受
- Then: 使用 AirAttack AttackData
- Given: GetState() = Dashing
- When: 攻击输入被接受
- Then: 使用 DashAttack AttackData

## Test Evidence

- Automated integration tests: `tests/integration/combat/3c_coordination_test.cs`
- Test type: Integration (BLOCKING)

## Files to Create/Modify

- `Assets/Scripts/Core/CombatFSM.cs` (modify — add 3C coordination calls)
- `Assets/Scripts/Core/CharacterController.cs` (new — coordinator that calls Movement → Combat → Attack in order)
