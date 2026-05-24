# Story: Cancel Table

> **Epic**: combat-state-machine
> **Status**: Ready
> **Layer**: Core
> **Type**: Logic
> **Estimate**: M (3-4 hours)
> **Control Manifest Version**: 2026-05-24

## Context

- **GDD**: `design/gdd/combat-state-machine.md`
- **TR Range**: TR-CBT-009, TR-CBT-010, TR-CBT-017, TR-CBT-030, TR-CBT-031
- **Governing ADR**: ADR-0002 (Dual FSM Architecture)
- **Engine**: Unity 2022.3.51 LTS, LOW risk

## Summary

实现取消规则表（CancelTable）逻辑：在 Recovery 阶段的取消窗口内，根据攻击数据中的 CancelEntry 检查缓冲中的输入是否匹配取消条件。支持数据驱动的取消配置，包括目标状态类型、输入要求和帧窗口验证。

## Acceptance Criteria (from GDD)

- **GIVEN** 角色在 Attacking.Recovery, **WHEN** 取消窗口内且取消表允许新攻击, **THEN** 取消到新攻击的 Startup
- **GIVEN** Jab (4/3/8) 且 CancelWindowFrames=5, **WHEN** CurrentAttackFrame = 6, **THEN** 取消窗口未打开
- **GIVEN** Jab (4/3/8) 且 CancelWindowFrames=5, **WHEN** CurrentAttackFrame = 7, **THEN** 取消窗口打开
- **GIVEN** Jab (4/3/8) 且 CancelWindowFrames=5, **WHEN** CurrentAttackFrame = 11, **THEN** 取消窗口最后一个可取消帧
- **GIVEN** Jab (4/3/8) 且 CancelWindowFrames=5, **WHEN** CurrentAttackFrame = 12, **THEN** 取消窗口已关闭
- **GIVEN** CancelWindowFrames > RecoveryFrames, **WHEN** 计算取消窗口, **THEN** 钳制为 RecoveryFrames
- **GIVEN** 取消目标条件不满足（如尝试空中攻击但已在地面）, **WHEN** 检查取消表, **THEN** 取消被拒绝，输入保留在缓冲中

## Implementation Notes (from ADR-0002)

- CancelEntry 包含: TargetState (string), InputRequired (string), RequiredPhase (AttackPhase)
- 取消窗口: CancelStartFrame = StartupFrames + ActiveFrames, CancelEndFrame = CancelStartFrame + CancelWindowFrames - 1
- 取消优先级（取消表内多个条件同时满足时）: 技能攻击 > 基础攻击 > 闪避/跳跃
- MVP 默认取消规则: Recovery 可取消到任意攻击或 Dash
- 取消检查在每帧 FixedUpdateState() 中执行，仅在 Recovery 阶段 + 取消窗口内 + 缓冲有匹配输入时触发

## Out of Scope

- 投射物取消（attack-system 层面）
- 条件取消的复杂表达式解析（MVP 使用简单的 3C 状态查询）
- 技能系统注入的条件取消

## Dependencies

- Story 001 (CombatFSM Core) must be DONE
- Story 002 (Attack Phase Progression) must be DONE
- Story 004 (Input Buffer) must be DONE
- `CancelEntry` 结构体已定义（`Assets/Scripts/Core/Data/CancelEntry.cs`）
- `IMovementController.GetState()` 用于条件检查

## QA Test Cases

### Logic Tests (Given/When/Then)

**Test: 取消窗口边界**
- Given: Startup=4, Active=3, Recovery=8, CancelWindowFrames=5
- When: Frame=6
- Then: IsCancelWindowActive=false
- When: Frame=7
- Then: IsCancelWindowActive=true
- When: Frame=11
- Then: IsCancelWindowActive=true（最后一帧）
- When: Frame=12
- Then: IsCancelWindowActive=false

**Test: 取消到新攻击**
- Given: Recovery 阶段，取消窗口打开，CancelTable 包含 {TargetState="Attack", InputRequired="Attack"}
- When: 缓冲中有 Attack 输入
- Then: 执行取消，CombatState 保持 Attacking，AttackPhase 重置为 Startup

**Test: 取消到 Dash**
- Given: CancelTable 包含 {TargetState="Dash", InputRequired="Dash"}
- When: 缓冲中有 Dash 输入
- Then: CombatState → Idle，3C 接管 Dash

**Test: 条件不满足时保留**
- Given: CancelEntry 条件="AirOnly"，当前在地面
- When: 检查取消
- Then: 取消被拒绝，输入保留在缓冲

**Test: CancelWindowFrames 超过 RecoveryFrames**
- Given: RecoveryFrames=8, CancelWindowFrames=15
- When: 计算取消窗口
- Then: 有效 CancelWindowFrames = 8

## Test Evidence

- Automated unit tests: `tests/unit/combat/cancel_table_test.cs`
- Test type: Logic (BLOCKING)

## Files to Create/Modify

- `Assets/Scripts/Core/CombatFSM.cs` (modify — add cancel evaluation logic)
- `Assets/Scripts/Core/Formulas/CombatFormulas.cs` (modify — add cancel window formula)
