# Story 001: ArenaConfig SO 数据结构与验证逻辑

> **Epic**: 场地/平台系统
> **Status**: Ready
> **Layer**: Foundation
> **Type**: Logic
> **Estimate**: M
> **Manifest Version**: 2026-05-24
> **Last Updated**: 2026-05-24

## Context

**GDD**: `design/gdd/arena-platform-system.md`
**Requirement**: TR-ARE-001 ~ TR-ARE-012 (数据结构与验证相关)
**ADR Governing Implementation**: ADR-0011: Arena Platform Architecture — ArenaConfig SO + PlatformEffector2D + MonoBehaviour Lifecycle
**ADR Decision Summary**: ArenaConfig ScriptableObject 定义场地数据, 加载时执行 BlastBuffer/出生点验证, 验证公式为纯静态方法。
**Engine**: Unity 2022.3.51 LTS | **Risk**: LOW
**Engine Notes**: ArenaConfig SO 使用 CreateAssetMenu, OnValidate 编辑器数据完整性检查。

**Control Manifest Rules (Foundation)**:
- Required: ArenaConfig 遵循 SO 数据驱动模式
- Required: SO 运行时只读, 运行时状态由 ArenaManager 持有
- Required: 每个 SO 实现 OnValidate() 编辑器数据完整性检查
- Forbidden: JSON/CSV 外部数据文件, 硬编码 C# 常量类

---

## Acceptance Criteria

- [ ] ArenaConfig SO 定义: arenaId, arenaName, platforms[], blastZone, cameraBounds, spawnPoints[], themeId
- [ ] ArenaConfig 包含验证旋钮: MinBlastBufferX=7.0, MinBlastBufferY=6.0, MinSpawnDistance=4.0, SpawnHeightOffset=0.5
- [ ] BlastBufferFormulas 纯静态类, IsValid 验证摄像机边界与 Blast Zone 的缓冲距离
- [ ] SpawnFormulas 纯静态类: IsValidDistance (出生点最小距离), IsOnPlatform (出生点在平台上方)
- [ ] Blast Buffer 验证公式: CamBound-Blast 距离 >= MinBlastBuffer, 所有四个方向
- [ ] 出生点距离验证: 欧几里得距离 >= MinSpawnDistance
- [ ] 出生点平台验证: 出生点 X 在平台宽度内, Y 在平台顶面 + SpawnHeightOffset 范围内
- [ ] 出生点数量验证: 至少 2 个出生点
- [ ] ArenaConfig.OnValidate() 在 Inspector 编辑时触发数据完整性检查
- [ ] MVP 默认战场型配置: 4 平台 + 正确边界 + 2 出生点
- [ ] 所有验证公式可独立单元测试 (无 Unity 运行时依赖)

---

## Implementation Notes

**来自 ADR-0011 的具体指导**:

1. ArenaConfig SO 数据结构 (已有部分在 Foundation/Data/ 中):
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
    public float MinBlastBufferX = 7.0f;
    public float MinBlastBufferY = 6.0f;
    public float MinSpawnDistance = 4.0f;
    public float SpawnHeightOffset = 0.5f;
}
```

2. 数据结构已存在于 Foundation 层:
   - `BoundsData { Left, Right, Top, Bottom }` (已有)
   - `PlatformData { Position, Width, Height, Type }` (已有, 需确认 PlatformType enum)
   - `SpawnPointData { Position, FacingDirection }` (已有)
   - `ArenaState { Unloaded, Loading, Active, Unloading, Error }` (已有)

3. BlastBufferFormulas 和 SpawnFormulas 为纯静态类, 零 Unity 依赖, 100% 可测试

**MVP 默认战场型配置** (来自 GDD):
- 主舞台: Solid (0,0) 12.0x0.5
- 左平台: PassThrough (-3.5, 2.8) 5.0x0.15
- 中央平台: PassThrough (0, 5.0) 5.0x0.15
- 右平台: PassThrough (3.5, 2.8) 5.0x0.15
- BlastZone: {-15, 15, 14, -10}
- CameraBounds: {-8, 8, 8, -3}
- Spawn1: (-3.0, 0.75) Right, Spawn2: (3.0, 0.75) Left

---

## Out of Scope

- ArenaManager MonoBehaviour 运行时加载/卸载 (Story 003)
- 平台碰撞体实例化 (Story 002)
- 视觉主题渲染

---

## QA Test Cases

- **AC-5 (Blast Buffer 验证)**:
  - Given: CamBoundLeft=-8.0, BlastLeft=-14.0 (缓冲=6.0 < 7.0)
  - When: 执行 BlastBufferFormulas.IsValid
  - Then: 返回 false (验证失败)

- **AC-5 (Blast Buffer 验证通过)**:
  - Given: MVP 默认配置 (CamBoundLeft=-8, BlastLeft=-15, 缓冲=7.0 >= 7.0)
  - When: 执行 IsValid
  - Then: 返回 true

- **AC-6 (出生点距离)**:
  - Given: 两个出生点距离 3.0u < MinSpawnDistance(4.0)
  - When: 执行 SpawnFormulas.IsValidDistance
  - Then: 返回 false

- **AC-6 (出生点距离通过)**:
  - Given: MVP 默认 Spawn1=(-3,0.75) Spawn2=(3,0.75), 距离=6.0u >= 4.0
  - Then: 返回 true

- **AC-7 (出生点平台验证)**:
  - Given: 出生点 (50.0, 10.0) 无附近平台
  - When: 执行 IsOnPlatform
  - Then: 返回 false

- **AC-7 (出生点平台验证通过)**:
  - Given: MVP 默认 Spawn(-3, 0.75), 主舞台顶面 0.25, 偏移 0.5
  - Then: 返回 true

- **AC-10 (MVP 配置全验证)**:
  - Given: MVP 默认战场型配置
  - When: 执行所有验证
  - Then: 全部通过

---

## Test Evidence

**Story Type**: Logic
**Required evidence**: `tests/unit/arena/arena-config-validation_test.cs`
**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: None (Foundation 层, 纯数据结构和公式)
- Unlocks: Story 002 (平台碰撞实例化), Story 003 (场地生命周期), Story 004 (数据查询)
