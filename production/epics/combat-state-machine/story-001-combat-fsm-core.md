# Story: CombatFSM Core

> **Epic**: combat-state-machine
> **Status**: Ready
> **Layer**: Core
> **Type**: Logic
> **Estimate**: M (3-4 hours)
> **Control Manifest Version**: 2026-05-24

## Context

- **GDD**: `design/gdd/combat-state-machine.md`
- **TR Range**: TR-CBT-001, TR-CBT-002, TR-CBT-003, TR-CBT-007, TR-CBT-018, TR-CBT-035, TR-CBT-036, TR-CBT-043
- **Governing ADR**: ADR-0002 (Dual FSM Architecture)
- **Engine**: Unity 2022.3.51 LTS, LOW risk

## Summary

实现 CombatFSM 的核心骨架：CombatState 枚举状态流转、AttackPhase 三阶段帧计数器、状态转换事件、以及 ICombatStateProvider 接口的实现。这是格斗状态机的基座，后续故事在此基础上添加输入缓冲、取消规则和状态扩展。

## Acceptance Criteria (from GDD)

- **GIVEN** CombatState 枚举包含 {Idle, Attacking, HitStun, Knockback}, **WHEN** FSM 初始化, **THEN** 当前状态为 Idle，AttackPhase 为 None
- **GIVEN** 角色在 CombatIdle, **WHEN** 攻击输入被接受（本故事中直接调用方法模拟）, **THEN** 进入 Attacking.Startup
- **GIVEN** 攻击帧数据 Startup=4/Active=3/Recovery=8, **WHEN** CurrentAttackFrame = 3, **THEN** 阶段为 Startup (TR-CBT-018)
- **GIVEN** 攻击帧数据 Startup=4/Active=3/Recovery=8, **WHEN** CurrentAttackFrame = 4, **THEN** 阶段推进为 Active
- **GIVEN** 攻击帧数据 Startup=4/Active=3/Recovery=8, **WHEN** CurrentAttackFrame = 7, **THEN** 阶段推进为 Recovery
- **GIVEN** 攻击帧数据 Startup=4/Active=3/Recovery=8, **WHEN** CurrentAttackFrame = 15, **THEN** 攻击结束，回到 Idle
- **GIVEN** CombatFSM 更新中, **THEN** OnCombatStateChanged 和 OnAttackPhaseChanged 事件在状态/阶段变化时触发 (TR-CBT-036)
- **GIVEN** 攻击帧数据 Startup=0/Active=0/Recovery=0, **WHEN** 尝试开始攻击, **THEN** 该输入被忽略 (TR-CBT-027)
- **GIVEN** 2 人对战进行中, **THEN** 格斗状态机帧耗时 < 0.5ms (TR-CBT-037)

## Implementation Notes (from ADR-0002)

- CombatFSM 是独立 MonoBehaviour，不依赖 Script Execution Order
- 通过 CharacterController 协调器显式调用 `FixedUpdateState()`
- CombatState 用 enum，攻击子阶段由当前 AttackData 帧数据驱动（不是独立状态）
- StateDefinition 存储在 `Dictionary<string, StateDefinition>` 中
- 使用 `IMovementController` 接口协调 3C：Attacking/HitStun 时 `FreezeMovement(true)`，转回 Idle 时 `FreezeMovement(false)`
- 解冻逻辑在 FixedUpdate 末尾统一处理，不依赖每个转换路径

## Out of Scope

- 输入缓冲（Story 004）
- 取消表逻辑（Story 005）
- HitStun/Knockback 状态进入（Story 003）
- 动态技能状态注册（Story 006）
- 视觉/音频反馈（TR-CBT-040, TR-CBT-041, TR-CBT-042）

## Dependencies

- Foundation epics must be DONE (3c-system, arena-platform, game-state-management)
- `IMovementController` 接口必须已定义（Foundation: `Assets/Scripts/Foundation/Interfaces/IMovementController.cs`）
- `CombatState` 枚举已存在（`Assets/Scripts/Core/Enums/CombatState.cs`）
- `AttackPhase` 枚举已存在（`Assets/Scripts/Core/Enums/AttackPhase.cs`）
- `StateDefinition` 已定义（`Assets/Scripts/Core/Data/StateDefinition.cs`）
- `ICombatStateProvider` 接口已定义（`Assets/Scripts/Core/Interfaces/ICombatStateProvider.cs`）

## QA Test Cases

### Logic Tests (Given/When/Then)

**Test: FSM 初始化为 Idle**
- Given: CombatFSM 实例创建
- When: 初始化完成
- Then: GetCurrentState() == Idle, GetCurrentAttackPhase() == None

**Test: 进入攻击状态**
- Given: FSM 在 Idle 状态
- When: 调用 StartAttack(validStateDefinition)
- Then: GetCurrentState() == Attacking, GetCurrentAttackPhase() == Startup, OnCombatStateChanged 触发 (Idle → Attacking)

**Test: 三阶段自动推进**
- Given: 攻击帧数据 Startup=4/Active=3/Recovery=8
- When: FixedUpdateState() 调用 4 次
- Then: AttackPhase 推进到 Active
- When: 再调用 3 次
- Then: AttackPhase 推进到 Recovery
- When: 再调用 8 次
- Then: 回到 Idle, OnCombatStateChanged 触发 (Attacking → Idle)

**Test: 零帧攻击被拒绝**
- Given: StateDefinition 的 StartupFrames=0, ActiveFrames=0, RecoveryFrames=0
- When: 尝试 StartAttack(stateDef)
- Then: 状态保持 Idle，攻击未开始

**Test: 性能预算**
- Given: 2 个 CombatFSM 实例
- When: 每帧调用 FixedUpdateState() 1000 次
- Then: 平均每帧耗时 < 0.5ms

## Test Evidence

- Automated unit tests: `tests/unit/combat/combat_fsm_core_test.cs`
- Test type: Logic (BLOCKING)

## Files to Create/Modify

- `Assets/Scripts/Core/CombatFSM.cs` (new — MonoBehaviour implementing ICombatStateProvider)
