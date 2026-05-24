# Story 006: 平台交互 — 穿越平台与着陆延迟

> **Epic**: 3C系统
> **Status**: Ready
> **Layer**: Foundation
> **Type**: Integration
> **Estimate**: M
> **Manifest Version**: 2026-05-24
> **Last Updated**: 2026-05-24

## Context

**GDD**: `design/gdd/3c-system.md`
**Requirement**: TR-MOV-039 ~ TR-MOV-044 (平台交互相关)
**ADR Governing Implementation**: ADR-0001: Physics Timestep — 60Hz 物理步 + 碰撞检测
**ADR Decision Summary**: 平台碰撞通过 Unity 物理系统自动检测, OnCollisionEnter2D 触发着陆判定。
**Engine**: Unity 2022.3.51 LTS | **Risk**: LOW
**Engine Notes**: PassThrough 平台使用 PlatformEffector2D (由 arena-platform epic 设置), 3C 系统通过物理回调检测着陆。

**Control Manifest Rules (Foundation)**:
- Required: Physics2D.autoSyncTransforms = true
- Required: 碰撞回调驱动状态转换
- Guardrail: 平台碰撞检测不增加每帧开销 (物理回调模式)

---

## Acceptance Criteria

- [ ] 穿越平台: 按住下键 + 按下跳跃键, 角色向下穿越当前平台
- [ ] 穿越输入窗口: 下键必须在跳跃键之前或同一帧按下, PlatformDropInputWindow=3帧
- [ ] 着陆检测: 从空中接触平台顶部时, 进入 Landing 状态
- [ ] 着陆延迟: LandingLagFrames=3帧, 期间不可跳跃/冲刺 (输入被忽略)
- [ ] 着陆延迟结束: 转入 Idle (无方向输入) 或 Running (有方向输入)
- [ ] PassThrough 平台: 仅从顶面碰撞, 角色可从下方跳穿
- [ ] Solid 平台: 四面碰撞, 角色无法穿越
- [ ] 空中跳次数在着地后重置

---

## Implementation Notes

**平台交互实现策略**:

1. 着陆检测: 使用 OnCollisionEnter2D / OnCollisionStay2D 检测与平台碰撞体的接触
2. 地面检测: 使用 OnCollisionStay2D 持续确认角色站在平台上
3. 穿越平台实现:
   - 检测到下键 + 跳跃键组合输入
   - 临时禁用角色与 PassThrough 平台的碰撞
   - PlatformDrop 状态持续到角色完全穿越平台
   - 恢复碰撞

4. PlatformEffector2D 交互: PassThrough 平台已由 arena-platform epic 配置好 PlatformEffector2D, 3C 系统无需额外处理 one-way 逻辑

**状态转换 (来自 GDD 状态表)**:
- Falling/Jumping + 着地 -> Landing
- Landing + 着陆延迟结束 + 方向输入 -> Running
- Landing + 着陆延迟结束 + 无输入 -> Idle
- Falling + 下键+跳跃(在平台上) -> PlatformDrop
- PlatformDrop + 穿越完成 -> Falling

**默认参数值**:
- LandingLagFrames = 3 帧
- PlatformDropInputWindow = 3 帧

---

## Out of Scope

- 平台碰撞体的创建和配置 (属于 arena-platform epic)
- PlatformEffector2D 配置 (属于 arena-platform epic)
- 着陆视觉反馈 (squash 动画) (属于 Visual/Feel 层)

---

## QA Test Cases

- **AC-1 (穿越平台)**:
  - Given: 角色站在 PassThrough 平台上
  - When: 同时按下下方向 + 跳跃键 (下键先于或同一帧)
  - Then: 角色向下穿越平台

- **AC-2 (穿越输入窗口)**:
  - Given: 角色站在 PassThrough 平台上
  - When: 先按跳跃键, 3 帧后才按住下键
  - Then: 不触发穿越 (下键必须在跳跃键之前或同一帧)

- **AC-3 (着陆检测)**:
  - Given: 角色从空中下落
  - When: 接触平台顶部
  - Then: 进入 Landing 状态

- **AC-4 (着陆延迟)**:
  - Given: 角色刚着陆 (Landing 状态)
  - When: 在 LandingLagFrames (3帧) 内按跳跃键
  - Then: 忽略跳跃输入, 不可跳跃

- **AC-5 (着陆结束)**:
  - Given: 着陆延迟结束
  - When: 无方向输入
  - Then: 转入 Idle; 有方向输入 -> Running

- **AC-7 (空中跳重置)**:
  - Given: 角色空中跳已用完
  - When: 着地
  - Then: 空中跳次数重置为 MaxAirJumps

---

## Test Evidence

**Story Type**: Integration (依赖物理碰撞)
**Required evidence**: `tests/integration/movement/platform-interaction_test.cs`
**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: Story 002 (地面移动), Story 003 (跳跃 — 空中跳次数重置), arena-platform epic (平台碰撞体)
- Unlocks: None
