# Story 006: Projectile System — Independent GameObject, Spawn, Flight, Lifetime, Pooling

> **Epic**: attack-system
> **Status**: Ready
> **Layer**: Core
> **Type**: Logic
> **Estimate**: L
> **Manifest Version**: 2026-05-24
> **Last Updated**: 2026-05-24

## Context

**GDD**: `design/gdd/attack-system.md`
**Requirement**: TR-ATK-020 ~ TR-ATK-029 (投射物生成/飞行/存活)
**ADR Governing Implementation**: ADR-0013: Projectile System, ADR-0003: Hitbox/Hurtbox Detection
**ADR Decision Summary**: 投射物是独立 GameObject (非角色子物体), 在 Active 阶段生成于角色位置+HitboxOffset*FacingDirection。水平直线飞行: Position += Speed * FacingDir * dt。三销毁条件: 超时(AgeFrames >= Lifetime), 命中hurtbox, 命中solid platform。投射物与攻击者完全独立。MaxProjectileCount=5。ProjectileFormulas 纯静态类可单元测试。不使用对象池 (MVP投射物极少)。
**Engine**: Unity 2022.3.51 LTS | **Risk**: LOW

**Control Manifest Rules**:
- Required: 投射物 hitbox 是独立 GameObject, 不在角色层级下
- Required: 投射物位置在 FixedUpdate 中手动更新
- Required: Physics2D.autoSyncTransforms = true
- Required: MinHitboxWidth = Max(designerWidth, ProjectileSpeed * fixedDeltaTime * 2)
- Guardrail: 5 projectiles total < 0.05ms

---

## Acceptance Criteria

- [ ] 投射物在 Active 阶段开始时生成, 位置 = 角色位置 + HitboxOffset * FacingDirection
- [ ] 投射物是独立 GameObject (非角色子物体), layer = "Hitbox" (8)
- [ ] 投射物以 ProjectileSpeed 沿 FacingDirection 水平直线飞行, 无重力
- [ ] 投射物拥有 BoxCollider2D (isTrigger=true), size 由 AttackData.HitboxSize 决定
- [ ] 投射物存活判定: AgeFrames < ProjectileLifetime AND NOT WasDestroyed
- [ ] 投射物超时 (AgeFrames >= ProjectileLifetime) 自动销毁
- [ ] MaxProjectileCount (5) 限制: 超过时销毁最早的投射物
- [ ] 攻击者被 KO 后投射物继续飞行, 不受影响
- [ ] ProjectileLifetime=0: 投射物生成后立即销毁, 记录警告
- [ ] ProjectileSpeed=0: 投射物在原位不动, 存活直到超时 (合法但奇怪)
- [ ] 投射物 hitbox 最小宽度: Max(HitboxSize.x, ProjectileSpeed * dt * 2)

---

## Implementation Notes

**来自 ADR-0013 的具体指导**:

1. 投射物是 AttackSystem 的扩展, 不引入新系统:
```
AttackSystem.FixedUpdateSystem():
  for each active attack:
    if IsProjectile:
      UpdateProjectile(attack)
    else:
      UpdateMeleeHitbox(attack)
```

2. 投射物 GameObject 创建:
```csharp
var go = new GameObject($"Projectile_{attack.AttackerId}_{attack.SpawnFrame}");
go.layer = LayerMask.NameToLayer("Hitbox");
var collider = go.AddComponent<BoxCollider2D>();
collider.isTrigger = true;
collider.size = ComputeProjectileHitboxSize(attack.Data);
go.transform.position = spawnPosition;
```

3. ProjectileFormulas 纯静态类:
```csharp
public static class ProjectileFormulas
{
    public static Vector2 ComputePosition(Vector2 current, float speed, int facingDir, float dt)
    public static bool IsExpired(int ageFrames, int lifetime)
    public static float ComputeMinHitboxWidth(float designerWidth, float speed, float dt)
}
```

4. ProjectileState 结构:
```csharp
public struct ProjectileState
{
    public GameObject HitboxObject;
    public Vector2 Position;
    public int FacingDirection;
    public int SpawnFrame;
    public int AgeFrames;
    public bool IsDestroyed;
    public HashSet<int> HitTargets;
}
```

5. KO 独立性: 攻击者被 KO 时不销毁投射物, 仅标记攻击者已 KO

6. 不使用对象池 — MVP 投射物极少, Instantiate/Destroy 开销可忽略

---

## Out of Scope

- 投射物碰撞检测 (Story 007 — 命中 hurtbox/platform 的处理)
- 近战 hitbox 管理 (Story 001-002)
- 多次命中防护的投射物部分 (Story 003 已包含)
- 投射物的视觉/音频效果 (VFX/audio epic)

---

## QA Test Cases

- **AC-1 (生成位置)**:
  - Given: 法师 GroundAttack (IsProjectile=true), 角色在 (3.0, 1.0), FacingDirection=1, HitboxOffset=(0.5, 0.2)
  - When: Active 阶段开始
  - Then: 投射物生成在 (3.5, 1.2)

- **AC-2 (独立 GameObject)**:
  - Given: 投射物已生成
  - When: 检查 parent
  - Then: parent = null, 不在角色层级下

- **AC-3 (水平飞行)**:
  - Given: ProjectileSpeed=8.0, FacingDirection=1
  - When: 1 帧更新 (dt=1/60)
  - Then: Position.x += 8.0/60 = 0.133u

- **AC-5 (超时销毁)**:
  - Given: ProjectileLifetime=60, SpawnFrame=100
  - When: 帧编号达到 160
  - Then: IsExpired=true, 投射物销毁

- **AC-7 (MaxProjectileCount)**:
  - Given: 5 个投射物已存在
  - When: 第 6 个投射物需要生成
  - Then: 最早的投射物被销毁, 新投射物生成

- **AC-8 (攻击者 KO 独立)**:
  - Given: 投射物飞行中
  - When: 攻击者被 KO
  - Then: 投射物继续飞行不受影响

- **AC-9 (Lifetime=0)**:
  - Given: ProjectileLifetime=0
  - When: 投射物生成
  - Then: 立即销毁, 记录警告

- **AC-11 (最小 hitbox 宽度)**:
  - Given: ProjectileSpeed=15.0, designerWidth=0.2
  - When: hitbox 创建
  - Then: width = Max(0.2, 15.0/60*2) = 0.5u

---

## Test Evidence

**Story Type**: Logic
**Required evidence**: `tests/unit/attack/projectile-system_test.cs`
**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: Story 001 (Attack Lifecycle), Story 002 (Hitbox Positioning — 生成位置公式), Story 005 (Attack Type Resolution — 确定是投射物攻击)
- Unlocks: Story 007 (Projectile Collision — 命中/碰撞处理)
