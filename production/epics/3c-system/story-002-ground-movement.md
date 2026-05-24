# Story 002: 地面移动 — 加速、减速、方向切换

> **Epic**: 3C系统
> **Status**: Ready
> **Layer**: Foundation
> **Type**: Logic
> **Estimate**: M
> **Manifest Version**: 2026-05-24
> **Last Updated**: 2026-05-24

## Context

**GDD**: `design/gdd/3c-system.md`
**Requirement**: TR-MOV-009 ~ TR-MOV-016 (地面移动相关)
**ADR Governing Implementation**: ADR-0001: Physics Timestep — 60Hz FixedTimestep + Manual Gravity + Rigidbody2D.velocity direct assignment
**ADR Decision Summary**: 60Hz FixedUpdate, gravityScale=0, 直接 velocity 赋值，不使用 AddForce。所有运动公式使用 Time.fixedDeltaTime。
**Engine**: Unity 2022.3.51 LTS | **Risk**: LOW
**Engine Notes**: Rigidbody2D.interpolation = Interpolate 消除视觉抖动。

**Control Manifest Rules (Foundation)**:
- Required: Rigidbody2D.gravityScale = 0, 直接 velocity 赋值, Time.fixedDeltaTime = 1/60
- Required: 所有 GDD 公式使用帧基准 (1/60s units)
- Forbidden: Unity 默认 50Hz 物理步, AddForce, Time.deltaTime 用于物理

---

## Acceptance Criteria

- [ ] MovementState 枚举扩展为 GDD 定义的 9 个状态: Idle, Running, Jumping, Falling, FastFalling, Dashing, AirDodging, Landing, PlatformDrop
- [ ] MovementParams 数据结构包含地面移动参数: MoveAcceleration, MaxGroundSpeed, GroundFriction, StopThreshold, WalkSpeedRatio
- [ ] 地面加速公式: `V_new = Min(|V + MoveAcceleration * |input_x| * dt|, MaxGroundSpeed) * sign(input_x)`
- [ ] 地面摩擦公式: `V_new = V * pow(1 - GroundFriction, dt * 60)`, |V| < StopThreshold 时归零
- [ ] 方向切换即时生效，无转身锁定帧
- [ ] 摇杆偏移量影响加速力度 (input_x 作为乘数)
- [ ] 步行/奔跑为纯动画驱动: < 0.3 步行, > 0.7 奔跑, 0.3-0.7 线性过渡 (不影响速度公式)
- [ ] 所有移动参数从 MovementParams 注入，不硬编码
- [ ] 物理在 FixedUpdate 60Hz 中执行，使用 Time.fixedDeltaTime

---

## Implementation Notes

**来自 ADR-0001 的具体指导**:

1. FixedTimestep 通过 Project Settings 设为 0.0166667，不通过运行时代码
2. Maximum Allowed Timestep 设为 0.0333333 (2 个物理步)
3. Rigidbody2D.velocity 直接赋值: `rb.velocity = newVelocity`
4. 所有角色 Rigidbody2D.interpolation = Interpolate

**来自 ADR-0002 的具体指导**:

1. MovementController 是独立 MonoBehaviour，管理 MovementState
2. 通过 IMovementController 接口暴露给 CombatFSM
3. CharacterController 协调器按顺序调度: MovementController 先于 CombatFSM

**默认参数值** (来自 GDD Tuning Knobs):
- MoveAcceleration = 57.0 u/s^2
- MaxGroundSpeed = 5.0 u/s
- GroundFriction = 0.15
- StopThreshold = 0.05 u/s
- WalkSpeedRatio = 0.4

**注意**: 当前 MovementState 枚举只有 Grounded/Airborne/Dashing 三个值，需扩展为 9 个值以匹配 GDD 状态表。

---

## Out of Scope

- 空中移动 (Story 003 处理)
- 跳跃逻辑 (Story 003)
- 冲刺/Dash (Story 005)
- 摄像机 (Story 007)
- CombatFSM 冻结/解冻移动的协调 (由 CombatFSM 通过 IMovementController.FreezeMovement 调用)
- 角色动画播放 (由 Visual/Feel 层处理)

---

## QA Test Cases

- **AC-3 (加速到最大速度)**:
  - Given: 角色静止在地面 (V=0)
  - When: 按住右方向 10 帧 (摇杆推满 input_x=1.0)
  - Then: 水平速度达到 MaxGroundSpeed (5.0 u/s +- 0.1)
  - Edge cases: 摇杆偏移量 0.5 时加速力度减半

- **AC-4 (摩擦减速)**:
  - Given: 角色以 MaxGroundSpeed (5.0 u/s) 奔跑
  - When: 松开方向键 (input_x=0)
  - Then: 15 帧内速度降至 StopThreshold (0.05 u/s) 以下
  - Edge cases: 速度低于 StopThreshold 时精确归零

- **AC-5 (方向即时切换)**:
  - Given: 角色以最大速度向右奔跑 (V=+5.0)
  - When: 立即按下左方向
  - Then: 无转身锁定帧，立即开始向左加速
  - Edge cases: 同帧左+右方向按下，优先最后按下的方向

- **AC-7 (步行/奔跑不影响速度)**:
  - Given: 摇杆偏移量 0.2 (步行范围)
  - When: 按住方向移动
  - Then: 速度公式使用完整的 MaxGroundSpeed 作为上限，步行比例仅影响动画选择

---

## Test Evidence

**Story Type**: Logic
**Required evidence**: `tests/unit/movement/ground-movement_test.cs`
**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: Story 001 (InputReader 提供 IInputReader 接口读取方向输入)
- Unlocks: Story 003 (跳跃系统), Story 005 (冲刺)
