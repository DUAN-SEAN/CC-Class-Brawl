# 攻击系统 (Attack System)

> **Status**: Designed
> **Author**: SeanDuan + agents
> **Last Updated**: 2026-05-23
> **Implements Pillar**: Pillar 1: 秒学秒玩, Pillar 3: 高手菜鸟都开心, Pillar 4: 快速战斗

## Overview

攻击系统是职业对决的战斗执行层，负责将玩家的攻击输入转化为实际的战斗行为——从读取职业的基础招式数据（或技能系统注入的技能招式数据），到创建和定位 hitbox，到管理攻击的三阶段帧生命周期（Startup → Active → Recovery），到执行命中时的 hitstop 效果和投射物的生成与飞行。它是格斗状态机和碰撞判定系统之间的桥梁：格斗状态机决定"是否允许攻击"，攻击系统决定"攻击如何执行"，碰撞判定系统决定"hitbox 是否命中了 hurtbox"。对于玩家而言，攻击系统定义了每一次攻击的"打击感"——hitbox 的大小和位置决定了攻击的范围和判定，hitstop 的帧数决定了命中瞬间的"卡顿感"，投射物的速度和方向决定了法师的远程战斗手感。攻击系统同时消费职业系统提供的 AttackData（基础招式）和技能系统注入的 AttackData（随机技能招式），以统一的方式处理所有攻击类型。没有攻击系统，格斗状态机只有空壳状态转换而无实际战斗行为，碰撞判定系统没有 hitbox 可以检测。

## Player Fantasy

**核心幻想：「每一击都有分量」**

玩家应该感觉攻击不是"按了按钮然后发生了什么"，而是一个有重量、有节奏、有结果的物理动作。轻击是轻击——快速但伤害低；重击是重击——慢但命中时有满足的"砰"感。命中的瞬间，游戏短暂"定格"（hitstop），让玩家品尝这一击的冲击。挥空则是一个脆弱的瞬间——恢复帧中无法行动，对手可以惩罚。

**关键情感时刻**：
- **命中满足感** — hitstop 让命中的瞬间被"放大"，时间仿佛暂停了一帧，然后对手被击退——这种"砰"的感觉是格斗游戏的灵魂
- **挥空的恐惧** — 启动慢的攻击被打断、或者攻击落空后的长恢复，是"我不该在那个位置出这招"的学习时刻
- **取消的精确感** — 在恢复帧中精确按下下一次攻击，连招流畅衔接——时机对了是"我太强了"，时机错了是"差一点"
- **投射物的控制感** — 法师玩家发射投射物后的"远程压制"感——不用贴脸就能威胁对手

**支柱对齐**：
- 服务 **Pillar 1: 秒学秒玩** — 按攻击键就攻击，无复杂输入，帧数据直觉化（快角色出招快，重角色出招重）
- 服务 **Pillar 3: 高手菜鸟都开心** — 新手可以狂按攻击键享受命中反馈，高手精确利用取消窗口和帧优势打出连招
- 服务 **Pillar 4: 快速战斗** — hitstop 时间短（3-6 帧），不中断战斗节奏

> `creative-director` not consulted — Lean mode. Review manually before production.

## Detailed Design

### Core Rules

**1. 攻击数据管线**

攻击系统统一处理两种来源的 AttackData：
- **基础招式**：来自职业系统（ClassData.AttackData[]），按 GroundAttack/AirAttack/DashAttack 索引
- **技能招式**：来自技能装备管理系统，通过 `IAttackDataProvider` 接口注入

攻击数据结构（`AttackData`）：

| 字段 | 类型 | 来源 | 说明 |
|------|------|------|------|
| AttackId | string | 职业/技能 | 唯一标识符，用于取消表和日志 |
| StartupFrames | int | 职业/技能 | 启动帧数 |
| ActiveFrames | int | 职业/技能 | 活跃帧数 |
| RecoveryFrames | int | 职业/技能 | 恢复帧数 |
| HitStunFrames | int | 职业/技能 | 命中后对手硬直帧数 |
| BaseDamage | float | 职业/技能 | 基础伤害值 |
| BaseKnockback | float | 职业/技能 | 基础击退力度 |
| IsProjectile | bool | 职业/技能 | 是否为投射物 |
| HitstopFrames | int | 职业/技能 | 命中时双方冻结帧数（MVP: 3-6） |
| HitboxOffset | Vector2 | 职业/技能 | hitbox 中心相对角色位置的偏移 |
| HitboxSize | Vector2 | 职业/技能 | hitbox 的宽度和高度 |
| ProjectileSpeed | float | 仅投射物 | 投射物飞行速度（u/s） |
| ProjectileLifetime | int | 仅投射物 | 投射物存活帧数 |
| CancelTable | CancelEntry[] | 职业/技能 | 可取消目标列表 |

**2. 攻击类型解析**

当格斗状态机接受一个攻击输入时，攻击系统查询 3C 系统确定攻击类型：

| 3C MovementState | 攻击类型 | 使用的 AttackData |
|-----------------|---------|-----------------|
| Idle, Running | 地面攻击 | GroundAttack |
| Jumping, Falling, FastFalling | 空中攻击 | AirAttack |
| Dashing | 冲刺攻击 | DashAttack |

优先级：技能招式 > 基础招式。如果技能系统激活了一个覆盖当前攻击类型的技能，使用技能的 AttackData。

**3. Hitbox 生命周期**

每个攻击创建一个 hitbox（碰撞检测区域），其生命周期由攻击的帧阶段驱动：

| 阶段 | hitbox 状态 | 行为 |
|------|-----------|------|
| Startup | 不存在 | hitbox 尚未创建，攻击无判定 |
| Active | 已激活 | hitbox 创建并定位，可被碰撞判定系统检测 |
| Recovery | 已销毁 | hitbox 移除，攻击不再有判定 |
| 攻击结束/被取消 | 已销毁 | hitbox 移除 |

Hitbox 定位规则：
- **近战攻击**：hitbox 位置 = 角色位置 + HitboxOffset × 面朝方向。X 分量随面朝方向镜像。
- **投射物攻击**：Startup 阶段无 hitbox。进入 Active 阶段时，在角色位置 + HitboxOffset 处生成投射物实体。投射物拥有独立的 hitbox，飞行后脱离角色。

**4. 多次命中防护**

同一攻击（同一 AttackId）对同一目标只能命中一次。实现方式：
- 攻击系统维护一个 `HitTargets` 集合（每攻击实例）
- 命中时将目标 ID 加入集合
- 碰撞判定系统检测到 hitbox/hurtbox 重叠时，攻击系统检查目标是否已在集合中
- 已在集合中的目标不再触发命中事件

**5. Hitstop 执行**

命中时，攻击系统触发 hitstop：
1. 通知格斗状态机暂停帧计数（命中者和被命中者双方）
2. hitstop 持续 `HitstopFrames` 帧
3. hitstop 期间：双方角色动画冻结，hitbox 保持活跃（命中者的 Active 阶段不推进）
4. hitstop 结束后：格斗状态机恢复帧计数，攻击阶段正常推进

**6. 投射物系统**

投射物是独立的游戏对象，生命周期由攻击系统管理：
1. **生成**：攻击进入 Active 阶段时，在 `角色位置 + HitboxOffset × 面朝方向` 处生成投射物
2. **飞行**：投射物以 `ProjectileSpeed` 沿面朝方向水平飞行
3. **碰撞**：投射物拥有自己的 hitbox，由碰撞判定系统检测命中
4. **销毁条件**（满足任一即销毁）：
   - 帧数超过 `ProjectileLifetime`
   - 命中了一个 hurtbox（角色或护盾）
   - 碰到场地碰撞体（墙壁或实心平台）
5. **投射物不拥有者死亡**：投射物生成后独立于攻击者——攻击者被击中不影响已发射的投射物

**7. 取消表评估**

取消表定义了 Recovery 阶段可以取消到哪些状态。格斗状态机负责检查当前帧是否在取消窗口内，攻击系统提供取消表数据。

每个 CancelEntry 包含：
- `TargetState`: 目标状态（Attack/Dash/Jump/Skill）
- `InputRequired`: 需要的输入（Attack/Dash/Jump/SkillButton）
- `Condition`: 额外条件（如"仅在地面"），可选

MVP 默认取消规则：Recovery 可取消到任意攻击或 Dash。

### States and Transitions

攻击系统不维护独立的状态机。攻击的生命周期由格斗状态机驱动：

| 格斗状态机状态 | 攻击系统行为 |
|-------------|------------|
| 进入 Attacking.Startup | 读取 AttackData，准备攻击 |
| Attacking.Startup → Active | 创建 hitbox（近战）或生成投射物（投射物攻击） |
| Attacking.Active 期间 | hitbox 保持活跃，等待命中事件 |
| Attacking.Active → Recovery | 销毁近战 hitbox |
| Attacking.Recovery → Idle | 清理攻击实例，重置 HitTargets |
| 被取消（HitStun/Knockback） | 销毁 hitbox，清理攻击实例 |
| hitstop 期间 | 冻结帧计数，hitbox 保持不变 |

### Interactions with Other Systems

| 系统 | 数据流方向 | 接口描述 |
|------|-----------|---------|
| 格斗状态机 | FSM → 攻击 | 通知当前攻击阶段（Startup/Active/Recovery）和当前帧数 |
| 格斗状态机 | 攻击 → FSM | 提供 CancelTable 供 FSM 评估取消；通知 hitstop 开始/结束 |
| 碰撞判定系统 | 攻击 → 碰撞 | 提供 hitbox 引用（位置、大小）供碰撞检测 |
| 碰撞判定系统 | 碰撞 → 攻击 | 通知 hitbox 与 hurtbox 重叠事件（包含攻击者和被击者 ID） |
| 伤害计算系统 | 攻击 → 伤害 | 命中时传递 AttackData（BaseDamage, BaseKnockback, HitStunFrames） |
| 3C系统 | 3C → 攻击 | 提供 MovementState 和 IsGrounded() 用于攻击类型解析；提供 GetFacing() 用于 hitbox 定位 |
| 职业系统 | 职业 → 攻击 | 提供 3 个基础招式的 AttackData |
| 技能装备管理 | 技能 → 攻击 | 通过 IAttackDataProvider 注入技能招式 AttackData |
| 能量视觉系统 | 攻击 → 视觉 | 通知攻击阶段变化（用于轮廓光、特效触发） |

## Formulas

**单位系统**: 与 3C 系统一致，1 Unity 单位 = 64 像素，时间以 60Hz 帧为基准。

### 1. Hitbox 位置计算（近战）

`HitboxCenter = CharacterPosition + Vector2(HitboxOffset.x × FacingDirection, HitboxOffset.y)`

| Variable | Symbol | Type | Range | Description |
|----------|--------|------|-------|-------------|
| 角色位置 | CharacterPosition | Vector2 | 场地范围内 | 角色当前中心坐标 |
| Hitbox 偏移 | HitboxOffset | Vector2 | (-3, -2) 到 (3, 3) | hitbox 相对角色中心的偏移（AttackData 定义） |
| 面朝方向 | FacingDirection | int | {-1, 1} | 角色面朝方向（来自 3C GetFacing()） |
| Hitbox 中心 | HitboxCenter | Vector2 | 场地范围内 | hitbox 碰撞体中心坐标 |

**Output Range:** 场地范围内（可能超出——超出后 hitbox 无效，不命中任何目标）
**Example:** 角色在 (2.0, 0.5)，面朝右(1)，HitboxOffset = (0.8, 0.3) → HitboxCenter = (2.8, 0.8)

### 2. 投射物位移

`ProjectilePosition_new = ProjectilePosition + Vector2(ProjectileSpeed × FacingDirection × dt, 0)`

| Variable | Symbol | Type | Range | Description |
|----------|--------|------|-------|-------------|
| 投射物当前位置 | ProjectilePosition | Vector2 | 场地范围内 | 投射物当前坐标 |
| 投射物速度 | ProjectileSpeed | float | 5.0–15.0 u/s | 水平飞行速度 |
| 面朝方向 | FacingDirection | int | {-1, 1} | 生成时攻击者面朝方向（固定不变） |
| 时间步长 | dt | float | 1/60 s | FixedUpdate 时间步 |

**Output Range:** 沿水平方向持续移动直到销毁
**Example:** ProjectileSpeed = 8.0 u/s, 面朝右 → 每帧移动 8.0/60 = 0.133u，1秒后飞行 8.0u

### 3. 投射物存活判定

```
AgeFrames = CurrentFrame - SpawnFrame
IsAlive = (AgeFrames < ProjectileLifetime) AND NOT WasDestroyed
```

| Variable | Symbol | Type | Range | Description |
|----------|--------|------|-------|-------------|
| 当前帧 | CurrentFrame | int | 0+ | 全局帧编号 |
| 生成帧 | SpawnFrame | int | 0+ | 投射物生成的帧编号 |
| 存活帧数 | ProjectileLifetime | int | 30–120 | 投射物最大存活帧数（0.5s–2s） |
| 已被销毁 | WasDestroyed | bool | — | 因碰撞或其他原因已销毁 |
| 存活帧数 | AgeFrames | int | 0+ | 投射物已存活帧数 |

**Output Range:** 布尔值
**Example:** ProjectileLifetime = 60（1秒），SpawnFrame = 100。帧 159: Age=59 < 60, IsAlive=true。帧 160: Age=60, IsAlive=false

### 4. 多次命中检查

`HasAlreadyHit = HitTargets.Contains(TargetId)`

| Variable | Symbol | Type | Range | Description |
|----------|--------|------|-------|-------------|
| 已命中集合 | HitTargets | Set<int> | 0–4 个 ID | 本次攻击已命中的目标 ID 集合 |
| 目标 ID | TargetId | int | 1–4 | 碰撞检测到的角色 ID |
| 已命中过 | HasAlreadyHit | bool | — | 该目标是否已在本次攻击中命中 |

**Output Range:** 布尔值
**Example:** 投射物命中 P2 → HitTargets = {2}。同一投射物再碰到 P2 → HasAlreadyHit=true，忽略

## Edge Cases

**Hitbox 相关**:
- **如果 hitbox 在场地边界之外**: 正常创建。碰撞判定系统可能检测不到任何 hurtbox。攻击无效果但不报错。
- **如果角色在攻击 Active 阶段转身（面朝方向改变）**: hitbox 位置随面朝方向实时更新。如果角色被击退导致面朝翻转，hitbox 跟随移动。
- **如果攻击被取消时 hitbox 已激活**: hitbox 立即销毁，不再有判定。如果碰撞判定系统在同一帧已经检测到命中，以命中事件先到达为准（帧内先处理碰撞事件，再处理取消）。
- **如果同一帧 hitbox 同时命中多个目标**: 按目标 ID 排序依次处理。每个目标独立判定多次命中防护。

**Hitstop 相关**:
- **如果 HitstopFrames = 0**: 无 hitstop。命中瞬间无冻结，立即继续。这是合法设计（可用于极轻攻击）。
- **如果在 hitstop 期间角色被另一个攻击命中**: hitstop 不提供无敌——被命中方在 hitstop 中仍可被新攻击命中。新攻击的 hitstop 叠加（不替换），以更长的为准。
- **如果在 hitstop 期间攻击者被击中**: 攻击者的 hitstop 被打断，进入 HitStun/Knockback。被命中者的 hitstop 正常结束。

**投射物相关**:
- **如果投射物飞出场地 Blast Zone**: 不触发攻击者的 KO（投射物不属于角色位置）。投射物在超出可视范围后正常存活直到 ProjectileLifetime 耗尽。
- **如果同一帧投射物同时碰到 hurtbox 和场地碰撞体**: 命中 hurtbox 优先——投射物对目标造成伤害后销毁，不再检测场地碰撞。
- **如果投射物碰到另一个投射物**: 互相穿过，不交互。MVP 不支持投射物碰撞。
- **如果攻击者在投射物飞行期间被 KO**: 投射物继续飞行，不受攻击者状态影响。
- **如果投射物飞行方向上有穿越平台**: 投射物水平飞行，不受穿越平台碰撞影响（仅实心平台和墙壁阻挡投射物）。

**取消相关**:
- **如果取消目标需要条件（如"仅在地面"）但条件不满足**: 取消被拒绝，输入保留在缓冲中继续等待。
- **如果取消表中包含未注册的技能 ID**: 忽略该条目，不报错。技能可能已被移除。

**数据完整性**:
- **如果 AttackData 的 HitboxSize = (0, 0)**: 视为数据错误，攻击无判定（hitbox 大小为零永远不会碰到 hurtbox）。记录警告。
- **如果投射物攻击的 ProjectileSpeed = 0**: 投射物在原位不动，存活直到 ProjectileLifetime 耗尽。这是合法但奇怪的设计——可能是"放置型"技能。
- **如果投射物攻击的 ProjectileLifetime = 0**: 投射物生成后立即销毁。相当于无投射物。记录警告。

## Dependencies

| 依赖系统 | 方向 | 类型 | 数据接口 | GDD 状态 |
|---------|------|------|---------|---------|
| 格斗状态机 | 上游（硬依赖） | 事件驱动 | FSM 通知攻击阶段变化；攻击系统提供 CancelTable 和 hitstop 通知 | Designed |
| 3C系统 | 上游（硬依赖） | 查询 | MovementState, IsGrounded(), GetFacing(), GetPosition() 用于攻击类型解析和 hitbox 定位 | In Review |
| 职业系统 | 上游（硬依赖） | 数据注入 | 提供 3 个基础招式 AttackData（含帧数据、hitbox 参数、投射物参数） | Designed |
| 碰撞判定系统 | 下游（硬依赖） | 双向 | 攻击系统提供 hitbox 引用；碰撞系统通知命中事件 | 未设计 |
| 伤害计算系统 | 下游（硬依赖） | 数据传递 | 命中时传递 AttackData（BaseDamage, BaseKnockback, HitStunFrames） | 未设计 |
| 击退与击飞系统 | 下游（软依赖） | 数据传递 | 通过伤害计算系统间接关联（击退力度来自 AttackData.BaseKnockback） | 未设计 |
| 技能装备管理 | 上游（软依赖） | 数据注入 | 通过 IAttackDataProvider 注入技能 AttackData | 未设计 |
| 能量视觉系统 | 下游（软依赖） | 事件通知 | 攻击阶段变化事件（用于视觉反馈） | 未设计 |

**向上提供的接口契约**:
- `IAttackSystem` 接口: 攻击执行和命中的管理入口
- `GetCurrentAttack()`: 返回当前执行的 AttackData（或 null）
- `GetHitbox()`: 返回当前 hitbox 的位置和大小（供碰撞系统查询）
- `OnPhaseChanged(AttackPhase)`: 由 FSM 调用，驱动 hitbox 生命周期
- `OnHitDetected(TargetId)`: 由碰撞系统调用，处理命中逻辑
- 事件: `OnAttackHit(AttackData, TargetId)`, `OnAttackMiss()`, `OnHitstopStart(frames)`, `OnHitstopEnd()`

**双向一致性检查**:
- 格斗状态机 GDD: "攻击系统 | 攻击 → FSM | 提供 AttackData" ✓ 一致
- 格斗状态机 GDD: "碰撞判定系统 | 碰撞 → FSM | 命中事件 OnHitReceived" — 攻击系统在碰撞和 FSM 之间桥接 ✓
- 职业系统 GDD: "攻击系统 | 职业 → 攻击 | 职业提供基础招式 AttackData 数组" ✓ 一致

## Tuning Knobs

### 全局旋钮

| 旋钮名 | 默认值 | 安全范围 | 调高效果 | 调低效果 |
|--------|--------|---------|---------|---------|
| DefaultHitstopFrames | 4 帧 | 0–8 | 命中"卡顿感"更强，节奏更慢 | 命中反馈更弱，节奏更快 |
| MaxProjectileCount | 5 | 1–10 | 允许同屏更多投射物 | 更严格的投射物数量限制 |
| ProjectileCollisionLayer | "Projectile" | — | Unity 物理层设置，投射物碰撞检测层 | — |

### 攻击数据旋钮（每个攻击独立配置）

| 旋钮名 | 说明 | 安全范围 |
|--------|------|---------|
| HitboxOffset.x | hitbox 水平偏移（正=前方） | -3.0 到 3.0 u |
| HitboxOffset.y | hitbox 垂直偏移 | -2.0 到 3.0 u |
| HitboxSize.x | hitbox 宽度 | 0.3 到 3.0 u |
| HitboxSize.y | hitbox 高度 | 0.3 到 2.0 u |
| HitstopFrames | 命中冻结帧数 | 0–8 帧 |
| ProjectileSpeed | 投射物飞行速度 | 5.0–15.0 u/s |
| ProjectileLifetime | 投射物存活帧数 | 30–120 帧（0.5s–2s） |

**旋钮交互警告**:
- `HitboxSize` 和 `HitboxOffset` 共同决定攻击判定范围——增大 hitbox 同时前移偏移，攻击覆盖范围显著扩大
- `ProjectileSpeed` 和 `ProjectileLifetime` 共同决定投射物最大飞行距离（= Speed × Lifetime/60）。调一个必须检查另一个。
- `DefaultHitstopFrames` 影响所有攻击的命中手感——它是默认值，每个攻击可以用自己的 HitstopFrames 覆盖
- 近战 hitbox 大小需要配合角色的 SilhouetteScale——小角色（盗贼 SilhouetteScale=0.85）的 hitbox 应相应缩小

## Visual/Audio Requirements

[To be designed]

## UI Requirements

[To be designed]

## Acceptance Criteria

### Hitbox 生命周期

- **GIVEN** 角色进入 Attacking.Startup, **WHEN** 帧数未达到 StartupFrames, **THEN** 无 hitbox 存在
- **GIVEN** 攻击进入 Active 阶段（近战攻击）, **WHEN** Active 阶段开始, **THEN** hitbox 创建在 CharacterPosition + HitboxOffset × FacingDirection 位置
- **GIVEN** 攻击进入 Recovery 阶段, **WHEN** 近战 hitbox 存在, **THEN** hitbox 被销毁
- **GIVEN** 攻击被 HitStun 强制取消, **WHEN** hitbox 存在, **THEN** hitbox 立即销毁

### Hitbox 定位

- **GIVEN** 角色在 (2.0, 0.5)，面朝右(1)，HitboxOffset = (0.8, 0.3), **WHEN** hitbox 创建, **THEN** hitbox 中心 = (2.8, 0.8)
- **GIVEN** 角色面朝左(-1)，HitboxOffset = (0.8, 0.3), **WHEN** hitbox 创建, **THEN** hitbox 中心 X 分量镜像（偏移 = -0.8）

### 投射物

- **GIVEN** 法师 GroundAttack（IsProjectile=true, ProjectileSpeed=8.0, ProjectileLifetime=60）, **WHEN** Active 阶段开始, **THEN** 投射物在角色前方生成，以 8.0 u/s 水平飞行
- **GIVEN** 投射物存活 60 帧, **WHEN** 帧数到达, **THEN** 投射物自动销毁
- **GIVEN** 投射物命中 hurtbox, **WHEN** 碰撞检测确认命中, **THEN** 投射物销毁，触发伤害事件
- **GIVEN** 投射物碰到实心平台/墙壁, **WHEN** 碰撞检测确认, **THEN** 投射物销毁（不造成伤害）
- **GIVEN** 攻击者被 KO, **WHEN** 投射物仍在飞行, **THEN** 投射物继续飞行不受影响

### 多次命中防护

- **GIVEN** 战士 GroundAttack 命中 P2, **WHEN** 同一攻击 Active 阶段继续, **THEN** P2 不会再被同一攻击命中
- **GIVEN** 投射物命中 P2, **WHEN** 投射物继续飞行并碰到 P2, **THEN** 忽略（P2 已在 HitTargets 中）

### Hitstop

- **GIVEN** 攻击命中（HitstopFrames=4）, **WHEN** 命中确认, **THEN** 命中者和被命中者双方冻结 4 帧，然后恢复
- **GIVEN** HitstopFrames=0, **WHEN** 命中, **THEN** 无冻结，立即继续
- **GIVEN** hitstop 期间被新攻击命中, **WHEN** 新攻击 HitstopFrames=5 > 当前剩余 hitstop 3, **THEN** hitstop 延长至 5 帧

### 攻击类型解析

- **GIVEN** 3C MovementState = Idle, **WHEN** 攻击输入被接受, **THEN** 使用 GroundAttack 数据
- **GIVEN** 3C MovementState = Jumping, **WHEN** 攻击输入被接受, **THEN** 使用 AirAttack 数据
- **GIVEN** 3C MovementState = Dashing, **WHEN** 攻击输入被接受, **THEN** 使用 DashAttack 数据

### 数据完整性

- **GIVEN** AttackData.HitboxSize = (0, 0), **WHEN** 攻击执行, **THEN** hitbox 创建但大小为零（不会命中任何目标），记录警告
- **GIVEN** 投射物攻击 ProjectileLifetime = 0, **WHEN** Active 阶段开始, **THEN** 投射物立即销毁，记录警告

### 性能

- **GIVEN** 2 人对战进行中，每人可能有 1 个投射物, **THEN** 攻击系统帧耗时 < 1.0ms（含 hitbox 更新和投射物移动）

> `qa-lead` not consulted — Lean mode. Review manually before production.

## Open Questions

[To be designed]
