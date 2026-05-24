# Story 004: 快速下落与终端速度

> **Epic**: 3C系统
> **Status**: Ready
> **Layer**: Foundation
> **Type**: Logic
> **Estimate**: S
> **Manifest Version**: 2026-05-24
> **Last Updated**: 2026-05-24

## Context

**GDD**: `design/gdd/3c-system.md`
**Requirement**: TR-MOV-027 ~ TR-MOV-030 (快速下落与终端速度)
**ADR Governing Implementation**: ADR-0001: Physics Timestep — 60Hz 手动重力 + 不同状态不同重力倍率
**ADR Decision Summary**: 重力手动施加 velocity.y -= Gravity * multiplier * dt, 终端速度钳制, FastFall 时 multiplier = 2.2。
**Engine**: Unity 2022.3.51 LTS | **Risk**: LOW
**Engine Notes**: 无特殊引擎注意事项。

**Control Manifest Rules (Foundation)**:
- Required: 不同角色状态必须使用不同重力倍率
- Required: TerminalVelocity 钳制为 20.0 u/s, Gravity 32.0 u/s^2
- Guardrail: Gravity 和 TerminalVelocity 使用共享 Constants 类

---

## Acceptance Criteria

- [ ] 正常下落: 重力倍率 1.0x, `V_vertical -= Gravity * dt`
- [ ] 快速下落: 按住下键 (Y < -0.5) 且角色在下落阶段 (已过顶点), 重力倍率 FastFallGravityMultiplier (2.2x)
- [ ] 快速下落触发瞬间: V_vertical 立即设为 `Min(V_vertical, -FastFallInitiationSpeed)`, 确保瞬间可感知
- [ ] 终端速度: V_vertical 始终钳制在 [-TerminalVelocity, +JumpVelocity] 范围内
- [ ] 快速下落期间着地: 正常着陆, 着陆延迟与正常下落相同 (3 帧)
- [ ] FastFalling 状态正确转换: 下键松开或着地时退出 FastFalling
- [ ] 空中控制: 空中水平移动使用独立参数 AirAcceleration 和 MaxAirSpeed

---

## Implementation Notes

**GDD 公式 — 重力与快速下落**:
```
V_vertical_new = V_vertical - Gravity * FastFallMultiplier * dt
V_vertical_new = Max(V_vertical_new, -TerminalVelocity)
```

**GDD 公式 — 空中控制**:
```
V_air_new = Clamp(V_air + AirAcceleration * input_x * dt, -MaxAirSpeed, MaxAirSpeed)
```

**默认参数值**:
- Gravity = 32.0 u/s^2 (共享常量)
- TerminalVelocity = 20.0 u/s
- FastFallGravityMultiplier = 2.2
- FastFallInitiationSpeed = 5.0 u/s
- AirAcceleration = 28.0 u/s^2
- MaxAirSpeed = 3.5 u/s

**状态转换 (来自 GDD 状态表)**:
- Jumping + V_vertical <= 0 (顶点) + 下键 (Y < -0.5) -> FastFalling
- Falling + 下键 (Y < -0.5) -> FastFalling
- FastFalling + 着地 -> Landing
- FastFalling + 下键松开 -> Falling (恢复正常重力)

**验证示例**: 正常下落 10 帧: V = -5.33 u/s; 快速下落 10 帧: V = -11.73 u/s; 快速下落约 17 帧达到终端速度。

---

## Out of Scope

- 击退期间的重力行为 (由击退系统处理)
- 快速下落的视觉拖尾效果 (Visual/Feel 层)

---

## QA Test Cases

- **AC-2 (快速下落重力倍率)**:
  - Given: 角色在空中下落阶段 (V_vertical < 0)
  - When: 按住下键 (Y < -0.5)
  - Then: 重力倍率提升至 2.2x, 下落加速度明显加快

- **AC-3 (快速下落瞬间响应)**:
  - Given: 角色在空中, V_vertical = -2.0
  - When: 按住下键触发快速下落
  - Then: V_vertical 立即设为 Min(-2.0, -5.0) = -5.0 (瞬间可感知)

- **AC-4 (终端速度)**:
  - Given: 角色快速下落中
  - When: 达到 TerminalVelocity (20.0 u/s)
  - Then: 下落速度不再增加

- **AC-5 (快速下落着地)**:
  - Given: 角色处于 FastFalling 状态
  - When: 接触平台
  - Then: 正常着陆, 着陆延迟 = 3 帧

- **AC-7 (空中控制)**:
  - Given: 角色在空中, 水平静止
  - When: 按住右方向 8 帧
  - Then: 水平速度达到 MaxAirSpeed (3.5 u/s), 低于 MaxGroundSpeed

---

## Test Evidence

**Story Type**: Logic
**Required evidence**: `tests/unit/movement/fast-fall_test.cs`
**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: Story 002 (MovementState 基础), Story 003 (跳跃系统 — Jumping/Falling 状态)
- Unlocks: Story 005 (冲刺)
