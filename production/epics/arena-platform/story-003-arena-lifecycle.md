# Story 003: 场地生命周期 — 加载/卸载状态机

> **Epic**: 场地/平台系统
> **Status**: Ready
> **Layer**: Foundation
> **Type**: Logic
> **Estimate**: M
> **Manifest Version**: 2026-05-24
> **Last Updated**: 2026-05-24

## Context

**GDD**: `design/gdd/arena-platform-system.md`
**Requirement**: TR-ARE-018 ~ TR-ARE-022 (场地生命周期相关)
**ADR Governing Implementation**: ADR-0011: Arena Platform Architecture — ArenaManager MonoBehaviour 生命周期管理
**ADR Decision Summary**: ArenaManager 持有 ArenaConfig 引用, 管理 Unloaded->Loading->Active->Unloading->Error 状态机, 平台实例化和销毁, Blast Zone 缓存。
**Engine**: Unity 2022.3.51 LTS | **Risk**: LOW
**Engine Notes**: 无特殊引擎注意事项。

**Control Manifest Rules (Foundation)**:
- Required: IArenaDataProvider 接口为唯一查询入口
- Required: Active 状态下运行时只读, 拒绝修改请求
- Guardrail: LoadArena 完成 < 2ms, UnloadArena 完成 < 1 帧

---

## Acceptance Criteria

- [ ] ArenaState 状态机: Unloaded -> Loading -> Active -> Unloading -> Unloaded
- [ ] Error 状态: 验证失败或资源创建失败 -> Error (非 Active)
- [ ] LoadArena(arenaId): 验证配置 -> 实例化平台 -> 缓存数据 -> Active
- [ ] UnloadArena(): 销毁所有平台实例 -> 清理缓存 -> Unloaded
- [ ] 验证失败: 不进入 Active, 通知调用方加载失败
- [ ] Active 状态只读: 修改请求被忽略, 数据不变
- [ ] Active 状态调用 LoadArena: 拒绝, 返回错误
- [ ] Error 状态调用 UnloadArena: 转为 Unloaded
- [ ] 平台实例管理: List<GameObject> 追踪, UnloadArena 时遍历 Destroy
- [ ] Blast Zone 和 CameraBounds 缓存: Active 后直接返回缓存值, 无查找开销

---

## Implementation Notes

**来自 ADR-0011 的具体指导**:

1. ArenaManager 是 MonoBehaviour, 实现 IArenaDataProvider
2. LoadArena 流程:
   - 检查当前状态 (仅 Unloaded 允许加载)
   - ValidateConfig (调用 BlastBufferFormulas + SpawnFormulas)
   - 实例化平台碰撞体 (调用 Story 002 的实例化逻辑)
   - 缓存 BlastZone 和 CameraBounds
   - 状态 -> Active

3. UnloadArena 流程:
   - 遍历 _platformInstances, Destroy 每个 GameObject
   - 清空缓存
   - 状态 -> Unloaded

4. IArenaDataProvider 当前接口 (已存在于 Foundation):
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

5. 状态查询: GetState() 返回当前 ArenaState, 非活跃状态查询返回默认值

---

## Out of Scope

- 对局管理系统调用 LoadArena/UnloadArena 的编排 (属于 match-management epic)
- 加载进度 UI
- 视觉主题切换

---

## QA Test Cases

- **AC-3 (LoadArena 成功)**:
  - Given: 有效 ArenaConfig
  - When: LoadArena(arenaId) 被调用
  - Then: Unloaded -> Loading -> Active, 所有平台碰撞体实例化就绪

- **AC-4 (UnloadArena)**:
  - Given: Active 状态的场地
  - When: UnloadArena() 被调用
  - Then: Active -> Unloading -> Unloaded, 所有平台 GameObject 已销毁

- **AC-5 (验证失败)**:
  - Given: 验证失败的 ArenaConfig (如 BlastBuffer 不足)
  - When: LoadArena() 被调用
  - Then: 状态进入 Error 而非 Active

- **AC-6 (Active 只读)**:
  - Given: Active 状态的场地
  - When: 任何系统尝试修改配置
  - Then: 修改被忽略, 数据不变

- **AC-7 (Active 调用 LoadArena)**:
  - Given: Active 状态的场地
  - When: 再次调用 LoadArena()
  - Then: 被拒绝

- **AC-8 (Error 调用 UnloadArena)**:
  - Given: Error 状态的场地
  - When: 调用 UnloadArena()
  - Then: 状态转为 Unloaded

---

## Test Evidence

**Story Type**: Logic
**Required evidence**: `tests/unit/arena/arena-lifecycle_test.cs`
**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: Story 001 (ArenaConfig SO + 验证逻辑), Story 002 (平台碰撞体实例化)
- Unlocks: Story 004 (数据查询接口), game-state-management (MatchLoading 阶段调用 LoadArena)
