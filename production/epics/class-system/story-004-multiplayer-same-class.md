# Story: Multi-Player Same-Class Support

> **Epic**: class-system
> **Status**: Ready
> **Layer**: Core
> **Type**: Integration
> **Estimate**: S (2-3 hours)
> **Control Manifest Version**: 2026-05-24

## Context

- **GDD**: `design/gdd/class-system.md`
- **TR Range**: TR-CLS-017
- **Governing ADR**: ADR-0004 (Skill System Data-Driven)
- **Engine**: Unity 2022.3.51 LTS, LOW risk

## Summary

支持两个玩家选择相同职业（"镜像战"）。相同职业的两个角色实例独立注入数据（各自独立的运行时状态），视觉上通过 P1/P2 标识色叠加在职业色之上进行区分。确保 ClassData SO 共享引用不导致运行时状态互相污染。

## Acceptance Criteria (from GDD)

- **GIVEN** P1 选择战士, P2 也选择战士, **WHEN** 对局开始, **THEN** 两个角色具有相同的移动属性和招式数据，但视觉上有 P1/P2 标识色区分
- **GIVEN** P1 和 P2 都选择盗贼, **WHEN** 各自注入职业数据, **THEN** 两者的运行时状态完全独立（一个角色被击中不影响另一个的属性）

## Implementation Notes (from ADR-0004)

- ClassData SO 是共享引用（只读），两个角色读取同一个 SO 资产
- 运行时状态由各自的系统管理器持有，与 SO 完全分离
- P1/P2 区分通过 PlayerIndex（来自 PlayerInput.playerIndex）实现
- 视觉区分: PrimaryColor 基础上叠加 P1 标识色（蓝色）/ P2 标识色（红色）
- SilhouetteScale 对两个相同职业角色相同，hurtbox 大小一致

## Out of Scope

- 角色选择 UI 实现
- P1/P2 标识色具体渲染（Presentation 层）
- 4 人模式扩展

## Dependencies

- Story 003 (Runtime Injection) must be DONE
- combat-state-machine Story 006 (Dynamic Skill State Registration) 必须支持多角色独立注册

## QA Test Cases

### Integration Tests (Given/When/Then)

**Test: 镜像战数据一致**
- Given: P1 和 P2 都选择 WarriorClassData
- When: 两角色各自注入
- Then: P1.MaxGroundSpeed = P2.MaxGroundSpeed = 3.8, 攻击数据完全相同

**Test: 运行时状态独立**
- Given: P1 和 P2 都选择 WarriorClassData，P1 被击中 DamagePercent=50
- When: 查询 P2.DamagePercent
- Then: P2.DamagePercent = 0.0（不受 P1 影响）

**Test: 视觉标识区分**
- Given: P1 和 P2 都选择 RogueClassData
- When: 查询角色标识
- Then: P1.PlayerIndex=0, P2.PlayerIndex=1，标识色不同

**Test: 独立技能状态注册**
- Given: P1 和 P2 都选择 WarriorClassData
- When: P1 注册技能 "fireball"，P2 不注册
- Then: P1 的 CombatFSM 包含 "fireball"，P2 的不包含

## Test Evidence

- Automated integration tests: `tests/integration/class/mirror_match_test.cs`
- Test type: Integration (BLOCKING)

## Files to Create/Modify

- `Assets/Scripts/Core/ClassInjector.cs` (modify — ensure per-player independent injection)
