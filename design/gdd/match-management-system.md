# 对局管理系统 (Match Management System)

> **Status**: Designed
> **Author**: SeanDuan + agents
> **Last Updated**: 2026-05-23
> **Implements Pillar**: Pillar 3: 高手菜鸟都开心, Pillar 4: 快速战斗

## Overview

对局管理系统是比赛层级的编排器，负责追踪多局对战的回合进程——从第一局的 3-2-1-FIGHT 到最终胜利者的判定。作为 Feature 层系统，它在游戏状态管理的 Battle 状态内激活，接收击退与击飞系统发出的 KO 信号来判定单局胜负，累积回合比分，当一方达到胜利阈值时通知游戏状态管理结束整场比赛。数据层面，它维护当前比分、回合序号、胜者记录；玩家体验层面，它创造了多局制的节奏——每局伤害和专注值重置但已装备技能保留，比分贯穿始终，制造"我先拿下一局，看你能不能翻盘"的张力。它直接服务 Pillar 4（快速战斗——单局 2-5 分钟，多局制让总时长可控制在 10-15 分钟内）和 Pillar 3（高手菜鸟都开心——多局制降低单局随机性的影响，让"更好的玩家"更可能赢下系列赛）。

## Player Fantasy

**核心幻想：「翻盘的叙事弧线」**

每一局对战都是一个微型故事（从弱到强的技能进化），而多局制把这些故事串联成一个更大的叙事——比分的起伏、气势的逆转、最终的决胜局。玩家应该感受到：第一局赢了不保险，输了不绝望，因为下一局专注值重置（仍可积累新技能），而保留的技能让每局的角色越来越强大，后期局的混乱度逐渐攀升。

**关键情感时刻**：
- **"我先拿一局"的主动权** — 赢下第一局后，你获得了心理优势，但对手也获得了复仇动力
- **比分压力的递增** — 0-0 时轻松试探，1-1 时每局都关键，赛点时心跳加速
- **决胜局的仪式感** — 最后一局，屏幕上应该有某种视觉标识："这是决定胜负的一局"
- **翻盘的史诗感** — 从 0-2 落后到 3-2 逆转，比任何单局胜利都更有记忆点

**支柱对齐**：
- 服务 **Pillar 3: 高手菜鸟都开心** — 多局制让运气的影响被稀释，"更好的玩家"更可能赢下系列赛，同时每局的不同技能组合保持趣味性
- 服务 **Pillar 4: 快速战斗** — 单局 2-5 分钟，3局2胜最多 15 分钟，节奏紧凑不拖沓
- 呼应 **Pillar 2: 每局都是新故事** — 每局专注值重置带来新的技能解锁可能，保留的技能让后期局更混乱也更有趣，多局制放大了"不可预测"的乐趣

> `creative-director` 未咨询 — Lean 模式。正式上线前需人工审核。

## Detailed Design

### Core Rules

**1. 比赛初始化**

1. 对局管理系统在游戏状态管理的 MatchLoading 阶段由角色选择画面提供初始化数据
2. 初始化数据包含：赛制格式（Bo1/Bo3/Bo5）、参战玩家列表（PlayerSlot 数组）
3. 系统创建 `MatchState` 数据对象，比分初始化为 [0, 0]，回合序号为 1
4. 赛制格式映射：Bo1 → 先赢 1 局，Bo3 → 先赢 2 局，Bo5 → 先赢 3 局

**2. 回合生命周期**

1. 每个回合对应游戏状态管理的一个完整 Battle 状态周期（Countdown → Battle → BattleEnd）
2. 回合开始时，对局管理系统重置以下数据：伤害百分比归零、专注值归零、角色位置回到出生点、角色速度归零
3. 回合中，已装备技能保留（跨局成长弧线），专注值从 0 重新积累（仍可解锁新技能）
4. 回合进行中，对局管理系统处于被动监听状态——等待 KO 事件

**3. KO 处理与比分更新**

1. 击退与击飞系统发出 `OnKO(CharacterId, KODirection)` 事件
2. 对局管理系统接收 KO 事件，通过 CharacterId 确定被 KO 的玩家
3. 被 KO 玩家的对手获得本回合胜利，比分 [winnerIndex] +1
4. 如果双 KO（同一帧两个玩家都被 KO），双方各得一分
5. 回合序号 +1

**4. 比赛胜负判定**

1. 每次比分更新后，检查是否有玩家达到胜利阈值（`WinsNeeded`）
2. 如果某玩家比分 ≥ WinsNeeded：该玩家获得比赛胜利
3. 如果双 KO 导致双方同时达到胜利阈值：判定为**比赛平局**（在 Bo3 中，比分 1-1 → 双 KO → 2-2 = 平局）
4. 比赛胜利或平局时，对局管理系统通知游戏状态管理结束比赛

**5. 局间流转**

1. 回合结束后（KO 已确认），游戏状态管理进入 BattleEnd（冻结帧）
2. 冻结帧结束后：
   - 如果比赛未结束：显示局间结果画面（`InterRoundDuration` 秒），然后进入下一回合的 Countdown
   - 如果比赛已结束：直接进入 Results 状态
3. 局间结果画面显示当前比分，不阻塞玩家输入（玩家可提前确认跳过）

**6. 数据重置规则**

每回合开始时重置以下数据：

| 数据 | 重置为 | 拥有系统 |
|------|--------|---------|
| 伤害百分比 | 0% | 伤害计算系统 |
| 专注值 | 0 | 专注值系统 |
| 角色位置 | 出生点 | 3C 系统 |
| 角色速度 | 0 | 3C 系统 |
| 击退状态 | 无 | 击退与击飞系统 |
| 格斗状态 | Idle | 格斗状态机 |
| 已装备技能 | 保留 | 技能装备管理 |
| 职业选择 | 保留 | 职业系统 |

### States and Transitions

对局管理系统维护内部状态机，独立于游戏状态管理的全局状态机：

| 当前状态 | 触发条件 | 目标状态 | 描述 |
|---------|---------|---------|------|
| Inactive | GameState 进入 MatchLoading | WaitingForBattle | 初始化比赛数据，等待第一局开始 |
| WaitingForBattle | GameState 进入 Battle | RoundInProgress | 回合开始，监听 KO |
| RoundInProgress | 收到 OnKO 事件 | RoundResolved | KO 发生，判定回合胜者 |
| RoundResolved | 比赛未结束 | WaitingForBattle | 局间结果展示后进入下一回合 |
| RoundResolved | 比赛已结束 | MatchComplete | 通知 GameState 进入 Results |
| MatchComplete | — | — | 终态，等待 GameState 重置 |

### Interactions with Other Systems

| 系统 | 数据流方向 | 接口描述 |
|------|-----------|---------|
| 游戏状态管理 | Match → GameState | `SignalRoundEnd(winnerIndex, matchOver)` — 通知回合结束；`matchOver=true` 时 GameState 进入 Results，`matchOver=false` 时回到 Countdown |
| 游戏状态管理 | GameState → Match | `IGameState.GetState()` 查询当前状态；`IsBattleActive()` 确认战斗进行中 |
| 击退与击飞系统 | Knockback → Match | `OnKO(CharacterId, KODirection)` 事件——触发回合结束判定 |
| 伤害计算系统 | Match → Damage | 回合开始时请求 `ResetDamagePercent(playerIndex)` |
| 专注值系统 | Match → Focus | 回合开始时请求 `ResetFocus(playerIndex)` |
| 3C系统 | Match → 3C | 回合开始时请求 `ResetPosition(playerIndex)` 到出生点 |
| 技能装备管理 | Match → SkillEquip | 回合开始时**不**重置（技能保留） |
| 战斗HUD | Match → HUD | 比分更新事件、回合序号、局间结果数据 |
| 格斗状态机 | Match → FSM | 回合开始时请求 `ResetToIdle(playerIndex)` |

**对局管理系统向上提供的接口契约**：
- `IMatchManager` 接口：比赛生命周期管理入口
- `OnRoundEnd(winnerIndex, scores)` 事件：回合结束通知
- `OnMatchEnd(winnerIndex or draw)` 事件：比赛结束通知
- `GetMatchState()` → MatchState：查询当前比赛数据
- `GetScores()` → int[2]：查询当前比分
- `GetCurrentRound()` → int：查询当前回合序号

## Formulas

### 1. 胜利阈值计算

`WinsNeeded` 公式定义为：

`WinsNeeded = Ceil(MatchFormat / 2.0)`

| 变量 | 符号 | 类型 | 范围 | 描述 |
|------|------|------|------|------|
| 赛制格式 | MatchFormat | int | {1, 3, 5} | Bo1=1, Bo3=3, Bo5=5 |
| 胜利阈值 | WinsNeeded | int | {1, 2, 3} | 需要赢的局数 |

**输出范围**: 1 到 3
**示例**: MatchFormat=3 → WinsNeeded=Ceil(1.5)=2（Bo3 需赢 2 局）

### 2. 最大回合数

`MaxRounds = (WinsNeeded × 2) - 1`

| 变量 | 符号 | 类型 | 范围 | 描述 |
|------|------|------|------|------|
| 胜利阈值 | WinsNeeded | int | {1, 2, 3} | 上面公式计算 |
| 最大回合数 | MaxRounds | int | {1, 3, 5} | 不含双 KO 重赛的最多局数 |

**输出范围**: 1 到 5
**示例**: Bo3, WinsNeeded=2 → MaxRounds=3（最多打 3 局）

### 3. 比赛结束判定

`IsMatchOver = (scores[0] >= WinsNeeded) OR (scores[1] >= WinsNeeded)`

| 变量 | 符号 | 类型 | 范围 | 描述 |
|------|------|------|------|------|
| 玩家比分 | scores[0], scores[1] | int | [0, MaxRounds] | 当前比分 |
| 胜利阈值 | WinsNeeded | int | {1, 2, 3} | 需要赢的局数 |

**输出范围**: Boolean
**示例**: scores=[2, 1], WinsNeeded=2 → IsMatchOver=true（玩家 1 赢得比赛）

### 4. 比赛平局判定

`IsDraw = (scores[0] >= WinsNeeded) AND (scores[1] >= WinsNeeded)`

仅在双 KO 后比分更新时可能触发。

**输出范围**: Boolean
**示例**: Bo3, scores=[1, 1], 双 KO → scores=[2, 2], WinsNeeded=2 → IsDraw=true

### 5. 预期比赛时长

`EstimatedMatchDuration = AvgRoundDuration × ExpectedRounds`

| 变量 | 符号 | 类型 | 范围 | 描述 |
|------|------|------|------|------|
| 平均回合时长 | AvgRoundDuration | float | 2.0–5.0 分钟 | 游戏概念定义 3-5 分钟 |
| 预期回合数 | ExpectedRounds | float | — | 统计期望值 |
| 预期比赛时长 | EstimatedMatchDuration | float | — | 用于验证节奏 |

`ExpectedRounds ≈ WinsNeeded × 1.5`（简化估算：假设 50/50 对战）

| 赛制 | WinsNeeded | ExpectedRounds | 预期时长 (3min/回合) | 预期时长 (5min/回合) |
|------|-----------|---------------|---------------------|---------------------|
| Bo1 | 1 | ~1 | 3 min | 5 min |
| Bo3 | 2 | ~3 | 9 min | 15 min |
| Bo5 | 3 | ~4.5 | 13.5 min | 22.5 min |

**设计意图**: Bo3 的预期时长 9-15 分钟符合 Pillar 4（快速战斗）。Bo5 在 5 分钟/局时可能过长（22.5 分钟），但实际平均应低于此值。

## Edge Cases

**双 KO 相关**:
- **如果 Bo3 比分 1-1 时发生双 KO**: scores=[2, 2], WinsNeeded=2 → IsDraw=true。判定为比赛平局，进入 Results 显示平局结果
- **如果 Bo5 比分 2-2 时发生双 KO**: 同理，scores=[3, 3], WinsNeeded=3 → IsDraw=true，比赛平局
- **如果 Bo1 发生双 KO**: scores=[1, 1], WinsNeeded=1 → IsDraw=true，单局平局

**比分极端情况**:
- **如果一方领先时双 KO**: 例如 Bo3 比分 1-0，双 KO → [2, 1]。玩家 1 达到 WinsNeeded=2，玩家 2 未达到 → 玩家 1 赢得比赛（非平局）。正确处理
- **如果连续多次双 KO 导致比分超过 MaxRounds**: 不可能。双 KO 时双方各 +1，比分差不变。如果某方在双 KO 之前已经领先，双 KO 后仍领先且可能达到 WinsNeeded

**状态异常**:
- **如果在 RoundResolved 阶段收到额外的 KO 事件**: 忽略。第一个 KO 事件已锁定回合结果，后续 KO 信号无效（与游戏状态管理的 BattleEnd 冻结逻辑一致）
- **如果在 WaitingForBattle 状态下收到 KO 事件**: 忽略。KO 仅在 RoundInProgress 状态下有效
- **如果在 MatchComplete 状态下收到任何事件**: 忽略。比赛已结束

**初始化异常**:
- **如果赛制格式不是 {1, 3, 5}**: 钳制到最近合法值。0 或负数 → Bo1 (1)；偶数 → +1（如 2→3=Bo3）；>5 → Bo5 (5)
- **如果玩家列表为空或只有 1 人**: 不初始化比赛。记录错误，通知游戏状态管理回退到 CharacterSelect
- **如果在 MatchLoading 阶段对局管理系统未收到初始化数据**: 不进入 Battle 状态。游戏状态管理的 MatchInitTimeout（3.0s）会触发超时处理

**局间流转**:
- **如果局间结果画面期间玩家断开手柄**: 冻结局间计时器。显示"等待玩家重连"提示。重连后继续倒计时
- **如果玩家在局间按确认跳过**: 立即结束局间等待，进入下一回合 Countdown。两个玩家中任意一个按确认即可跳过
- **如果数据重置请求失败（某系统未响应）**: 不进入 Battle。记录警告，重试一次。如果仍失败，中止比赛回到 CharacterSelect

**比赛结果**:
- **如果比赛平局（IsDraw=true）**: Results 画面显示平局结果，不标注胜者。两个玩家都显示相同的结果图标
- **如果比赛在第一局就平局（Bo1 双 KO）**: 算作完整的比赛记录。不影响后续比赛

## Dependencies

| 依赖系统 | 方向 | 类型 | 数据接口 | GDD 状态 |
|---------|------|------|---------|---------|
| 游戏状态管理 | 上游（硬依赖） | 查询 + 控制 | `IGameState.GetState()`, `IsBattleActive()`, `SignalRoundEnd(winnerIndex, matchOver)` | Designed |
| 击退与击飞系统 | 上游（硬依赖） | 事件 | `OnKO(CharacterId, KODirection)` | Designed |
| 伤害计算系统 | 下游（硬依赖） | 控制 | `ResetDamagePercent(playerIndex)` — 回合重置 | Designed |
| 专注值系统 | 下游（硬依赖） | 控制 | `ResetFocus(playerIndex)` — 回合重置 | Designed |
| 3C系统 | 下游（硬依赖） | 控制 | `ResetPosition(playerIndex)` — 回合重置到出生点 | In Review |
| 格斗状态机 | 下游（软依赖） | 控制 | `ResetToIdle(playerIndex)` — 回合重置状态 | Designed |
| 技能装备管理 | 下游（软依赖） | 查询 | 回合间**不**重置技能，但需查询已装备技能（HUD 展示） | Designed |
| 战斗HUD | 下游（硬依赖） | 事件 | `OnRoundEnd`, `OnMatchEnd`, 比分/回合数据 | 未设计 |
| 对局UI | 下游（硬依赖） | 事件 | `OnMatchEnd`, 比赛结果数据 | 未设计 |

**对游戏状态管理的接口需求变更**:
- 现有接口 `SignalKO(winnerPlayerSlot)` 需扩展为 `SignalRoundEnd(winnerIndex, matchOver: bool)`
- `matchOver=false` 时，GameState 从 BattleEnd 回到 Countdown（而非 Results），支持多局制
- `matchOver=true` 时，GameState 进入 Results（比赛结束）
- 此变更为**破坏性变更**，需更新游戏状态管理 GDD

**双向依赖验证**:
- 游戏状态管理 GDD 列出"对局管理系统"为下游依赖 ✅
- 击退与击飞系统 GDD 列出"对局管理系统"为下游依赖 ✅
- 战斗HUD GDD 未设计（将在其 GDD 中确认反向引用）
- 对局UI GDD 未设计（将在其 GDD 中确认反向引用）

## Tuning Knobs

| 旋钮名 | 默认值 | 安全范围 | 调高效果 | 调低效果 | 所属规则 |
|--------|--------|---------|---------|---------|---------|
| DefaultMatchFormat | 3 (Bo3) | {1, 3, 5} | 比赛更长，更多翻盘空间 | 比赛更短，更紧凑 | 比赛初始化 |
| InterRoundDuration | 2.5 s | 1.0–5.0 | 局间休息更长，玩家有更多时间调整心态 | 局间休息更短，节奏更快 | 局间流转 |
| SkipAllowed | true | {true, false} | 玩家可跳过局间等待 | 玩家必须等待完整时间 | 局间流转 |

**旋钮交互说明**:
- `DefaultMatchFormat` 直接决定 `WinsNeeded` 和 `MaxRounds`，影响整场比赛的时长和节奏
- `InterRoundDuration` 不影响战斗本身，但影响总比赛时长。Bo3 + 2.5s 局间 ≈ 额外 5s 局间等待（2 次过渡）

## Visual/Audio Requirements

本系统不直接产生视觉效果，但触发以下视觉/音频事件：

| 事件 | 触发时机 | 视觉需求 | 音频需求 |
|------|---------|---------|---------|
| OnRoundEnd | 回合结束（KO 确认） | 局间比分展示动画（由对局 UI 实现） | 回合结束音效（短促提示音） |
| OnMatchEnd | 比赛结束（胜利） | 胜利者高光 + 比赛结果画面（由对局 UI 实现） | 比赛胜利音效（胜利号角/欢呼声） |
| OnMatchEnd | 比赛结束（平局） | 平局结果画面，双方相同展示 | 平局音效（不同于胜利音效） |
| DecisiveRound | 进入决胜局（如 Bo3 的第 3 局） | 屏幕短暂闪烁/特效标识"决胜局" | 决胜局提示音（增加紧张感） |

**视觉层级**:
- 普通回合结束：简洁的比分更新动画
- 赛点回合（一方再赢一局就获胜）：比分旁边显示赛点标识
- 决胜局：更醒目的视觉标识

## UI Requirements

对局管理系统本身不渲染 UI，但为以下 UI 元素提供数据：

1. **比赛比分显示**（战斗 HUD 的一部分）
   - 位置：屏幕顶部中央
   - 内容：`[P1 score] - [P2 score]`，格式如 "1 - 0"
   - 更新时机：OnRoundEnd 事件触发时
   - 数据来源：`IMatchManager.GetScores()`

2. **局间结果画面**
   - 触发：回合结束且比赛未结束时
   - 内容：本局胜者标识 + 当前比分 + 赛制信息（如 "Bo3 第 2 局"）
   - 持续时间：InterRoundDuration（可跳过）
   - 数据来源：`IMatchManager.GetMatchState()`

3. **比赛结果画面**（Results 状态的一部分）
   - 触发：比赛结束时
   - 内容：比赛胜利者 或 "平局" + 最终比分
   - 数据来源：`OnMatchEnd` 事件

**📌 UX 标记 — 对局管理系统**: 本系统有 UI 需求。在 Phase 4（Pre-Production），运行 `/ux-design` 为局间结果画面和比分显示创建 UX spec **在编写 epic 之前**。Story 应引用 `design/ux/[screen].md`，而非直接引用 GDD。

## Acceptance Criteria

### 比赛初始化

- **GIVEN** 游戏处于 MatchLoading 状态，**WHEN** 角色选择画面提供 MatchConfig(format=Bo3, players=[P1, P2])，**THEN** 对局管理系统初始化 MatchState: scores=[0,0], WinsNeeded=2, currentRound=1
- **GIVEN** 游戏处于 MatchLoading 状态，**WHEN** 角色选择画面提供 MatchConfig(format=Bo1)，**THEN** WinsNeeded=1, MaxRounds=1

### 回合生命周期

- **GIVEN** 比赛已初始化，GameState 进入 Countdown，**WHEN** Countdown 结束进入 Battle，**THEN** 对局管理系统状态为 RoundInProgress，伤害百分比为 0%，专注值为 0，角色在出生点

### KO 处理

- **GIVEN** 对局管理系统处于 RoundInProgress，**WHEN** 收到 OnKO(player2_CharacterId)，**THEN** scores=[1,0], currentRound=2, 回合胜者为玩家 1
- **GIVEN** 对局管理系统处于 RoundInProgress，**WHEN** 同一帧收到两个 OnKO 事件（双 KO），**THEN** 双方各得一分，scores 各 +1

### 比赛胜负

- **GIVEN** Bo3 比赛，scores=[1,1]，**WHEN** 玩家 1 赢得第 3 局，**THEN** scores=[2,1], IsMatchOver=true, 比赛胜利者为玩家 1
- **GIVEN** Bo3 比赛，scores=[1,1]，**WHEN** 双 KO 发生，**THEN** scores=[2,2], IsDraw=true, 比赛平局
- **GIVEN** Bo1 比赛，**WHEN** 玩家 1 被KO，**THEN** scores=[0,1], IsMatchOver=true

### 局间流转

- **GIVEN** 回合结束且比赛未结束（Bo3 scores=[1,0]），**WHEN** BattleEnd 冻结帧结束，**THEN** 显示局间结果画面 InterRoundDuration 秒后进入下一回合 Countdown
- **GIVEN** 局间结果画面期间，**WHEN** 任一玩家按确认，**THEN** 立即跳过等待，进入 Countdown
- **GIVEN** 回合结束且比赛已结束，**WHEN** BattleEnd 冻结帧结束，**THEN** 直接进入 Results 状态

### 数据重置

- **GIVEN** 第 1 局结束时玩家 1 有 2 个已装备技能、85% 伤害、专注值 42.0，**WHEN** 第 2 局开始，**THEN** 伤害% = 0, 专注值 = 0, 已装备技能保持 2 个不变

### 状态异常

- **GIVEN** 对局管理系统处于 RoundResolved 状态，**WHEN** 收到额外的 OnKO 事件，**THEN** 事件被忽略，不改变已锁定的回合结果
- **GIVEN** 对局管理系统处于 MatchComplete 状态，**WHEN** 收到任何事件，**THEN** 事件被忽略

### 接口事件

- **GIVEN** 回合结束，**THEN** `OnRoundEnd(winnerIndex, scores)` 事件触发，所有注册监听者收到通知（≤1 帧延迟）
- **GIVEN** 比赛结束（胜利或平局），**THEN** `OnMatchEnd(winnerIndex or draw)` 事件触发

### 性能

- **GIVEN** 2 人对战，**THEN** 对局管理系统每帧处理耗时 < 0.05ms（仅事件监听和条件检查，无持续计算）

> `qa-lead` 未咨询 — Lean 模式。正式上线前需人工审核。

## Open Questions

1. **Bo5 模式下时长是否过长？** 预期最长 22.5 分钟（5min/局 × 4.5 局），可能超过 Pillar 4 的"快速战斗"目标。解决方案选项：(a) 移除 Bo5，(b) 缩短 Bo5 的单局时长，(c) 保留但标注为"竞技模式"。（Owner: 设计师，里程碑: 原型验证后）

2. **局间是否允许换职业？** 当前设计局间不回到角色选择，不允许换职业。但游戏概念提到"每局结束可更换起始职业"。如果允许换职业，需要修改局间流程回到 CharacterSelect 或增加快捷换人 UI。（Owner: 设计师，里程碑: MVP）

3. **赛制选择 UI 的具体交互？** 在角色选择画面中如何选择 Bo1/Bo3/Bo5？需要一个赛制选择区域。具体交互留给角色选择 UI GDD 定义。（Owner: UX 设计师，里程碑: VS）

4. **技能跨局保留是否破坏平衡？** 第一局获得强力技能的玩家在后续局中是否优势过大？需要在原型阶段验证。如果问题严重，备选方案是每局完全重置技能。（Owner: 设计师，里程碑: 原型验证后）
