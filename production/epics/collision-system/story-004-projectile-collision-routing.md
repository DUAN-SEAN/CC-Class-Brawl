# Story 004: Projectile Collision Routing — Hit Hurtbox→HitEvent, Hit Solid→Destroy, Passthrough Ignore, Penetration Prevention

> **Epic**: collision-system
> **Status**: Ready
> **Layer**: Core
> **Type**: Integration
> **Estimate**: M
> **Manifest Version**: 2026-05-24
> **Last Updated**: 2026-05-24

## Context

**GDD**: `design/gdd/collision-system.md`
**Requirement**: TR-COL-023 ~ TR-COL-031
**ADR Governing Implementation**: ADR-0003: Hitbox/Hurtbox Detection, ADR-0013: Projectile System
**ADR Decision Summary**: 投射物 hitbox 碰到 hurtbox → HitEvent + 通知攻击系统销毁。碰到 SolidPlatform → 通知攻击系统销毁 (无 HitEvent)。碰到 PassThrough → 忽略。碰到其他投射物 → 忽略 (Layer Matrix)。同帧 hurtbox > platform 优先。MinHitboxWidth 防穿透。
**Engine**: Unity 2022.3.51 LTS | **Risk**: LOW

**Control Manifest Rules**:
- Required: 投射物 hitbox 碰到 SolidPlatform → 通知攻击系统销毁 (无 HitEvent)
- Required: 近战 hitbox 碰到 SolidPlatform 的 OnTriggerEnter2D 被忽略
- Required: MinHitboxWidth = Max(designerWidth, ProjectileSpeed * fixedDeltaTime * 2)
- Guardrail: 碰撞系统帧耗时 < 0.5ms

---

## Acceptance Criteria

- [ ] 投射物 hitbox 碰到 hurtbox → 执行完整命中管线 (自伤排除 + 多次命中 + 命中点) → HitEvent 分发 + 通知攻击系统销毁投射物
- [ ] 投射物 hitbox 碰到 SolidPlatform → 通知攻击系统销毁投射物 (不创建 HitEvent, 不造成伤害)
- [ ] 投射物碰到 PassThrough platform → 无碰撞回调 (Layer Matrix 配置, 投射物穿过)
- [ ] 投射物碰到其他投射物 → 无碰撞回调 (Hitbox 层不与自身碰撞)
- [ ] 近战 hitbox 碰到 SolidPlatform 的 OnTriggerEnter2D → 忽略 (近战不会飞出角色范围)
- [ ] 同帧 hurtbox + SolidPlatform: hurtbox 命中优先处理, 投射物对目标造成伤害后销毁, platform 命中不再处理
- [ ] MinHitboxWidth: 投射物 hitbox 创建时强制 width >= ProjectileSpeed * dt * 2

---

## Implementation Notes

**来自 ADR-0003/ADR-0013 的具体指导**:

1. CollisionDetector 中区分投射物碰撞:
```
OnTriggerEnter2D(Collider2D other):
  if (self is hitbox && other is hurtbox):
    ExecuteHitPipeline(self, other)   // 标准 hit 流程
  elif (self is hitbox && other is SolidPlatform):
    if (IsProjectileHitbox(self)):
      NotifyAttackSystemDestroyProjectile(self)  // 仅投射物
    else:
      Ignore  // 近战忽略
```

2. 同帧优先级 (由 AttackSystem 处理, 但碰撞系统需要正确路由):
   - 碰撞系统将所有重叠事件发送给攻击系统
   - 攻击系统决定优先级: hurtbox > platform

3. PassThrough 忽略:
   - Layer Matrix 配置: Hitbox 不与 PassThrough 层碰撞
   - 不需要运行时检查, 物理引擎自动过滤

4. 投射物互相穿过:
   - Layer Matrix: Hitbox 不与 Hitbox 碰撞
   - 不需要运行时检查

5. MinHitboxWidth 执行:
   - 在投射物 hitbox 创建时 (attack-system Story 006)
   - 碰撞系统可额外验证 hitbox 宽度

---

## Out of Scope

- Layer Matrix 配置 (Story 001)
- HitEvent 构建 (Story 002)
- 自伤排除/多次命中逻辑 (Story 003)
- Hurtbox 大小管理 (Story 005)
- 命中点计算公式 (Story 006)
- 投射物生成/飞行 (attack-system Story 006)

---

## QA Test Cases

- **AC-1 (投射物命中 hurtbox)**:
  - Given: 投射物 hitbox 与角色 hurtbox 重叠
  - When: OnTriggerEnter2D 触发
  - Then: 完整命中管线执行, HitEvent 分发, 通知攻击系统销毁投射物

- **AC-2 (投射物命中 SolidPlatform)**:
  - Given: 投射物碰到实心平台
  - When: 碰撞回调触发
  - Then: 通知攻击系统销毁投射物, 无 HitEvent

- **AC-3 (PassThrough 穿过)**:
  - Given: 投射物飞行方向上有穿越平台
  - When: 物理步执行
  - Then: 无碰撞回调, 投射物穿过

- **AC-4 (投射物互相穿过)**:
  - Given: 两个投射物 hitbox 重叠
  - When: 物理步执行
  - Then: 无碰撞回调

- **AC-5 (近战忽略 platform)**:
  - Given: 近战 hitbox 碰到 SolidPlatform
  - When: OnTriggerEnter2D 触发
  - Then: 忽略, 无动作

- **AC-6 (同帧优先级)**:
  - Given: 投射物同一帧碰到 hurtbox 和 SolidPlatform
  - When: 两个碰撞回调触发
  - Then: hurtbox 命中优先处理 → HitEvent 创建 → 投射物销毁

---

## Test Evidence

**Story Type**: Integration
**Required evidence**: `tests/integration/collision/projectile-collision-routing_test.cs`
**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: Story 001 (Layer Matrix), Story 002 (HitEvent Construction), Story 003 (Self-hit/Multi-hit)
- Unlocks: Story 005 (Hurtbox Management), Story 006 (Hitpoint Calculation)
