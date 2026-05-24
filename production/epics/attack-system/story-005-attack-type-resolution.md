# Story 005: Attack Type Resolution — MovementState to GroundAttack/AirAttack/DashAttack

> **Epic**: attack-system
> **Status**: Ready
> **Layer**: Core
> **Type**: Logic
> **Estimate**: S
> **Manifest Version**: 2026-05-24
> **Last Updated**: 2026-05-24

## Context

**GDD**: `design/gdd/attack-system.md`
**Requirement**: TR-ATK-004, TR-ATK-017, TR-ATK-018, TR-ATK-019
**ADR Governing Implementation**: ADR-0002: Dual FSM Architecture, ADR-0004: Skill System Data-Driven
**ADR Decision Summary**: 攻击类型由 3C MovementState 决定: Idle/Running→GroundAttack, Jumping/Falling/FastFalling→AirAttack, Dashing→DashAttack。优先级: 技能招式 > 基础招式。如果技能系统激活了覆盖当前攻击类型的技能, 使用技能的 AttackData。攻击系统消费 AttackData 时不区分来源。
**Engine**: Unity 2022.3.51 LTS | **Risk**: LOW

**Control Manifest Rules**:
- Required: MovementState enum 包含 Idle, Running, Jumping, Falling, FastFalling, Dashing
- Required: 攻击系统消费 AttackData 时不区分来源 (ADR-0004)
- Required: IMovementController.GetState() 提供当前 MovementState
- Required: 技能招式优先于基础招式

---

## Acceptance Criteria

- [ ] MovementState=Idle → 使用 GroundAttack AttackData
- [ ] MovementState=Running → 使用 GroundAttack AttackData
- [ ] MovementState=Jumping → 使用 AirAttack AttackData
- [ ] MovementState=Falling → 使用 AirAttack AttackData
- [ ] MovementState=FastFalling → 使用 AirAttack AttackData
- [ ] MovementState=Dashing → 使用 DashAttack AttackData
- [ ] 技能招式优先: 如果技能系统激活了覆盖当前攻击类型的技能, 使用技能 AttackData
- [ ] 无技能覆盖时使用职业基础招式 (来自 ClassData)
- [ ] 统一 AttackData 格式 — 基础和技能走完全相同的代码路径

---

## Implementation Notes

**来自 ADR-0002/ADR-0004 的具体指导**:

1. 攻击类型解析逻辑:
```csharp
AttackData ResolveAttackData(MovementState moveState, IAttackDataProvider skillProvider, ClassData classData)
{
    AttackType type = moveState switch
    {
        MovementState.Idle or MovementState.Running => AttackType.GroundAttack,
        MovementState.Jumping or MovementState.Falling or MovementState.FastFalling => AttackType.AirAttack,
        MovementState.Dashing => AttackType.DashAttack,
        _ => AttackType.GroundAttack
    };

    // 优先级: 技能 > 基础
    if (skillProvider.TryGetAttackData(type, out var skillAttack))
        return skillAttack;

    return classData.GetAttackData(type);
}
```

2. MovementState 来自 3C: IMovementController.GetState()
3. ClassData 提供 3 个基础招式: GroundAttack, AirAttack, DashAttack
4. IAttackDataProvider 接口 (来自技能装备管理) 提供技能招式

5. AttackType 枚举: GroundAttack, AirAttack, DashAttack (已定义)

---

## Out of Scope

- 攻击生命周期 (Story 001)
- Hitbox 定位 (Story 002)
- 多次命中防护 (Story 003)
- Hitstop (Story 004)
- 投射物系统 (Story 006-007)
- 技能装备管理的具体实现 (skill-equipment epic)

---

## QA Test Cases

- **AC-1 (Idle→GroundAttack)**:
  - Given: MovementState=Idle
  - When: 攻击输入被接受
  - Then: 使用 GroundAttack 数据

- **AC-2 (Running→GroundAttack)**:
  - Given: MovementState=Running
  - When: 攻击输入被接受
  - Then: 使用 GroundAttack 数据

- **AC-3 (Jumping→AirAttack)**:
  - Given: MovementState=Jumping
  - When: 攻击输入被接受
  - Then: 使用 AirAttack 数据

- **AC-5 (Falling→AirAttack)**:
  - Given: MovementState=Falling
  - When: 攻击输入被接受
  - Then: 使用 AirAttack 数据

- **AC-6 (Dashing→DashAttack)**:
  - Given: MovementState=Dashing
  - When: 攻击输入被接受
  - Then: 使用 DashAttack 数据

- **AC-7 (技能优先)**:
  - Given: MovementState=Idle, 技能系统激活了 GroundAttack 覆盖
  - When: 攻击输入被接受
  - Then: 使用技能 AttackData (非职业基础)

- **AC-9 (统一处理)**:
  - Given: 基础招式和技能招式各有不同 AttackData
  - When: 分别执行攻击
  - Then: 代码路径完全一致, 行为统一

---

## Test Evidence

**Story Type**: Logic
**Required evidence**: `tests/unit/attack/attack-type-resolution_test.cs`
**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: Story 001 (Attack Lifecycle), 3c-system epic (MovementState 定义), class-system epic (ClassData 定义)
- Unlocks: Story 006 (Projectile — 需要知道攻击类型才能决定是否是投射物)
