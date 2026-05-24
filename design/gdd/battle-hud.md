# 战斗HUD (Battle HUD)

> **Status**: In Design
> **Author**: SeanDuan + agents
> **Last Updated**: 2026-05-23
> **Implements Pillar**: Pillar 1: 秒学秒玩, Pillar 4: 快速战斗

## Overview

战斗HUD是职业对决战斗期间的实时信息显示层，负责将伤害计算系统、专注值系统、技能装备管理系统和对局管理系统的核心数据转化为屏幕上玩家一眼可读的视觉信息——每个玩家的伤害百分比、专注值进度条、已装备技能槽位图标，以及比赛比分和回合序号。数据层面，HUD是一个纯被动渲染系统：它订阅上游系统的事件（`OnDamagePercentChanged`、`OnFocusChanged`、`OnSkillEquipped`、`OnRoundEnd`），将数值更新映射到对应的UI元素，不产生任何影响游戏逻辑的输出。玩家体验层面，战斗HUD是"战斗的第二语言"——玩家不需要停下来分析场上状况，HUD以颜色、动画、数字让信息在余光中传递：看到自己的百分比数字变红就知道该防守了，看到专注值条脉动就知道快解锁了，看到技能图标弹入就知道新招式到手了。它直接服务 Pillar 1（秒学秒玩——所有信息直觉可读，无需教学）和 Pillar 4（快速战斗——HUD不阻塞战斗节奏，信息更新与帧同步）。设计约束：HUD不可遮挡战斗区域核心视觉（角色和平台），所有元素位于屏幕边缘；支持手柄方向键导航（MVP仅信息展示，无交互导航需求）；每帧更新但渲染开销不超过帧预算的 5%。

## Player Fantasy

**核心幻想：「战场一目了然，信息在余光中流动」**

玩家不应该"看"HUD——HUD应该是战斗视野的自然延伸。当一切运转正常时，玩家甚至不会意识到自己在读HUD——百分比的颜色变化触发了"该防守了"的直觉，专注值条的脉动速度传递了"快解锁了"的紧迫感，技能槽的亮起带来了"我有新武器了"的兴奋。好的HUD是无声的战场通讯员：不喊叫，但该说的都说了。

**关键情感时刻**：
- **百分比跳动的节奏感** — 每次命中后数字跳变的动画，是战斗节奏的视觉心跳。连击时数字连续跳动，带来"我在碾压"的满足感
- **颜色变红的紧张感** — 从白到黄到橙到红，百分比颜色的渐变是"危险正在逼近"的直觉信号，不需要读数字
- **专注值满的期待感** — 进度条脉动加速时，玩家进攻欲望陡增——"再打一下就解锁了"
- **技能图标弹入的惊喜感** — 空槽位突然亮起，技能图标弹入，稀有度颜色的光晕闪烁——"这次抽到了什么？"
- **比分牌的叙事感** — "1 - 0" 到 "1 - 1" 到 "2 - 1"，比分的变化是比赛故事的文本

**支柱对齐**：
- 服务 **Pillar 1: 秒学秒玩** — HUD不需要教学，颜色和动画直觉传达信息
- 服务 **Pillar 4: 快速战斗** — HUD不阻塞、不暂停、不等待确认，与战斗帧同步更新

> `creative-director` 未咨询 — Lean 模式。正式上线前需人工审核。

## Detailed Design

### Core Rules

**1. HUD 整体布局**

战斗HUD采用屏幕边缘布局，不遮挡中央战斗区域。所有元素固定在屏幕空间（Screen Space - Overlay），不跟随世界摄像机移动。

```
┌──────────────────────────────────┐
│            1 - 0  R2/3           │  ← 比分区 (顶部中央)
│                                  │
│                                  │
│        [战斗区域：角色+平台]        │
│                                  │
│  42%  ████████░░  [■□□□]  P1    │  ← P1 信息区 (左下角)
│              P2    [□■□□]  28%   │  ← P2 信息区 (右下角)
└──────────────────────────────────┘
```

每个玩家的信息区包含三个组件（从外到内）：
- 伤害百分比（数字 + 颜色编码）
- 专注值进度条
- 技能槽位组（4 个槽位横向排列）

**2. 伤害百分比显示**

- 显示内容：`Floor(DamagePercent)` 的整数值 + "%" 后缀
- 字体大小：足够在 1920×1080 下从 2 米外清晰可读（≈ 48-64px 等效）
- 颜色编码（来自伤害计算系统 GDD）：
  - 0%–49%：白色 (`#FFFFFF`)
  - 50%–99%：黄色 (`#FFD700`)
  - 100%–149%：橙色 (`#FF8C00`)
  - 150%+：红色闪烁 (`#FF2020`，0.5Hz 闪烁)
- 更新时机：收到 `OnDamagePercentChanged(CharacterId, newPercent)` 事件时
- 数字跳变动画：数值变化时触发缩放弹跳（1.0x → 1.3x → 1.0x，持续 0.15 秒）
- 颜色过渡：颜色变化时平滑过渡（0.2 秒），不跳变
- 数据来源：伤害计算系统 → `GetDamagePercent(CharacterId)`

**3. 专注值进度条**

- 显示内容：水平进度条，填充比例 = `FocusPoints / UnlockThreshold`
- 颜色：填充色使用玩家职业主色（Warrior `#E84545`、Rogue `#2ECC71`、Mage `#5EADF2`），背景色暗灰 `#333333`
- 宽度：约占玩家信息区的 40%，高度 ≈ 8-12px
- 脉动效果：当 `FocusPoints / UnlockThreshold > 0.8` 时，进度条填充色脉动加速（正弦波亮度调制，频率从 1Hz 提升到 3Hz）
- 解锁动画：达到阈值瞬间——进度条闪烁白色（0.1 秒）→ 清空 → 剩余专注值重新填充
- 已达上限（`UnlockedCount >= MaxSkillsPerMatch`）：进度条变灰 `#666666`，显示 "MAX" 标记
- 阈值标记线：进度条背景上有一条垂直细线标记当前 `UnlockThreshold` 位置（阈值变化时标记线滑动动画）
- 更新时机：收到 `OnFocusChanged(CharacterId, FocusPoints, UnlockThreshold)` 事件时
- 数据来源：专注值系统 → `GetFocusPoints(CharacterId)`, `GetUnlockThreshold(CharacterId)`, `GetUnlockedCount(CharacterId)`

**4. 技能槽位组**

- 布局：4 个技能槽位横向排列，Slot 1（最左）到 Slot 4（最右）
- 每个槽位尺寸：正方形，约 48×48px（1920×1080 基准）
- 空槽位：灰色半透明轮廓 `#555555` + 按键提示文字（P1: "1"/"2"/"3"/"4"，P2 可配置）
- 已装备槽位：技能图标（`SkillData.Icon`）+ 稀有度边框颜色（Common `#4488FF`、Rare `#8844CC`、Epic `#FFB800`）
- 当前执行中的技能槽位：高亮 + 脉动边框（脉动频率 2Hz）
- 技能被打断时：槽位短暂闪红（0.2 秒）
- 装备动画：技能图标缩放弹入（0 → 1.2x → 1.0x，持续 0.25 秒）+ 稀有度色光晕闪烁
- 更新时机：
  - 装备：收到 `OnSkillEquipped(CharacterId, SlotIndex, SkillData)` 事件
  - 激活/打断：格斗状态机状态变化事件
- 数据来源：技能装备管理 → `GetEquippedSkills(CharacterId)`, `GetSkillSlot(CharacterId, SlotIndex)`

**5. 比分区**

- 位置：屏幕顶部中央
- 显示内容：`[P1分数] - [P2分数]`（如 "1 - 0"）+ 回合序号（如 "R2/3" 表示第 2 局共 3 局）
- 字体大小：中等（≈ 24-32px），不喧宾夺主
- 颜色：白色文字 + 半透明暗色背景条
- 赛点标记：当任一方 `scores[i] == WinsNeeded - 1` 时，在领先方分数旁显示 "●" 标记（黄色脉动）
- 更新时机：收到 `OnRoundEnd(winnerIndex, scores)` 事件时
- 数据来源：对局管理系统 → `GetScores()`, `GetCurrentRound()`, `GetMatchState()`

**6. HUD 可见性控制**

- HUD 仅在 Battle 状态（Countdown → Battle → BattleEnd）期间可见
- MainMenu / CharacterSelect / Results 状态下 HUD 隐藏
- 过渡动画：进入战斗时 HUD 淡入（0.3 秒），退出战斗时 HUD 淡出（0.3 秒）
- 触发：游戏状态管理状态变化事件

**7. 双人布局对称性**

- P1 信息区在左下角，P2 信息区在右下角
- P1 的伤害%在最左，技能槽在 P1 信息区右侧
- P2 的伤害%在最右，技能槽在 P2 信息区左侧
- 双方信息区镜像对称，中间为战斗区域
- 未来扩展（3-4 人）：需要重新设计布局，不在 MVP 范围内

### States and Transitions

战斗HUD无独立状态机——它的行为由游戏状态管理的全局状态驱动。

| 游戏状态 | HUD 行为 |
|---------|---------|
| MainMenu | 隐藏 |
| CharacterSelect | 隐藏 |
| MatchLoading | 隐藏 |
| Countdown | 可见（淡入动画），显示初始值（伤害 0%、专注 0、空技能槽、比分 0-0） |
| Battle | 可见，持续响应上游事件更新 |
| BattleEnd | 可见（冻结在最终状态），2-3 帧画面定格期间 HUD 保持 |
| Results | 淡出隐藏 |

### Interactions with Other Systems

| 系统 | 数据流方向 | 接口描述 |
|------|-----------|---------|
| 伤害计算系统 | 上游 → HUD | `OnDamagePercentChanged(CharacterId, newPercent)` — 更新百分比数字和颜色 |
| 专注值系统 | 上游 → HUD | `OnFocusChanged(CharacterId, FocusPoints, UnlockThreshold)` — 更新进度条和脉动 |
| 专注值系统 | 上游 → HUD | `OnFocusReady(CharacterId, UnlockedCount)` — 触发解锁动画 |
| 技能装备管理 | 上游 → HUD | `OnSkillEquipped(CharacterId, SlotIndex, SkillData)` — 更新技能槽位图标 |
| 技能装备管理 | 上游 → HUD | `OnSkillUnequipped(CharacterId, SlotIndex)` — 清空技能槽位 |
| 对局管理系统 | 上游 → HUD | `OnRoundEnd(winnerIndex, scores)` — 更新比分显示 |
| 对局管理系统 | 上游 → HUD | `OnMatchEnd(winnerIndex or draw)` — 比赛结束标记 |
| 对局管理系统 | 上游 → HUD | `GetScores()`, `GetCurrentRound()` — 查询比分和回合 |
| 游戏状态管理 | 上游 → HUD | 状态变化事件 — 控制HUD可见性 |
| 格斗状态机 | 上游 → HUD | 状态变化事件 — 技能槽位激活/打断高亮 |

## Formulas

**单位系统**: 显示单位为像素（基准分辨率 1920×1080），时间以秒为基准。HUD 不参与游戏逻辑计算。

### 1. 伤害百分比显示值

`DisplayPercent = Floor(TargetDamagePercent)`

| Variable | Symbol | Type | Range | Description |
|----------|--------|------|-------|-------------|
| 目标伤害百分比 | TargetDamagePercent | float | 0.0–999.0+ | 来自伤害计算系统 |
| 显示百分比 | DisplayPercent | int | 0–999 | 向下取整后显示 |

**Output Range**: 0 到 999
**Example**: TargetDamagePercent = 42.7 → DisplayPercent = 42，显示 "42%"
**Note**: 与伤害计算系统的 `display_percent_rounding` 公式一致。HUD 不重新计算，直接使用上游提供的 `DisplayPercent`。

### 2. 伤害百分比颜色索引

```
if TargetDamagePercent >= 150: ColorIndex = RED_FLASH
else if TargetDamagePercent >= 100: ColorIndex = ORANGE
else if TargetDamagePercent >= 50: ColorIndex = YELLOW
else: ColorIndex = WHITE
```

| Variable | Symbol | Type | Range | Description |
|----------|--------|------|-------|-------------|
| 目标伤害百分比 | TargetDamagePercent | float | 0.0–999.0+ | 来自伤害计算系统 |
| 颜色索引 | ColorIndex | enum | {WHITE, YELLOW, ORANGE, RED_FLASH} | 决定百分比数字的显示颜色 |

**Output Range**: 4 级颜色
**Example**: TargetDamagePercent = 127.3 → ColorIndex = ORANGE，显示 `#FF8C00`

### 3. 专注值进度条填充比例

`FocusFillRatio = FocusPoints / UnlockThreshold`

| Variable | Symbol | Type | Range | Description |
|----------|--------|------|-------|-------------|
| 当前专注值 | FocusPoints | float | 0.0–55.0 | 来自专注值系统 |
| 当前解锁阈值 | UnlockThreshold | float | 40.0–55.0 | 来自专注值系统 |
| 填充比例 | FocusFillRatio | float | 0.0–1.0+ | 进度条填充宽度百分比 |

**Output Range**: 0.0（空）到 1.0+（满/溢出，显示时钳制到 1.0）
**Example**: FocusPoints = 32.0, UnlockThreshold = 40.0 → FocusFillRatio = 0.8（80%）

### 4. 专注值脉动频率

```
if UnlockedCount >= MaxSkillsPerMatch: PulseFrequency = 0 (变灰)
else if FocusFillRatio > 0.8: PulseFrequency = Lerp(1.0, 3.0, (FocusFillRatio - 0.8) / 0.2)
else: PulseFrequency = 1.0
```

| Variable | Symbol | Type | Range | Description |
|----------|--------|------|-------|-------------|
| 填充比例 | FocusFillRatio | float | 0.0–1.0+ | 公式 3 计算结果 |
| 已解锁数 | UnlockedCount | int | 0–4 | 来自专注值系统 |
| 每局上限 | MaxSkillsPerMatch | int | 4 | 来自专注值系统 |
| 脉动频率 | PulseFrequency | float | 0.0–3.0 | Hz，进度条亮度调制频率 |

**Output Range**: 0.0（静态/变灰）到 3.0（快速脉动）
**Example**: FocusFillRatio = 0.9, UnlockedCount = 2 → PulseFrequency = Lerp(1.0, 3.0, 0.5) = **2.0 Hz**

### 5. 赛点标记判定

`IsMatchPoint = (scores[0] == WinsNeeded - 1) OR (scores[1] == WinsNeeded - 1)`

| Variable | Symbol | Type | Range | Description |
|----------|--------|------|-------|-------------|
| 玩家比分 | scores[0], scores[1] | int | [0, MaxRounds] | 来自对局管理系统 |
| 胜利阈值 | WinsNeeded | int | {1, 2, 3} | 来自对局管理系统 |
| 赛点标记 | IsMatchPoint | bool | — | 是否在比分旁显示赛点标记 |

**Output Range**: Boolean
**Example**: Bo3, scores = [1, 0], WinsNeeded = 2 → IsMatchPoint = true

## Edge Cases

**数据显示异常**:
- **如果 TargetDamagePercent > 999.0（极端对局）**: 显示 "999+"，不再显示精确数字。颜色仍按规则判定（红色闪烁）。
- **如果 TargetDamagePercent 为负数（上游数据错误）**: 钳制显示为 0%，白色。记录警告。
- **如果 FocusPoints 或 UnlockThreshold 为负数或零（上游错误）**: 填充比例钳制为 0.0（空进度条），不触发除零异常。UnlockThreshold 为零时跳过除法，直接显示满条。记录警告。

**事件时序异常**:
- **如果同一帧收到多个 OnDamagePercentChanged**: 依次处理，最终显示最新值。中间值不产生可见动画——只有最后一次更新触发跳变动画。
- **如果 OnSkillEquipped 到达时对应槽位已有图标（不应发生）**: 覆盖显示为新技能图标，重新播放装备动画。记录警告。
- **如果 OnSkillUnequipped 到达时对应槽位已显示为空**: 忽略，不播放动画。
- **如果格斗状态机状态变化事件在 HUD 初始化之前到达**: 忽略。HUD 仅在 Countdown 状态下激活，在此之前的事件被丢弃。

**可见性控制异常**:
- **如果游戏状态管理未发送状态变化事件**: HUD 保持上一状态。最长等待 3 帧后主动查询 `IGameState.GetState()` 确认。防御性设计，避免 HUD 卡在错误可见性。
- **如果 Countdown 状态异常跳过（直接进入 Battle）**: HUD 在 Battle 状态开始时强制初始化并淡入（0.3 秒），显示当前上游数据的快照值。

**分辨率和布局**:
- **如果窗口分辨率不是 16:9**: HUD 通过 Canvas Scaler 的 Scale With Screen Size 模式自适应。元素比例保持不变，可能产生左右黑边但不裁剪 HUD。
- **如果窗口宽度不足以显示完整 HUD**: 信息区缩小但不低于最小尺寸（伤害% 24px、进度条 60px 宽、技能槽 32px）。极限情况优先保证伤害%可读。

**上游系统未响应**:
- **如果伤害计算系统停止发送事件**: 百分比冻结在最后收到的值，不归零。超时 5 秒后在百分比旁显示 "?" 标记提示数据可能过期。
- **如果专注值系统停止发送事件**: 同理，进度条冻结 + "?" 标记。

## Dependencies

| 依赖系统 | 方向 | 类型 | 数据接口 | GDD 状态 |
|---------|------|------|---------|---------|
| 伤害计算系统 | 上游（硬依赖） | 事件 | `OnDamagePercentChanged(CharacterId, newPercent)` — 更新百分比数字和颜色 | Designed |
| 专注值系统 | 上游（硬依赖） | 事件 | `OnFocusChanged(CharacterId, FocusPoints, UnlockThreshold)` — 更新进度条；`OnFocusReady(CharacterId, UnlockedCount)` — 触发解锁动画 | Designed |
| 技能装备管理 | 上游（硬依赖） | 事件 | `OnSkillEquipped(CharacterId, SlotIndex, SkillData)` — 更新技能图标；`OnSkillUnequipped(CharacterId, SlotIndex)` — 清空槽位 | Designed |
| 对局管理系统 | 上游（硬依赖） | 事件 + 查询 | `OnRoundEnd(winnerIndex, scores)` — 更新比分；`GetScores()`, `GetCurrentRound()`, `GetMatchState()` — 查询比赛数据 | Designed |
| 游戏状态管理 | 上游（硬依赖） | 事件 | 状态变化事件 — 控制HUD可见性（隐藏/淡入/可见/淡出） | Designed |
| 格斗状态机 | 上游（软依赖） | 事件 | 状态变化事件 — 技能槽位激活/打断高亮 | Designed |

**双向一致性验证**:
- 伤害计算系统 GDD: "战斗HUD | 伤害 → HUD | DamagePercent 变化事件（显示百分比数字）" ✓ 一致
- 专注值系统 GDD: "战斗HUD | 专注值 → HUD | OnFocusChanged(CharacterId, FocusPoints, UnlockThreshold) — 专注值变化事件" ✓ 一致
- 技能装备管理 GDD: "战斗HUD | 下游（硬依赖） | 事件 | OnSkillEquipped(CharacterId, SlotIndex, SkillData) — 更新技能槽位图标" ✓ 一致
- 对局管理系统 GDD: "战斗HUD | 下游（硬依赖） | 事件 | OnRoundEnd, OnMatchEnd, 比分/回合数据" ✓ 一致

**Note**: 战斗HUD是下游终端系统，无下游依赖。所有数据流向为上游 → HUD，HUD 不向任何系统发送数据。

## Tuning Knobs

| 旋钮名 | 默认值 | 安全范围 | 调高效果 | 调低效果 | 所属组件 |
|--------|--------|---------|---------|---------|---------|
| DamageNumberBounceScale | 1.3 | 1.0–1.5 | 百分比跳变更明显，视觉反馈更强 | 跳变更小，接近无动画 | 伤害百分比 |
| DamageNumberBounceDuration | 0.15s | 0.05–0.30s | 跳变动画更慢，更"肉" | 跳变更快，更干脆 | 伤害百分比 |
| FocusPulseMinFrequency | 1.0 Hz | 0.5–2.0 Hz | 基础脉动更快 | 基础脉动更慢 | 专注值进度条 |
| FocusPulseMaxFrequency | 3.0 Hz | 2.0–5.0 Hz | 接近满时脉动更快更紧张 | 接近满时脉动更缓 | 专注值进度条 |
| FocusPulseThreshold | 0.8 | 0.6–0.95 | 更早开始脉动 | 只在非常接近满时脉动 | 专注值进度条 |
| SkillSlotSize | 48px | 32–64px | 技能槽更大更醒目，但占更多屏幕空间 | 技能槽更小更紧凑 | 技能槽位 |
| SkillEquipAnimDuration | 0.25s | 0.1–0.5s | 装备动画更长更有仪式感 | 装备动画更快更干脆 | 技能槽位 |
| HudFadeInDuration | 0.3s | 0.1–0.5s | 淡入更慢更优雅 | 淡入更快更即时 | HUD 全局 |
| HudFadeOutDuration | 0.3s | 0.1–0.5s | 淡出更慢 | 淡出更快 | HUD 全局 |
| DataStaleTimeout | 5.0s | 2.0–10.0s | 更宽容，更晚显示过期标记 | 更严格，更快提示数据可能过期 | 全局 |

**旋钮交互说明**:
- `FocusPulseMinFrequency` 和 `FocusPulseMaxFrequency` 定义脉动加速区间——Max 必须 > Min，否则无效
- `SkillSlotSize` 影响底部信息区整体宽度——4 个槽位 + 间距 + 百分比 + 进度条必须适配屏幕宽度
- 所有动画持续时间之和不应超过 0.5 秒（任何单个元素的动画），避免影响战斗节奏感知

## Visual/Audio Requirements

### 视觉风格

战斗HUD的视觉风格遵循游戏概念的"能量爆发"视觉规则：
- **暗底亮色原则** — HUD 元素使用鲜明色彩（职业色、稀有度色），背景元素使用暗色半透明，与暗色场地背景协调但不混淆
- **信息层级** — 伤害百分比（最大、最醒目）> 专注值进度条（中等、持续脉动）> 技能槽位（较小、被动更新）> 比分（最小、仅在变化时注意）
- **无遮挡原则** — 所有 HUD 元素位于屏幕边缘，战斗区域（屏幕中央 70%）完全无遮挡

### 动画规范

| 动画 | 持续时间 | 缓动函数 | 触发条件 |
|------|---------|---------|---------|
| 百分比数字弹跳 | 0.15s | EaseOutBack | OnDamagePercentChanged |
| 颜色过渡 | 0.2s | EaseInOut | 颜色阈值跨越 |
| 专注值进度条更新 | 即时（无动画） | — | OnFocusChanged |
| 专注值解锁闪白 | 0.1s | Linear | OnFocusReady |
| 技能图标弹入 | 0.25s | EaseOutBack | OnSkillEquipped |
| 技能槽闪红 | 0.2s | Linear | 技能被打断 |
| HUD 淡入 | 0.3s | EaseOut | 进入 Countdown |
| HUD 淡出 | 0.3s | EaseIn | 进入 Results |

### 音频协调

战斗HUD不直接播放音效——音效由各上游系统和音效系统负责。HUD 仅负责视觉反馈。以下视觉动画应与对应音效协调时序：

| 视觉动画 | 应协调的音效 | 来源系统 |
|---------|------------|---------|
| 百分比颜色变红（150%+） | 百分比里程碑警示音 | 伤害计算系统 |
| 专注值解锁闪白 | 解锁音效（OnFocusUnlock） | 专注值系统 |
| 技能图标弹入 | 装备完成音效（OnSkillEquipped） | 技能装备管理 |

## UI Requirements

**实现技术选择**:
- 推荐使用 Unity UI Toolkit（UXML/USS）实现战斗HUD——现代、性能好、CSS 样式易于迭代
- 基准分辨率：1920×1080，使用 Scale With Screen Size 自适应
- 渲染模式：Screen Space - Overlay（不需要摄像机引用）

**HUD 根结构（UXML 概念）**:
```
BattleHUD (root)
├── ScoreArea (顶部中央)
│   ├── ScoreText "1 - 0"
│   ├── RoundText "R2/3"
│   └── MatchPointIndicator "●"
├── P1InfoArea (左下角)
│   ├── DamagePercent "42%"
│   ├── FocusBar (进度条)
│   └── SkillSlots
│       ├── Slot1 [■/□]
│       ├── Slot2 [■/□]
│       ├── Slot3 [■/□]
│       └── Slot4 [■/□]
└── P2InfoArea (右下角, 镜像)
    ├── SkillSlots
    ├── FocusBar
    └── DamagePercent "28%"
```

**手柄导航**: 战斗HUD是纯信息展示，无交互导航需求。不需要手柄方向键焦点管理。

**无障碍考虑**:
- 百分比颜色变化不是唯一信息渠道——数字本身也携带信息
- 进度条脉动不应过于激烈（遵守光敏癫痫安全阈值）
- 技能槽位的按键提示文字最小 12px

> **📌 UX 标记 — 战斗HUD**: 本系统是 UI 系统的核心呈现层。在 Pre-Production 阶段，运行 `/ux-design` 为战斗 HUD 创建 UX spec，包括各元素的精确像素尺寸、间距、响应式断点、动画曲线细节。Story 应引用 `design/ux/hud.md`。

## Acceptance Criteria

### 伤害百分比显示

- **GIVEN** 角色当前 DamagePercent=0.0, **WHEN** HUD 初始化, **THEN** 显示 "0%"，白色
- **GIVEN** 角色当前 DamagePercent=42.7, **WHEN** OnDamagePercentChanged 到达, **THEN** 显示 "42%"，白色，触发缩放弹跳动画
- **GIVEN** 角色当前 DamagePercent=49.0, **WHEN** 命中后 DamagePercent=52.0, **THEN** 显示从 "49%" 变为 "52%"，颜色从白色平滑过渡到黄色（0.2 秒）
- **GIVEN** 角色当前 DamagePercent=99.0, **WHEN** 命中后 DamagePercent=101.0, **THEN** 颜色从黄色过渡到橙色
- **GIVEN** 角色当前 DamagePercent=149.0, **WHEN** 命中后 DamagePercent=152.0, **THEN** 颜色从橙色变为红色闪烁（0.5Hz）
- **GIVEN** 角色当前 DamagePercent=1002.3, **WHEN** OnDamagePercentChanged 到达, **THEN** 显示 "999+"

### 专注值进度条

- **GIVEN** FocusPoints=0, UnlockThreshold=40.0, **WHEN** HUD 初始化, **THEN** 进度条空（0%），职业色填充
- **GIVEN** FocusPoints=32.0, UnlockThreshold=40.0, **WHEN** OnFocusChanged 到达, **THEN** 进度条填充 80%，基础脉动（1.0Hz）
- **GIVEN** FocusPoints=36.0, UnlockThreshold=40.0（>80%）, **WHEN** OnFocusChanged 到达, **THEN** 进度条填充 90%，脉动加速（Lerp(1,3,0.5)=2Hz）
- **GIVEN** FocusPoints=41.6, UnlockThreshold=40.0（触发解锁）, **WHEN** OnFocusReady 到达, **THEN** 进度条闪白→清空→1.6/45.0=3.6% 重新填充
- **GIVEN** UnlockedCount=4 (MaxSkillsPerMatch), **WHEN** OnFocusChanged 到达, **THEN** 进度条变灰，显示 "MAX"

### 技能槽位

- **GIVEN** 所有槽位为空, **WHEN** HUD 初始化, **THEN** 显示 4 个灰色轮廓空槽位，带按键提示
- **GIVEN** Slot 1 为空, **WHEN** OnSkillEquipped(P1, 1, Fireball[Common]), **THEN** Slot 1 显示火球图标 + 蓝色边框，播放弹入动画
- **GIVEN** Slot 1 已装备 Fireball, **WHEN** FSM 进入 Fireball.Startup, **THEN** Slot 1 高亮 + 脉动边框
- **GIVEN** Slot 1 Fireball 执行中被 HitStun 打断, **WHEN** FSM 状态变化, **THEN** Slot 1 短暂闪红（0.2 秒）
- **GIVEN** Slot 1-4 全部已装备, **WHEN** OnSkillUnequipped(P1, 1/2/3/4) 到达, **THEN** 所有槽位恢复为灰色空槽

### 比分显示

- **GIVEN** 比赛初始化, **WHEN** Countdown 开始, **THEN** 顶部中央显示 "0 - 0  R1/3"
- **GIVEN** scores=[0,0], **WHEN** P1 赢得第 1 局, **THEN** 显示 "1 - 0  R2/3"
- **GIVEN** Bo3, scores=[1,0], WinsNeeded=2, **WHEN** 比分更新, **THEN** P1 分数旁显示赛点标记 "●"（黄色脉动）
- **GIVEN** scores=[2,1], WinsNeeded=2, **WHEN** OnMatchEnd 到达, **THEN** 比分区显示最终结果

### HUD 可见性

- **GIVEN** GameState = MainMenu, **WHEN** HUD 检查可见性, **THEN** HUD 完全隐藏
- **GIVEN** GameState 从 MatchLoading → Countdown, **WHEN** 状态变化, **THEN** HUD 淡入（0.3 秒），显示初始值
- **GIVEN** GameState = BattleEnd → Results, **WHEN** 状态变化, **THEN** HUD 淡出（0.3 秒）

### 性能

- **GIVEN** 2 人对战进行中, **THEN** HUD 每帧渲染开销 < 0.8ms（帧预算 16.6ms 的 5%）
- **GIVEN** 同一帧收到 3 个事件更新, **THEN** HUD 在同一帧内完成所有视觉更新，不丢帧

> `qa-lead` 未咨询 — Lean 模式。正式上线前需人工审核。

## Open Questions

1. **HUD 是否需要支持屏幕中央弹出通知？** 当前设计无中央弹出元素，但解锁瞬间的仪式感可能需要全屏闪光或中央弹出。这在对局UI和能量视觉系统 GDD 中可能有更详细定义。如果需要，需在战斗HUD中预留中央通知区域。（Owner: UX 设计师，里程碑: UX 规范创建）

2. **P2 的技能键提示是否应显示手柄图标而非数字？** 当前设计 P1 用数字 "1/2/3/4"，P2 可配置。如果两个玩家都用键盘，键位不同如何显示？需要与 Input System 配置协调。（Owner: UX 设计师，里程碑: UX 规范创建）

3. **比分区是否需要显示赛制格式（如 "Bo3"）？** 当前仅显示回合序号（R2/3）。玩家可能需要知道当前是 Bo1/Bo3/Bo5。（Owner: 设计师，里程碑: MVP）
