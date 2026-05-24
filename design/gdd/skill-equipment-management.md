# 技能装备管理 (Skill Equipment Management)

> **Status**: In Design
> **Author**: SeanDuan + agents
> **Last Updated**: 2026-05-23
> **Implements Pillar**: Pillar 1: 秒学秒玩, Pillar 2: 每局都是新故事, Pillar 3: 高手菜鸟都开心

## Overview

技能装备管理系统是职业对决肉鸽循环的"装备执行层"，负责将技能抽取系统的随机抽取结果转化为战斗中可实际使用的技能能力。核心职责链路：接收 `OnSkillDrawn(CharacterId, SkillData)` 事件 → 将 SkillData 分配到技能槽位 → 通过 `ICombatStateProvider.RegisterState()` 将技能的帧数据和取消表注册到格斗状态机 → 映射手柄按钮到技能槽位 → 在战斗中响应玩家输入触发技能的战斗状态。系统同时维护每个技能槽的运行时状态（装备状态、可用性、执行追踪）。每局开始时所有槽位清空，随着专注值解锁逐步填满——从"空手开局"到"满载技能"的成长感是这个系统的核心体验贡献。没有它，技能抽取只是"开了一个盲盒"但没有使用途径——装备管理是把"抽到的纸片"变成"手中的武器"的转化器。对玩家而言，技能装备管理体现为 HUD 上的技能槽位和手柄上的技能按钮——抽到什么技能，按下对应按钮就能在战斗中使用，Pillar 1（秒学秒玩）要求这个过程直觉到不需要教学。

## Player Fantasy

**核心幻想：「我的角色在对战中不断进化，每次解锁都让我变成一个不同的战士」**

玩家应该感觉技能装备管理是角色成长的"武器库"——开局只有基础攻击，像一块白板。第一次解锁时，技能图标弹入 HUD 槽位，手柄按钮突然有了新的意义——"我有一个新招式了！"。按下技能按钮的那一刻，角色释放出一个与基础攻击完全不同的攻击——可能是大范围的回旋踢、追踪的火球、或者瞬移突刺——玩家立即感受到"我的战斗方式变了"。装备管理的幻想不是"我变强了"，而是**"我变成了不同的战士"**。第一个解锁的技能定义了接下来 30 秒的战斗身份——抽到火球术？你变成了远程骚扰者。抽到盾击？你变成了压制型近战。第二个、第三个技能叠加后，角色变成了独一无二的混搭战士——这就是 Pillar 2（每局都是新故事）的核心体验。

**关键情感时刻**：
- **第一个技能装备** — HUD 空槽位突然亮起，按钮图标出现，"我有一个新招式了"
- **第一次使用技能命中对手** — "这招好强！"或"这招原来是这样用的"
- **技能组合发现** — "等等，技能 A 的恢复帧可以取消到技能 B？"从偶然到精通
- **满槽状态** — 所有技能槽全满，角色从一个白板变成了拥有独特"招式组合"的战士
- **技能被打断** — "蓄力重击被打断了，下一次要更谨慎地选时机"

**支柱对齐**：
- 服务 **Pillar 1: 秒学秒玩** — 按钮映射直觉，按技能按钮就用技能，不需要教学
- 服务 **Pillar 2: 每局都是新故事** — 不同的技能组合 = 不同的战斗身份
- 服务 **Pillar 3: 高手菜鸟都开心** — 菜鸟享受"按按钮就放技能"，高手享受取消窗口和技能组合的深度

> `creative-director` 未咨询 — Lean 模式。上线前请手动审查。

## Detailed Design

### Core Rules

**1. 技能槽系统**

每个角色拥有 4 个技能槽位（SkillSlot），按装备顺序编号，双输入映射：

| 槽位 | 键盘 | 手柄 | 装备顺序 |
|-------|------|------|---------|
| Slot 1 | 1 | RB | 第 1 次抽取 |
| Slot 2 | 2 | RT | 第 2 次抽取 |
| Slot 3 | 3 | LB | 第 3 次抽取 |
| Slot 4 | 4 | LT | 第 4 次抽取 |

- 每局开始时所有槽位为空（Empty）
- 槽位数 = MaxSkillsPerMatch（4），不会出现满槽溢出
- 槽位按固定顺序填入（1 → 2 → 3 → 4），玩家无需选择装备位置
- 输入映射可在 Input System 中配置重映射

**2. 装备流程**

收到 `OnSkillDrawn(CharacterId, SkillData)` 后：

1. 找到第一个空槽位（1 → 2 → 3 → 4 顺序）
2. 从 SkillData 创建运行时 SkillInstance（槽位索引 + SkillData 只读引用）
3. 通过 `ICombatStateProvider.RegisterState(stateDefinition)` 注册到格斗状态机：
   - StateName = SkillData.SkillId
   - StartupFrames / ActiveFrames / RecoveryFrames = SkillData.AttackData 对应值
   - CancelTable = SkillData.AttackData.CancelTable
   - InputMapping = 对应槽位的输入绑定（1/RB、2/RT、3/LB、4/LT）
4. 发出 `OnSkillEquipped(CharacterId, SlotIndex, SkillData)` 通知下游系统

**3. 技能激活**

玩家按下技能键（1/2/3/4 或 RB/RT/LB/LT）时：

1. 根据按键确定目标技能槽位
2. 检查目标槽位是否已装备——未装备则输入被忽略
3. 已装备：将技能输入写入格斗状态机的输入缓冲
4. 格斗状态机按正常优先级处理：
   - Idle → 立即执行技能
   - Attacking.Recovery 且 CancelTable 允许 → 取消到技能
   - HitStun/Knockback → 输入缓冲等待可执行帧
5. 技能输入优先级：技能攻击 > 基础攻击 > 闪避/跳跃（格斗状态机保证）

**4. 技能执行**

- 技能使用与基础攻击完全相同的帧结构：Startup → Active → Recovery
- AttackData 格式与基础招式一致，攻击系统无需区分"职业招式"和"技能招式"
- Recovery 阶段可按 CancelTable 取消到其他状态
- 可被 HitStun/Knockback 强制取消（与基础攻击相同）
- 被打断后无额外惩罚，Recovery 结束后立即可用
- 无冷却机制

**5. 状态注册规则**

- 使用 SkillId 作为 FSM 中的唯一状态名
- 每次抽取的技能不同（抽取系统保证），不会注册重复状态名
- 取消优先级（格斗状态机保证）：技能攻击 > 基础攻击 > 闪避/跳跃
- 投射物技能的 IsProjectile、ProjectileSpeed、ProjectileLifetime 由攻击系统统一处理

**6. 对局生命周期**

- **OnRoundStart（回合间）**：不重置——已装备技能保留（跨局成长弧线，参见 ADR-0009 §5）
- **OnMatchEnd（对局间）**：清空所有槽位，通过 `ICombatStateProvider.DeregisterAllSkillStates()` 从 FSM 移除所有技能状态
- 对局间重置后角色回到只有基础招式状态
- 注意：`ISkillEquipmentManager` 无 `ResetForNewRound` 方法，仅提供 `ResetForNewMatch`（参见 ADR-0009 §5 接口定义）

### States and Transitions

**技能槽状态**：

| 槽位状态 | 触发 | 行为 |
|---------|------|------|
| Empty | 初始 / OnMatchEnd（对局间重置） | 技能键输入忽略 |
| Equipped | OnSkillDrawn 装备完成 | FSM 状态已注册，按键映射激活 |
| Empty | OnMatchEnd 重置 | FSM 状态注销，按键映射移除 |

**装备管理器状态**（系统级）：

| 状态 | 触发 | 行为 |
|------|------|------|
| Waiting | 初始 / OnMatchEnd | 等待 OnSkillDrawn |
| Equipping | 收到 OnSkillDrawn | 执行装备流程 |
| Waiting | 装备完成 | 等待下一个 OnSkillDrawn 或对局结束 |

### Interactions with Other Systems

| 系统 | 方向 | 类型 | 接口描述 |
|------|------|------|---------|
| 技能抽取系统 | 上游（硬） | 事件 | `OnSkillDrawn(CharacterId, SkillData)` 触发装备 |
| 技能数据库 | 上游（硬） | 数据引用 | SkillData 只读引用，获取 AttackData |
| 格斗状态机 | 下游（硬） | 注册 | `RegisterState(stateDefinition)` 注册技能战斗状态 |
| 格斗状态机 | 上游（查询） | 查询 | `GetCurrentState()`, `CanAcceptInput()` 可用性检查 |
| 攻击系统 | 下游（间接） | 执行 | 技能 AttackData 通过 FSM 执行，攻击系统统一处理命中 |
| 战斗HUD | 下游（硬） | 事件 | `OnSkillEquipped` 更新技能图标和槽位 |
| 能量视觉系统 | 下游（软） | 事件 | `OnSkillEquipped` 触发装备特效 |
| 技能协同系统 | 下游（软） | 查询 | `GetEquippedSkills(CharacterId)` 协同检测 |
| 对局管理系统 | 上游（软） | 事件 | `OnRoundStart` / `OnRoundEnd` 触发重置 |

## Formulas

**单位系统**: 帧数以 60Hz 固定时间步为基准。槽位索引从 1 开始。

### 1. 槽位分配

`SlotIndex = NextEmptySlot(SkillSlots)`

遍历 SkillSlots[1..4]，返回第一个状态为 Empty 的槽位索引。

| 变量 | 符号 | 类型 | 范围 | 说明 |
|------|------|------|------|------|
| 技能槽数组 | SkillSlots | SkillSlot[4] | — | 角色的 4 个技能槽位 |
| 目标槽位 | SlotIndex | int | 1–4 | 装备目标，第一个 Empty 槽 |

**Output Range:** 1 到 4。如果所有槽位都已装备（不应发生，因槽位数 = MaxSkillsPerMatch），返回 0 表示无可用槽位。
**Example:** 第 1 次抽取 → SlotIndex=1。第 3 次抽取 → SlotIndex=3。第 5 次抽取 → 不可能（MaxSkillsPerMatch=4 限制）。

### 2. 装备计数

`EquippedCount = Count(slot where slot.State == Equipped)`

| 变量 | 符号 | 类型 | 范围 | 说明 |
|------|------|------|------|------|
| 已装备数 | EquippedCount | int | 0–4 | 当前已装备的技能数量 |

**Output Range:** 0 到 4。每局结束时重置为 0。
**Example:** 开局 0，第 1 次抽取后 1，第 4 次抽取后 4。

### 3. 技能可用性判定

`CanUseSkill = (SkillSlots[SlotIndex].State == Equipped) AND (CombatFSM.CanAcceptInput() == true OR InputBuffer.HasValidBufferedInput())`

| 变量 | 符号 | 类型 | 范围 | 说明 |
|------|------|------|------|------|
| 目标槽位装备状态 | Slot.State | enum | {Empty, Equipped} | 槽位是否已装备技能 |
| FSM 可接受输入 | CanAcceptInput | bool | — | 格斗状态机当前是否可接受新输入 |
| 缓冲有效输入 | HasValidBufferedInput | bool | — | 输入缓冲中是否有未过期的技能输入 |
| 技能可用 | CanUseSkill | bool | — | 技能是否可以在本帧激活 |

**Output Range:** true/false。Empty 槽位永远返回 false。
**Example:** Slot 1 已装备，FSM 处于 Idle → CanUseSkill=true。Slot 2 已装备，FSM 处于 Attacking.Startup → CanUseSkill=false（输入缓冲等待 Recovery）。

注：技能的帧数据公式（Startup/Active/Recovery 推进、取消窗口等）由格斗状态机 GDD 定义，本系统不重新定义。本系统仅负责"将技能注册到 FSM"和"判断技能槽是否可用"。

## Edge Cases

**装备流程**:
- **如果 OnSkillDrawn 到达时所有槽位已满（不应发生）**: 返回 0 表示无可用槽位，不执行装备。记录警告。上游专注值系统保证 MaxSkillsPerMatch 限制，此情况仅在 bug 时出现。
- **如果 OnSkillDrawn 传入的 SkillData 为 null**: 装备流程跳过该技能，不填充任何槽位。记录错误。抽取系统应在发送事件前验证 SkillData 非空。
- **如果 FSM RegisterState 调用失败（如状态名重复）**: 装备失败，槽位保持 Empty，发出装备失败事件。不消耗抽取次数——抽取系统已将技能加入 AlreadyDrawnSkillIds，但装备未完成意味着该技能不可用。
- **如果装备期间对局结束（OnRoundStart 到达）**: 中断装备流程，执行重置。未完成的装备被丢弃。

**技能激活**:
- **如果玩家按下空槽位的技能键**: 输入被忽略，不写入输入缓冲。空槽位的输入不占用缓冲空间。
- **如果玩家在 HitStun 中按下技能键**: 输入写入输入缓冲，等待 HitStun 结束后执行。缓冲有效期 = InputBufferFrames（8 帧）。如果 HitStun 超过 8 帧，输入过期被丢弃。
- **如果玩家在 Knockback 不可操作期按下技能键**: 同 HitStun——输入缓冲等待，超时丢弃。
- **如果玩家同时按下多个技能键（同一帧）**: 按优先级处理：Slot 1 > Slot 2 > Slot 3 > Slot 4。只有优先级最高的技能被接受。格斗状态机一次只能执行一个攻击状态。
- **如果技能正在执行（Startup/Active/Recovery）中玩家再次按同一技能键**: 输入写入缓冲。如果当前技能的 Recovery CancelTable 允许取消到自身，则在 Recovery 阶段取消并重新执行。如果 CancelTable 不允许自身取消，输入在 Recovery 结束后或缓冲过期后丢弃。

**状态注册**:
- **如果两个不同角色装备了同一个 SkillData（如火球术）**: 各自独立注册，使用同一个 SkillId。格斗状态机按角色实例独立管理——每个角色有自己的 FSM 实例。SkillId 在同一个 FSM 内唯一，跨 FSM 允许重复。
- **如果技能的 CancelTable 引用了基础攻击的状态名（如 "Jab"）**: 合法——技能可以取消到基础攻击。CancelTable 的目标由攻击系统定义。
- **如果技能的 CancelTable 为空（如 Meteor 陨石坠落）**: Recovery 阶段不可取消到任何状态，必须等待 Recovery 自然结束。这是设计意图（完全承诺型攻击）。

**对局重置**:
- **如果 OnMatchEnd 到达时有技能正在执行（FSM 处于 Attacking 状态）**: FSM 的重置优先于技能执行。所有注册状态立即注销，FSM 强制回到 Idle。
- **如果装备管理器在 Equipping 状态时收到 OnMatchEnd**: 中断装备，执行重置。未完成的装备被丢弃，对应的 SkillData 不再有效。
- **如果 OnRoundStart 到达时（回合间重置）**: 不影响已装备技能——技能跨局保留。仅专注值归零（由 FocusSystem 处理），已装备技能继续可用。

**输入冲突**:
- **如果技能键（1/2/3/4 或 RB/RT/LB/LT）与其他系统的输入冲突**: 输入映射由 Input System 统一管理。技能键在技能装备前不产生效果（空槽位忽略输入），装备后激活。不需要额外的"输入模式切换"。
- **如果角色在 KO 状态下有已装备技能**: 技能槽位保持不变。KO 由对局管理系统处理，技能装备不受 KO 影响。重生后技能仍可用（如果对局模式允许重生）。

## Dependencies

| 依赖系统 | 方向 | 类型 | 数据接口 | GDD 状态 |
|---------|------|------|---------|---------|
| 技能抽取系统 | 上游（硬） | 事件 | `OnSkillDrawn(CharacterId, SkillData)` — 触发装备流程 | Designed |
| 技能数据库 | 上游（硬） | 数据引用 | SkillData 包含完整 AttackData、CancelTable、Icon、VFXColor | Designed |
| 格斗状态机 | 下游（硬） | 注册 | `ICombatStateProvider.RegisterState(stateDefinition)` — 注册技能战斗状态；`DeregisterAllSkillStates()` — 对局重置 | Designed |
| 格斗状态机 | 上游（查询） | 查询 | `GetCurrentState()`, `CanAcceptInput()`, `GetCurrentAttackPhase()` — 技能可用性和阶段判定 | Designed |
| 攻击系统 | 下游（间接） | 执行 | 技能的 AttackData 通过 FSM 消费，攻击系统统一处理 hitbox/hurtbox 判定 | Designed |
| 专注值系统 | 上游（间接） | 约束 | MaxSkillsPerMatch=4 约束槽位数，通过技能抽取系统间接关联 | Designed |
| 战斗HUD | 下游（硬） | 事件 | `OnSkillEquipped(CharacterId, SlotIndex, SkillData)` — 更新技能槽位图标 | Not Started |
| 能量视觉系统 | 下游（软） | 事件 | `OnSkillEquipped` — 触发装备特效；技能执行时的视觉反馈 | Not Started |
| 技能附属物系统 | 下游（软） | 查询 | `GetEquippedSkills(CharacterId)` — 查询已装备技能用于附属物显示 | Not Started |
| 音效系统 | 下游（软） | 事件 | 技能激活/命中/被打断的音效事件（通过 FSM 和攻击系统间接触发） | Not Started |
| 技能协同系统 | 下游（软） | 查询 | `GetEquippedSkills(CharacterId)` — 查询已装备技能列表用于协同检测 | Not Started |
| 对局管理系统 | 上游（软） | 事件 | `OnMatchEnd` — 触发槽位和状态重置（OnRoundStart 不重置，技能跨局保留） | Not Started |
| 职业系统 | 上游（间接） | 数据 | 通过 SkillData.Tags 间接关联，装备管理不直接查询职业 | Designed |

**向上提供的接口契约**:
- `ISkillEquipmentManager` 接口: 技能装备管理和查询入口
- `OnSkillEquipped(CharacterId, SlotIndex, SkillData)`: 技能装备完成事件
- `OnSkillUnequipped(CharacterId, SlotIndex)`: 技能卸载事件（仅对局重置时触发）
- `GetEquippedSkills(CharacterId)`: 返回已装备技能列表（SkillData[]，按槽位顺序）
- `GetSkillSlot(CharacterId, SlotIndex)`: 查询指定槽位状态
- `GetEquippedCount(CharacterId)`: 查询已装备技能数量
- `ResetEquipment(CharacterId)`: 重置角色所有装备和注册状态
- `ResetAll()`: 重置所有角色装备

**双向一致性检查**:
- 技能抽取系统 GDD: "技能装备管理 | 下游（硬依赖）| OnSkillDrawn(CharacterId, SkillData) — 传递抽中的技能数据，装备管理负责实例化" ✓ 一致
- 格斗状态机 GDD: "技能装备管理 | 技能 → FSM | ICombatStateProvider.RegisterState(stateDefinition) — 注入新技能状态" ✓ 一致
- 技能数据库 GDD: "技能装备管理 | 技能DB → 装备 | 提供 GetSkillById(SkillId) 获取技能定义" ✓ 一致

## Tuning Knobs

**本系统定义的旋钮**:

| 旋钮名 | 默认值 | 安全范围 | 调高效果 | 调低效果 |
|--------|--------|---------|---------|---------|
| SkillSlotCount | 4 | 2–6 | 更多同时可用技能，战斗更复杂 | 更少技能，更简洁但成长感弱 |

**引用的上游旋钮（不重复定义）**:
- `MaxSkillsPerMatch` — 每局最大抽取/装备次数，由专注值系统 GDD 定义。**约束：SkillSlotCount 必须等于 MaxSkillsPerMatch**，否则会出现槽位不够或槽位浪费。
- `InputBufferFrames` — 格斗状态机的输入缓冲窗口，技能输入使用同一缓冲（8 帧）。
- 每个技能的 AttackData 字段（BaseDamage, BaseKnockback, StartupFrames 等）由技能数据库 GDD 定义。
- 每个技能的 CancelTable 由技能数据库 GDD 定义。

**旋钮交互警告**:
- `SkillSlotCount` 与 `MaxSkillsPerMatch` 必须保持一致——改变一个必须同步改变另一个
- 技能输入使用格斗状态机的 `InputBufferFrames`——不独立定义缓冲窗口，避免两个不同的缓冲值造成行为不一致
- 输入映射（键盘 1-4 / 手柄 RB/RT/LB/LT）通过 Unity Input System 配置，不在 GDD 中硬编码——重映射是引擎层功能

## Visual/Audio Requirements

**装备瞬间的视觉反馈**:

当 OnSkillEquipped 触发时：
- 技能图标弹入 HUD 对应槽位（缩放弹入动画 < 0.3 秒）
- 稀有度对应颜色的光晕闪烁槽位边框（Common: 蓝, Rare: 紫, Epic: 金）
- 角色轮廓短暂爆发稀有度颜色光芒（< 0.5 秒），与抽取系统的解锁特效协调

**技能激活的视觉反馈**:

当技能被激活（FSM 进入技能的 Startup 阶段）时：
- HUD 对应槽位图标高亮 + 边框加粗
- 角色轮廓发出技能对应颜色的光芒（Startup 期间线性增强，与格斗状态机 GDD 一致）
- 投射物技能：角色手部/施法点生成能量聚集特效（Startup），投射物在 Active 帧发射

**技能被打断的视觉反馈**:

当技能在执行中被 HitStun 打断时：
- HUD 槽位短暂闪红（< 0.2 秒）表示被打断
- 角色轮廓光效瞬间消失（与格斗状态机的受击闪烁协调）

**音频事件**:

| 音频事件 | 触发时机 | 描述 |
|---------|---------|------|
| `OnSkillEquipped` | 技能装备完成 | "装备完成"音效——清脆的金属声 + 稀有度音调 |
| `OnSkillActivated` | 技能键按下且技能开始执行 | "技能启动"音效——与技能本身的蓄力音效衔接 |
| `OnSkillInterrupted` | 技能被 HitStun 打断 | "被打断"音效——短促的断裂声 |

## UI Requirements

**技能槽位 HUD（战斗界面组件）**:
- 形态：屏幕底部（或角色下方）的 4 个技能槽位
- 布局：横向排列，按 Slot 1-4 从左到右
- 每个槽位内容：技能图标（SkillData.Icon）、稀有度边框颜色
- 空槽位：灰色轮廓 + 对应按键提示（1/2/3/4 或 RB/RT/LB/LT）
- 已装备槽位：技能图标 + 稀有度边框 + 按键提示
- 当前执行中的技能：槽位高亮 + 脉动动画
- 按键提示可关闭（设置选项）

**手柄导航**: 技能槽位是纯信息显示，无需手柄方向键导航。技能使用通过直接按键触发。

> **UX Flag — 技能装备管理**: 此系统有 UI 需求（技能槽位 HUD）。在 Pre-Production 阶段，运行 `/ux-design` 为技能槽位 HUD 创建 UX 规范。引用 `design/ux/skill-slots-hud.md`。

## Acceptance Criteria

### 装备流程

- **GIVEN** 角色开局状态（所有槽位 Empty），**WHEN** 收到 OnSkillDrawn(P1, SkillData[Fireball])，**THEN** Slot 1 装备 Fireball，FSM 注册状态 "skill_fireball"，发出 OnSkillEquipped(P1, 1, Fireball)
- **GIVEN** Slot 1 已装备 Fireball，**WHEN** 收到 OnSkillDrawn(P1, SkillData[ShieldBash])，**THEN** Slot 2 装备 ShieldBash，FSM 注册状态 "skill_shield-bash"
- **GIVEN** Slot 1-4 全部已装备，**WHEN** 收到 OnSkillDrawn（不应发生），**THEN** 返回 SlotIndex=0，不执行装备，记录警告
- **GIVEN** 收到 OnSkillDrawn(P1, null)，**THEN** 装备跳过，所有槽位保持不变，记录错误

### 技能激活

- **GIVEN** Slot 1 已装备 Fireball 且 FSM 在 Idle，**WHEN** 玩家按下技能键 1（或 RB），**THEN** 角色进入 Fireball 的 Startup 帧
- **GIVEN** Slot 2 未装备（Empty），**WHEN** 玩家按下技能键 2（或 RT），**THEN** 输入被忽略，角色不执行任何操作
- **GIVEN** Slot 1 已装备 Fireball 且 FSM 在 Attacking.Recovery（CancelTable 允许技能），**WHEN** 玩家按下技能键 1，**THEN** 取消当前攻击到 Fireball 的 Startup
- **GIVEN** Slot 1 已装备且 FSM 在 HitStun（剩余 5 帧），**WHEN** 玩家按下技能键 1，**THEN** 输入写入缓冲，等待 HitStun 结束后执行
- **GIVEN** Slot 1 已装备且 FSM 在 HitStun（剩余 10 帧），**WHEN** 玩家按下技能键 1（InputBufferFrames=8），**THEN** 输入在 HitStun 结束前过期，不执行

### 技能执行

- **GIVEN** 角色正在执行 Fireball（Startup 阶段），**WHEN** 被对手击中，**THEN** 技能被打断（hitbox 关闭），进入 HitStun
- **GIVEN** Fireball 被打断后，**WHEN** 角色从 HitStun 恢复到 Idle 且玩家再次按技能键 1，**THEN** Fireball 可立即再次使用（无额外惩罚）
- **GIVEN** 角色正在执行 Meteor（CancelTable 为空，Recovery 不可取消），**WHEN** Recovery 阶段玩家按技能键，**THEN** 输入被缓冲，Recovery 自然结束后处理
- **GIVEN** 角色执行技能的 Recovery 阶段（CancelTable 允许），**WHEN** 玩家按基础攻击键，**THEN** 技能被取消到基础攻击（优先级：技能 > 基础攻击）

### 对局重置

- **GIVEN** P1 已装备 3 个技能（Slot 1-3），**WHEN** 收到 OnMatchEnd（对局间重置），**THEN** 所有槽位清空，FSM 注销所有 3 个技能状态，发出 OnSkillUnequipped(P1, 1/2/3)
- **GIVEN** P1 已装备 3 个技能（Slot 1-3），**WHEN** 收到 OnRoundStart（回合间），**THEN** 槽位不变，技能保留，FSM 状态保留
- **GIVEN** P1 正在执行技能（FSM 在 Attacking 状态），**WHEN** 收到 OnMatchEnd，**THEN** FSM 强制回到 Idle，技能状态注销

### 双人独立性

- **GIVEN** P1 和 P2 都装备了 Fireball，**WHEN** P1 使用 Fireball，**THEN** P2 的 Fireball 不受影响（各自 FSM 独立）
- **GIVEN** P1 装备了 2 个技能，P2 装备了 3 个技能，**WHEN** 各自独立查询，**THEN** P1 EquippedCount=2, P2 EquippedCount=3

### 性能

- **GIVEN** 2 人对战、每人 4 个技能已装备，**WHEN** 执行装备查询或激活判定，**THEN** 总处理时间 < 0.1ms（纯数组遍历+枚举比较）

## Open Questions

1. **技能槽位的 HUD 精确位置和尺寸** — 当前定义了功能需求和布局原则，但槽位的具体尺寸、间距、是否跟随角色移动还是固定在屏幕底部，需要与 UX 规范协调。（Owner: UX 设计师，里程碑: UX 规范创建）

2. **技能键输入与基础攻击输入的精确冲突处理** — 当前设计技能键独立于攻击键，但如果玩家同时按攻击键和技能键，格斗状态机如何处理？需要与格斗状态机的输入缓冲系统协调。（Owner: 系统设计师，里程碑: 原型验证）

3. **SkillSlotCount 与 MaxSkillsPerMatch 的同步机制** — 当前设计两者必须相等。如果后续想支持"槽位数 < 抽取上限"（如 6 次抽取但只保留最后 4 个），需要重新设计替换/选择机制。（Owner: 游戏设计师，里程碑: Vertical Slice 阶段评估）
