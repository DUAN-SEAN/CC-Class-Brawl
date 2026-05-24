# Story: Attack Phase Progression

> **Epic**: combat-state-machine
> **Status**: Ready
> **Layer**: Core
> **Type**: Logic
> **Estimate**: S (2-3 hours)
> **Control Manifest Version**: 2026-05-24

## Context

- **GDD**: `design/gdd/combat-state-machine.md`
- **TR Range**: TR-CBT-007, TR-CBT-012, TR-CBT-018, TR-CBT-043
- **Governing ADR**: ADR-0002 (Dual FSM Architecture)
- **Engine**: Unity 2022.3.51 LTS, LOW risk

## Summary

实现攻击三阶段帧计数器的精确推进逻辑：Startup → Active → Recovery → Idle，包括帧 0 开始计数、阶段边界判断、自然结束回到 Idle。确保 AttackPhase 事件在每个阶段转换时正确触发。本故事专注于帧计数公式和阶段推进的数学正确性。

## Acceptance Criteria (from GDD)

- **GIVEN** 攻击帧数据 Startup=4/Active=3/Recovery=8 (总帧 15), **WHEN** CurrentAttackFrame 从 0 到 3, **THEN** Phase = Startup
- **GIVEN** 同上, **WHEN** CurrentAttackFrame = 4 到 6, **THEN** Phase = Active
- **GIVEN** 同上, **WHEN** CurrentAttackFrame = 7 到 14, **THEN** Phase = Recovery
- **GIVEN** 同上, **WHEN** CurrentAttackFrame = 15, **THEN** 攻击结束回到 Idle
- **GIVEN** Jab (4/3/8, CancelWindowFrames=5), **WHEN** CurrentAttackFrame = 7, **THEN** 取消窗口打开（CancelStartFrame = 7）(TR-CBT-017)
- **GIVEN** Jab (4/3/8, CancelWindowFrames=5), **WHEN** CurrentAttackFrame = 11, **THEN** 取消窗口最后一个可取消帧
- **GIVEN** Jab (4/3/8, CancelWindowFrames=5), **WHEN** CurrentAttackFrame = 12, **THEN** 取消窗口已关闭

## Implementation Notes (from ADR-0002)

- 攻击阶段由 AttackData 帧数据驱动，不是独立状态
- Attacking 内部阶段通过 `CurrentAttackFrame` 与 StartupFrames/ActiveFrames/RecoveryFrames 的累加比较判定
- 帧计数从 0 开始（帧 0 = Startup 第 1 帧）
- 公式: `if frame < Startup → Startup; elif frame < Startup+Active → Active; elif frame < Startup+Active+Recovery → Recovery; else → End`
- CancelStartFrame = StartupFrames + ActiveFrames（取消窗口在 Recovery 开始时打开）

## Out of Scope

- HitStun/Knockback 中断攻击（Story 003）
- 取消表执行逻辑（Story 005）
- 输入缓冲（Story 004）
- 动态状态注册（Story 006）

## Dependencies

- Story 001 (CombatFSM Core) must be DONE
- `AttackData` 结构体已定义（`Assets/Scripts/Core/Data/AttackData.cs`）
- `CancelEntry` 结构体已定义（`Assets/Scripts/Core/Data/CancelEntry.cs`）

## QA Test Cases

### Logic Tests (Given/When/Then)

**Test: Jab 三阶段推进**
- Given: AttackData Startup=4, Active=3, Recovery=8
- When: FixedUpdateState() 调用逐帧推进
- Then: 帧 0-3 → Startup, 帧 4-6 → Active, 帧 7-14 → Recovery, 帧 15 → Idle

**Test: OnAttackPhaseChanged 事件**
- Given: 攻击正在进行
- When: 阶段从 Startup 推进到 Active
- Then: OnAttackPhaseChanged(AttackPhase.Active) 事件触发一次

**Test: 取消窗口边界**
- Given: Startup=4, Active=3, CancelWindowFrames=5
- When: CurrentAttackFrame = 6
- Then: 取消窗口未打开（CancelStartFrame=7）
- When: CurrentAttackFrame = 7
- Then: 取消窗口打开
- When: CurrentAttackFrame = 11
- Then: 取消窗口最后一个可取消帧
- When: CurrentAttackFrame = 12
- Then: 取消窗口关闭

**Test: 取消窗口超过 Recovery**
- Given: RecoveryFrames=8, CancelWindowFrames=10
- When: 计算取消窗口
- Then: CancelWindowFrames 钳制为 8（等于 RecoveryFrames）

## Test Evidence

- Automated unit tests: `tests/unit/combat/attack_phase_progression_test.cs`
- Test type: Logic (BLOCKING)

## Files to Create/Modify

- `Assets/Scripts/Core/CombatFSM.cs` (modify — add phase progression logic)
- `Assets/Scripts/Core/Formulas/CombatFormulas.cs` (new — pure static class for phase/cancel window formulas)
