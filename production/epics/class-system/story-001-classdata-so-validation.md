# Story: ClassData SO Creation + Validation

> **Epic**: class-system
> **Status**: Ready
> **Layer**: Core
> **Type**: Logic
> **Estimate**: M (3-4 hours)
> **Control Manifest Version**: 2026-05-24

## Context

- **GDD**: `design/gdd/class-system.md`
- **TR Range**: TR-CLS-001
- **Governing ADR**: ADR-0004 (Skill System Data-Driven — ClassData SO structure)
- **Engine**: Unity 2022.3.51 LTS, LOW risk

## Summary

完善 ClassData ScriptableObject 的数据结构和编辑期验证逻辑。ClassData 已有基本骨架（ClassId, DisplayName, Movement, BaseAttacks, Visual, SkillPoolTags），需要添加 OnValidate() 数据完整性检查、IClassData 接口的完整实现、以及数据边界保护（移动属性钳制到安全范围、招式帧数据验证）。

## Acceptance Criteria (from GDD)

- **GIVEN** 职业配置数据已加载, **WHEN** 查询任意职业的移动属性, **THEN** 所有 5 个属性（MaxGroundSpeed, MoveAcceleration, JumpHeight, MaxAirSpeed, DashDistance）均有正值且在 3C 系统安全范围内
- **GIVEN** 职业配置数据已加载, **WHEN** 查询任意职业的招式数据, **THEN** 恰好有 3 个 AttackData（GroundAttack, AirAttack, DashAttack），每个的 Startup+Active+Recovery > 0
- **GIVEN** 职业配置数据中某移动属性为 0 或负数, **WHEN** 注入 3C 系统, **THEN** 该值被钳制到 3C 安全范围下限并记录警告

## Implementation Notes (from ADR-0004)

- ClassData 是 ScriptableObject，运行时只读
- `List<T>` 字段不在声明时初始化（让 Unity 序列化器处理）
- OnValidate() 检查: 移动属性 > 0，AttackData 帧总和 > 0，ClassId 非空
- 安全范围: MaxGroundSpeed [3.0, 8.0]，MoveAcceleration [30, 100]，JumpHeight [2.0, 5.0]，MaxAirSpeed [2.0, 6.0]，DashDistance [1.5, 4.0]
- IClassData 接口提供只读访问: GetMovementParams(), GetAttackData(AttackType), GetVisualData(), GetSkillPoolTags()
- AttackData 的 CancelTable 为空数组是合法设计

## Out of Scope

- MVP 职业实例创建（Story 002）
- 运行时注入逻辑（Story 003）
- 多人同职业支持（Story 004）
- UI 展示（Presentation 层）

## Dependencies

- Foundation epics must be DONE
- `ClassData` 基本结构已存在（`Assets/Scripts/Core/Data/ClassData.cs`）
- `IClassData` 接口已定义（`Assets/Scripts/Core/Interfaces/IClassData.cs`）
- `MovementParams` 已定义（`Assets/Scripts/Core/Data/MovementParams.cs`）
- `VisualData` 已定义（`Assets/Scripts/Foundation/Data/VisualData.cs`）
- `AttackData` 已定义（`Assets/Scripts/Core/Data/AttackData.cs`）

## QA Test Cases

### Logic Tests (Given/When/Then)

**Test: 有效 ClassData 验证通过**
- Given: ClassData SO 所有字段正确填充
- When: OnValidate() 执行
- Then: 无警告，数据完整性通过

**Test: 零值移动属性钳制**
- Given: ClassData.Movement.MaxGroundSpeed = 0
- When: GetMovementParams() 调用（运行时钳制）
- Then: 返回值 MaxGroundSpeed = 3.0（安全下限）

**Test: 负值移动属性钳制**
- Given: ClassData.Movement.JumpHeight = -1.0
- When: GetMovementParams() 调用
- Then: 返回值 JumpHeight = 2.0（安全下限）

**Test: 攻击数据完整性**
- Given: BaseAttacks 数组包含 3 个 AttackData，每个帧总和 > 0
- When: 查询招式数据
- Then: GetAttackData(GroundAttack), GetAttackData(AirAttack), GetAttackData(DashAttack) 均返回有效数据

**Test: 空攻击数据索引**
- Given: BaseAttacks 为空数组
- When: GetAttackData(GroundAttack)
- Then: 返回默认 AttackData（帧总和=0，调用方应忽略）

## Test Evidence

- Automated unit tests: `tests/unit/class/classdata_validation_test.cs`
- Test type: Logic (BLOCKING)

## Files to Create/Modify

- `Assets/Scripts/Core/Data/ClassData.cs` (modify — add OnValidate, safe range clamping, IClassData implementation)
