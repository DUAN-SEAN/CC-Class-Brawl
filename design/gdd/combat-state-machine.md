# 格斗状态机 (Combat State Machine)

> **Status**: In Design
> **Author**: SeanDuan + agents
> **Last Updated**: 2026-05-23
> **Implements Pillar**: Pillar 1: 秒学秒玩, Pillar 4: 快速战斗

## Overview

格斗状态机是职业对决的战斗行为调度中心，负责管理角色在战斗中的所有状态（待机、攻击、受击、击飞、倒地等）以及它们之间的转换规则和优先级。它是一个与 3C 移动状态机并行运行的独立状态层——3C 管理物理移动（Idle、Running、Jumping 等），格斗状态机管理战斗行为（Idle、Attacking、HitStun、Knockback 等），两者通过 `IMovementController` 接口协调：当角色进入攻击状态时，格斗状态机冻结 3C 移动；当受到击退时，格斗状态机委托 3C 施加物理力。玩家的每一次按键——攻击、闪避、技能——都先经过格斗状态机判断"当前是否允许执行"，只有通过的状态转换才会触发实际的战斗行为。没有它，角色无法区分"站着不动"和"正在攻击后摇中"，无法处理受击打断，无法支持随机技能注入新的战斗状态。对于玩家而言，格斗状态机决定的是"我的输入是否有响应"的直觉感受——好的状态机让玩家觉得"我想做什么就能做什么"，差的状态机让玩家觉得"我按了但没反应"。

## Player Fantasy

**核心幻想：「我的输入永远有回应」**

玩家应该感觉角色是一个高度训练的战士——每一个指令都有清晰的回应。按攻击就攻击，被打了就硬直，攻击完了就能动。没有任何"我明明按了为什么没反应"的困惑时刻——如果输入被忽略，玩家必须立刻知道为什么（比如正在受击硬直中，角色有明显的受击动画）。

**关键情感时刻**：
- **流畅进攻** — 从地面攻击接跳跃攻击接技能，每个输入无缝衔接，感觉"停不下来"
- **被惩罚的清晰感** — 被击中时短暂硬直，玩家清楚知道"我被抓住了"，而不是"游戏卡了"
- **精确取消** — 在攻击恢复帧中按闪避成功取消，时机的精确感是高手与菜鸟的分界线
- **随机技能的惊喜融入** — 新解锁的技能不是替换整个战斗方式，而是自然地"加入"当前状态流转

**支柱对齐**：
- 服务 **Pillar 1: 秒学秒玩** — 状态规则直觉化：能打就打，被打就停，打完就动
- 服务 **Pillar 3: 高手菜鸟都开心** — 基础状态流转简单直觉，但取消规则和优先级系统为高手提供深度
- 服务 **Pillar 4: 快速战斗** — 无长时间锁定状态，战斗节奏紧凑

## Detailed Design

### Core Rules

**1. 双机并行架构**

格斗状态机与 3C 移动状态机是两个独立的状态机，并行运行在同一角色上：
- **3C 状态机** 管理物理移动：`MovementState {Idle, Running, Jumping, Falling, FastFalling, Dashing, AirDodging, Landing, PlatformDrop}`
- **格斗状态机** 管理战斗行为：`CombatState {Idle, Attacking, HitStun, Knockback}`
- 两者通过 `IMovementController` 接口协调

**控制权规则**：
- `CombatState = Idle` → 3C 完全控制移动，战斗状态机仅监听攻击输入
- `CombatState = Attacking` → 格斗状态机冻结 3C 移动（调用 `FreezeMovement(true)`），攻击结束后释放
- `CombatState = HitStun` → 格斗状态机冻结 3C 移动，角色播放受击动画
- `CombatState = Knockback` → 格斗状态机委托 3C 施加击退物理力（通过 `SetVelocity()`），3C 负责物理运动

**2. 攻击输入处理**

1. 攻击输入进入格斗状态机的**输入缓冲**（`InputBufferFrames` = 8 帧），而非立即执行
2. 每帧，格斗状态机检查缓冲中的输入是否可以被当前状态接受
3. 输入解释依赖 3C 的 `MovementState` 和 `IsGrounded()` 查询：
   - 地面 + Idle/Running → 地面攻击
   - 空中（Jumping/Falling/FastFalling）→ 空中攻击
   - 冲刺中（Dashing）→ 冲刺攻击
4. 攻击输入优先级：特殊攻击 > 空中攻击 > 地面攻击（由技能系统决定具体映射）

**3. 三阶段攻击帧结构**

每个攻击状态分为三个帧阶段：

| 阶段 | 描述 | 可被取消？ | 移动？ |
|------|------|-----------|--------|
| **Startup**（启动帧） | 攻击动作开始，hitbox 尚未激活 | 仅被 HitStun 强制取消 | 冻结 |
| **Active**（活跃帧） | hitbox 已激活，可命中对手 | 仅被 HitStun 强制取消 | 冻结 |
| **Recovery**（恢复帧） | hitbox 关闭，角色收招 | 可取消到 Attacking（新攻击）、Dash、Jump（见取消表） | 冻结 |

帧计数方式：从攻击输入被接受的帧开始计数（帧 0 = Startup 第 1 帧）。每个攻击的帧数据由攻击系统/技能系统定义。

**4. 取消规则**

格斗状态机采用**单强制优先级 + 可配置取消表**的模式：

**强制取消（不可覆盖）**：
- `HitStun` 可以打断任何其他战斗状态。这是唯一的硬编码优先级。
- 受击时，当前攻击状态立即终止，hitbox 关闭，角色进入 HitStun。

**可配置取消表**（数据驱动，由攻击/技能系统定义）：
- 每个攻击在 Recovery 阶段有一个 `CancelTable`，列出可以取消到哪些状态
- 取消条件包括：目标状态类型（攻击/闪避/跳跃/技能）、输入要求、帧窗口
- MVP 默认取消规则：Recovery 可取消到任何新的攻击或 Dash

**取消优先级**（取消表内多个条件同时满足时）：
1. 技能攻击（由技能系统注入）
2. 基础攻击
3. 闪避/跳跃（恢复到 3C 控制）

**5. 状态扩展接口**

格斗状态机必须支持通过技能系统动态添加新状态：
- 新技能可以注册新的 `CombatState`（如"火球术"作为一个新的攻击子状态）
- 新状态遵循相同的帧结构（Startup → Active → Recovery）
- 新状态遵循相同的取消规则（可被 HitStun 强制取消，Recovery 可配置取消目标）
- 通过 `ICombatStateProvider` 接口注册新状态，不需要修改核心 FSM 代码

**6. HitStun 与 Knockback 的区分**

- **HitStun**：被攻击命中后的短暂硬直。持续时间由攻击数据定义（`HitStunFrames`）。HitStun 结束后回到 `CombatIdle`。
- **Knockback**：被高力度攻击命中后，角色被击退。Knockback 包含一段不可操作的 hitstun 期（`KnockbackHitstunFrames`），之后角色可以操作（可攻击、可跳跃、可用技能），但物理运动持续直到被 3C 正常接管。
- 判定规则：如果攻击产生的击退力度 > `KnockbackThreshold`，进入 Knockback；否则进入 HitStun。

### States and Transitions

**战斗状态表**：

| 当前状态 | 触发条件 | 目标状态 | 备注 |
|---------|---------|---------|------|
| Idle | 攻击输入被接受（缓冲匹配） | Attacking.Startup | 查询 3C MovementState 确定攻击类型 |
| Idle | 受击（hitbox 判定命中） | HitStun 或 Knockback | 由击退力度决定 |
| Attacking.Startup | 当前帧 = StartupFrames | Attacking.Active | 自动推进 |
| Attacking.Startup | 受击 | HitStun 或 Knockback | 强制取消，hitbox 关闭 |
| Attacking.Active | 当前帧 = StartupFrames + ActiveFrames | Attacking.Recovery | 自动推进 |
| Attacking.Active | 受击 | HitStun 或 Knockback | 强制取消，hitbox 关闭 |
| Attacking.Active | 命中对手 | 继续当前阶段 | 不打断自身（hitstop 由攻击系统处理） |
| Attacking.Recovery | 当前帧 = StartupFrames + ActiveFrames + RecoveryFrames | Idle | 自然结束 |
| Attacking.Recovery | 取消表匹配的输入 | 目标取消状态 | 可取消到新攻击/Dash/技能 |
| Attacking.Recovery | 受击 | HitStun 或 Knockback | 强制取消 |
| HitStun | HitStunFrames 耗尽 | Idle | 自动恢复 |
| HitStun | 受击（再次被击中） | HitStun 或 Knockback | 可叠加连击 |
| Knockback | KnockbackHitstunFrames 耗尽 | Idle（可操作） | 物理运动可能仍在继续 |
| Knockback | 着地 | Idle | 着地时击退状态结束 |

**状态优先级**（同一帧多个触发时）：
1. 受击（HitStun/Knockback） — 最高，不可被覆盖
2. 攻击推进（阶段自动推进）
3. 取消表触发（Recovery 阶段的输入取消）
4. 自然结束（帧计数到头）

### Interactions with Other Systems

| 系统 | 数据流方向 | 接口描述 |
|------|-----------|---------|
| 3C系统 | FSM → 3C | `IMovementController.FreezeMovement(bool)` — 攻击/受击期间冻结移动 |
| 3C系统 | FSM → 3C | `IMovementController.SetVelocity(Vector2)` — Knockback 时施加击退速度 |
| 3C系统 | 3C → FSM | `IMovementController.GetState()` — 查询移动状态以确定攻击类型（地面/空中） |
| 3C系统 | 3C → FSM | `IMovementController.IsGrounded()` — 判断是否在地面 |
| 3C系统 | 3C → FSM | `IMovementController.GetFacing()` — 获取面朝方向 |
| 攻击系统 | 攻击 → FSM | 提供 `AttackData`（帧数据、hitbox 定义、取消表） |
| 攻击系统 | FSM → 攻击 | 当前攻击阶段（Startup/Active/Recovery）和当前帧数 |
| 碰撞判定系统 | 碰撞 → FSM | 命中事件：`OnHitReceived(attacker, attackData, hitPoint)` — 触发 HitStun/Knockback |
| 伤害计算系统 | 伤害 → FSM | 提供击退力度值 — 用于判断 HitStun vs Knockback |
| 击退与击飞系统 | 击退 → FSM | 提供击退向量 — FSM 委托 3C 施加 |
| 专注值系统 | 专注值 → FSM | 无直接交互（通过碰撞判定系统间接关联） |
| 技能装备管理 | 技能 → FSM | `ICombatStateProvider.RegisterState(stateDefinition)` — 注入新技能状态 |
| 技能装备管理 | FSM → 技能 | 当前战斗状态查询 — 技能系统判断技能是否可用 |
| AI对手 | AI → FSM | 读取当前 CombatState 用于决策 |
| 对局管理系统 | 对局 → FSM | 无直接交互（通过击退与击飞系统间接关联） |

## Formulas

**单位系统**: 帧数以 60Hz 固定时间步为基准（1 帧 = 1/60 秒 ≈ 16.6ms）。所有帧计数从 0 开始。

### 1. HitStun 持续时间

`HitStunDuration = HitStunFrames`

| Variable | Symbol | Type | Range | Description |
|----------|--------|------|-------|-------------|
| 受击硬直帧数 | HitStunFrames | int | 1–60 | 攻击数据定义的硬直帧数（由攻击系统提供） |
| HitStun 持续时间 | HitStunDuration | int | 1–60 | HitStun 状态持续的总帧数 |

**Output Range:** 1 到 60 帧（1 帧 = 最轻微 jab，60 帧 ≈ 1 秒 = 重击上限）
**Example:** 轻拳 Jab: HitStunFrames = 8 帧（~133ms）；重拳 Smash: HitStunFrames = 25 帧（~417ms）

### 2. Knockback Hitstun 期

```
KnockbackHitstunFrames = Floor(BaseKnockbackHitstun + KnockbackHitstunGrowth × KnockbackMagnitude)
KnockbackHitstunFrames = Min(KnockbackHitstunFrames, KnockbackHitstunCap)
```

| Variable | Symbol | Type | Range | Description |
|----------|--------|------|-------|-------------|
| 基础击退硬直 | BaseKnockbackHitstun | int | 2–5 帧 | 最轻微击退的最小不可操作帧数 |
| 击退硬直增长率 | KnockbackHitstunGrowth | float | 0.3–0.8 帧/单位 | 每单位击退力度增加的硬直帧数 |
| 击退力度 | KnockbackMagnitude | float | 0.0+ | 击退力度值（由击退与击飞系统传入） |
| 击退硬直上限 | KnockbackHitstunCap | int | 40–60 帧 | 硬直帧数上限 |
| 击退不可操作帧数 | KnockbackHitstunFrames | int | 2–KnockbackHitstunCap | Knockback 状态的不可操作期 |

**Output Range:** BaseKnockbackHitstun（下限）到 KnockbackHitstunCap（上限），整数帧数
**Example:** 轻微击退（5.0）: Floor(3 + 0.5 × 5.0) = 5 帧；中等击退（15.0）: 10 帧；重击（40.0）: 23 帧；极端（100.0）: Min(53, 50) = 50 帧（触及上限）

### 3. 输入缓冲有效性

```
BufferAge = CurrentFrame - InputRecordedFrame
IsBufferValid = BufferAge <= InputBufferFrames AND BufferAge >= 0
ShouldExecute = IsBufferValid AND CurrentStateAccepts(InputType)
ShouldDiscard = BufferAge > InputBufferFrames OR ShouldExecute
```

| Variable | Symbol | Type | Range | Description |
|----------|--------|------|-------|-------------|
| 当前帧 | CurrentFrame | int | 0+ | 全局帧编号 |
| 输入记录帧 | InputRecordedFrame | int | 0+ | 输入被记录时的帧编号 |
| 缓冲窗口 | InputBufferFrames | int | 8 | 缓冲有效窗口大小 |
| 缓冲年龄 | BufferAge | int | 0+ | 输入等待的帧数 |
| 是否有效 | IsBufferValid | bool | — | 输入是否在有效窗口内 |
| 是否执行 | ShouldExecute | bool | — | 输入是否应在本帧执行 |
| 是否丢弃 | ShouldDiscard | bool | — | 输入是否应从缓冲移除 |

**Output Range:** 三个布尔值判定
**Example:** 帧 100 按攻击，帧 103 检查: BufferAge=3, Valid=true；帧 109 检查: BufferAge=9, Valid=false（过期）；帧 105 Recovery 中且取消表允许: Execute=true, Discard=true

### 4. Knockback vs HitStun 判定

```
if KnockbackMagnitude > KnockbackThreshold:
    NextState = Knockback, HitstunDuration = KnockbackHitstunFrames (公式2)
else:
    NextState = HitStun, HitstunDuration = HitStunFrames (公式1)
```

| Variable | Symbol | Type | Range | Description |
|----------|--------|------|-------|-------------|
| 击退力度 | KnockbackMagnitude | float | 0.0+ | 攻击产生的击退力度（击退与击飞系统传入） |
| 判定阈值 | KnockbackThreshold | float | 9.0 (校准值) | 力度超过此值进入 Knockback，否则 HitStun |
| 目标状态 | NextState | enum | {HitStun, Knockback} | 命中后的战斗状态 |

**Output Range:** 一个状态枚举 + 一个持续时间
**Example:** Jab 命中（力度 2.0 < 9.0）→ HitStun, 8 帧；Smash 命中（力度 25.0 > 9.0）→ Knockback, 15 帧

### 5. 取消窗口有效期

```
CancelStartFrame = StartupFrames + ActiveFrames
CancelEndFrame = CancelStartFrame + CancelWindowFrames - 1
IsCancelWindowActive = CancelStartFrame <= CurrentAttackFrame <= CancelEndFrame
```

| Variable | Symbol | Type | Range | Description |
|----------|--------|------|-------|-------------|
| 启动帧数 | StartupFrames | int | 1+ | 攻击启动帧数（攻击数据提供） |
| 活跃帧数 | ActiveFrames | int | 1+ | 攻击活跃帧数（攻击数据提供） |
| 取消窗口帧数 | CancelWindowFrames | int | 1–RecoveryFrames | Recovery 中可取消的帧数 |
| 当前攻击帧 | CurrentAttackFrame | int | 0–TotalFrames | 攻击开始后经过的帧数 |
| 取消窗口起始 | CancelStartFrame | int | StartupFrames + ActiveFrames | 取消窗口第一帧 |
| 取消窗口结束 | CancelEndFrame | int | CancelStartFrame + CancelWindowFrames - 1 | 取消窗口最后一帧 |

**Output Range:** 布尔值 — 当前帧是否在取消窗口内
**Example:** Jab (4/3/8), CancelWindowFrames=5: CancelStartFrame=7, CancelEndFrame=11. 帧 7-11 可取消，帧 12-14 不可（自然结束回 Idle）

### 6. 攻击阶段推进

```
if CurrentAttackFrame < StartupFrames: Phase = Startup
elif CurrentAttackFrame < StartupFrames + ActiveFrames: Phase = Active
elif CurrentAttackFrame < StartupFrames + ActiveFrames + RecoveryFrames: Phase = Recovery
else: Phase = End → 转换到 Idle
```

| Variable | Symbol | Type | Range | Description |
|----------|--------|------|-------|-------------|
| 当前攻击帧 | CurrentAttackFrame | int | 0+ | 攻击开始后的帧计数 |
| 启动帧数 | StartupFrames | int | 1+ | 攻击启动帧数 |
| 活跃帧数 | ActiveFrames | int | 1+ | 攻击活跃帧数 |
| 恢复帧数 | RecoveryFrames | int | 1+ | 攻击恢复帧数 |
| 当前阶段 | Phase | enum | {Startup, Active, Recovery, End} | 攻击当前所处阶段 |

**Output Range:** 一个阶段枚举值
**Example:** Jab (4/3/8, 总 15 帧): 帧 0-3 → Startup, 帧 4-6 → Active, 帧 7-14 → Recovery, 帧 15 → End → Idle

## Edge Cases

**HitStun 相关**:
- **如果角色在 HitStun 中再次被击中**: 重置 HitStun 计时器，以新攻击的 `HitStunFrames` 为准（不叠加，替换）。如果是 Knockback 级别的击退，进入 Knockback 状态。
- **如果 HitStunFrames = 0（数据错误）**: 强制最少 1 帧。0 帧硬直意味着"命中了但没反馈"，违反核心体验。
- **如果角色在 HitStun 结束的同一帧被攻击命中**: 正常进入新的 HitStun/Knockback。状态优先级中"受击"最高，HitStun → Idle → HitStun 的转换在同一帧内完成。

**Knockback 相关**:
- **如果 KnockbackHitstunFrames 计算结果超过 KnockbackHitstunCap**: 钳制到 KnockbackHitstunCap。防止无限连击。
- **如果角色在 Knockback 可操作期间再次被击中**: 正常判定 HitStun/Knockback（基于新的击退力度）。击退可以叠加——新的击退速度覆盖当前物理速度。
- **如果角色在 Knockback 状态着地**: Knockback 状态结束，转入 3C 的 Landing 状态（3 帧着陆延迟），同时 CombatState 回到 Idle。着地取消了飞行中的击退惯性。
- **如果 KnockbackMagnitude 恰好等于 KnockbackThreshold**: 进入 HitStun（`>` 严格大于判定）。阈值处不进入 Knockback。这是有意设计——Knockback 是"明显被击飞"的体验，阈值处应该更接近"被击中硬直"。
- **如果角色在 Knockback 中飞出 blast zone**: 由对局管理系统处理 KO 判定，格斗状态机收到 KO 事件后停止所有状态更新。

**攻击帧相关**:
- **如果角色在 Attacking.Startup 中被击中**: 攻击立即取消，hitbox 关闭，进入 HitStun/Knockback。Startup 阶段没有无敌，攻击者承担风险。
- **如果角色在 Attacking.Active 中被击中**: 同 Startup——攻击取消，hitbox 关闭。没有"交易击"（trade）机制——先命中者胜出。理由：MVP 保持简单，交易击增加实现复杂度且难以让新手理解。
- **如果同一帧两个角色互相命中**: 由碰撞判定系统决定命中优先级（通常由攻击 ID 或玩家编号排序）。格斗状态机只处理收到的命中事件，不做额外的"同时命中"判定。
- **如果攻击帧数据总和为 0（Startup=0, Active=0, Recovery=0）**: 视为数据错误，忽略该攻击输入。任何有效攻击必须至少有 1 帧总时长。

**输入缓冲相关**:
- **如果缓冲中有多个输入（同一帧按了攻击+技能）**: 按取消优先级处理（技能 > 攻击）。只有优先级最高的输入被接受，其余丢弃。
- **如果 BufferAge < 0（帧序号错误）**: 丢弃该输入。正常的 BufferAge 不可能为负——如果出现，说明存在帧计数同步错误。
- **如果 InputBufferFrames = 0**: 无缓冲，所有攻击必须精确在可接受的帧按下。不推荐但技术上允许（给高手模式预留）。

**取消相关**:
- **如果取消窗口帧数 > RecoveryFrames**: 钳制为 RecoveryFrames（整个 Recovery 都可取消）。
- **如果取消目标状态的条件不满足（如尝试空中攻击但已在地面）**: 取消被拒绝，输入保留在缓冲中继续等待，直到窗口过期。
- **如果技能系统注册的新状态被 HitStun 打断**: 与基础攻击相同——新技能的 hitbox 关闭，状态重置。技能系统需要处理技能的"被打断"回调。

**状态扩展相关**:
- **如果技能系统注册了重复的状态名称**: 忽略后注册的（先注册优先）。在日志中记录警告。
- **如果注册的新状态没有提供帧数据**: 拒绝注册。所有状态必须提供完整的帧结构。

## Dependencies

| 依赖系统 | 方向 | 类型 | 数据接口 | GDD 状态 |
|---------|------|------|---------|---------|
| 3C系统 | 上游（硬依赖） | 查询 + 控制 | `IMovementController`: GetState(), IsGrounded(), GetFacing(), FreezeMovement(), SetVelocity() | In Review |
| 攻击系统 | 上游（硬依赖） | 数据注入 | 提供 `AttackData`（帧数据: StartupFrames/ActiveFrames/RecoveryFrames, HitStunFrames, CancelTable） | 未设计 |
| 碰撞判定系统 | 上游（硬依赖） | 事件 | `OnHitReceived(attacker, attackData, hitPoint)` — 触发 HitStun/Knockback | 未设计 |
| 伤害计算系统 | 上游（硬依赖） | 数据查询 | 提供击退力度值（KnockbackMagnitude）用于 HitStun/Knockback 判定 | 未设计 |
| 击退与击飞系统 | 上游（硬依赖） | 数据查询 | 提供击退向量（KnockbackVector）用于委托 3C 施加物理力 | 未设计 |
| 职业系统 | 上游（软依赖） | 数据注入 | 职业可能提供基础战斗属性（如有，由职业 GDD 定义） | 未设计 |
| 技能装备管理 | 下游（硬依赖） | 扩展接口 | `ICombatStateProvider.RegisterState(stateDefinition)` — 技能注入新战斗状态 | 未设计 |
| AI对手 | 下游（软依赖） | 只读查询 | 读取当前 CombatState 用于 AI 决策 | 未设计 |

**向上提供的接口契约**:
- `CombatState` 枚举: `{Idle, Attacking, HitStun, Knockback}`
- `AttackPhase` 枚举: `{Startup, Active, Recovery}`
- 查询接口: `GetCurrentState()`, `GetCurrentAttackPhase()`, `GetCurrentAttackFrame()`, `CanAcceptInput()`
- 事件: `OnCombatStateChanged(CombatState from, CombatState to)`, `OnAttackPhaseChanged(AttackPhase phase)`

## Tuning Knobs

| 旋钮名 | 默认值 | 安全范围 | 调高效果 | 调低效果 | 所属公式 |
|--------|--------|---------|---------|---------|---------|
| InputBufferFrames | 8 帧 | 0–12 帧 | 输入更宽松，新手友好 | 输入更严格，需要精确时机 | 输入缓冲 |
| BaseKnockbackHitstun | 3 帧 | 2–5 帧 | 最轻微击退也有明显硬直 | 最轻微击退几乎无硬直 | Knockback Hitstun |
| KnockbackHitstunGrowth | 0.5 帧/单位 | 0.3–0.8 | 高力度击退的硬直更长，连击更易 | 高力度击退的硬直更短，被击中方更容易恢复 | Knockback Hitstun |
| KnockbackHitstunCap | 50 帧 | 40–60 帧 | 最高硬直更长，极端击退更致命 | 最高硬直更短，给被击中方更多恢复机会 | Knockback Hitstun |
| KnockbackThreshold | 9.0 (校准值) | 3.0–10.0 | 更多攻击触发 Knockback（更容易被击飞） | 更少攻击触发 Knockback（更多停留在 HitStun） | Knockback vs HitStun 判定 |

**旋钮交互警告**:
- `KnockbackHitstunGrowth` 和 `KnockbackHitstunCap`: 增长率越高，越容易触及上限。调整一个时必须检查另一个。
- `KnockbackThreshold` 已由击退与击飞系统 GDD 校准为 9.0。后续调整需同步更新 knockback-launch-system.md 和 registry。
- `InputBufferFrames` 直接影响取消手感——较大的缓冲窗口让 Recovery 取消更容易，但也会让"过早按下"的输入意外执行。

## Visual/Audio Requirements

**视觉反馈（MVP 核心层）**:

**攻击阶段视觉区分**:
- **Startup**: 角色轮廓微弱发光（职业色），蓄力感。发光强度在 Startup 期间线性增强。
- **Active**: 轮廓光达到最亮 + 简单几何形状挥击特效（弧线/矩形，跟随职业色）。hitbox 激活的视觉标志。
- **Recovery**: 轮廓光快速衰减至消失。无额外特效。
- **hitstop 视觉表现**（数值由攻击系统定义）: 命中瞬间全局时间冻结 + 屏幕微震（振幅 1-2 像素，1-2 帧）+ 命中点能量爆发特效（简单几何扩展圆）。hitstop 期间角色保持 Active 阶段姿势冻结。

**受击（HitStun）视觉反馈**:
- 角色轮廓闪烁白色（频率：每 2 帧交替），持续 HitStun 全程
- 角色轻微后仰（动画驱动，非物理）
- 最短 HitStun（1-3 帧）时闪烁可能只发生一次——这是可接受的

**击退（Knockback）视觉反馈**:
- 角色保持受击姿势 + 速度线拖尾（方向与击退方向相反）
- 击退速度越快，拖尾越明显（长度与速度成正比）
- hitstun 期结束后，闪烁停止，角色恢复正常轮廓

**状态转换视觉**:
- Idle → Attacking: 无特殊转换特效（攻击本身的 Startup 轮廓光已足够）
- 任何状态 → HitStun: 瞬间白色闪烁
- 任何状态 → Knockback: 闪烁 + 速度线启动

**增强层（MVP 后）**:
- 攻击粒子系统（Startup 蓄力粒子、Active 命中碎片）
- hitstop 屏幕后处理（色差、径向模糊）
- Knockback 运动模糊效果
- 高伤害 % 时角色持续发光脉动

**音频反馈**（定义触发事件，音效系统实现）:

| 音频事件 | 触发时机 | 描述 |
|---------|---------|------|
| `OnAttackStartup` | 进入 Attacking.Startup | 蓄力音效，短促"呼"声 |
| `OnAttackHit` | Attacking.Active 阶段命中对手 | 打击音效，力度感。hitstop 期间播放 |
| `OnAttackMiss` | Attacking.Active 结束但未命中 | 挥空音效，轻微"嗖"声 |
| `OnAttackEnd` | 攻击完全结束（Recovery 耗尽或被取消） | 收招音效，轻微 |
| `OnHitReceived` | 进入 HitStun | 受击音效，痛苦短促 |
| `OnKnockback` | 进入 Knockback | 重击受击音效，比 OnHitReceived 更重 |
| `OnHitstopStart` | hitstop 开始 | 可选：轻微时间冻结音效 |
| `OnHitstopEnd` | hitstop 结束 | 可选：轻微恢复音效 |

## UI Requirements

格斗状态机不直接产生 UI 元素。以下信息由战斗 HUD 使用格斗状态机提供的数据显示：
- 当前战斗状态 → 由 HUD 查询 `GetCurrentState()`（可能用于角色状态图标或颜色变化）
- 攻击阶段 → 由 HUD 查询 `GetCurrentAttackPhase()`（调试显示，非玩家 UI）

无独立的 UI 需求。

## Acceptance Criteria

### 状态转换

- **GIVEN** 角色在 CombatIdle, **WHEN** 当前帧检查缓冲且存在有效攻击输入（BufferAge <= InputBufferFrames）, **THEN** 进入 Attacking.Startup 并查询 3C MovementState 确定攻击类型
- **GIVEN** 角色在 Attacking.Startup（帧数未满 StartupFrames）, **WHEN** 受击, **THEN** 攻击取消，hitbox 关闭，进入 HitStun 或 Knockback
- **GIVEN** 角色在 Attacking.Active, **WHEN** 受击, **THEN** 攻击取消，hitbox 关闭，进入 HitStun 或 Knockback
- **GIVEN** 角色在 Attacking.Active, **WHEN** 命中对手, **THEN** 不打断自身攻击，继续当前阶段
- **GIVEN** 角色在 Attacking.Recovery, **WHEN** 取消窗口内且取消表允许新攻击, **THEN** 取消到新攻击的 Startup
- **GIVEN** 角色在 Attacking.Recovery, **WHEN** 取消窗口已过且帧数耗尽, **THEN** 自然结束回到 Idle
- **GIVEN** 角色在 HitStun, **WHEN** HitStunFrames 耗尽, **THEN** 回到 Idle
- **GIVEN** 角色在 HitStun（剩余 5 帧）, **WHEN** 再次被击中（力度 <= KnockbackThreshold）, **THEN** HitStun 计时器重置为新攻击的 HitStunFrames（不叠加）
- **GIVEN** 角色在 HitStun, **WHEN** 再次被击中（力度 > KnockbackThreshold）, **THEN** 进入 Knockback
- **GIVEN** 角色在 Knockback, **WHEN** KnockbackHitstunFrames 耗尽, **THEN** 回到 Idle（可操作，但物理运动可能仍在持续）
- **GIVEN** 角色在 Knockback 可操作期, **WHEN** 再次被击中, **THEN** 正常判定 HitStun/Knockback，新击退速度覆盖当前物理速度
- **GIVEN** 角色在 Knockback 状态, **WHEN** 角色着地, **THEN** Knockback 结束，CombatState 回到 Idle，3C 转入 Landing 状态

### 攻击阶段推进

- **GIVEN** 攻击帧数据 Startup=4/Active=3/Recovery=8, **WHEN** CurrentAttackFrame = 3, **THEN** 阶段为 Startup
- **GIVEN** 攻击帧数据 Startup=4/Active=3/Recovery=8, **WHEN** CurrentAttackFrame = 4, **THEN** 阶段推进为 Active
- **GIVEN** 攻击帧数据 Startup=4/Active=3/Recovery=8, **WHEN** CurrentAttackFrame = 7, **THEN** 阶段推进为 Recovery
- **GIVEN** 攻击帧数据 Startup=4/Active=3/Recovery=8, **WHEN** CurrentAttackFrame = 15, **THEN** 攻击结束，回到 Idle

### Knockback vs HitStun 判定

- **GIVEN** KnockbackThreshold = 9.0, **WHEN** 击退力度 = 2.0, **THEN** 进入 HitStun
- **GIVEN** KnockbackThreshold = 9.0, **WHEN** 击退力度 = 9.0, **THEN** 进入 HitStun（严格大于判定，等于不触发 Knockback）
- **GIVEN** KnockbackThreshold = 9.0, **WHEN** 击退力度 = 25.0, **THEN** 进入 Knockback

### 公式验证

- **GIVEN** 攻击定义 HitStunFrames = 8, **WHEN** 角色进入 HitStun, **THEN** HitStun 持续恰好 8 帧后回到 Idle
- **GIVEN** 攻击定义 HitStunFrames = 0（数据错误）, **WHEN** 角色进入 HitStun, **THEN** 强制最少 1 帧硬直
- **GIVEN** KnockbackMagnitude = 15.0, **THEN** KnockbackHitstunFrames = Floor(3 + 0.5 × 15) = 10 帧
- **GIVEN** KnockbackMagnitude = 100.0, **THEN** KnockbackHitstunFrames = Min(Floor(53), 50) = 50 帧（触及上限）
- **GIVEN** Jab (4/3/8) 且 CancelWindowFrames=5, **WHEN** CurrentAttackFrame = 6, **THEN** 取消窗口未打开
- **GIVEN** Jab (4/3/8) 且 CancelWindowFrames=5, **WHEN** CurrentAttackFrame = 7, **THEN** 取消窗口打开
- **GIVEN** Jab (4/3/8) 且 CancelWindowFrames=5, **WHEN** CurrentAttackFrame = 11, **THEN** 取消窗口最后一个可取消帧
- **GIVEN** Jab (4/3/8) 且 CancelWindowFrames=5, **WHEN** CurrentAttackFrame = 12, **THEN** 取消窗口已关闭

### 输入缓冲

- **GIVEN** InputBufferFrames = 8, **WHEN** 在可操作帧前 8 帧内按下攻击, **THEN** 输入被接受执行
- **GIVEN** InputBufferFrames = 8, **WHEN** 在可操作帧前 9 帧按下攻击, **THEN** 输入过期被丢弃
- **GIVEN** 缓冲中有攻击输入（BufferAge=3）且当前状态为 Attacking.Startup, **WHEN** 检查缓冲, **THEN** 输入保留在缓冲中，不执行也不丢弃
- **GIVEN** 缓冲中有攻击输入（BufferAge=3）且当前状态为 Attacking.Recovery（取消表允许）, **WHEN** 检查缓冲, **THEN** 输入执行并从缓冲中丢弃
- **GIVEN** BufferAge < 0（帧序号错误）, **WHEN** 检查缓冲, **THEN** 该输入被丢弃
- **GIVEN** 同一帧缓冲中有攻击输入和技能输入, **WHEN** 当前状态可接受输入, **THEN** 按优先级（技能 > 攻击）接受技能输入，攻击输入丢弃

### 3C 协调

- **GIVEN** 角色进入 Attacking, **THEN** 3C 移动被冻结（FreezeMovement(true)）
- **GIVEN** 角色攻击结束回到 Idle, **THEN** 3C 移动恢复（FreezeMovement(false)）
- **GIVEN** 角色进入 HitStun, **THEN** 3C 移动被冻结，角色播放受击动画
- **GIVEN** 角色进入 Knockback, **THEN** 格斗状态机调用 SetVelocity(击退向量) 委托 3C 施加物理力

### 状态扩展

- **GIVEN** 技能系统注册一个新状态（含完整帧数据）, **WHEN** 注册成功且输入触发, **THEN** 新状态正常执行（Startup → Active → Recovery）
- **GIVEN** 技能系统注册新状态（未提供帧数据）, **WHEN** 注册调用, **THEN** 注册被拒绝
- **GIVEN** 技能系统注册新状态（名称重复）, **WHEN** 注册调用, **THEN** 后注册的被忽略，日志中记录警告
- **GIVEN** 技能系统注册的新状态在执行中被 HitStun 打断, **THEN** hitbox 关闭，状态重置，技能系统收到"被打断"回调

### 错误处理

- **GIVEN** 攻击帧数据 Startup=0/Active=0/Recovery=0, **WHEN** 攻击输入被缓冲, **THEN** 该输入被忽略，角色不进入 Attacking
- **GIVEN** 同一帧两个角色互相命中, **WHEN** 碰撞判定系统发送命中事件, **THEN** 各自独立处理受击

### 性能

- **GIVEN** 2 人对战进行中, **THEN** 格斗状态机帧耗时 < 0.5ms

## Open Questions

1. **hitstop 是否应该影响所有角色还是只有命中者和被命中者？** 大乱斗中 hitstop 只影响双方，其他玩家正常。MVP 为 2 人所以无影响，但扩展到 3-4 人时需要决定。（Owner: 设计师，里程碑: VS）
2. **Guard/Shield 状态是否纳入后续版本？** 当前 MVP 无防御状态，防御完全依赖 Dash 无敌帧。需要验证这是否足够。（Owner: 设计师，里程碑: 原型验证后）
3. ~~**KnockbackThreshold 的最终值需要在击退系统设计后校准。**~~ **已解决**: 由 knockback-launch-system GDD 校准为 9.0。占位值 5.0 → 9.0。
4. **取消表是否需要支持条件取消（如"仅在地面时取消到技能X"）？** 当前设计支持，但具体语法需要与技能系统 GDD 协调。（Owner: 技能系统设计师，里程碑: 技能装备管理 GDD）
