# Story: Runtime Injection

> **Epic**: class-system
> **Status**: Ready
> **Layer**: Core
> **Type**: Integration
> **Estimate**: M (3-4 hours)
> **Control Manifest Version**: 2026-05-24

## Context

- **GDD**: `design/gdd/class-system.md`
- **TR Range**: TR-CLS-011
- **Governing ADR**: ADR-0004 (Skill System Data-Driven)
- **Engine**: Unity 2022.3.51 LTS, LOW risk

## Summary

实现职业数据运行时注入：在角色实例创建时，从 ClassData SO 读取移动属性注入 3C 系统、读取攻击数据注册到攻击系统/格斗状态机。注入是一次性操作（对局开始时），此后整局固定。对局结束时角色实例销毁，职业数据不残留。

## Acceptance Criteria (from GDD)

- **GIVEN** 玩家选择了盗贼, **WHEN** 角色实例创建, **THEN** 3C 系统使用盗贼的移动属性值（MaxGroundSpeed=6.5, MoveAcceleration=75.0 等）而非默认值
- **GIVEN** 玩家选择了战士, **WHEN** 角色实例创建, **THEN** 攻击系统注册战士的 3 个基础招式 AttackData
- **GIVEN** 对局结束, **WHEN** 角色实例销毁, **THEN** 职业数据不残留——下一局重新选择职业时使用新数据
- **GIVEN** 对局进行中，战士已获得 2 个随机技能, **WHEN** 查询战士的基础移动属性, **THEN** MaxGroundSpeed 等属性值与注入时完全一致（随机技能不修改职业基础属性）
- **GIVEN** 3 个职业配置数据已加载, **THEN** 查询任意职业数据的耗时 < 0.01ms

## Implementation Notes (from ADR-0004)

- 注入由 CharacterController 协调器在角色初始化时调用
- 注入流程: ClassData.Movement → IMovementController 覆盖默认值; ClassData.BaseAttacks → CombatFSM.RegisterState() 注册基础攻击
- 职业数据运行时只读，注入后不可修改
- SkillPoolTags MVP 为空（不限制技能池）
- VisualData 注入到角色视觉系统（Presentation 层，MVP 可暂不实现）

## Out of Scope

- 角色选择 UI（Presentation 层）
- 技能装备注入（Feature 层 skill-equipment）
- 视觉系统注入

## Dependencies

- Story 001 (ClassData SO Creation + Validation) must be DONE
- Story 002 (MVP Class Data Instances) must be DONE
- combat-state-machine Story 001 (CombatFSM Core) must be DONE — 需要 RegisterState()
- `IMovementController` 接口可用（属性覆盖方法）

## QA Test Cases

### Integration Tests (Given/When/Then)

**Test: 盗贼移动属性注入**
- Given: RogueClassData.asset 和 MockMovementController
- When: 执行注入
- Then: MockMovementController.MaxGroundSpeed = 6.5, JumpHeight = 4.2

**Test: 战士攻击数据注册**
- Given: WarriorClassData.asset 和 MockCombatFSM
- When: 执行注入
- Then: 3 个 StateDefinition 注册到 CombatFSM（GroundAttack, AirAttack, DashAttack）

**Test: 对局结束清理**
- Given: 角色已注入盗贼数据
- When: CleanupCharacter() 调用
- Then: 3C 系统恢复默认值，CombatFSM 清除所有已注册状态

**Test: 职业数据不变性**
- Given: 角色已注入战士数据，后装备 2 个技能
- When: 查询基础移动属性
- Then: MaxGroundSpeed = 3.8（与注入时一致）

**Test: 性能预算**
- Given: 3 个 ClassData SO 加载
- When: 查询 1000 次 GetMovementParams()
- Then: 平均耗时 < 0.01ms（纯内存读取）

## Test Evidence

- Automated integration tests: `tests/integration/class/runtime_injection_test.cs`
- Test type: Integration (BLOCKING)

## Files to Create/Modify

- `Assets/Scripts/Core/ClassInjector.cs` (new — handles ClassData → 3C/Attack injection)
