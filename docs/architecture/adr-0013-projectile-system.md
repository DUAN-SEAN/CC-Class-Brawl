# ADR-0013: Projectile System — Independent GameObject + Horizontal Flight + Trigger Collision

## Status
Accepted

## Date
2026-05-24

## Engine Compatibility

| Field | Value |
|-------|-------|
| **Engine** | Unity 2022.3.51 LTS |
| **Domain** | Physics / Combat |
| **Knowledge Risk** | LOW — version is within LLM training data |
| **References Consulted** | `docs/engine-reference/unity/VERSION.md` |
| **Post-Cutoff APIs Used** | None |
| **Verification Required** | Verify independent projectile GameObject survives attacker destruction (OnDestroy does not cascade); verify BoxCollider2D trigger on "Hitbox" layer correctly detects SolidPlatform layer overlaps |

## ADR Dependencies

| Field | Value |
|-------|-------|
| **Depends On** | ADR-0001 (Physics Timestep — 60Hz, autoSyncTransforms), ADR-0002 (Dual FSM — CombatFSM attack phases), ADR-0003 (Hitbox/Hurtbox Detection — layers, trigger detection), ADR-0004 (Skill System — AttackData.IsProjectile/ProjectileSpeed/ProjectileLifetime) |
| **Enables** | Mage class base attacks (GroundAttack, AirAttack), ranged skills (Fireball, Ice Arrow) |
| **Blocks** | Mage class implementation, ranged skill implementation |
| **Ordering Note** | Core layer. Requires Foundation (physics) and Core (hitbox, FSM). Implements on top of existing attack system. |

## Context

### Problem Statement
攻击系统需要同时支持近战和投射物两种攻击模式。投射物从攻击者位置生成后独立飞行，拥有自己的碰撞体和生命周期，不随攻击者的状态（KO、受击）而变化。Mage 职业的地面/空中基础攻击和部分技能（Fireball、Ice Arrow）依赖投射物系统。系统需要在现有 hitbox/hurtbox 检测架构（ADR-0003）和技能数据结构（ADR-0004）中无缝集成。

### Constraints
- 投射物数据已在 AttackData 中定义：IsProjectile、ProjectileSpeed、ProjectileLifetime
- 碰撞层架构已在 ADR-0003 中确定：Hitbox(8) 与 Hurtbox(9)、SolidPlatform(11) 碰撞
- 投射物与投射物不碰撞（Hitbox 层不与自身碰撞）
- 穿越平台（PassThrough）不阻挡投射物
- 投射物数量极少（MVP 最多 5 个同屏）
- 角色使用 gravityScale=0 手动重力（ADR-0001），投射物无重力

### Requirements
- 投射物生成于 Active 帧开始时，位置 = 角色位置 + HitboxOffset × FacingDirection
- 水平直线飞行：Position += Speed × FacingDir × dt
- 三种销毁条件：超时（AgeFrames ≥ Lifetime）、命中 hurtbox、命中 solid platform
- 投射物与攻击者完全独立——攻击者 KO 后投射物继续飞行
- 防穿透：最小 hitbox 宽度 = Max(HitboxSize.x, ProjectileSpeed × dt × 2)
- 多命中防止：每实例 HitTargets 集合
- 同帧优先级：hurtbox 命中 > 平台命中

## Decision

采用 **独立 GameObject + AttackSystem 管理生命周期 + 现有 Hitbox/Hurtbox 层架构** 方案。

### 1. 投射物不是新系统——是 AttackSystem 的扩展

投射物不引入新的 MonoBehaviour 组件。现有 AttackSystem 管理 hitbox 生命周期，投射物是其一个分支：

```
AttackSystem.FixedUpdateSystem():
  for each active attack:
    if attack.Data.IsProjectile:
      UpdateProjectile(attack)      // 移动 + 老化 + 销毁检查
    else:
      UpdateMeleeHitbox(attack)     // 跟随角色位置
    CheckHitboxOverlaps(attack)     // 统一的碰撞检测（近战/投射物共用）
```

这避免了引入 ProjectileManager 单例——AttackSystem 已经遍历所有活跃攻击实例，只需在更新逻辑中分支处理。

### 2. 投射物 GameObject 结构

```csharp
// 投射物 hitbox 是独立 GameObject，不是角色的子物体
var go = new GameObject($"Projectile_{attack.AttackerId}_{attack.SpawnFrame}");
go.layer = LayerMask.NameToLayer("Hitbox"); // Layer 8
var collider = go.AddComponent<BoxCollider2D>();
collider.isTrigger = true;
collider.usedByEffector = false;
collider.size = ComputeProjectileHitboxSize(attack.Data);
go.transform.position = spawnPosition;
```

**独立 GameObject 的理由**：
- 角色被 KO 时 Destroy 角色 GameObject，子物体会被级联销毁
- 投射物需要在攻击者 KO 后继续存在
- 投射物位置独立于角色 Rigidbody2D，不受角色物理影响

### 3. 投射物更新逻辑

```csharp
public static class ProjectileFormulas
{
    // 水平直线飞行（无重力）
    public static Vector2 ComputePosition(Vector2 current, float speed, int facingDir, float dt)
    {
        return current + new Vector2(speed * facingDir * dt, 0f);
    }

    // 老化检查
    public static bool IsExpired(int ageFrames, int lifetime)
    {
        return ageFrames >= lifetime;
    }

    // 防穿透：最小 hitbox 宽度
    public static float ComputeMinHitboxWidth(float designerWidth, float speed, float dt)
    {
        return Mathf.Max(designerWidth, speed * dt * 2f);
    }
}
```

ProjectileFormulas 是纯静态类，可完整单元测试。

### 4. 投射物生命周期状态

```csharp
public struct ProjectileState
{
    public GameObject HitboxObject;
    public Vector2 Position;
    public int FacingDirection;
    public int SpawnFrame;
    public int AgeFrames;
    public bool IsDestroyed;
    public HashSet<int> HitTargets;  // 已命中角色 ID 集合
}
```

在 AttackSystem 中，每个攻击实例（AttackInstance）持有可选的 ProjectileState：

```csharp
public struct AttackInstance
{
    public int AttackerId;
    public AttackData Data;
    public int StartFrame;
    public AttackPhase Phase;
    public int PhaseFrame;

    // 近战 hitbox（角色子物体引用）
    public Collider2D MeleeHitbox;

    // 投射物状态（仅 IsProjectile=true 时使用）
    public ProjectileState? Projectile;
}
```

### 5. 销毁条件处理

```csharp
void UpdateProjectile(AttackInstance attack)
{
    var proj = attack.Projectile.Value;

    // 1. 移动
    proj.Position = ProjectileFormulas.ComputePosition(
        proj.Position, attack.Data.ProjectileSpeed,
        proj.FacingDirection, Time.fixedDeltaTime);
    proj.HitboxObject.transform.position = proj.Position;
    proj.AgeFrames++;

    // 2. 超时检查
    if (ProjectileFormulas.IsExpired(proj.AgeFrames, attack.Data.ProjectileLifetime))
    {
        DestroyProjectile(attack);
        return;
    }

    attack.Projectile = proj; // struct 是值类型，需要写回
}

// 碰撞回调中处理 hurtbox/platform 命中
void OnProjectileHitHurtbox(AttackInstance attack, int targetId)
{
    if (attack.Projectile.Value.HitTargets.Contains(targetId)) return; // 防多命中
    attack.Projectile.Value.HitTargets.Add(targetId);
    // 触发 HitEvent（与近战共用路径）
    CreateHitEvent(attack, targetId);
    DestroyProjectile(attack); // 命中即销毁
}

void OnProjectileHitPlatform(AttackInstance attack)
{
    DestroyProjectile(attack); // 命中平台即销毁，无 HitEvent
}
```

### 6. 同帧优先级

当投射物在同一帧内同时接触到 hurtbox 和 solid platform 时，hurtbox 优先：

```csharp
// AttackSystem 的碰撞处理顺序
void ProcessProjectileOverlaps(AttackInstance attack)
{
    var hits = new List<Collider2D>();
    var filter = new ContactFilter2D { layerMask = _hitboxContactMask, useLayerMask = true };
    attack.Projectile.Value.HitboxObject.GetComponent<Collider2D>()
        .OverlapCollider(filter, hits);

    bool hitHurtbox = false;
    foreach (var hit in hits)
    {
        if (hit.gameObject.layer == _hurtboxLayer)
        {
            int targetId = GetCharacterId(hit);
            if (!attack.Projectile.Value.HitTargets.Contains(targetId))
            {
                OnProjectileHitHurtbox(attack, targetId);
                hitHurtbox = true;
                break; // hurtbox 优先，处理后退出
            }
        }
    }

    // 只有未命中 hurtbox 时才检查平台
    if (!hitHurtbox)
    {
        foreach (var hit in hits)
        {
            if (hit.gameObject.layer == _solidPlatformLayer)
            {
                OnProjectileHitPlatform(attack);
                break;
            }
        }
    }
}
```

### 7. 投射物数量限制

```csharp
const int MaxProjectileCount = 5; // 可调，范围 1-10

void SpawnProjectile(AttackInstance attack)
{
    int activeCount = _activeAttacks.Count(a => a.Data.IsProjectile && a.Projectile.HasValue && !a.Projectile.Value.IsDestroyed);
    if (activeCount >= MaxProjectileCount)
    {
        // 超过上限：销毁最早的投射物
        DestroyOldestProjectile();
    }
    // ... 创建新投射物
}
```

### 8. 穿越平台处理

投射物 hitbox 在 "Hitbox" 层 (8)。Layer Collision Matrix 中 Hitbox(8) 不与 PassThrough 平台所在层碰撞——穿越平台不阻挡投射物。这与 ADR-0011 的平台层设置一致。

### 9. KO 独立性保证

```csharp
// 当攻击者被 KO 时
void OnCharacterKO(int characterId)
{
    // 遍历活跃攻击，只处理投射物
    foreach (var attack in _activeAttacks)
    {
        if (attack.AttackerId == characterId && attack.Data.IsProjectile)
        {
            // 不销毁投射物——让它继续飞行
            // 但标记攻击者已 KO，防止新的投射物生成
        }
    }
    // 清除该角色的近战 hitbox（角色被 Destroy 时子物体自动销毁）
}
```

### Architecture Diagram

```
┌─ AttackData (SO / SkillData) ────────────────────────┐
│  IsProjectile: bool                                   │
│  ProjectileSpeed: float                               │
│  ProjectileLifetime: int                              │
│  HitboxSize: Vector2                                  │
└──────────────────────────────────────────────────────┘
                         ↓ CombatFSM enters Active phase
┌─ AttackSystem (核心循环) ─────────────────────────────┐
│                                                       │
│  FixedUpdateSystem():                                 │
│    for each AttackInstance:                           │
│      if IsProjectile:                                │
│        Move → Age → DestroyCheck → ProcessOverlaps   │
│      else:                                           │
│        SyncToCharacter → ProcessOverlaps             │
│                                                       │
│  Projectile Lifecycle:                                │
│    Spawn → Active Phase → Flight → [Hit/Timeout]     │
│    ↓                                                 │
│    Independent GameObject on "Hitbox" layer           │
│    NOT child of character Rigidbody2D                 │
│                                                       │
│  Collision Priority:                                  │
│    Hurtbox hit > Platform hit (same frame)            │
│                                                       │
│  Multi-hit Prevention:                                │
│    HitTargets HashSet per projectile instance         │
│                                                       │
└──────────────────────────────────────────────────────┘
         ↓                ↓               ↓
    CollisionSystem   DamageSystem     KO Effect
  (overlap detection) (HitEvent)    (attacker KO → projectile survives)
```

### Key Interfaces

- `ProjectileFormulas` — 纯静态公式类，可单元测试
- `AttackInstance.Projectile` — 可选 ProjectileState（仅 IsProjectile=true）
- 投射物与近战共用 `IHitEventPublisher` 和碰撞检测路径

## Alternatives Considered

### Alternative 1: 独立 ProjectileManager MonoBehaviour
- **Description**: 创建专门的 ProjectileManager 管理所有投射物的生命周期
- **Pros**: 职责分离——投射物逻辑独立于近战逻辑
- **Cons**: AttackSystem 已经管理所有活跃攻击实例；两个系统需要协调（AttackSystem 创建 → ProjectileManager 更新）；增加一个 MonoBehaviour 和接口
- **Rejection Reason**: 投射物生命周期与攻击生命周期绑定（Active 帧创建，命中/超时销毁）。拆分到两个系统增加协调复杂度，而代码量收益极小（投射物更新逻辑约 50 行）

### Alternative 2: 投射物使用 Rigidbody2D 运动
- **Description**: 给投射物 GameObject 添加 Rigidbody2D，用 velocity 驱动移动
- **Cons**: 引入不必要的物理组件；需要设置 gravityScale=0；Rigidbody2D 的 trigger 回调不如直接位置更新直观；角色已经使用 gravityScale=0 手动控制，投射物不需要物理模拟
- **Rejection Reason**: 投射物做简单的水平直线运动，直接设置 transform.position 更简洁高效，与角色物理方案一致（gravityScale=0 + 手动位移）

### Alternative 3: 对象池（Object Pool）复用投射物
- **Description**: 预创建投射物 GameObject 池，复用而非 Instantiate/Destroy
- **Pros**: 避免频繁 GC 和 Instantiate 开销
- **Cons**: MVP 最多 5 个投射物同屏，每局创建/销毁次数极少；对象池增加初始化和回收逻辑复杂度；过早优化
- **Rejection Reason**: 投射物数量极少，Instantiate/Destroy 的性能影响可忽略。如果后期性能分析发现问题，可以引入对象池（架构不阻碍）

## Consequences

### Positive
- 投射物在现有 AttackSystem 中集成，不引入新系统
- ProjectileFormulas 纯静态类可完整单元测试
- 独立 GameObject 保证投射物与攻击者生命周期解耦
- 防穿透、多命中防止、同帧优先级等边缘情况明确处理
- 近战和投射物共用碰撞检测路径，代码复用

### Negative
- AttackSystem 的更新逻辑增加一个分支（if IsProjectile）
- ProjectileState 使用可空 struct（ProjectileState?），每次更新需要写回
- 独立 GameObject 需要手动管理 Destroy，忘记调用会导致泄漏

### Risks
- **投射物泄漏**: 如果 AttackSystem 在清理时遗漏投射物 GameObject → 缓解: 在 UnloadArena/回合重置时统一清理所有 "Hitbox" 层的独立 GameObject
- **防穿透不足**: ProjectileSpeed 上限 15.0 u/s，dt = 1/60，最小 hitbox 宽度 = Max(designerWidth, 15/60×2) = 0.5u，对 hurtbox 最小尺寸 0.4u 的场景仍有风险 → 缓解: 实际测试验证，如果出现穿透，降低最大速度或增大最小 hitbox 宽度
- **Layer Collision Matrix 误配置**: 如果 SolidPlatform 层未正确设置为与 Hitbox 层碰撞，投射物会穿透平台 → 缓解: ADR-0003 已定义完整的 Layer Matrix，实现时验证

## GDD Requirements Addressed

| GDD System | Requirement | How This ADR Addresses It |
|------------|-------------|--------------------------|
| attack-system.md | "Projectile lifecycle: Spawn → Flight → Collision/Destroy" | AttackSystem 生命周期管理 |
| attack-system.md | "Projectile spawns at CharacterPosition + HitboxOffset * FacingDirection" | Active phase spawn logic |
| attack-system.md | "Horizontal flight: Position += Speed * FacingDir * dt" | ProjectileFormulas.ComputePosition |
| attack-system.md | "AgeFrames = CurrentFrame - SpawnFrame; IsAlive = AgeFrames < Lifetime AND NOT Destroyed" | ProjectileFormulas.IsExpired |
| attack-system.md | "Destroy conditions: timeout, hurtbox hit, solid platform hit" | Three destroy paths |
| attack-system.md | "Attacker independence: projectile survives attacker KO" | Independent GameObject |
| attack-system.md | "Multi-hit prevention: HitTargets set per instance" | HashSet<int> HitTargets |
| attack-system.md | "MaxProjectileCount = 5" | Count check + oldest eviction |
| attack-system.md | "Same-frame priority: hurtbox hit > platform hit" | Ordered overlap processing |
| collision-system.md | "Projectile hitbox on Hitbox layer" | Layer 8 assignment |
| collision-system.md | "Projectile ignores PassThrough platforms" | Layer Matrix configuration |
| collision-system.md | "Projectile ignores other projectiles" | Hitbox layer no self-collision |
| collision-system.md | "Penetration prevention: MinHitboxWidth = Max(width, Speed * dt * 2)" | ProjectileFormulas.ComputeMinHitboxWidth |
| collision-system.md | "Projectile survives Blast Zone (no KO trigger for attacker)" | No Blast Zone check for projectiles |
| skill-database.md | "Fireball: Speed 7.0, Lifetime 90 frames" | Data-driven via AttackData |
| skill-database.md | "Ice Arrow: Speed 12.0, Lifetime 45 frames" | Data-driven via AttackData |
| skill-database.md | "ProjectileLifetime = 0: reject load" | Validation in SkillDatabase load |
| skill-database.md | "ProjectileSpeed = 0: load succeeds with warning" | Validation in SkillDatabase load |
| class-system.md | "Mage GroundAttack/AirAttack: IsProjectile = true" | Data-driven via ClassData.Attacks |
| class-system.md | "Graceful degradation: treat as melee if projectile system not implemented" | IsProjectile check with fallback |

## Performance Implications
- **CPU**: 投射物更新 per instance < 0.01ms (position + age + overlap check); 5 projectiles < 0.05ms total
- **Memory**: ProjectileState per instance ~100B; 5 instances ~500B; HitboxObject (BoxCollider2D) ~200B each; total < 2KB
- **Load Time**: 无影响（运行时创建/销毁）
- **Network**: 不适用

## Migration Plan
不适用——新项目初始架构决策。

## Validation Criteria
- [ ] Mage GroundAttack 生成投射物，水平飞向 FacingDirection
- [ ] 投射物命中 hurtbox → 造成伤害 → 销毁
- [ ] 投射物命中 solid platform → 销毁（无伤害）
- [ ] 投射物穿越 PassThrough 平台（无碰撞）
- [ ] 投射物投射物互相穿透（不碰撞）
- [ ] ProjectileLifetime 超时 → 销毁
- [ ] 攻击者被 KO → 投射物继续飞行
- [ ] 同一 hurtbox 不被同一投射物重复命中
- [ ] 同帧 hurtbox + platform → hurtbox 优先
- [ ] MaxProjectileCount 上限正确执行
- [ ] Hitbox 宽度 ≥ ProjectileSpeed × dt × 2（防穿透）
- [ ] ProjectileFormulas 单元测试通过（位置计算、超时检查、防穿透公式）
- [ ] 投射物更新 + 碰撞检测 < 0.05ms（5 个投射物）

## Related Decisions
- ADR-0001: Physics Timestep — 60Hz FixedTimestep，投射物更新在同一物理步
- ADR-0002: Dual FSM — CombatFSM Active phase 触发投射物生成
- ADR-0003: Hitbox/Hurtbox Detection — Layer Matrix、trigger 碰撞、OverlapCollider API
- ADR-0004: Skill System — AttackData.IsProjectile/ProjectileSpeed/ProjectileLifetime 字段
- ADR-0011: Arena Platform — PassThrough 平台不阻挡投射物
