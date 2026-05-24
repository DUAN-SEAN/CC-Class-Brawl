# 游戏状态管理 (Game State Management)

> **Status**: Designed
> **Author**: SeanDuan + agents
> **Last Updated**: 2026-05-23
> **Implements Pillar**: Pillar 1: 秒学秒玩, Pillar 4: 快速战斗

## Overview

游戏状态管理是游戏的全局编排器，负责控制从主菜单到角色选择、倒计时、战斗、结果画面的完整状态流转。作为 Foundation 层基础设施，它维护一个有限状态机来决定当前活跃的游戏阶段，协调场景加载与卸载、管理玩家数据的生命周期（加入/离开、角色选择），并为所有 UI 和游戏系统提供统一的接口以查询和响应状态变化。玩家体验此系统的方式是游戏的节奏——启动游戏 → 选角色 → "3, 2, 1, FIGHT!" → 战斗 → 看结果 → 再来一局——所有过渡应即时且无缝，直接服务 Pillar 1（秒学秒玩，玩家可在几秒内跳入战斗）和 Pillar 4（快速战斗，局间无多余等待）。本系统无上游依赖，下游被对局管理系统、对局 UI 和角色选择 UI 依赖。

## Player Fantasy

**核心幻想：「无缝节奏」**

玩家不会直接"操作"状态管理——他们体验的是它的效果。游戏应该像一个流畅的节拍器：从打开游戏到第一场战斗的整个过程不超过 15 秒（选人+倒计时），每局结束后的"再来一局"过渡不超过 3 秒。没有任何生硬的加载画面或不必要的确认步骤打断节奏。

**关键情感时刻**：
- **即开即打** — 打开游戏→选角色→"FIGHT!"，整个过程行云流水，没有等待
- **无缝循环** — 一局结束→看结果→一键再来→立即开始下一局，3 秒内完成过渡
- **不中断的派对感** — 本地多人场景中，玩家可以在结果画面立即按"再来"，不需要回到主菜单重新走流程

**支柱对齐**：
- 服务 **Pillar 1: 秒学秒玩** — 状态流转越快，玩家越快进入游戏
- 服务 **Pillar 4: 快速战斗** — 局间过渡零摩擦，保持"再来一局"的驱动力

## Detailed Design

### Core Rules

**1. 全局状态机**

1. 游戏状态管理维护一个全局有限状态机（FSM），在任意时刻游戏处于且仅处于一个确定的状态
2. 状态转换由明确的触发条件驱动，不存在模糊或条件竞争的转换
3. 状态转换是原子的——转换过程中游戏逻辑暂停，直到新状态完全初始化
4. 每个状态有明确的职责边界：该状态下哪些系统活跃、哪些被冻结

**2. 场景架构**

1. 游戏使用 2 个 Unity 场景：
   - **MenuScene** — 仅包含 MainMenu 状态
   - **GameScene** — 包含 CharacterSelect、MatchLoading、Countdown、Battle、BattleEnd、Results 状态
2. 场景切换使用异步加载（`SceneManager.LoadSceneAsync`），加载期间显示最小化过渡效果
3. GameScene 加载后保持常驻——局间回到 CharacterSelect 不需要重新加载场景

**3. 玩家注册与数据**

1. 玩家通过手柄连接自动注册为参战者（最多 2 人 MVP，架构预留 4 人）
2. 每个注册玩家持有一个 `PlayerSlot` 数据对象：玩家编号、已选角色、输入设备引用
3. 玩家数据在状态间持久化——角色选择在局间保持，直到玩家主动更换
4. 输入设备断开时，玩家槽位保留，状态冻结（由 3C 系统处理断开提示）

**4. 过渡时间约束**

1. MainMenu → CharacterSelect：即时（场景加载，目标 < 2s）
2. CharacterSelect → Countdown：初始化时间（目标 < 1s，GameScene 内状态切换）
3. Countdown 持续时间：3 秒（固定）
4. BattleEnd → Results：短暂戏剧性暂停（`BattleEndFreezeFrames`，默认 60 帧 / 1 秒）
5. Results → CharacterSelect：即时（GameScene 内状态切换，< 0.5s）
6. Results → MainMenu：场景卸载+加载（目标 < 2s）

### States and Transitions

| 当前状态 | 触发条件 | 目标状态 | 场景 |
|---------|---------|---------|------|
| MainMenu | 任意手柄按 Start / 键盘确认 | CharacterSelect | MenuScene → GameScene |
| CharacterSelect | 所有玩家确认角色选择 | MatchLoading | GameScene |
| MatchLoading | 所有战斗系统初始化完成 | Countdown | GameScene |
| Countdown | 倒计时结束（3秒） | Battle | GameScene |
| Battle | 对局管理系统信号 KO | BattleEnd | GameScene |
| BattleEnd | 冻结帧结束 | Results | GameScene |
| Results | 玩家选择"再来一局" | CharacterSelect | GameScene |
| Results | 玩家选择"退出到菜单" | MainMenu | GameScene → MenuScene |

### Interactions with Other Systems

| 系统 | 数据流方向 | 接口描述 |
|------|-----------|---------|
| 对局管理系统 | GameState → Match | GameState 提供当前状态查询：`IGameState.GetState()`，`IsBattleActive()`。对局管理系统仅在 Battle 状态下激活 |
| 对局管理系统 | Match → GameState | 对局管理系统通过 `IGameState.SignalKO(winnerPlayerSlot)` 通知战斗结束 |
| 对局UI | GameState → UI | UI 系统监听状态变化事件 `OnStateChanged(GamePhase newState)`，据此显示倒计时、FIGHT!、VICTORY! 等 |
| 角色选择UI | CharSelect → GameState | 角色选择 UI 通过 `IGameState.SetPlayerCharacter(playerSlot, characterId)` 注册角色选择 |
| 角色选择UI | GameState → CharSelect | GameState 通知所有玩家确认状态 `OnAllPlayersReady()` |
| 3C系统 | GameState → 3C | GameState 在 Countdown/BattleEnd 期间通知 3C 冻结/解冻输入 |
| 职业系统 | GameState → Class | GameState 在 MatchLoading 阶段请求职业系统初始化选定角色的属性 |

**GameState 向上提供的接口契约**：
- `IGameState` 接口是所有系统查询游戏状态的唯一入口
- 状态枚举: `GamePhase { MainMenu, CharacterSelect, MatchLoading, Countdown, Battle, BattleEnd, Results }`
- 事件: `OnStateChanged(GamePhase newState)` — 所有 UI 和系统可监听

## Formulas

本系统为流程控制型系统，无复杂数学公式。所有时序参数（倒计时长度、冻结帧数、过渡时间上限）为固定常量，定义在 Tuning Knobs 节中。

**唯一的计算逻辑**：

**倒计时显示帧映射**
```
DisplayNumber = Max(1, Ceil(RemainingCountdownTime / (1.0 / 60.0) / 60))
```
| 变量 | 类型 | 描述 |
|------|------|------|
| RemainingCountdownTime | float | 剩余倒计时秒数 |
| DisplayNumber | int | 当前显示的数字（3, 2, 1） |

**输出范围**: 1 到 CountdownDuration 的整数

## Edge Cases

**状态转换**:
- **如果在 BattleEnd 冻结期间有第二个玩家也被 KO**: 忽略后续 KO 信号——第一个 KO 信号已锁定胜者，冻结期间不再处理战斗事件
- **如果在 CharacterSelect 中只剩 1 个手柄连接（需要 2 人）**: 角色选择无法完成，显示"等待第二位玩家"提示。不允许 1 人开始对战（MVP 阶段）
- **如果在 MatchLoading 阶段加载失败（场景资源缺失）**: 回退到 MainMenu 状态并显示错误提示。不尝试重试——资源缺失是致命错误
- **如果在 Battle 状态中所有玩家手柄同时断开**: 暂停战斗（由 3C 系统处理暂停），显示"控制器断开"提示。任一手柄重连后继续

**玩家数据**:
- **如果玩家在 Results 画面断开手柄**: 玩家槽位保留。当状态回到 CharacterSelect 时，断开的玩家显示为"等待连接"。其他玩家可以继续操作
- **如果玩家在 CharacterSelect 中切换已选角色**: 新选择立即生效，旧选择释放。不锁定角色——两个玩家可以选择相同角色（MVP 阶段）

**场景管理**:
- **如果从 Results 回到 CharacterSelect 时 GameScene 已加载**: 不重新加载场景，仅重置状态和数据。场景加载仅在 MainMenu → CharacterSelect 时发生一次
- **如果在场景异步加载期间玩家按返回**: 取消加载操作，回到 MainMenu 状态

## Dependencies

游戏状态管理是 Foundation 层，无上游依赖。以下是所有下游依赖关系：

| 依赖系统 | 方向 | 类型 | 数据接口 | GDD 状态 |
|---------|------|------|---------|---------|
| 对局管理系统 | 下游（硬依赖） | 查询 + 控制 | `IGameState.GetState()`, `IsBattleActive()`, `SignalKO(winner)` | 未设计 |
| 对局UI | 下游（硬依赖） | 查询 + 事件 | `OnStateChanged(GamePhase)` 事件，倒计时数据，胜利者数据 | 未设计 |
| 角色选择UI | 下游（硬依赖） | 控制 + 事件 | `SetPlayerCharacter(slot, charId)`, `OnAllPlayersReady()` 事件 | 未设计 |
| 3C系统 | 下游（软依赖） | 控制 | 冻结/解冻输入通知（Countdown/BattleEnd 期间）| In Review |
| 职业系统 | 下游（软依赖） | 控制 | 请求初始化选定角色属性（MatchLoading 阶段）| 未设计 |

**游戏状态管理向上提供的接口契约**：
- `IGameState` 接口是所有下游系统查询游戏状态的唯一入口
- 状态枚举: `GamePhase { MainMenu, CharacterSelect, MatchLoading, Countdown, Battle, BattleEnd, Results }`
- 玩家数据: `PlayerSlot { int playerIndex, string characterId, InputDevice device, bool isConnected }`
- 事件: `OnStateChanged(GamePhase)`, `OnAllPlayersReady()`, `OnPlayerJoined(PlayerSlot)`, `OnPlayerLeft(int playerIndex)`

## Tuning Knobs

| 旋钮名 | 默认值 | 安全范围 | 调高效果 | 调低效果 | 所属阶段 |
|--------|--------|---------|---------|---------|---------|
| CountdownDuration | 3.0 s | 1.0–5.0 | 倒计时更长，准备时间更多 | 倒计时更短，更快进入 | Countdown |
| BattleEndFreezeFrames | 60 帧 | 30–120 帧 | KO 后戏剧性暂停更长 | 更快进入 Results | BattleEnd |
| MaxPlayerCount | 2 | 2–4 | 支持更多玩家 | 仅 2 人 | 全局 |
| SceneLoadTimeout | 5.0 s | 3.0–10.0 | 加载超时容忍度更高 | 更快判定加载失败 | MatchLoading |
| MatchInitTimeout | 3.0 s | 1.0–5.0 | 系统初始化超时容忍度更高 | 更快判定初始化失败 | MatchLoading |

## Visual/Audio Requirements

本系统不直接产生视觉/音频效果。状态转换时的视觉反馈（倒计时动画、FIGHT! 文字、KO 特效、结果画面）由对局 UI 系统负责，基于 `OnStateChanged` 事件触发。本系统仅提供状态数据和事件信号。

## UI Requirements

本系统不直接产生 UI 元素。所有 UI（角色选择界面、倒计时显示、结果画面）由角色选择 UI 和对局 UI 系统实现，通过 `IGameState` 接口和 `OnStateChanged` 事件获取数据。本系统仅提供状态查询和数据接口。

## Acceptance Criteria

### 状态转换
- **GIVEN** 游戏处于 MainMenu 状态，**WHEN** 任意手柄按下 Start，**THEN** 状态转换到 CharacterSelect，GameScene 异步加载在 SceneLoadTimeout（5.0s）内完成
- **GIVEN** 游戏处于 CharacterSelect 状态且 2 名玩家已确认角色，**WHEN** 最后一名玩家确认，**THEN** 状态转换到 MatchLoading，`OnAllPlayersReady()` 事件触发

### 倒计时
- **GIVEN** 游戏处于 Countdown 状态，**THEN** 倒计时显示 3, 2, 1 各持续 1 秒（±2 帧），3 秒后自动转换到 Battle 状态
- **GIVEN** 游戏处于 Countdown 期间，**THEN** 所有玩家输入被冻结（3C 系统不响应方向/跳跃/攻击输入）

### 战斗结束
- **GIVEN** 游戏处于 Battle 状态，**WHEN** 对局管理系统调用 `SignalKO(winnerSlot)`，**THEN** 状态转换到 BattleEnd，游戏冻结 BattleEndFreezeFrames（60 帧）后转换到 Results

### 结果与循环
- **GIVEN** 游戏处于 Results 状态，**WHEN** 玩家选择"再来一局"，**THEN** 状态转换到 CharacterSelect，过渡时间 < 0.5s，场景不重新加载
- **GIVEN** 游戏处于 Results 状态，**WHEN** 玩家选择"退出到菜单"，**THEN** 状态转换到 MainMenu，GameScene 卸载

### 玩家数据
- **GIVEN** 玩家1 选择了角色 A，玩家2 选择了角色 B，**WHEN** 对局结束后回到 CharacterSelect，**THEN** 两个玩家的已选角色保持为 A 和 B（直到玩家主动更换）
- **GIVEN** 2 个手柄已连接，**WHEN** 玩家1 的手柄断开，**THEN** 玩家1 槽位保留，显示"等待连接"提示，玩家2 可继续操作

### 接口事件
- **GIVEN** 任意状态转换发生，**THEN** `OnStateChanged(newState)` 事件触发，所有注册监听者收到通知（≤1 帧延迟）

## Open Questions

1. **是否需要暂停菜单？** 当前设计未包含 Battle 状态下的暂停功能（按 Start 暂停）。本地多人暂停需要双方同意或暂停者独占操作。决定：MVP 不包含暂停功能，后续版本考虑。（Owner: 设计师，里程碑: Alpha）
2. **是否支持回放/观战模式？** 当前状态机不包含 Replay 或 Spectate 状态。如果后续添加在线多人，可能需要。决定：MVP 不包含。（Owner: 设计师，里程碑: VS）
3. **是否需要多局制（Bo3/Bo5）？** 当前每局独立。对局管理系统可能需要追踪多局比分，但这属于对局管理系统的职责，不影响本系统的状态机设计。决定：本系统不支持，由对局管理系统在上层处理。（Owner: 设计师，里程碑: Alpha）
