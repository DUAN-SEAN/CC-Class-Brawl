# Story 002: 平台碰撞体实例化 — Solid + PassThrough 配置

> **Epic**: 场地/平台系统
> **Status**: Ready
> **Layer**: Foundation
> **Type**: Integration
> **Estimate**: M
> **Manifest Version**: 2026-05-24
> **Last Updated**: 2026-05-24

## Context

**GDD**: `design/gdd/arena-platform-system.md`
**Requirement**: TR-ARE-013 ~ TR-ARE-017 (平台碰撞相关)
**ADR Governing Implementation**: ADR-0011: Arena Platform Architecture — PlatformEffector2D one-way 碰撞
**ADR Decision Summary**: Solid 平台用 BoxCollider2D 四面碰撞; PassThrough 平台用 BoxCollider2D + PlatformEffector2D (surfaceArc=180, useOneWay=true) 实现顶面碰撞。
**Engine**: Unity 2022.3.51 LTS | **Risk**: LOW
**Engine Notes**: PlatformEffector2D 与 gravityScale=0 的交互需验证 (手动重力可能影响 one-way 碰撞行为)。

**Control Manifest Rules (Foundation)**:
- Required: SolidPlatform Layer (11) 碰撞矩阵配置
- Required: Physics2D.autoSyncTransforms = true
- Guardrail: 平台实例化 < 1ms (4 个碰撞体)

---

## Acceptance Criteria

- [ ] Solid 平台: BoxCollider2D 四面阻挡角色通过
- [ ] PassThrough 平台: 角色从上方着陆正常
- [ ] PassThrough 平台: 角色从下方或侧面穿过无碰撞
- [ ] PlatformEffector2D 配置: surfaceArc=180, useOneWay=true, collider.usedByEffector=true
- [ ] 两个角色站在同一穿越平台上各自独立检测碰撞, 互不干扰
- [ ] 碰撞层正确: SolidPlatform 层与角色层有碰撞, Hitbox 与 SolidPlatform 有碰撞
- [ ] 平台实例化: 从 ArenaConfig.Platforms 列表创建 GameObject + 碰撞体
- [ ] LoadArena 完成 < 1 帧 (< 16.6ms, 4 个平台)

---

## Implementation Notes

**来自 ADR-0011 的具体指导**:

1. Solid 平台实例化:
```csharp
var go = new GameObject($"Platform_{i}_{data.Type}");
var collider = go.AddComponent<BoxCollider2D>();
collider.size = new Vector2(data.Width, data.Height);
// 放在 "SolidPlatform" 层 (Layer 11)
```

2. PassThrough 平台实例化:
```csharp
var go = new GameObject($"Platform_{i}_{data.Type}");
var collider = go.AddComponent<BoxCollider2D>();
collider.size = new Vector2(data.Width, data.Height);
var effector = go.AddComponent<PlatformEffector2D>();
effector.surfaceArc = 180f;
effector.useOneWay = true;
collider.usedByEffector = true;
```

3. **风险项**: PlatformEffector2D + gravityScale=0 的交互需专项测试。手动重力模式可能导致 one-way 碰撞行为异常。如果 PlatformEffector2D 行为不正确, 回退方案: 3C 系统在检测到角色在穿越平台下方时临时禁用该碰撞体。

**PlatformType enum 需确认**: 当前 PlatformData 中 Type 字段的类型需要定义为 `PlatformType { Solid, PassThrough }` enum。

---

## Out of Scope

- 角色着陆逻辑 (属于 3c-system epic Story 006)
- 穿越平台的下键+跳跃键操作 (属于 3c-system epic Story 006)
- 视觉主题渲染

---

## QA Test Cases

- **AC-1 (Solid 四面碰撞)**:
  - Given: 实心平台位于 (0,0) 宽 12.0u 高 0.5u
  - When: 角色从上/下/左/右接触平台
  - Then: BoxCollider2D 四面阻挡

- **AC-2 (PassThrough 顶面碰撞)**:
  - Given: 穿越平台配有 PlatformEffector2D (surfaceArc=180, useOneWay=true)
  - When: 角色从上方下落到平台
  - Then: 正常着陆在平台顶面

- **AC-3 (PassThrough 穿透)**:
  - Given: 穿越平台
  - When: 角色从下方或侧面穿过
  - Then: 无碰撞, 角色自由通过

- **AC-5 (两人同平台)**:
  - Given: 两个角色站在同一穿越平台上
  - Then: 各自独立检测碰撞, 互不干扰

---

## Test Evidence

**Story Type**: Integration (依赖 Unity 物理系统)
**Required evidence**: `tests/integration/arena/platform-collision_test.cs`
**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: Story 001 (ArenaConfig SO 提供 PlatformData 列表)
- Unlocks: Story 003 (ArenaManager 加载时调用实例化), Story 004 (数据查询)
