# Story: Dynamic Skill State Registration

> **Epic**: combat-state-machine
> **Status**: Ready
> **Layer**: Core
> **Type**: Logic
> **Estimate**: S (2-3 hours)
> **Control Manifest Version**: 2026-05-24

## Context

- **GDD**: `design/gdd/combat-state-machine.md`
- **TR Range**: TR-CBT-011, TR-CBT-032, TR-CBT-033, TR-CBT-034
- **Governing ADR**: ADR-0002 (Dual FSM Architecture), ADR-0004 (Skill System Data-Driven)
- **Engine**: Unity 2022.3.51 LTS, LOW risk

## Summary

实现通过 `ICombatStateProvider.RegisterState(StateDefinition)` 动态注册新技能战斗状态，以及 `DeregisterAllSkillStates()` 清除所有技能状态。支持技能系统在运行时注入新的攻击定义而不修改核心 FSM 代码。包括重复名称处理（先注册优先）、缺少帧数据拒绝注册、以及技能状态被打断时的回调通知。

## Acceptance Criteria (from GDD)

- **GIVEN** 技能系统注册一个新状态（含完整帧数据）, **WHEN** 注册成功且输入触发, **THEN** 新状态正常执行（Startup → Active → Recovery）
- **GIVEN** 技能系统注册新状态（未提供帧数据，Startup=0/Active=0/Recovery=0）, **WHEN** 注册调用, **THEN** 注册被拒绝
- **GIVEN** 技能系统注册新状态（名称重复）, **WHEN** 注册调用, **THEN** 后注册的被忽略，日志中记录警告
- **GIVEN** 技能系统注册的新状态在执行中被 HitStun 打断, **THEN** hitbox 关闭，状态重置，技能系统收到"被打断"回调

## Implementation Notes (from ADR-0002, ADR-0004)

- 使用 `Dictionary<string, StateDefinition>` 存储已注册状态
- 基础攻击（GroundAttack/AirAttack/DashAttack）在初始化时注册，技能状态动态添加
- `DeregisterAllSkillStates()` 清除技能状态但保留基础攻击
- StateDefinition 是 readonly struct，栈分配零 GC
- 技能被打断时触发回调事件 `OnSkillInterrupted(string stateId)` 供技能系统监听
- 验证规则: StartupFrames + ActiveFrames + RecoveryFrames > 0

## Out of Scope

- 技能抽取系统（Feature 层 skill-draw）
- 技能装备管理（Feature 层 skill-equipment）
- 技能视觉/音效反馈

## Dependencies

- Story 001 (CombatFSM Core) must be DONE
- `StateDefinition` 已定义（`Assets/Scripts/Core/Data/StateDefinition.cs`）
- `ICombatStateProvider.RegisterState()` 接口已定义

## QA Test Cases

### Logic Tests (Given/When/Then)

**Test: 注册有效状态**
- Given: StateDefinition {StateName="fireball", Startup=10, Active=4, Recovery=12}
- When: RegisterState(stateDef)
- Then: 字典包含 "fireball" 条目，注册成功返回 true

**Test: 注册无效状态（零帧）**
- Given: StateDefinition {StateName="invalid", Startup=0, Active=0, Recovery=0}
- When: RegisterState(stateDef)
- Then: 注册被拒绝，返回 false

**Test: 重复名称注册**
- Given: "fireball" 已注册
- When: RegisterState(new StateDefinition {StateName="fireball"})
- Then: 第二次注册被忽略，日志记录警告，原始状态不变

**Test: DeregisterAllSkillStates**
- Given: 基础攻击 + 2 个技能状态已注册
- When: DeregisterAllSkillStates()
- Then: 技能状态被清除，基础攻击保留

**Test: 技能状态执行**
- Given: "fireball" 已注册
- When: 输入触发 fireball 状态
- Then: 攻击正常执行 Startup → Active → Recovery → Idle

**Test: 技能状态被打断**
- Given: "fireball" 正在执行（Active 阶段）
- When: OnHitReceived() → HitStun
- Then: OnSkillInterrupted("fireball") 事件触发

## Test Evidence

- Automated unit tests: `tests/unit/combat/skill_state_registration_test.cs`
- Test type: Logic (BLOCKING)

## Files to Create/Modify

- `Assets/Scripts/Core/CombatFSM.cs` (modify — add RegisterState/DeregisterAllSkillStates + skill interrupted callback)
