# ADR-0006: Damage & Knockback Pipeline — Pure Computation Layer + Blast Zone KO Detection

## Status
Accepted

## Date
2026-05-24

## Engine Compatibility

| Field | Value |
|-------|-------|
| **Engine** | Unity 2022.3.51 LTS |
| **Domain** | Core (Combat Pipeline) |
| **Knowledge Risk** | LOW — version is within LLM training data |
| **References Consulted** | `docs/engine-reference/unity/VERSION.md` |
| **Post-Cutoff APIs Used** | None |
| **Verification Required** | Verify Rigidbody2D.velocity assignment order in knockback does not conflict with 3C FixedUpdate velocity assignment |

## ADR Dependencies

| Field | Value |
|-------|-------|
| **Depends On** | ADR-0001 (Physics Timestep — 60Hz basis, Gravity/TerminalVelocity constants), ADR-0002 (Dual FSM — HitStun/Knockback state definitions, IMovementController.SetVelocity), ADR-0003 (Hitbox/Hurtbox — HitEvent struct), ADR-0004 (Skill System — unified AttackData struct) |
| **Enables** | Focus System (focus gain on hit), Skill Draw System (focus trigger), Match Management (KO event), Battle HUD (damage display, KO animation) |
| **Blocks** | Focus system, match management, HUD — all downstream combat consumers |
| **Ordering Note** | Must be Accepted before focus/skill-draw/match-management implementation |

## Context

### Problem Statement
每次命中需要将攻击数据转化为两个核心输出：被击者累积伤害百分比（DamagePercent）和击退力度（KnockbackMagnitude），然后将力度转化为带方向的物理速度，并持续监控角色是否飞出 Blast Zone 触发 KO。伤害计算是纯数学层（无状态机），击退物理委托 3C 的 SetVelocity，KO 判定查询场地的 Blast Zone 边界。三个职责（伤害累积、击退向量、KO 检测）需要明确的数据流和接口边界。

### Constraints
- 所有计算在 60Hz FixedUpdate 中执行，不使用 deltaTime 乘法（击退衰减除外）
- 伤害和击退计算必须在一帧内同步完成——KnockbackMagnitude 在 DamagePercent 更新后立即计算（使用更新后的百分比）
- KO 判定每帧执行，使用严格不等式
- 击退系统不直接操作 Rigidbody2D——通过 IMovementController.SetVelocity 委托
- Gravity=32.0 u/s², TerminalVelocity=20.0 u/s 与 3C 系统共享常量

### Requirements
- 伤害百分比只增不减（MVP），浮点精度存储
- 击退力度公式：BaseKnockbackGrowth × (DamagePercent/100) × BaseKnockback + BaseKnockback
- 击退方向：水平由攻击者→被击者位置决定，仰角由 KnockbackLaunchRatio 控制
- 击退速度衰减（不可操作期 + 恢复期两阶段）
- Blast Zone KO 检测（严格不等式）
- 统一 AttackData 消费（不区分职业/技能来源）

## Decision

采用 **纯计算层 + 两阶段击退衰减 + 每帧 Blast Zone 检查** 架构：

### 1. DamageCalculator — 无状态纯计算

DamageCalculator 不持有任何运行时状态，仅提供静态计算方法：

```csharp
public static class DamageFormulas
{
    public static float CalculateDamageGain(float baseDamage)
        => baseDamage;

    public static float CalculateKnockbackMagnitude(
        float baseKnockbackGrowth,
        float targetDamagePercent,
        float baseKnockback)
        => baseKnockbackGrowth * (targetDamagePercent / 100f) * baseKnockback
           + baseKnockback;

    public static int ToDisplayPercent(float damagePercent)
        => (int)Mathf.Floor(damagePercent);
}
```

理由：公式是纯函数，无副作用，天然可单元测试。不需要 MonoBehaviour 或 ScriptableObject。

### 2. DamageSystem — 每角色状态持有者

DamageSystem 是 MonoBehaviour，持有每角色的 DamagePercent 状态，订阅命中事件，协调计算和分发：

```
DamageSystem (MonoBehaviour, per-scene singleton)
  ├── float[] _damagePercent  (indexed by playerIndex)
  ├── AttackData lookup via AttackSystem/ClassData
  ├── subscribes to: OnHitDetected (from CombatFSM)
  ├── dispatches: OnHitProcessed, OnDamagePercentChanged
  └── provides: IDamageSystem interface
```

**命中处理管线（每帧，在 FixedUpdate 中由 CombatFSM 触发）：**

```
OnHitDetected(HitEvent)
  → 查询 AttackData (by AttackId)
  → _damagePercent[target] += AttackData.BaseDamage
  → KnockbackMagnitude = DamageFormulas.CalculateKnockbackMagnitude(...)
  → 分发 OnHitProcessed(HitEvent, AttackData, KnockbackMagnitude)
  → 分发 OnDamagePercentChanged(target, _damagePercent[target])
```

### 3. KnockbackSystem — 两阶段衰减 + KO 检查

KnockbackSystem 也是 MonoBehaviour，持有每角色的击退运行时状态：

```csharp
public struct KnockbackRuntimeState
{
    public bool IsActive;
    public bool IsKO;
    public Vector2 CurrentVelocity;
    public bool IsInHitstun;     // true during unopérable period
    public float RemainingHitstunFrames;
}
```

**击退向量计算：**

```csharp
public static class KnockbackFormulas
{
    public static Vector2 CalculateKnockbackVector(
        Vector2 attackerPos, Vector2 targetPos,
        FacingDirection attackerFacing,
        float knockbackMagnitude,
        float knockbackSpeedMultiplier,
        float knockbackLaunchRatio)
    {
        float horizontalDir = Mathf.Sign(targetPos.x - attackerPos.x);
        if (horizontalDir == 0f)
            horizontalDir = (int)attackerFacing;

        var dir = new Vector2(horizontalDir, knockbackLaunchRatio).normalized;
        return dir * knockbackMagnitude * knockbackSpeedMultiplier;
    }
}
```

**不可操作期物理更新（每帧，仅在 KnockbackRuntimeState.IsActive 且 IsInHitstun 时）：**

```csharp
void UpdateKnockbackPhysics(int playerIndex, float dt)
{
    ref var state = ref _states[playerIndex];
    state.CurrentVelocity.x *= _knockbackDecayRate;
    state.CurrentVelocity.y -= Constants.Gravity * dt;
    state.CurrentVelocity.y = Mathf.Max(state.CurrentVelocity.y, -Constants.TerminalVelocity);

    _movementControllers[playerIndex].SetVelocity(state.CurrentVelocity);
}
```

**恢复期衰减（每帧，仅 IsInHitstun=false 且 |Vx| > MaxAirSpeed 时）：**

```csharp
void UpdateRecoveryDecay(int playerIndex)
{
    ref var state = ref _states[playerIndex];
    if (Mathf.Abs(state.CurrentVelocity.x) > _maxAirSpeed)
    {
        state.CurrentVelocity.x *= _knockbackRecoveryRate;
        _movementControllers[playerIndex].SetVelocity(state.CurrentVelocity);
    }
    else
    {
        state.IsActive = false; // 3C 正常空中控制接管
    }
}
```

**Blast Zone KO 检查（每帧，对所有活跃角色）：**

```csharp
void CheckBlastZone(int playerIndex)
{
    ref var state = ref _states[playerIndex];
    if (state.IsKO) return;

    var pos = _movementControllers[playerIndex].GetPosition();
    var blast = _arena.GetBlastZone();

    if (pos.x < blast.Left || pos.x > blast.Right
     || pos.y < blast.Bottom || pos.y > blast.Top)
    {
        state.IsKO = true;
        state.IsActive = false;
        // 计算方向：哪个边界被越过
        var koDir = CalculateKODirection(pos, blast);
        OnKO?.Invoke(playerIndex, koDir);
    }
}
```

### 4. 执行时序（在 FixedUpdate 60Hz 中）

```
CombatFSM.FixedUpdate:
  1. 检查 InputBuffer → 可能触发攻击
  2. 推进攻击帧阶段
  3. 如果命中事件到达（从碰撞系统回调）:
     → DamageSystem.ProcessHit(HitEvent)
       → 更新 DamagePercent
       → 计算 KnockbackMagnitude
       → 返回给 CombatFSM
     → CombatFSM 判定 HitStun/Knockback
     → 如果 Knockback:
       → KnockbackSystem.ApplyKnockback(playerIndex, vector, hitstunFrames)
       → IMovementController.SetVelocity(vector)

KnockbackSystem.FixedUpdate (独立，在 CombatFSM 之后):
  1. 对每个处于击退状态的角色：
     → 如果 IsInHitstun: UpdateKnockbackPhysics
     → 如果 !IsInHitstun: UpdateRecoveryDecay
  2. 对每个活跃角色: CheckBlastZone
```

### 5. 接口定义

```csharp
public interface IDamageSystem
{
    float GetDamagePercent(int playerIndex);
    int GetDisplayPercent(int playerIndex);
    void ResetDamage(int playerIndex);
    void ResetAll();

    event Action<int, float> OnDamagePercentChanged;
    // int = playerIndex, float = new DamagePercent
}

public interface IKnockbackSystem
{
    KnockbackState GetKnockbackState(int playerIndex);
    Vector2 GetKnockbackVelocity(int playerIndex);
    void ResetKnockback(int playerIndex);
    void ResetAll();

    event Action<int, Vector2> OnKO;
    // int = playerIndex, Vector2 = KO direction
}

public enum KnockbackState
{
    None,
    Hitstun,    // 不可操作期
    Recovering, // 可操作恢复期
    KO          // 已被 KO
}
```

### Architecture Diagram

```
┌─ HitEvent Flow ──────────────────────────────────────┐
│                                                       │
│  CollisionDetector.OnTriggerEnter2D                   │
│       ↓ HitEvent                                      │
│  CombatFSM.ProcessHit                                 │
│       ↓ HitEvent                                      │
│  DamageSystem.ProcessHit                              │
│    ├── _damagePercent[target] += BaseDamage           │
│    ├── KnockbackMagnitude = DamageFormulas.Calc(...)  │
│    ├── OnDamagePercentChanged(target, newPercent)      │
│    └── OnHitProcessed(event, attackData, magnitude)   │
│       ↓                                               │
│  CombatFSM evaluates HitStun vs Knockback             │
│    (KnockbackMagnitude > KnockbackThreshold?)         │
│       ↓ if Knockback                                  │
│  KnockbackSystem.ApplyKnockback                       │
│    ├── KnockbackVector = KnockbackFormulas.Calc(...)  │
│    └── IMovementController.SetVelocity(vector)        │
│                                                       │
└───────────────────────────────────────────────────────┘

┌─ Per-Frame Update (FixedUpdate 60Hz) ────────────────┐
│                                                       │
│  KnockbackSystem.FixedUpdate:                         │
│    for each active player:                            │
│      if Hitstun:  UpdateKnockbackPhysics (decay+grav) │
│      if Recovering: UpdateRecoveryDecay               │
│      if !KO: CheckBlastZone → OnKO if out of bounds  │
│                                                       │
└───────────────────────────────────────────────────────┘
```

## Alternatives Considered

### Alternative 1: 单一 DamageAndKnockbackSystem 合并类
- **Description**: 将伤害计算和击退物理合并为一个 MonoBehaviour
- **Pros**: 减少类数量，命中处理在单一方法中完成
- **Cons**: 违反单一职责——伤害计算是纯数学，击退是物理模拟，KO 是位置检查；测试困难；职责不清
- **Rejection Reason**: 三个职责有不同的变化原因：伤害公式随平衡调整，击退物理随引擎特性调整，KO 判定随场地配置变化

### Alternative 2: ScriptableObject 存储运行时 DamagePercent
- **Description**: 将 DamagePercent 存储在 SO 中而非 MonoBehaviour
- **Cons**: SO 是共享资产，不应用于运行时可变状态；两个角色共享同一个 SO 会互相干扰
- **Rejection Reason**: 违反 ADR-0004 的"运行时只读原则"——SO 是只读数据，运行时状态由系统管理器持有

## Consequences

### Positive
- DamageFormulas 和 KnockbackFormulas 是纯静态类——100% 可单元测试，无需 Unity 运行时
- 伤害累积、击退物理、KO 检测三个职责清晰分离
- 统一 AttackData 消费——职业招式和技能招式走完全相同的伤害/击退管线
- 通过 IDamageSystem/IKnockbackSystem 接口暴露，下游系统不耦合实现细节

### Negative
- 命中处理跨越 3 个系统（CombatFSM → DamageSystem → KnockbackSystem），调试时需要跟踪事件流
- KnockbackSystem 需要每帧更新所有角色的击退状态和 Blast Zone 检查，即使大部分角色不在击退中
- DamageSystem 需要引用 AttackSystem 或 ClassData 来查询 AttackData——增加了初始化依赖

### Risks
- **DamagePercent 更新与 KnockbackMagnitude 计算的帧内串行依赖**: 第二次命中使用的 DamagePercent 包含第一次增量——这是设计意图，不是 bug，但实现时必须保证顺序处理
- **KnockbackSystem.SetVelocity 与 3C SetVelocity 冲突**: 两者在同一 FixedUpdate 中调用 SetVelocity → 缓解: ADR-0002 的执行顺序保证 CombatFSM 在 3C 之后运行，KnockbackSystem 在 CombatFSM 之后运行
- **Blast Zone 查询频率**: 每帧查询 IArenaDataProvider.GetBlastZone() → 缓解: 场地数据在战斗中是只读的，KnockbackSystem 可以在战斗开始时缓存 BoundsData 引用
- **极端速度穿越 Blast Zone**: 理论上速度 > 1800 u/s 时可能一帧穿越整个 Blast Zone → 缓解: MVP 中不可能达到此速度（正常范围 < 35 u/s）

## GDD Requirements Addressed

| GDD System | Requirement | How This ADR Addresses It |
|------------|-------------|--------------------------|
| damage-calculation-system.md | "DamagePercent 只增不减，浮点精度存储" | DamageSystem._damagePercent float[], MVP 无重置路径 |
| damage-calculation-system.md | "DamageGain = AttackData.BaseDamage" | DamageFormulas.CalculateDamageGain |
| damage-calculation-system.md | "KnockbackMagnitude = BBG × (DP/100) × BB + BB" | DamageFormulas.CalculateKnockbackMagnitude |
| damage-calculation-system.md | "命中处理管线 7 步" | DamageSystem.ProcessHit 流程 |
| damage-calculation-system.md | "IDamageSystem 接口" | IDamageSystem 定义 |
| knockback-launch-system.md | "击退方向 = normalize(dir, ratio) × magnitude × multiplier" | KnockbackFormulas.CalculateKnockbackVector |
| knockback-launch-system.md | "不可操作期: Vx *= DecayRate; Vy -= Gravity × dt" | KnockbackSystem.UpdateKnockbackPhysics |
| knockback-launch-system.md | "恢复期: |Vx| > MaxAirSpeed 时继续衰减" | KnockbackSystem.UpdateRecoveryDecay |
| knockback-launch-system.md | "KO: position outside BlastZone (strict inequality)" | KnockbackSystem.CheckBlastZone |
| knockback-launch-system.md | "IKnockbackSystem 接口" | IKnockbackSystem 定义 |
| knockback-launch-system.md | "Gravity=32.0, TerminalVelocity=20.0 与 3C 一致" | 共享 Constants 类 |
| combat-state-machine.md | "KnockbackThreshold=9.0 判定" | CombatFSM 使用 DamageSystem 输出的 KnockbackMagnitude 判定 |

## Performance Implications
- **CPU**: DamageFormulas + KnockbackFormulas 计算 < 1μs/hit; KnockbackSystem per-frame update (2 players) < 0.1ms
- **Memory**: KnockbackRuntimeState × 2 = ~64B; float[] damagePercent × 2 = 8B; negligible
- **Load Time**: 无额外加载
- **Network**: 不适用

## Migration Plan
不适用——新项目初始架构决策。

## Validation Criteria
- [ ] Warrior GroundAttack (BB=8.0) 命中 DP=0 目标 → KnockbackMagnitude=8.0
- [ ] Warrior GroundAttack (BB=8.0) 命中 DP=100 目标 → KnockbackMagnitude=8.4
- [ ] 击退向量: 攻击者(-2,0.75), 被击者(2,0.75), Magnitude=8.4, Multiplier=2.0 → Vector≈(11.88, 11.88)
- [ ] 不可操作期 1 帧物理: Vx=11.88→11.76, Vy=11.88→11.35 (DecayRate=0.99, Gravity=32.0)
- [ ] KO: position=(16.5, 3.0), BlastRight=15.0 → IsKO=true
- [ ] 边界: position=(15.0, 3.0), BlastRight=15.0 → IsKO=false (严格不等式)
- [ ] DamagePercent 重置: OnRoundStart → 所有角色 DP=0.0
- [ ] AttackId 无效: 命中被忽略，记录错误
- [ ] BaseKnockback=0: KnockbackMagnitude=0，不进入 Knockback 状态
- [ ] 伤害 + 击退 + KO 管线帧耗时 < 0.2ms（2 人对战）

## Related Decisions
- ADR-0001: Physics Timestep — 共享 Gravity/TerminalVelocity 常量
- ADR-0002: Dual FSM — CombatFSM 消费 KnockbackMagnitude 判定 HitStun/Knockback
- ADR-0003: Hitbox/Hurtbox — HitEvent 是伤害管线的输入
- ADR-0004: Skill System — 统一 AttackData 被伤害管线消费
