# 技能数据库 (Skill Database)

> **Status**: Designed
> **Author**: SeanDuan + agents
> **Last Updated**: 2026-05-23
> **Implements Pillar**: Pillar 1: 秒学秒玩, Pillar 2: 每局都是新故事

## Overview

技能数据库是职业对决肉鸽循环的数据基石，负责存储所有可解锁随机技能的完整定义（名称、描述、帧数据、hitbox 参数、投射物参数、稀有度、标签等），以 Unity ScriptableObject 为载体提供设计师可编辑的数据驱动配置。作为纯数据层，它不执行任何运行时逻辑——技能抽取系统从数据库中查询和筛选可抽取的技能池，技能装备管理系统从数据库读取技能定义来实例化技能并注入格斗状态机和攻击系统，攻击系统消费技能的 AttackData 以统一方式处理所有攻击类型。技能数据库定义了技能的三维分类体系：**类型**（近战/投射物）、**稀有度**（普通/稀有/史诗，对应蓝/紫/金色视觉标记）、**标签**（用于职业专属池过滤）。MVP 包含 10 个技能（7 Common + 2 Rare + 1 Epic），完整版扩展至 40-50 个。所有技能数据在设计阶段定义，运行时只读不可修改。技能数据库的存在理由是：没有它，技能抽取系统没有"牌池"可抽取，技能装备管理没有模板可实例化，随机技能系统无从谈起。它是 Pillar 2（每局都是新故事）的数据基础——技能池的多样性和差异性直接决定每局体验的独特程度。

## Player Fantasy

技能数据库是纯基础设施——玩家永远不会直接感知到"数据库"的存在。但它的设计决策深刻影响玩家的间接体验：技能池的多样性决定了"每局都不同"的真实感，技能之间的差异化程度决定了"发现新组合"的惊喜感，稀有度的分布决定了"抽到金技能"的兴奋感。

数据库层面创造的不是玩家幻想本身，而是**支撑幻想的数据条件**：没有足够多样的技能（MVP 10-15 个），玩家会在 3-4 局后开始看到重复组合，Pillar 2（每局都是新故事）失效；没有清晰的稀有度分层，每次解锁的感觉是"又一个普通技能"而不是"我抽到了什么"的期待；没有标签体系，职业差异化在随机技能层面消失，所有职业的游戏体验趋同。

**关键设计目标**（间接支撑玩家幻想）：
- **多样性**: MVP 10 个技能（3 通用 + 2 职业专属/职业 + 1 通用 Epic = 6 可用/职业），每职业 C(6,4)=15 种组合，3 职业 = 45 种跨职业体验
- **差异化**: 每个技能在操作手感或视觉效果上必须与基础招式有明显区别——不是"多一点伤害"的微调
- **稀有度仪式感**: 稀有技能不只是在数值上更强，而是提供独特的机制（如传送、护盾、范围控制）

**支柱对齐**：
- 支撑 **Pillar 2: 每局都是新故事** — 技能池是多样性的源头
- 支撑 **Pillar 1: 秒学秒玩** — 每个技能必须几秒内就能理解（复杂度上限约束数据库设计）

> `creative-director` not consulted — Lean mode. Review manually before production.

## Detailed Design

### Core Rules

**1. 技能数据结构（SkillData）**

每个技能是一个独立的 ScriptableObject 配置资产，包含以下数据：

| 字段 | 类型 | 说明 |
|------|------|------|
| SkillId | string | 唯一标识符，格式 `skill_[name]`（如 `skill_fireball`） |
| DisplayName | string | 技能名称（本地化 key） |
| Description | string | 技能描述（本地化 key） |
| Rarity | enum | 稀有度：`Common` / `Rare` / `Epic` |
| RarityWeight | float | 抽取权重（同稀有度内的相对权重） |
| Tags | string[] | 标签数组（用于职业专属池过滤：空=通用，或指定职业名） |
| AttackData | AttackData | 完整的攻击数据（与职业基础招式格式一致） |
| Icon | Sprite | 技能图标（HUD 显示用） |
| VFXColor | Color | 技能特效主色调（按稀有度：蓝/紫/金） |

**AttackData 复用规则**：技能的 `AttackData` 字段结构与职业基础招式完全一致（StartupFrames, ActiveFrames, RecoveryFrames, HitStunFrames, BaseDamage, BaseKnockback, IsProjectile, HitstopFrames, HitboxOffset, HitboxSize, ProjectileSpeed, ProjectileLifetime, CancelTable），攻击系统无需区分"这是职业招式还是技能招式"——统一的 AttackData 接口是核心设计原则。

**2. 稀有度体系**

| 稀有度 | 视觉标记 | 抽取权重占比 | 设计定位 |
|--------|---------|-------------|---------|
| Common (普通) | 蓝色 | 70% | 基础攻击变体——方向不同但机制直观 |
| Rare (稀有) | 紫色 | 20% | 独特机制——改变战斗方式的技能 |
| Epic (史诗) | 金色 | 10% | 变局技能——能扭转战局的高影响力技能 |

稀有度之间的差异不在于数值强度（Epic 不是 "伤害更高的 Common"），而在于**机制的独特性**：
- Common：标准的攻击变体（不同方向的投射物、不同范围/速度的近战）
- Rare：引入新机制（传送后攻击、大范围AOE、返回式投射物）
- Epic：改变战局的独特效果（超大范围、多段命中、特殊位移）

**设计原则**：一个 Common 技能与一个 Epic 技能在总伤害输出上不应差异过大——差异在于"这一击改变了什么"而不是"这一击打了多少"。

**3. 技能 ID 与命名规范**

- 格式：`skill_[kebab-case-name]`（如 `skill_fireball`, `skill_thunder-strike`）
- 全局唯一，不可重复
- 技能 ID 在取消表（CancelTable）中作为目标引用

**4. 数据库查询接口（ISkillDatabase）**

技能数据库提供以下只读查询接口：

| 方法 | 返回类型 | 说明 |
|------|---------|------|
| `GetAllSkills()` | `List<SkillData>` | 返回所有已注册技能 |
| `GetSkillById(SkillId)` | `SkillData` | 按 ID 精确查询 |
| `GetSkillsByRarity(Rarity)` | `List<SkillData>` | 按稀有度筛选 |
| `GetSkillsByTag(Tag)` | `List<SkillData>` | 按标签筛选（用于职业专属池过滤） |
| `GetSkillCount()` | `int` | 返回技能总数 |
| `GetTotalWeight(Rarity)` | `float` | 返回指定稀有度的总权重 |

**5. 运行时行为**

- 技能数据库在游戏初始化时加载所有 SkillData ScriptableObject
- 运行时只读——不对技能数据进行任何修改
- 查询接口供技能抽取系统（筛选牌池）和技能装备管理（实例化技能）使用
- 每个技能数据的查询耗时 < 0.01ms（纯内存读取）

**6. 技能数据与战斗状态注册**

当技能被装备时（技能装备管理负责），技能的 `AttackData` 通过 `ICombatStateProvider.RegisterState(stateDefinition)` 注册到格斗状态机。数据库不负责注册——它只提供数据。`stateDefinition` 包含：
- 状态名称（使用 SkillId）
- 帧数据（StartupFrames, ActiveFrames, RecoveryFrames）
- CancelTable（定义技能后可取消到哪些状态）
- 输入映射（映射到技能槽 A 或 B 的输入按钮）

### States and Transitions

技能数据库是无状态的纯数据层，不维护运行时状态机。

**数据生命周期**：

| 阶段 | 触发 | 行为 |
|------|------|------|
| 设计 | 设计师在 Unity Editor 中创建 SkillData ScriptableObject | 定义技能参数 |
| 构建 | Unity 构建时将 SkillData 打包到资产包 | 成为运行时可加载的数据 |
| 加载 | 游戏初始化（或场景加载时） | 将所有 SkillData 加载到内存，构建索引 |
| 查询 | 技能抽取/装备系统调用 ISkillDatabase 接口 | 返回 SkillData 引用（不复制） |
| 实例化 | 技能装备管理系统读取 SkillData 并注册到格斗状态机 | 创建运行时技能状态 |

### Interactions with Other Systems

| 系统 | 数据流方向 | 接口描述 |
|------|-----------|---------|
| 职业系统 | 职业 → 技能DB | 职业可能定义技能池标签（MVP: 空 = 所有职业共享池）。标签数据由职业系统提供，技能数据库的 Tags 字段用于匹配 |
| 技能抽取系统 | 技能DB → 抽取 | 提供 `GetAllSkills()` 或 `GetSkillsByRarity()` 构建抽取牌池。抽取系统按稀有度权重随机选择 |
| 技能装备管理 | 技能DB → 装备 | 提供 `GetSkillById(SkillId)` 获取技能定义，装备管理读取 AttackData 和状态定义进行注册 |
| 攻击系统 | 间接 | 技能装备管理将技能的 AttackData 注入攻击系统（通过 IAttackDataProvider），攻击系统统一处理 |
| 格斗状态机 | 间接 | 技能装备管理将技能注册为新的 CombatState（通过 ICombatStateProvider），数据库不直接交互 |
| 战斗HUD | 间接 | 技能装备管理提供技能图标和状态信息给 HUD，数据库中的 Icon 和 DisplayName 是数据来源 |
| 能量视觉系统 | 间接 | VFXColor 字段供视觉系统确定技能特效颜色 |

## Formulas

技能数据库是纯数据定义层，不执行运行时公式计算。以下为 MVP 10 个技能的完整数据定义。

**单位系统**: 与攻击系统一致，1 Unity 单位 = 64 像素，时间以 60Hz 帧为基准。

### 1. 稀有度权重分布

`SkillDrawWeight_Skill = (RarityPoolWeight / RarityPoolTotalWeight) × SkillRarityWeight`

| Variable | Symbol | Type | Range | Description |
|----------|--------|------|-------|-------------|
| 稀有度池权重 | RarityPoolWeight | float | 0.1–0.7 | 该稀有度的抽取概率（Common=0.7, Rare=0.2, Epic=0.1） |
| 稀有度池总权重 | RarityPoolTotalWeight | float | — | 该稀有度内所有可用技能的 RarityWeight 之和 |
| 技能稀有度权重 | SkillRarityWeight | float | 0.5–2.0 | 该技能在其稀有度内的相对权重 |

**Output Range**: 0–1.0（归一化后为该技能的绝对抽取概率）
**Example**: Common 稀有度有 7 个技能，每个 RarityWeight=1.0，RarityPoolTotalWeight=7.0。则每个 Common 技能的概率 = 0.7 × (1.0/7.0) = 0.10 = 10%。

### 2. 技能数据总览表

| # | SkillId | 名称 | 稀有度 | 所属 | BaseDmg | BaseKB | Startup | Active | Recovery | 投射物 | 核心定位 |
|---|---------|------|--------|------|---------|--------|---------|--------|----------|--------|---------|
| 1 | skill_counter-strike | 弹反斩 | Common | 通用 | 8.0 | 10.0 | 3 | 2 | 10 | 否 | 短帧反击，高风险高回报 |
| 2 | skill_spinning-kick | 回旋踢 | Common | 通用 | 6.0 | 5.0 | 6 | 8 | 10 | 否 | 360° 周围攻击 |
| 3 | skill_dash-strike | 疾风步 | Common | 通用 | 5.0 | 3.0 | 4 | 4 | 6 | 否 | 快速前冲追击 |
| 4 | skill_shield-bash | 盾击 | Common | 战士 | 8.0 | 14.0 | 6 | 3 | 10 | 否 | 短距离超高击退 |
| 5 | skill_power-strike | 蓄力重击 | Rare | 战士 | 18.0 | 15.0 | 12 | 5 | 14 | 否 | 慢速超高伤害一击 |
| 6 | skill_double-slash | 连斩 | Common | 盗贼 | 9.0 | 4.0 | 3 | 5 | 6 | 否 | 快速连击 |
| 7 | skill_shadow-step | 影步 | Rare | 盗贼 | 7.0 | 6.0 | 7 | 3 | 8 | 否 | 短距瞬移攻击 |
| 8 | skill_ice-arrow | 冰箭 | Common | 法师 | 4.0 | 2.0 | 5 | 2 | 8 | 是 | 快速轻投射物 |
| 9 | skill_fireball | 火球术 | Common | 法师 | 10.0 | 6.0 | 8 | 3 | 10 | 是 | 中速中伤害投射物 |
| 10 | skill_meteor | 陨石坠落 | Epic | 通用 | 20.0 | 18.0 | 20 | 6 | 16 | 否 | 超大范围终极攻击 |

### 3. 通用技能详细数据

**#1 弹反斩 (Counter Strike)** — Common, 通用

| 字段 | 值 | 说明 |
|------|-----|------|
| StartupFrames | 3 | 极短窗口，需精确反应 |
| ActiveFrames | 2 | 判定帧极短，高风险 |
| RecoveryFrames | 10 | 失败后可被惩罚 |
| HitStunFrames | 20 | 成功时高回报 |
| BaseDamage | 8.0 | 中等伤害 |
| BaseKnockback | 10.0 | 高击退（超过 KnockbackThreshold=9.0） |
| IsProjectile | false | |
| HitstopFrames | 6 | 成功命中额外满足感 |
| HitboxOffset | (0.5, 0.2) | 近距离前方 |
| HitboxSize | (0.6, 0.8) | 小判定，需精确 |
| CancelTable | Recovery → Any Attack/Dash/Skill | |

**#2 回旋踢 (Spinning Kick)** — Common, 通用

| 字段 | 值 | 说明 |
|------|-----|------|
| StartupFrames | 6 | |
| ActiveFrames | 8 | 长活跃帧，覆盖旋转动作 |
| RecoveryFrames | 10 | |
| HitStunFrames | 12 | |
| BaseDamage | 6.0 | 低伤害 |
| BaseKnockback | 5.0 | 低击退 |
| IsProjectile | false | |
| HitstopFrames | 3 | |
| HitboxOffset | (0, 0) | 角色中心 |
| HitboxSize | (1.8, 0.8) | 宽广的周围判定 |
| CancelTable | Recovery → Any Attack/Dash/Skill | |

**#3 疾风步 (Dash Strike)** — Common, 通用

| 字段 | 值 | 说明 |
|------|-----|------|
| StartupFrames | 4 | 快速出手 |
| ActiveFrames | 4 | |
| RecoveryFrames | 6 | 快速恢复，可追击 |
| HitStunFrames | 10 | |
| BaseDamage | 5.0 | 轻攻击 |
| BaseKnockback | 3.0 | 低击退 |
| IsProjectile | false | |
| HitstopFrames | 3 | |
| HitboxOffset | (1.0, 0.0) | 前方偏移 |
| HitboxSize | (1.0, 0.6) | 狭长前方判定 |
| CancelTable | Recovery → Any Attack/Dash/Skill | |

### 4. 战士技能详细数据

**#4 盾击 (Shield Bash)** — Common, 战士专属

| 字段 | 值 | 说明 |
|------|-----|------|
| StartupFrames | 6 | |
| ActiveFrames | 3 | |
| RecoveryFrames | 10 | |
| HitStunFrames | 15 | |
| BaseDamage | 8.0 | 中等伤害 |
| BaseKnockback | 14.0 | 超高击退（远超 KnockbackThreshold） |
| IsProjectile | false | |
| HitstopFrames | 5 | |
| HitboxOffset | (0.6, 0.0) | 极近距离 |
| HitboxSize | (0.7, 0.9) | 中等判定 |
| CancelTable | Recovery → Any Attack/Dash/Skill | |

**#5 蓄力重击 (Power Strike)** — Rare, 战士专属

| 字段 | 值 | 说明 |
|------|-----|------|
| StartupFrames | 12 | 极慢启动，可被反应 |
| ActiveFrames | 5 | |
| RecoveryFrames | 14 | 长恢复，挥空代价极高 |
| HitStunFrames | 25 | 超长硬直 |
| BaseDamage | 18.0 | 全游戏最高单次伤害 |
| BaseKnockback | 15.0 | 超高击退 |
| IsProjectile | false | |
| HitstopFrames | 6 | |
| HitboxOffset | (0.8, 0.1) | 前方 |
| HitboxSize | (1.2, 1.0) | 大判定 |
| CancelTable | Recovery → Any Attack/Dash/Skill | |

### 5. 盗贼技能详细数据

**#6 连斩 (Double Slash)** — Common, 盗贼专属

| 字段 | 值 | 说明 |
|------|-----|------|
| StartupFrames | 3 | 极快出手 |
| ActiveFrames | 5 | 长活跃帧模拟连斩 |
| RecoveryFrames | 6 | 快速恢复 |
| HitStunFrames | 10 | |
| BaseDamage | 9.0 | 双段合计伤害 |
| BaseKnockback | 4.0 | 低击退，适合连招 |
| IsProjectile | false | |
| HitstopFrames | 4 | |
| HitboxOffset | (0.7, 0.1) | |
| HitboxSize | (1.0, 0.7) | |
| CancelTable | Recovery → Any Attack/Dash/Skill | |

**#7 影步 (Shadow Step)** — Rare, 盗贼专属

| 字段 | 值 | 说明 |
|------|-----|------|
| StartupFrames | 7 | "消失"动画 |
| ActiveFrames | 3 | "再现 + 攻击" |
| RecoveryFrames | 8 | |
| HitStunFrames | 14 | |
| BaseDamage | 7.0 | 中等伤害 |
| BaseKnockback | 6.0 | 中等击退 |
| IsProjectile | false | |
| HitstopFrames | 4 | |
| HitboxOffset | (1.5, 0.0) | 较远前方（瞬移距离） |
| HitboxSize | (0.8, 0.8) | |
| CancelTable | Recovery → Any Attack/Dash/Skill | |

**设计说明**：影步的 HitboxOffset=(1.5, 0.0) 意味着 hitbox 出现在角色前方 1.5 单位处——视觉上角色"闪现"到该位置攻击。实际实现由技能装备管理系统和攻击系统协调：Startup 阶段角色模型播放消失特效，Active 阶段角色模型瞬移到 HitboxOffset 位置并显示攻击判定。

### 6. 法师技能详细数据

**#8 冰箭 (Ice Arrow)** — Common, 法师专属

| 字段 | 值 | 说明 |
|------|-----|------|
| StartupFrames | 5 | |
| ActiveFrames | 2 | 投射物生成帧 |
| RecoveryFrames | 8 | |
| HitStunFrames | 8 | |
| BaseDamage | 4.0 | 轻伤害 |
| BaseKnockback | 2.0 | 低击退 |
| IsProjectile | true | |
| HitstopFrames | 3 | |
| HitboxOffset | (0.5, 0.3) | 投射物生成点 |
| HitboxSize | (0.3, 0.3) | 小投射物 |
| ProjectileSpeed | 12.0 | 快速飞行 |
| ProjectileLifetime | 45 | 0.75 秒存活，最大射程 9.0 u |
| CancelTable | Recovery → Any Attack/Dash/Skill | |

**#9 火球术 (Fireball)** — Common, 法师专属

| 字段 | 值 | 说明 |
|------|-----|------|
| StartupFrames | 8 | 施法蓄力 |
| ActiveFrames | 3 | 投射物生成帧 |
| RecoveryFrames | 10 | 施法后摇 |
| HitStunFrames | 12 | |
| BaseDamage | 10.0 | 中高伤害 |
| BaseKnockback | 6.0 | 中等击退 |
| IsProjectile | true | |
| HitstopFrames | 4 | |
| HitboxOffset | (0.5, 0.3) | 投射物生成点 |
| HitboxSize | (0.5, 0.5) | 中等投射物 |
| ProjectileSpeed | 7.0 | 中速飞行 |
| ProjectileLifetime | 90 | 1.5 秒存活，最大射程 10.5 u |
| CancelTable | Recovery → Any Attack/Dash/Skill | |

### 7. 史诗技能详细数据

**#10 陨石坠落 (Meteor)** — Epic, 通用

| 字段 | 值 | 说明 |
|------|-----|------|
| StartupFrames | 20 | 极慢启动（0.33 秒），有明确视觉预警 |
| ActiveFrames | 6 | 超大判定持续 |
| RecoveryFrames | 16 | 完全暴露的恢复期 |
| HitStunFrames | 30 | 超长硬直 |
| BaseDamage | 20.0 | 全游戏最高伤害 |
| BaseKnockback | 18.0 | 极端击退 |
| IsProjectile | false | |
| HitstopFrames | 8 | 最强命中反馈 |
| HitboxOffset | (0, 1.5) | 角色上方 |
| HitboxSize | (3.0, 2.0) | 巨大范围 |
| CancelTable | 空（不可取消） | 完全承诺型攻击 |

### 8. 职业技能池构成

| 职业 | 可抽取技能 | 可用技能数 | 4 技能组合数 |
|------|-----------|-----------|-------------|
| 战士 | #1, #2, #3, #4, #5, #10 | 6 | C(6,4) = 15 |
| 盗贼 | #1, #2, #3, #6, #7, #10 | 6 | C(6,4) = 15 |
| 法师 | #1, #2, #3, #8, #9, #10 | 6 | C(6,4) = 15 |

**多样性评估**：每职业 15 种组合，3 职业 = 45 种跨职业组合体验。MVP 足够覆盖前 10-15 局不重复。扩展版（40-50 技能）将显著提升组合多样性。

## Edge Cases

**数据完整性**:
- **如果 SkillData 的 SkillId 为空字符串**: 拒绝加载该技能。数据库初始化时验证所有 SkillId 非空且唯一。重复或空 ID 在 Editor 中报错。
- **如果 SkillData 的 AttackData 的 StartupFrames + ActiveFrames + RecoveryFrames = 0**: 拒绝注册该技能。零帧攻击无法执行。
- **如果技能的 AttackData.HitboxSize = (0, 0)**: 视为数据错误——hitbox 大小为零永远不会命中任何目标。拒绝加载并记录警告。
- **如果投射物技能的 ProjectileSpeed = 0**: 投射物在原位不动。这是合法但无用的设计——不拒绝加载，但在 Editor 中显示警告。
- **如果投射物技能的 ProjectileLifetime = 0**: 投射物生成后立即销毁。拒绝加载——零寿命投射物无意义。

**稀有度权重异常**:
- **如果某稀有度下所有技能的 RarityWeight 之和为 0**: 该稀有度不会被抽到。这是合法的（可用于临时禁用某稀有度），但应有明确的编辑器提示。
- **如果某个技能的 RarityWeight 为负数**: 钳制到 0.0。负权重无意义。

**标签过滤**:
- **如果某个职业没有匹配任何专属技能（Tags 无匹配）**: 该职业只能抽取通用技能和 Epic 通用技能。例如，如果战士专属技能被全部移除，战士的池子 = 3 通用 + 1 Epic = 4 个技能。这是合法的但应警告——组合数降到 C(4,4)=1，每局都一样。
- **如果技能的 Tags 数组为空**: 视为通用技能，所有职业都可抽取。这是默认状态。

**职业池边界**:
- **如果某职业的可用技能数 < MaxSkillsPerMatch (4)**: 该职业无法解锁满 4 个技能。例如可用池只有 3 个技能时，UnlockedCount 上限自动降为 3。由技能抽取系统在查询池时处理。
- **如果所有通用技能 + 职业专属技能的总数 < 4**: 全局技能不足。此时所有职业的 MaxSkillsPerMatch 自动降到总技能数 - 1（至少保留 1 个在池中供抽取）。

**技能被装备后的数据修改**:
- **如果运行时尝试修改已加载的 SkillData**: ScriptableObject 是引用类型——修改会影响所有使用该技能的角色。数据库在初始化后标记为只读。运行时修改应被阻止（通过属性保护或只读接口）。
- **如果两个角色装备了同一个 SkillData**: 共享同一引用，不复制。这是正确行为——技能数据是只读模板，不是运行时状态。运行时状态（冷却、使用次数等）由技能装备管理系统维护。

**影步特殊行为**:
- **如果影步的 HitboxOffset 导致 hitbox 出现在场地边界外**: hitbox 正常创建但可能无目标可命中。与基础攻击的越界行为一致。
- **如果影步瞬移后角色被卡在平台内**: 由 3C 系统的碰撞解算处理（推到最近的合法位置）。技能数据库只定义 hitbox 位置，不负责物理解算。

**扩展性**:
- **如果新增技能但未指定 Rarity**: 默认为 Common。记录警告。
- **如果新增技能的 SkillId 与已有技能重复**: 拒绝加载，Editor 报错。SkillId 必须全局唯一。
- **如果新增技能的 CancelTable 引用了不存在的状态名**: 忽略该条目，不报错。目标状态可能在后续版本中添加。

> `systems-designer` not consulted — Lean mode. Review manually before production.

## Dependencies

| 依赖系统 | 方向 | 类型 | 数据接口 | GDD 状态 |
|---------|------|------|---------|---------|
| 职业系统 | 上游（硬依赖） | 标签过滤 | 职业提供技能池标签（ClassExclusiveTag），数据库的 Tags 字段用于匹配。MVP: 战士/盗贼/法师各有专属技能 | Designed |
| 技能抽取系统 | 下游（硬依赖） | 数据查询 | 提供 `GetAllSkills()`, `GetSkillsByRarity()`, `GetSkillsByTag()` 构建抽取牌池 | Not Started |
| 技能装备管理 | 下游（硬依赖） | 数据查询 | 提供 `GetSkillById(SkillId)` 获取完整 SkillData，装备管理读取 AttackData 和状态定义 | Not Started |
| 攻击系统 | 下游（间接） | 数据消费 | 技能的 AttackData 通过技能装备管理注入攻击系统（IAttackDataProvider），格式与职业基础招式完全一致 | Designed |
| 格斗状态机 | 下游（间接） | 状态注册 | 技能通过技能装备管理注册为新的 CombatState（ICombatStateProvider.RegisterState），数据库不直接交互 | Designed |
| 战斗HUD | 下游（间接） | 数据展示 | 数据库中的 Icon, DisplayName, VFXColor 供 HUD 显示技能信息 | Not Started |
| 能量视觉系统 | 下游（间接） | 视觉数据 | VFXColor 字段供视觉系统确定技能特效颜色（蓝/紫/金按稀有度） | Not Started |
| 专注值系统 | 间接 | 约束 | MaxSkillsPerMatch=4 约束技能抽取上限，专注值系统通过技能抽取系统间接关联 | Designed |

**向上提供的接口契约**:
- `ISkillDatabase` 接口: 技能数据只读查询入口
- `GetAllSkills()`: 返回所有已注册技能的 SkillData 列表
- `GetSkillById(SkillId)`: 按 ID 精确查询，返回单个 SkillData
- `GetSkillsByRarity(Rarity)`: 按稀有度筛选
- `GetSkillsByTag(Tag)`: 按标签筛选（用于职业专属池过滤）
- `GetSkillCount()`: 返回技能总数
- `GetTotalWeight(Rarity)`: 返回指定稀有度的总抽取权重

**双向一致性检查**:
- 职业系统 GDD: "技能数据库 | 职业 → 技能DB | 职业提供技能池标签" — MVP 更新：标签用于过滤，战士/盗贼/法师各有专属技能 ✓
- 攻击系统 GDD: "技能装备管理 | 技能 → 攻击 | 通过 IAttackDataProvider 注入技能 AttackData" ✓ 一致
- 格斗状态机 GDD: "技能装备管理 | 技能 → FSM | ICombatStateProvider.RegisterState" ✓ 一致
- 专注值系统 GDD: "技能抽取系统 | 专注值 → 技能抽取 | OnFocusReady 触发随机抽取" ✓ 一致

## Tuning Knobs

### 稀有度池权重旋钮

| 旋钮名 | 默认值 | 安全范围 | 调高效果 | 调低效果 |
|--------|--------|---------|---------|---------|
| CommonPoolWeight | 0.70 | 0.40–0.85 | 更多 Common 技能被抽到，体验更稳定 | Common 技能变少，Rare/Epic 更常见 |
| RarePoolWeight | 0.20 | 0.10–0.40 | 更多 Rare 技能，独特机制更频繁 | Rare 技能更难获得 |
| EpicPoolWeight | 0.10 | 0.05–0.20 | Epic 技能更常见，对局更"疯狂" | Epic 技能更稀有，抽到更珍贵 |

**约束**: CommonPoolWeight + RarePoolWeight + EpicPoolWeight = 1.0。调整一个必须调整其他。

### 技能数据旋钮（每个技能独立配置）

每个技能的 AttackData 字段都是独立可调的。以下是核心旋钮及其对游戏的影响：

| 旋钮名 | 说明 | 安全范围 | 调高效果 | 调低效果 |
|--------|------|---------|---------|---------|
| BaseDamage | 技能基础伤害 | 2.0–25.0 | 伤害更高，击杀更快 | 伤害更低，偏向骚扰 |
| BaseKnockback | 技能基础击退 | 1.0–20.0 | 击退更远，更容易 KO | 击退更近，偏向连招 |
| StartupFrames | 攻击启动帧数 | 2–25 | 更易被反应和惩罚 | 更难被反应，突袭感更强 |
| ActiveFrames | 判定持续帧数 | 1–10 | 判定窗口更大，更容易命中 | 判定窗口更小，需更精确 |
| RecoveryFrames | 恢复帧数 | 4–20 | 挥空代价更高 | 恢复更快，风险更低 |
| HitstopFrames | 命中冻结帧数 | 0–10 | 命中"卡顿感"更强 | 命中反馈更弱 |
| HitboxSize | hitbox 尺寸 | (0.2,0.2)–(3.0,2.5) | 攻击范围更大 | 需要更精确的定位 |
| HitboxOffset | hitbox 偏移 | (-3,-2)–(3,3) | 攻击更远/更高 | 攻击更近/更低 |
| ProjectileSpeed | 投射物速度 | 3.0–15.0 u/s | 投射物更快，更难闪避 | 投射物更慢，更容易反应 |
| ProjectileLifetime | 投射物存活帧数 | 20–120 | 射程更远 | 射程更短 |
| RarityWeight | 同稀有度内权重 | 0.5–2.0 | 该技能更常被抽到 | 该技能更少被抽到 |

**旋钮交互警告**:
- `BaseDamage` 和 `BaseKnockback` 共同决定技能的总威胁等级——两者同时调高会创造"一击必杀"技能，与设计原则冲突
- `StartupFrames` 和 `RecoveryFrames` 决定技能的总帧数——总帧数 > 30 帧的技能需要极高的回报才值得使用
- `ProjectileSpeed` × `ProjectileLifetime / 60` = 投射物最大射程。调一个必须检查另一个
- `HitboxSize` 需要配合角色体型（SilhouetteScale）——小角色的技能 hitbox 不应与角色体型不协调
- 所有技能伤害值需要与伤害计算系统、击退系统的公式联调——单独调整可能导致整体平衡崩溃
- `CommonPoolWeight` 和 `RarePoolWeight` 的比例直接影响每局"普通 vs 独特"技能的出现频率——Pillar 2 要求独特感，比例不宜过于极端

## Visual/Audio Requirements

**技能视觉标识（稀有度着色）**:

每个技能按稀有度使用固定颜色体系，贯穿所有视觉元素：

| 稀有度 | 主色调 | Hex | 应用场景 |
|--------|--------|-----|---------|
| Common | 蓝色 | #4080FF | 技能图标边框、命中特效、HUD 图标光晕 |
| Rare | 紫色 | #A040FF | 技能图标边框、命中特效、解锁动画强调 |
| Epic | 金色 | #FFB020 | 技能图标边框、命中特效、解锁动画爆发、全屏闪光 |

**技能特效方向（每个技能的视觉差异）**:

| 技能 | 特效关键词 | 视觉参考 |
|------|-----------|---------|
| 弹反斩 | 短暂闪光 + 冲击波纹 | 精确反击的瞬间爆发 |
| 回旋踢 | 旋转弧线拖尾 | 脚下圆形判定可视化 |
| 疾风步 | 残影拖尾 | 快速移动的视觉痕迹 |
| 盾击 | 盾形冲击波 | 短距离但厚重的视觉反馈 |
| 蓄力重击 | 蓄力光球 → 爆发 | 启动时能量聚集，命中时大爆发 |
| 连斩 | 双重斩击弧线 | 两道交错的攻击痕迹 |
| 影步 | 消失烟雾 → 再现闪光 | 起点烟雾、终点闪光 |
| 冰箭 | 冰晶投射物 + 命中碎冰 | 小型冰蓝色飞行体 |
| 火球术 | 火焰投射物 + 命中爆炸 | 中型火球飞行 + 爆炸 |
| 陨石坠落 | 天空预警标记 → 巨大陨石坠落 | 启动时地面出现红色警告圈 |

**音频事件（定义触发时机，音效系统实现）**:

| 音频事件 | 触发时机 | 描述 |
|---------|---------|------|
| `OnSkillHit_Common` | Common 技能命中 | 标准技能命中音效 |
| `OnSkillHit_Rare` | Rare 技能命中 | 更强烈的命中音效 |
| `OnSkillHit_Epic` | Epic 技能命中 | 最强烈的命中 + 环境震音 |
| `OnSkillStartup_ShadowStep` | 影步启动 | "消失"音效（嗖/噗） |
| `OnSkillStartup_Meteor` | 陨石启动 | 低频预警轰鸣 |

> **Asset Spec Flag** — Visual/Audio 需求已定义。Art Bible 审批后，运行 `/asset-spec system:skill-database` 生成每个技能的视觉描述、尺寸和制作提示。

## UI Requirements

**技能图标（战斗 HUD 组件）**:
- 每个技能有独立的 Sprite 图标（SkillData.Icon 字段）
- 图标边框颜色按稀有度：蓝/紫/金
- 技能槽位显示当前装备的技能图标（由技能装备管理系统提供数据）

**技能信息展示**:
- 技能名称（SkillData.DisplayName）
- 技能描述（SkillData.Description）——在解锁时短暂弹出显示
- 稀有度标记——图标边框颜色即可，无需额外文字

**手柄导航**: 技能图标是纯信息显示，无需手柄交互。技能使用通过技能槽按钮触发。

## Acceptance Criteria

### 数据完整性

- **GIVEN** 技能数据库已初始化, **WHEN** 查询所有技能, **THEN** 恰好返回 10 个 SkillData（MVP）
- **GIVEN** 技能数据库已初始化, **WHEN** 查询任意技能的 SkillId, **THEN** SkillId 非空、格式为 `skill_[kebab-case]`、全局唯一
- **GIVEN** 任意 SkillData, **WHEN** 检查 AttackData, **THEN** StartupFrames + ActiveFrames + RecoveryFrames > 0，HitboxSize > (0, 0)
- **GIVEN** 投射物技能（IsProjectile=true）, **WHEN** 检查 AttackData, **THEN** ProjectileSpeed > 0 且 ProjectileLifetime > 0

### 稀有度分布

- **GIVEN** 10 个技能已加载, **WHEN** 按稀有度统计, **THEN** Common=7, Rare=2, Epic=1
- **GIVEN** 稀有度权重 CommonPoolWeight=0.7, RarePoolWeight=0.2, EpicPoolWeight=0.1, **WHEN** 计算每个技能的绝对抽取概率, **THEN** 每个 Common 技能 = 10%，每个 Rare 技能 = 10%，Epic 技能 = 10%
- **GIVEN** CommonPoolWeight + RarePoolWeight + EpicPoolWeight, **WHEN** 验证, **THEN** 总和 = 1.0

### 职业技能池

- **GIVEN** 战士职业查询可用技能池, **WHEN** 过滤 Tags 匹配, **THEN** 返回 6 个技能（#1, #2, #3, #4, #5, #10）
- **GIVEN** 盗贼职业查询可用技能池, **WHEN** 过滤 Tags 匹配, **THEN** 返回 6 个技能（#1, #2, #3, #6, #7, #10）
- **GIVEN** 法师职业查询可用技能池, **WHEN** 过滤 Tags 匹配, **THEN** 返回 6 个技能（#1, #2, #3, #8, #9, #10）
- **GIVEN** 某职业可用技能数为 N < MaxSkillsPerMatch(4), **WHEN** 技能抽取系统查询池, **THEN** MaxSkillsPerMatch 自动降为 N

### 查询接口

- **GIVEN** 数据库已加载 10 个技能, **WHEN** 调用 GetSkillById("skill_fireball"), **THEN** 返回 Fireball 的 SkillData
- **GIVEN** 数据库已加载 10 个技能, **WHEN** 调用 GetSkillById("skill_nonexistent"), **THEN** 返回 null
- **GIVEN** 数据库已加载 10 个技能, **WHEN** 调用 GetSkillsByRarity(Rare), **THEN** 返回 2 个技能（#5, #7）
- **GIVEN** 数据库已加载 10 个技能, **WHEN** 调用 GetSkillCount(), **THEN** 返回 10

### 技能差异化

- **GIVEN** 所有 10 个技能, **WHEN** 比较 BaseDamage, **THEN** 最低 4.0（冰箭），最高 20.0（陨石），至少有 3 个不同的伤害值
- **GIVEN** 所有 10 个技能, **WHEN** 比较 StartupFrames, **THEN** 最快 3（弹反斩），最慢 20（陨石）
- **GIVEN** 7 个 Common 技能, **WHEN** 比较 BaseDamage, **THEN** Common 技能的伤害在 4.0–10.0 范围内（不超过 Rare/Epic）
- **GIVEN** 所有投射物技能, **WHEN** 检查 IsProjectile=true, **THEN** 恰好 2 个（冰箭、火球术，均为法师专属）

### 数据安全性

- **GIVEN** 数据库初始化完成, **WHEN** 尝试修改已加载的 SkillData 的 BaseDamage, **THEN** 修改被阻止或无效（只读保护）
- **GIVEN** 两个角色装备了同一个 SkillData（如火球术）, **WHEN** 角色A 使用该技能, **THEN** 角色B 的 SkillData 不受影响（共享引用但运行时状态独立）

### 性能

- **GIVEN** 10 个技能已加载, **WHEN** 调用任意查询接口, **THEN** 响应时间 < 0.01ms（纯内存读取）

> `qa-lead` not consulted — Lean mode. Review manually before production.

## Open Questions

1. **影步的瞬移机制如何实现？** 当前设计 HitboxOffset=(1.5, 0.0) 定义了 hitbox 位置，但角色模型的实际位移需要在技能装备管理和 3C 系统中协调。需要确定：角色是否真正移动到 hitbox 位置？还是只有视觉瞬移效果？（Owner: 技能装备管理设计师，里程碑: 技能装备管理 GDD）

2. **连斩是否应该真正实现双段命中？** 当前设计为单次 9.0 伤害合并双斩。如果需要真正的双段命中（每段独立判定），需要修改 AttackData 结构或引入多段攻击机制。（Owner: 系统设计师，里程碑: 技能装备管理 GDD）

3. **技能池多样性如何在扩展版中提升？** MVP 每职业只有 15 种组合。扩展到 40-50 技能时，需要定义更多职业专属技能和通用技能，同时维护平衡。（Owner: 游戏设计师，里程碑: Vertical Slice 阶段）

4. **通用技能是否应该考虑职业差异？** 当前弹反斩/回旋踢/疾风步对所有职业表现完全相同。是否需要按职业微调 hitbox 大小（配合 SilhouetteScale）？（Owner: 平衡设计师，里程碑: 平衡调试期）
