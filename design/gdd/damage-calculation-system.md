# 伤害计算系统 (Damage Calculation System)

> **Status**: In Design
> **Author**: SeanDuan + agents
> **Last Updated**: 2026-05-23
> **Implements Pillar**: Pillar 1: 秒学秒玩, Pillar 3: 高手菜鸟都开心, Pillar 4: 快速战斗

## Overview

伤害计算系统是职业对决的伤害累积与击退力度计算层，负责在每次命中时将攻击数据（BaseDamage、BaseKnockback）转化为两个核心输出：被击者累积的伤害百分比（Damage%）和本次攻击产生的击退力度（KnockbackMagnitude）。伤害百分比是平台格斗游戏的核心资源——它不直接导致死亡，但随着百分比升高，每次攻击产生的击退力越来越大，被击飞出场地边界（Blast Zone）的风险也随之升高。这种"越伤越危险"的正反馈环创造了天然的对局张力曲线：开局双方 0% 时安全感十足，50% 时开始紧张，100%+ 时每一次攻击都可能是致命的。伤害计算系统从碰撞判定系统接收命中事件（通过格斗状态机转发），查询攻击者的 AttackData，结合被击者当前的伤害百分比计算出最终伤害增量和击退力度，然后将结果传递给击退与击飞系统（物理力方向和大小）和战斗 HUD（百分比显示）。物理运行在 60Hz 固定时间步中执行（参见 `docs/architecture/adr-physics-timestep.md`）。对于玩家而言，伤害计算系统决定了"这个游戏有多痛"——轻击轻伤、重击重伤、高百分比时一记轻击也可能致命，这种直觉化的伤害反馈让玩家无需理解公式就能感受到伤害的分量。

## Player Fantasy

**核心幻想：「伤害是倒计时——越高越危险」**

玩家应该感觉伤害百分比是一个不断升温的"危险计"。0% 时打人很轻松，因为对手不会被击飞太远；但随着对手的百分比升高，同样的攻击开始把他们击飞得越来越远——这种"我的攻击越来越致命"的正反馈让每次命中都越来越刺激。同时，自己的百分比也在升高，对手的每次命中也越来越危险——双方都在"升温"，对局自然走向高潮。

**关键情感时刻**：
- **伤害累积的紧张感** — 看到自己的百分比从 30% 跳到 50% 时，"快了，快到危险区了"的紧张
- **高百分比的致命感** — 100%+ 时，对手的一个轻攻击就能把你击飞出场，"我得小心"的紧迫感
- **击飞弧线的满足感** — 当你 150% 的对手被你的重攻击击飞出屏幕，那道弧线就是"完美一击"的视觉回报
- **逆风翻盘的可能性** — 低百分比不意味着安全——如果被对手连击到高百分比，一局可能在 30 秒内翻盘

**支柱对齐**：
- 服务 **Pillar 1: 秒学秒玩** — 百分比伤害直觉化：数字越大越危险，无需解释
- 服务 **Pillar 3: 高手菜鸟都开心** — 新手享受伤害数字增长的满足感，高手理解击退公式精确计算"这个百分比能 KO 吗"
- 服务 **Pillar 4: 快速战斗** — 伤害增长足够快，2-3 分钟内对局达到高潮

> `creative-director` not consulted — Lean mode. Review manually before production.

## Detailed Design

### Core Rules

**1. 伤害百分比模型**

每个角色维护一个伤害百分比值（`DamagePercent`），对局开始时为 0.0。

- 伤害百分比只增不减（MVP 无治疗机制）
- 伤害百分比无上限（理论上可以到 999%+，但实际对局通常在 100-200% 结束）
- 伤害百分比以浮点精度存储，以整数显示给玩家（向下取整显示）

每次命中时的伤害增量：
```
DamageGain = AttackData.BaseDamage
TargetDamagePercent += DamageGain
```

伤害增量就是攻击数据中定义的 `BaseDamage`——战士重击 +12%，盗贼轻击 +4%，法师投射物 +7%。不乘以任何百分比修正因子。

**2. 击退力度计算**

击退力度由攻击的 `BaseKnockback` 和被击者当前的 `DamagePercent` 共同决定。百分比越高，同样的攻击产生的击退力越大。

```
KnockbackMagnitude = BaseKnockbackGrowth × DamagePercent × BaseKnockback / 100 + BaseKnockback
```

公式逻辑：
- `BaseKnockbackGrowth`：百分比增长系数——控制"百分比每增加 1% 击退力增加多少"
- `DamagePercent / 100`：将百分比标准化到 0-1+ 范围
- `BaseKnockback`：攻击的基础击退值——重攻击比轻攻击击退更远，即使在 0% 也是如此

当 DamagePercent = 0 时：KnockbackMagnitude = BaseKnockback（纯基础击退）
当 DamagePercent = 100 时：KnockbackMagnitude = BaseKnockbackGrowth × 1.0 × BaseKnockback + BaseKnockback

**3. 命中处理管线**

当格斗状态机转发命中事件时，伤害计算系统执行：

1. **接收 HitEvent**：包含 AttackerId, TargetId, AttackId
2. **查询 AttackData**：通过 AttackId 查找攻击数据（BaseDamage, BaseKnockback, HitStunFrames）
3. **更新伤害百分比**：TargetDamagePercent += BaseDamage
4. **计算击退力度**：KnockbackMagnitude = 公式(2)
5. **传递给格斗状态机**：KnockbackMagnitude 用于 HitStun/Knockback 判定
6. **传递给击退系统**：KnockbackMagnitude + HitPoint 用于计算击退向量
7. **通知 HUD**：DamagePercent 变化事件

**4. 伤害百分比重置**

- 新一局开始时：所有角色 DamagePercent = 0.0
- 角色被 KO 后：在下一局重生时重置为 0.0
- 同一局内 KO 后重生（如果对局格式是多局制）：待对局管理系统 GDD 定义

### States and Transitions

伤害计算系统维护每个角色的伤害状态，但无独立状态机——它是纯计算层。

| 触发条件 | 行为 |
|---------|------|
| 收到命中事件 | 更新目标 DamagePercent，计算 KnockbackMagnitude，分发结果 |
| 新一局开始 | 所有角色 DamagePercent 重置为 0.0 |
| 角色重生（KO 后） | 该角色 DamagePercent 重置为 0.0 |

### Interactions with Other Systems

| 系统 | 数据流方向 | 接口描述 |
|------|-----------|---------|
| 碰撞判定系统 | 间接（通过 FSM） | 碰撞系统发送 HitEvent → FSM 转发给伤害系统 |
| 格斗状态机 | FSM → 伤害 | 转发 HitEvent（AttackerId, TargetId, AttackId, HitPoint） |
| 格斗状态机 | 伤害 → FSM | 返回 KnockbackMagnitude 和 HitStunFrames |
| 攻击系统 | 伤害 → 攻击 | 通过 AttackId 查询 AttackData |
| 职业系统 | 间接 | AttackData 来源包含职业基础招式的伤害/击退值 |
| 击退与击飞系统 | 伤害 → 击退 | 提供 KnockbackMagnitude + HitPoint |
| 专注值系统 | 间接 | 命中事件驱动专注值积累（通过攻击系统转发） |
| 战斗HUD | 伤害 → HUD | DamagePercent 变化事件（显示百分比数字） |
| 对局管理系统 | 对局 → 伤害 | 触发新一局重置 DamagePercent |

## Formulas

**单位系统**: 与 3C 系统一致，1 Unity 单位 = 64 像素，时间以 60Hz 帧为基准。

### 1. 伤害百分比更新

`TargetDamagePercent_new = TargetDamagePercent_old + BaseDamage`

| Variable | Symbol | Type | Range | Description |
|----------|--------|------|-------|-------------|
| 被击者当前伤害% | TargetDamagePercent_old | float | 0.0–999.0 | 命中前的累积伤害百分比 |
| 攻击基础伤害 | BaseDamage | float | 3.0–15.0 | AttackData 中定义的伤害值 |
| 被击者更新后伤害% | TargetDamagePercent_new | float | 0.0–999.0+ | 命中后的累积伤害百分比 |

**Output Range**: 0.0 到无上限（实际通常 0–200）
**Example**: Warrior GroundAttack (BaseDamage=12.0) 命中 DamagePercent=30.0 的目标 → 42.0%

### 2. 击退力度计算

`KnockbackMagnitude = BaseKnockbackGrowth × (TargetDamagePercent / 100) × BaseKnockback + BaseKnockback`

| Variable | Symbol | Type | Range | Description |
|----------|--------|------|-------|-------------|
| 攻击基础击退 | BaseKnockback | float | 1.5–12.0 | AttackData 中定义的击退值 |
| 被击者伤害百分比 | TargetDamagePercent | float | 0.0–999.0 | 命中后的伤害百分比 |
| 百分比增长系数 | BaseKnockbackGrowth | float | 0.15 默认 | 每 1% 伤害对击退的增长影响 |
| 击退力度 | KnockbackMagnitude | float | BaseKnockback 到 50+ | 最终击退力度值 |

**Output Range**: BaseKnockback（0% 时）到无上限（高百分比时）；正常对局范围约 2.0–30.0
**Example**: Warrior GroundAttack (BaseKnockback=8.0) 命中 DamagePercent=100%: 0.15×1.0×8.0+8.0 = **9.2**。命中 150%: 0.15×1.5×8.0+8.0 = **9.8**。命中 200%: 0.15×2.0×8.0+8.0 = **10.4**。Rogue GroundAttack (BaseKnockback=2.0) 命中 100%: **2.3**。命中 150%: **2.45**

### 3. 击退力度各职业 KO 百分比估算

基于 KnockbackThreshold=9.0（由击退与击飞系统 GDD 校准），计算各攻击开始触发 Knockback 时的被击者%：

| 攻击者 | 攻击 | BaseKnockback | 触发 Knockback 的最低% |
|--------|------|---------------|----------------------|
| Warrior | GroundAttack | 8.0 | ~8% |
| Warrior | DashAttack | 12.0 | < 0%（始终触发） |
| Rogue | GroundAttack | 2.0 | ~2333%（实战永不触发） |
| Rogue | DashAttack | 3.5 | ~1048%（实战永不触发） |
| Mage | GroundAttack | 4.0 | ~833%（实战永不触发） |
| Mage | DashAttack | 5.0 | ~533%（实战永不触发） |

**设计说明**: 阈值 9.0 下，Warrior 的攻击在正常对局百分比范围内能触发 Knockback（GroundAttack ~8% 起触发），其余职业的基础招式主要产生 HitStun。这为技能系统的击退型技能留出了设计空间。详见 knockback-launch-system.md 公式 6。

### 4. 显示用百分比取整

`DisplayPercent = Floor(TargetDamagePercent)`

| Variable | Symbol | Type | Range | Description |
|----------|--------|------|-------|-------------|
| 实际伤害百分比 | TargetDamagePercent | float | 0.0–999.0 | 浮点精度的累积伤害 |
| 显示百分比 | DisplayPercent | int | 0–999 | 向下取整后显示给玩家 |

**Output Range**: 0 到 999
**Example**: TargetDamagePercent = 42.7 → DisplayPercent = 42

## Edge Cases

**伤害累积相关**:
- **如果 DamagePercent 因连续命中超过 999.0**: 不钳制。百分比无上限。显示时直接显示 999+。高百分比是合法的极端对局状态。
- **如果 DamagePercent 为负数（数据错误）**: 钳制为 0.0。伤害百分比不可能为负——记录警告。
- **如果同一帧角色被多次命中**: 每次命中独立处理——依次更新 DamagePercent 并计算各自的 KnockbackMagnitude。第二次命中使用的 DamagePercent 已包含第一次命中的增量。

**击退力度相关**:
- **如果 BaseKnockback = 0（数据错误）**: KnockbackMagnitude = 0。攻击造成伤害但不产生击退——角色进入 HitStun。记录警告。
- **如果 BaseKnockbackGrowth = 0**: 击退力度恒等于 BaseKnockback，不受百分比影响。合法但消除张力曲线。
- **如果 KnockbackMagnitude 计算结果为负数（不应发生）**: 钳制为 0.0。负数击退无物理意义。记录警告。

**AttackData 查询相关**:
- **如果 AttackId 无效或未注册**: 无法查询 AttackData。命中被忽略，不更新伤害。记录错误。
- **如果 AttackData 的 BaseDamage = 0**: 伤害百分比为 0（不增加），但击退力度仍正常计算。合法设计（纯击退攻击）。

**对局管理相关**:
- **如果新一局开始时 DamagePercent 未重置**: 伤害计算系统必须在对局管理系统的 OnRoundStart 事件中强制重置所有角色的 DamagePercent 为 0.0。
- **如果角色 KO 后重生但 DamagePercent 未重置**: 由对局管理系统在重生时触发重置。伤害计算系统提供 `ResetDamage(CharacterId)` 接口。

## Dependencies

| 依赖系统 | 方向 | 类型 | 数据接口 | GDD 状态 |
|---------|------|------|---------|---------|
| 碰撞判定系统 | 上游（间接） | 事件 | HitEvent 通过 FSM 转发 | Designed |
| 格斗状态机 | 上游（硬依赖） | 双向 | FSM 转发 HitEvent；伤害系统返回 KnockbackMagnitude 和 HitStunFrames | Designed |
| 攻击系统 | 上游（硬依赖） | 查询 | 通过 AttackId 查询 AttackData | Designed |
| 职业系统 | 上游（间接） | 数据来源 | AttackData 来源包含职业基础招式值 | Designed |
| 击退与击飞系统 | 下游（硬依赖） | 数据传递 | 提供 KnockbackMagnitude + HitPoint + AttackerId + TargetId | 未设计 |
| 专注值系统 | 下游（软依赖） | 间接 | 通过攻击系统转发命中事件 | 未设计 |
| 战斗HUD | 下游（硬依赖） | 事件通知 | DamagePercent 变化事件 | 未设计 |
| 对局管理系统 | 上游（软依赖） | 事件 | OnRoundStart 触发 DamagePercent 重置 | Designed |

**向上提供的接口契约**:
- `IDamageSystem` 接口: 伤害计算和查询入口
- `OnHitProcessed(HitEvent, AttackData, KnockbackMagnitude)`: 命中处理完成事件
- `GetDamagePercent(CharacterId)`: 查询角色当前伤害百分比
- `ResetDamage(CharacterId)`: 重置角色伤害百分比
- `ResetAll()`: 重置所有角色伤害百分比

## Tuning Knobs

| 旋钮名 | 默认值 | 安全范围 | 调高效果 | 调低效果 | 所属公式 |
|--------|--------|---------|---------|---------|---------|
| BaseKnockbackGrowth | 0.15 | 0.05–0.30 | 百分比对击退影响更大——高%时击退更猛烈，KO 更快 | 百分比对击退影响更小——需要更高%才能 KO | 击退力度 |

**旋钮交互警告**:
- `BaseKnockbackGrowth` 与格斗状态机的 `KnockbackThreshold` 共同决定 KO 节奏——调一个必须检查另一个。
- `BaseKnockbackGrowth` 与击退系统的物理参数共同决定最终击飞距离——本 GDD 只定义力度。
- 各职业的 `BaseKnockback` 与 `BaseKnockbackGrowth` 乘法关系——高 BaseKnockback 的职业从百分比增长中获益更多。

## Visual/Audio Requirements

伤害计算系统的视觉和音效反馈围绕"伤害即危险"的核心幻想展开，目标是让玩家仅通过视觉和听觉就能感知"这个百分比对不对劲"。

### 视觉反馈

**伤害数字弹出（Damage Popup）**：
- 每次命中时，在被击者头顶弹出伤害增量数字（如 "+12"），使用攻击者的职业色（Warrior 红、Rogue 紫、Mage 蓝）
- 弹出动画：向上漂浮 → 缩放（1.5x → 1.0x）→ 淡出，持续约 0.8 秒
- 多次快速命中时数字叠加偏移（避免重叠），每个数字独立动画

**百分比颜色阈值**：
- 0%–49%：白色（安全感）
- 50%–99%：黄色（警告区）
- 100%–149%：橙色（危险区）
- 150%+：红色闪烁（致命区）

颜色变化在 HUD 百分比数字上体现，不做全屏效果。

**击退力度视觉暗示**：
- 高击退力度命中时（KnockbackMagnitude > KnockbackThreshold），被击者身上的击退视觉特效更强烈（由击退系统定义，此处仅标记"需要配合"）
- 低百分比命中（KnockbackMagnitude < KnockbackThreshold）使用轻量命中特效

### 音效反馈

**命中确认音效（Hit Confirmation）**：
- 每次命中播放命中音效，音量和音调随 KnockbackMagnitude 变化：
  - 轻命中（< KnockbackThreshold）：轻击音效，标准音量
  - 中等命中（KnockbackThreshold ~ 2x）：标准命中音效，音量 +20%
  - 重命中（> 2x KnockbackThreshold）：重击音效，音量 +40%，低频增强

**百分比里程碑音效**：
- 跨越颜色阈值时（50%、100%、150%）播放短促警示音
- 100%+ 时使用更紧张的音调，与"危险区"视觉呼应

**全局节奏**：
- 命中音效不得打断 BGM，混音层级为 SFX（与碰撞系统共享音效触发时机）

## UI Requirements

[To be designed]

## Acceptance Criteria

### 伤害百分比更新

- **GIVEN** 角色当前 DamagePercent=30.0, **WHEN** 被 Warrior GroundAttack (BaseDamage=12.0) 命中, **THEN** DamagePercent = 42.0
- **GIVEN** 角色当前 DamagePercent=150.0, **WHEN** 被 Rogue GroundAttack (BaseDamage=4.0) 命中, **THEN** DamagePercent = 154.0

### 击退力度计算

- **GIVEN** BaseKnockback=8.0, BaseKnockbackGrowth=0.15, TargetDamagePercent=0, **THEN** KnockbackMagnitude = 8.0
- **GIVEN** BaseKnockback=8.0, BaseKnockbackGrowth=0.15, TargetDamagePercent=100, **THEN** KnockbackMagnitude = 9.2
- **GIVEN** BaseKnockback=8.0, BaseKnockbackGrowth=0.15, TargetDamagePercent=150, **THEN** KnockbackMagnitude = 9.8
- **GIVEN** BaseKnockback=2.0, BaseKnockbackGrowth=0.15, TargetDamagePercent=0, **THEN** KnockbackMagnitude = 2.0
- **GIVEN** BaseKnockback=2.0, BaseKnockbackGrowth=0.15, TargetDamagePercent=100, **THEN** KnockbackMagnitude = 2.3

### 百分比取整

- **GIVEN** DamagePercent = 42.7, **THEN** DisplayPercent = 42
- **GIVEN** DamagePercent = 0.3, **THEN** DisplayPercent = 0

### 重置

- **GIVEN** 新一局开始, **WHEN** 对局管理系统触发 OnRoundStart, **THEN** 所有角色 DamagePercent = 0.0

### 数据错误

- **GIVEN** AttackId 无效, **WHEN** 命中事件到达, **THEN** 命中被忽略，记录错误
- **GIVEN** BaseKnockback = 0, **WHEN** 命中处理, **THEN** KnockbackMagnitude = 0，角色进入 HitStun

### 性能

- **GIVEN** 2 人对战, **THEN** 伤害计算系统每次命中处理耗时 < 0.1ms

> `qa-lead` not consulted — Lean mode. Review manually before production.

## Open Questions

[To be designed]
