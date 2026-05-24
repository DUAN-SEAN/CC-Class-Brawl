# ADR-0001: Physics Timestep — 60Hz FixedTimestep + Manual Gravity

## Status
Accepted

## Date
2026-05-24

## Engine Compatibility

| Field | Value |
|-------|-------|
| **Engine** | Unity 2022.3.51 LTS |
| **Domain** | Physics |
| **Knowledge Risk** | LOW — version is within LLM training data |
| **References Consulted** | `docs/engine-reference/unity/VERSION.md`, `docs/engine-reference/unity/breaking-changes.md`, `docs/engine-reference/unity/deprecated-apis.md` |
| **Post-Cutoff APIs Used** | None |
| **Verification Required** | Confirm Physics2D settings (autoSyncTransforms, Layer Collision Matrix), confirm Rigidbody2D.interpolation behavior at 60Hz |

## ADR Dependencies

| Field | Value |
|-------|-------|
| **Depends On** | None |
| **Enables** | ADR-0002 (Dual FSM), ADR-0003 (Hitbox/Hurtbox), ADR-0005 (Input System) |
| **Blocks** | 所有 Core 层系统的实现 — 3C, CombatFSM, Collision, Knockback |
| **Ordering Note** | 必须在任何使用物理的系统编码之前 Accepted |

## Context

### Problem Statement
Unity 默认物理频率为 50Hz (`fixedDeltaTime = 0.02s`)，重力通过 `Rigidbody2D.gravityScale` 施加。对于 60fps 格斗游戏，50Hz 物理步导致帧动画（60fps）与物理更新（50fps）不同步，造成命中判定时机不精确。格斗游戏要求帧精确的物理——每个攻击帧、输入缓冲帧、状态转换帧都必须对应一个物理步，以确保确定性。

### Constraints
- 目标帧率 60 FPS，帧预算 16.6ms
- 所有 GDD 公式以帧（1/60s）为单位，不使用 deltaTime
- 角色速度范围：地面 5.0 u/s，冲刺 25.0 u/s，击退最高 ~35 u/s
- 投射物速度最高 15.0 u/s
- Gravity = 32.0 u/s²，TerminalVelocity = 20.0 u/s
- 不同角色状态需要不同的重力倍率（正常 1.0x，快速下落 2.2x）

### Requirements
- 物理步频率必须精确 60Hz
- 重力必须可按状态动态调整倍率
- 速度赋值必须在当前物理步立即生效（击退系统要求帧精确）
- 碰撞检测频率与帧动画一致
- 2 人对战中 3C + 碰撞 + 击退物理总耗时 < 3ms/frame

## Decision

采用 **60Hz FixedTimestep + gravityScale=0 + 手动重力 + 直接 velocity 赋值** 模式：

### 1. FixedTimestep 设置

通过 `Project Settings > Time > Fixed Timestep` 设为 `0.0166667`（1/60 秒）。不通过运行时代码设置，避免脚本执行顺序问题。

`Maximum Allowed Timestep` 设为 `0.0333333`（2 个物理步），防止单渲染帧内堆积过多物理步。

### 2. 重力手动化

所有角色 `Rigidbody2D.gravityScale = 0`。重力在 FixedUpdate 中手动施加：

```csharp
velocity.y -= Gravity * Time.fixedDeltaTime;
velocity.y = Mathf.Max(velocity.y, -TerminalVelocity);
```

理由：不同移动状态需要不同重力倍率（快速下落 2.2x、hitstun 期间重力不同），终端速度需要精确钳制，公式透明可测试。

### 3. 直接 Velocity 赋值

使用 `Rigidbody2D.velocity = newVelocity` 而非 `AddForce`。

理由：`AddForce` 经过 Box2D 速度求解器后才改变速度，存在一帧延迟且结果不确定。格斗游戏需要帧精确的速度控制——击退 SetVelocity 必须在当前物理步立即生效。这与 `deprecated-apis.md` 中"prefer AddForce"的通用建议不同，但在格斗游戏场景下直接赋值是行业惯例（大乱斗、MUGEN 等均使用此模式）。

### 4. Rigidbody2D 插值

所有角色 `Rigidbody2D.interpolation = RigidbodyInterpolation2D.Interpolate`。

理由：物理以 60Hz 离散运行，渲染帧率可能不完全对齐 60Hz。Interpolate 让 Unity 在渲染帧之间线性插值 Transform，消除视觉抖动。不影响物理步本身。

### 5. 执行顺序

```
FixedUpdate 阶段（按 Script Execution Order 排序）:
  → 输入缓冲检查
  → 状态机推进（CombatFSM, 3C MovementState）
  → 速度计算（加速度、摩擦、重力、击退衰减）
  → Rigidbody2D.velocity = newVelocity

Unity 内部物理步:
  → Box2D velocity integration（使用刚赋的 velocity）
  → Collision/Trigger detection
  → 位置更新到 Rigidbody2D

物理回调阶段:
  → OnCollisionEnter2D / OnTriggerEnter2D
  → 碰撞系统命中处理
```

### Architecture Diagram

```
┌─ FixedUpdate (60Hz) ──────────────────────────────────┐
│                                                         │
│  1. Input System → 读取 InputAction 值                 │
│  2. 3C System → 速度计算 + 手动重力                    │
│  3. CombatFSM → 状态推进 + 冻结/释放移动               │
│  4. Knockback → 击退衰减物理                           │
│  5. Rigidbody2D.velocity = newVelocity (所有系统)      │
│                                                         │
│  ── Unity Physics Step (Box2D) ─────────────────────  │
│  6. 速度积分 + 碰撞检测                                │
│  7. OnTriggerEnter2D → 碰撞系统 → HitEvent            │
│                                                         │
└─────────────────────────────────────────────────────────┘
```

### Key Interfaces

- `Time.fixedDeltaTime` = 1/60 (只读，Project Settings 配置)
- `Rigidbody2D.velocity` — 所有速度更新的唯一写入点
- `Rigidbody2D.gravityScale` = 0 — 永远不使用 Unity 自动重力
- `Rigidbody2D.interpolation` = Interpolate — 视觉平滑

## Alternatives Considered

### Alternative 1: Unity 默认 50Hz + gravityScale
- **Description**: 使用 Unity 默认的 50Hz 物理步和 gravityScale 自动重力
- **Pros**: 无需配置，Unity 标准模式
- **Cons**: 50Hz 与 60fps 帧动画不同步；gravityScale 无法按状态调整倍率；帧数据无法精确对应物理步
- **Rejection Reason**: 格斗游戏要求物理步频率与帧动画一致，50Hz 不满足帧精确需求

### Alternative 2: 变长 Update + deltaTime
- **Description**: 在 Update 中使用 `Time.deltaTime` 处理所有物理
- **Pros**: 渲染帧与物理帧完全同步
- **Cons**: 非确定性——不同帧率下物理行为不同；帧计数无法精确；格斗游戏的输入缓冲、取消窗口等帧精确机制无法实现
- **Rejection Reason**: 格斗游戏的核心是帧精确的确定性，变长物理破坏所有 GDD 公式的帧计数基础

## Consequences

### Positive
- 所有系统共享统一的物理时间基准（60Hz, 1/60s）
- 帧数据精确对应物理步——攻击帧、缓冲窗口、取消时机可确定性验证
- 重力可按状态动态调整（快速下落 2.2x、正常 1.0x）
- 公式透明、可测试——所有运动公式为纯数学，不依赖 Unity 物理黑箱
- 击退系统获得帧精确的速度控制

### Negative
- 需要在项目初始化时正确配置 Project Settings
- 直接 velocity 赋值绕过 Box2D 的物理求解器，牺牲了物理"自然感"——但格斗游戏不需要物理自然感
- 60Hz 物理步在高负载时可能导致一帧内执行多个 FixedUpdate（Maximum Allowed Timestep 限制为 2 步）

### Risks
- **视觉抖动**: 物理 60Hz 与渲染帧不完全对齐 → 缓解: Rigidbody2D.interpolation = Interpolate
- **高速投射物穿透**: 投射物 15 u/s + 小 hitbox 可能穿透 hurtbox → 缓解: 碰撞系统 GDD 已定义自动扩展 hitbox 最小宽度
- **Project Settings 被覆盖**: 多人协作时 Project Settings 可能被意外修改 → 缓解: 文档化配置，CI 中验证

## GDD Requirements Addressed

| GDD System | Requirement | How This ADR Addresses It |
|------------|-------------|--------------------------|
| 3c-system.md | "所有运动逻辑在 FixedUpdate 中以 60Hz 固定时间步执行" | fixedDeltaTime = 1/60 |
| 3c-system.md | "Rigidbody2D.gravityScale = 0，重力由公式 4 手动施加" | gravityScale = 0 + 手动重力 |
| 3c-system.md | "所有公式中的 dt = Time.fixedDeltaTime = 1/60" | 统一使用 fixedDeltaTime |
| 3c-system.md | "切勿使用 Unity 默认的 50Hz 物理频率" | 显式设为 60Hz |
| combat-state-machine.md | "帧数以 60Hz 固定时间步为基准（1 帧 = 1/60 秒）" | 60Hz 物理步 |
| combat-state-machine.md | "输入缓冲有效性基于帧计数" | 帧精确的 FixedUpdate 支持 |
| collision-system.md | "每物理帧（FixedUpdate, 60Hz）的检测流程" | 60Hz FixedUpdate 触发 OnTriggerEnter2D |
| knockback-launch-system.md | "时间以 60Hz 帧为基准（dt = 1/60）" | 统一 60Hz 时间基准 |
| knockback-launch-system.md | "Gravity = 32.0 u/s², TerminalVelocity = 20.0 u/s" | 手动重力支持精确重力参数 |

## Performance Implications
- **CPU**: 60Hz 物理步比 50Hz 多 ~20% 物理计算，但 2D 格斗游戏物体数量极少（2 角色 + 少量投射物 + 平台），实测影响 < 0.1ms
- **Memory**: 无额外内存开销
- **Load Time**: 无影响
- **Network**: 不适用（本地多人）

## Migration Plan
不适用——这是新项目的初始架构决策，无需迁移。

## Validation Criteria
- [ ] `Time.fixedDeltaTime` 在运行时返回 ~0.0166667
- [ ] 所有角色 `Rigidbody2D.gravityScale == 0`
- [ ] 手动重力公式：从静止下落 10 帧，垂直速度 = -5.33 u/s（Gravity × 10/60 = 32.0 × 0.1667 = 5.33）
- [ ] 冲刺速度 25.0 u/s 在 6 帧内精确移动 2.5u（25.0 × 6/60 = 2.5）
- [ ] 3C + 碰撞 + 击退系统合计帧耗时 < 3ms
- [ ] 无视觉抖动（Rigidbody2D.interpolation = Interpolate 生效）

## Related Decisions
- ADR-0002: Dual FSM Architecture（依赖本 ADR 的 60Hz 帧基准）
- ADR-0003: Hitbox/Hurtbox Detection（依赖本 ADR 的物理步时序）
- ADR-0005: Input System（输入读取在本 ADR 定义的 FixedUpdate 执行顺序中）
