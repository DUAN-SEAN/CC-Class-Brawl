# ADR-0011: Arena Platform Architecture — ArenaConfig SO + PlatformEffector2D + MonoBehaviour Lifecycle

## Status
Accepted

## Date
2026-05-24

## Engine Compatibility

| Field | Value |
|-------|-------|
| **Engine** | Unity 2022.3.51 LTS |
| **Domain** | Scene / Physics (Platform) |
| **Knowledge Risk** | LOW — version is within LLM training data |
| **References Consulted** | `docs/engine-reference/unity/VERSION.md`, `docs/engine-reference/unity/deprecated-apis.md` |
| **Post-Cutoff APIs Used** | None |
| **Verification Required** | Verify PlatformEffector2D surfaceArc=180° one-way behavior with Rigidbody2D gravityScale=0 (manual gravity); verify BoxCollider2D trigger interactions with PlatformEffector2D |

## ADR Dependencies

| Field | Value |
|-------|-------|
| **Depends On** | ADR-0001 (Physics Timestep — 60Hz frame basis, Rigidbody2D settings) |
| **Enables** | ADR-0006 (Knockback Pipeline — Blast Zone KO data), ADR-0007 (Scene & Game State — MatchLoading arena init), 3C System (ground detection, camera bounds) |
| **Blocks** | Knockback KO detection (needs Blast Zone data), 3C camera boundary clamping (needs camera bounds), MatchLoading initialization |
| **Ordering Note** | Foundation layer. Can be implemented alongside ADR-0001. |

## Context

### Problem Statement
场地/平台系统管理战斗空间的物理定义——平台碰撞体、淘汰区边界、摄像机边界和出生点。系统需要：数据驱动配置（设计师在 Inspector 中调整场地参数）、两种平台类型（实心+穿越）的正确物理行为、加载时配置验证、运行时只读数据查询接口。作为 Foundation 层，它为 3C 系统（地面检测、摄像机边界）、击退系统（Blast Zone KO 判定）和对局管理系统（场地加载/卸载、出生点）提供数据基础。

### Constraints
- 2 种平台类型：Solid（四面碰撞）和 PassThrough（仅顶面碰撞）
- PassThrough 需要 PlatformEffector2D 实现单向碰撞
- 场地配置通过 ScriptableObject 定义，运行时只读
- 加载/卸载由对局管理系统在 MatchLoading 阶段触发
- 平台数量极少（MVP 4 个），性能不是瓶颈
- 场地数据在战斗中不变，可在加载时缓存

### Requirements
- ArenaConfig ScriptableObject 数据驱动
- IArenaDataProvider 接口提供统一查询入口
- 加载时验证（Blast Buffer、出生点位置、出生点距离）
- 运行时 Blast Zone 数据缓存（击退系统每帧查询）
- 平台碰撞体实例化和销毁的生命周期管理

## Decision

采用 **ArenaConfig SO + ArenaManager MonoBehaviour + PlatformEffector2D + 加载时验证 + Blast Zone 缓存** 架构：

### 1. ArenaConfig ScriptableObject

```csharp
[CreateAssetMenu(fileName = "ArenaConfig", menuName = "ClassBrawl/ArenaConfig")]
public class ArenaConfig : ScriptableObject
{
    public string ArenaId;
    public string ArenaName;
    public List<PlatformData> Platforms;
    public BoundsData BlastZone;
    public BoundsData CameraBounds;
    public List<SpawnPointData> SpawnPoints;
    public string ThemeId;

    // Tuning knobs
    public float MinBlastBufferX = 7.0f;
    public float MinBlastBufferY = 6.0f;
    public float MinSpawnDistance = 4.0f;
    public float SpawnHeightOffset = 0.5f;
}
```

### 2. Platform Data Structures

```csharp
public enum PlatformType { Solid, PassThrough }

[Serializable]
public struct PlatformData
{
    public Vector2 Position;
    public float Width;
    public float Height;
    public PlatformType Type;
}

[Serializable]
public struct BoundsData
{
    public float Left;
    public float Right;
    public float Top;
    public float Bottom;
}

[Serializable]
public struct SpawnPointData
{
    public Vector2 Position;
    public int FacingDirection; // 1 = Right, -1 = Left
}
```

### 3. Platform Instantiation

在 `LoadArena()` 中为每个 PlatformData 创建 GameObject：

**Solid 平台**：
```csharp
var go = new GameObject($"Platform_{i}_{data.Type}");
var collider = go.AddComponent<BoxCollider2D>();
collider.size = new Vector2(data.Width, data.Height);
// 放在 "Default" 或 "SolidPlatform" 层
```

**PassThrough 平台**：
```csharp
var go = new GameObject($"Platform_{i}_{data.Type}");
var collider = go.AddComponent<BoxCollider2D>();
collider.size = new Vector2(data.Width, data.Height);
var effector = go.AddComponent<PlatformEffector2D>();
effector.surfaceArc = 180f;
effector.useOneWay = true;
collider.usedByEffector = true;
```

理由：PlatformEffector2D 的 `surfaceArc=180°` + `useOneWay=true` 实现了"仅顶面碰撞"的穿越平台行为。角色从上方下落时正常着陆，从下方跳上时穿透通过。

### 4. ArenaManager MonoBehaviour

ArenaManager 持有 ArenaConfig 引用，管理平台 GameObject 的生命周期，实现 IArenaDataProvider：

```csharp
public interface IArenaDataProvider
{
    BoundsData GetBlastZone();
    BoundsData GetCameraBounds();
    IReadOnlyList<PlatformData> GetPlatforms();
    IReadOnlyList<SpawnPointData> GetSpawnPoints();
    ArenaState GetState();
    void LoadArena(string arenaId);
    void UnloadArena();
}
```

**Blast Zone 缓存**：LoadArena 成功后，缓存 `_cachedBlastZone` 和 `_cachedCameraBounds`。击退系统每帧调用 GetBlastZone() 时直接返回缓存值，无查找开销。

**平台实例管理**：LoadArena 时将所有创建的 GameObject 存入 `List<GameObject> _platformInstances`，UnloadArena 时遍历 Destroy。

### 5. 加载时验证

LoadArena 执行以下验证，任一失败则进入 Error 状态：

```csharp
bool ValidateConfig(ArenaConfig config)
{
    // 1. Blast Buffer 验证
    if (!BlastBufferFormulas.IsValid(config.CameraBounds, config.BlastZone,
        config.MinBlastBufferX, config.MinBlastBufferY))
        return false;

    // 2. 出生点数量
    if (config.SpawnPoints.Count < 2)
        return false;

    // 3. 出生点最小距离
    if (!SpawnFormulas.IsValidDistance(config.SpawnPoints, config.MinSpawnDistance))
        return false;

    // 4. 出生点在平台上方
    foreach (var spawn in config.SpawnPoints)
    {
        if (!SpawnFormulas.IsOnPlatform(spawn, config.Platforms, config.SpawnHeightOffset))
            return false;
    }

    return true;
}
```

验证公式为纯静态方法，可单元测试。

### 6. 运行时只读

ArenaManager 在 Active 状态下拒绝所有修改请求。GetBlastZone()、GetCameraBounds()、GetPlatforms()、GetSpawnPoints() 返回缓存数据或 SO 只读引用。

### Architecture Diagram

```
┌─ ArenaConfig SO (Inspector editable) ────────────────┐
│  Platforms[], BlastZone, CameraBounds, SpawnPoints[]  │
│  OnValidate() → data integrity checks                 │
└───────────────────────────────────────────────────────┘
                         ↓ LoadArena(arenaId)
┌─ ArenaManager (MonoBehaviour, GameScene) ────────────┐
│                                                       │
│  ValidateConfig() → if fail: Error state              │
│  Instantiate platforms (BoxCollider2D + Effector2D)   │
│  Cache BlastZone + CameraBounds                       │
│  State: Unloaded → Loading → Active                   │
│                                                       │
│  IArenaDataProvider queries:                          │
│    GetBlastZone()    → cached BoundsData (< 0.01ms)  │
│    GetCameraBounds() → cached BoundsData              │
│    GetPlatforms()    → SO readonly list               │
│    GetSpawnPoints()  → SO readonly list               │
│                                                       │
└───────────────────────────────────────────────────────┘
         ↓                ↓               ↓
    3C System       KnockbackSystem   MatchManager
  (ground detect,   (KO per frame     (load/unload,
   camera bounds)    blast zone)       spawn points)
```

### Key Interfaces

- `IArenaDataProvider` — 场地数据查询唯一入口
- `ArenaConfig : ScriptableObject` — 场地配置数据载体
- `PlatformData`, `BoundsData`, `SpawnPointData` — 纯数据结构

## Alternatives Considered

### Alternative 1: 场景内预放置平台（不用 SO）
- **Description**: 在 Unity Scene 中直接放置平台 GameObject，不用数据驱动
- **Pros**: 简单直接，所见即所得
- **Cons**: 无法在 Inspector 中调整场地参数；无法在运行时切换场地；无法做配置验证；设计师无法独立创建新场地
- **Rejection Reason**: 违反数据驱动原则。GDD 要求"创建新场地只需创建新 SO 资产"

### Alternative 2: 自定义单向碰撞（不用 PlatformEffector2D）
- **Description**: 手动实现单向平台碰撞逻辑（在 OnCollisionEnter2D 中检查角色速度方向）
- **Pros**: 完全控制碰撞行为
- **Cons**: 重复 Unity 已有功能；需要处理大量边缘情况（角色在平台边缘、穿透恢复等）；PlatformEffector2D 已经解决了这些问题
- **Rejection Reason**: PlatformEffector2D 是 Unity 内置的一穿越平台解决方案，在 2022.3 LTS 中稳定可靠。手动实现增加代码量和维护负担，无性能收益

## Consequences

### Positive
- ArenaConfig SO 实现数据驱动——设计师创建新场地不需要改代码
- IArenaDataProvider 接口清晰——下游系统不耦合实现细节
- 加载时验证防止无效配置进入运行时
- Blast Zone 缓存确保击退系统每帧查询零开销

### Negative
- 平台 Instantiate/Destroy 在加载/卸载时有短暂开销（但仅在 MatchLoading 阶段，不影响战斗帧率）
- PlatformEffector2D 与 gravityScale=0 的交互需要在实现时验证（手动重力可能影响 one-way collision 行为）

### Risks
- **PlatformEffector2D + gravityScale=0 交互**: 手动重力模式可能导致角色从平台下方进入时被碰撞体卡住 → 缓解: 实现时做专项测试，如果 PlatformEffector2D 行为异常，改为 3C 系统在检测到角色在穿越平台下方时临时禁用该平台碰撞体
- **场地配置被意外修改**: ArenaConfig 是共享 SO，意外修改影响所有使用该配置的对局 → 缓解: 运行时只读原则 + SO 运行时不可修改
- **多次加载**: 在 Active 状态调用 LoadArena → 缓解: ArenaManager 检查当前状态，非 Unloaded 状态拒绝加载请求

## GDD Requirements Addressed

| GDD System | Requirement | How This ADR Addresses It |
|------------|-------------|--------------------------|
| arena-platform-system.md | "ArenaConfig ScriptableObject: arenaId, platforms[], blastZone, cameraBounds, spawnPoints[], themeId" | ArenaConfig SO definition |
| arena-platform-system.md | "Two platform types: Solid (BoxCollider2D) and PassThrough (BoxCollider2D + PlatformEffector2D)" | Platform instantiation with type-specific setup |
| arena-platform-system.md | "IArenaDataProvider: GetBlastZone, GetCameraBounds, GetPlatforms, GetSpawnPoints, GetState, LoadArena, UnloadArena" | IArenaDataProvider interface |
| arena-platform-system.md | "Blast Buffer validation on load" | ValidateConfig with BlastBufferFormulas |
| arena-platform-system.md | "Spawn point distance and platform-on-top validation" | ValidateConfig with SpawnFormulas |
| arena-platform-system.md | "Platform runtime read-only" | ArenaManager Active state rejects modifications |
| arena-platform-system.md | "ArenaState FSM: Unloaded → Loading → Active → Unloading → Error" | ArenaManager state management |
| knockback-launch-system.md | "Blast Zone bounds via IArenaDataProvider.GetBlastZone()" | Cached Blast Zone data provision |
| 3c-system.md | "Camera cannot show area beyond arena boundaries" | GetCameraBounds() data provision |
| match-management-system.md | "CoordinateRoundReset: position=spawn" | GetSpawnPoints() data provision |

## Performance Implications
- **CPU**: GetBlastZone() / GetCameraBounds() = cached field return < 0.001ms; GetPlatforms() / GetSpawnPoints() = SO reference return < 0.001ms
- **Memory**: ArenaConfig SO ~200B; platform instances (4 × ~100B collider + effector) ~400B; cached BoundsData ~32B; total < 1KB
- **Load Time**: Instantiate 4 platforms < 1ms; ValidateConfig < 0.1ms; total LoadArena < 2ms
- **Network**: 不适用

## Migration Plan
不适用——新项目初始架构决策。

## Validation Criteria
- [ ] Solid platform: BoxCollider2D 四面阻挡角色通过
- [ ] PassThrough platform: 角色从上方着陆正常，从下方/侧面穿透通过
- [ ] LoadArena("battlefield") → Unloaded → Loading → Active
- [ ] Active 状态下 GetBlastZone() 返回正确的 BoundsData
- [ ] Active 状态下 GetSpawnPoints() 返回至少 2 个 SpawnPointData
- [ ] 无效配置（Blast Buffer 不足）→ 加载失败，进入 Error 状态
- [ ] 无效配置（出生点距离 < MinSpawnDistance）→ 加载失败
- [ ] 无效配置（出生点不在平台上方）→ 加载失败
- [ ] UnloadArena() → Active → Unloading → Unloaded，平台全部销毁
- [ ] Active 状态调用 LoadArena() → 被拒绝
- [ ] 单次 GetBlastZone() 查询 < 0.01ms
- [ ] LoadArena 完成 < 2ms

## Related Decisions
- ADR-0001: Physics Timestep — 场地系统的物理配置在 60Hz 物理步中生效
- ADR-0006: Damage & Knockback Pipeline — 击退系统每帧查询 Blast Zone 数据
- ADR-0007: Scene & Game State — MatchLoading 阶段触发 LoadArena
- ADR-0004: Skill System Data-Driven — ArenaConfig 遵循 SO 数据驱动模式
