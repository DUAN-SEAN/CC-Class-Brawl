# Story 004: 数据查询接口 — Blast Zone、摄像机边界、出生点

> **Epic**: 场地/平台系统
> **Status**: Ready
> **Layer**: Foundation
> **Type**: Integration
> **Estimate**: S
> **Manifest Version**: 2026-05-24
> **Last Updated**: 2026-05-24

## Context

**GDD**: `design/gdd/arena-platform-system.md`
**Requirement**: TR-ARE-023 ~ TR-ARE-025 (数据查询相关)
**ADR Governing Implementation**: ADR-0011: Arena Platform Architecture — IArenaDataProvider 缓存查询
**ADR Decision Summary**: GetBlastZone/GetCameraBounds 返回缓存 BoundsData, GetPlatforms/GetSpawnPoints 返回 SO 只读引用。查询耗时 < 0.01ms。
**Engine**: Unity 2022.3.51 LTS | **Risk**: LOW
**Engine Notes**: 无特殊引擎注意事项。

**Control Manifest Rules (Foundation)**:
- Required: IArenaDataProvider 接口为所有系统查询场地数据的唯一入口
- Guardrail: 单次查询耗时 < 0.1ms (纯数据查找, 无每帧计算)

---

## Acceptance Criteria

- [ ] GetBlastZone() 返回缓存的 BoundsData, 值与 ArenaConfig 一致
- [ ] GetCameraBounds() 返回缓存的 BoundsData, 值与 ArenaConfig 一致
- [ ] GetPlatforms() 返回 ArenaConfig.Platforms 的只读列表
- [ ] GetSpawnPoints() 返回至少 2 个 SpawnPointData (Active 状态下)
- [ ] MVP 默认场地: Spawn1 = (-3.0, 0.75) 面朝右, Spawn2 = (3.0, 0.75) 面朝左
- [ ] 非活跃状态查询返回默认值 (不抛异常)
- [ ] GetState() 返回当前 ArenaState
- [ ] 单次查询耗时 < 0.1ms (Active 状态, 4 个平台)
- [ ] 击退系统每帧查询 GetBlastZone() 零开销 (缓存返回)

---

## Implementation Notes

**来自 ADR-0011 的具体指导**:

1. Active 状态下 GetBlastZone() / GetCameraBounds() 返回 _cachedBlastZone / _cachedCameraBounds
2. GetPlatforms() / GetSpawnPoints() 返回 ArenaConfig 的只读列表引用
3. 非 Active 状态: 返回默认 BoundsData (zero) 或空列表

**IArenaDataProvider 接口 (已定义)**:
```csharp
public interface IArenaDataProvider
{
    BoundsData GetBlastZone();
    BoundsData GetCameraBounds();
    IReadOnlyList<PlatformData> GetPlatforms();
    IReadOnlyList<SpawnPointData> GetSpawnPoints();
    ArenaState GetState();
}
```

---

## Out of Scope

- KO 判定逻辑 (属于 knockback-launch epic, 仅查询 Blast Zone 数据)
- 摄像机边界钳制实现 (属于 3c-system epic Story 007, 仅查询 CameraBounds 数据)
- AI 路径规划 (属于 ai 相关 epic, 仅查询 Platforms 数据)

---

## QA Test Cases

- **AC-1 (Blast Zone 查询)**:
  - Given: Active 状态场地, BlastZone = {-15, 15, 14, -10}
  - When: 调用 GetBlastZone()
  - Then: 返回 BoundsData { Left=-15.0, Right=15.0, Top=14.0, Bottom=-10.0 }

- **AC-2 (CameraBounds 查询)**:
  - Given: Active 状态场地
  - When: 调用 GetCameraBounds()
  - Then: 返回 BoundsData { Left=-8.0, Right=8.0, Top=8.0, Bottom=-3.0 }

- **AC-3 (Blast Buffer 验证值)**:
  - Given: 摄像机边界和 Blast Zone
  - When: 计算缓冲
  - Then: 水平缓冲 = 7.0u >= MinBlastBufferX(7.0), 垂直缓冲 >= MinBlastBufferY(6.0)

- **AC-4 (出生点查询)**:
  - Given: Active 状态 MVP 默认场地
  - When: 调用 GetSpawnPoints()
  - Then: 返回 2 个 SpawnPointData, Spawn1 = (-3.0, 0.75) Right, Spawn2 = (3.0, 0.75) Left

- **AC-6 (非活跃状态查询)**:
  - Given: Unloaded 状态
  - When: 调用 GetBlastZone()
  - Then: 返回默认 BoundsData (不抛异常)

- **AC-7 (性能)**:
  - Given: Active 状态, 4 个平台
  - When: 测量单次 GetBlastZone() 查询
  - Then: 耗时 < 0.1ms

---

## Test Evidence

**Story Type**: Integration
**Required evidence**: `tests/integration/arena/data-provider_test.cs`
**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: Story 003 (ArenaManager 提供 Active 状态的缓存数据)
- Unlocks: knockback-launch epic (Blast Zone KO 判定), 3c-system Story 007 (摄像机边界), match-management (出生点)
