# 碰撞判定系统 (Hitbox/Hurtbox Detection)

> **Status**: In Design
> **Author**: SeanDuan + agents
> **Last Updated**: 2026-05-23
> **Implements Pillar**: Pillar 1: 秒学秒玩, Pillar 3: 高手菜鸟都开心

## Overview

碰撞判定系统是职业对决的命中检测基础设施，负责在每帧（60Hz）检测所有活跃 hitbox 与所有 hurtbox 之间的空间重叠，并将检测结果以命中事件的形式通知攻击系统、格斗状态机和下游的伤害计算系统。它消费攻击系统提供的 hitbox 定义（位置、大小、攻击者 ID、AttackId）和角色系统的 hurtbox 定义（位置、大小、角色 ID），执行矩形 AABB 重叠检测和多次命中过滤，最终产出结构化的命中事件：攻击者、被击者、命中点坐标、以及触发完整命中管线所需的 AttackData 引用。碰撞判定系统不关心"攻击是怎么发动的"（攻击系统负责）或"命中后发生什么"（格斗状态机和伤害系统负责），它只回答一个问题：**这一帧，哪些 hitbox 碰到了哪些 hurtbox？** 这个回答决定了每一次攻击的"公平感"——判定准确，玩家觉得"确实打中了"；判定缺失，玩家觉得"我明明打到了"的挫败感。物理运行在 60Hz 固定时间步中执行（参见 `docs/architecture/adr-physics-timestep.md`），确保检测频率与帧动画一致。

## Player Fantasy

**核心幻想：「眼见为实——看到了就该打中」**

玩家应该感觉命中判定是公平和直觉的——如果攻击动画看起来碰到了对手，那就应该命中；如果看起来没碰到，那就不应该命中。没有"我明明打到了怎么没中"的困惑，也没有"我怎么被打了我明明在攻击范围外"的不满。hitbox 的形状和位置必须与视觉攻击效果合理对应——攻击特效伸展到哪里，判定范围就到哪里。

**关键情感时刻**：
- **公平的命中** — 出拳的范围就是判定的范围，玩家直觉理解"这个攻击能打多远"
- **精确的擦边** — 攻击刚好擦过对手模型边缘时命中（或未命中），这种毫厘之间的差异是格斗游戏的深度来源
- **帧精确的响应** — 命中发生在 Active 帧的精确时刻，不是"大概这个时间段内"。高手可以通过帧数据预测判定窗口
- **投射物的可靠命中** — 投射物飞行路径上碰到对手就命中，不会"穿过去"

**支柱对齐**：
- 服务 **Pillar 1: 秒学秒玩** — 命中判定直觉化，攻击看起来打到就打到
- 服务 **Pillar 3: 高手菜鸟都开心** — 新手享受直觉的命中反馈，高手可以利用对 hitbox 形状和时机的精确理解制造优势

> `creative-director` not consulted — Lean mode. Review manually before production.

## Detailed Design

### Core Rules

**1. 碰撞系统架构**

碰撞判定系统使用 Unity 2D 物理引擎的 Trigger 碰撞检测。攻击系统在 Active 阶段创建 hitbox（BoxCollider2D, IsTrigger=true，位于 "Hitbox" 物理层），角色拥有永久的 hurtbox（BoxCollider2D, IsTrigger=true，位于 "Hurtbox" 物理层）。Unity 物理引擎在每次物理步中检测层间碰撞，触发 OnTriggerEnter2D 回调，碰撞系统在此回调中处理命中逻辑。

Layer Collision Matrix 配置：
- "Hitbox" 层仅与 "Hurtbox" 层碰撞
- "Hitbox" 不与 "Hitbox" 碰撞（投射物互相穿过，与攻击系统 GDD 一致）
- "Hurtbox" 不与 "Hurtbox" 碰撞

**2. Hitbox 定义与管理**

Hitbox 的创建、定位和销毁由攻击系统负责（见攻击系统 GDD）。碰撞系统消费攻击系统提供的 hitbox 实例。每个 hitbox 携带：
- `AttackerId` (int): 攻击者角色 ID
- `AttackId` (string): 攻击唯一标识符
- `HitboxCenter` (Vector2): hitbox 中心世界坐标
- `HitboxSize` (Vector2): hitbox 宽度和高度

**3. Hurtbox 定义**

每个存活角色拥有一个永久的 hurtbox，覆盖角色全身：
- 位置：角色中心（与 3C 系统的 CharacterPosition 同步，每帧通过 transform 更新）
- 大小：`HurtboxBaseSize × SilhouetteScale`（SilhouetteScale 来自职业系统）
- 生命周期：角色存活时持续存在；角色被 KO 后 hurtbox 禁用（Collider2D.enabled = false）

| 职业 | SilhouetteScale | Hurtbox 大小（宽×高） |
|------|----------------|---------------------|
| Warrior | 1.2 | Base × 1.2 |
| Rogue | 0.85 | Base × 0.85 |
| Mage | 1.0 | Base × 1.0 |

**4. 命中检测管线**

每物理帧（FixedUpdate, 60Hz）的检测流程：

1. **触发事件接收**：Unity 检测到 Hitbox 层与 Hurtbox 层的 Trigger 重叠 → OnTriggerEnter2D 回调
2. **身份识别**：从 hitbox Collider 读取 AttackerId 和 AttackId，从 hurtbox Collider 读取 TargetId
3. **自伤排除**：如果 AttackerId == TargetId → 跳过（不可自伤）
4. **多次命中检查**：查询攻击系统的 HitTargets 集合，如果 TargetId 已在集合中 → 跳过
5. **命中点计算**：计算 hitbox 和 hurtbox 重叠区域的中心点
6. **命中事件分发**：创建 HitEvent 并通知攻击系统和格斗状态机

**5. 命中事件结构**

```
HitEvent {
    AttackerId: int        // 攻击者角色 ID
    TargetId: int          // 被击者角色 ID
    AttackId: string       // 攻击标识符（用于查 AttackData）
    HitPoint: Vector2      // 命中点（重叠区域中心）
    HitboxCenter: Vector2  // hitbox 中心
    HurtboxCenter: Vector2 // hurtbox 中心
}
```

**6. 同帧互命中处理**

碰撞系统**不做优先级裁定**。同一帧如果两个 hitbox 分别命中了对方的 hurtbox，碰撞系统发送两个独立的 HitEvent，各自包含完整的攻击者和被击者信息。格斗状态机负责决定处理顺序和结果。

**7. 投射物碰撞**

投射物拥有独立的 hitbox（与近战 hitbox 同层，但碰撞系统通过 AttackId 关联到投射物实例）。投射物的碰撞检测：
- 命中 hurtbox → 触发 HitEvent，通知攻击系统销毁投射物
- 碰到实心平台/墙壁 → 通过 "Projectile" 层与 "SolidPlatform" 层的碰撞检测，通知攻击系统销毁投射物（不触发 HitEvent）
- 碰到穿越平台 → 忽略（投射物不受穿越平台影响，与攻击系统 GDD 一致）
- 碰到其他投射物 → 忽略（Hitbox 层不与自身碰撞）

### States and Transitions

碰撞判定系统是无状态的检测层——不维护内部状态机。其行为完全由上游的攻击系统（hitbox 生命周期）和 Unity 物理回调驱动。

| 触发条件 | 行为 |
|---------|------|
| OnTriggerEnter2D（Hitbox ↔ Hurtbox） | 执行命中管线（身份识别 → 验证 → 分发） |
| 角色被 KO | 该角色的 hurtbox 禁用（不再触发碰撞） |
| 新一局开始 | 所有 hurtbox 重新启用 |

### Interactions with Other Systems

| 系统 | 数据流方向 | 接口描述 |
|------|-----------|---------|
| 攻击系统 | 攻击 → 碰撞 | 提供 hitbox 实例（含 AttackerId, AttackId）；维护 HitTargets 集合供碰撞系统查询 |
| 攻击系统 | 碰撞 → 攻击 | 通知命中事件（HitEvent），攻击系统更新 HitTargets 并触发 hitstop |
| 格斗状态机 | 碰撞 → FSM | 发送 HitEvent，FSM 决定 HitStun/Knockback |
| 格斗状态机 | FSM → 碰撞 | 无直接接口（FSM 通过攻击系统间接影响 hitbox 生命周期） |
| 3C系统 | 3C → 碰撞 | 提供 CharacterPosition 用于 hurtbox 定位（通过 transform 同步） |
| 3C系统 | 碰撞 → 3C | 无直接接口（击退由 FSM 委托 3C） |
| 职业系统 | 职业 → 碰撞 | 提供 SilhouetteScale 用于 hurtbox 大小缩放 |
| 伤害计算系统 | 碰撞 → 伤害 | 通过 FSM 转发 HitEvent（碰撞系统不直接调用伤害系统） |
| 专注值系统 | 碰撞 → 专注值 | 通过攻击系统转发命中事件（碰撞系统不直接调用专注值系统） |

## Formulas

**单位系统**: 与 3C 系统一致，1 Unity 单位 = 64 像素，时间以 60Hz 帧为基准。

### 1. Hurtbox 大小计算

`HurtboxSize = HurtboxBaseSize × SilhouetteScale`

| Variable | Symbol | Type | Range | Description |
|----------|--------|------|-------|-------------|
| 基础 hurtbox 大小 | HurtboxBaseSize | Vector2 | (0.6, 1.0) 默认 | 所有角色共享的 hurtbox 基础尺寸（宽×高，单位 u） |
| 轮廓缩放 | SilhouetteScale | float | 0.85–1.2 | 职业系统定义的体型缩放系数 |
| 最终 hurtbox 大小 | HurtboxSize | Vector2 | (0.51, 0.85) 到 (0.72, 1.2) | 角色的实际 hurtbox 碰撞体尺寸 |

**Output Range**: 宽 0.51u–0.72u，高 0.85u–1.2u
**Example**: Warrior (SilhouetteScale=1.2): HurtboxSize = (0.72, 1.2); Rogue (0.85): (0.51, 0.85); Mage (1.0): (0.6, 1.0)

### 2. 命中点计算

```
OverlapMin = Max(HitboxMin, HurtboxMin)
OverlapMax = Min(HitboxMax, HurtboxMax)
HitPoint = (OverlapMin + OverlapMax) / 2
```

其中：
- `HitboxMin = HitboxCenter - HitboxSize / 2`, `HitboxMax = HitboxCenter + HitboxSize / 2`
- `HurtboxMin = HurtboxCenter - HurtboxSize / 2`, `HurtboxMax = HurtboxCenter + HurtboxSize / 2`

| Variable | Symbol | Type | Range | Description |
|----------|--------|------|-------|-------------|
| Hitbox 中心 | HitboxCenter | Vector2 | 场地范围内 | hitbox 碰撞体中心世界坐标 |
| Hitbox 大小 | HitboxSize | Vector2 | (0.3, 0.3)–(3.0, 2.0) | hitbox 宽×高 |
| Hurtbox 中心 | HurtboxCenter | Vector2 | 场地范围内 | hurtbox 碰撞体中心世界坐标 |
| Hurtbox 大小 | HurtboxSize | Vector2 | (0.51, 0.85)–(0.72, 1.2) | hurtbox 宽×高 |
| 命中点 | HitPoint | Vector2 | 重叠区域内 | hitbox 和 hurtbox 重叠区域的中心坐标 |

**Output Range**: 在 hitbox 和 hurtbox 重叠区域内
**Example**: Hitbox 中心 (2.8, 0.8), 大小 (0.6, 0.4); Hurtbox 中心 (3.0, 0.9), 大小 (0.72, 1.2). OverlapMin=(2.5, 0.6), OverlapMax=(3.36, 1.0). HitPoint = (2.93, 0.8)

### 3. AABB 重叠判定

```
IsOverlapping = (HitboxMin.x < HurtboxMax.x) AND (HitboxMax.x > HurtboxMin.x)
           AND (HitboxMin.y < HurtboxMax.y) AND (HitboxMax.y > HurtboxMin.y)
```

| Variable | Symbol | Type | Range | Description |
|----------|--------|------|-------|-------------|
| (同公式 2 的变量) | | | | |
| 是否重叠 | IsOverlapping | bool | — | 两个矩形是否有重叠区域 |

**Output Range**: 布尔值
**Example**: Hitbox [2.5, 3.1] × [0.6, 1.0], Hurtbox [2.64, 3.36] × [0.3, 1.5] → X: 2.5<3.36 AND 3.1>2.64 ✓, Y: 0.6<1.5 AND 1.0>0.3 ✓ → IsOverlapping=true

### 4. 高速穿透风险检查

```
MaxDisplacement = Max(AttackerSpeed, ProjectileSpeed) × dt
MinOverlap = Min(HitboxSize.x, HitboxSize.y, HurtboxSize.x, HurtboxSize.y) / 2
PenetrationRisk = MaxDisplacement > MinOverlap
```

| Variable | Symbol | Type | Range | Description |
|----------|--------|------|-------|-------------|
| 最大速度 | MaxSpeed | float | 0–25.0 u/s | 角色/投射物在碰撞方向上的最大速度 |
| 时间步长 | dt | float | 1/60 s | FixedUpdate 时间步 |
| 最大单帧位移 | MaxDisplacement | float | 0–0.417 u | 单帧最大移动距离 |
| 最小碰撞维度 | MinOverlap | float | 0.15–0.6 u | hitbox 和 hurtbox 最小边长的一半 |
| 穿透风险 | PenetrationRisk | bool | — | 单帧位移是否可能超过碰撞体最小维度 |

**Output Range**: 布尔值（风险评估，非运行时判定）
**Example**: Rogue max speed 6.5 u/s → MaxDisplacement = 6.5/60 = 0.108u; MinOverlap (Rogue hurtbox) = 0.51/2 = 0.255u → 0.108 < 0.255 → PenetrationRisk=false。投射物 speed 15 u/s → 15/60 = 0.25u; 最小 hitbox 0.3/2 = 0.15u → 0.25 > 0.15 → PenetrationRisk=**true**

**设计影响**: 投射物在高速度 + 小 hitbox 场景下存在穿透风险。缓解措施：投射物的 hitbox 最小宽度不应低于 `ProjectileSpeed × dt × 2`（详见 Edge Cases）。

## Edge Cases

**穿透相关**:
- **如果投射物 hitbox 宽度 < ProjectileSpeed × dt × 2（穿透阈值）**: 投射物可能在单帧内完全穿过 hurtbox 而不触发 OnTriggerEnter2D。缓解方案：强制投射物 hitbox 最小宽度 = `Max(HitboxSize.x, ProjectileSpeed × dt × 2)`。设计师定义的 HitboxSize.x 如果低于此阈值，碰撞系统自动扩展到最小安全值。
- **如果角色在 hitbox 创建的同一帧高速移动导致 hitbox 和 hurtbox 错位**: OnTriggerEnter2D 依赖 Unity 物理引擎的碰撞检测时序。hitbox 在 Active 阶段开始帧创建，Unity 在同一物理步中检测重叠。如果角色位置在同一帧被更新（3C 系统的 FixedUpdate 先于碰撞检测），hitbox 位置应该已经同步。时序保证：3C 移动 → hitbox 位置更新 → Unity 碰撞检测 → OnTriggerEnter2D 回调。

**多次命中相关**:
- **如果同一攻击的 hitbox 在多帧中持续与同一 hurtbox 重叠**: OnTriggerEnter2D 仅在重叠**开始**时触发一次（Unity 的行为）。因此多次命中防护主要依赖攻击系统的 HitTargets 集合，与 Unity Trigger 的自然去重行为一致。如果 hitbox 被销毁后重新创建（不正常的攻击数据），OnTriggerEnter2D 会再次触发——此时 HitTargets 检查阻止重复命中。
- **如果同一帧一个 hitbox 同时碰到多个 hurtbox（2v2 或 4 人模式）**: OnTriggerEnter2D 为每个 hurtbox 独立触发。碰撞系统逐个处理，每个都执行完整的验证管线（自伤排除、多次命中检查）。攻击系统的 HitTargets 集合正确追踪所有已命中目标。

**投射物碰撞相关**:
- **如果投射物同一帧碰到 hurtbox 和实心平台**: Unity 为每个碰撞触发独立的回调。碰撞系统区分处理：碰到 hurtbox → HitEvent；碰到实心平台 → 通知攻击系统销毁投射物（无 HitEvent）。如果两个回调同一帧到达，命中 hurtbox 优先处理（与攻击系统 GDD 一致）。
- **如果投射物飞出可视范围但仍在 ProjectileLifetime 内**: hurtbox 已不在碰撞范围内，OnTriggerEnter2D 不会触发。投射物正常存活直到超时。

**角色状态相关**:
- **如果角色在 hitstop 期间被另一个攻击命中**: hurtbox 仍然活跃（hitstop 不影响 hurtbox）。OnTriggerEnter2D 正常触发，碰撞系统发送新的 HitEvent。hitstop 期间 hurtbox 不禁用（与攻击系统 GDD 一致：hitstop 不提供无敌）。
- **如果角色被 KO 后 hurtbox 已禁用，但仍有投射物在飞行**: KO 角色的 hurtbox 的 Collider2D.enabled = false，投射物飞过不触发碰撞。投射物继续飞行直到 ProjectileLifetime 耗尽或碰到实心平台。
- **如果新一局开始时 hurtbox 重新启用**: Collider2D.enabled = true。如果此时场上没有 hitbox（正常情况），不会触发误判。

**数据完整性**:
- **如果 hitbox 的 AttackerId 与任何存活角色的 ID 都不匹配**: 视为数据错误，忽略该碰撞事件。记录警告。
- **如果 hurtbox 的 TargetId 与任何存活角色的 ID 都不匹配**: 同上，忽略并警告。
- **如果 HurtboxBaseSize 的任一分量为 0 或负数**: 视为数据错误，使用硬编码的最小值 (0.3, 0.5)。记录警告。

## Dependencies

| 依赖系统 | 方向 | 类型 | 数据接口 | GDD 状态 |
|---------|------|------|---------|---------|
| 攻击系统 | 上游（硬依赖） | 数据提供 | 提供 hitbox 实例（AttackerId, AttackId, HitboxCenter, HitboxSize）；提供 HitTargets 集合供多次命中查询 | Designed |
| 3C系统 | 上游（硬依赖） | 间接 | 提供 CharacterPosition 通过 transform 同步 hurtbox 位置 | In Review |
| 职业系统 | 上游（硬依赖） | 数据注入 | 提供 SilhouetteScale 用于 hurtbox 大小缩放 | Designed |
| 格斗状态机 | 下游（硬依赖） | 事件通知 | 发送 HitEvent（AttackerId, TargetId, AttackId, HitPoint）触发 HitStun/Knockback 判定 | Designed |
| 伤害计算系统 | 下游（硬依赖） | 间接转发 | 通过 FSM 转发 HitEvent（碰撞系统不直接调用伤害系统） | 未设计 |
| 专注值系统 | 下游（软依赖） | 间接转发 | 通过攻击系统转发命中事件 | 未设计 |
| 音效系统 | 下游（软依赖） | 间接转发 | 通过攻击系统或 FSM 间接获取命中事件 | 未设计 |
| 场地/平台系统 | 上游（软依赖） | 碰撞层 | 定义 "SolidPlatform" 物理层，投射物碰到此层时销毁 | Designed |

**向上提供的接口契约**:
- `HitEvent` 结构体: AttackerId, TargetId, AttackId, HitPoint, HitboxCenter, HurtboxCenter
- 事件: `OnHitDetected(HitEvent)` — 通知攻击系统和格斗状态机
- 查询接口: `IsHurtboxActive(CharacterId)` — 检查角色 hurtbox 是否启用

**双向一致性检查**:
- 攻击系统 GDD: "碰撞判定系统 | 攻击 → 碰撞 | 提供 hitbox 引用（位置、大小）供碰撞检测" ✓ 一致
- 攻击系统 GDD: "碰撞判定系统 | 碰撞 → 攻击 | 通知 hitbox 与 hurtbox 重叠事件" ✓ 一致
- 格斗状态机 GDD: "碰撞判定系统 | 碰撞 → FSM | 命中事件 OnHitReceived(attacker, attackData, hitPoint)" ✓ 一致
- 3C系统 GDD: "碰撞判定系统 | 碰撞 → 3C | 平台碰撞体决定地面检测和着陆判定" — 碰撞系统不处理平台地面检测（3C 自己处理），但投射物与实心平台的碰撞依赖场地系统的物理层 ✓ 一致

## Tuning Knobs

| 旋钮名 | 默认值 | 安全范围 | 调高效果 | 调低效果 | 所属公式 |
|--------|--------|---------|---------|---------|---------|
| HurtboxBaseSize.x | 0.6 u | 0.4–0.8 | 所有角色更容易被命中 | 所有角色更难被命中 | Hurtbox 大小 |
| HurtboxBaseSize.y | 1.0 u | 0.7–1.4 | 角色垂直方向更容易被命中 | 角色跳跃/下落时更难被命中 | Hurtbox 大小 |
| MinHitboxWidth | ProjectileSpeed × dt × 2 | ProjectileSpeed/30 – ProjectileSpeed/15 | 投射物碰撞体更"厚"，穿透风险更低但判定范围偏大 | 投射物碰撞体更精确但有穿透风险 | 穿透防护 |

**旋钮交互警告**:
- `HurtboxBaseSize` 与职业系统的 `SilhouetteScale` 共同决定最终 hurtbox 大小。调基础值时必须考虑对 Rogue（最小）和 Warrior（最大）的影响差异——增大基础值对 Warrior 的影响更大（乘数效应）。
- `HurtboxBaseSize.y` 影响空中战斗——过高的 hurtbox 让跳跃中的角色太容易被对空攻击命中。
- `MinHitboxWidth` 依赖投射物速度——如果攻击系统调了 ProjectileSpeed，必须同步检查碰撞系统的最小宽度约束。

## Visual/Audio Requirements

**命中检测视觉反馈**:

**命中确认特效**:
- 命中点（HitPoint）处生成一个简短的"冲击"特效——能量爆发扩展圆，颜色跟随攻击者职业色
- 特效持续 3-5 帧，尺寸与 hitbox 大小成正比
- 此特效由碰撞系统触发，视觉系统实现

**hitbox/hurtbox 调试可视化**（开发期，不进发布版本）:
- 所有活跃 hitbox 显示为半透明红色矩形
- 所有 hurtbox 显示为半透明绿色矩形
- 命中时 hitbox 短暂高亮（白色闪烁 2 帧）
- 通过 Gizmos 或专用调试 overlay 实现，仅在 Debug 模式下显示
- 此功能对平衡调整至关重要——设计师需要看到实际判定范围

**音频事件**（定义触发事件，音效系统实现）:

碰撞系统不直接触发音效。所有命中音效通过攻击系统的 `OnAttackHit` 事件触发（攻击系统 GDD 已定义）。碰撞系统仅负责检测和通知，音频管线为：碰撞系统 → 攻击系统（OnHitDetected）→ 音效系统（OnAttackHit）。

## UI Requirements

碰撞判定系统不直接产生 UI 元素。调试模式的 hitbox/hurtbox 可视化由碰撞系统提供（见 Visual/Audio Requirements），但非玩家 UI。

无独立的 UI 需求。

## Acceptance Criteria

### Hurtbox 大小

- **GIVEN** HurtboxBaseSize = (0.6, 1.0) 且 SilhouetteScale = 1.2 (Warrior), **WHEN** hurtbox 创建, **THEN** HurtboxSize = (0.72, 1.2)
- **GIVEN** HurtboxBaseSize = (0.6, 1.0) 且 SilhouetteScale = 0.85 (Rogue), **WHEN** hurtbox 创建, **THEN** HurtboxSize = (0.51, 0.85)

### 命中检测

- **GIVEN** Warrior 的 hitbox 在 (2.8, 0.8) 大小 (0.6, 0.4) 和 Rogue 的 hurtbox 在 (3.0, 0.9) 大小 (0.51, 0.85), **WHEN** Unity 执行碰撞检测, **THEN** OnTriggerEnter2D 触发，HitPoint 在重叠区域内
- **GIVEN** Warrior 的 hitbox 在 (5.0, 0.5) 大小 (0.3, 0.3) 和 Rogue 的 hurtbox 在 (1.0, 0.5), **WHEN** Unity 执行碰撞检测, **THEN** 无 OnTriggerEnter2D 触发（不重叠）

### 自伤排除

- **GIVEN** Player 1 的 hitbox (AttackerId=1) 和 Player 1 的 hurtbox (TargetId=1), **WHEN** OnTriggerEnter2D 触发, **THEN** 碰撞系统跳过该事件（AttackerId == TargetId）

### 多次命中防护

- **GIVEN** Warrior GroundAttack 已命中 Rogue（HitTargets = {2}）, **WHEN** 同一攻击的 hitbox 继续与 Rogue 的 hurtbox 重叠, **THEN** OnTriggerEnter2D 不会在同一攻击实例中再次为 Rogue 触发 HitEvent

### 同帧互命中

- **GIVEN** Player 1 和 Player 2 在同一帧互相命中, **WHEN** 两个 OnTriggerEnter2D 回调触发, **THEN** 碰撞系统发送两个独立的 HitEvent，不裁定优先级

### 投射物碰撞

- **GIVEN** 投射物 hitbox 碰到角色 hurtbox, **WHEN** OnTriggerEnter2D 触发, **THEN** 创建 HitEvent 并通知攻击系统销毁投射物
- **GIVEN** 投射物碰到实心平台, **WHEN** 碰撞回调触发, **THEN** 通知攻击系统销毁投射物（不创建 HitEvent）
- **GIVEN** 投射物碰到穿越平台, **WHEN** 碰撞检测执行, **THEN** 无碰撞回调（投射物穿过）

### 角色状态

- **GIVEN** 角色 KO 后 hurtbox 禁用（Collider2D.enabled=false）, **WHEN** 投射物飞过 KO 角色位置, **THEN** 无 OnTriggerEnter2D 触发
- **GIVEN** 角色在 hitstop 期间, **WHEN** 另一个 hitbox 与其 hurtbox 重叠, **THEN** OnTriggerEnter2D 正常触发，HitEvent 正常发送

### 命中点计算

- **GIVEN** Hitbox 中心 (2.8, 0.8) 大小 (0.6, 0.4) 和 Hurtbox 中心 (3.0, 0.9) 大小 (0.72, 1.2), **WHEN** 命中确认, **THEN** HitPoint = 重叠区域中心 ≈ (2.93, 0.8)

### 穿透防护

- **GIVEN** 投射物 ProjectileSpeed = 15.0 u/s 且设计师定义 HitboxSize.x = 0.2, **WHEN** hitbox 创建, **THEN** 碰撞系统强制扩展 hitbox 宽度为 Max(0.2, 15.0/60×2) = Max(0.2, 0.5) = 0.5 u

### 性能

- **GIVEN** 2 人对战，最多 2 个活跃 hitbox + 2 个投射物 hitbox + 2 个 hurtbox, **THEN** 碰撞系统帧耗时 < 0.5ms（Unity 物理 Trigger 检测已包含在物理步中）

> `qa-lead` not consulted — Lean mode. Review manually before production.

## Open Questions

1. **hitbox 可视化是否应作为正式游戏功能提供给玩家？** 大乱斗社区广泛使用 hitbox 显示 mod 进行练习。如果提供官方 hitbox 训练模式，碰撞系统需要支持运行时 hitbox/hurtbox 渲染。（Owner: 设计师，里程碑: VS）
2. **多段攻击（multi-hit attack）的碰撞检测规则？** 当前设计每个攻击实例只有一个 hitbox。某些技能可能需要多段命中（如连续拳击），每段有独立的 hitbox 和命中判定。需要与技能数据库 GDD 协调定义。（Owner: 技能系统设计师，里程碑: 技能数据库 GDD）
3. **帧间插值（tunneling prevention）是否需要在 MVP 中实现？** 公式 4 显示投射物存在穿透风险。当前缓解方案是自动扩展 hitbox 宽度，但如果后续高速技能（如瞬移斩击）出现，可能需要 BoxCast 扫掠检测。（Owner: 技术总监，里程碑: 原型验证后）
