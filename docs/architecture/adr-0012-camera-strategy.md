# ADR-0012: Camera Strategy — Custom Orthographic Camera with Bounding Box Tracking

## Status
Accepted

## Date
2026-05-24

## Engine Compatibility

| Field | Value |
|-------|-------|
| **Engine** | Unity 2022.3.51 LTS |
| **Domain** | Rendering / Camera |
| **Knowledge Risk** | LOW — version is within LLM training data |
| **References Consulted** | `docs/engine-reference/unity/VERSION.md` |
| **Post-Cutoff APIs Used** | None |
| **Verification Required** | Verify Camera.orthographicSize Lerp smoothness at 60Hz FixedUpdate; verify LateUpdate vs FixedUpdate timing for camera position with physics-driven characters |

## ADR Dependencies

| Field | Value |
|-------|-------|
| **Depends On** | ADR-0001 (Physics Timestep — 60Hz FixedTimestep, character Rigidbody2D positions), ADR-0011 (Arena Platform — IArenaDataProvider.GetCameraBounds()) |
| **Enables** | 3C Camera system, KO zoom effect, off-screen direction arrows |
| **Blocks** | 3C camera implementation, off-screen indicators, KO visual feedback |
| **Ordering Note** | Core layer. Depends on Foundation (physics + arena). Can be implemented alongside ADR-0002 (Dual FSM). |

## Context

### Problem Statement
2D 平台格斗游戏需要摄像机动态追踪所有活跃角色的位置，根据角色间距离自动调整缩放（Orthographic Size），同时保证摄像机永远不显示场地边界之外的区域。摄像机需要支持 2-4 人对战，提供平滑插值避免画面跳跃，并为 KO 特效提供缩放接口。

### Constraints
- 2D 正交摄像机（不是透视），使用 `Camera.orthographicSize`
- MVP 2 人对战，架构支持 4 人
- 角色位置由 Rigidbody2D 驱动（60Hz FixedUpdate）
- 场地边界数据由 IArenaDataProvider.GetCameraBounds() 提供（加载时缓存）
- 3C 系统（输入 + 移动 + 摄像机）帧预算 < 2ms
- 摄像机缩放公式已在 GDD 中精确定义，不需要设计决策

### Requirements
- 摄像机中心 = 所有活跃角色位置的包围盒中心
- 动态 Orthographic Size：根据角色水平/垂直展开距离计算
- Size 限制在 [MinCamSize, MaxCamSize] 范围内
- 位置和缩放平滑插值（Lerp）
- 场地边界限制：摄像机视锥边缘不超出 CameraBounds
- KO 缩放效果接口
- 单角色/全角色重合/超出范围等边缘情况

## Decision

采用 **自定义 CameraController MonoBehaviour + LateUpdate + 包围盒计算 + 边界限制** 架构，不使用 Cinemachine。

### 1. 不使用 Cinemachine 的理由

- GDD 的缩放公式精确到变量名（`RequiredHalfWidth`、`RequiredHalfHeight`、`TargetOrthoSize`），是纯数学计算
- Cinemachine 的 Framing Transposer 需要大量配置才能匹配 GDD 公式，且调试困难
- 自定义脚本约 150 行，完全可控，可单元测试核心公式
- 2D 正交 + 2-4 角色 + 无跟踪点 = 简单到不值得引入 Cinemachine 依赖

### 2. CameraController MonoBehaviour

```csharp
public class CameraController : MonoBehaviour
{
    [SerializeField] private Camera _camera;
    [SerializeField] private CameraTuningData _tuning;

    private BoundsData _arenaBounds;
    private List<Transform> _playerTransforms = new List<Transform>(4);

    // 缓存计算结果
    private Vector3 _targetPosition;
    private float _targetOrthoSize;
    private float _currentOrthoSize;

    public void Initialize(BoundsData arenaBounds, List<Transform> playerTransforms)
    {
        _arenaBounds = arenaBounds;
        _playerTransforms = playerTransforms;
        _currentOrthoSize = _tuning.MinCamSize;
    }

    // 由 CharacterController 协调器在 FixedUpdate 末尾调用
    // 或使用 LateUpdate（见下文决策）
    public void UpdateCamera()
    {
        if (_playerTransforms.Count == 0) return;

        // 1. 计算包围盒
        var bbox = ComputeBoundingBox(_playerTransforms);

        // 2. 计算目标 OrthoSize（CameraFormulas — 可单元测试）
        _targetOrthoSize = CameraFormulas.ComputeTargetOrthoSize(
            bbox, _tuning, _camera.aspect);

        // 3. 平滑插值
        _currentOrthoSize = CameraFormulas.SmoothDamp(
            _currentOrthoSize, _targetOrthoSize,
            _tuning.CameraSmoothSpeed, Time.fixedDeltaTime);
        _currentOrthoSize = Mathf.Clamp(
            _currentOrthoSize, _tuning.MinCamSize, _tuning.MaxCamSize);
        _camera.orthographicSize = _currentOrthoSize;

        // 4. 计算目标位置
        _targetPosition = new Vector3(bbox.center.x, bbox.center.y, _camera.transform.position.z);

        // 5. 位置插值
        var pos = Vector3.Lerp(
            _camera.transform.position, _targetPosition,
            _tuning.CameraSmoothSpeed * Time.fixedDeltaTime);

        // 6. 场地边界限制
        pos = CameraFormulas.ClampToArenaBounds(pos, _currentOrthoSize, _camera.aspect, _arenaBounds);

        _camera.transform.position = pos;
    }
}
```

### 3. LateUpdate vs FixedUpdate

**决策：使用 LateUpdate。**

理由：
- 角色位置由 Rigidbody2D 在 FixedUpdate 中更新
- Unity 的 `autoSyncTransforms = true`（ADR-0003 设置）确保 Transform 在 FixedUpdate 后立即同步
- LateUpdate 在所有 FixedUpdate 和 Update 完成后执行，保证读取到最新的角色位置
- 摄像机不是物理对象，不需要物理帧同步
- 如果使用 FixedUpdate，可能在物理子步中多次调用，浪费计算

**替代方案（显式调度）**：在 CharacterController 协调器的 FixedUpdate 末尾调用 `cameraController.UpdateCamera()`。优点是执行顺序在代码中显式可见。缺点是依赖 Rigidbody2D 的 `autoSyncTransforms` 在同一帧内同步——已确认开启，此方案可行。

**最终选择 LateUpdate**——标准 Unity 摄像机模式，不依赖 autoSyncTransforms 的帧内行为，更安全。

### 4. 摄像机公式（CameraFormulas — 纯静态类，可单元测试）

```csharp
public static class CameraFormulas
{
    public static float ComputeTargetOrthoSize(
        Bounds playerBBox, CameraTuningData tuning, float aspectRatio)
    {
        float playerSpreadX = playerBBox.max.x - playerBBox.min.x;
        float playerSpreadY = playerBBox.max.y - playerBBox.min.y;

        float requiredHalfWidth = (playerSpreadX * 0.5f + tuning.CamPaddingX) / aspectRatio;
        float requiredHalfHeight = playerSpreadY * 0.5f + tuning.CamPaddingY;

        float targetOrthoSize = Mathf.Max(requiredHalfHeight, requiredHalfWidth);
        return Mathf.Clamp(targetOrthoSize, tuning.MinCamSize, tuning.MaxCamSize);
    }

    // SmoothDamp 比 Lerp 更适合摄像机——不依赖帧率
    public static float SmoothDamp(
        float current, float target, float smoothSpeed, float deltaTime)
    {
        return Mathf.Lerp(current, target, 1f - Mathf.Exp(-smoothSpeed * deltaTime));
    }

    // 场地边界限制：确保摄像机视锥不超出 arenaBounds
    public static Vector3 ClampToArenaBounds(
        Vector3 cameraPos, float orthoSize, float aspectRatio, BoundsData arena)
    {
        float halfHeight = orthoSize;
        float halfWidth = orthoSize * aspectRatio;

        float minX = arena.Left + halfWidth;
        float maxX = arena.Right - halfWidth;
        float minY = arena.Bottom + halfHeight;
        float maxY = arena.Top - halfHeight;

        // 如果场地小于摄像机视锥，居中显示
        if (minX > maxX) { minX = maxX = (arena.Left + arena.Right) * 0.5f; }
        if (minY > maxY) { minY = maxY = (arena.Top + arena.Bottom) * 0.5f; }

        return new Vector3(
            Mathf.Clamp(cameraPos.x, minX, maxX),
            Mathf.Clamp(cameraPos.y, minY, maxY),
            cameraPos.z);
    }
}
```

### 5. 包围盒计算（N 角色）

```csharp
private Bounds ComputeBoundingBox(List<Transform> players)
{
    if (players.Count == 0) return new Bounds(Vector3.zero, Vector3.zero);

    var first = players[0].position;
    var bbox = new Bounds(first, Vector3.zero);
    for (int i = 1; i < players.Count; i++)
    {
        bbox.Encapsulate(players[i].position);
    }
    return bbox;
}
```

N 角色通用——MVP 2 人和未来 4 人使用同一逻辑。

### 6. 边缘情况处理

| 场景 | 处理 |
|------|------|
| 所有角色在同一位置 | OrthoSize = MinCamSize，中心 = 该点 |
| 角色展开超出 MaxCamSize | 摄像机停在 MaxCamSize，超出角色显示方向箭头（Battle HUD 负责） |
| 单角色存活 | OrthoSize = MinCamSize，跟随存活角色 |
| 无角色（加载阶段） | 摄像机保持在场地中心，OrthoSize = MinCamSize |

### 7. KO 缩放效果

KO 缩放由 MatchManager 通过接口触发：

```csharp
public interface ICameraEffectProvider
{
    void TriggerKOZoom(float intensity, float duration);
}
```

实现：在 `TriggerKOZoom` 中覆盖 `_currentOrthoSize` 一个缩放量（如 -0.5），然后让 SmoothDamp 自然恢复。这不是独立系统——只是 CameraController 的一个状态叠加。

### 8. CameraTuningData

```csharp
[CreateAssetMenu(fileName = "CameraTuningData", menuName = "ClassBrawl/CameraTuningData")]
public class CameraTuningData : ScriptableObject
{
    public float MinCamSize = 4.2f;        // 安全范围 3.0-6.0
    public float MaxCamSize = 8.0f;        // 安全范围 6.0-12.0
    public float CamPaddingX = 3.0f;       // 安全范围 1.0-5.0
    public float CamPaddingY = 2.0f;       // 安全范围 1.0-4.0
    public float CameraSmoothSpeed = 5.0f;  // 安全范围 2.0-15.0
}
```

### Architecture Diagram

```
┌─ ArenaConfig SO (Inspector editable) ────────────────┐
│  CameraBounds: BoundsData { left, right, top, bottom }│
└──────────────────────────────────────────────────────┘
                         ↓ LoadArena → _arenaBounds cache
┌─ CameraController (MonoBehaviour, GameScene) ────────┐
│                                                       │
│  LateUpdate:                                          │
│    1. ComputeBoundingBox(playerTransforms)             │
│    2. CameraFormulas.ComputeTargetOrthoSize(...)       │
│    3. SmoothDamp(current → target)                    │
│    4. Clamp to arena bounds                           │
│    5. Apply to Camera.orthographicSize + transform     │
│                                                       │
│  ICameraEffectProvider:                               │
│    TriggerKOZoom(intensity, duration) → 状态叠加       │
│                                                       │
│  Data In:                                             │
│    - playerTransforms[] from CharacterControllers      │
│    - arenaBounds from IArenaDataProvider              │
│    - tuning from CameraTuningData SO                  │
│                                                       │
└──────────────────────────────────────────────────────┘
         ↓                ↓
    Battle HUD        KO Effect
  (direction arrows)  (zoom + recover)
```

### Key Interfaces

```csharp
public interface ICameraDataProvider
{
    Vector3 GetCameraPosition();
    float GetOrthographicSize();
    float GetHalfWidth();
    float GetHalfHeight();
}

public interface ICameraEffectProvider
{
    void TriggerKOZoom(float intensity, float duration);
}
```

`ICameraDataProvider` 供 Battle HUD 查询——判断角色是否在屏幕外以显示方向箭头。

## Alternatives Considered

### Alternative 1: Cinemachine 2D Camera
- **Description**: 使用 Cinemachine Virtual Camera + Framing Transposer
- **Pros**: 内置_dead zone、damping、lookahead；编辑器可视化调试
- **Cons**: Cinemachine 的缩放逻辑不直接对应 GDD 公式——需要大量配置和可能的自定义扩展；引入额外包依赖；调试时无法直接看到公式变量值；对于 2-4 角色包围盒场景，自定义脚本更简洁
- **Rejection Reason**: GDD 公式精确且简单（~20 行核心逻辑），Cinemachine 的配置复杂度超过了它带来的便利

### Alternative 2: FixedUpdate 驱动摄像机
- **Description**: 在 CharacterController 协调器的 FixedUpdate 末尾调用摄像机更新
- **Cons**: 依赖 `autoSyncTransforms = true` 的帧内同步行为（虽然已开启）；非标准 Unity 摄像机模式
- **Rejection Reason**: LateUpdate 是 Unity 摄像机的标准驱动点，更安全且不依赖物理同步细节

### Alternative 3: 帧率无关的 SmoothDamp 替换为固定步长 Lerp
- **Description**: 使用 `Mathf.Lerp(current, target, speed * fixedDeltaTime)` 而非指数衰减
- **Cons**: Lerp 在固定步长下是帧率相关的（虽然 FixedUpdate 帧率固定）；指数衰减的数学特性更适合"趋近目标"的摄像机运动
- **Rejection Reason**: `1 - e^(-speed * dt)` 是帧率无关的平滑方法，即使未来改为 Update 驱动也能保持一致行为

## Consequences

### Positive
- 自定义脚本完全对应 GDD 公式，无抽象层阻碍调试
- CameraFormulas 是纯静态方法，可完整单元测试
- CameraTuningData SO 数据驱动，设计师可独立调参
- LateUpdate 标准模式，不依赖物理同步细节
- N 角色通用，2→4 人扩展无需改代码

### Negative
- 不使用 Cinemachine 意味着没有编辑器可视化调试工具（Gizmos 可部分补偿）
- KO 缩放效果是简单的叠加状态，不支持复杂的多阶段摄像机动画

### Risks
- **场地小于最小摄像机视锥**: 如果 arenaBounds 的宽度/高度 < MinCamSize 对应的视锥宽度，ClampToArenaBounds 会居中但显示场外空白 → 缓解: MinCamSize 默认 4.2，视锥宽度 ≈ 4.2 × 16/9 ≈ 7.47u，默认场地宽度 16u，远大于此
- **LateUpdate 时序**: 如果有其他 LateUpdate 脚本修改角色位置，摄像机可能读取到不一致状态 → 缓解: 项目中只有 CameraController 使用 LateUpdate，角色位置在 FixedUpdate 中确定
- **SmoothDamp 跨帧抖动**: 如果 FixedUpdate 和 Update 帧率不一致（物理子步），摄像机可能微抖 → 缓解: Unity 2022.3 默认 physics sync mode 同步到 FixedUpdate，60Hz 匹配不会出现此问题

## GDD Requirements Addressed

| GDD System | Requirement | How This ADR Addresses It |
|------------|-------------|--------------------------|
| 3c-system.md | "Camera continuously tracks all active player positions; camera center = bounding box center" | ComputeBoundingBox + LateUpdate |
| 3c-system.md | "Dynamic Orthographic Size formula: RequiredHalfWidth, RequiredHalfHeight, TargetOrthoSize" | CameraFormulas.ComputeTargetOrthoSize |
| 3c-system.md | "MinCamSize = 4.2u, MaxCamSize = 8.0u" | CameraTuningData SO + Mathf.Clamp |
| 3c-system.md | "Camera must not display area beyond arena boundaries" | CameraFormulas.ClampToArenaBounds |
| 3c-system.md | "CameraSmoothSpeed = 5.0, Lerp smooth interpolation" | SmoothDamp (指数衰减，帧率无关) |
| 3c-system.md | "Edge case: all players co-located → OrthoSize = MinCamSize" | Edge case handling in ComputeTargetOrthoSize |
| 3c-system.md | "Edge case: players exceed MaxCamSize → camera stays at MaxCamSize, direction arrows" | Clamp + ICameraDataProvider for HUD |
| 3c-system.md | "Edge case: single player remaining → MinCamSize, follow" | Edge case handling |
| knockback-launch-system.md | "KO: camera micro-zoom (zoom in then recover, ~0.3s)" | ICameraEffectProvider.TriggerKOZoom |
| arena-platform-system.md | "IArenaDataProvider.GetCameraBounds() provides bounds data" | Initialize with cached BoundsData |
| battle-hud.md | "Off-screen direction arrows need camera position/size" | ICameraDataProvider interface |

## Performance Implications
- **CPU**: ComputeBoundingBox (2-4 transforms) < 0.005ms; CameraFormulas (4 float ops + Clamp) < 0.005ms; total LateUpdate < 0.02ms
- **Memory**: CameraController ~200B; CameraTuningData SO ~100B; playerTransforms list (4 refs) ~32B; total < 500B
- **Load Time**: Initialize < 0.1ms (cache bounds + store references)
- **Network**: 不适用

## Migration Plan
不适用——新项目初始架构决策。

## Validation Criteria
- [ ] 2 角色在场地上，摄像机中心 = 两人位置中点
- [ ] 角色水平展开时，摄像机平滑放大；靠近时平滑缩小
- [ ] Orthographic Size 始终在 [4.2, 8.0] 范围内
- [ ] 摄像机视锥边缘不超出场地 CameraBounds
- [ ] SmoothDamp 过渡平滑无跳跃（视觉验证）
- [ ] 2 角色重合 → OrthoSize = MinCamSize
- [ ] 1 角色存活 → OrthoSize = MinCamSize，跟随该角色
- [ ] TriggerKOZoom → 摄像机缩放后自然恢复，总时长 ~0.3s
- [ ] CameraFormulas.ComputeTargetOrthoSize 单元测试通过（边界值、典型值）
- [ ] CameraFormulas.ClampToArenaBounds 单元测试通过（正常/场地过小情况）
- [ ] CameraController.LateUpdate 帧耗时 < 0.02ms

## Related Decisions
- ADR-0001: Physics Timestep — 角色位置在 60Hz FixedUpdate 中更新
- ADR-0011: Arena Platform — IArenaDataProvider.GetCameraBounds() 提供场地边界
- ADR-0003: Hitbox/Hurtbox Detection — autoSyncTransforms = true 保证 Transform 同步
- ADR-0014: UI Architecture — Battle HUD 通过 ICameraDataProvider 查询摄像机数据
