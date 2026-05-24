# 技能抽取系统 (Skill Draw System)

> **Status**: Designed
> **Author**: SeanDuan + agents
> **Last Updated**: 2026-05-23
> **Implements Pillar**: Pillar 2: 每局都是新故事, Pillar 1: 秒学秒玩

## Overview

技能抽取系统是职业对决肉鸽循环的随机引擎，负责将专注值系统的解锁事件（`OnFocusReady`）转化为一个具体的技能选择结果——系统在收到解锁信号后，根据当前角色的职业标签和已有技能列表从技能数据库中构建可抽取牌池，按稀有度权重执行加权随机选择，将抽中的技能数据传递给下游的技能装备管理系统进行实例化。作为纯逻辑层，技能抽取系统不直接控制战斗、视觉或 UI——它只负责"从哪些技能中随机选一个"的决策过程。抽取算法采用两层加权：第一层按稀有度（Common 70% / Rare 20% / Epic 10%）选择稀有度层级，第二层在选中层级的技能中按各技能的 RarityWeight 均匀分配概率。每局每人最多抽取 MaxSkillsPerMatch（4）次，已抽取的技能从牌池中移除以确保不重复。MVP 中每职业可用池为 6 个技能（3 通用 + 2 职业专属 + 1 通用 Epic），C(6,4)=15 种组合，3 职业 = 45 种跨职业体验。技能抽取系统的存在理由是：没有它，专注值解锁只是"攒够了值"但没有"然后呢"的惊喜——抽取是从积累到收获的转化器，是 Pillar 2（每局都是新故事）的核心机制。对玩家而言，每次抽取都是一次"开箱"时刻——不确定性带来的兴奋，与"不管抽到什么我都能用"的信心交织，创造格斗游戏中最独特的成长节奏。

## Player Fantasy

**核心幻想：「每次解锁都是一次惊喜开箱，每次开箱都重塑战斗方式」**

玩家应该感觉技能抽取系统是每局对战的"命运之轮"——专注值攒满的那一刻，进度条爆裂，系统从牌池中随机抽出一张"卡牌"，技能图标弹入屏幕，稀有度颜色闪烁（蓝/紫/金）。在那一瞬间，玩家同时体验两种情绪：**"我抽到了什么？"**的好奇，和**"不管是什么我都能用"**的自信。

抽取系统创造的幻想不是"获得更强的能力"，而是**"获得不同的能力"**。一个 Common 弹反斩和一个 Epic 陨石坠落没有"谁更强"的绝对答案——它们改变的是你接下来 30 秒的战斗方式。战士抽到冰箭？你变成了一个中距离骚扰者。盗贼抽到盾击？你变成了一个压制型近战。这种即兴身份转换是核心幻想——你不是在选择角色，你在适应命运给你的工具。

**关键情感时刻**：
- **抽取前的期待** — 专注值 90%，进度条脉动加速，"下一次命中就解锁了"
- **抽取瞬间的爆发** — 画面定格、特效炸裂、技能图标弹入，2 秒内完成从"未知"到"已知"的转化
- **看到结果后的策略转向** — "我抽到了火球术……现在该远程骚扰了"，从策略到执行的即时转换
- **连续抽取的混乱升级** — 第 2、3 个技能解锁后，角色能力越来越复杂，战斗越来越疯狂，"我已经不是开局那个我了"
- **对局间的差异感** — "上一局我是近战战士，这局我变成了远程法师"——每局的命运不同

**支柱对齐**：
- 服务 **Pillar 2: 每局都是新故事** — 抽取是多样性的直接引擎，每次抽取都在创造独特叙事
- 服务 **Pillar 1: 秒学秒玩** — 抽取过程本身不超过 2 秒，不需要玩家做复杂选择
- 服务 **Pillar 3: 高手菜鸟都开心** — 菜鸟享受随机惊喜，高手享受即兴适应挑战

> `creative-director` not consulted — Lean mode. Review manually before production.

## Detailed Design

### Core Rules

**1. 抽取触发机制**

当专注值系统发出 `OnFocusReady(CharacterId, UnlockedCount)` 事件时，技能抽取系统在同一帧启动一次抽取流程。触发条件：
- `UnlockedCount < MaxSkillsPerMatch`（由专注值系统保证）
- 当前角色本局尚未达到抽取上限

**2. 牌池构建（Eligible Pool）**

每次抽取前，系统动态构建可抽取牌池：

1. 从技能数据库查询所有技能：`ISkillDatabase.GetAllSkills()`
2. 过滤条件一：职业匹配——技能的 `Tags` 为空（通用技能）或包含当前角色职业名（职业专属技能）
3. 过滤条件二：去重——排除本局已抽取的技能（通过 `AlreadyDrawnSkillIds` 集合跟踪）
4. 过滤后的技能集合即为本次抽取的 Eligible Pool

**3. 三选一候选生成**

从 Eligible Pool 中无放回抽取 3 个技能作为候选列表（Candidate List）：

1. 使用加权随机（基于每个技能的 `SkillDrawWeight`）从 Eligible Pool 中无放回抽取 3 个技能
2. 如果 Eligible Pool 中的技能数 < 3：池中所有技能成为候选（不足 3 个按实际数量展示）
3. 如果 Eligible Pool 为空：本次抽取失败，不消耗解锁次数（由 Edge Cases 节详细定义）

**4. 玩家选择流程**

候选列表生成后：

1. 系统发出 `OnDrawReady(CharacterId, CandidateList)` 事件，通知 UI 层展示选择界面
2. 选择界面以悬浮 UI 形式叠加在游戏画面上，**游戏不暂停**
3. 玩家通过手柄/键盘选择一个技能（方向键切换候选，确认键确认）
4. 超时：5 秒内未选择 → 自动选择候选列表中的第一个技能
5. 选择后（玩家确认或超时）：发出 `OnSkillSelected(CharacterId, SelectedSkillData)` 事件

**5. 抽取结果处理**

选择完成后：

1. 被选中的技能加入 `AlreadyDrawnSkillIds` 集合（防止重复抽取）
2. 未被选中的 2 个技能**返回牌池**（不会被消耗）
3. 系统发出 `OnSkillDrawn(CharacterId, SkillData)` 事件通知下游系统：
   - 技能装备管理系统：接收 SkillData 进行装备和状态注册
   - 战斗 HUD：接收技能图标、名称、稀有度信息进行显示
   - 能量视觉系统：接收稀有度信息触发解锁特效

**6. 抽取频率限制**

- 每局每人最多抽取 `MaxSkillsPerMatch`（4）次
- 抽取计数由专注值系统的 `UnlockedCount` 跟踪（两个系统共享同一计数器）
- 达到上限后，专注值系统不再发出 `OnFocusReady`，抽取系统不再被触发

### States and Transitions

技能抽取系统维护一个简单的抽取状态机：

| 状态 | 触发条件 | 行为 | 超时 |
|------|---------|------|------|
| **Idle** | 默认/抽取完成 | 等待 `OnFocusReady` 事件 | — |
| **Drawing** | 收到 `OnFocusReady` | 构建牌池，生成 3 个候选，发出 `OnDrawReady` | — |
| **AwaitingSelection** | 候选已展示 | 等待玩家选择或超时 | 5 秒 |
| **Complete** | 玩家选择/超时 | 处理结果，发出 `OnSkillDrawn`，转回 Idle | — |

**状态转换图**:

```
Idle → (OnFocusReady) → Drawing → (OnDrawReady) → AwaitingSelection → (选择/超时) → Complete → Idle
```

**异常转换**:
- `Drawing` → `Idle`：如果 Eligible Pool 为空（无技能可抽），直接回 Idle，不消耗解锁次数
- `AwaitingSelection` → `Complete`：超时自动选择第一个候选

### Interactions with Other Systems

| 系统 | 数据流方向 | 接口描述 |
|------|-----------|---------|
| 专注值系统 | 上游（硬依赖） | `OnFocusReady(CharacterId, UnlockedCount)` — 触发抽取 |
| 技能数据库 | 上游（硬依赖） | `GetAllSkills()` — 提供完整技能列表用于牌池构建 |
| 技能装备管理 | 下游（硬依赖） | `OnSkillDrawn(CharacterId, SkillData)` — 传递抽中的技能数据，由装备管理负责实例化和状态注册 |
| 战斗HUD | 下游（硬依赖） | `OnDrawReady(CharacterId, CandidateList)` — 展示三选一 UI；`OnSkillDrawn` — 显示已装备技能信息 |
| 能量视觉系统 | 下游（软依赖） | `OnSkillDrawn` — 触发解锁特效（稀有度颜色闪光） |
| 职业系统 | 间接 | 通过 CharacterId 查询角色职业，用于技能 Tags 过滤 |
| 对局管理系统 | 上游（软依赖） | `OnRoundStart` — 重置 `AlreadyDrawnSkillIds` 和抽取状态 |
| 游戏状态管理 | 间接 | 对局开始/结束状态影响抽取系统的激活/停用 |

## Formulas

**单位系统**: 无单位概率值（0.0–1.0），时间以 60Hz 帧为基准。本系统的公式均为概率计算，不涉及物理单位。

### 1. 职业池权重重算

在职业过滤和去重后，重新计算每个技能在当前牌池内的抽取权重：

`PoolWeight_i = (RarityPoolWeight_i / RarityPoolCount_inPool) × SkillRarityWeight_i`

然后归一化：`DrawWeight_i = PoolWeight_i / Sum(PoolWeight_j for all j in EligiblePool)`

**Variables:**

| 变量 | 符号 | 类型 | 范围 | 说明 |
|------|------|------|------|------|
| 稀有度池权重 | RarityPoolWeight | float | 0.1–0.7 | 稀有度层级概率（Common=0.7, Rare=0.2, Epic=0.1） |
| 池内该稀有度技能数 | RarityPoolCount_inPool | int | 0–5 | 牌池中属于该稀有度的技能数量 |
| 技能稀有度权重 | SkillRarityWeight | float | 0.5–2.0 | 技能在其稀有度内的相对权重（MVP 全部=1.0） |
| 池内原始权重 | PoolWeight_i | float | 0.0–0.7 | 过滤后重算的原始权重 |
| 归一化抽取权重 | DrawWeight_i | float | 0.0–1.0 | 最终抽取概率（所有技能之和=1.0） |

**Output Range:** 0.0–1.0，所有 DrawWeight 之和 = 1.0
**Example (Warrior 首次抽取):** 4 Common(0.7/4=0.175), 1 Rare(0.2/1=0.20), 1 Epic(0.1/1=0.10)。Sum=1.0，无需归一化。
**Example (Mage 首次抽取):** 5 Common(0.7/5=0.14), 0 Rare(跳过), 1 Epic(0.1/1=0.10)。Sum=0.80，归一化后：每个 Common=0.14/0.80=0.175，Epic=0.10/0.80=0.125。

### 2. 三选一候选生成（加权随机无放回）

```
对于 k = 1 到 min(3, PoolSize):
    P(skill_i) = DrawWeight_i / Sum(DrawWeight_j, j ∈ 剩余池)
    抽取 Candidate_k
    从临时池中移除 Candidate_k，重新归一化剩余权重
```

**Variables:**

| 变量 | 符号 | 类型 | 范围 | 说明 |
|------|------|------|------|------|
| 合格牌池 | EligiblePool | set | 1–6 | 通过职业+去重过滤的技能集合 |
| 牌池大小 | PoolSize | int | 0–6 | EligiblePool 的技能数量 |
| 抽取迭代 | k | int | 1–3 | 当前正在抽取第几个候选 |
| 剩余池 | Pool_k | set | 0–6 | 第 k 次抽取时可用的技能集合 |
| 候选列表 | CandidateList | list | 1–3 | 最终生成的候选技能列表 |

**Output Range:** 1–3 个唯一 SkillData。PoolSize=0 时返回空列表。
**Example (Warrior 首次):** Pool_0 含 6 技能。Draw 1: ShieldBash(0.175)。Draw 2: 剩余 5 技能重归一化，PowerStrike(0.200/0.825=0.242)。Draw 3: 剩余 4 技能重归一化。结果: [ShieldBash, DashStrike, CounterStrike]。

### 3. 牌池耗尽检查

`CanDraw = (PoolSize >= 1)`

`CandidateCount = min(3, PoolSize)`

**降级模式:**

| PoolSize | CandidateCount | 体验 |
|----------|---------------|------|
| ≥3 | 3 | 完整三选一 |
| 2 | 2 | 二选一 |
| 1 | 1 | 自动选择（无选择） |
| 0 | 0 | 抽取失败，跳过 |

**Example:** Warrior 已抽 3 个技能，RemainingPoolSize = 6-3 = 3 ≥ 3，完整三选一。MVP 中永远不会触发降级模式。

### 4. 抽取序列追踪

`RemainingPoolSize_n = InitialPoolSize - n`

**Variables:**

| 变量 | 符号 | 类型 | 范围 | 说明 |
|------|------|------|------|------|
| 初始牌池大小 | InitialPoolSize | int | 4–6 | 职业匹配的初始技能数量 |
| 已抽取数 | n | int | 0–4 | 本局已选中的技能数量（= UnlockedCount） |
| 剩余牌池大小 | RemainingPoolSize_n | int | 0–6 | 仍可抽取的技能数量 |

**Output Range:** 0 到 InitialPoolSize。MVP 中 InitialPoolSize=6，4 次抽取后剩余 2 个技能。
**Example (Warrior):** n=0→6, n=1→5, n=2→4, n=3→3, n=4→2。

## Edge Cases

**牌池构建**:
- **如果某稀有度在职业池中无任何技能（如 Mage 无 Rare）**: 该稀有度的 RarityPoolWeight 自动归入归一化分母但不产生任何候选。归一化后其他稀有度的权重按比例提升。Mage 的 0.20 Rare 权重被重新分配给 Common 和 Epic。
- **如果技能的 Tags 包含多个职业名（数据错误）**: 只要有任何一个 Tag 匹配当前职业即视为合格。不检查多 Tag 的一致性。
- **如果 AlreadyDrawnSkillIds 中包含不在职业池中的 SkillId**: 无影响——该 ID 不在 EligiblePool 中，去重过滤自动跳过。可能是调试残留数据。

**候选生成**:
- **如果 EligiblePool 只有 1 个技能**: CandidateCount=1，跳过加权随机，直接返回该技能。不进入 AwaitingSelection 状态——直接完成抽取（无选择界面展示）。
- **如果 EligiblePool 只有 2 个技能**: CandidateCount=2，展示二选一 UI。超时逻辑不变（5 秒）。
- **如果加权随机的累积权重因浮点精度不为 1.0**: 如果随机值 R 超过所有累积权重，回退到最后一个技能。避免除零或越界。
- **如果三个候选都是同一稀有度**: 合法结果。MVP 中 Common 技能占比最高（Warrior 4/6），三个 Common 候选是正常分布。
- **如果两个候选的 DrawWeight 完全相同**: 随机打破平局。权重相同时先在池中排列靠前的技能概率均等。

**选择流程**:
- **如果超时发生时选择 UI 尚未完全渲染**: 仍然执行自动选择。超时从 `OnDrawReady` 事件发出时开始计时，不依赖 UI 渲染完成。
- **如果玩家在选择期间被 KO（角色死亡）**: 如果对局管理系统允许重生，选择流程继续（超时倒计时不停）。技能在 KO 状态下被装备，重生后可用。具体取决于对局管理系统 GDD 的重生规则。
- **如果玩家在选择期间再次触发 OnFocusReady（极端情况）**: 不可能发生——专注值系统在解锁触发后扣减专注值，同一帧不会连续触发两次。但如果因帧延迟导致第二次触发到达，队列化处理：第一次选择完成后才处理第二次。
- **如果两个玩家同一帧都触发抽取**: 各自独立处理。两个独立的 AwaitingSelection 实例并行运行，各自的超时独立计时。
- **如果手柄输入在选择期间与战斗输入冲突**: 选择 UI 激活时，手柄的方向键和确认键被选择 UI 拦截，不传递给战斗系统。但角色仍可通过摇杆移动（不暂停模式下的移动不通过 UI 输入）。

**抽取结果**:
- **如果下游技能装备管理系统在 OnSkillDrawn 时报错**: 抽取结果已确定——技能已加入 AlreadyDrawnSkillIds。装备失败不回滚抽取（已消耗的解锁机会不退回）。装备系统应在下一帧重试。
- **如果 OnSkillDrawn 事件没有订阅者**: 抽取仍然完成。事件机制是发布-订阅模式，无订阅者不阻塞。

**对局流程**:
- **如果新一局开始时 AlreadyDrawnSkillIds 未重置**: 对局管理系统的 OnRoundStart 必须触发重置。如果未触发，旧对局的已抽取列表会污染新对局的牌池——所有已抽取技能在新局中不可用。这是 bug 不是 feature。
- **如果对局在 AwaitingSelection 状态下结束**: 技能已从牌池扣除但未完成选择。新局重置时状态清理，该抽取"丢失"——与专注值系统 GDD 一致（对局结束时未完成的解锁可接受地丢失）。

**数据完整性**:
- **如果技能数据库返回 null 技能**: 在牌池构建时过滤掉 null 条目。记录警告。
- **如果 SkillDrawWeight 因数据错误为负数**: 钳制到 0.0，从牌池中排除（等效于零权重技能）。
- **如果 UnlockedCount 与 AlreadyDrawnSkillIds 数量不一致**: 以 AlreadyDrawnSkillIds 为准（它由抽取系统自身维护）。不一致说明专注值系统的计数有误，应在调试时发现。

> `systems-designer` not consulted — Lean mode. Review manually before production.

## Dependencies

| 依赖系统 | 方向 | 类型 | 数据接口 | GDD 状态 |
|---------|------|------|---------|---------|
| 专注值系统 | 上游（硬依赖） | 事件 | `OnFocusReady(CharacterId, UnlockedCount)` — 触发抽取流程 | Designed |
| 技能数据库 | 上游（硬依赖） | 数据查询 | `GetAllSkills()` — 提供完整技能列表用于牌池构建和权重计算 | Designed |
| 职业系统 | 上游（间接） | 数据查询 | 通过 CharacterId 查询角色职业名，用于技能 Tags 过滤 | Designed |
| 对局管理系统 | 上游（软依赖） | 事件 | `OnRoundStart` — 重置 AlreadyDrawnSkillIds 和抽取状态 | Not Started |
| 技能装备管理 | 下游（硬依赖） | 事件 | `OnSkillDrawn(CharacterId, SkillData)` — 传递抽中的技能数据，装备管理负责实例化 | Not Started |
| 战斗HUD | 下游（硬依赖） | 事件 | `OnDrawReady(CharacterId, CandidateList)` — 展示三选一 UI；`OnSkillDrawn` — 更新技能图标 | Not Started |
| 能量视觉系统 | 下游（软依赖） | 事件 | `OnSkillDrawn` — 触发解锁特效（按稀有度颜色） | Not Started |

**向上提供的接口契约**:
- `ISkillDrawSystem` 接口: 抽取系统管理和查询入口
- `OnDrawReady(CharacterId, CandidateList)`: 候选列表已生成，请求玩家选择
- `OnSkillSelected(CharacterId, SelectedSkillData)`: 玩家已做出选择（内部事件）
- `OnSkillDrawn(CharacterId, SkillData)`: 抽取完成，技能已确定（通知下游系统）
- `GetAlreadyDrawnSkillIds(CharacterId)`: 查询角色本局已抽取的技能 ID 集合
- `GetDrawState(CharacterId)`: 查询角色当前抽取状态（Idle/Drawing/AwaitingSelection）
- `ResetDrawState(CharacterId)`: 重置角色抽取状态和已抽取列表
- `ResetAll()`: 重置所有角色抽取状态

**双向一致性检查**:
- 专注值系统 GDD: "技能抽取系统 | 专注值 → 技能抽取 | `OnFocusReady` 触发随机抽取" ✓ 一致
- 技能数据库 GDD: "技能抽取系统 | 技能DB → 抽取 | 提供 `GetAllSkills()` 或 `GetSkillsByRarity()` 构建抽取牌池" ✓ 一致（本系统使用 `GetAllSkills()` + 自行过滤）
- 专注值系统 GDD: "OnFocusReady(CharacterId, UnlockedCount)" ✓ 接口签名一致

## Tuning Knobs

| 旋钮名 | 默认值 | 安全范围 | 调高效果 | 调低效果 |
|--------|--------|---------|---------|---------|
| CandidateCount | 3 | 2–4 | 更多选择，策略深度增加，选择时间更长 | 更少选择，更接近纯随机，节奏更快 |
| SelectionTimeout | 5.0 | 2.0–10.0 | 玩家有更多时间阅读技能信息 | 更快节奏，促决策，减少战斗中断 |

**引用的上游旋钮（不重复定义）**:
- `CommonPoolWeight`, `RarePoolWeight`, `EpicPoolWeight` — 稀有度池权重，由技能数据库 GDD 定义
- `MaxSkillsPerMatch` — 每局最大抽取次数，由专注值系统 GDD 定义
- `FocusBaseThreshold`, `FocusThresholdGrowth` — 抽取触发频率，由专注值系统 GDD 定义

**旋钮交互警告**:
- `CandidateCount` 和牌池大小共同决定选择的自由度——CandidateCount > PoolSize 时降级，不需要额外处理
- `SelectionTimeout` 直接影响战斗节奏——超过 5 秒在快节奏格斗中会明显打断流畅度
- 改变 `CandidateCount` 需要同步更新 UI 布局（战斗 HUD GDD 需要支持动态候选数量）

## Visual/Audio Requirements

**解锁瞬间的抽取特效**:

当 OnFocusReady 触发、系统进入 Drawing 状态时：
- 短暂画面定格（2-3 帧），全局白色闪光（与专注值系统的解锁特效协调）
- 角色轮廓爆发职业色光芒

当候选列表生成、系统发出 OnDrawReady 时：
- 3 张技能卡牌从屏幕边缘弹入（滑入动画 < 0.3 秒）
- 每张卡牌显示：技能图标、技能名称、稀有度边框（蓝/紫/金）
- 卡牌悬停在高亮状态时，轻微放大 + 光晕效果

当玩家选择完成（OnSkillDrawn）时：
- 被选中的卡牌放大闪光后飞向 HUD 技能槽位（< 0.5 秒飞行动画）
- 未选中的 2 张卡牌淡出（< 0.2 秒）
- 选中技能的稀有度对应颜色闪过全屏（Common: 蓝色微光, Rare: 紫色闪光, Epic: 金色爆发 + 短暂全屏金色边框）

**超时视觉提示**:
- 超时倒计时以圆形进度条显示在选择 UI 中心或底部
- 最后 1 秒倒计时加速闪烁，传达紧迫感

**音频事件**:

| 音频事件 | 触发时机 | 描述 |
|---------|---------|------|
| `OnDrawReady` | 候选列表展示 | "开箱"音效——能量汇聚 + 卡牌弹出声 |
| `OnDrawHover` | 玩家切换候选高亮 | 轻微"切换"音效 |
| `OnDrawSelect` | 玩家确认选择 | "确认"音效——清脆的"叮" + 稀有度对应音调 |
| `OnDrawTimeout` | 超时自动选择 | 更低沉的"自动选择"音效，区别于主动选择 |

## UI Requirements

**技能选择悬浮 UI（战斗 HUD 组件）**:
- 形态：悬浮在游戏画面上的 3 张技能卡牌，不暂停游戏
- 位置：屏幕下半部分中央，不遮挡角色头部区域
- 布局：横向排列 3 张卡牌，间距均匀
- 每张卡牌内容：技能图标（SkillData.Icon）、技能名称（SkillData.DisplayName）、稀有度边框颜色
- 当前高亮卡牌：放大 + 光晕 + 边框加粗
- 超时指示：圆形倒计时进度条
- 输入：方向键左右切换（循环 wrap），确认键选择

**手柄导航**:
- 左右方向键/D-pad 切换候选（循环）
- A/确认键选择当前高亮候选
- 选择 UI 激活时，方向键输入被 UI 拦截，不传递给战斗系统
- 摇杆移动不拦截——角色仍可在选择期间移动

**降级模式 UI**:
- 2 张卡牌：二选一布局
- 1 张卡牌：自动选择，不展示选择 UI

> **UX Flag — 技能抽取系统**: 此系统有 UI 需求（三选一悬浮选择界面）。在 Pre-Production 阶段，运行 `/ux-design` 为技能选择 UI 创建 UX 规范。引用 `design/ux/skill-selection.md`。

## Acceptance Criteria

### 牌池构建

- **GIVEN** Warrior 角色且 AlreadyDrawnSkillIds={}, **WHEN** 构建合格牌池, **THEN** 恰好返回 6 个技能：skill_counter-strike, skill_spinning-kick, skill_dash-strike, skill_shield-bash, skill_power-strike, skill_meteor
- **GIVEN** Mage 角色且 AlreadyDrawnSkillIds={}, **WHEN** 构建合格牌池, **THEN** 恰好返回 6 个技能（5 Common + 1 Epic），Rare 技能数为 0
- **GIVEN** Warrior 角色且 AlreadyDrawnSkillIds={skill_counter-strike, skill_dash-strike}, **WHEN** 构建合格牌池, **THEN** 恰好返回 4 个技能（已抽取的 2 个被排除）
- **GIVEN** 技能数据库返回含 null 条目的列表, **WHEN** 构建合格牌池, **THEN** null 被过滤，记录警告，只保留有效 SkillData

### 候选生成

- **GIVEN** Warrior 6 技能牌池（4C+1R+1E）, **WHEN** 生成候选, **THEN** 恰好返回 3 个唯一技能，均在牌池中
- **GIVEN** Warrior 6 技能牌池, **WHEN** 计算权重, **THEN** 每个 Common=0.175, Rare=0.200, Epic=0.100，总和=1.0
- **GIVEN** Mage 6 技能牌池（5C+0R+1E）, **WHEN** 计算权重, **THEN** 每个 Common=0.175（0.14/0.80）, Epic=0.125（0.10/0.80），总和=1.0
- **GIVEN** 牌池恰好 2 个技能, **WHEN** 生成候选, **THEN** 返回 2 个候选（二选一模式）
- **GIVEN** 牌池恰好 1 个技能, **WHEN** 生成候选, **THEN** 返回 1 个候选，跳过 AwaitingSelection 状态，直接完成
- **GIVEN** 牌池为空（0 技能）, **WHEN** 尝试生成候选, **THEN** 返回空列表，状态回 Idle，不消耗解锁次数，不发出 OnSkillDrawn

### 玩家选择

- **GIVEN** AwaitingSelection 状态且有 3 个候选, **WHEN** 玩家按确认键选择候选[1], **THEN** 选中候选[1]，状态转 Complete，发出 OnSkillDrawn
- **GIVEN** AwaitingSelection 状态且有 3 个候选, **WHEN** 5.0 秒内无输入, **THEN** 自动选择候选[0]，状态转 Complete
- **GIVEN** AwaitingSelection 状态, **WHEN** 高亮在候选[2]且玩家按右键, **THEN** 高亮循环回候选[0]（wrap 模式）
- **GIVEN** AwaitingSelection 状态, **WHEN** 查看游戏画面, **THEN** 游戏不暂停（对手可移动、攻击可执行、物理在模拟）
- **GIVEN** 玩家在 KO 状态下处于 AwaitingSelection, **WHEN** 玩家按方向键/确认键, **THEN** 输入正常响应，可以完成选择

### 抽取结果

- **GIVEN** AlreadyDrawnSkillIds={} 且玩家选择了 skill_fireball, **WHEN** 抽取完成, **THEN** AlreadyDrawnSkillIds={skill_fireball}
- **GIVEN** 候选=[A, B, C] 且玩家选择 B, **WHEN** 下一次牌池构建, **THEN** A 和 C 仍在牌池中（未被消耗）
- **GIVEN** 玩家(P1)选择 skill_meteor(Epic), **WHEN** 抽取完成, **THEN** OnSkillDrawn("P1", SkillData) 发出，其中 SkillData.SkillId="skill_meteor", Rarity=Epic
- **GIVEN** 无下游系统订阅 OnSkillDrawn, **WHEN** 抽取完成, **THEN** 事件发布不阻塞，技能仍加入 AlreadyDrawnSkillIds

### 抽取频率

- **GIVEN** UnlockedCount=3 且 AlreadyDrawnSkillIds 有 3 个 ID, **WHEN** OnFocusReady 触发, **THEN** 正常执行第 4 次抽取
- **GIVEN** UnlockedCount=4（已达上限）, **WHEN** OnFocusReady 误触发, **THEN** 抽取系统忽略该事件，保持在 Idle 状态
- **GIVEN** Warrior InitialPoolSize=6, **WHEN** n=0→4, **THEN** RemainingPoolSize 依次为 6→5→4→3→2

### 重置

- **GIVEN** P1 已抽取 3 个技能且状态=Idle, **WHEN** 收到 OnRoundStart, **THEN** AlreadyDrawnSkillIds 清空，状态回 Idle
- **GIVEN** P1 处于 AwaitingSelection 状态, **WHEN** 收到 OnRoundStart（对局结束）, **THEN** 抽取被丢弃，候选不计入 AlreadyDrawnSkillIds，状态回 Idle

### 双人同时抽取

- **GIVEN** P1(Warrior)和 P2(Rogue)同一帧触发 OnFocusReady, **WHEN** 两个抽取处理完成, **THEN** 各自从各自职业池抽取，独立候选，独立超时
- **GIVEN** P1 和 P2 都抽到 skill_counter-strike（通用技能）, **WHEN** 两个抽取完成, **THEN** P1 的 AlreadyDrawnSkillIds 包含它，P2 的也包含它（按角色独立）

### 排队与边界

- **GIVEN** P1 处于 AwaitingSelection 状态（第 1 次抽取进行中）, **WHEN** 第 2 个 OnFocusReady 到达, **THEN** 队列化处理，等第 1 次完成后再执行第 2 次。超过 1 个的额外事件被忽略
- **GIVEN** 技能的 SkillDrawWeight 为负数（数据错误）, **WHEN** 计算权重, **THEN** 钳制到 0.0，该技能被排除在抽取概率之外
- **GIVEN** 随机值 R 超过累积权重之和（浮点精度）, **WHEN** 执行加权随机, **THEN** 选择最后一个技能作为回退，无越界错误

### 性能

- **GIVEN** 2 人对战、50 技能数据库（未来扩展）, **WHEN** 执行牌池构建+权重计算+候选生成, **THEN** 总处理时间 < 0.5ms

> `qa-lead` consulted for acceptance criteria validation.

## Open Questions

1. **选择 UI 的具体视觉设计** — 当前定义了功能需求和布局原则，但卡牌的具体视觉风格（像素风/简约几何/卡牌游戏风）需要与 art bible 协调。（Owner: 艺术总监，里程碑: Art Bible 审批后）

2. **选择期间角色移动的精确行为** — 当前设计摇杆移动不被拦截，但角色是否能执行攻击、跳跃等操作？如果可以，玩家可能在选择期间同时战斗，造成输入冲突。（Owner: 系统设计师，里程碑: 技能装备管理 GDD）

3. **不暂停模式对竞技公平性的影响** — 选择期间游戏不暂停意味着对手可以趁机进攻。这是否会让"选技能"变成一种惩罚而非奖励？需要原型验证。（Owner: 游戏设计师，里程碑: 原型验证）

4. **扩展到 40-50 技能时的选择体验** — 更大的牌池意味着更复杂的权重分布和更多可能的候选组合。MVP 的 6 技能/职业池是否足够验证三选一机制的趣味性？（Owner: 游戏设计师，里程碑: Vertical Slice 阶段）
