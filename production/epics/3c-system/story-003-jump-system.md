# Story 003: 跳跃系统 — 地面跳、空中跳、短跳、土狼时间、跳跃缓冲

> **Epic**: 3C系统
> **Status**: Ready
> **Layer**: Foundation
> **Type**: Logic
> **Estimate**: L
> **Manifest Version**: 2026-05-24
> **Last Updated**: 2026-05-24

## Context

**GDD**: `design/gdd/3c-system.md`
**Requirement**: TR-MOV-017 ~ TR-MOV-026 (跳跃相关)
**ADR Governing Implementation**: ADR-0001: Physics Timestep — 60Hz 手动重力 + velocity 直接赋值
**ADR Decision Summary**: 手动重力 velocity.y -= Gravity * dt, 终端速度钳制, 不同状态不同重力倍率。
**Engine**: Unity 2022.3.51 LTS | **Risk**: LOW
**Engine Notes**: 重力公式需要精确匹配 GDD: JumpVelocity = sqrt(2 * Gravity * JumpHeight)。

**Control Manifest Rules (Foundation)**:
- Required: Rigidbody2D.gravityScale = 0, 手动施加重力, TerminalVelocity 钳制
- Required: 所有速度更新通过 Rigidbody2D.velocity 直接赋值
- Guardrail: 3C + collision + knockback 物理 < 3ms/frame

---

## Acceptance Criteria

- [ ] 地面跳: 按跳跃键从地面起跳, 获得向上初速度 JumpVelocity = sqrt(2 * Gravity * JumpHeight)
- [ ] 空中跳 (二段跳): 空中按跳跃键, 初速度 = AirJumpForceRatio * JumpVelocity, 次数上限 MaxAirJumps=1
- [ ] 空中跳次数着地后重置
- [ ] 短跳: 起跳后 ShortHopWindow 内松键, V_vertical 直接设为 ShortHopVelocity (速度设定模式, 非钳制)
- [ ] 短跳实现: 起跳以完整 JumpVelocity 发射, 松键时 V_vertical = ShortHopVelocity
- [ ] 跳跃高度验证: 完整跳达到 JumpHeight (3.5u +- 0.1), 顶点帧数约 28 帧 (+-2)
- [ ] 土狼时间 (Coyote Time): 离开平台边缘后 CoyoteTimeFrames (4帧) 内仍可地面跳 (不消耗空中跳)
- [ ] 跳跃输入缓冲: 着陆前 JumpInputBufferFrames (3帧) 内的跳跃键被缓冲, 着陆延迟结束后执行
- [ ] 着陆延迟期间不可跳跃 (防止弹跳式无限跳跃)
- [ ] 从平台边缘走出 (未跳跃): 正常进入 Falling, 不消耗空中跳次数

---

## Implementation Notes

**GDD 公式 — 跳跃初速度**:
```
JumpVelocity = sqrt(2 * Gravity * JumpHeight)
AirJumpVelocity = AirJumpForceRatio * JumpVelocity
ShortHopVelocity = ShortHopHeightRatio * JumpVelocity
```

**GDD 公式 — 重力**:
```
V_vertical_new = V_vertical - Gravity * FastFallMultiplier * dt
V_vertical_new = Max(V_vertical_new, -TerminalVelocity)
```

**默认参数值**:
- Gravity = 32.0 u/s^2
- JumpHeight = 3.5 u (-> JumpVelocity 约 14.97 u/s)
- AirJumpForceRatio = 0.85 (-> AirJumpVelocity 约 12.72 u/s)
- ShortHopHeightRatio = 0.45 (-> ShortHopVelocity 约 6.73 u/s)
- ShortHopWindow = 5 帧
- MaxAirJumps = 1
- CoyoteTimeFrames = 4 帧
- JumpInputBufferFrames = 3 帧

**状态转换 (来自 GDD 状态表)**:
- Idle/Running + 跳跃键(在地面) -> Jumping
- Jumping + V_vertical <= 0 (顶点) -> Falling
- Jumping + 跳跃键(有剩余空中跳) -> Jumping (重置为空中跳)
- Falling + 着地 -> Landing
- Falling + 跳跃键(有剩余空中跳) -> Jumping

**土狼时间实现**: 记录最后离开地面的帧号, 在 CoyoteTimeFrames 内按跳跃键视为地面跳。

**跳跃缓冲实现**: InputReader 检测跳跃输入时记录帧号, 着陆延迟结束时检查缓冲窗口内是否有未消费的跳跃输入。

---

## Out of Scope

- 快速下落 (Story 004)
- 平台穿越 (Story 006)
- 着陆延迟的完整实现需要平台检测 (Story 006 提供平台碰撞)
- 攻击期间的跳跃冻结 (由 CombatFSM 通过 FreezeMovement 控制)

---

## QA Test Cases

- **AC-1 (地面跳初速度)**:
  - Given: 角色站在地面上
  - When: 按下跳跃键
  - Then: V_vertical = JumpVelocity (约 14.97 u/s, +-0.1)

- **AC-2 (空中跳)**:
  - Given: 角色在空中 (已用地面跳)
  - When: 按下跳跃键
  - Then: V_vertical = AirJumpVelocity (约 12.72 u/s), 空中跳次数 -1

- **AC-3 (空中跳用完)**:
  - Given: 角色在空中, 空中跳已用完 (剩余 0 次)
  - When: 按下跳跃键
  - Then: 无响应, 不消耗输入

- **AC-4 (短跳)**:
  - Given: 角色刚起跳 (在 ShortHopWindow=5帧 内)
  - When: 松开跳跃键
  - Then: V_vertical 直接设为 ShortHopVelocity (约 6.73 u/s)

- **AC-5 (跳跃高度)**:
  - Given: 完整地面跳 (不松键)
  - When: 角色到达顶点 (V_vertical <= 0)
  - Then: 跳跃高度 = JumpHeight (3.5u +- 0.1), 帧数约 28 帧 (+-2)

- **AC-7 (土狼时间)**:
  - Given: 角色从平台边缘走出 (未跳跃)
  - When: 4 帧内按跳跃键
  - Then: 执行地面跳 (V = JumpVelocity), 不消耗空中跳次数

- **AC-8 (跳跃缓冲)**:
  - Given: 角色在空中即将着陆
  - When: 着陆前 3 帧内按下跳跃键
  - Then: 着陆延迟结束后自动执行地面跳

- **AC-9 (着陆延迟期间不可跳)**:
  - Given: 角色处于 Landing 状态 (着陆延迟 3 帧)
  - When: 按跳跃键
  - Then: 忽略, 不缓冲不执行

---

## Test Evidence

**Story Type**: Logic
**Required evidence**: `tests/unit/movement/jump-system_test.cs`
**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: Story 001 (InputReader 跳跃输入 + shortHop 标志), Story 002 (地面移动基础 + MovementState)
- Unlocks: Story 004 (快速下落), Story 006 (平台交互 — 着陆检测)
