# Story 005: 冲刺/闪避 — 地面冲刺与空中闪避

> **Epic**: 3C系统
> **Status**: Ready
> **Layer**: Foundation
> **Type**: Logic
> **Estimate**: M
> **Manifest Version**: 2026-05-24
> **Last Updated**: 2026-05-24

## Context

**GDD**: `design/gdd/3c-system.md`
**Requirement**: TR-MOV-031 ~ TR-MOV-038 (冲刺/闪避相关)
**ADR Governing Implementation**: ADR-0002: Dual FSM Architecture — CombatFSM 通过 IMovementController 冻结移动
**ADR Decision Summary**: MovementController 管理 Dashing/AirDodging 状态, CombatFSM 通过 FreezeMovement 控制。
**Engine**: Unity 2022.3.51 LTS | **Risk**: LOW
**Engine Notes**: 冲刺速度通过 velocity 直接赋值实现。

**Control Manifest Rules (Foundation)**:
- Required: IMovementController 接口: GetState(), FreezeMovement(), SetVelocity()
- Required: MovementState 包含 Dashing, AirDodging
- Guardrail: 两个 FSM 合计帧耗时 < 0.5ms

---

## Acceptance Criteria

- [ ] 地面冲刺: 按闪避键向面朝方向快速位移
- [ ] 三阶段帧数据: StartupFrames=2帧(无位移), ActiveFrames=6帧(高速位移), RecoveryFrames=4帧(可被攻击)
- [ ] 冲刺速度公式: `DashSpeed = DashDistance * 60.0 / DashActiveFrames` (默认 25.0 u/s)
- [ ] 冲刺冷却: DashCooldownFrames (30帧/0.5s), 冷却期间不可再次冲刺
- [ ] 冲刺方向固定为面朝方向, 冲刺中不可改变
- [ ] 空中闪避: 空中按闪避键, 保留空中惯性, 向当前方向快速位移
- [ ] 空中闪避击退衰减: 闪避期间水平击退速度减半 (50%), 闪避结束后恢复
- [ ] 空中闪避活跃帧结束: 到达地面 -> Landing, 否则 -> Falling
- [ ] 启动帧期间被攻击: 无无敌, 正常受击, 冲刺取消
- [ ] 冲刺活跃帧前 2 帧有无敌判定 (后续帧可被攻击)
- [ ] 着陆不重置冲刺冷却

---

## Implementation Notes

**GDD 公式 — 冲刺速度**:
```
DashSpeed = DashDistance * 60.0 / DashActiveFrames
```

**默认参数值**:
- DashDistance = 2.5 u
- DashStartupFrames = 2 帧
- DashActiveFrames = 6 帧
- DashRecoveryFrames = 4 帧
- DashCooldownFrames = 30 帧 (0.5s)
- DashSpeed = 25.0 u/s (计算值)

**状态转换 (来自 GDD 状态表)**:
- Idle/Running + 闪避键 + 冷却完毕 -> Dashing
- Jumping/Falling + 闪避键 -> AirDodging
- Dashing + 恢复帧结束 + 方向输入 -> Running
- Dashing + 恢复帧结束 + 无输入 -> Idle
- AirDodging + 活跃帧结束 + 到达地面 -> Landing
- AirDodging + 活跃帧结束 + 未到达地面 -> Falling

**状态优先级 (来自 GDD)**: Dashing/AirDodging > Landing > Jumping/Falling/FastFalling > Running > Idle

**无敌帧实现**: 前 2 帧无敌通过标记位实现, 碰撞系统检查此标记位。碰撞系统在本 epic 范围外, 此处仅暴露接口。

---

## Out of Scope

- 碰撞系统对无敌帧的实际处理 (属于 collision-system epic)
- 冲刺的视觉效果 (运动模糊/拖尾) (属于 Visual/Feel 层)
- 冲刺尘土粒子效果

---

## QA Test Cases

- **AC-2 (三阶段帧数据)**:
  - Given: 角色在地面, 冲刺冷却完毕
  - When: 按闪避键
  - Then: 2 帧启动(无位移) -> 6 帧活跃(DashSpeed=25.0 u/s) -> 4 帧恢复(可被攻击)

- **AC-3 (冲刺速度)**:
  - Given: 冲刺活跃帧期间
  - When: 测量位移
  - Then: 速度 = DashSpeed (25.0 u/s), 6 帧总位移 = 2.5u

- **AC-4 (冲刺冷却)**:
  - Given: 冲刺刚结束
  - When: 在 DashCooldownFrames (30帧) 内再次按闪避键
  - Then: 无响应

- **AC-5 (冲刺方向固定)**:
  - Given: 角色面朝右, 冲刺中
  - When: 按左方向
  - Then: 冲刺方向不变 (仍向右), 面朝方向不变

- **AC-6 (空中闪避)**:
  - Given: 角色在空中
  - When: 按闪避键
  - Then: 执行空中闪避, 保留空中惯性

- **AC-7 (空中闪避击退衰减)**:
  - Given: 空中闪避期间
  - When: 受到击退力
  - Then: 水平击退速度减半, 垂直击退不变

- **AC-9 (启动帧被攻击)**:
  - Given: 冲刺启动帧期间
  - When: 被攻击
  - Then: 无无敌保护, 正常受击, 冲刺取消

---

## Test Evidence

**Story Type**: Logic
**Required evidence**: `tests/unit/movement/dash-dodge_test.cs`
**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: Story 001 (InputReader 闪避输入), Story 002 (地面移动 + MovementState)
- Unlocks: None (后续 Core 层系统依赖)
