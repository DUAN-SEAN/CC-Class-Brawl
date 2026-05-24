# ADR-0005: Input System — Unity New Input System + Per-Player Device Mapping

## Status
Accepted

## Date
2026-05-24

## Engine Compatibility

| Field | Value |
|-------|-------|
| **Engine** | Unity 2022.3.51 LTS |
| **Domain** | Input |
| **Knowledge Risk** | LOW — version is within LLM training data |
| **References Consulted** | `docs/engine-reference/unity/VERSION.md` |
| **Post-Cutoff APIs Used** | None |
| **Verification Required** | Verify PlayerInputManager join behavior with 2 gamepads; verify ReadValue<Vector2> in FixedUpdate returns latest Update value; verify onDeviceLost/onControlsChanged event timing |

## ADR Dependencies

| Field | Value |
|-------|-------|
| **Depends On** | ADR-0001 (Physics Timestep — 60Hz frame basis), ADR-0002 (Dual FSM — input buffer architecture) |
| **Enables** | All player-controlled systems (3C movement, combat, skill activation, pause) |
| **Blocks** | 3C System, Combat FSM, Skill Equipment — all player-interaction systems |
| **Ordering Note** | Must be Accepted before 3C and combat implementation |

## Context

### Problem Statement
本地 2 人对战格斗游戏需要精确的输入系统：每个玩家有独立的输入设备（手柄或键盘半区），按钮输入必须帧精确地到达 60Hz FixedUpdate 中的格斗状态机输入缓冲，方向输入必须实时反映摇杆位置，并且需要在设备断连时优雅处理。

### Constraints
- 2 人本地同屏，架构预留 4 人扩展
- 游戏逻辑运行在 60Hz FixedUpdate，输入回调在 Update 中触发
- FixedUpdate 和 Update 频率不同（渲染帧率可能是 60-144Hz）
- 攻击输入缓冲窗口 8 帧（GDD 定义）
- 跳跃输入缓冲窗口 3 帧（GDD 定义）
- 方向输入 Dead Zone 0.15（GDD 定义）

### Requirements
- 每个玩家独立映射输入设备（手柄或键盘半区）
- 手柄和键盘自动切换，无需手动配置
- 按钮输入帧精确到达 FixedUpdate（8 帧缓冲窗口）
- 方向输入实时读取最新值（持续量，不走缓冲）
- 跳跃支持按下和松开两个事件（控制跳跃高度）
- 设备断连时角色保持当前状态，显示提示
- Pause 输入立即响应，不走物理帧缓冲

## Decision

采用 **Unity New Input System + PlayerInput 组件 + 分层读取策略** 架构：

### 1. Input System 包

使用 `com.unity.inputsystem` 1.7.x（Unity 2022.3 LTS 内置版本）。

### 2. Action Map 结构

定义一个 `.inputactions` 资产，包含两个 Action Map：

**Gameplay Map**（对局中使用）：

| Action | Type | Gamepad Binding | KeyboardLeft | KeyboardRight |
|--------|------|-----------------|-------------|---------------|
| Move | Value, Vector2 | leftStick | WASD | Arrow Keys |
| Jump | Button | buttonSouth (A) | Space | Numpad0 |
| Attack | Button | buttonWest (X) | J | Numpad1 |
| Dash | Button | buttonEast (B) | K | Numpad2 |
| Skill1 | Button | rightShoulder | U | Numpad4 |
| Skill2 | Button | leftShoulder | I | Numpad5 |
| Skill3 | Button | rightTrigger | O | Numpad6 |
| Skill4 | Button | leftTrigger | P | Numpad7 |

**UI Map**（菜单/暂停中使用）：

| Action | Type | Binding |
|--------|------|---------|
| Navigate | Value, Vector2 | leftStick / WASD / Arrow Keys |
| Submit | Button | buttonSouth (A) / Space / Enter |
| Cancel | Button | buttonEast (B) / Escape |
| Pause | Button | startButton / Escape |

**Control Schemes**：
- `Gamepad` — 要求 `<Gamepad>` 设备
- `KeyboardLeft` — 要求 `<Keyboard>`，WASD 侧按键
- `KeyboardRight` — 要求 `<Keyboard>`，方向键侧按键

Pause 不在 Gameplay Map 中，而在 UI Map 中，通过独立回调处理，不进入战斗输入缓冲。

### 3. 设备配对：PlayerInput + PlayerInputManager

```
PlayerInputManager (singleton on scene)
  ├── playerPrefab: Character prefab with PlayerInput component
  ├── maxPlayerCount: 2 (MVP), 4 (architecture reserved)
  ├── joinBehavior: JoinPlayersWhenButtonIsPressed
  └── onPlayerJoined → initialize character systems

PlayerInput (per-character)
  ├── Behavior: InvokeCSharpEvents (NOT SendMessage)
  ├── Actions: references .inputactions asset
  ├── Default Map: "Gameplay"
  └── onActionTriggered → InputReader handles routing
```

**关键：`Behavior` 必须设为 `InvokeCSharpEvents`**。默认的 `SendMessage` 使用反射调用，性能不可接受。`InvokeCSharpEvents` 通过直接委托调用，零反射开销。

**设备配对流程**：
1. 角色选择画面，`PlayerInputManager.EnableJoining()` 开启加入
2. 玩家按任意键 → `onPlayerJoined` 回调 → 自动分配设备和 `playerIndex`
3. `PlayerInput` 根据已配对设备自动选择匹配的 Control Scheme
4. 对局开始 → `PlayerInputManager.DisableJoining()` 锁定

**设备断连**：
- `PlayerInput.onDeviceLost` → 角色保持当前状态，通知 UI 显示断连提示
- `PlayerInput.onControlsChanged` → 控制方案切换（如手柄断开切键盘）

### 4. 分层读取策略

方向输入（持续量）和按钮输入（离散事件）使用不同的读取策略：

**方向输入（Move）**：在 FixedUpdate 中直接读取当前值。

```csharp
// 在 InputReader.FixedUpdatePoll() 中
Vector2 moveInput = _moveAction.ReadValue<Vector2>();
if (moveInput.magnitude < DeadZone) moveInput = Vector2.zero;
```

理由：方向输入是持续量，不需要帧号记录。`ReadValue<>()` 返回最近一次 `InputSystem.Update()` 时的值，对于方向控制足够精确。

**按钮输入（Jump/Attack/Dash/Skill）**：回调写入环形缓冲，FixedUpdate 消费。

```csharp
// InputReader 订阅 PlayerInput.onActionTriggered
// 按钮按下时写入缓冲：
void OnButtonPerformed(InputActionType type)
{
    _inputBuffer.Write(type, _frameCounter.CurrentFrame);
}

// FixedUpdate 中消费缓冲：
bool TryConsume(InputActionType type, int bufferFrames)
{
    // BufferAge = CurrentFrame - RecordedFrame
    // Valid: BufferAge >= 0 && BufferAge <= bufferFrames
    // Execute: Valid && CurrentStateAccepts(type)
    // Discard: BufferAge > bufferFrames || Executed
}
```

**跳跃特殊处理**：跳跃需要 `performed` 和 `canceled` 两个事件（控制跳跃高度）。`performed` 写入缓冲，`canceled` 设置标志位，FixedUpdate 检查标志位决定是否应用短跳衰减。

### 5. InputReader 组件架构

每个角色 GameObject 上的组件层次：

```
Character GameObject
├── PlayerInput (Unity 组件，设备配对)
├── InputReader (自定义组件，回调路由)
│   ├── 方向输入 → 直接暴露 ReadValue<Vector2> 给 3C
│   ├── 按钮输入 → 写入 InputBuffer（环形缓冲）
│   └── Pause 输入 → 直接事件，不走缓冲
├── InputBuffer (环形缓冲，8 条目)
```

InputReader 是唯一的输入入口点。3C 系统和格斗状态机从不直接访问 PlayerInput 或 InputAction——它们通过 InputReader 提供的接口获取输入。

### 6. 帧号同步

全局帧号由 `FrameCounter` 在 FixedUpdate 中递增：

```csharp
public class FrameCounter : MonoBehaviour
{
    public int CurrentFrame { get; private set; }
    
    private void FixedUpdate()
    {
        CurrentFrame++;
    }
}
```

Update 回调中记录输入时，使用 `FrameCounter.CurrentFrame` 作为 `RecordedFrame`。这确保：
- 渲染帧率高于 60Hz 时：同一帧号下可能记录多个输入（BufferAge=0，全部有效）
- 渲染帧率低于 60Hz 时：BufferAge 增大，超过窗口的输入被丢弃

### Architecture Diagram

```
┌─ Unity Update Loop ──────────────────────────────────┐
│                                                       │
│  InputSystem.Update() (automatic, pre-Update)        │
│       ↓                                               │
│  InputAction callbacks fire:                          │
│    Move.performed → update cached Vector2             │
│    Attack.performed → InputBuffer.Write(type, frame)  │
│    Jump.performed → InputBuffer.Write + flag          │
│    Jump.canceled → set shortHop flag                  │
│    Pause.performed → OnPauseRequested event           │
│                                                       │
└───────────────────────────────────────────────────────┘
                    ↓ FixedUpdate (60Hz)
┌─ CharacterController.FixedUpdate ────────────────────┐
│                                                       │
│  InputReader.FixedUpdatePoll():                       │
│    1. Direction: _moveAction.ReadValue<Vector2>()     │
│       → MovementController (direct, every frame)      │
│    2. Buttons: InputBuffer.TryConsume(type, 8)        │
│       → CombatFSM (buffered, age-checked)             │
│    3. Jump cancel: check shortHop flag                │
│       → MovementController (height control)           │
│                                                       │
└───────────────────────────────────────────────────────┘
```

### Key Interfaces

- `InputReader` — 每角色输入入口，封装 PlayerInput + InputBuffer
- `InputBuffer` — 环形缓冲（8 条目），Write(Update) / TryConsume(FixedUpdate)
- `FrameCounter` — 全局物理帧号，FixedUpdate 递增
- `IInputReader` — 输入读取接口（3C 和 CombatFSM 消费）

```csharp
public interface IInputReader
{
    Vector2 GetMoveInput();
    bool TryConsumeAction(InputActionType type, int bufferFrames);
    bool IsJumpHeld();
    bool WasJumpReleasedThisFrame();
    int PlayerIndex { get; }
}
```

## Alternatives Considered

### Alternative 1: 旧 Input Manager (Input.GetAxis)
- **Description**: 使用 Unity 经典 Input Manager，GetAxis/GetButton 轮询
- **Pros**: 简单直接，无额外包依赖；性能可预测
- **Cons**: 无法区分多个同类型设备（两个手柄无法区分）；不支持事件驱动回调；不支持 Control Schemes 自动切换；API 已进入维护模式
- **Rejection Reason**: 格斗游戏的多人设备配对是新输入系统的核心场景。旧系统 `Input.GetAxis("Horizontal")` 是全局的，两个玩家用两个手柄无法分别读取各自的摇杆

### Alternative 2: 手动 Gamepad.current / Keyboard.current 读取
- **Description**: 直接使用 `Gamepad.all[0]`、`Gamepad.all[1]` 读取设备
- **Pros**: 最细粒度控制；无 PlayerInput 抽象层开销
- **Cons**: 设备列表顺序不确定（手柄断连重连后索引可能变化）；需要手动管理设备发现和配对；需要手动处理键盘分半区逻辑；代码量是 PlayerInput 方案的 3-4 倍
- **Rejection Reason**: PlayerInput 已封装了设备发现、配对、Control Scheme 切换、断连处理。手动实现等同重复造轮子，且设备索引顺序不确定性是已知的 bug 来源

## Consequences

### Positive
- PlayerInput 自动处理设备发现和配对，设计师无需手动配置
- 分层读取策略（持续量直接读 + 离散事件缓冲）匹配格斗游戏的输入特性
- Control Schemes 支持手柄和键盘无缝切换
- InvokeCSharpEvents 避免 SendMessage 反射开销
- InputReader 作为唯一入口点，3C 和 CombatFSM 不直接耦合 Input System API

### Negative
- 依赖 com.unity.inputsystem 包（额外的包依赖）
- Update 和 FixedUpdate 频率不一致需要缓冲层——增加了架构复杂度
- PlayerInput 组件的 Inspector 配置（特别是 Unity Events 绑定）在多人场景下需要仔细设置
- 键盘双人共享同一物理设备，Control Scheme 依赖绑定路径区分（不如手柄直观）

### Risks
- **Update/FixedUpdate 频率差异**: 高渲染帧率下同一物理帧可能记录多个输入 → 缓解: BufferAge=0 的多个输入按优先级消费，不造成逻辑错误
- **设备索引不确定性**: 如果不使用 PlayerInput 而直接用 Gamepad.all[idx]，断连重连后索引可能变化 → 缓解: 使用 PlayerInput 管理，不直接索引设备列表
- **键盘双人冲突**: 两个 Control Scheme 都需要 `<Keyboard>`，某些按键可能被两个 Scheme 同时匹配 → 缓解: 绑定路径严格区分（WASD 侧 vs 方向键侧），PlayerInput 根据 Control Scheme 要求自动选择。如果自动匹配有问题，可在 `onPlayerJoined` 中手动调用 `SwitchCurrentControlScheme` 强制切换
- **键盘加入 Scheme 歧义**: 玩家按不属于任何 Scheme 特定绑定的键加入时，Scheme 选择可能失败 → 缓解: 加入阶段只启用 Gameplay Map，确保所有加入触发键唯一映射到某个 Scheme
- **设备断连回调顺序**: `onDeviceLost` 和 `onControlsChanged` 触发顺序不确定 → 缓解: 不依赖回调顺序，分别设置标志位处理。补充 `onDeviceRegained` 事件处理设备重连
- **缓冲容量 vs 缓冲窗口**: InputBuffer 容量 8 条目（最多存储 8 个未消费事件）与缓冲窗口 8 帧（时间有效性）是不同概念 → 缓解: 代码中用 `BufferCapacity` 和 `BufferWindowFrames` 区分命名

## GDD Requirements Addressed

| GDD System | Requirement | How This ADR Addresses It |
|------------|-------------|--------------------------|
| 3c-system.md | "使用 Unity 新 Input System，每个玩家通过 PlayerInput 组件独立映射输入设备" | PlayerInput + PlayerInputManager 架构 |
| 3c-system.md | "方向输入：模拟量 0.0–1.0，Dead Zone 0.15" | ReadValue<Vector2>() + magnitude 检查 |
| 3c-system.md | "跳跃输入：按钮，支持按下和松开两个事件" | Jump performed/canceled 双回调 + shortHop 标志 |
| 3c-system.md | "MVP 支持 2 人本地同屏，架构预留 4 人扩展" | PlayerInputManager.maxPlayerCount 可配置 |
| combat-state-machine.md | "攻击输入进入输入缓冲（InputBufferFrames = 8 帧）" | 环形缓冲 8 条目 + BufferAge 校验 |
| combat-state-machine.md | "输入优先级：特殊攻击 > 空中攻击 > 地面攻击" | InputReader 按优先级消费缓冲 |
| combat-state-machine.md | "BufferAge = CurrentFrame - InputRecordedFrame" | FrameCounter 全局帧号 + 缓冲年龄公式 |
| 3c-system.md | "移动输入直接驱动物理，不做输入缓冲" | 方向输入 ReadValue 直接传给 3C，不走缓冲 |
| 3c-system.md | "输入设备断开：角色保持当前状态，显示提示" | onDeviceLost 事件 + UI 通知 |

## Performance Implications
- **CPU**: InputAction 回调 < 0.01ms/input；InputBuffer.TryConsume O(N) N≤8 < 1μs；PlayerInput 组件开销可忽略
- **Memory**: InputBuffer 固定 8 × 24B = 192B/玩家；InputReader 组件 < 1KB/玩家
- **Load Time**: .inputactions 资产加载 < 1ms
- **Network**: 不适用（本地多人）

## Migration Plan
不适用——新项目初始架构决策。

## Validation Criteria
- [ ] 两个手柄各自控制独立角色，输入互不干扰
- [ ] 一个手柄 + 一个键盘各自控制独立角色
- [ ] 方向输入 Dead Zone 0.15 正确过滤噪声
- [ ] 按钮输入在 8 帧缓冲窗口内被正确消费
- [ ] 超过 8 帧缓冲窗口的输入被正确丢弃
- [ ] 跳跃按下/松开正确控制跳跃高度
- [ ] Pause 输入立即响应，不受 FixedUpdate 频率影响
- [ ] 手柄断连时触发 onDeviceLost，角色保持当前状态
- [ ] Control Scheme 自动切换（手柄 ↔ 键盘）正常工作
- [ ] 输入系统帧耗时 < 0.1ms
- [ ] 两个键盘玩家同时游戏时，各自只响应自己 Scheme 的按键，不互相干扰

## Related Decisions
- ADR-0001: Physics Timestep — 输入缓冲基于 60Hz 物理帧计数
- ADR-0002: Dual FSM Architecture — CombatFSM 消费 InputBuffer 中的按钮输入，3C 消费方向输入
- ADR-0004: Skill System Data-Driven — Skill InputMapping 决定哪个按钮触发技能
