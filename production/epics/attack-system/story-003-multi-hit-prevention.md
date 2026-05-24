# Story 003: Multi-Hit Prevention — HitTargets HashSet, Same-Attack Dedup, Projectile Dedup

> **Epic**: attack-system
> **Status**: Ready
> **Layer**: Core
> **Type**: Logic
> **Estimate**: S
> **Manifest Version**: 2026-05-24
> **Last Updated**: 2026-05-24

## Context

**GDD**: `design/gdd/attack-system.md`
**Requirement**: TR-ATK-009, TR-ATK-010, TR-ATK-011, TR-ATK-012
**ADR Governing Implementation**: ADR-0003: Hitbox/Hurtbox Detection, ADR-0013: Projectile System
**ADR Decision Summary**: 同一攻击 (同一 AttackId) 对同一目标只能命中一次。攻击系统维护 HitTargets HashSet<int> (每攻击实例)。命中时加入集合, 碰撞系统查询集合过滤已命中目标。OnTriggerEnter2D 仅在重叠开始时触发一次, HitTargets 集合提供额外防护层。
**Engine**: Unity 2022.3.51 LTS | **Risk**: LOW

**Control Manifest Rules**:
- Required: 命中检测管线包含多次命中检查步骤
- Required: HitTargets 集合查询由碰撞系统调用攻击系统接口
- Guardrail: OnTriggerEnter2D 回调 < 0.1ms

---

## Acceptance Criteria

- [ ] 每个攻击实例维护一个 HitTargets HashSet<int> 集合
- [ ] 命中时将目标 ID 加入集合, 触发 HitEvent
- [ ] 碰撞系统检测到重叠时查询 HitTargets, 已在集合中的目标不触发 HitEvent
- [ ] 近战攻击: 同一 Active 阶段内同一目标只命中一次
- [ ] 投射物: 飞行过程中同一目标只命中一次 (即使 hitbox 持续存在)
- [ ] 攻击结束/被取消时 HitTargets 集合清理
- [ ] 同一帧 hitbox 同时命中多个目标: 按目标 ID 排序依次处理, 每个目标独立判定

---

## Implementation Notes

**来自 ADR-0003 的具体指导**:

1. HitTargets 集合是 AttackInstance 的一部分:
```csharp
public struct AttackInstance
{
    // ...
    public HashSet<int> HitTargets; // 已命中角色 ID 集合
}
```

2. 命中检查公式: HasAlreadyHit = HitTargets.Contains(TargetId)

3. 碰撞管线中的位置 (ADR-0003):
   - 身份识别 → 自伤排除 → **多次命中检查 (HitTargets)** → 命中点计算 → HitEvent

4. OnTriggerEnter2D 天然去重: 仅在重叠开始时触发一次。HitTargets 提供额外防护层, 防止:
   - hitbox 被销毁后重新创建 (不正常数据)
   - 边缘情况下的重复回调

5. 多目标同时命中:
   - OnTriggerEnter2D 为每个 hurtbox 独立触发
   - 每个目标独立执行完整验证管线
   - HitTargets 正确追踪所有已命中目标

6. 投射物 HitTargets: 投射物有独立的 ProjectileState.HitTargets

---

## Out of Scope

- 攻击生命周期 (Story 001)
- Hitbox 定位 (Story 002)
- Hitstop (Story 004)
- 攻击类型解析 (Story 005)
- 投射物完整系统 (Story 006-007)
- 碰撞系统的自伤排除 (collision-system Story 003)

---

## QA Test Cases

- **AC-2 (命中加入集合)**:
  - Given: 战士 GroundAttack 命中 P2 (TargetId=2)
  - When: HitEvent 触发
  - Then: HitTargets = {2}

- **AC-3 (重复命中过滤)**:
  - Given: HitTargets = {2}, 同一攻击 Active 阶段继续
  - When: hitbox 继续与 P2 hurtbox 重叠
  - Then: HasAlreadyHit=true, 不触发 HitEvent

- **AC-4 (近战单次命中)**:
  - Given: 近战攻击进入 Active 阶段
  - When: 命中 P2, 然后 Active 阶段继续多帧
  - Then: P2 只被命中一次

- **AC-5 (投射物单次命中)**:
  - Given: 投射物飞行中, 已命中 P2
  - When: 投射物继续飞行再次碰到 P2
  - Then: HitTargets.Contains(2) = true, 忽略

- **AC-6 (攻击结束清理)**:
  - Given: 攻击结束 (Recovery 完成)
  - When: AttackInstance 清理
  - Then: HitTargets 集合释放

- **AC-7 (多目标独立判定)**:
  - Given: 4 人模式, hitbox 同时碰到 P2 和 P3
  - When: 两个 OnTriggerEnter2D 回调
  - Then: P2 和 P3 各自独立判定, HitTargets = {2, 3}

---

## Test Evidence

**Story Type**: Logic
**Required evidence**: `tests/unit/attack/multi-hit-prevention_test.cs`
**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: Story 001 (Attack Lifecycle), Story 002 (Hitbox Positioning)
- Unlocks: Story 004 (Hitstop — 命中后触发), Story 006 (Projectile — 投射物 HitTargets)
