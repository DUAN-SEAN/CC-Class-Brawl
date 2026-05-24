# ADR-0008: Event Architecture — C# Event Delegates per Interface Provider

## Status
Accepted

## Date
2026-05-24

## Engine Compatibility

| Field | Value |
|-------|-------|
| **Engine** | Unity 2022.3.51 LTS |
| **Domain** | Core (Cross-System Communication) |
| **Knowledge Risk** | LOW — version is within LLM training data |
| **References Consulted** | `docs/engine-reference/unity/VERSION.md` |
| **Post-Cutoff APIs Used** | None |
| **Verification Required** | None |

## ADR Dependencies

| Field | Value |
|-------|-------|
| **Depends On** | All system ADRs (ADR-0001~0007) — events are defined on their respective interface providers |
| **Enables** | All cross-system communication, HUD data binding, audio triggers, VFX triggers |
| **Blocks** | None specifically — but any system that consumes events from other systems needs this pattern established |
| **Ordering Note** | Cross-cutting concern. Must be established before implementation of any system that subscribes to events. |

## Context

### Problem Statement
职业对决有 13+ 个跨系统事件（OnAttackHit, OnKO, OnFocusReady, OnDamagePercentChanged 等），需要统一的通信模式。架构需要决定：使用独立全局 EventBus、C# event 委托、Unity Events、还是 ScriptableObject 事件通道。

### Constraints
- 事件源是明确的——每个事件只有一个生产者（单写者原则）
- 事件消费者可能多个（HUD、音频、VFX、下游系统）
- 事件在 FixedUpdate 中触发，消费者应在同一帧或下一帧响应
- MVP 不需要事件队列或延迟分发
- 性能要求：事件分发 < 0.01ms/event

### Requirements
- 明确的事件所有权——每个事件的声明位置可追溯
- 类型安全——编译期检查事件签名
- 无反射——性能可预测
- 调试友好——可以追踪事件的生产者和消费者
- 不引入外部依赖

## Decision

采用 **C# `event` 委托模式，声明在接口提供者类上** 的架构：

### 1. 核心规则

- **事件声明在接口提供者类上**（实现接口的 MonoBehaviour），不在接口本身
- **事件在接口文档中描述签名**，确保所有实现者签名一致
- **不使用独立全局 EventBus**——每个系统的事件是该系统的一部分
- **不使用 Unity Events（Inspector 绑定）**——代码可追踪，无运行时配置风险
- **不使用 ScriptableObject 事件通道**——增加资产管理复杂度，MVP 不需要

### 2. 事件签名标准

```csharp
// 标准：On + 事件名称 + (发送者关键信息, 事件数据)
// 发送者通常是 playerIndex (int) 或 CharacterId
// 事件数据使用 struct（零 GC）或基础类型

// ✅ 正确的签名示例
event Action<int, float> OnDamagePercentChanged;    // (playerIndex, newPercent)
event Action<int, Vector2> OnKO;                     // (playerIndex, koDirection)
event Action<int, int> OnFocusReady;                 // (playerIndex, unlockedCount)
event Action<GamePhase> OnStateChanged;              // (newPhase)

// ❌ 不允许的签名
// event Action OnSomething;                        // 缺少上下文，消费者无法知道发生了什么
// event Action<object> OnEvent;                    // 无类型安全
// event Action<GameObject> OnHit;                  // 暴露实现细节 (GameObject)
```

### 3. 完整事件清单

按系统分组，每个事件的声明者（生产者）和典型消费者：

#### GameStateManager (IGameState)

| 事件 | 签名 | 消费者 |
|------|------|--------|
| `OnStateChanged` | `Action<GamePhase>` | 所有系统（状态驱动激活/冻结） |
| `OnPlayerJoined` | `Action<PlayerSlot>` | 角色选择 UI, HUD |
| `OnPlayerLeft` | `Action<int>` | 角色选择 UI, HUD |
| `OnAllPlayersReady` | `Action` | 角色选择 UI |

#### CombatFSM (ICombatStateProvider)

| 事件 | 签名 | 消费者 |
|------|------|--------|
| `OnCombatStateChanged` | `Action<int, CombatState, CombatState>` | HUD, 音效, VFX |
| `OnAttackPhaseChanged` | `Action<int, AttackPhase>` | 攻击系统 (hitbox 管理), VFX |

#### AttackSystem (IAttackSystem)

| 事件 | 签名 | 消费者 |
|------|------|--------|
| `OnAttackHit` | `Action<int, AttackData, int>` | 专注值系统, 音效 |
| `OnHitstopStart` | `Action<int>` | 格斗 FSM, VFX |
| `OnHitstopEnd` | `Action` | 格斗 FSM |

#### DamageSystem (IDamageSystem)

| 事件 | 签名 | 消费者 |
|------|------|--------|
| `OnDamagePercentChanged` | `Action<int, float>` | HUD |

#### KnockbackSystem (IKnockbackSystem)

| 事件 | 签名 | 消费者 |
|------|------|--------|
| `OnKO` | `Action<int, Vector2>` | 对局管理, HUD, 音效, VFX |

#### FocusSystem (IFocusSystem)

| 事件 | 签名 | 消费者 |
|------|------|--------|
| `OnFocusReady` | `Action<int, int>` | 技能抽取系统, HUD, VFX |
| `OnFocusChanged` | `Action<int, float, float>` | HUD (points, threshold) |

#### SkillDrawSystem (ISkillDrawSystem)

| 事件 | 签名 | 消费者 |
|------|------|--------|
| `OnDrawReady` | `Action<int, IReadOnlyList<SkillData>>` | 技能选择 UI |
| `OnSkillDrawn` | `Action<int, SkillData>` | 技能装备管理, HUD, 音效 |

#### SkillEquipmentManager (ISkillEquipmentManager)

| 事件 | 签名 | 消费者 |
|------|------|--------|
| `OnSkillEquipped` | `Action<int, int, SkillData>` | HUD, VFX, 音效 |
| `OnSkillUnequipped` | `Action<int, int>` | HUD |

#### MatchManager (IMatchManager)

| 事件 | 签名 | 消费者 |
|------|------|--------|
| `OnRoundEnd` | `Action<int, int[]>` | GameState, HUD |
| `OnMatchEnd` | `Action<int?>` | GameState, HUD |

### 4. 订阅/取消订阅规范

```csharp
// 在 OnEnable 中订阅，OnDisable 中取消订阅
// 这确保对象禁用时不会收到事件（避免空引用异常）

void OnEnable()
{
    _damageSystem.OnDamagePercentChanged += HandleDamageChanged;
    _knockbackSystem.OnKO += HandleKO;
}

void OnDisable()
{
    _damageSystem.OnDamagePercentChanged -= HandleDamageChanged;
    _knockbackSystem.OnKO -= HandleKO;
}
```

### 5. 事件触发时机

所有游戏逻辑事件在 FixedUpdate 中触发（60Hz）。消费者在同一个 FixedUpdate 帧中收到通知。

**例外**：
- `OnStateChanged` — 可能在异步操作完成后触发（如场景加载完成），此时在 Update 回调中触发
- `OnPlayerJoined` / `OnPlayerLeft` — 由 Input System 回调触发（Update 时机）
- `OnDrawReady` / `OnSkillDrawn` — 在 FixedUpdate 中由专注值系统触发

### 6. 同帧多事件处理

如果同一帧中多个事件触发（如一次命中触发 OnAttackHit + OnDamagePercentChanged + OnFocusChanged），消费者应：
- 按顺序处理所有事件
- 只有最后一次更新触发视觉动画（避免动画堆叠）
- 不依赖事件顺序（消费者之间无保证的触发顺序）

## Alternatives Considered

### Alternative 1: 全局 EventBus (Publish/Subscribe)
- **Description**: 使用静态 EventBus 类，字符串或泛型类型标识事件
- **Pros**: 完全解耦——生产者和消费者互不知道；灵活添加新消费者
- **Cons**: 事件所有权不明确——任何类都可以触发任何事件；调试困难——无法追踪事件的来源；字符串类型不安全；反射性能开销
- **Rejection Reason**: 违反单写者原则——全局 EventBus 允许多个类触发同一事件，难以追踪 bug。C# event 的 `+=` 订阅机制已经提供了解耦，同时保持了类型安全和所有权明确性

### Alternative 2: ScriptableObject 事件通道
- **Description**: 使用 SO 作为事件通道（GameEventListener 模式）
- **Pros**: Unity Inspector 可配置订阅；无需代码引用即可连接生产者和消费者
- **Cons**: 增加大量 SO 资产（13+ 事件通道）；Inspector 配置是运行时不可见的隐式依赖；无法在编译期检查事件签名一致性
- **Rejection Reason**: MVP 有 13+ 事件，但生产者和消费者关系固定（HUD 总是订阅 DamageSystem 的 OnDamagePercentChanged）。ScriptableObject 事件通道增加的灵活性在 MVP 中不需要，但增加的资产管理复杂度是实际成本

### Alternative 3: Unity Events (Inspector 绑定)
- **Description**: 使用 UnityEvent 在 Inspector 中配置回调
- **Cons**: 仅支持 Inspector 绑定的对象；无法订阅跨场景的系统；运行时配置可能被意外修改
- **Rejection Reason**: Unity Events 适合预制体内部通信（如按钮点击），不适合跨系统事件分发。13+ 个事件的 Inspector 绑定配置是维护负担

## Consequences

### Positive
- 事件所有权明确——每个事件的生产者是声明它的系统
- C# `event` 是零成本抽象——委托调用是直接函数调用，无反射
- 类型安全——编译期检查签名匹配
- 调试友好——在 IDE 中可以 "Find All References" 追踪事件的消费者
- 不引入外部依赖

### Negative
- 生产者和消费者之间存在直接引用——消费者需要持有生产者的引用才能订阅事件
- 添加新事件需要修改生产者类——不能在不修改代码的情况下添加事件类型
- 13+ 个事件意味着 13+ 对订阅/取消订阅代码需要维护

### Risks
- **忘记取消订阅**: 如果消费者在 OnDisable/OnDestroy 中没有 -= 取消订阅，会收到对已销毁对象的事件调用 → 缓解: 严格遵循 OnEnable/OnDisable 订阅模式，代码审查检查
- **事件触发顺序不确定**: 多个消费者订阅同一事件时，触发顺序不保证 → 缓解: 消费者之间不应有依赖关系；如果需要顺序处理，应该由生产者协调
- **同帧事件风暴**: 一次命中可能触发 5+ 个事件 → 缓解: 每个事件处理应该轻量（< 0.01ms）；视觉动画使用 dirty flag 延迟到 LateUpdate 处理

## GDD Requirements Addressed

| GDD System | Requirement | How This ADR Addresses It |
|------------|-------------|--------------------------|
| architecture.md | "使用 C# event 委托模式（在接口提供者类上声明）" | C# event delegates per provider |
| architecture.md | "13+ 核心事件清单" | Complete event inventory |
| damage-calculation-system.md | "OnHitProcessed, OnDamagePercentChanged 事件" | IDamageSystem events |
| knockback-launch-system.md | "OnKO(CharacterId, KODirection) 事件" | IKnockbackSystem.OnKO |
| focus-system.md | "OnFocusReady, OnFocusChanged 事件" | IFocusSystem events |
| skill-draw-system.md | "OnDrawReady, OnSkillDrawn 事件" | ISkillDrawSystem events |
| skill-equipment-management.md | "OnSkillEquipped, OnSkillUnequipped 事件" | ISkillEquipmentManager events |
| match-management-system.md | "OnRoundEnd, OnMatchEnd 事件" | IMatchManager events |
| game-state-management.md | "OnStateChanged 事件，≤1帧延迟" | IGameState.OnStateChanged |
| battle-hud.md | "HUD 订阅 5+ 上游系统事件" | HUD subscribes to provider events |

## Performance Implications
- **CPU**: C# delegate invocation < 0.001ms/event; 13 events × ~3 subscribers = ~39 invocations per significant game event < 0.04ms
- **Memory**: Event delegate backing fields ~64B per event per provider; negligible
- **GC**: No allocations during event dispatch (Action<T> delegates are pre-allocated on subscription)
- **Network**: 不适用

## Migration Plan
不适用——新项目初始架构决策。

## Validation Criteria
- [ ] 所有 13+ 事件使用 C# `event Action<...>` 声明在接口提供者类上
- [ ] 没有使用全局 EventBus、ScriptableObject 事件通道、或 Unity Events
- [ ] 所有消费者在 OnEnable/OnDisable 中正确订阅/取消订阅
- [ ] 事件签名类型安全——无 object 类型参数
- [ ] 一次命中触发的事件链（OnAttackHit → OnDamagePercentChanged → OnFocusChanged）在 1 帧内完成
- [ ] 事件分发帧耗时 < 0.1ms（所有事件合计）
- [ ] 无事件泄漏——禁用的消费者不收到事件

## Related Decisions
- All ADR-0001~0007 — 事件定义在各系统的接口上
- architecture.md "Data Flow" section — 事件驱动的数据流架构
