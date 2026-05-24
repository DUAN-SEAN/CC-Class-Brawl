# Story 007: Projectile Collision — Hit Hurtbox, Hit Solid, Passthrough Platforms, Penetration Prevention

> **Epic**: attack-system
> **Status**: Ready
> **Layer**: Core
> **Type**: Integration
> **Estimate**: M
> **Manifest Version**: 2026-05-24
> **Last Updated**: 2026-05-24

## Context

**GDD**: `design/gdd/attack-system.md`, `design/gdd/collision-system.md`
**Requirement**: TR-ATK-030 ~ TR-ATK-036, TR-COL-025 ~ TR-COL-031
**ADR Governing Implementation**: ADR-0013: Projectile System, ADR-0003: Hitbox/Hurtbox Detection
**ADR Decision Summary**: 投射物碰撞: 命中 hurtbox → HitEvent + 销毁; 命中 solid platform → 销毁 (无 HitEvent); 碰到 passthrough platform → 忽略; 投射物互相穿过。同帧优先级: hurtbox > platform。防穿透: MinHitboxWidth = Max(designWidth, Speed*dt*2)。HitTargets HashSet 防止同一投射物重复命中同一目标。
**Engine**: Unity 2022.3.51 LTS | **Risk**: LOW

**Control Manifest Rules**:
- Required: Layer "Hitbox" (8) 与 Hurtbox(9), SolidPlatform(11) 碰撞
- Required: Hitbox 不与 Hitbox 碰撞 (投射物互相穿过)
- Required: 投射物 hitbox 碰到 SolidPlatform → 通知攻击系统销毁 (无 HitEvent)
- Required: MinHitboxWidth 强制执行
- Guardrail: 碰撞系统帧耗时 < 0.5ms

---

## Acceptance Criteria

- [ ] 投射物 hitbox 碰到 hurtbox → 创建 HitEvent (含 AttackerId, TargetId, AttackId, HitPoint), 通知攻击系统, 销毁投射物
- [ ] 投射物 hitbox 碰到 solid platform → 销毁投射物 (不创建 HitEvent, 不造成伤害)
- [ ] 投射物碰到 passthrough platform → 忽略 (投射物穿过, 无碰撞回调)
- [ ] 投射物碰到其他投射物 → 忽略 (Hitbox 层不与自身碰撞)
- [ ] 同一投射物对同一目标只命中一次 (HitTargets 集合)
- [ ] 同帧 hurtbox + platform: hurtbox 命中优先处理 → 投射物销毁, platform 忽略
- [ ] 投射物飞出 Blast Zone: 不触发攻击者 KO, 正常存活直到超时
- [ ] 投射物 hitbox 宽度 >= ProjectileSpeed * dt * 2 (防穿透)

---

## Implementation Notes

**来自 ADR-0013/ADR-0003 的具体指导**:

1. 同帧优先级处理:
```csharp
void ProcessProjectileOverlaps(AttackInstance attack)
{
    // 先检查所有 hurtbox
    bool hitHurtbox = false;
    foreach (var hit in overlaps)
    {
        if (hit.layer == _hurtboxLayer && !HitTargets.Contains(targetId))
        {
            OnProjectileHitHurtbox(attack, targetId);
            hitHurtbox = true;
            break;
        }
    }
    // 只有未命中 hurtbox 才检查 platform
    if (!hitHurtbox)
    {
        foreach (var hit in overlaps)
        {
            if (hit.layer == _solidPlatformLayer)
                OnProjectileHitPlatform(attack);
        }
    }
}
```

2. Layer 碰撞规则 (Layer Collision Matrix):
   - Hitbox(8) ↔ Hurtbox(9): 碰撞
   - Hitbox(8) ↔ SolidPlatform(11): 碰撞
   - Hitbox(8) ↔ Hitbox(8): 不碰撞 (投射物互相穿过)
   - PassThrough 平台不与 Hitbox 碰撞 (穿越)

3. 防穿透:
   - ProjectileFormulas.ComputeMinHitboxWidth(designerWidth, speed, dt)
   - 投射物 15 u/s → MinHitboxWidth = Max(设计值, 0.5u)

4. Blast Zone 处理:
   - 投射物不检测 Blast Zone, 正常存活直到 Lifetime 耗尽

5. 近战 hitbox 碰到 SolidPlatform 的 OnTriggerEnter2D 被忽略 (近战不会飞出角色范围)

---

## Out of Scope

- 近战 hitbox 碰撞 (collision-system epic)
- 投射物生成/飞行 (Story 006)
- 投射物的视觉/音频效果
- 投射物对象池 (MVP 不需要)

---

## QA Test Cases

- **AC-1 (命中 hurtbox)**:
  - Given: 投射物 hitbox 与角色 hurtbox 重叠
  - When: OnTriggerEnter2D 触发
  - Then: HitEvent 创建并分发, 投射物销毁

- **AC-2 (命中 solid platform)**:
  - Given: 投射物碰到实心平台/墙壁
  - When: 碰撞回调触发
  - Then: 投射物销毁, 无 HitEvent, 无伤害

- **AC-3 (passthrough 穿过)**:
  - Given: 投射物飞行方向上有穿越平台
  - When: 碰撞检测执行
  - Then: 无碰撞回调, 投射物穿过

- **AC-4 (投射物互相穿过)**:
  - Given: 两个投射物在同一位置
  - When: 碰撞检测执行
  - Then: 无碰撞回调, 投射物互相穿过

- **AC-5 (单次命中)**:
  - Given: 投射物已命中 P2 (HitTargets={2})
  - When: 投射物继续飞行碰到 P2
  - Then: HitTargets.Contains(2)=true, 忽略

- **AC-6 (同帧优先级)**:
  - Given: 投射物同一帧碰到 hurtbox 和 solid platform
  - When: 处理碰撞
  - Then: hurtbox 命中优先, 投射物对目标造成伤害后销毁

- **AC-7 (Blast Zone 无影响)**:
  - Given: 投射物飞出可视范围
  - When: 超出 Blast Zone
  - Then: 不触发攻击者 KO, 投射物存活直到超时

- **AC-8 (防穿透)**:
  - Given: ProjectileSpeed=15.0, 设计师 HitboxSize.x=0.2
  - When: hitbox 创建
  - Then: 宽度 = Max(0.2, 0.5) = 0.5u

---

## Test Evidence

**Story Type**: Integration
**Required evidence**: `tests/integration/attack/projectile-collision_test.cs`
**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: Story 006 (Projectile System — 投射物必须已存在才能检测碰撞), collision-system Story 001 (Layer Matrix 配置), collision-system Story 002 (HitEvent 构建)
- Unlocks: None (attack-system epic 最终 story)
