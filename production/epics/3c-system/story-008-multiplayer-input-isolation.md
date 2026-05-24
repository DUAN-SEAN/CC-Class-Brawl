# Story 008: 多人输入隔离 — 2 人独立控制验证

> **Epic**: 3C系统
> **Status**: Ready
> **Layer**: Foundation
> **Type**: Integration
> **Estimate**: S
> **Manifest Version**: 2026-05-24
> **Last Updated**: 2026-05-24

## Context

**GDD**: `design/gdd/3c-system.md`
**Requirement**: TR-MOV-051 ~ TR-MOV-052 (多人输入相关)
**ADR Governing Implementation**: ADR-0005: Input System — PlayerInput + PlayerInputManager 多设备配对
**ADR Decision Summary**: PlayerInputManager 管理设备配对, 每个角色有独立 PlayerInput, InputReader 封装输入读取, 玩家间输入完全隔离。
**Engine**: Unity 2022.3.51 LTS | **Risk**: LOW
**Engine Notes**: PlayerInputManager 自动处理多手柄配对和分离。

**Control Manifest Rules (Foundation)**:
- Required: PlayerInputManager maxPlayerCount=2, joinBehavior=JoinPlayersWhenButtonIsPressed
- Required: PlayerInput Behavior=InvokeCSharpEvents
- Guardrail: 输入系统帧耗时 < 0.1ms

---

## Acceptance Criteria

- [ ] 2 个手柄各自控制独立角色, 输入互不干扰
- [ ] 1 个手柄 + 1 个键盘各自控制独立角色
- [ ] 玩家1 按跳跃不影响玩家2 的角色
- [ ] 玩家1 摇杆方向不影响玩家2 的角色移动
- [ ] 键盘双人: WASD 侧和方向键侧各自独立, 不互相干扰
- [ ] 手柄断开重连后恢复配对, 不影响另一玩家
- [ ] 3C 系统 (输入+移动+摄像机) 合计帧耗时 < 2ms (2 人对战)

---

## Implementation Notes

**来自 ADR-0005 的具体指导**:

1. PlayerInputManager singleton 管理玩家加入
2. 每个角色 GameObject: PlayerInput + InputReader + InputBuffer
3. PlayerInput 根据 Control Scheme 自动选择 (Gamepad/KeyboardLeft/KeyboardRight)
4. onPlayerJoined 回调 -> 初始化角色系统 -> 分配 playerIndex

**集成测试重点**:
- 验证两个 InputReader 实例各自读取正确的设备输入
- 验证 InputBuffer 消费时不会消费另一玩家的输入
- 验证 Control Scheme 切换不干扰另一玩家

**性能测量**: 在 FixedUpdate 中测量 InputReader + MovementController + CameraController 合计耗时。

---

## Out of Scope

- 4 人对战 (架构预留但不实现)
- 角色选择 UI 的玩家加入流程 (属于 game-state-management epic)

---

## QA Test Cases

- **AC-1 (双手柄独立控制)**:
  - Given: 2 个手柄已连接并配对
  - When: 玩家1 向右推摇杆, 玩家2 向左推摇杆
  - Then: 角色1 向右移动, 角色2 向左移动, 互不干扰

- **AC-2 (手柄+键盘混合)**:
  - Given: 1 个手柄 + 1 个键盘已配对
  - When: 手柄玩家跳跃, 键盘玩家攻击
  - Then: 手柄控制的角色跳跃, 键盘控制的角色攻击

- **AC-3 (输入隔离)**:
  - Given: 2 个手柄已配对
  - When: 玩家1 按跳跃键
  - Then: 仅玩家1 的角色跳跃, 玩家2 无反应

- **AC-5 (键盘双人)**:
  - Given: WASD 侧和方向键侧分别配对
  - When: WASD 侧按 Space, 方向键侧按 Numpad0
  - Then: 各自独立触发跳跃, 不互相干扰

- **AC-6 (手柄断连重连)**:
  - Given: 2 个手柄已配对
  - When: 玩家1 手柄断开然后重连
  - Then: 玩家1 恢复控制, 玩家2 不受影响

- **AC-7 (性能)**:
  - Given: 2 人对战进行中
  - When: 测量帧耗时
  - Then: 3C 系统 (输入+移动+摄像机) < 2ms

---

## Test Evidence

**Story Type**: Integration (多设备集成测试)
**Required evidence**: `tests/integration/movement/multiplayer-input_test.cs`
**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: Story 001 (InputReader + PlayerInput), Story 002 (地面移动), Story 007 (摄像机)
- Unlocks: None (验证性故事)
