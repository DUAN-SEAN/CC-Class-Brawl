# ADR-0002: Dual FSM Architecture — Movement + Combat FSM

## Status
Accepted

## Date
2026-05-24

## Engine Compatibility

| Field | Value |
|-------|-------|
| **Engine** | Unity 2022.3.51 LTS |
| **Domain** | Core |
| **Knowledge Risk** | LOW — version is within LLM training data |
| **References Consulted** | `docs/engine-reference/unity/VERSION.md` |
| **Post-Cutoff APIs Used** | None |
| **Verification Required** | Validate execution order correctness with two MonoBehaviour FSMs in FixedUpdate |

## ADR Dependencies

| Field | Value |
|-------|-------|
| **Depends On** | ADR-0001 (Physics Timestep — 60Hz frame basis) |
| **Enables** | ADR-0003 (Hitbox/Hurtbox Detection), ADR-0004 (Skill System Data-Driven), ADR-0005 (Input System) |
| **Blocks** | CombatFSM, Attack System, Skill Equipment Management — all combat pipeline systems |
| **Ordering Note** | Must be Accepted before any combat system implementation |

## Context

### Problem Statement
每个角色同时拥有两种不同维度的状态：物理移动状态（地面奔跑、跳跃、空中控制、闪避等）和战斗行为状态（待机、攻击、受击硬直、击退）。这两种状态有独立的转换规则，但需要紧密协调——攻击时冻结移动，击退时委托移动系统施加物理力，技能系统需要动态注册新的战斗状态。如何设计状态机架构来满足这些需求？

### Constraints
- 两个 FSM 在同一个 FixedUpdate 中更新，执行顺序有严格依赖（3C 先于 Combat）
- 技能系统需要在运行时注册新战斗状态（帧数据 + 取消表）
- 输入缓冲窗口 8 帧，需要每帧检查
- 角色数量有限（MVP 2 人，预留 4 人），不需要 ECS 批量处理
- 所有状态转换基于帧计数，不使用 deltaTime

### Requirements
- 移动状态和战斗状态独立管理各自的转换规则
- 战斗状态可以通过接口控制移动系统的冻结/速度覆盖
- 技能系统可以动态注册新的攻击定义（帧数据 + 取消表）
- 攻击恢复帧可以取消到其他攻击或技能（数据驱动的取消表）
- 帧精确的状态转换和输入缓冲

## Decision

采用 **双机并行 + 接口协调 + 混合状态表示** 架构：

### 1. 双 FSM 结构

每个角色拥有两个独立的 MonoBehaviour 组件：

```
Character GameObject
├── MovementController (3C FSM) — MovementState enum
└── CombatFSM — CombatState enum + 攻击定义字典
```

**MovementState** 枚举（编译期固定）：
`{Idle, Running, Jumping, Falling, FastFalling, Dashing, AirDodging, Landing, PlatformDrop}`

**CombatState** 枚举（编译期固定）：
`{Idle, Attacking, HitStun, Knockback}`

CombatState 只管理顶层状态。Attacking 内部的阶段（Startup/Active/Recovery）由当前 AttackData 的帧数据驱动，不是独立状态。

### 2. 协调接口 IMovementController

Combat FSM 通过 `IMovementController` 接口控制 3C：

| CombatState | 3C 行为 | 接口调用 |
|-------------|---------|---------|
| Idle | 完全控制移动 | 无调用 |
| Attacking | 冻结移动 | `FreezeMovement(true)` |
| HitStun | 冻结移动 | `FreezeMovement(true)` |
| Knockback | 委托速度 | `SetVelocity(knockbackVector)` + `FreezeMovement(true)` 在 hitstun 期 |

状态转换时的解冻由 Combat FSM 在 FixedUpdate 末尾统一处理——从非 Idle 状态转入 Idle 时调用 `FreezeMovement(false)`。

### 3. 动态攻击注册

**混合状态表示**：
- 顶层状态用 enum（编译期已知，Inspector 可读，零分配）
- 攻击定义用 `Dictionary<string, StateDefinition>`（运行时可扩展）

```csharp
// readonly struct: 不可变 + 栈分配零 GC（与 ADR-0004 定义一致）
public readonly struct StateDefinition
{
    public readonly string StateId;          // "jab", "skill_fireball" 等
    public readonly int StartupFrames;
    public readonly int ActiveFrames;
    public readonly int RecoveryFrames;
    public readonly CancelEntry[] CancelTable;
    public readonly InputType InputMapping;  // 哪个输入触发此状态

    public StateDefinition(string stateId, int startup, int active, int recovery,
        CancelEntry[] cancelTable, InputType inputMapping)
    {
        StateId = stateId;
        StartupFrames = startup;
        ActiveFrames = active;
        RecoveryFrames = recovery;
        CancelTable = cancelTable;
        InputMapping = inputMapping;
    }
}
```

`RegisterState(StateDefinition)` 将新定义加入字典。技能系统通过 `DeregisterAllSkillStates()` 在回合重置时清除所有注册的技能状态。

格斗状态机在 Attacking 状态中查询当前 AttackData 的帧数据推进阶段，不需要为每个技能创建新的 enum 成员。

### 4. 执行顺序

使用**显式调度**而非 Script Execution Order。一个 `CharacterController` 协调器在单一 FixedUpdate 中按顺序调用两个 FSM：

```
CharacterController.FixedUpdate():
  1. _movementController.FixedUpdateState()  // 3C 先更新
  2. _combatFSM.FixedUpdateState()           // Combat 后更新
  3. _attackSystem.FixedUpdateSystem()       // 攻击 hitbox 生命周期
  4. _knockbackSystem.FixedUpdateSystem()    // 击退物理
```

理由：显式调用顺序在代码中自文档化，不依赖全局项目设置，依赖关系清晰。

### 5. 输入缓冲

使用环形缓冲区（Circular Buffer），固定大小 8 条目：

```csharp
public struct InputEntry
{
    public InputType Type;       // Attack, Dash, Jump, Skill
    public int RecordedFrame;
    public bool Consumed;
}

InputEntry[] _buffer = new InputEntry[8];
int _head;  // 写入指针，自动覆盖最旧数据
```

每帧检查：遍历未消费条目，找到优先级最高且当前状态可接受的输入。已消费和过期条目标记为 Consumed。

### Architecture Diagram

```
┌─ CharacterController.FixedUpdate ────────────────────┐
│                                                       │
│  ┌─ MovementController (3C FSM) ──────────────────┐  │
│  │ MovementState: Idle/Running/Jumping/Falling/... │  │
│  │ 更新移动速度 → Rigidbody2D.velocity             │  │
│  └────────────────────────────────────────────────┘  │
│                                                       │
│  ┌─ CombatFSM ────────────────────────────────────┐  │
│  │ CombatState: Idle/Attacking/HitStun/Knockback  │  │
│  │ 输入缓冲检查 → 状态转换                         │  │
│  │ 协调: FreezeMovement() / SetVelocity()         │  │
│  │ 攻击定义: Dictionary<string, StateDefinition>  │  │
│  └────────────────────────────────────────────────┘  │
│                                                       │
│  ┌─ AttackSystem ─────────────────────────────────┐  │
│  │ Hitbox 生命周期 (由 CombatFSM 阶段驱动)        │  │
│  └────────────────────────────────────────────────┘  │
│                                                       │
│  ┌─ KnockbackSystem ──────────────────────────────┐  │
│  │ 击退物理衰减 (委托 3C SetVelocity)             │  │
│  └────────────────────────────────────────────────┘  │
│                                                       │
└───────────────────────────────────────────────────────┘
```

### Key Interfaces

```csharp
public interface IMovementController
{
    MovementState GetState();
    bool IsGrounded();
    FacingDirection GetFacing();
    void FreezeMovement(bool frozen);
    void SetVelocity(Vector2 velocity);
}

public interface ICombatStateProvider
{
    CombatState GetCurrentState();
    AttackPhase GetCurrentAttackPhase();
    bool CanAcceptInput();
    void RegisterState(StateDefinition stateDef);
    void DeregisterAllSkillStates();
    void ResetToIdle(int playerIndex);
}
```

## Alternatives Considered

### Alternative 1: 单一大型 FSM
- **Description**: 合并移动和战斗为一个状态机，状态如 Idle_OnGround, Attacking_Ground, Attacking_Air, HitStun_Ground 等
- **Pros**: 无需协调，单一状态源
- **Cons**: 状态空间爆炸（9 移动 × 4 战斗 = 36+ 组合状态）；违反单一职责；技能系统注册新状态更复杂
- **Rejection Reason**: 状态空间乘法增长不可维护。移动和战斗是正交关注点，不应合并

### Alternative 2: 分层 FSM (HFSM)
- **Description**: 移动 FSM 作为子状态机嵌套在战斗 FSM 内部
- **Pros**: 层次清晰，自包含
- **Cons**: Unity MonoBehaviour 不天然支持 HFSM；实现复杂度高；格斗游戏的状态平级交互（移动查询战斗，战斗控制移动）不适合严格的父子关系
- **Rejection Reason**: 过度工程化。双机并行 + 接口协调更简洁且满足所有需求

### Alternative 3: 完全数据驱动 FSM（无 Enum）
- **Description**: 所有状态（包括 Idle, Attacking）都用 string/int ID 表示
- **Pros**: 完全运行时可扩展
- **Cons**: 失去编译期类型安全；Inspector 可读性差；顶层状态是固定的，不需要运行时扩展
- **Rejection Reason**: 顶层 CombatState 在 MVP 中固定不变。混合模式（enum + 字典）在类型安全和扩展性之间取得平衡

## Consequences

### Positive
- 移动和战斗关注点分离——修改移动不影响战斗逻辑，反之亦然
- 技能系统通过 RegisterState 动态扩展战斗能力，不需要修改核心 FSM 代码
- IMovementController 接口清晰定义了两个系统之间的契约
- enum 顶层状态保证类型安全和 Inspector 可调试性
- 显式调度优于 Script Execution Order，依赖关系在代码中可见

### Negative
- 两个 FSM 需要仔细协调——解冻遗漏会导致角色永久冻结
- Landing（移动）+ Attacking（战斗）的组合行为需要额外规则：空中攻击着陆时，Combat FSM 仍处于 Attacking.Recovery，MovementState 转入 Landing，但 3C 已被冻结所以着陆延迟的实际效果由 Combat 恢复后触发
- 字典查找增加了少量间接性（性能可忽略，O(1) 查找）

### Risks
- **解冻遗漏**: Combat FSM 状态转换路径未正确调用 FreezeMovement(false) → 缓解: 在 Combat FSM FixedUpdate 末尾统一处理解冻逻辑，不依赖每个转换路径
- **状态组合冲突**: Movement=Landing + Combat=Attacking 的行为未完全定义 → 缓解: GDD 中明确规则——Combat 拥有移动控制权时，Movement FSM 仍跟踪状态但不执行物理
- **动态注册的调试复杂度**: 运行时注册的状态难以在 Inspector 中预览 → 缓解: 添加自定义 Inspector 显示当前已注册的所有攻击定义

## GDD Requirements Addressed

| GDD System | Requirement | How This ADR Addresses It |
|------------|-------------|--------------------------|
| combat-state-machine.md | "格斗状态机与 3C 移动状态机是两个独立的状态机，并行运行" | 双 FSM 架构 |
| combat-state-machine.md | "CombatState=Idle → 3C 完全控制；=Attacking → 冻结 3C" | IMovementController 协调接口 |
| combat-state-machine.md | "通过 ICombatStateProvider.RegisterState() 注册新状态" | Dictionary + StateDefinition 动态注册 |
| combat-state-machine.md | "输入缓冲 8 帧" | 环形缓冲区实现 |
| 3c-system.md | "格斗状态机可以冻结移动、强制位移、修改速度" | IMovementController.FreezeMovement() + SetVelocity() |
| attack-system.md | "攻击类型由 3C MovementState 决定" | Combat FSM 查询 IMovementController.GetState() |
| skill-equipment-management.md | "RegisterState(stateDefinition) 注册技能战斗状态" | StateDefinition 字典注册 |

## Performance Implications
- **CPU**: Dictionary lookup per frame O(1) < 1μs；环形缓冲区遍历 8 条目 < 1μs；两个 FSM 状态更新合计 < 0.5ms
- **Memory**: StateDefinition 字典每角色 4-10 条目 ≈ 1KB；环形缓冲区固定 8 × 16B = 128B
- **Load Time**: 无影响
- **Network**: 不适用（MVP 本地多人）

## Migration Plan
不适用——新项目初始架构决策。

## Validation Criteria
- [ ] Combat=Attacking 时 3C 移动被冻结，角色速度归零
- [ ] Combat 从任何非 Idle 状态转入 Idle 时，3C 正确解冻
- [ ] 技能系统注册新 StateDefinition 后，输入可触发对应攻击
- [ ] DeregisterAllSkillStates() 后，已注册技能的输入不再触发
- [ ] 输入缓冲正确处理 8 帧窗口内的输入匹配和过期
- [ ] Landing + Attacking 组合不会导致死锁或双重物理控制
- [ ] 两个 FSM 合计帧耗时 < 0.5ms

## Related Decisions
- ADR-0001: Physics Timestep — 本 ADR 的 FSM 在 60Hz FixedUpdate 中运行
- ADR-0003: Hitbox/Hurtbox Detection — 碰撞事件驱动 Combat FSM 的 HitStun/Knockback 转换
- ADR-0004: Skill System Data-Driven — 技能的 StateDefinition 数据来源
- ADR-0005: Input System — 输入读取在本 ADR 的输入缓冲之前
