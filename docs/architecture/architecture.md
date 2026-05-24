# 职业对决 (Class Brawl) — Master Architecture

## Document Status
- Version: 1.0-draft
- Last Updated: 2026-05-24
- Engine: Unity 2022.3.51 LTS | URP 2D | C#
- GDDs Covered: 15 MVP systems
- ADRs Referenced: ADR-0001~0010 (see docs/architecture/)
- Technical Director Sign-Off: Pending
- Lead Programmer Feasibility: Pending

## Engine Knowledge Gap Summary
Unity 2022.3.51 LTS is within LLM training data (cutoff ~May 2025).
All engine APIs referenced by GDDs are LOW risk — no post-cutoff verification needed.
Engine reference docs in `docs/engine-reference/unity/` describe Unity 6.3 upgrade path;
changes documented there do not apply to this project's 2022 LTS version.

## System Layer Map

```
┌─────────────────────────────────────────────────────────────┐
│  PRESENTATION LAYER                                         │
│  战斗HUD — 纯被动渲染，无上游数据写入                        │
├─────────────────────────────────────────────────────────────┤
│  FEATURE LAYER                                              │
│  技能抽取系统 | 技能装备管理 | 对局管理系统                    │
├─────────────────────────────────────────────────────────────┤
│  CORE LAYER                                                 │
│  格斗状态机 | 职业系统 | 攻击系统 | 碰撞判定系统              │
│  伤害计算系统 | 击退与击飞系统 | 专注值系统 | 技能数据库       │
├─────────────────────────────────────────────────────────────┤
│  FOUNDATION LAYER                                           │
│  3C系统 | 场地/平台系统 | 游戏状态管理                        │
├─────────────────────────────────────────────────────────────┤
│  PLATFORM LAYER                                             │
│  Unity 2022.3.51 LTS | URP 2D | Box2D Physics | Input System│
└─────────────────────────────────────────────────────────────┘
```

### Foundation Layer

| # | System | Interface | Owns |
|---|--------|-----------|------|
| 1 | 3C系统 | `IMovementController` | MovementState, velocity, facing, input reading, camera |
| 2 | 场地/平台系统 | `IArenaDataProvider` | ArenaConfig (SO), platform colliders, blast zone, camera bounds, spawn points |
| 3 | 游戏状态管理 | `IGameState` | GamePhase FSM, PlayerSlot[], scene loading, countdown timer |

### Core Layer

| # | System | Interface | Owns |
|---|--------|-----------|------|
| 4 | 格斗状态机 | `ICombatStateProvider` | CombatState FSM, AttackPhase, input buffer (8 frames), cancel tables, dynamic skill state registration |
| 5 | 职业系统 | `IClassData` | ClassData (SO), movement params, attack params, visual identity |
| 6 | 攻击系统 | `IAttackSystem` | AttackData, hitbox lifecycle, HitTargets set, projectile pool, hitstop |
| 7 | 碰撞判定系统 | `OnHitDetected` event | HitEvent struct, hitbox/hurtbox AABB, Unity Layer Matrix |
| 8 | 伤害计算系统 | `IDamageSystem` | DamagePercent per character, knockback magnitude calculation |
| 9 | 击退与击飞系统 | `IKnockbackSystem` | Knockback vector, KO detection, knockback physics (velocity decay) |
| 10 | 专注值系统 | `IFocusSystem` | FocusPoints per character, unlock thresholds, unlock event |
| 11 | 技能数据库 | `ISkillDatabase` | SkillData (SO) collection, rarity weights, tags, read-only queries |

### Feature Layer

| # | System | Interface | Owns |
|---|--------|-----------|------|
| 12 | 技能抽取系统 | `ISkillDrawSystem` | DrawState FSM, eligible pool construction, weighted random selection, candidate list |
| 13 | 技能装备管理 | `ISkillEquipmentManager` | SkillSlot[4], FSM state registration/deregistration, skill activation |
| 14 | 对局管理系统 | `IMatchManager` | MatchState, scores, round tracking, Bo1/3/5 format, inter-round reset coordination |

### Presentation Layer

| # | System | Interface | Owns |
|---|--------|-----------|------|
| 15 | 战斗HUD | (pure consumer) | Display state only — no game logic, no upstream writes |

## Module Ownership

### Foundation Layer

| Module | Owns | Exposes | Consumes | Engine APIs |
|--------|------|---------|----------|-------------|
| **3C系统** | MovementState, FacingDirection, velocity, airJumpCount, dashCooldown, coyoteTimer, jumpBufferTimer, MovementParams (injected) | `IMovementController`: GetState(), GetPosition(), GetFacing(), IsGrounded(), FreezeMovement(bool), SetVelocity(Vector2), ModifySpeed(float) | Class system (params), Arena (camera bounds), GameState (freeze signal) | `PlayerInput`, `Rigidbody2D.velocity`, `FixedUpdate` 60Hz, `OnCollisionEnter2D/Exit2D`, `PlatformEffector2D`, `Camera.orthographicSize`, `Vector3.Lerp` |
| **场地/平台系统** | ArenaConfig (SO), platform collider instances, ArenaState, blast zone, camera bounds, spawn points | `IArenaDataProvider`: GetBlastZone(), GetCameraBounds(), GetPlatforms(), GetSpawnPoints(), GetState() | None (pure data provider) | `ScriptableObject`, `BoxCollider2D`, `PlatformEffector2D`, `Instantiate`/`Destroy` |
| **游戏状态管理** | GamePhase FSM, PlayerSlot[], countdown timer, scene load async ops | `IGameState`: GetState(), IsBattleActive(), SignalRoundEnd(winnerIndex, matchOver), SetPlayerCharacter(), OnStateChanged event | None (root authority) | `SceneManager.LoadSceneAsync/UnloadSceneAsync`, Input device events |

### Core Layer

| Module | Owns | Exposes | Consumes | Engine APIs |
|--------|------|---------|----------|-------------|
| **格斗状态机** | CombatState, AttackPhase, attack frame counter, input buffer (8 frames), cancel tables, registered skill states | `ICombatStateProvider`: RegisterState(), DeregisterAllSkillStates(), GetCurrentState(), CanAcceptInput(), GetCurrentAttackPhase(), OnCombatStateChanged event | `IMovementController` (3C), AttackData (from attack/skill), HitEvent (from collision), KnockbackMagnitude/Vector | `FixedUpdate` 60Hz |
| **职业系统** | ClassData (SO) collection, class selection per player | `IClassData`: GetMovementParams(), GetAttackData(type), GetVisualData(), GetSkillPoolTags() | None (data provider) | `ScriptableObject`, `Color` |
| **攻击系统** | Active AttackData, hitbox state, HitTargets set, projectile pool, hitstop counter | `IAttackSystem`: GetCurrentAttack(), OnAttackHit event, OnHitstopStart/End events | `IMovementController` (position, facing, state), `IAttackDataProvider` (skill injection) | `BoxCollider2D.IsTrigger`, Unity Layer "Hitbox", Transform.position |
| **碰撞判定系统** | HitEvent construction, hurtbox enabled state | `OnHitDetected(HitEvent)` event, `IsHurtboxActive(CharacterId)` | Hitbox colliders (attack), Hurtbox colliders, SilhouetteScale (class) | `BoxCollider2D.IsTrigger`, Unity Layer "Hitbox"/"Hurtbox"/"Projectile"/"SolidPlatform", Layer Collision Matrix |
| **伤害计算系统** | DamagePercent per character | `IDamageSystem`: GetDamagePercent(), ResetDamage(), OnDamagePercentChanged event | HitEvent (from FSM), AttackData (BaseDamage, BaseKnockback, HitStunFrames) | None (pure computation) |
| **击退与击飞系统** | Knockback velocity, knockback phase, KO state | `IKnockbackSystem`: GetKnockbackState(), GetKnockbackVelocity(), OnKO event | KnockbackMagnitude (damage), HitPoint (collision), `IArenaDataProvider.GetBlastZone()`, `IMovementController.SetVelocity()` | `Rigidbody2D.velocity`, `Mathf.Sign`, `Vector2.Normalize`, `FixedUpdate` 60Hz |
| **专注值系统** | FocusPoints per character, UnlockedCount, threshold calculation | `IFocusSystem`: GetFocusPoints(), GetUnlockThreshold(), GetUnlockedCount(), OnFocusReady event, OnFocusChanged event | OnAttackHit event (attack system) | None (pure computation) |
| **技能数据库** | SkillData (SO) collection, rarity weights, tags | `ISkillDatabase`: GetAllSkills(), GetSkillById(), GetSkillsByRarity(), GetSkillsByTag() | None (read-only data) | `ScriptableObject`, `Sprite` |

### Feature Layer

| Module | Owns | Exposes | Consumes | Engine APIs |
|--------|------|---------|----------|-------------|
| **技能抽取系统** | DrawState per character, AlreadyDrawnSkillIds, eligible pool, candidate list | `ISkillDrawSystem`: GetAlreadyDrawnSkillIds(), GetDrawState(), ResetDrawState(), OnDrawReady event, OnSkillDrawn event | `ISkillDatabase`, OnFocusReady (focus), IClassData (tags), OnRoundStart | `System.Random` (weighted selection) |
| **技能装备管理** | SkillSlot[4] per character, FSM registered skill states | `ISkillEquipmentManager`: GetEquippedSkills(), GetSkillSlot(), GetEquippedCount(), OnSkillEquipped/Unequipped events | OnSkillDrawn (draw), `ICombatStateProvider.RegisterState/Deregister`, OnRoundStart/End | None |
| **对局管理系统** | MatchState, scores, round counter, round result | `IMatchManager`: GetMatchState(), GetScores(), GetCurrentRound(), OnRoundEnd event, OnMatchEnd event | OnKO (knockback), `IGameState`, ResetDamage/ResetFocus/ResetPosition/ResetToIdle (downstream) | None |

### Presentation Layer

| Module | Owns | Exposes | Consumes | Engine APIs |
|--------|------|---------|----------|-------------|
| **战斗HUD** | Display state (ephemeral) | None (pure consumer) | All upstream events/queries | UI Toolkit (UXML/USS), Canvas Scaler, animation system |

### Dependency Diagram

```
                    ┌──────────────────┐
                    │   游戏状态管理    │
                    │   IGameState     │
                    └───┬──────────────┘
            ┌───────────┼─────────────┐
            ▼           ▼               ▼
    ┌───────────┐ ┌───────────┐ ┌──────────────────┐
    │  3C系统   │ │ 场地/平台  │ │   对局管理系统    │
    │IMovement  │ │IArenaData │ │   IMatchManager   │
    └─────┬─────┘ └─────┬─────┘ └──┬────────────────┘
    ┌─────┴──────────────┴──────────┴─────────────────────┐
    │                    CORE LAYER                        │
    │  职业系统 ──→ 格斗状态机 ←── 攻击系统 ←── 碰撞系统   │
    │       │         │    │          │                    │
    │       │    伤害计算  击退系统    │                    │
    │       │    专注值系统           │                    │
    │       │    技能数据库           │                    │
    └───────┼─────────┼──────────────┼─────────────────────┘
            │   技能抽取系统 → 技能装备管理                  │
            │          PRESENTATION: 战斗HUD                │
      ClassData injected at match init
```

## Data Flow

### Frame Update Path (60Hz FixedUpdate)

1. Input System → 3C系统: 读入 InputAction 值 → 运动状态更新
2. 3C系统 → Rigidbody2D: 手动重力+加速度+速度上限 → `Rigidbody2D.velocity = newVelocity`
3. Unity Physics Step (自动): 移动 Rigidbody2D, 碰撞回调
4. 碰撞判定系统: `OnTriggerEnter2D` → `OnHitDetected(HitEvent)` → 格斗状态机
5. 格斗状态机 FSM 评估: InputBuffer → CombatState转换 → 攻击系统 hitbox 激活/停用
6. Hit Pipeline (命中时): FSM→伤害计算(DamagePercent, KnockbackMagnitude) → 击退系统→3C(SetVelocity) | 专注值积累 | HitTargets+Hitstop
7. 专注值阈值检查 → OnFocusReady → 技能抽取
8. Camera: Lerp toward target, clamp to arena bounds
9. HUD: 读取上游状态 → 更新视觉

### Event System

使用 C# `event` 委托模式（在接口提供者类上声明）。不使用独立全局 EventBus。

**核心事件清单**:
- `OnAttackHit(AttackData, TargetId)` — 攻击系统→伤害+专注值+hitstop
- `OnKO(CharacterId, KODirection)` — 击退系统→对局管理+HUD
- `OnFocusReady(CharacterId, UnlockedCount)` — 专注值→技能抽取+HUD
- `OnDamagePercentChanged(CharacterId, newPercent)` — 伤害→HUD
- `OnSkillDrawn(CharacterId, SkillData)` — 技能抽取→装备管理+HUD
- `OnSkillEquipped(CharacterId, SlotIndex, SkillData)` — 装备管理→HUD+能量视觉
- `OnStateChanged(GamePhase)` — 游戏状态→所有系统
- `OnRoundStart` — 对局管理→伤害+专注值+技能+3C+FSM 重置
- `OnRoundEnd(winnerIndex, scores)` — 对局管理→HUD
- `OnMatchEnd(winnerIndex or draw)` — 对局管理→HUD+对局UI

### Save/Load

MVP 无持久化需求。所有状态为会话级。
- DamagePercent/FocusPoints: 单回合，回合重置时清零
- SkillSlots: 跨回合保留（对局内），比赛结束清零
- ClassData/SkillData: ScriptableObject 只读资产

### Initialisation Order

GameScene loaded → 1. ArenaSystem.Initialize → 2. ClassSystem.Initialize → 3. CharacterController.Initialize (inject params) → 4. CombatFSM.Initialize (base attacks) → 5. SkillDatabase.Initialize → 6. MatchManager.Initialize → 7. HUD.Initialize → 8. GameState→Countdown→Battle

## API Boundaries

```csharp
// === FOUNDATION LAYER ===

public interface IMovementController
{
    MovementState GetState();
    Vector2 GetPosition();
    FacingDirection GetFacing();
    bool IsGrounded();
    void FreezeMovement(bool frozen);
    void SetVelocity(Vector2 velocity);
    void ModifySpeed(float multiplier);
    event Action OnJump;
    event Action OnLand;
    event Action OnDashStart;
}

public interface IArenaDataProvider
{
    BoundsData GetBlastZone();
    BoundsData GetCameraBounds();
    IReadOnlyList<PlatformData> GetPlatforms();
    IReadOnlyList<SpawnPointData> GetSpawnPoints();
    ArenaState GetState();
}

public interface IGameState
{
    GamePhase GetState();
    bool IsBattleActive();
    void SetPlayerCharacter(int playerSlot, string characterId);
    event Action<GamePhase> OnStateChanged;
    event Action<PlayerSlot> OnPlayerJoined;
    event Action<int> OnPlayerLeft;
}

// === CORE LAYER ===

public interface ICombatStateProvider
{
    CombatState GetCurrentState();
    AttackPhase GetCurrentAttackPhase();
    bool CanAcceptInput();
    void RegisterState(StateDefinition stateDef);
    void DeregisterAllSkillStates();
    void ResetToIdle(int playerIndex);
    event Action<CombatState, CombatState> OnCombatStateChanged;
}

public interface IClassData
{
    string GetClassId();
    MovementParams GetMovementParams();
    AttackData GetAttackData(AttackType type);
    VisualData GetVisualData();
    string[] GetSkillPoolTags();
}

public interface IAttackSystem
{
    AttackData GetCurrentAttack();
    bool IsHitstopActive();
    event Action<AttackData, int> OnAttackHit;
    event Action<int> OnHitstopStart;
    event Action OnHitstopEnd;
}

public interface IDamageSystem
{
    float GetDamagePercent(int playerIndex);
    int GetDisplayPercent(int playerIndex);
    void ResetDamage(int playerIndex);
    void ResetAll();
    event Action<int, float> OnDamagePercentChanged;
}

public interface IKnockbackSystem
{
    KnockbackState GetKnockbackState(int playerIndex);
    Vector2 GetKnockbackVelocity(int playerIndex);
    event Action<int, Vector2> OnKO;
}

public interface IFocusSystem
{
    float GetFocusPoints(int playerIndex);
    float GetUnlockThreshold(int playerIndex);
    int GetUnlockedCount(int playerIndex);
    void ResetFocus(int playerIndex);
    void ResetAll();
    event Action<int, int> OnFocusReady;
    event Action<int, float, float> OnFocusChanged;
}

public interface ISkillDatabase
{
    IReadOnlyList<SkillData> GetAllSkills();
    SkillData GetSkillById(string skillId);
    IReadOnlyList<SkillData> GetSkillsByRarity(Rarity rarity);
    IReadOnlyList<SkillData> GetSkillsByTag(string tag);
    int GetSkillCount();
}

// === FEATURE LAYER ===

public interface ISkillDrawSystem
{
    HashSet<string> GetAlreadyDrawnSkillIds(int playerIndex);
    DrawState GetDrawState(int playerIndex);
    void ResetDrawState(int playerIndex);
    void ResetAll();
    event Action<int, IReadOnlyList<SkillData>> OnDrawReady;
    event Action<int, SkillData> OnSkillDrawn;
}

public interface ISkillEquipmentManager
{
    SkillData[] GetEquippedSkills(int playerIndex);
    SkillSlot GetSkillSlot(int playerIndex, int slotIndex);
    int GetEquippedCount(int playerIndex);
    void ResetEquipment(int playerIndex);
    void ResetAll();
    event Action<int, int, SkillData> OnSkillEquipped;
    event Action<int, int> OnSkillUnequipped;
}

public interface IMatchManager
{
    MatchManagerState GetMatchState();
    int[] GetScores();
    int GetCurrentRound();
    event Action<int, int[]> OnRoundEnd;
    event Action<int?> OnMatchEnd;
}
```

## ADR Audit

**Existing ADRs**: 10 (all Accepted)
- ADR-0001: Physics Timestep — 60Hz FixedTimestep + Manual Gravity ✅ Accepted
- ADR-0002: Dual FSM Architecture — Movement + Combat FSM ✅ Accepted
- ADR-0003: Hitbox/Hurtbox Detection — Unity Physics2D Triggers + Layer Matrix ✅ Accepted
- ADR-0004: Skill System Data-Driven Architecture — SO + Dynamic FSM Registration ✅ Accepted
- ADR-0005: Input System — Unity Input System + Per-Player Device Mapping ✅ Accepted
- ADR-0006: Damage & Knockback Pipeline — Pure Computation + Blast Zone KO ✅ Accepted
- ADR-0007: Scene & Game State Management — Two-Scene + GamePhase FSM ✅ Accepted
- ADR-0008: Event Architecture — C# Event Delegates per Interface ✅ Accepted
- ADR-0009: Focus & Skill Draw Pipeline — Event-Driven Roguelike Skill Acquisition ✅ Accepted
- ADR-0010: Match & Round Lifecycle — BoN Format + Round Reset Coordination ✅ Accepted

**Traceability Coverage**: ~55% (10 ADRs cover all Foundation/Core systems and key Feature systems; remaining gaps: camera, projectile, UI architecture)

## Required ADRs

### Must Create Before Coding (Foundation & Core)

| # | Title | Covers GDDs | Unblocks |
|---|-------|-------------|----------|
| ADR-0001 | Physics Timestep — 60Hz FixedTimestep + Manual Gravity | 3C, CombatFSM, Collision, Knockback | All physics-dependent systems |
| ADR-0002 | Dual FSM Architecture — Movement + Combat FSM | 3C, CombatFSM, Attack, SkillEquip | Combat pipeline |
| ADR-0003 | Hitbox/Hurtbox Detection — Unity Physics2D Triggers + Layer Matrix | Collision, Attack, Arena | Hit pipeline |
| ADR-0004 | Skill System Data-Driven Architecture — SO + Dynamic FSM Registration | SkillDB, SkillDraw, SkillEquip, Attack | Core unique mechanic |
| ADR-0005 | Input System — Unity Input System + Per-Player Device Mapping | 3C, GameState, SkillEquip, SkillDraw | All input-dependent systems |

### Should Create Before Relevant System Is Built

| # | Title | Covers | Priority |
|---|-------|--------|----------|
| ADR-0011 | Camera Strategy | 3C Camera | Medium |
| ADR-0012 | Projectile System — Object Pooling | Attack, Collision | Medium |
| ADR-0013 | UI Architecture — UI Toolkit vs UGUI | BattleHUD, GameState | Medium |

### Can Defer to Implementation

| # | Title | Covers | Priority |
|---|-------|--------|----------|
| ADR-0014 | Debug Visualization for Hitboxes | Collision Gizmos | Low |
| ADR-0015 | SO Loading Strategy (Resources vs Addressables) | SkillDB loading | Low |

## Architecture Principles

1. **数据驱动优先** — 所有游戏数值存储在 ScriptableObject 中，代码读取不硬编码。新职业/技能/场地只需创建新 SO，不改代码。
2. **接口隔离** — 系统间通过 C# interface 通信，不直接引用 MonoBehaviour。每个系统只暴露调用者需要的最小接口。
3. **事件驱动解耦** — 下游系统通过事件订阅上游变化。HUD 和视觉系统永远不写入游戏逻辑状态。
4. **60Hz 帧精确** — 所有游戏逻辑（移动、碰撞、FSM、击退）在 FixedUpdate 中以帧为单位执行，不依赖 deltaTime。
5. **单写者原则** — 每个数据（DamagePercent, FocusPoints, velocity）只有一个系统拥有写入权。其他系统通过接口查询或事件监听。

## Open Questions

| ID | Summary | Priority | Resolution Path |
|----|---------|----------|-----------------|
| QQ-01 | HUD 布局冲突：art-bible 顶部 vs battle-hud 底部 | High | 协调统一后更新 battle-hud GDD |
| QQ-02 | 职业主色不统一：art-bible vs class-system/battle-hud | High | 以 art-bible 为准，propagate-design-change |
| QQ-03 | SignalKO → SignalRoundEnd 接口变更 | Resolved | ADR-0007 + architecture.md updated |
| QQ-04 | 技能选择不暂停游戏的用户体验验证 | Medium | 原型阶段测试 |
| QQ-05 | 击退增长系数 0.05 的 KO 感知是否足够 | Medium | 原型阶段调优 |
| QQ-06 | Unity 项目骨架未创建（无 .csproj/.sln） | High | Technical Setup 阶段创建 |
