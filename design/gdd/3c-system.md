# 3C系统 (Character, Controls, Camera)

> **Status**: Designed
> **Author**: SeanDuan + agents
> **Last Updated**: 2026-05-23
> **Implements Pillar**: Pillar 1: 秒学秒玩, Pillar 4: 快速战斗

## Overview

3C系统是职业对决的基础操控层，负责处理玩家输入（Controls）、角色移动与物理（Character）、以及摄像机行为（Camera）。作为基础设施层，它为上层所有系统提供统一的输入路由、角色状态驱动和视角管理接口。玩家通过手柄（主要）或键盘（次要）发出指令，3C系统将这些指令转化为角色的地面奔跑、跳跃、空中控制、快速下落和平台交互，同时摄像机实时跟踪所有在场玩家，保持战斗区域完整可见。手感目标是"即按即动"——从按下方向键到角色响应必须在一帧（16.6ms）内完成，让玩家感觉"角色就是我的延伸"。3C系统是格斗状态机、职业系统、攻击系统等 6 个上层系统的基础依赖。

## Player Fantasy

**核心幻想：「角色就是你的身体」**

玩家应该感觉不到"我在操控一个角色"——角色应该像自己的手和脚一样自然响应。按跳跃就跳，按方向就移动，没有任何迟疑或延迟。这种"即想即动"的感觉是平台格斗游戏的基础体验，所有上层战斗、技能系统的乐趣都建立在这个基础上。

**关键情感时刻**：
- **流畅穿梭** — 在多层平台间跳跃、奔跑、追逐对手时的流畅感，像在跑酷
- **精准着陆** — 精确降落在目标平台上的满足感，拇指松开跳跃键的那一刻角色恰好落在平台边缘
- **追逃张力** — 在平台上追逐或逃离对手时，移动的灵活性直接创造紧张感

**支柱对齐**：
- 服务 **Pillar 1: 秒学秒玩** — 移动和跳跃的操控方式必须一眼就懂，不需要教程
- 服务 **Pillar 4: 快速战斗** — 移动速度足够快，战斗节奏紧凑不拖沓

## Detailed Design

### Core Rules

**1. 输入处理 (Controls)**

1. 使用 Unity 新 Input System，每个玩家通过 `PlayerInput` 组件独立映射输入设备
2. MVP 支持 2 人本地同屏，架构预留 4 人扩展
3. 输入类型：
   - **方向输入**：模拟摇杆/方向键（模拟量，0.0–1.0），Dead Zone 0.15
   - **跳跃输入**：按钮，支持按下和松开两个事件（用于控制跳跃高度）
   - **闪避输入**：按钮，单独按键或方向+按键组合
   - **下键**：方向输入 Y < -0.5
4. 移动输入直接驱动物理，不做输入缓冲（攻击输入缓冲由格斗状态机处理）

**2. 地面移动 (Ground Movement)**

1. 按住方向键施加持续加速度（`MoveAcceleration`），模拟摇杆偏移量影响加速力度
2. 达到最大地面速度（`MaxGroundSpeed`）后不再加速
3. 松开方向键后，摩擦力（`GroundFriction`）将角色减速至停止
4. 方向切换即时生效，无转身锁定帧
5. 摇杆偏移量 < 0.3 时为"步行"速度（`WalkSpeedRatio × MaxGroundSpeed`），> 0.7 为"奔跑"，0.3–0.7 之间为线性过渡区。步行/奔跑为纯动画区别（影响角色动画播放和音效触发），不影响移动公式——移动公式统一使用 MaxGroundSpeed 作为速度上限

**3. 跳跃 (Jumping)**

1. **地面跳**：按跳跃键从地面或平台起跳，给角色一个向上的初始速度（`JumpForce`）
2. **空中跳（二段跳）**：空中按跳跃键触发，初始速度略低于地面跳（`AirJumpForceRatio × JumpForce`）
3. **短跳**：在起跳后 N 帧内松开跳跃键（`ShortHopWindow`），垂直速度立即设定为 `ShortHopVelocity`（速度设定模式，非钳制）。实现方式：起跳时以完整 JumpVelocity 发射（无输入延迟），若在 ShortHopWindow 内松键则将 V_vertical 直接设为 ShortHopVelocity
4. **快速下落**：空中按住下键（Y < -0.5）时，重力倍率提升至 `FastFallGravityMultiplier`，同时垂直速度立即设为 `Min(V_vertical, -FastFallInitiationSpeed)`，确保按下瞬间有可感知的响应
5. **空中控制**：空中水平移动使用独立的空中加速度（`AirAcceleration`）和最大空中速度（`MaxAirSpeed`），空中控制力 < 地面控制力
6. 二段跳有使用次数上限（`MaxAirJumps = 1`），着地后重置

**4. 闪避/冲刺 (Dash)**

1. **地面冲刺**：按闪避键向面朝方向快速位移一段距离
2. 分为三个阶段：
   - **启动帧** (`DashStartupFrames` = 2帧)：角色开始动作，尚无位移
   - **活跃帧** (`DashActiveFrames` = 6帧)：高速位移，前 2 帧无敌
   - **恢复帧** (`DashRecoveryFrames` = 4帧)：位移结束，角色可被攻击
3. **空中闪避**：空中按闪避键，角色向当前方向快速位移（保留空中惯性），无敌帧同地面。空中闪避期间击退动量衰减为50%（水平击退速度减半），闪避结束后恢复
4. 冲刺有冷却时间（`DashCooldownFrames`），防止连续闪避
5. 冲刺方向固定为面朝方向，不可在冲刺中改变

**5. 平台交互 (Platform Interaction)**

1. **穿越平台**：按住下键 + 按下跳跃键（输入窗口 `PlatformDropInputWindow` = 3帧，下键必须在跳跃键之前或同一帧按下），角色向下穿越当前平台
2. **着陆**：从空中接触平台顶部时，有短暂着陆延迟（`LandingLagFrames` = 3帧）
3. 平台仅从顶部有碰撞——角色可从下方跳穿平台

**6. 物理常量**

> **物理架构约定**: 所有运动逻辑在 `FixedUpdate` 中以 60Hz 固定时间步执行（Unity `FixedTimestep = 1/60`）。`Rigidbody2D.gravityScale = 0`，重力由公式 4 手动施加。所有公式中的 `dt = Time.fixedDeltaTime = 1/60`。切勿使用 Unity 默认的 50Hz 物理频率。此决策记录在 ADR 中（`docs/architecture/adr-0001-physics-timestep.md`）。

1. 重力（`Gravity`）持续作用于空中角色
2. 终端下落速度（`TerminalVelocity`）为最大下落速度上限
3. 击退力由击退系统处理，不在此定义

**7. 摄像机 (Camera)**

1. 摄像机持续跟踪所有在场玩家位置
2. 计算所有玩家位置的包围盒（bounding box），摄像机中心为包围盒中心
3. 根据包围盒大小动态调整正交摄像机尺寸（Orthographic Size）
4. 摄像机尺寸有最小值（`MinCamSize`）和最大值（`MaxCamSize`）约束
5. 摄像机不能显示超出场地边界的区域
6. 摄像机移动使用平滑插值（`CameraSmoothSpeed`），避免生硬跳动

### States and Transitions

3C 系统的移动状态表（战斗状态由格斗状态机定义，不在此处）：

| 当前状态 | 触发条件 | 目标状态 |
|---------|---------|---------|
| Idle | 方向输入 > dead zone | Running |
| Idle | 跳跃键按下（在地面） | Jumping |
| Idle | 闪避键按下 | Dashing |
| Running | 方向输入 < dead zone | Idle |
| Running | 跳跃键按下 | Jumping |
| Running | 闪避键按下 | Dashing |
| Jumping | 垂直速度 <= 0（到达跳跃顶点） | Falling |
| Jumping | 跳跃键按下（有剩余空中跳次数） | Jumping（重置为空中跳） |
| Jumping | 闪避键按下（空中） | AirDodging |
| Jumping | 下键（Y < -0.5，且 V_vertical < 0 即已过顶点） | FastFalling |
| Falling | 着地 | Landing |
| Falling | 下键 + 跳跃键（在平台上） | PlatformDrop |
| Falling | 快速下落键 | FastFalling |
| Falling | 跳跃键按下（有剩余空中跳次数） | Jumping |
| Falling | 闪避键按下 | AirDodging |
| FastFalling | 着地 | Landing |
| AirDodging | 活跃帧结束 | Falling |
| Dashing | 恢复帧结束（在地面） | Running（方向输入 > dead zone）或 Idle（方向输入 < dead zone）|
| Landing | 着陆延迟结束 | Idle |
| Landing | 着陆延迟结束 + 方向输入 | Running |
| PlatformDrop | 穿越平台完成 | Falling |

状态优先级（高→低）：Dashing/AirDodging > Landing > Jumping/Falling/FastFalling > Running > Idle

### Interactions with Other Systems

| 系统 | 数据流方向 | 接口描述 |
|------|-----------|---------|
| 格斗状态机 | 3C → FSM | 3C 提供当前移动状态、角色位置、面朝方向、是否在地面。FSM 通过 `IMovementController` 接口查询 |
| 格斗状态机 | FSM → 3C | FSM 可以冻结移动（攻击动画期间）、强制位移（击退）、修改速度 |
| 职业系统 | 职业 → 3C | 职业提供基础移动属性：`MoveAcceleration`, `MaxGroundSpeed`, `JumpForce`, `MaxAirJumps` 等 |
| 攻击系统 | 攻击 → 3C | 攻击期间锁定移动状态（不可奔跑/跳跃），攻击结束恢复 |
| 碰撞判定系统 | 碰撞 → 3C | 平台碰撞体决定地面检测和着陆判定 |
| 击退与击飞系统 | 击退 → 3C | 击退力直接施加到角色物理体，覆盖当前移动速度 |
| 专注值系统 | 无直接交互 | 通过格斗状态机间接关联 |
| 技能附属物系统 | 技能 → 3C | 某些技能可能修改移动参数（加速/减速），通过 `IMovementController.ModifySpeed()` 接口 |

## Formulas

**单位系统**: 1 Unity 单位 = 64 像素 (PPU = 64)。角色高度 = 1 单位。

### 1. 地面速度更新（加速）

`V_ground_new = Min(|V_ground + MoveAcceleration × |input_x| × dt|, MaxGroundSpeed) × sign(input_x)`

| 变量 | 符号 | 类型 | 范围 | 描述 |
|------|------|------|------|------|
| 当前地面速度 | V_ground | float | [-MaxGroundSpeed, MaxGroundSpeed] | 角色水平速度 |
| 移动加速度 | MoveAcceleration | float | 默认 57.0 u/s² | 按方向时的加速力度 |
| 水平输入 | input_x | float | [-1.0, 1.0] | 摇杆水平轴（经过 dead zone） |
| 最大地面速度 | MaxGroundSpeed | float | 默认 5.0 u/s | 地面速度上限 |

**输出范围**: -MaxGroundSpeed 到 MaxGroundSpeed
**示例**: 从静止开始，摇杆推满向右：每帧加速 57.0 × 1.0 × (1/60) = 0.95 u/s，约 5-6 帧达到 MaxGroundSpeed（5.0 u/s）

### 2. 地面摩擦（减速）

`V_ground_new = V_ground × pow(1 - GroundFriction, dt × 60)`

当 |V_ground_new| < StopThreshold 时归零。

| 变量 | 符号 | 类型 | 范围 | 描述 |
|------|------|------|------|------|
| 地面摩擦 | GroundFriction | float | 默认 0.15 | 标准化摩擦系数（帧率无关） |
| 停止阈值 | StopThreshold | float | 默认 0.05 u/s | 低于此值速度归零 |

**输出范围**: 趋近于 0.0
**示例**: 以 5.0 u/s 奔跑时松开摇杆：10帧后 = 5.0 × 0.85^10 = 0.98 u/s，约 19帧降至 StopThreshold（0.05）以下

### 3. 跳跃初速度

```
JumpVelocity = sqrt(2 × Gravity × JumpHeight)
AirJumpVelocity = AirJumpForceRatio × JumpVelocity
ShortHopVelocity = ShortHopHeightRatio × JumpVelocity
```

| 变量 | 符号 | 类型 | 范围 | 描述 |
|------|------|------|------|------|
| 重力 | Gravity | float | 默认 32.0 u/s² | 空中向下加速度 |
| 跳跃高度 | JumpHeight | float | 默认 3.5 u | 完整跳跃顶点高度 |
| 地面跳初速度 | JumpVelocity | float | 默认 ~14.97 u/s | 地面跳初始向上速度 |
| 空中跳比例 | AirJumpForceRatio | float | 默认 0.85 | 空中跳力度 = 地面跳的 85% |
| 短跳比例 | ShortHopHeightRatio | float | 默认 0.45 | 短跳高度 = 完整跳的 45% |
| 短跳窗口 | ShortHopWindow | int | 默认 5 帧 | 松开跳跃键可触发短跳的时间窗口 |

**输出范围**: 0 到 ~14.97 u/s（向上）
**示例**: 地面跳 → 14.97 u/s 向上，约 28 帧到达 3.5u 高度（总空中时间约 56 帧）；空中跳 → 12.72 u/s，到达 2.53u

### 4. 重力与快速下落

```
V_vertical_new = V_vertical - Gravity × FastFallMultiplier × dt
V_vertical_new = Max(V_vertical_new, -TerminalVelocity)
```

| 变量 | 符号 | 类型 | 范围 | 描述 |
|------|------|------|------|------|
| 垂直速度 | V_vertical | float | [-TerminalVelocity, +15] | 正=向上，负=向下 |
| 快速下落倍率 | FastFallGravityMultiplier | float | 默认 2.2 | 快速下落时重力倍率 |
| 终端速度 | TerminalVelocity | float | 默认 20.0 u/s | 最大下落速度 |

**输出范围**: -TerminalVelocity 到无上限（初始跳跃速度）
**示例**: 正常下落 10帧: -5.33 u/s；快速下落 10帧: -11.73 u/s；快速下落约 17帧达到终端速度

### 5. 空中控制

`V_air_new = Clamp(V_air + AirAcceleration × input_x × dt, -MaxAirSpeed, MaxAirSpeed)`

| 变量 | 符号 | 类型 | 范围 | 描述 |
|------|------|------|------|------|
| 空中加速度 | AirAcceleration | float | 默认 28.0 u/s² | 空中水平加速度 |
| 最大空中速度 | MaxAirSpeed | float | 默认 3.5 u/s | 空中水平速度上限 |

**输出范围**: -MaxAirSpeed 到 MaxAirSpeed
**示例**: 空中推满方向 8帧 → 3.73 u/s → 钳制到 3.5 u/s（约 7.5帧达到最大空中速度）

### 6. 冲刺速度

`DashSpeed = DashDistance × 60.0 / DashActiveFrames`

| 变量 | 符号 | 类型 | 范围 | 描述 |
|------|------|------|------|------|
| 冲刺距离 | DashDistance | float | 默认 2.5 u | 活跃帧期间总位移（2.5个角色宽度） |
| 冲刺速度 | DashSpeed | float | 默认 25.0 u/s | 活跃帧期间恒定速度 |
| 启动帧 | DashStartupFrames | int | 默认 2 帧 | 开始动作但无位移 |
| 活跃帧 | DashActiveFrames | int | 默认 6 帧 | 实际高速位移 |
| 恢复帧 | DashRecoveryFrames | int | 默认 4 帧 | 可被攻击 |
| 冲刺冷却 | DashCooldownFrames | int | 默认 30 帧 (0.5s) | 防止连续冲刺 |

**输出范围**: 0 到 35+ u/s（默认 25.0）
**示例**: 2.5u 在 6帧内 = 25.0 u/s。总冲刺持续 12帧 (0.2s)，但只在中间 6帧移动

### 7. 摄像机正交尺寸

```
RequiredHalfWidth = (PlayerSpreadX × 0.5 + CamPaddingX) / CamAspectRatio
RequiredHalfHeight = PlayerSpreadY × 0.5 + CamPaddingY
TargetOrthoSize = Max(RequiredHalfHeight, RequiredHalfWidth)
OrthoSize = Clamp(Lerp(CurrentOrthoSize, TargetOrthoSize, CameraSmoothSpeed × dt), MinCamSize, MaxCamSize)
```

| 变量 | 符号 | 类型 | 范围 | 描述 |
|------|------|------|------|------|
| 水平间距 | PlayerSpreadX | float | 0+ u | 最左与最右玩家间距 |
| 垂直间距 | PlayerSpreadY | float | 0+ u | 最上与最下玩家间距 |
| 水平边距 | CamPaddingX | float | 默认 3.0 u | 玩家与屏幕边缘的水平留白 |
| 垂直边距 | CamPaddingY | float | 默认 2.0 u | 玩家与屏幕边缘的垂直留白 |
| 宽高比 | CamAspectRatio | float | 默认 1.78 | 16:9 |
| 平滑速度 | CameraSmoothSpeed | float | 默认 5.0 | Lerp 插值速率 |
| 最小尺寸 | MinCamSize | float | 默认 4.2 u | 1v1 默认缩放 |
| 最大尺寸 | MaxCamSize | float | 默认 8.0 u | 最大缩放上限 |

**输出范围**: MinCamSize 到 MaxCamSize
**示例**: 两玩家 (-3,0) 和 (3,2) → TargetOrthoSize = 3.37 → 钳制到 MinCamSize = 4.2（玩家距离近，保持默认视角）

## Edge Cases

**跳跃与平台**:
- **如果在着陆延迟（Landing）期间按跳跃键**: 忽略跳跃输入。着陆延迟结束前不可跳跃。理由：防止"弹跳"式无限跳跃。
- **如果在二段跳已经用完后按跳跃键**: 忽略。空中跳次数已归零，必须着地后重置。
- **如果在快速下落期间着地**: 正常着陆，快速下落状态结束。着陆延迟与正常下落相同（3帧）。
- **如果角色从平台边缘走出（未跳跃）**: 正常进入 Falling 状态，可使用二段跳。不消耗空中跳次数——离开地面不消耗跳跃。
- **土狼时间（Coyote Time）**: 角色从平台边缘走出的前 `CoyoteTimeFrames`（默认 4 帧）内，仍可执行地面跳（不消耗空中跳次数）。超过窗口后按跳跃键消耗空中跳。
- **跳跃输入缓冲**: 在着陆前 `JumpInputBufferFrames`（默认 3 帧）内按下的跳跃键会被缓冲，着陆延迟结束后自动执行地面跳。缓冲的跳跃在窗口过期后丢弃。

**闪避/冲刺**:
- **如果在冲刺冷却期间按闪避键**: 忽略输入。冷却期间不可再次冲刺。
- **如果空中闪避后再次到达地面**: 冲刺冷却正常计算，不因着地而重置。
- **如果空中闪避活跃帧结束时角色到达地面**: 转入 Landing 状态而非 Falling。着陆延迟正常施加。
- **如果空中闪避期间受击退力影响**: 击退水平速度在闪避期间减半（50%衰减），垂直速度不变。闪避结束后击退力按正常衰减。这使得空中闪避成为回场辅助（降低水平推力）而非无风险解——回场仍有紧张感。
- **如果在冲刺启动帧期间被攻击**: 启动帧期间没有无敌，正常受击，冲刺取消。

**输入**:
- **如果两个方向键同时按下（左+右）**: 优先最后按下的方向。如果同时（同一帧），优先上一次输入的方向。
- **如果跳跃键在短跳窗口内松开但角色已经受击**: 短跳逻辑不生效，角色按击退系统处理。
- **如果输入设备在游戏中断开**: 角色保持当前状态，不自动操作。显示"控制器断开"提示（由对局管理系统处理UI）。
- **方向切换即时生效**: 速度在一帧内反转到目标方向（有意设计，最大化响应性）。这是有意为之，不是bug——与 Pillar 1（秒学秒玩）和核心幻想（即按即动）一致。

**摄像机**:
- **如果所有玩家位于完全相同位置**: OrthoSize = MinCamSize，摄像机中心为该点。
- **如果玩家间距超过 MaxCamSize 能容纳的范围**: 摄像机停在 MaxCamSize，超出屏幕的玩家显示方向指示箭头（由战斗HUD处理）。
- **如果只有 1 个玩家在场（其余被KO）**: 摄像机缩放到 MinCamSize，跟随剩余玩家。

**物理**:
- **如果角色被击退后同时碰到两面墙**: 垂直墙壁停止水平速度，保持垂直速度不变。不会出现"卡墙"。
- **如果角色速度因击退叠加超过任何最大速度上限**: 击退力不受 MaxGroundSpeed/MaxAirSpeed 限制——击退系统拥有最高速度权限。3C 系统在击退结束后才重新施加速度上限。

## Dependencies

3C系统是基础层，无上游依赖。以下是所有下游依赖关系：

| 依赖系统 | 方向 | 类型 | 数据接口 | GDD 状态 |
|---------|------|------|---------|---------|
| 格斗状态机 | 下游（硬依赖） | 查询 + 控制 | `IMovementController`: GetState(), GetPosition(), GetFacing(), IsGrounded(), FreezeMovement(), SetVelocity() | 未设计 |
| 职业系统 | 下游（硬依赖） | 数据注入 | 职业提供: MoveAcceleration, MaxGroundSpeed, JumpForce, JumpHeight, MaxAirJumps, DashCooldownFrames 等 | 未设计 |
| 攻击系统 | 下游（硬依赖） | 控制 | 攻击期间调用 FreezeMovement(true)，攻击结束调用 FreezeMovement(false) | 未设计 |
| 碰撞判定系统 | 下游（硬依赖） | 查询 | 提供地面检测: IsOnPlatform(), IsOnGround()；平台碰撞体定义着陆判定 | 未设计 |
| 击退与击飞系统 | 下游（硬依赖） | 控制 | 调用 SetVelocity(knockbackVector) 覆盖当前速度；击退结束后 3C 重新接管 | 未设计 |
| 专注值系统 | 下游（软依赖） | 无直接交互 | 通过格斗状态机间接关联 | 未设计 |
| 技能附属物系统 | 下游（软依赖） | 修改 | 通过 ModifySpeed(multiplier) 临时修改移动参数 | 未设计 |

**3C 系统向上提供的接口契约**:
- `IMovementController` 接口是所有下游系统与 3C 交互的唯一入口
- 移动状态枚举: `MovementState { Idle, Running, Jumping, Falling, FastFalling, Dashing, AirDodging, Landing, PlatformDrop }`
- 面朝方向: `FacingDirection { Left = -1, Right = 1 }`

## Tuning Knobs

| 旋钮名 | 默认值 | 安全范围 | 调高效果 | 调低效果 | 所属公式 |
|--------|--------|---------|---------|---------|---------|
| MoveAcceleration | 57.0 u/s² | 30–100 | 加速更快，手感更灵敏 | 加速更慢，感觉"笨重" | 地面速度 |
| MaxGroundSpeed | 5.0 u/s | 3.0–8.0 | 奔跑更快，节奏更快 | 奔跑更慢，节奏更慢 | 地面速度 |
| GroundFriction | 0.15 | 0.05–0.35 | 停止更快，更精准 | 滑行更远，更有惯性 | 地面摩擦 |
| StopThreshold | 0.05 u/s | 0.01–0.2 | 几乎立即停止 | 需要更多时间滑到停止 | 地面摩擦 |
| Gravity | 32.0 u/s² | 20–50 | 跳跃更短更重，下落更快 | 跳跃更高更"飘"，下落更慢 | 重力/跳跃 |
| JumpHeight | 3.5 u | 2.0–5.0 | 跳得更高 | 跳得更低 | 跳跃初速度 |
| AirJumpForceRatio | 0.85 | 0.6–1.0 | 空中跳更高 | 空中跳更低 | 跳跃初速度 |
| ShortHopHeightRatio | 0.45 | 0.3–0.6 | 短跳更高 | 短跳更低 | 跳跃初速度 |
| ShortHopWindow | 5 帧 | 3–10 帧 | 短跳更容易触发 | 短跳更难触发 | 跳跃初速度 |
| FastFallGravityMultiplier | 2.2 | 1.5–3.5 | 快速下落更快更明显 | 快速下落不太明显 | 重力 |
| TerminalVelocity | 20.0 u/s | 12–30 | 最高下落速度更快 | 最高下落速度更慢 | 重力 |
| AirAcceleration | 28.0 u/s² | 15–50 | 空中转向更快 | 空中转向更慢 | 空中控制 |
| MaxAirSpeed | 3.5 u/s | 2.0–6.0 | 空中水平速度更快 | 空中水平速度更慢 | 空中控制 |
| DashDistance | 2.5 u | 1.5–4.0 | 冲刺更远 | 冲刺更近 | 冲刺 |
| DashStartupFrames | 2 帧 | 0–4 帧 | 冲刺启动更快 | 冲刺启动更慢 | 冲刺 |
| DashActiveFrames | 6 帧 | 4–10 帧 | 冲刺移动时间更长 | 冲刺移动时间更短 | 冲刺 |
| DashRecoveryFrames | 4 帧 | 2–8 帧 | 恢复期更长（更易被反击） | 恢复期更短（更安全） | 冲刺 |
| DashCooldownFrames | 30 帧 | 15–60 帧 | 冷却更短（可更频繁闪避） | 冷却更长（闪避更谨慎） | 冲刺 |
| LandingLagFrames | 3 帧 | 1–6 帧 | 着陆恢复更快 | 着陆延迟更明显 | 着陆 |
| MinCamSize | 4.2 u | 3.0–6.0 | 默认视角更远 | 默认视角更近 | 摄像机 |
| MaxCamSize | 8.0 u | 6.0–12.0 | 最大缩放更大（可容纳更远玩家） | 最大缩放更小 | 摄像机 |
| CamPaddingX | 3.0 u | 1.0–5.0 | 水平留白更大 | 水平留白更小 | 摄像机 |
| CamPaddingY | 2.0 u | 1.0–4.0 | 垂直留白更大 | 垂直留白更小 | 摄像机 |
| CameraSmoothSpeed | 5.0 | 2.0–15.0 | 摄像机跟踪更快 | 摄像机跟踪更平滑 | 摄像机 |
| WalkSpeedRatio | 0.4 | 0.2–0.6 | 步行速度更快 | 步行速度更慢 | 动画驱动 |
| FastFallInitiationSpeed | 5.0 u/s | 2.0–8.0 | 快速下落瞬间响应更强 | 快速下落瞬间响应更弱 | 快速下落 |
| CoyoteTimeFrames | 4 帧 | 2–8 帧 | 离开边缘后跳跃窗口更长 | 窗口更短，需要更精确 | 着陆/跳跃 |
| JumpInputBufferFrames | 3 帧 | 0–6 帧 | 着陆前跳跃缓冲更大 | 缓冲更小或不缓冲 | 着陆/跳跃 |
| PlatformDropInputWindow | 3 帧 | 1–6 帧 | 穿越平台输入窗口更宽松 | 窗口更严格 | 平台交互 |

**旋钮交互警告**:
- `MoveAcceleration` 和 `MaxGroundSpeed` 相互影响：加速到极限的时间 = MaxGroundSpeed / MoveAcceleration
- `Gravity` 和 `JumpHeight` 共同决定跳跃手感：改一个必须检查另一个
- `DashDistance` 和 `DashActiveFrames` 决定 DashSpeed：改距离或时间都会改变冲刺速度

## Visual/Audio Requirements

**移动视觉反馈**:
- 角色奔跑时，身体有轻微的拉伸/倾斜效果（运动模糊感），倾斜角度与速度成正比
- 跳跃起跳时有小幅度"压缩→拉伸"的 squash & stretch 动画（3-4帧）
- 着陆时有压缩效果（squash），持续 LandingLagFrames 时长
- 快速下落时角色有拖尾效果（简单的残影或线条）
- 冲刺时角色有运动模糊/拖尾效果，启动帧有尘土粒子

**摄像机视觉**:
- 摄像机缩放时无突兀跳变——平滑插值保证过渡自然
- 击飞KO时摄像机可能有短暂的缩放效果（由对局管理系统控制，不在此系统范围内）

**音频反馈**（由音效系统实现，此处仅定义触发事件）:
- 跳跃起跳音效事件：`OnJump`
- 着陆音效事件：`OnLand`
- 冲刺启动音效事件：`OnDashStart`
- 平台穿越音效事件：`OnPlatformDrop`

## UI Requirements

3C 系统不直接产生 UI 元素。以下信息由战斗HUD使用 3C 提供的数据显示：
- 角色位置和状态 → 由 HUD 系统查询 `IMovementController.GetPosition()` 和 `GetState()`
- 摄像机行为直接影响所有 UI 元素的屏幕定位

无独立的 UI 需求。

## Acceptance Criteria

### 输入处理
- **GIVEN** 一个已连接的手柄，**WHEN** 玩家将摇杆推到最右，**THEN** input_x = 1.0（±0.01）
- **GIVEN** 摇杆静止在 dead zone 内，**WHEN** 系统读取输入，**THEN** input_x = 0.0

### 地面移动
- **GIVEN** 角色静止在地面，**WHEN** 玩家按住右方向 10 帧，**THEN** 角色水平速度达到 MaxGroundSpeed（5.0 u/s ± 0.1）
- **GIVEN** 角色以 MaxGroundSpeed 奔跑，**WHEN** 松开方向键，**THEN** 15 帧内速度降至 StopThreshold 以下
- **GIVEN** 角色以最大速度向右奔跑，**WHEN** 玩家立即按下左方向，**THEN** 无转身锁定帧，立即开始向左加速

### 跳跃
- **GIVEN** 角色站在地面上，**WHEN** 按下跳跃键，**THEN** 角色获得 JumpVelocity（~14.97 u/s）向上初速度
- **GIVEN** 角色在空中（已用地面跳），**WHEN** 按下跳跃键，**THEN** 获得空中跳初速度（~12.72 u/s），空中跳次数减 1
- **GIVEN** 角色在空中（空中跳已用完），**WHEN** 按下跳跃键，**THEN** 无响应，不消耗输入
- **GIVEN** 角色刚起跳（在 ShortHopWindow 内），**WHEN** 松开跳跃键，**THEN** 垂直速度截断为 ShortHopVelocity（~6.73 u/s）
- **GIVEN** 角色从地面跳到达顶点，**THEN** 跳跃高度 = JumpHeight（3.5u ± 0.1），到达顶点帧数 ≈ 28帧（±2帧）

### 快速下落
- **GIVEN** 角色在空中下落阶段，**WHEN** 按住下键（Y < -0.5），**THEN** 重力倍率提升至 FastFallGravityMultiplier（2.2x），下落加速度明显加快
- **GIVEN** 角色快速下落中，**WHEN** 达到 TerminalVelocity（20.0 u/s），**THEN** 下落速度不再增加

### 闪避/冲刺
- **GIVEN** 角色站在地面且冲刺冷却完毕，**WHEN** 按下闪避键，**THEN** 角色在 2帧启动后以 DashSpeed（25.0 u/s）冲刺 6帧，随后 4帧恢复
- **GIVEN** 冲刺活跃帧期间，**THEN** 前 2帧角色有无敌判定（帧 3-4，从输入帧计数），帧 5-8 可被攻击
- **GIVEN** 冲刺刚结束，**WHEN** 在 DashCooldownFrames（30帧）内再次按闪避键，**THEN** 无响应
- **GIVEN** 角色在空中，**WHEN** 按下闪避键，**THEN** 执行空中闪避（保留空中惯性）

### 平台交互
- **GIVEN** 角色站在平台上，**WHEN** 同时按下下方向+跳跃，**THEN** 角色向下穿越平台
- **GIVEN** 角色从空中落到平台上，**THEN** 着陆延迟 = LandingLagFrames（3帧），期间不可跳跃

### 摄像机
- **GIVEN** 2 个玩家在场，**WHEN** 两者距离较近（spread < 3u），**THEN** 摄像机 OrthoSize = MinCamSize（4.2u）
- **GIVEN** 2 个玩家在场，**WHEN** 两者距离拉开，**THEN** 摄像机平滑缩放，不超过 MaxCamSize（8.0u）
- **GIVEN** 摄像机位置计算完成，**THEN** 不显示超出场地边界的区域

### 多人输入
- **GIVEN** 2 个手柄已连接，**WHEN** 玩家1 和玩家2 同时操作，**THEN** 各自的输入互不干扰，角色独立响应

### 性能
- **GIVEN** 2 人对战进行中，**THEN** 3C 系统（输入+移动+摄像机）帧耗时 < 2ms，不造成帧率下降

## Open Questions

1. **墙壁跳是否纳入 MVP？** 当前设计中未包含墙壁跳，但平台格斗中墙壁跳是常见机制。决定：MVP 不包含，后续版本考虑。（Owner: 设计师，里程碑: Alpha）
2. **角色边缘抓挂（Edge Grab）是否纳入？** 大乱斗的边缘抓挂是核心机制，但增加实现复杂度。决定：MVP 不包含。（Owner: 设计师，里程碑: VS）
3. **冲刺方向是否改为可操控？** 当前冲刺固定为面朝方向。部分平台格斗允许在冲刺启动帧期间改变方向。决定：保持固定方向，简化操控。（Owner: 设计师，里程碑: 原型验证后）
