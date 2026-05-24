# 专注值系统 (Focus System)

> **Status**: In Design
> **Author**: SeanDuan + agents
> **Last Updated**: 2026-05-23
> **Implements Pillar**: Pillar 2: 每局都是新故事, Pillar 4: 快速战斗

## Overview

专注值系统是职业对决肉鸽循环的资源引擎，负责将玩家的攻击命中行为转化为可量化的"专注值"资源——每次命中对手，攻击者获得与攻击强度成正比的专注值；当专注值积累到解锁阈值时，系统触发一次技能解锁事件，通知下游的技能抽取系统随机抽取新技能加入角色招式库。对于玩家而言，专注值系统创造了战斗中的"第二根进度条"——除了伤害百分比（"我有多危险"），还有专注值条（"我离下一个新技能有多近"）。每次命中的专注值增长让攻击行为有了双重回报：既造成伤害，又推进了自己的成长。专注值的积累速度和解锁频率直接决定了对局的"肉鸽密度"——太快则每次解锁失去仪式感，太慢则玩家感受不到成长。设计目标是：开局 45-60 秒内触发第一次解锁，此后每 30-45 秒解锁一次，一局 3-5 分钟内解锁 2-4 个随机技能。专注值系统从攻击系统接收命中事件（`OnAttackHit`），查询 AttackData 中的专注值奖励参数，更新角色的专注值状态，并在达到阈值时发出解锁事件（`OnFocusReady`）通知技能抽取系统。新一局开始时专注值重置为 0。

## Player Fantasy

**核心幻想：「每次命中都在靠近下一次惊喜」**

玩家应该感觉专注值条是一个充满可能性的蓄能器——每一次命中对手，进度条涨一截，"下一个技能会是什么"的期待感随之升温。当进度条满的那一刻——屏幕闪烁、角色发光、技能图标弹出——是一种被精心设计的"开箱"时刻：不确定性带来的兴奋，与"不管抽到什么我都能用"的信心交织。

专注值系统创造的不是"管理资源的压力"，而是"越打越兴奋的正反馈"。玩家不需要做资源管理决策——只要打中人就在积累。但积累的速度和节奏创造了自然的情绪弧线：开局快速涨条的兴奋感，中期抽到好技能后"我变强了"的满足感，以及后期"还能再来一个吗"的期待。

**关键情感时刻**：
- **进度条即将满的紧张感** — 专注值 90%，"再打中一次就解锁了"，进攻欲望陡增
- **解锁瞬间的爆发感** — 画面停顿、特效炸裂、技能图标弹入——这是每局最令人兴奋的 2 秒
- **连续解锁的疯狂感** — 后期技能越来越多，战斗变得越来越混乱和精彩，"我已经有 3 个随机技能了"
- **落后方的翻盘希望** — 即使伤害百分比落后，专注值积累给落后方一个"我快解锁新技能了"的心理支撑

**支柱对齐**：
- 服务 **Pillar 2: 每局都是新故事** — 专注值是随机技能的"入口"，它的积累频率直接决定每局的多样性和独特性
- 服务 **Pillar 4: 快速战斗** — 专注值积累足够快，3-5 分钟内完成 2-4 次解锁，不拖沓

> `creative-director` not consulted — Lean mode. Review manually before production.

## Detailed Design

### Core Rules

**1. 专注值资源模型**

每个角色维护一个专注值（`FocusPoints`），对局开始时为 0。专注值有两个获取来源和明确的消耗机制。

- 专注值范围为 `[0, FocusCap]`
- 专注值只增不减（MVP 无消耗/降级机制）
- 达到 `UnlockThreshold` 时自动触发技能解锁事件
- 解锁后专注值归零，下一次解锁的阈值提高

**2. 专注值获取——攻击者奖励**

当攻击者的攻击命中对手时（攻击系统发出 `OnAttackHit` 事件），攻击者获得专注值奖励。

- 获取量与攻击的 `BaseDamage` 成正比：`FocusGain_Attacker = BaseDamage × FocusGainRate_Attacker`
- 所有命中一视同仁——不区分攻击类型（地面/空中/冲刺）、不区分投射物/近战
- 轻击获得少、重击获得多——自然鼓励激进进攻

**3. 专注值获取——被击者补偿**

当角色被攻击命中时，被击者也获得少量专注值补偿。

- 获取量与承受的 `BaseDamage` 成正比：`FocusGain_Defender = BaseDamage × FocusGainRate_Defender`
- `FocusGainRate_Defender` 远低于 `FocusGainRate_Attacker`（约为攻击者奖励的 1/3）
- 被击补偿的目标：让落后方"虽然挨打但也在积累"，提供心理支撑，避免完全被压制时毫无希望

**4. 递增阈值系统**

每次技能解锁后，下一次解锁需要更多专注值：

```
UnlockThreshold_n = FocusBaseThreshold + (n × FocusThresholdGrowth)
```

- `n` = 本次对局中该角色已解锁的技能数量（0-indexed）
- 第一次解锁：`FocusBaseThreshold`（最低门槛）
- 第二次解锁：`FocusBaseThreshold + FocusThresholdGrowth`
- 第三次解锁：`FocusBaseThreshold + 2 × FocusThresholdGrowth`
- 以此类推

**5. 自动解锁触发**

当 `FocusPoints >= UnlockThreshold_n` 时，在当前命中处理的同一帧内：

1. 发出 `OnFocusReady` 事件（通知技能抽取系统执行随机抽取）
2. 专注值扣减：`FocusPoints -= UnlockThreshold_n`
3. 已解锁计数递增：`UnlockedCount += 1`
4. 下次阈值更新为 `UnlockThreshold_{n+1}`

解锁是自动的——不需要玩家按键确认。理由：格斗游戏节奏快，要求玩家额外按键解锁会打断战斗流畅度。

**6. 专注值上限**

`FocusCap` 是专注值的最大值。如果专注值加上当次获取量超过 FocusCap，钳制到 FocusCap。

- FocusCap 设计为略高于第一解锁阈值，确保专注值不会过度"溢出"
- 后续解锁阈值可能超过 FocusCap——此时玩家需要多次积累-解锁循环
- FocusCap 防止"囤积专注值一次性解锁多个技能"的设计漏洞

**7. 重置规则**

- 新一局开始时：所有角色 `FocusPoints = 0`，`UnlockedCount = 0`
- 角色被 KO 后：专注值状态保留（不重置），因为 KO 后本局可能继续（取决于对局管理系统规则）
- 同一局内 KO 后重生：专注值保留（待对局管理系统 GDD 定义重生规则）

**8. 被动解锁上限**

每局最多解锁 `MaxSkillsPerMatch` 个技能（MVP: 4）。达到上限后：
- 专注值继续积累但不再触发解锁事件
- 专注值到达 FocusCap 后停止积累
- 目的：防止对局后期技能过多导致混乱失控

### States and Transitions

专注值系统是无状态的资源管理器——不维护独立状态机。其行为由攻击系统的事件驱动。

| 触发条件 | 行为 |
|---------|------|
| 攻击系统 `OnAttackHit(AttackData, TargetId)` | 攻击者获得 `BaseDamage × FocusGainRate_Attacker` 专注值 |
| 攻击系统 `OnAttackHit(AttackData, TargetId)`（被击方） | 被击者获得 `BaseDamage × FocusGainRate_Defender` 专注值 |
| `FocusPoints >= UnlockThreshold` 且 `UnlockedCount < MaxSkillsPerMatch` | 发出 `OnFocusReady`，扣减专注值，递增计数 |
| `FocusPoints >= FocusCap` | 钳制到 FocusCap |
| 新一局开始 | 所有角色 FocusPoints = 0，UnlockedCount = 0 |
| `UnlockedCount >= MaxSkillsPerMatch` | 专注值继续积累到 FocusCap 但不再触发解锁 |

### Interactions with Other Systems

| 系统 | 数据流方向 | 接口描述 |
|------|-----------|---------|
| 攻击系统 | 攻击 → 专注值 | `OnAttackHit(AttackData, TargetId)` — 命中事件驱动专注值积累 |
| 职业系统 | 间接 | AttackData 来源包含 BaseDamage，职业通过攻击系统间接影响专注值获取速度 |
| 技能抽取系统 | 专注值 → 技能抽取 | `OnFocusReady(CharacterId, UnlockedCount)` — 通知技能抽取系统执行随机抽取 |
| 技能装备管理 | 间接 | 技能抽取结果通过技能装备管理注入格斗状态机 |
| 战斗HUD | 专注值 → HUD | `OnFocusChanged(CharacterId, FocusPoints, UnlockThreshold)` — 专注值变化事件 |
| 能量视觉系统 | 专注值 → 视觉 | `OnFocusChanged` 事件驱动进度条和特效 |
| 对局管理系统 | 对局 → 专注值 | `OnRoundStart` 触发专注值重置 |
| 格斗状态机 | 专注值 → FSM | 无直接交互（解锁的技能通过技能装备管理注入 FSM） |

## Formulas

**单位系统**: 无单位资源值，时间参考以 60Hz 帧为基准（实际专注值更新由命中事件驱动，非每帧更新）。

### 1. 攻击者专注值获取

`FocusGain_Attacker = BaseDamage × FocusGainRate_Attacker`

| Variable | Symbol | Type | Range | Description |
|----------|--------|------|-------|-------------|
| 攻击基础伤害 | BaseDamage | float | 3.0–15.0 | AttackData 中定义的伤害值 |
| 攻击者获取比率 | FocusGainRate_Attacker | float | 0.20–0.40 | 每 1 点伤害对应的专注值获取量 |
| 攻击者专注值增量 | FocusGain_Attacker | float | 0.9–4.5 | 本次命中攻击者获得的专注值 |

**Output Range**: 0.9（Rogue 轻击）到 4.5（Warrior 重击）
**Example**: Warrior GroundAttack (BaseDamage=12.0) 命中 → FocusGain = 12.0 × 0.30 = **3.6** 专注值。Rogue GroundAttack (BaseDamage=4.0) → **1.2** 专注值。

### 2. 被击者专注值获取

`FocusGain_Defender = BaseDamage × FocusGainRate_Defender`

| Variable | Symbol | Type | Range | Description |
|----------|--------|------|-------|-------------|
| 攻击基础伤害 | BaseDamage | float | 3.0–15.0 | 命中攻击的伤害值 |
| 被击者获取比率 | FocusGainRate_Defender | float | 0.05–0.15 | 每 1 点伤害对应的专注值补偿量（约为攻击者的 1/3） |
| 被击者专注值增量 | FocusGain_Defender | float | 0.3–1.5 | 本次命中被击者获得的专注值 |

**Output Range**: 0.3（被 Rogue 轻击）到 1.5（被 Warrior 重击）
**Example**: 被 Warrior GroundAttack (BaseDamage=12.0) 命中 → FocusGain = 12.0 × 0.10 = **1.2** 专注值。被 Rogue GroundAttack (BaseDamage=4.0) 命中 → **0.4** 专注值。

### 3. 递增解锁阈值

`UnlockThreshold_n = FocusBaseThreshold + (n × FocusThresholdGrowth)`

| Variable | Symbol | Type | Range | Description |
|----------|--------|------|-------|-------------|
| 基础阈值 | FocusBaseThreshold | float | 20.0–40.0 | 第一次技能解锁需要的专注值 |
| 阈值增长率 | FocusThresholdGrowth | float | 3.0–8.0 | 每次解锁后阈值增加的量 |
| 已解锁次数 | n | int | 0–3 | 本次对局中该角色已解锁的技能数量 |
| 解锁阈值 | UnlockThreshold_n | float | 40.0–55.0 | 第 n 次解锁需要的专注值 |

**Output Range**: 40.0（第一次）到 55.0（第四次）
**Example**: 第一次解锁 (n=0): 40.0 + 0×5.0 = **40.0**。第三次解锁 (n=2): 40.0 + 2×5.0 = **50.0**。

**解锁阈值序列**:

| 解锁次序 (n) | 阈值 | 预计解锁时间* |
|-------------|------|-------------|
| 0（第一次） | 40.0 | ~50s |
| 1（第二次） | 45.0 | ~106s |
| 2（第三次） | 50.0 | ~169s |
| 3（第四次） | 55.0 | ~238s |

*基于中等战斗节奏（获取率 0.80 专注值/s）的估算。实际时间受命中频率和伤害分布影响。

### 4. 专注值更新与解锁判定

```
FocusPoints_new = Min(FocusPoints_old + FocusGain, FocusCap)

if FocusPoints_new >= UnlockThreshold_n AND UnlockedCount < MaxSkillsPerMatch:
    Trigger OnFocusReady(CharacterId, UnlockedCount)
    FocusPoints_final = FocusPoints_new - UnlockThreshold_n
    UnlockedCount += 1
else:
    FocusPoints_final = FocusPoints_new
```

| Variable | Symbol | Type | Range | Description |
|----------|--------|------|-------|-------------|
| 当前专注值 | FocusPoints_old | float | 0.0–55.0 | 更新前的专注值 |
| 本次专注值增量 | FocusGain | float | 0.3–4.5 | 攻击者或被击者获取的专注值 |
| 专注值上限 | FocusCap | float | 50.0–60.0 | 专注值最大值 |
| 解锁阈值 | UnlockThreshold_n | float | 40.0–55.0 | 当前解锁阈值 |
| 已解锁次数 | UnlockedCount | int | 0–4 | 本局已解锁的技能数量 |
| 每局最大解锁数 | MaxSkillsPerMatch | int | 2–6 | 每局最多解锁的技能数量 |
| 更新后专注值 | FocusPoints_final | float | 0.0–55.0 | 更新后的专注值（含解锁扣减） |

**Output Range**: FocusPoints 在 [0, FocusCap] 内，UnlockedCount 在 [0, MaxSkillsPerMatch] 内
**Example**: FocusPoints=38.0，Warrior 命中 (BaseDamage=12.0) 作为攻击者 → FocusGain=3.6 → FocusPoints_new=Min(41.6, 55.0)=41.6。41.6 >= 40.0（首次阈值）且 UnlockedCount=0 < 4 → 触发解锁，FocusPoints_final=41.6-40.0=**1.6**，UnlockedCount=1。

### 5. 专注值获取率估算（设计师参考）

`FocusIncomeRate = HitFrequency × AvgBaseDamage × (FocusGainRate_Attacker + FocusGainRate_Defender)`

| Variable | Symbol | Type | Range | Description |
|----------|--------|------|-------|-------------|
| 命中频率（单方视角） | HitFrequency | float | 0.15–0.33 次/s | 每 3-6 秒命中一次（攻击者视角） |
| 平均伤害 | AvgBaseDamage | float | 5.0–12.0 | 平均命中伤害 |
| 攻击者获取比率 | FocusGainRate_Attacker | float | 0.30 | 攻击者获取比率 |
| 被击者获取比率 | FocusGainRate_Defender | float | 0.10 | 被击者获取比率 |
| 专注值获取率 | FocusIncomeRate | float | 0.6–1.3 | 每秒获取的专注值 |

**Example**: 中等节奏（HitFrequency=0.25/s, AvgBaseDamage=8.0）: 0.25 × 8.0 × (0.30+0.10) = **0.80** 专注值/s。活跃战斗（0.33/s, 10.0）: **1.32** 专注值/s。

## Edge Cases

**解锁触发相关**:
- **如果一次命中的专注值增量导致 FocusPoints 同时超过 UnlockThreshold 和 FocusCap**: 先钳制到 FocusCap，再判定解锁。FocusPoints_new = Min(FocusPoints_old + FocusGain, FocusCap)。如果 FocusCap >= UnlockThreshold（对所有阈值都成立），解锁正常触发。解锁后 FocusPoints_final = FocusCap - UnlockThreshold_n。
- **如果解锁后剩余专注值已经超过下一次阈值**: 不连续触发。解锁事件处理后，下一次阈值在下一帧生效。如果 FocusPoints_final >= UnlockThreshold_{n+1}，在同一帧的下一个命中事件中再次触发——但这需要两个独立的命中事件，不可能在一次处理中连续触发两次。
- **如果 UnlockedCount 已达 MaxSkillsPerMatch (4) 且 FocusPoints 超过 FocusCap**: 钳制到 FocusCap。不再触发解锁事件。专注值"满了但不解锁"的状态对玩家可见（HUD 显示满条）。
- **如果 UnlockThreshold_n 因数据错误为负数或零**: 视为数据错误，强制最小值 1.0。阈值不可能为零或负——否则首次命中就会触发解锁。

**数值边界相关**:
- **如果 BaseDamage 为零（纯击退攻击）**: FocusGain = 0.0 × FocusGainRate = 0.0。该命中不产生专注值。合法设计——纯击退攻击不应提供资源奖励。
- **如果 FocusGainRate_Attacker 为零**: 攻击者永远无法获得专注值，只有被击补偿在积累。解锁速度极慢但不是不可能。合法但极端的配置——不应在正常设计中出现。
- **如果 FocusPoints 因浮点精度超过 FocusCap**: 在每次更新后强制钳制 `FocusPoints = Min(FocusPoints, FocusCap)`。浮点精度问题不应导致逻辑错误。
- **如果两个角色同一帧互相命中，都达到解锁阈值**: 各自独立处理。碰撞系统在同一帧发送两个独立的命中事件，专注值系统逐个处理。两个角色都可以在同一帧触发解锁，互不干扰。

**对局流程相关**:
- **如果新一局开始时 FocusPoints 未重置**: 专注值系统必须在对局管理系统的 OnRoundStart 事件中强制重置所有角色的 FocusPoints = 0.0 和 UnlockedCount = 0。
- **如果角色 KO 后重生但专注值未重置**: 专注值保留（设计意图：KO 不惩罚专注值积累）。待对局管理系统定义重生规则后确认。
- **如果对局在技能抽取系统处理 OnFocusReady 之前结束**: OnFocusReady 事件已发出但技能未实际抽取。下一局开始时专注值重置，该解锁"丢失"——这是可接受的，因为对局结束意味着所有状态归零。

**数据完整性**:
- **如果攻击系统的 OnAttackHit 事件中 AttackData 为 null**: 忽略该事件，不更新专注值。记录警告。
- **如果 CharacterId 无效或不存在**: 忽略该事件。记录警告。
- **如果 UnlockedCount 超过 MaxSkillsPerMatch**: 钳制到 MaxSkillsPerMatch。不应发生——如果发生，说明解锁判定逻辑有 bug。

## Dependencies

| 依赖系统 | 方向 | 类型 | 数据接口 | GDD 状态 |
|---------|------|------|---------|---------|
| 攻击系统 | 上游（硬依赖） | 事件 | `OnAttackHit(AttackData, TargetId)` — 命中事件驱动专注值积累 | Designed |
| 对局管理系统 | 上游（软依赖） | 事件 | `OnRoundStart` — 触发专注值和计数器重置 | Not Started |
| 技能抽取系统 | 下游（硬依赖） | 事件 | `OnFocusReady(CharacterId, UnlockedCount)` — 触发随机技能抽取 | Not Started |
| 战斗HUD | 下游（硬依赖） | 事件 | `OnFocusChanged(CharacterId, FocusPoints, UnlockThreshold)` — 专注值变化通知 | Not Started |
| 能量视觉系统 | 下游（软依赖） | 事件 | `OnFocusChanged` — 专注值进度和特效 | Not Started |
| 职业系统 | 间接 | 数据来源 | AttackData 中的 BaseDamage 影响专注值获取速度，但不直接交互 | Designed |
| 格斗状态机 | 间接 | 无直接交互 | 解锁的技能通过技能装备管理注入 FSM | Designed |

**向上提供的接口契约**:
- `IFocusSystem` 接口: 专注值管理和查询入口
- `OnFocusReady(CharacterId, UnlockedCount)`: 专注值达到解锁阈值事件
- `OnFocusChanged(CharacterId, FocusPoints, UnlockThreshold)`: 专注值变化事件（供 HUD 和视觉系统）
- `GetFocusPoints(CharacterId)`: 查询角色当前专注值
- `GetUnlockThreshold(CharacterId)`: 查询角色当前解锁阈值
- `GetUnlockedCount(CharacterId)`: 查询角色已解锁技能数量
- `ResetFocus(CharacterId)`: 重置角色专注值和计数器
- `ResetAll()`: 重置所有角色专注值和计数器

**双向一致性检查**:
- 碰撞判定系统 GDD: "专注值系统 | 碰撞 → 专注值 | 通过攻击系统转发命中事件" ✓ 一致（专注值系统通过攻击系统间接获取命中事件）
- 伤害计算系统 GDD: "专注值系统 | 间接 | 命中事件驱动专注值积累（通过攻击系统转发）" ✓ 一致
- 格斗状态机 GDD: "专注值系统 | 专注值 → FSM | 无直接交互" ✓ 一致

## Tuning Knobs

| 旋钮名 | 默认值 | 安全范围 | 调高效果 | 调低效果 | 所属公式 |
|--------|--------|---------|---------|---------|---------|
| FocusGainRate_Attacker | 0.30 | 0.20–0.40 | 攻击者每次命中获得更多专注值，解锁更快 | 攻击者积累更慢，首次解锁延迟 | 攻击者获取 |
| FocusGainRate_Defender | 0.10 | 0.05–0.15 | 被击者补偿更多，落后方翻盘机会更大 | 被击者几乎不获得专注值，纯惩罚 | 被击者获取 |
| FocusBaseThreshold | 40.0 | 20.0–60.0 | 首次解锁需要更多积累，首次解锁更晚 | 首次解锁更快，可能在前 30 秒就触发 | 递增阈值 |
| FocusThresholdGrowth | 5.0 | 3.0–10.0 | 后续解锁递增更陡，后期解锁显著更难 | 后续解锁几乎与首次一样，节奏平坦 | 递增阈值 |
| FocusCap | 55.0 | 45.0–65.0 | 允许更多专注值囤积，解锁后溢出更多 | 严格限制囤积，解锁后溢出很少 | 专注值上限 |
| MaxSkillsPerMatch | 4 | 2–6 | 每局可能解锁更多技能，对局更混乱 | 限制技能数量，对局更稳定 | 解锁上限 |

**旋钮交互警告**:
- `FocusGainRate_Attacker` 和 `FocusBaseThreshold` 共同决定首次解锁时间——调一个必须检查另一个
- `FocusGainRate_Defender` 影响落后方的心理支撑强度——过高会让"被击"也成为一种策略（故意挨打刷专注值）
- `FocusThresholdGrowth` 和 `FocusCap` 必须协调：所有 4 个阈值（FocusBaseThreshold 到 FocusBaseThreshold + 3×Growth）必须 <= FocusCap，否则后续解锁无法触发
- `MaxSkillsPerMatch` 影响游戏混乱度和平衡——超过 4 个随机技能后，角色之间的差异化可能被随机性淹没

## Visual/Audio Requirements

**视觉反馈（MVP 核心层）**:

**专注值进度条**:
- 位于角色头顶或 HUD 角落的进度条，显示 FocusPoints / UnlockThreshold 的比例
- 进度条使用职业色（Warrior 红、Rogue 紫、Mage 蓝）
- 进度条接近满时（>80%）脉动效果加速，传达"即将解锁"的紧迫感

**专注值增量反馈**:
- 每次获取专注值时，进度条短暂高亮闪烁
- 增量数值弹出（类似伤害数字，但用专注值专用颜色——白色/银色）
- 攻击者增量弹出在攻击者附近，被击者增量弹出在被击者附近

**解锁瞬间特效**:
- 进度条满时：短暂画面定格（2-3 帧），全局白色闪光
- 角色轮廓爆发出强烈的职业色光芒，持续约 0.5 秒后衰减
- 解锁特效结束后，进度条瞬间清空（扣除阈值后剩余的专注值重新填充进度条）
- 此特效应与游戏概念的"技能解锁要有仪式感"视觉规则对齐

**解锁后状态**:
- 解锁后进度条不是空的——剩余专注值立即显示在新进度条中
- 已解锁技能数量通过 HUD 技能图标显示（非专注值系统直接负责）

**音频反馈（定义触发事件，音效系统实现）**:

| 音频事件 | 触发时机 | 描述 |
|---------|---------|------|
| `OnFocusGain_Attacker` | 攻击者获得专注值 | 轻微"充能"音效，音调随进度升高 |
| `OnFocusGain_Defender` | 被击者获得专注值 | 更轻微的音效，区别于攻击者获取 |
| `OnFocusNearFull` | FocusPoints > 80% 阈值 | 低频脉动音效，节奏随进度加速 |
| `OnFocusUnlock` | 解锁触发 | 强烈的"解锁"音效——能量爆发声 + 上升音调 |
| `OnFocusCap` | FocusPoints 到达上限 | 短促"满"音效提示 |

## UI Requirements

**专注值进度条（战斗 HUD 组件）**:
- 位置：HUD 中角色信息区域（与伤害百分比并列）
- 显示内容：进度条（当前/阈值）+ 百分比或数值
- 颜色：职业色填充，背景暗色
- 阈值变化时：进度条背景标记线移动，显示新阈值位置
- 已达上限（MaxSkillsPerMatch）：进度条变灰或隐藏，显示"已满"标记

**专注值进度条的 HUD 布局**:
- 由战斗 HUD GDD 定义具体布局
- 专注值系统提供数据：FocusPoints, UnlockThreshold, UnlockedCount, MaxSkillsPerMatch

**手柄导航**: 进度条是纯信息显示，无需手柄交互。

> **UX Flag — 专注值系统**: 此系统有 UI 需求。在 Pre-Production 阶段，运行 `/ux-design` 为战斗 HUD 创建 UX 规范，包括专注值进度条的具体布局和交互。引用 `design/ux/hud.md`。

## Acceptance Criteria

### 攻击者专注值获取

- **GIVEN** 角色当前 FocusPoints=10.0 且 FocusGainRate_Attacker=0.30, **WHEN** 攻击命中对手（BaseDamage=12.0）, **THEN** FocusPoints = 10.0 + 12.0 × 0.30 = **13.6**
- **GIVEN** 角色当前 FocusPoints=10.0 且 FocusGainRate_Attacker=0.30, **WHEN** 攻击命中对手（BaseDamage=4.0）, **THEN** FocusPoints = 10.0 + 4.0 × 0.30 = **11.2**
- **GIVEN** 角色当前 FocusPoints=10.0 且 FocusGainRate_Attacker=0.30, **WHEN** 攻击命中对手（BaseDamage=0.0 纯击退攻击）, **THEN** FocusPoints = **10.0**（不变）

### 被击者专注值获取

- **GIVEN** 角色当前 FocusPoints=5.0 且 FocusGainRate_Defender=0.10, **WHEN** 被 BaseDamage=12.0 的攻击命中, **THEN** FocusPoints = 5.0 + 12.0 × 0.10 = **6.2**
- **GIVEN** 角色当前 FocusPoints=5.0 且 FocusGainRate_Defender=0.10, **WHEN** 被 BaseDamage=4.0 的攻击命中, **THEN** FocusPoints = 5.0 + 4.0 × 0.10 = **5.4**

### 递增阈值

- **GIVEN** FocusBaseThreshold=40.0 且 FocusThresholdGrowth=5.0, **WHEN** n=0, **THEN** UnlockThreshold = **40.0**
- **GIVEN** FocusBaseThreshold=40.0 且 FocusThresholdGrowth=5.0, **WHEN** n=1, **THEN** UnlockThreshold = **45.0**
- **GIVEN** FocusBaseThreshold=40.0 且 FocusThresholdGrowth=5.0, **WHEN** n=2, **THEN** UnlockThreshold = **50.0**
- **GIVEN** FocusBaseThreshold=40.0 且 FocusThresholdGrowth=5.0, **WHEN** n=3, **THEN** UnlockThreshold = **55.0**

### 解锁触发

- **GIVEN** FocusPoints=38.0, UnlockThreshold=40.0, UnlockedCount=0, MaxSkillsPerMatch=4, **WHEN** 攻击者获得 FocusGain=3.6（BaseDamage=12.0 × 0.30）, **THEN** FocusPoints_new=41.6, 触发 OnFocusReady, FocusPoints_final=1.6, UnlockedCount=1
- **GIVEN** FocusPoints=38.0, UnlockThreshold=40.0, UnlockedCount=0, FocusCap=55.0, **WHEN** 攻击者获得 FocusGain=20.0（极端情况）, **THEN** FocusPoints_new=Min(58.0, 55.0)=55.0, 触发 OnFocusReady, FocusPoints_final=55.0-40.0=15.0, UnlockedCount=1

### FocusCap 钳制

- **GIVEN** FocusPoints=53.0, FocusCap=55.0, UnlockedCount=4（已达上限）, **WHEN** 获得任何 FocusGain, **THEN** FocusPoints 钳制到 55.0，不触发解锁

### 解锁上限

- **GIVEN** UnlockedCount=3, UnlockThreshold_3=55.0, **WHEN** FocusPoints 达到 55.0, **THEN** 触发解锁，UnlockedCount=4
- **GIVEN** UnlockedCount=4（已达 MaxSkillsPerMatch）, **WHEN** FocusPoints 继续积累, **THEN** 不触发解锁，FocusPoints 钳制到 FocusCap

### 重置

- **GIVEN** FocusPoints=30.0, UnlockedCount=2, **WHEN** 对局管理系统触发 OnRoundStart, **THEN** FocusPoints=0.0, UnlockedCount=0

### 双方独立积累

- **GIVEN** P1 命中 P2（BaseDamage=8.0）, FocusGainRate_Attacker=0.30, FocusGainRate_Defender=0.10, **WHEN** 命中处理完成, **THEN** P1 FocusPoints += 2.4, P2 FocusPoints += 0.8

### 同帧互命中

- **GIVEN** P1 和 P2 同一帧互相命中（P1 BaseDamage=12.0, P2 BaseDamage=4.0）, **WHEN** 两个 OnAttackHit 事件处理完成, **THEN** P1 FocusPoints += (12.0×0.30 + 4.0×0.10) = 4.0, P2 FocusPoints += (4.0×0.30 + 12.0×0.10) = 2.4

### 数据错误

- **GIVEN** OnAttackHit 事件中 AttackData=null, **WHEN** 专注值系统处理该事件, **THEN** 忽略，不更新专注值，记录警告
- **GIVEN** UnlockThreshold_n 因参数错误计算为负数, **WHEN** 判定解锁, **THEN** 强制阈值最小值 1.0

### 性能

- **GIVEN** 2 人对战进行中, **THEN** 专注值系统每次命中处理耗时 < 0.1ms

> `qa-lead` not consulted — Lean mode. Review manually before production.

## Open Questions

1. **解锁瞬间是否应该短暂暂停游戏？** 当前设计有 2-3 帧画面定格，但这可能与 hitstop 冲突（如果同时发生命中和解锁）。需要原型验证解锁瞬间的视觉/节奏体验。（Owner: 设计师，里程碑: 原型验证）
2. **被击者补偿比率是否需要动态调整？** 固定 1:3 比率可能导致极端策略（故意挨打刷专注值）。如果成为问题，可引入递减补偿（连续被击时补偿递减）。（Owner: 系统设计师，里程碑: 平衡调试期）
3. **解锁阈值是否应考虑角色差异？** 当前设计所有职业使用相同阈值。Rogue（轻击多、频率高）可能比 Warrior（重击多、频率低）更快解锁。如果需要平衡，可在 AttackData 中添加独立的 FocusReward 值替代 BaseDamage 计算。（Owner: 平衡设计师，里程碑: 技能系统设计后）
