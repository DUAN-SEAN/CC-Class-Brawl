# 击退与击飞系统 (Knockback and Launch System)

> **Status**: In Design
> **Author**: SeanDuan + agents
> **Last Updated**: 2026-05-23
> **Implements Pillar**: Pillar 1: 秒学秒玩, Pillar 3: 高手菜鸟都开心, Pillar 4: 快速战斗

## Overview

击退与击飞系统是职业对决的物理反馈与淘汰判定层，负责在每次命中后将伤害计算系统输出的击退力度（KnockbackMagnitude）转化为带方向的物理速度，并持续监控角色是否飞出场地边界（Blast Zone）来触发 KO。它的工作分两部分：**击退向量计算**——结合命中点、攻击者与被击者的位置关系，将力度标量转化为方向明确的物理向量，然后委托 3C 系统施加到角色物理体上；**KO 判定**——每帧检查角色中心坐标是否超出 Blast Zone 边界，一旦超出即通知对局管理系统该角色被淘汰。击退速度受重力影响形成自然的抛物线弧——玩家看到的不是"数字变化"，而是"角色被打飞出去"的物理轨迹。对于玩家而言，这个系统决定了"被打飞多远"和"什么时候被 KO"这两个最直觉的问题——0% 时被击中只是轻微后退，100% 时同样的攻击把你弹飞到屏幕边缘，150% 时一记重击把你送出场地，那道弧线就是对局的高潮句号。

## Player Fantasy

**核心幻想：「被打飞的弧线就是对局的句号」**

玩家应该感觉击退是这个游戏最有冲击力的物理反馈——不是"数字变了"，而是"我的角色真的被打飞出去了"。每一次命中都在空间中画出一道弧线：0% 时是轻微的位移，50% 时是明显的后仰，100% 时角色撞向屏幕边缘，150% 时那道弧线直接把对手送出画面。这道弧线就是对局的张力曲线——它从温柔开始，逐渐加速，最终以一记将对手送入星空的重击作为高潮。

**关键情感时刻**：
- **击飞弧线的满足感** — 看着 150% 的对手被你的重攻击击飞出屏幕，那道抛物线就是"完美一击"的视觉奖赏
- **高百分比的危险感** — 自己 100%+ 时，每一次被命中都可能是最后一次，"我得躲开"的紧迫感
- **边缘回归的紧张博弈** — 被击飞到场地边缘，靠着二段跳和空中闪避挣扎回来，"我还活着！"的庆幸
- **KO 的终结感** — 对手飞出 Blast Zone 的那一刻，是对局最明确的"结束"信号

**支柱对齐**：
- 服务 **Pillar 1: 秒学秒玩** — 击退直觉化：百分比越高飞得越远，无需解释
- 服务 **Pillar 3: 高手菜鸟都开心** — 新手享受击飞和 KO 的视觉冲击，高手精确计算"这个百分比能不能 KO"
- 服务 **Pillar 4: 快速战斗** — 高百分比时 KO 节奏加速，对局自然走向高潮

> `creative-director` not consulted — Lean mode. Review manually before production.

## Detailed Design

### Core Rules

**1. 击退向量计算**

当伤害计算系统输出 `KnockbackMagnitude` 且格斗状态机判定 `KnockbackMagnitude > KnockbackThreshold` 进入 Knockback 状态时：

1. 计算水平方向：`horizontalDir = sign(target.position.x - attacker.position.x)`
   - 如果两者 x 坐标相同（极端情况），使用攻击者面朝方向
2. 计算击退方向：`knockbackDir = normalize(Vector2(horizontalDir, KnockbackLaunchRatio))`
3. 计算击退速度：`KnockbackSpeed = KnockbackMagnitude × KnockbackSpeedMultiplier`
4. 最终向量：`KnockbackVector = knockbackDir × KnockbackSpeed`
5. 传递给格斗状态机 → 委托 3C 施加：`SetVelocity(KnockbackVector)`

**2. 不可操作期物理（Hitstun 期间）**

击退命中后的不可操作帧数由格斗状态机公式定义（`KnockbackHitstunFrames`）。在此期间，击退系统每帧更新物理：

1. 水平衰减：`Vx = Vx × KnockbackDecayRate`
2. 垂直方向受重力：`Vy = Vy - Gravity × dt`（Gravity = 32.0 u/s²，与 3C 系统一致）
3. 更新速度：`SetVelocity(Vx, Vy)`
4. 玩家输入被冻结（3C 的 FreezeMovement = true）

**3. 可操作恢复期**

Hitstun 结束后，格斗状态机回到 Idle，3C 重新接管：

1. 角色当前速度（已衰减）成为 3C 的起始速度
2. 3C 正常施加重力和空中控制
3. 如果水平速度仍高于 MaxAirSpeed（3.5 u/s），使用**恢复衰减**平滑过渡：
   - `if |Vx| > MaxAirSpeed: Vx *= KnockbackRecoveryRate`
   - 一旦 |Vx| ≤ MaxAirSpeed，恢复正常空中控制
4. 玩家可以正常操作（跳跃、攻击、空中闪避、技能）

**4. KO 判定**

每帧检查角色中心坐标是否超出场地系统提供的 Blast Zone 边界：

1. 查询：`IArenaDataProvider.GetBlastZone() → BoundsData { left, right, top, bottom }`
2. KO 条件（任一满足）：
   - `position.x < BlastLeft` | `position.x > BlastRight`
   - `position.y < BlastBottom` | `position.y > BlastTop`
3. 严格不等式：恰好在边界上不判定 KO
4. KO 触发后：发射 KO 事件给对局管理系统，停止该角色物理更新

**5. 着地处理**

击退期间角色着地（3C 检测地面碰撞触发）：

1. 格斗状态机结束 Knockback 状态，转入 Landing（3 帧着陆延迟）
2. 垂直速度归零，水平速度被地面摩擦减速
3. KO 判定继续执行（但地面角色通常不会超出边界）

**6. 多次击退**

角色在恢复期再次被命中：新击退向量覆盖当前速度（不叠加），进入新的 Knockback 状态。

### States and Transitions

击退系统是计算层，不维护独立状态机。内部追踪以下逻辑阶段：

| 逻辑阶段 | 进入条件 | 退出条件 | 物理行为 |
|---------|---------|---------|
| 无击退 | 角色未受击退级攻击 | 受到击退级攻击 | 3C 正常控制 |
| 不可操作期 | Knockback 状态进入 | KnockbackHitstunFrames 耗尽 | 重力 + 水平衰减 + KO 检测 |
| 可操作恢复期 | Hitstun 结束 | 速度回到正常范围或着地 | 3C 控制 + 恢复衰减 + KO 检测 |
| KO | 位置超出 Blast Zone | — | 物理更新停止 |

### Interactions with Other Systems

| 系统 | 数据流方向 | 接口描述 |
|------|-----------|---------|
| 伤害计算系统 | 伤害 → 击退 | 提供 KnockbackMagnitude + HitPoint + AttackerId + TargetId |
| 格斗状态机 | 击退 → FSM | 提供击退向量（KnockbackVector），FSM 委托 3C 施加 |
| 3C系统 | 击退 → 3C（间接） | SetVelocity(KnockbackVector) 施加击退力 |
| 3C系统 | 3C → 击退 | 提供角色当前位置（KO 判定）和地面检测（着地事件） |
| 场地/平台系统 | 场地 → 击退 | GetBlastZone() 提供 KO 边界坐标 |
| 对局管理系统 | 击退 → 对局 | KO 事件通知（CharacterId, KO direction） |
| 战斗HUD | 击退 → HUD | KO 事件（用于 KO 动画）、击退力度（用于视觉特效强度） |

## Formulas

**单位系统**: 与 3C 系统一致，1 Unity 单位 = 64 像素，时间以 60Hz 帧为基准（dt = 1/60）。

### 1. 击退向量计算

`KnockbackVector = normalize(Vector2(horizontalDir, KnockbackLaunchRatio)) × KnockbackMagnitude × KnockbackSpeedMultiplier`

| 变量 | 符号 | 类型 | 范围 | 描述 |
|------|------|------|------|------|
| 水平方向 | horizontalDir | int | {-1, 1} | sign(target.x - attacker.x) |
| 发射仰角比 | KnockbackLaunchRatio | float | 0.5–2.0 | 1.0=45°, 0.5=~27°, 2.0=~63° |
| 击退力度 | KnockbackMagnitude | float | 1.5–50+ | 伤害计算系统输出 |
| 速度倍率 | KnockbackSpeedMultiplier | float | 1.0–3.0 | 力度→速度换算乘数 |
| 击退向量 | KnockbackVector | Vector2 | — | 最终施加的速度向量(u/s) |

**输出范围**: 速度分量无上限；正常对局约 (1, 1) 到 (35, 35) u/s
**示例**: 攻击者(-2, 0.75), 被击者(2, 0.75), KnockbackMagnitude=8.4, KnockbackSpeedMultiplier=2.0. horizontalDir=1, 方向=normalize(1,1)=(0.707,0.707). KnockbackVector = (0.707, 0.707) × 8.4 × 2.0 = (11.88, 11.88) u/s

### 2. 不可操作期速度更新

```
Vx_new = Vx × KnockbackDecayRate
Vy_new = Max(Vy - Gravity × dt, -TerminalVelocity)
```

| 变量 | 符号 | 类型 | 范围 | 描述 |
|------|------|------|------|------|
| 水平速度 | Vx | float | — | 当前击退水平速度(u/s) |
| 垂直速度 | Vy | float | — | 当前垂直速度(u/s)，正=上 |
| 击退衰减率 | KnockbackDecayRate | float | 0.95–0.995 | 每帧水平速度乘数 |
| 重力 | Gravity | float | 32.0 u/s² | 与 3C 系统一致 |
| 终端速度 | TerminalVelocity | float | 20.0 u/s | 与 3C 系统一致 |

**输出范围**: Vx 趋向 0（由恢复衰减接管）；Vy 钳制在 [-20.0, +inf]
**示例**: Vx=11.88, Vy=11.88, KnockbackDecayRate=0.99. Vx_new=11.76, Vy_new=11.88-0.533=11.35

### 3. 恢复期速度更新

```
if |Vx| > MaxAirSpeed:
    Vx_new = Vx × KnockbackRecoveryRate
else:
    3C 正常空中控制接管
```

| 变量 | 符号 | 类型 | 范围 | 描述 |
|------|------|------|------|------|
| 水平速度 | Vx | float | — | hitstun 结束后的残余速度 |
| 最大空中速度 | MaxAirSpeed | float | 3.5 u/s | 3C 系统定义 |
| 恢复衰减率 | KnockbackRecoveryRate | float | 0.85–0.95 | 比击退期衰减更快 |

**输出范围**: 从当前值衰减至 MaxAirSpeed 后停止
**示例**: Vx=10.0, KnockbackRecoveryRate=0.92. 帧1: 9.2, 帧2: 8.46, ...约 13 帧后 ≤ 3.5

### 4. Blast Zone KO 判定

`IsKO = (position.x < BlastLeft) OR (position.x > BlastRight) OR (position.y < BlastBottom) OR (position.y > BlastTop)`

| 变量 | 符号 | 类型 | 范围 | 描述 |
|------|------|------|------|------|
| 角色位置 | position.x, position.y | float | — | 角色中心坐标 |
| KO 边界 | BlastLeft/Right/Top/Bottom | float | — | 场地系统提供 |

**输出范围**: Boolean
**示例**: position=(16.5, 3.0), BlastRight=15.0 → 16.5 > 15.0 → IsKO=true

### 5. KO 距离估算（验证用）

基于 MVP 默认值（BlastZone ±15/14/-10, KnockbackSpeedMultiplier=2.0, KnockbackDecayRate=0.99, KnockbackRecoveryRate=0.92, BaseKnockbackGrowth=0.15）:

| 场景 | Magnitude | 初始速度(Vx) | 总水平位移 | KO 判定 |
|------|-----------|-------------|-----------|--------|
| 盗贼轻击, 50% | 2.15 | 3.0 u/s | ~1.6u | 安全（任何位置） |
| 战士地面攻击, 80% | 8.96 | 12.7 u/s | ~7.5u | KO 需离边界 ~7.5u |
| 战士地面攻击, 150% | 9.80 | 13.9 u/s | ~8.5u | KO 需离边界 ~6.5u |
| 战士冲刺攻击, 100% | 15.6 | 22.1 u/s | ~14.0u | KO 需离边界 ~1u |
| 战士冲刺攻击, 150% | 17.4 | 24.6 u/s | ~16.0u | 可从中心 KO！ |

**设计意图**: BaseKnockbackGrowth=0.15 让高百分比时 KO 更容易感知。战士冲刺攻击在 150% 可从场地中心 KO（位移 ~16u > 15u BlastZone），但这是最强职业的最强攻击在极高百分比下的结果——正常对局中更常见的是 100% 左右的 KO（仍需将对手逼到距边界 ~1u）。低击退攻击（盗贼/法师基础招式）在高百分比下仍需将对手逼向边缘。

### 6. KnockbackThreshold 校准建议

当前占位值 5.0 存在问题：战士所有攻击始终触发 Knockback（即使在 0%）。建议校准为 **9.0**（配合 BaseKnockbackGrowth=0.15）：

| 攻击 | BaseKnockback | 触发 Knockback 的最低% |
|------|---------------|---------------------|
| 盗贼轻击 | 2.0 | 永不（始终 HitStun） |
| 盗贼冲刺 | 3.5 | ~1048%（实际永不触发） |
| 法师地面攻击 | 4.0 | ~833%（实际永不触发） |
| 战士地面攻击 | 8.0 | ~8%（低% HitStun，之后 Knockback） |
| 战士冲刺攻击 | 12.0 | < 0%（始终 Knockback） |

此校准需要更新格斗状态机 GDD 中的 `KnockbackThreshold` 和实体注册表。

## Edge Cases

**击退向量相关**:
- **如果攻击者和被击者 x 坐标完全相同**: 使用攻击者面朝方向作为 horizontalDir。方向计算结果合理（向攻击者面朝方向击退）。
- **如果 KnockbackMagnitude 为 0（数据错误）**: 击退向量为零向量。角色不移动。不进入 Knockback 状态（0 < KnockbackThreshold）。记录警告。
- **如果 KnockbackSpeedMultiplier 为 0（配置错误）**: 同上，击退向量为零。记录警告。

**不可操作期相关**:
- **如果 KnockbackDecayRate = 1.0**: 水平速度不衰减。角色以恒定水平速度飞行直到重力将其拉回地面。合法但会导致极长飞行距离。
- **如果 KnockbackDecayRate > 1.0（配置错误）**: 钳制为 1.0。大于 1.0 意味着速度在增加，这不是击退衰减的正确方向。
- **如果 KnockbackDecayRate = 0**: 水平速度立即归零。角色只受垂直重力影响（纯上抛运动）。合法但感觉不自然。
- **如果重力在击退期间被禁用**: 击退系统使用 3C 的 Gravity 常量（32.0 u/s²）。如果 Gravity = 0，角色沿直线飞行不回落。这不是合法的击退物理。

**KO 判定相关**:
- **如果角色恰好停在 Blast Zone 边界线上**: 不判定 KO。判定使用严格不等式（`>` / `<`），等于边界不触发。与场地 GDD 的定义一致。
- **如果角色一帧内移动距离极大（极端击退 + 高衰减差）**: 每帧位置更新后再检查边界。即使速度极高（如 100+ u/s），每帧移动 ~1.7u，远小于 Blast Zone 尺寸（30u 宽），不会出现"穿越"问题。对于理论上的极端情况（速度 > 1800 u/s），需使用射线检测从旧位置到新位置是否穿越边界——MVP 中此情况不可能发生。
- **如果 KO 事件发出后角色仍在更新物理**: KO 事件触发后立即停止该角色的所有物理更新。不再检查边界、不再更新速度、不再响应输入。
- **如果两个角色同一帧都被 KO**: 各自独立触发 KO 事件。对局管理系统决定如何处理同帧双 KO（由对局管理 GDD 定义）。

**恢复期相关**:
- **如果恢复期角色着地**: 3C 检测地面碰撞，转入 Landing 状态。恢复衰减中断，垂直速度归零，水平速度被地面摩擦接管。
- **如果恢复期角色使用空中闪避**: 空中闪避期间击退水平速度减半（3C GDD 定义），闪避结束后恢复衰减继续。
- **如果恢复期角色再次被命中**: 新击退向量覆盖当前速度（包括恢复衰减中的残余速度），进入新的 Knockback 状态。旧衰减状态清除。
- **如果恢复衰减率 KnockbackRecoveryRate = 1.0**: 速度永不衰减到 MaxAirSpeed，角色永远保持高速度。非法——钳制为最高 0.95。

**场地相关**:
- **如果场地未加载（Unloaded/Error 状态）**: `GetBlastZone()` 返回默认值（0, 0, 0, 0）。所有角色立即 KO——这是场地加载失败的正确行为，不应在场地未就绪时进行战斗。
- **如果场地 Blast Zone 配置值不合理（如 left > right）**: 由场地系统在加载时验证。击退系统不做额外验证，信任场地系统返回的数据。

## Dependencies

| 依赖系统 | 方向 | 类型 | 数据接口 | GDD 状态 |
|---------|------|------|---------|---------|
| 伤害计算系统 | 上游（硬依赖） | 事件 | 提供 KnockbackMagnitude + HitPoint + AttackerId + TargetId | Designed |
| 格斗状态机 | 上游（硬依赖） | 双向 | FSM 判定 Knockback 状态，击退系统提供 KnockbackVector | Designed |
| 3C系统 | 上游（硬依赖） | 控制 | SetVelocity(KnockbackVector) 施加力；提供角色位置和地面检测 | In Review |
| 场地/平台系统 | 上游（硬依赖） | 查询 | `IArenaDataProvider.GetBlastZone()` 提供 KO 边界 | Designed |
| 对局管理系统 | 下游（硬依赖） | 事件 | KO 事件通知（CharacterId, KO direction） | 未设计 |
| 战斗HUD | 下游（软依赖） | 事件 | KO 事件、击退力度（用于视觉特效强度） | 未设计 |

**向上提供的接口契约**:
- `IKnockbackSystem` 接口: 击退计算和 KO 判定入口
- `OnKnockbackApplied(CharacterId, KnockbackVector)`: 击退施加事件
- `OnKO(CharacterId, KODirection)`: 角色被 KO 事件
- `GetKnockbackState(CharacterId)`: 查询角色当前击退状态（不可操作期/恢复期/无）
- `GetKnockbackVelocity(CharacterId)`: 查询角色当前击退速度（调试用）

**双向依赖验证**:
- 伤害计算系统 GDD 列出 "击退与击飞系统" 为下游依赖 ✅
- 格斗状态机 GDD 列出 "击退与击飞系统" 为上游依赖 ✅
- 场地/平台系统 GDD 列出 "击退与击飞系统" 为下游依赖 ✅
- 对局管理系统 GDD 未设计（将在其 GDD 中确认反向引用）

## Tuning Knobs

| 旋钮名 | 默认值 | 安全范围 | 调高效果 | 调低效果 | 所属公式 |
|--------|--------|---------|---------|---------|---------|
| KnockbackSpeedMultiplier | 2.0 | 1.0–3.0 | 击退速度更快，KO 距离更远，对局更快结束 | 击退速度更慢，需要更高%才能 KO | 击退向量 |
| KnockbackLaunchRatio | 1.0 | 0.5–2.0 | 击退更垂直（浮空更高），水平位移更少 | 击退更水平（横向推得更远），KO 更容易 | 击退向量 |
| KnockbackDecayRate | 0.99 | 0.95–0.995 | 速度衰减更慢，击退飞行更远 | 速度衰减更快，击退飞行更短 | 不可操作期 |
| KnockbackRecoveryRate | 0.92 | 0.85–0.95 | 恢复更慢，可操作后仍高速移动更长时间 | 恢复更快，玩家更快重获完全控制 | 恢复期 |

**旋钮交互警告**:
- `KnockbackSpeedMultiplier` 和 `KnockbackLaunchRatio` 共同决定初始击退轨迹——改一个必须验证 KO 距离是否仍然合理
- `KnockbackDecayRate` 和 `KnockbackRecoveryRate` 共同决定总飞行距离——衰减越慢飞得越远，直接影响 KO 阈值
- `KnockbackSpeedMultiplier` 与伤害系统的 `BaseKnockbackGrowth` 乘法关系——高 BaseKnockbackGrowth 意味着高%时 Magnitude 更大，乘以 SpeedMultiplier 后 KO 距离指数增长
- `KnockbackLaunchRatio` 与 3C 的 `Gravity` 共同决定飞行弧线——高 Gravity 让高弹道更快回落，低 Gravity 让角色飞得更高

## Visual/Audio Requirements

**击退视觉反馈**:

**击退轨迹拖尾**:
- 角色在击退不可操作期和恢复期显示运动拖尾
- 拖尾方向与击退方向相反，长度与速度成正比
- 拖尾颜色使用被击者职业色（与受伤闪烁呼应）
- 速度降至 MaxAirSpeed 以下时拖尾消失

**KO 视觉效果**:
- 角色超出 Blast Zone 时：屏幕闪光（白色，1-2 帧）+ 摄像机微缩放（zoom in 再恢复，~0.3 秒）
- KO 方向影响闪光来源：左侧 KO → 左侧闪光，右侧 KO → 右侧闪光
- 被击飞角色在超出摄像机边界后显示方向指示箭头（由战斗 HUD 处理）

**击退力度视觉层级**:
- 轻微击退（< KnockbackThreshold）：无拖尾，轻微命中特效
- 中等击退（KnockbackThreshold ~ 2x）：短拖尾 + 命中点小型能量爆发
- 重击（> 2x KnockbackThreshold）：长拖尾 + 命中点大型能量爆发 + 屏幕微震（2-3 像素，3-5 帧）

**音频反馈**（定义触发事件，由音效系统实现）:

| 音频事件 | 触发时机 | 描述 |
|---------|---------|------|
| `OnKnockback` | 进入 Knockback 状态 | 重击受击音效，比 HitStun 受击更沉更重 |
| `OnKO` | 角色超出 Blast Zone | KO 确认音效，短促有力的"终结"音 |
| `OnKOStar` | KO 时（视觉配合） | 可选：经典"飞星"音效，幽默感 |

**增强层（MVP 后）**:
- 击退运动模糊效果
- 高力度 KO 时的慢动作回放（Hit Stop + Time Dilation）
- KO 时背景短暂暗化 + 聚光灯效果

## UI Requirements

击退系统不直接产生 UI 元素，但触发以下 UI 事件：

- **KO 通知**: KO 发生时通知战斗 HUD，由 HUD 显示 "KO!" 文字动画（由战斗 HUD GDD 定义具体样式）
- **边缘警告**: 当角色接近 Blast Zone 边界时（距离 < 边缘警告阈值），屏幕边缘渐变红色/橙色。由战斗 HUD 查询角色位置和 Blast Zone 数据实现
- **方向指示箭头**: 角色超出摄像机边界后，在屏幕边缘显示箭头指向角色位置。由战斗 HUD 实现

无独立的 UI 需求。

## Acceptance Criteria

### 击退向量计算

- **GIVEN** 攻击者在 (-2, 0.75), 被击者在 (2, 0.75), KnockbackMagnitude=8.4, KnockbackSpeedMultiplier=2.0, KnockbackLaunchRatio=1.0, **WHEN** 计算击退向量, **THEN** KnockbackVector = (11.88, 11.88) u/s（±0.01）
- **GIVEN** 攻击者在 (2, 0.75), 被击者在 (-2, 0.75)（攻击者在右侧）, **WHEN** 计算击退方向, **THEN** horizontalDir = -1（向左击退）
- **GIVEN** 攻击者和被击者 x 坐标相同, 攻击者面朝右, **WHEN** 计算击退方向, **THEN** horizontalDir = 1（使用面朝方向）

### 不可操作期物理

- **GIVEN** 初始 KnockbackVector=(11.88, 11.88), KnockbackDecayRate=0.99, Gravity=32.0, **WHEN** 执行 1 帧物理更新, **THEN** Vx=11.76, Vy=11.35
- **GIVEN** 初始 KnockbackVector=(11.88, 11.88), **WHEN** 执行 9 帧 hitstun 物理更新, **THEN** Vx ≈ 10.85, 位移总量约 (1.75, 1.65) u

### 恢复期衰减

- **GIVEN** hitstun 结束时 Vx=10.0, MaxAirSpeed=3.5, KnockbackRecoveryRate=0.92, **WHEN** 执行恢复衰减, **THEN** 约 13 帧后 |Vx| ≤ 3.5

### KO 判定

- **GIVEN** 角色在 (16.5, 3.0), BlastRight=15.0, **WHEN** 检查 KO, **THEN** IsKO = true
- **GIVEN** 角色在 (15.0, 3.0)（恰好在边界上）, BlastRight=15.0, **WHEN** 检查 KO, **THEN** IsKO = false（严格大于）
- **GIVEN** 角色在 (-3.0, -10.5), BlastBottom=-10.0, **WHEN** 检查 KO, **THEN** IsKO = true（下方越界）
- **GIVEN** 角色在 (0, 0.75), **WHEN** 检查 KO, **THEN** IsKO = false（安全位置）

### 着地处理

- **GIVEN** 角色在击退恢复期着地, **WHEN** 3C 检测地面碰撞, **THEN** 恢复衰减中断，垂直速度归零，3C 地面摩擦接管水平速度

### 多次击退

- **GIVEN** 角色在恢复期（Vx=5.0 残余速度）, **WHEN** 再次被命中产生新 KnockbackVector=(15, 10), **THEN** 速度直接设为 (15, 10)，旧残余速度清除

### 场地交互

- **GIVEN** 场地系统返回 BlastZone {left=-15, right=15, top=14, bottom=-10}, **WHEN** 角色在 (0, 14.5), **THEN** IsKO = true（上方越界）

### 性能

- **GIVEN** 2 人对战, **THEN** 击退系统每帧处理耗时 < 0.1ms（纯速度更新 + 坐标比较）

> `qa-lead` not consulted — Lean mode. Review manually before production.

## Open Questions

1. **DI（方向影响）是否纳入后续版本？** MVP 不包含 DI。大乱斗中 DI 是核心深度机制——玩家在 hitstun 期间通过输入方向影响击退角度。建议在 VS 版本中考虑简化版 DI。（Owner: 设计师，里程碑: VS）
2. **攻击特定击退角度是否纳入后续版本？** MVP 使用固定仰角（KnockbackLaunchRatio=1.0）。大乱斗中每个攻击有自己的击退角度（上击、侧击、下击），增加战术深度。（Owner: 设计师，里程碑: Alpha）
3. **墙壁碰撞是否影响击退？** 当前设计中角色击退不与场地墙壁交互（场地只有平台，无墙壁）。如果后续加入封闭场地，需要定义击退与墙壁的碰撞行为。（Owner: 设计师，里程碑: Alpha）
4. **Edge Guard（边缘防守）机制？** 当前 MVP 无 Edge Grab（边缘抓挂）。击退系统需要与回场机制（二段跳 + 空中闪避）配合，确保被击飞的角色有合理的回场机会。（Owner: 设计师，里程碑: 原型验证后）
