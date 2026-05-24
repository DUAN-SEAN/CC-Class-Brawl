# ADR-0003: Hitbox/Hurtbox Detection — Unity Physics2D Triggers + Layer Matrix

## Status
Accepted

## Date
2026-05-24

## Engine Compatibility

| Field | Value |
|-------|-------|
| **Engine** | Unity 2022.3.51 LTS |
| **Domain** | Physics (Collision Detection) |
| **Knowledge Risk** | LOW — version is within LLM training data |
| **References Consulted** | `docs/engine-reference/unity/VERSION.md` |
| **Post-Cutoff APIs Used** | None |
| **Verification Required** | Verify Layer Collision Matrix configuration in Project Settings; verify autoSyncTransforms behavior with Trigger colliders |

## ADR Dependencies

| Field | Value |
|-------|-------|
| **Depends On** | ADR-0001 (Physics Timestep — 60Hz physics step timing), ADR-0002 (Dual FSM — combat state drives hitbox lifecycle) |
| **Enables** | Attack System hitbox management, Combat FSM hit detection pipeline |
| **Blocks** | Attack System implementation, Damage Calculation pipeline |
| **Ordering Note** | Must be Accepted before attack system and damage system implementation |

## Context

### Problem Statement
格斗游戏需要每帧检测攻击 hitbox 与角色 hurtbox 的空间重叠，并将检测结果以结构化事件通知战斗管线。检测必须与 60Hz 物理步同步、帧精确、性能可预测，并且正确处理自伤排除、多次命中过滤、投射物穿透风险等边界情况。

### Constraints
- 检测频率与 60Hz 物理步一致（每 FixedUpdate 一次）
- 同屏最多 2 角色 + 少量投射物（最多 5 个 hitbox + 2 个 hurtbox）
- 投射物速度最高 15 u/s，单帧位移 0.25u
- Trigger Collider 不参与 CCD（连续碰撞检测）
- hitbox 大小和位置由攻击数据驱动（ScriptableObject）

### Requirements
- hitbox 与 hurtbox 重叠时触发命中事件
- 同一攻击对同一目标只命中一次
- 投射物碰到实心平台时销毁（不触发命中事件）
- KO 角色的 hurtbox 不再参与碰撞检测
- 命中点精确到重叠区域中心
- 碰撞系统帧耗时 < 0.5ms

## Decision

采用 **Unity Physics2D Trigger + Layer Collision Matrix** 模式：

### 1. Layer 配置

| Layer | 用途 | 碰撞规则 |
|-------|------|---------|
| Hitbox (8) | 近战和投射物 hitbox | 仅与 Hurtbox、SolidPlatform 碰撞 |
| Hurtbox (9) | 角色受击区域 | 仅与 Hitbox 碰撞 |
| SolidPlatform (11) | 实心平台/墙壁 | 与 Hitbox、Projectile、角色碰撞 |

Layer Matrix 中 Hitbox 不与 Hitbox 碰撞（投射物互相穿过），Hurtbox 不与 Hurtbox 碰撞。

### 2. Hitbox 定位机制

**近战 hitbox**: 角色 Rigidbody2D 的子 GameObject，位置通过 `Transform.localPosition` 设置偏移量，随角色移动自动同步。攻击系统在 Active 阶段开始时通过 `gameObject.SetActive(true)` 激活 hitbox GameObject，Recovery 或取消时 `SetActive(false)`。

**投射物 hitbox**: 独立 GameObject（不在角色层级下），由攻击系统在 Active 阶段开始时实例化，位置在 FixedUpdate 中手动更新（ProjectilePosition += Speed × dt × FacingDir）。投射物销毁时 Destroy 该 GameObject。

理由：近战 hitbox 作为子物体保证位置与角色同步，无 Transform 同步延迟。投射物独立于角色，攻击者被 KO 后投射物继续飞行。

### 3. autoSyncTransforms 配置

`Physics2D.autoSyncTransforms = true`。

理由：碰撞体数量极少（<10），性能影响可忽略。保证 FixedUpdate 中 hitbox 位置变更（localOffset 调整、投射物移动）在物理步中立即反映。消除 Transform 同步时序 bug。

### 4. OnTriggerEnter2D 回调架构

碰撞系统拥有一个 `CollisionDetector` MonoBehaviour 组件注册在角色 GameObject 上。所有 hitbox/hurtbox 的 OnTriggerEnter2D 回调统一路由到此组件：

```
OnTriggerEnter2D(Collider2D other):
  1. 识别自身是 hitbox 还是 hurtbox
  2. 识别对方是 hitbox 还是 hurtbox
  3. 如果是 hitbox 碰到 hurtbox → 执行命中管线
  4. 如果是 hitbox 碰到 SolidPlatform → 仅投射物：通知攻击系统销毁
```

近战 hitbox 碰到 SolidPlatform 的 OnTriggerEnter2D 会被忽略（近战 hitbox 不会飞出角色范围，碰到平台无意义）。

### 5. 命中检测管线

```
OnTriggerEnter2D(Hitbox ↔ Hurtbox):
  1. 身份识别: 从 hitbox 读 AttackerId/AttackId，从 hurtbox 读 TargetId
  2. 自伤排除: AttackerId == TargetId → skip
  3. 多次命中检查: 查询攻击系统 HitTargets 集合
  4. 命中点计算: 重叠区域 AABB 中心
  5. 创建 HitEvent {AttackerId, TargetId, AttackId, HitPoint, HitboxCenter, HurtboxCenter}
  6. 分发: OnHitDetected(HitEvent) → 攻击系统 + Combat FSM
```

OnTriggerEnter2D 仅在重叠**开始**时触发一次，天然防止同一攻击重复命中同一目标。HitTargets 集合提供额外的防护层。

### 6. Hurtbox KO 处理

角色 KO 时 `Collider2D.enabled = false` 禁用 hurtbox。物理引擎立即移除该碰撞体，不再触发任何回调。已排队的回调仍会执行（时序安全）。

新回合开始时 `Collider2D.enabled = true` 重新启用。此时场上无残留 hitbox，不会触发误判。Unity 不会对已存在的重叠触发 OnTriggerEnter2D（只在新重叠开始时触发），所以即使有残留 hitbox 也不会误判。

### 7. 投射物穿透防护

Trigger Collider 不参与 CCD。投射物穿透风险通过最小 hitbox 宽度约束缓解：

`MinHitboxWidth = Max(designerHitboxWidth, ProjectileSpeed × Time.fixedDeltaTime × 2)`

投射物 15 u/s → MinHitboxWidth = Max(设计值, 0.5u)。碰撞系统在 hitbox 创建时自动扩展。

近战 hitbox 不受穿透风险影响——hitbox 是角色子物体，位置通过 Transform 层级同步，不存在"飞过"目标的问题。

### Architecture Diagram

```
┌─ Unity Physics Step (Box2D, within FixedUpdate) ────┐
│                                                       │
│  Physics2D Trigger Overlap Detection:                 │
│    Hitbox (layer 8) ↔ Hurtbox (layer 9)              │
│    Hitbox (layer 8) ↔ SolidPlatform (layer 11)       │
│                                                       │
│  OnTriggerEnter2D callbacks →                         │
│    CollisionDetector:                                  │
│      ├─ Hitbox↔Hurtbox → HitEvent pipeline            │
│      │   1. Self-hit exclusion                        │
│      │   2. Multi-hit check (HitTargets set)          │
│      │   3. HitPoint calculation                      │
│      │   4. OnHitDetected(HitEvent) → Attack + FSM    │
│      └─ Hitbox↔SolidPlatform → Projectile destroy     │
│          (melee hitbox ignored)                        │
│                                                       │
└───────────────────────────────────────────────────────┘
```

### Key Interfaces

- `HitEvent` struct: AttackerId, TargetId, AttackId, HitPoint, HitboxCenter, HurtboxCenter
- `OnHitDetected(HitEvent)` event → attack system + combat FSM
- `IsHurtboxActive(CharacterId)` query
- Hitbox colliders carry `HitboxData` component with AttackerId + AttackId
- Hurtbox colliders carry `HurtboxData` component with TargetId

## Alternatives Considered

### Alternative 1: 手动 AABB 重叠检测
- **Description**: 在 FixedUpdate 中手动遍历所有 hitbox/hurtbox 对，计算 AABB 重叠
- **Pros**: 完全控制检测时序；不依赖 Unity 物理回调；可自定义检测频率
- **Cons**: 需要自行实现 broad phase 优化；需要手动管理碰撞体注册/注销；重复 Unity Physics 已有功能
- **Rejection Reason**: 物体数量极少（<10），Unity 内置 Trigger 检测已经足够高效。手动实现增加代码量但无性能收益

### Alternative 2: Physics2D.OverlapBox 查询 API
- **Description**: 每帧用 OverlapBox 对每个 hitbox 区域查询碰撞
- **Pros**: 同步查询，不依赖回调；检测时机完全可控
- **Cons**: 每帧 N 个 hitbox × M 个查询 = N×M 次 API 调用；OverlapBox 返回所有重叠的 Collider，需要手动过滤 Layer；性能不如 Trigger 回调（Trigger 检测在物理步中自动完成）
- **Rejection Reason**: Trigger 回调天然匹配物理步时序，且 Unity 内部已优化。OverlapBox 是额外查询开销

## Consequences

### Positive
- 利用 Unity 内置物理引擎，无需自行实现碰撞检测
- OnTriggerEnter2D 天然去重（重叠开始时触发一次），简化多次命中防护
- Layer Matrix 在物理引擎层面过滤无效碰撞对，减少回调数量
- hitbox 作为子物体自动跟随角色，无需手动同步位置

### Negative
- 依赖 Unity 物理回调时序——回调在物理步之后执行，命中事件在同帧 FixedUpdate 末尾才到达
- Trigger 不参与 CCD，投射物穿透必须通过增大 hitbox 缓解（而非扫掠检测）
- autoSyncTransforms = true 在大量碰撞体场景下有性能开销（但本项目碰撞体极少，不构成问题）

### Risks
- **Hitbox 定位延迟**: 近战 hitbox 作为子物体，位置依赖 Transform 层级同步 → 缓解: autoSyncTransforms = true 保证即时同步
- **投射物穿透**: 高速投射物 + 小 hitbox 可能穿透 hurtbox → 缓解: 最小宽度强制约束 Max(designWidth, Speed × dt × 2)
- **Layer 配置被意外修改**: Layer Matrix 是 Project Settings，多人协作可能被覆盖 → 缓解: 文档化配置，CI 验证

## GDD Requirements Addressed

| GDD System | Requirement | How This ADR Addresses It |
|------------|-------------|--------------------------|
| collision-system.md | "使用 Unity 2D 物理引擎的 Trigger 碰撞检测" | Physics2D Trigger + Layer Matrix |
| collision-system.md | "Hitbox 层仅与 Hurtbox 层碰撞" | Layer Collision Matrix 配置 |
| collision-system.md | "OnTriggerEnter2D 回调中处理命中逻辑" | CollisionDetector 回调架构 |
| collision-system.md | "投射物碰到实心平台时销毁" | Hitbox ↔ SolidPlatform Layer 碰撞 |
| collision-system.md | "角色被 KO 后 hurtbox 禁用" | Collider2D.enabled = false |
| collision-system.md | "投射物穿透风险通过最小 hitbox 宽度缓解" | MinHitboxWidth 公式 |
| attack-system.md | "hitbox 位置 = 角色位置 + Offset × 面朝方向" | 近战 hitbox 作为子物体 + localOffset |

## Performance Implications
- **CPU**: OnTriggerEnter2D 回调处理 < 0.1ms（2 角色 + 少量投射物）；Layer Matrix 过滤减少无效回调
- **Memory**: HitEvent struct ~80 bytes，每次命中创建一个，栈分配无 GC
- **Load Time**: 无影响
- **Network**: 不适用

## Migration Plan
不适用——新项目初始架构决策。

## Validation Criteria
- [ ] Hitbox 在 Active 阶段创建、Recovery 阶段销毁
- [ ] OnTriggerEnter2D 仅在 Hitbox-Hurtbox 和 Hitbox-SolidPlatform 层对触发
- [ ] 自伤排除正确工作（AttackerId == TargetId 被跳过）
- [ ] 同一攻击对同一目标只触发一次 HitEvent
- [ ] KO 角色 hurtbox 禁用后不再触发碰撞回调
- [ ] 投射物命中 hurtbox 时销毁，碰到 SolidPlatform 时销毁（不触发 HitEvent）
- [ ] 命中点计算在 hitbox/hurtbox 重叠区域内
- [ ] 投射物 hitbox 宽度 >= ProjectileSpeed × dt × 2
- [ ] 碰撞系统帧耗时 < 0.5ms

## Related Decisions
- ADR-0001: Physics Timestep — 碰撞检测在 60Hz 物理步中执行
- ADR-0002: Dual FSM Architecture — Combat FSM 接收 HitEvent 驱动 HitStun/Knockback 转换
