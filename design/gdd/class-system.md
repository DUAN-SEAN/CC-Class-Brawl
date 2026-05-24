# 职业系统 (Class System)

> **Status**: Designed
> **Author**: SeanDuan + agents
> **Last Updated**: 2026-05-23
> **Implements Pillar**: Pillar 1: 秒学秒玩, Pillar 3: 高手菜鸟都开心

## Overview

职业系统是职业对决的角色身份定义层，负责管理游戏中的职业数据（移动属性、基础招式、视觉特征）并为上层系统提供统一的职业数据查询接口。作为数据驱动系统，它以配置数据为载体定义每个职业的唯一属性组合——战士力量型慢速重击、盗贼敏捷型快速连击、法师范围型远程攻击——这些属性值直接注入 3C 系统、攻击系统和技能系统，驱动角色的移动手感、战斗方式和视觉呈现。玩家在选人界面选择职业，此后整局对战中职业身份固定不变（随机技能叠加在职业基础之上，不替换职业核心）。职业系统的差异化维度有三个：移动属性（速度、跳跃力、冲刺距离等由 3C 系统消费）、基础招式集（地面攻击、空中攻击、冲刺攻击的帧数据由攻击系统消费）、以及视觉身份（轮廓、配色、动画风格由呈现层消费）。没有职业系统，所有角色将是完全相同的"白板"——无法体现"战士/法师/盗贼"的身份幻想，也无法为随机技能系统提供职业特色的生长基础。MVP 包含三个职业：战士（力量型，宽厚+暖色）、盗贼（敏捷型，纤细+暗色）、法师（范围型，飘逸+冷色）。

## Player Fantasy

**核心幻想：「我就是这个角色」**

玩家选择的不是一个数据集合，而是一个完整的战斗身份。选战士的玩家应该感觉自己是"那个力量型选手"——每一击都很重、每一步都踏实、对手被击飞的瞬间有一种"这就是战士该做的事"的满足感。选盗贼的玩家感觉自己是"那个速度型选手"——穿梭在平台间、连绵不断的快速攻击、对手还没反应过来就已经被打了一套。选法师的玩家感觉自己是"那个范围控制型选手"——远距离的魔法攻击、大面积的技能效果、站在场地中心掌控战局。

**关键情感时刻**：
- **第一次选择** — 看到三个截然不同的角色轮廓和配色，直觉就知道"我是哪一种人"
- **首次体验差异** — 从盗贼切换到战士时，明显感觉"哇这完全不一样"，不是微调而是质变
- **职业身份强化** — 经过几局后，玩家开始说"我是战士玩家"或"我主玩法师"，产生归属感
- **随机技能的叠加乐趣** — 随机技能叠加在职业基础上时，产生"我的战士学会了火球术"的惊喜，而不是"这火球术替换了战士身份"

**支柱对齐**：
- 服务 **Pillar 1: 秒学秒玩** — 看到角色就知道怎么玩：大个子 = 重击，小个子 = 速度
- 服务 **Pillar 3: 高手菜鸟都开心** — 新手凭直觉选职业就能打，高手在职业基础上探索随机技能的最优搭配

> `creative-director` not consulted — Lean mode. Review manually before production.

## Detailed Design

### Core Rules

**1. 职业数据架构**

每个职业是一个独立的配置数据单元（Unity ScriptableObject），包含三大类数据：
- **移动属性集** — 注入 3C 系统的参数值
- **基础招式集** — 注入攻击系统的 AttackData 数组（3 个招式）
- **视觉身份** — 注入呈现层的角色视觉定义

职业数据是只读的——一旦对局开始，属性值不可被修改。随机技能叠加在职业基础之上，不修改职业原始属性。

**2. 移动属性集**

职业定义以下移动参数，覆盖 3C 系统的默认值：

| 参数 | 对应 3C Tuning Knob | 说明 |
|------|---------------------|------|
| MaxGroundSpeed | MaxGroundSpeed | 最大地面奔跑速度 |
| MoveAcceleration | MoveAcceleration | 地面加速度 |
| JumpHeight | JumpHeight | 完整跳跃高度 |
| MaxAirSpeed | MaxAirSpeed | 空中水平速度上限 |
| DashDistance | DashDistance | 冲刺总距离 |

以下参数为全局共享（由 3C 系统默认值定义，所有职业相同）：Gravity, GroundFriction, TerminalVelocity, FastFallGravityMultiplier, AirAcceleration, AirJumpForceRatio, ShortHopWindow, DashStartupFrames, DashActiveFrames, DashRecoveryFrames, DashCooldownFrames, LandingLagFrames。

**3. 基础招式集**

每个职业定义 3 个基础招式，对应格斗状态机的攻击输入类型：

| 招式 ID | 触发条件 | 说明 |
|---------|---------|------|
| `GroundAttack` | 地面 + 攻击输入 | 地面基础攻击 |
| `AirAttack` | 空中 + 攻击输入 | 空中基础攻击 |
| `DashAttack` | 冲刺中 + 攻击输入 | 冲刺基础攻击 |

每个招式包含以下数据（对应格斗状态机消费的 AttackData）：

| 字段 | 类型 | 说明 |
|------|------|------|
| StartupFrames | int | 启动帧数 |
| ActiveFrames | int | 活跃帧数 |
| RecoveryFrames | int | 恢复帧数 |
| HitStunFrames | int | 命中后对手硬直帧数 |
| BaseDamage | float | 基础伤害值 |
| BaseKnockback | float | 基础击退力度 |
| IsProjectile | bool | 是否为投射物攻击 |
| CancelTable | CancelEntry[] | 可取消到哪些状态（默认：Recovery → 任意攻击/Dash） |

**4. 视觉身份**

每个职业定义以下视觉数据：

| 字段 | 说明 |
|------|------|
| ClassName | 职业名称（本地化 key） |
| ClassDescription | 职业描述（本地化 key） |
| PrimaryColor | 主色调（用于轮廓发光、特效） |
| SecondaryColor | 副色调（用于细节装饰） |
| BodyType | 体型分类：`Bulky`(宽厚) / `Slim`(纤细) / `Flowing`(飘逸) |
| SilhouetteScale | 轮廓缩放比例（影响角色可视大小） |

**5. 职业选择与生命周期**

1. **选择阶段**：玩家在选人界面选择职业（由角色选择 UI 管理）
2. **注入阶段**：选定后，职业数据注入该玩家的角色实例——移动属性传给 3C 系统，招式数据传给攻击系统
3. **战斗阶段**：职业属性整局固定，随机技能叠加不修改职业原始属性
4. **结算阶段**：对局结束，职业数据随角色实例销毁

**6. MVP 职业定义概要**

**战士 (Warrior)**
- 体型：宽厚 (Bulky)，SilhouetteScale > 1.0
- 配色：暖色主调（红/橙）
- 移动：慢速地面、短跳跃、长冲刺（冲刺是战士的主要接近手段）
- 战斗：高伤害、高击退、慢启动、长恢复。经典"一击必感"。
- 特殊：无投射物，所有攻击为近战接触

**盗贼 (Rogue)**
- 体型：纤细 (Slim)，SilhouetteScale < 1.0
- 配色：暗色主调（绿/深蓝）
- 移动：快速地面、高跳跃、短冲刺（机动性靠基础移动而非冲刺）
- 战斗：低伤害、低击退、快启动、短恢复。经典"连击压制"。
- 特殊：无投射物，所有攻击为近战接触

**法师 (Mage)**
- 体型：飘逸 (Flowing)，SilhouetteScale ≈ 1.0
- 配色：冷色主调（蓝/青）
- 移动：中等速度、中等跳跃、中等冲刺
- 战斗：中等伤害、中等击退、中等帧数据。独特手感的投射物攻击。
- 特殊：GroundAttack 和 AirAttack 为投射物攻击（`IsProjectile = true`），DashAttack 为近战。法师是 MVP 中唯一拥有投射物基础招式的职业。

### States and Transitions

职业系统本身是无状态的（纯数据层）。职业的"状态"由消费它的系统管理：
- **选择状态** → 由角色选择 UI / 游戏状态管理系统管理
- **注入状态** → 角色实例创建时一次性注入
- **战斗中的职业效果** → 由 3C、攻击、呈现层各自维护

职业系统不维护运行时状态机。

### Interactions with Other Systems

| 系统 | 数据流方向 | 接口描述 |
|------|-----------|---------|
| 3C系统 | 职业 → 3C | 职业提供移动属性值：MaxGroundSpeed, MoveAcceleration, JumpHeight, MaxAirSpeed, DashDistance。3C 系统在角色初始化时读取并覆盖默认值 |
| 攻击系统 | 职业 → 攻击 | 职业提供基础招式 AttackData 数组（3 个）。攻击系统在角色初始化时注册这些招式 |
| 技能数据库 | 职业 → 技能DB | 职业可能定义"职业专属技能池标签"，限制某些技能只能被特定职业抽到（MVP 可为空 = 所有职业共享技能池） |
| 角色选择UI | 职业 → UI | 职业提供视觉数据（ClassName, ClassDescription, PrimaryColor, BodyType）用于选人界面展示 |
| 战斗HUD | 职业 → HUD | 职业提供 PrimaryColor 用于血条/专注值条着色 |
| 伤害计算系统 | 职业 → 伤害 | 职业可能提供防御/抗性属性（MVP 无 — 伤害纯粹由攻击数据决定。预留 `DefenseMultiplier` 字段供后续版本使用） |

## Formulas

职业系统是纯数据定义层，不执行任何运行时公式计算。以下为 MVP 三个职业的完整数据定义表。所有数值为初始设计值，最终值通过 Tuning Knobs 调校。

**单位系统**: 与 3C 系统一致，1 Unity 单位 = 64 像素，时间以 60Hz 帧为基准。

### 1. 移动属性数据

| 属性 | 战士 | 盗贼 | 法师 | 3C 默认值 |
|------|------|------|------|----------|
| MaxGroundSpeed | 3.8 u/s | 6.5 u/s | 4.8 u/s | 5.0 u/s |
| MoveAcceleration | 40.0 u/s² | 75.0 u/s² | 55.0 u/s² | 57.0 u/s² |
| JumpHeight | 2.8 u | 4.2 u | 3.5 u | 3.5 u |
| MaxAirSpeed | 2.8 u/s | 4.5 u/s | 3.5 u/s | 3.5 u/s |
| DashDistance | 3.2 u | 2.0 u | 2.5 u | 2.5 u |

**设计意图**:
- 战士：最慢移动，但最长冲刺作为接近手段。短跳强迫对手接近。
- 盗贼：最快移动和最高跳跃，但短冲刺意味着机动性靠基础移动。
- 法师：全面中等，与默认值接近。投射物允许中距离作战。

### 2. 基础招式帧数据

**战士招式**:

| 招式 | Startup | Active | Recovery | 总帧 | HitStun | BaseDamage | BaseKnockback | IsProjectile |
|------|---------|--------|----------|------|---------|------------|---------------|-------------|
| GroundAttack | 8 | 6 | 14 | 28 | 18 | 12.0 | 8.0 | false |
| AirAttack | 7 | 5 | 12 | 24 | 15 | 10.0 | 7.0 | false |
| DashAttack | 9 | 7 | 16 | 32 | 22 | 15.0 | 12.0 | false |

**盗贼招式**:

| 招式 | Startup | Active | Recovery | 总帧 | HitStun | BaseDamage | BaseKnockback | IsProjectile |
|------|---------|--------|----------|------|---------|------------|---------------|-------------|
| GroundAttack | 3 | 3 | 6 | 12 | 8 | 4.0 | 2.0 | false |
| AirAttack | 3 | 2 | 7 | 12 | 7 | 3.0 | 1.5 | false |
| DashAttack | 4 | 4 | 8 | 16 | 10 | 6.0 | 3.5 | false |

**法师招式**:

| 招式 | Startup | Active | Recovery | 总帧 | HitStun | BaseDamage | BaseKnockback | IsProjectile |
|------|---------|--------|----------|------|---------|------------|---------------|-------------|
| GroundAttack | 10 | 4 | 12 | 26 | 12 | 7.0 | 4.0 | true |
| AirAttack | 8 | 3 | 10 | 21 | 10 | 6.0 | 3.5 | true |
| DashAttack | 5 | 5 | 10 | 20 | 12 | 8.0 | 5.0 | false |

**投射物说明**: 法师的 GroundAttack 和 AirAttack 标记为 `IsProjectile = true`。Startup 为施法蓄力帧，Active 为投射物生成帧（投射物独立飞行，由攻击系统管理其生命周期），Recovery 为施法后摇。投射物速度、范围、存活时间由攻击系统定义（不在此 GDD 中）。

### 3. 视觉身份数据

| 属性 | 战士 | 盗贼 | 法师 |
|------|------|------|------|
| BodyType | Bulky | Slim | Flowing |
| SilhouetteScale | 1.2 | 0.85 | 1.0 |
| PrimaryColor | 暖红 #E84545 | 暗绿 #2ECC71 | 冷蓝 #5EADF2 |
| SecondaryColor | 橙色 #F08020 | 深蓝 #203060 | 青色 #40D0D0 |

色彩值已由 art bible 校准（参见 design/art/art-bible.md §4.1）。

## Edge Cases

**数据完整性**:
- **如果职业 ScriptableObject 的移动属性值为 0 或负数**: 在注入时钳制到 3C 系统的安全范围下限（如 MaxGroundSpeed 最小 3.0）。记录警告日志。零值移动意味着角色完全无法动——这是数据错误，不是设计意图。
- **如果职业招式的 Startup/Active/Recovery 总和为 0**: 拒绝注入该招式，格斗状态机忽略此攻击输入。零帧攻击无法实现。
- **如果 CancelTable 为空数组**: Recovery 阶段不可取消到任何状态——攻击必须完整执行后才能行动。这是合法设计（可用于设计"高风险高回报"招式）。
- **如果 IsProjectile = true 但攻击系统尚未实现投射物逻辑**: 当作 `IsProjectile = false` 处理（近战 hitbox）。记录警告。MVP 阶段攻击系统的投射物支持可能延后。

**多职业选择**:
- **如果两个玩家选择相同职业**: 允许。相同职业的对战是合法的（"镜像战"）。视觉上通过玩家编号着色区分（P1/P2 标识色叠加在职业色之上）。
- **如果选人阶段没有任何玩家选择职业**: 由游戏状态管理系统处理（使用默认职业或等待超时），不在此系统范围内。

**属性极端值**:
- **如果 MaxGroundSpeed 被调到极高（> 8.0）**: 3C 系统的安全范围上限为 8.0 u/s，超过的值被 3C 系统钳制。职业系统不强制限制——由 3C 系统负责边界保护。
- **如果战士的 DashDistance 长于场地宽度**: 角色冲出场地边界，由场地系统处理（墙壁碰撞或 blast zone）。职业系统不限制冲刺距离。
- **如果 JumpHeight 极低（< 1.0 u）**: 角色几乎无法跳跃。3C 系统安全范围下限为 2.0 u。低于此值的配置由 3C 钳制。

**投射物相关**:
- **如果法师投射物速度为 0**: 投射物在原位生成但不动。由攻击系统处理——职业系统只定义"是否为投射物"，速度由攻击系统的招式数据定义。
- **如果法师空中投射物攻击时角色着地**: 攻击正常继续执行（格斗状态机不因着地取消空中攻击）。投射物独立于角色状态飞行。

**扩展性**:
- **如果新增职业但未提供完整的 5 个移动属性**: 缺失属性使用 3C 系统默认值。不拒绝加载——允许部分定义。
- **如果新增职业但未提供 3 个基础招式中的某个**: 缺失招式的攻击输入无响应（格斗状态机找不到对应 AttackData）。记录警告。这是合法设计——某些职业可能没有 DashAttack。
- **如果新增职业的 BodyType 不是三种预定义之一**: 默认为 Flowing（最中性的体型）。记录警告。

## Dependencies

| 依赖系统 | 方向 | 类型 | 数据接口 | GDD 状态 |
|---------|------|------|---------|---------|
| 3C系统 | 下游（硬依赖） | 数据注入 | 职业提供移动属性值：MaxGroundSpeed, MoveAcceleration, JumpHeight, MaxAirSpeed, DashDistance | In Review |
| 攻击系统 | 下游（硬依赖） | 数据注入 | 职业提供基础招式 AttackData 数组（3 个招式，含帧数据/伤害/击退/投射物标记/取消表） | 未设计 |
| 技能数据库 | 下游（软依赖） | 标签匹配 | 职业提供技能池标签（MVP 为空 = 所有职业共享技能池） | 未设计 |
| 角色选择UI | 下游（软依赖） | 数据展示 | 职业提供视觉数据：ClassName, ClassDescription, PrimaryColor, BodyType, SilhouetteScale | 未设计 |
| 战斗HUD | 下游（软依赖） | 数据展示 | 职业提供 PrimaryColor 用于 HUD 着色 | 未设计 |
| 伤害计算系统 | 下游（软依赖） | 数据注入 | 预留 DefenseMultiplier 字段（MVP 不使用） | 未设计 |
| 游戏状态管理 | 上游（软依赖） | 流程控制 | 游戏状态管理触发职业选择和注入时机 | Designed |

**向上提供的接口契约**:
- `IClassData` 接口: 职业数据的只读访问入口
- 移动属性: MaxGroundSpeed, MoveAcceleration, JumpHeight, MaxAirSpeed, DashDistance
- 招式数据: `AttackData[]` 数组（按 GroundAttack/AirAttack/DashAttack 索引）
- 视觉数据: ClassName, PrimaryColor, SecondaryColor, BodyType, SilhouetteScale
- 查询接口: `GetClassData()` → IClassData

**双向一致性检查**:
- 3C系统 GDD 列出"职业系统 → 3C: 职业提供基础移动属性" ✓ 一致
- 格斗状态机 GDD 列出"职业系统 | 上游（软依赖）" ✓ 一致（格斗状态机通过攻击系统间接获取职业招式数据）

## Tuning Knobs

### 移动属性旋钮

| 旋钮名 | 职业默认值 | 安全范围 | 调高效果 | 调低效果 |
|--------|-----------|---------|---------|---------|
| Warrior_MaxGroundSpeed | 3.8 u/s | 3.0–5.0 | 战士更快，接近对手更容易 | 战士更慢，更难接近 |
| Rogue_MaxGroundSpeed | 6.5 u/s | 5.0–8.0 | 盗贼更快，压制力更强 | 盗贼变慢，压制力降低 |
| Mage_MaxGroundSpeed | 4.8 u/s | 3.5–6.0 | 法师更灵活 | 法师更笨重 |
| Warrior_MoveAcceleration | 40.0 u/s² | 30–55 | 战士加速更灵敏 | 战士加速更迟钝 |
| Rogue_MoveAcceleration | 75.0 u/s² | 55–100 | 盗贼加速更灵敏 | 盗贼加速更迟钝 |
| Mage_MoveAcceleration | 55.0 u/s² | 40–70 | 法师加速更灵敏 | 法师加速更迟钝 |
| Warrior_JumpHeight | 2.8 u | 2.0–3.5 | 战士跳得更高 | 战士跳得更低 |
| Rogue_JumpHeight | 4.2 u | 3.5–5.0 | 盗贼跳得更高 | 盗贼跳得更低 |
| Mage_JumpHeight | 3.5 u | 2.5–4.5 | 法师跳得更高 | 法师跳得更低 |
| Warrior_MaxAirSpeed | 2.8 u/s | 2.0–3.5 | 战士空中控制更好 | 战士空中更僵硬 |
| Rogue_MaxAirSpeed | 4.5 u/s | 3.5–6.0 | 盗贼空中控制更好 | 盗贼空中更僵硬 |
| Mage_MaxAirSpeed | 3.5 u/s | 2.5–4.5 | 法师空中控制更好 | 法师空中更僵硬 |
| Warrior_DashDistance | 3.2 u | 2.5–4.0 | 战士冲刺更远（核心接近手段） | 战士冲刺更近 |
| Rogue_DashDistance | 2.0 u | 1.5–3.0 | 盗贼冲刺更远 | 盗贼冲刺更近 |
| Mage_DashDistance | 2.5 u | 1.5–3.5 | 法师冲刺更远 | 法师冲刺更近 |

### 招式帧数据旋钮

每个招式有 8 个可调值（StartupFrames, ActiveFrames, RecoveryFrames, HitStunFrames, BaseDamage, BaseKnockback, IsProjectile, CancelTable）。IsProjectile 和 CancelTable 为功能开关，不做连续调整。以下是可调的数值旋钮，以每个职业的 GroundAttack 为例（AirAttack 和 DashAttack 结构相同）：

| 旋钮名 | 战士 | 盗贼 | 法师 | 安全范围 | 调高效果 | 调低效果 |
|--------|------|------|------|---------|---------|---------|
| Warrior_Ground_Startup | 8 | — | — | 4–15 | 启动更慢（更易被反应） | 启动更快（更难被反应） |
| Rogue_Ground_Startup | — | 3 | — | 2–6 | 启动更慢 | 启动更快（极快连击） |
| Mage_Ground_Startup | — | — | 10 | 6–15 | 启动更慢（投射物蓄力更久） | 启动更快（投射物更快发出） |
| Warrior_Ground_Damage | 12.0 | — | — | 8–18 | 伤害更高 | 伤害更低 |
| Rogue_Ground_Damage | — | 4.0 | — | 2–7 | 伤害更高 | 伤害更低 |
| Mage_Ground_Damage | — | — | 7.0 | 4–12 | 伤害更高 | 伤害更低 |
| Warrior_Ground_Knockback | 8.0 | — | — | 5–15 | 击退更远 | 击退更近 |
| Rogue_Ground_Knockback | — | 2.0 | — | 1–5 | 击退更远 | 击退更近 |
| Mage_Ground_Knockback | — | — | 4.0 | 2–8 | 击退更远 | 击退更近 |

### 视觉旋钮

| 旋钮名 | 默认值 | 安全范围 | 说明 |
|--------|--------|---------|------|
| Warrior_SilhouetteScale | 1.2 | 1.0–1.5 | 战士体型大小 |
| Rogue_SilhouetteScale | 0.85 | 0.7–1.0 | 盗贼体型大小 |
| Mage_SilhouetteScale | 1.0 | 0.85–1.2 | 法师体型大小 |
| 职业主色调 | (见 Formulas §3) | — | 由 art bible 校准，不在平衡调参范围 |

**旋钮交互警告**:
- 同一职业的 `MaxGroundSpeed` 和 `MoveAcceleration` 决定达到极速的时间（= MaxSpeed / Accel）。调一个必须检查另一个。
- 战士的 `DashDistance` 是核心接近手段——缩短太多会让战士无法接近投射物型对手（法师），破坏职业三角平衡。
- 盗贼的招式总帧数极短（12 帧 = 0.2 秒）——进一步缩短会让盗贼变得无法被惩罚（太快了）。
- 所有伤害和击退值需要与伤害计算系统、击退系统的公式联调——单独调整职业值可能导致整体平衡崩溃。

## Visual/Audio Requirements

[To be designed]

## UI Requirements

[To be designed]

## Acceptance Criteria

### 数据完整性

- **GIVEN** 职业配置数据已加载, **WHEN** 查询任意职业的移动属性, **THEN** 所有 5 个属性（MaxGroundSpeed, MoveAcceleration, JumpHeight, MaxAirSpeed, DashDistance）均有正值且在 3C 系统安全范围内
- **GIVEN** 职业配置数据已加载, **WHEN** 查询任意职业的招式数据, **THEN** 恰好有 3 个 AttackData（GroundAttack, AirAttack, DashAttack），每个的 Startup+Active+Recovery > 0
- **GIVEN** 职业配置数据中某移动属性为 0 或负数, **WHEN** 注入 3C 系统, **THEN** 该值被钳制到 3C 安全范围下限并记录警告

### 职业差异化

- **GIVEN** 选择了战士职业, **WHEN** 注入 3C 系统, **THEN** MaxGroundSpeed < 默认值（5.0）且 DashDistance > 默认值（2.5）
- **GIVEN** 选择了盗贼职业, **WHEN** 注入 3C 系统, **THEN** MaxGroundSpeed > 默认值（5.0）且 JumpHeight > 默认值（3.5）
- **GIVEN** 选择了法师职业, **WHEN** 查询 GroundAttack, **THEN** IsProjectile = true
- **GIVEN** 选择了战士职业, **WHEN** 查询所有招式, **THEN** 所有 IsProjectile = false
- **GIVEN** 选择了盗贼职业, **WHEN** 查询 GroundAttack, **THEN** StartupFrames < 战士的 GroundAttack StartupFrames（盗贼更快）

### 多人选择

- **GIVEN** P1 选择战士, P2 也选择战士, **WHEN** 对局开始, **THEN** 两个角色具有相同的移动属性和招式数据，但视觉上有 P1/P2 标识色区分

### 数据注入

- **GIVEN** 玩家在选人界面选择了盗贼, **WHEN** 角色实例创建, **THEN** 3C 系统使用盗贼的移动属性值（MaxGroundSpeed=6.5, MoveAcceleration=75.0, JumpHeight=4.2 等）而非默认值
- **GIVEN** 玩家在选人界面选择了战士, **WHEN** 角色实例创建, **THEN** 攻击系统注册战士的 3 个基础招式 AttackData
- **GIVEN** 对局结束, **WHEN** 角色实例销毁, **THEN** 职业数据不残留——下一局重新选择职业时使用新数据

### 视觉身份

- **GIVEN** 选择了战士职业, **WHEN** 渲染角色, **THEN** 角色轮廓为宽厚型（Bulky），主色调为暖色系
- **GIVEN** 选择了盗贼职业, **WHEN** 渲染角色, **THEN** 角色轮廓为纤细型（Slim），主色调为暗色系
- **GIVEN** 选择了法师职业, **WHEN** 渲染角色, **THEN** 角色轮廓为飘逸型（Flowing），主色调为冷色系

### 固定性

- **GIVEN** 对局进行中，战士已获得 2 个随机技能, **WHEN** 查询战士的基础移动属性, **THEN** MaxGroundSpeed 等属性值与注入时完全一致（随机技能不修改职业基础属性）

### 性能

- **GIVEN** 3 个职业配置数据已加载, **THEN** 查询任意职业数据的耗时 < 0.01ms（纯内存读取）

> `qa-lead` not consulted — Lean mode. Review manually before production.

## Open Questions

[To be designed]
