# ADR-0007: Scene & Game State Management — Two-Scene Architecture + GamePhase FSM

## Status
Accepted

## Date
2026-05-24

## Engine Compatibility

| Field | Value |
|-------|-------|
| **Engine** | Unity 2022.3.51 LTS |
| **Domain** | Scene Management / State |
| **Knowledge Risk** | LOW — version is within LLM training data |
| **References Consulted** | `docs/engine-reference/unity/VERSION.md` |
| **Post-Cutoff APIs Used** | None |
| **Verification Required** | Verify SceneManager.LoadSceneAsync with LoadSceneMode.Single correctly unloads previous scene; verify async operation allows cancellation via AllowSceneActivation; verify PlayerInputManager persists across scene loads with DontDestroyOnLoad |

## ADR Dependencies

| Field | Value |
|-------|-------|
| **Depends On** | ADR-0005 (Input System — PlayerInputManager + device pairing for PlayerSlot management) |
| **Enables** | Match Management System, Character Select UI, Match UI, all game flow |
| **Blocks** | All game flow systems — nothing runs without scene loading and state management |
| **Ordering Note** | Foundation layer. Can be implemented alongside ADR-0001~0005 |

## Context

### Problem Statement
游戏需要从主菜单到角色选择、倒计时、战斗、结果画面的完整状态流转。Unity 场景加载是异步操作，需要处理加载超时和取消。同时，对局管理系统需要多局制（Bo3/Bo5），在局间循环回 Countdown 而非每次都回到 Results。SignalKO 接口需要扩展为 SignalRoundEnd 以支持多局制。

### Constraints
- 2 个 Unity 场景：MenuScene（主菜单）+ GameScene（所有战斗相关状态）
- GameScene 加载后常驻——局间循环不重新加载
- 状态转换必须原子化——转换期间游戏逻辑暂停
- MVP 2 人对战，架构预留 4 人
- PlayerInputManager 需要 DontDestroyOnLoad 跨场景持久化
- 倒计时 3 秒，冻结帧 60 帧，场景加载超时 5 秒

### Requirements
- GamePhase 7 状态 FSM：MainMenu → CharacterSelect → MatchLoading → Countdown → Battle → BattleEnd → Results
- 两场景架构，GameScene 常驻
- PlayerSlot 数据跨状态持久化
- SignalRoundEnd 替代 SignalKO（支持多局制）
- 场景异步加载支持超时和取消
- 事件驱动通知（OnStateChanged）

## Decision

采用 **两场景架构 + GamePhase FSM + SignalRoundEnd 接口 + DontDestroyOnLoad 管理器** 架构：

### 1. 两场景架构

```
MenuScene
  └── MainMenu UI, background, title

GameScene
  ├── CharacterSelect UI Canvas
  ├── Countdown/BattleEnd/Results UI Canvas
  ├── Arena (platforms, blast zone, spawn points)
  ├── Character GameObjects (spawned during MatchLoading)
  ├── Camera Rig
  └── All runtime systems (DamageSystem, KnockbackSystem, etc.)

DontDestroyOnLoad (persists across scenes)
  ├── PlayerInputManager
  ├── GameStateManager (FSM + scene orchestration)
  └── GameManager (top-level coordinator)
```

GameScene 加载策略：
- `MainMenu → CharacterSelect`: `SceneManager.LoadSceneAsync("GameScene", LoadSceneMode.Single)` — MenuScene 自动卸载
- `Results → MainMenu`: `SceneManager.LoadSceneAsync("MenuScene", LoadSceneMode.Single)` — GameScene 自动卸载
- `Results → CharacterSelect`（再来一局）: 不加载场景，仅重置 GameScene 内状态

### 2. GamePhase FSM

```csharp
public enum GamePhase
{
    MainMenu,
    CharacterSelect,
    MatchLoading,
    Countdown,
    Battle,
    BattleEnd,
    Results
}
```

状态转换逻辑封装在 GameStateManager 中，每个状态有明确的进入/退出动作：

| 状态 | 进入动作 | 退出动作 | 活跃系统 |
|------|---------|---------|---------|
| MainMenu | 显示主菜单 UI | 隐藏 UI，开始异步加载 | 仅 UI |
| CharacterSelect | 启用角色选择 UI，允许玩家加入 | 锁定选择 | PlayerInputManager, 角色选择 UI |
| MatchLoading | 初始化 Arena, Character, CombatFSM, 所有战斗系统 | — | 所有系统初始化 |
| Countdown | 冻结 3C 输入，显示 3-2-1 倒计时 | 解冻输入 | 摄像机, UI |
| Battle | 激活所有战斗系统 | — | 全部 |
| BattleEnd | 冻结所有输入，等待 BattleEndFreezeFrames | — | 仅摄像机 |
| Results | 显示结果 UI，等待玩家选择 | 重置或卸载 | 仅 UI |

### 3. SignalRoundEnd 接口（替代 SignalKO）

**这是对 architecture.md 中 IGameState 接口的破坏性更新：**

```csharp
public interface IGameState
{
    GamePhase GetState();
    bool IsBattleActive();

    // 角色选择
    void SetPlayerCharacter(int playerSlot, string characterId);
    PlayerSlot GetPlayerSlot(int playerSlot);
    IReadOnlyList<PlayerSlot> GetAllPlayerSlots();

    // 战斗结束信号（支持多局制）
    void SignalRoundEnd(int winnerIndex, bool matchOver);

    // 事件
    event Action<GamePhase> OnStateChanged;
    event Action<PlayerSlot> OnPlayerJoined;
    event Action<int> OnPlayerLeft;
    event Action OnAllPlayersReady;
}

public struct PlayerSlot
{
    public int PlayerIndex;
    public string CharacterId;
    public bool IsConnected;
    public bool IsReady;
}
```

**SignalRoundEnd 行为：**
- `matchOver = false`: GameStateManager 进入 BattleEnd → 冻结帧结束后回到 **Countdown**（不是 Results）
- `matchOver = true`: GameStateManager 进入 BattleEnd → 冻结帧结束后进入 **Results**

这个设计将对局格式（Bo1/Bo3/Bo5）的决策权交给 MatchManager，GameStateManager 只负责状态流转。

### 4. 异步加载与超时

```csharp
// Unity 2022.3 使用协程（IEnumerator），不支持 Unity 6 的 Awaitable
IEnumerator LoadGameSceneWithTimeout(float timeoutSeconds)
{
    var asyncOp = SceneManager.LoadSceneAsync("GameScene");
    asyncOp.allowSceneActivation = false;

    float elapsed = 0f;
    // allowSceneActivation=false 时 progress 最高只到 0.9f（Unity 引擎设计行为）
    while (asyncOp.progress < 0.9f)
    {
        elapsed += Time.unscaledDeltaTime;
        if (elapsed > timeoutSeconds)
        {
            // 超时：回退到 MainMenu
            Debug.LogError("Scene load timeout");
            TransitionTo(GamePhase.MainMenu);
            yield break;
        }
        yield return null;
    }

    asyncOp.allowSceneActivation = true;
    yield return null; // 等待场景激活
}
```

注意：使用 `Time.unscaledDeltaTime` 而非 `Time.deltaTime`——场景加载期间游戏时间可能暂停。

### 5. 初始化顺序（MatchLoading 状态内）

```
MatchLoading → InitializeAllSystems():
  1. ArenaSystem.Initialize(arenaId) — 实例化平台碰撞体
  2. ClassSystem.Initialize(playerSlots) — 加载 ClassData SO
  3. CharacterControllers.Initialize() — 生成角色 GameObject，注入参数
  4. CombatFSM.Initialize() — 注册基础攻击定义
  5. DamageSystem.Initialize(playerCount)
  6. KnockbackSystem.Initialize(playerCount)
  7. FocusSystem.Initialize(playerCount)
  8. SkillDatabase.Initialize() — 构建查询索引
  9. HUD.Initialize() — 绑定事件监听
  10. TransitionTo(Countdown)
```

如果任何步骤失败，MatchInitTimeout 后回退到 CharacterSelect 并显示错误。

### Architecture Diagram

```
┌─ MenuScene ─────────────┐     ┌─ GameScene ───────────────────────────┐
│                          │     │                                        │
│  MainMenu UI             │────→│  CharacterSelect UI                    │
│                          │     │    ↓ (all players ready)               │
│                          │     │  MatchLoading (system init)            │
│                          │     │    ↓                                   │
│                          │     │  Countdown (3s) → Battle → BattleEnd  │
│                          │     │                          ↓             │
│  MainMenu UI             │←────│  Results ──→ "再来" → CharSelect      │
│                          │     │          └→ "退出" → MenuScene        │
└──────────────────────────┘     └────────────────────────────────────────┘

┌─ DontDestroyOnLoad ────┐
│  PlayerInputManager     │
│  GameStateManager       │
│  GameManager            │
└─────────────────────────┘
```

## Alternatives Considered

### Alternative 1: 每状态一场景（7 场景）
- **Description**: 每个状态一个独立的 Unity 场景
- **Pros**: 场景间完全隔离，每个场景可以独立优化
- **Cons**: CharacterSelect → MatchLoading → Countdown 是连续状态转换，3 次场景加载太慢；场景间共享 GameObject（如角色）需要 DontDestroyOnLoad 或重新创建
- **Rejection Reason**: 违反 Pillar 1 "秒学秒玩"——多次场景加载增加等待时间。两场景架构中 GameScene 常驻，局间循环是纯数据重置，零加载时间

### Alternative 2: 单场景架构
- **Description**: 所有状态在一个场景中，通过启用/禁用 GameObject 切换
- **Pros**: 零场景加载，最快的状态切换
- **Cons**: 主菜单和战斗共享同一场景，内存浪费；所有系统同时存在于内存中；无法利用 Unity 场景的隔离性
- **Rejection Reason**: 主菜单是轻量 UI，战斗场景包含大量运行时对象。分离后主菜单加载极快，且战斗场景不加载时释放内存

## Consequences

### Positive
- 两场景架构平衡了加载性能和内存管理——主菜单轻量，战斗场景常驻
- SignalRoundEnd 接口支持任意对局格式（Bo1/Bo3/Bo5），GameStateManager 不关心对局逻辑
- GamePhase FSM 原子转换保证状态一致性
- DontDestroyOnLoad 的 PlayerInputManager 跨场景持久化，玩家不需要重新配对设备

### Negative
- GameStateManager 作为全局单例，初始化顺序依赖较多（10 个系统按序初始化）
- DontDestroyOnLoad 对象在场景切换时的生命周期管理需要额外注意
- SignalRoundEnd 是对 architecture.md 现有 IGameState 接口的破坏性变更

### Risks
- **异步加载卡在 0.9**: Unity 的 AllowSceneActivation 模式在 progress 达到 0.9 时等待激活许可 → 缓解: 超时检测 + AllowSceneActivation 手动控制
- **场景切换时 MonoBehaviour 引用失效**: GameScene 中的系统引用在场景卸载时被销毁 → 缓解: GameStateManager 在 DontDestroyOnLoad 中，仅持有 GameScene 系统的接口引用（非直接 MonoBehaviour 引用），场景加载后重新绑定
- **PlayerSlot 数据丢失**: DontDestroyOnLoad 对象与 GameScene 对象之间的数据传递 → 缓解: PlayerSlot 数据在 GameStateManager 中持有（也在 DontDestroyOnLoad 中）
- **MatchLoading 初始化顺序错误**: 如果系统初始化顺序不正确（如 CombatFSM 在 ClassSystem 之前），会导致空引用 → 缓解: 使用显式初始化序列 + 超时检测

## GDD Requirements Addressed

| GDD System | Requirement | How This ADR Addresses It |
|------------|-------------|--------------------------|
| game-state-management.md | "7-state FSM: MainMenu → CharacterSelect → MatchLoading → Countdown → Battle → BattleEnd → Results" | GamePhase enum + GameStateManager |
| game-state-management.md | "2 Unity scenes: MenuScene + GameScene" | Two-scene architecture |
| game-state-management.md | "Scene loading with async + timeout" | LoadSceneAsync + SceneLoadTimeout |
| game-state-management.md | "PlayerSlot data persists across states" | PlayerSlot in DontDestroyOnLoad GameStateManager |
| game-state-management.md | "IGameState interface as sole entry point" | IGameState with SignalRoundEnd |
| game-state-management.md | "OnStateChanged event for all systems" | C# event on GameStateManager |
| game-state-management.md | "Atomic state transitions" | TransitionTo() with enter/exit actions |
| match-management-system.md | "SignalKO → SignalRoundEnd(winnerIndex, matchOver)" | IGameState.SignalRoundEnd interface |
| match-management-system.md | "matchOver=false loops to Countdown, matchOver=true goes to Results" | SignalRoundEnd behavior in GameStateManager |

## Performance Implications
- **CPU**: FSM 评估 < 0.01ms/frame; scene loading is async, non-blocking
- **Memory**: GameStateManager + PlayerInputManager in DontDestroyOnLoad < 1KB; GameScene systems sized by ADR-0001~0006
- **Load Time**: MenuScene < 0.5s; GameScene first load < 2s (4 platforms + 2 characters + systems)
- **Network**: 不适用

## Migration Plan
不适用——新项目初始架构决策。

**Breaking Change from architecture.md**: IGameState 接口中的 `SignalKO(winnerPlayerSlot)` 必须替换为 `SignalRoundEnd(int winnerIndex, bool matchOver)`。所有引用 IGameState 的代码需要更新。

## Validation Criteria
- [ ] MainMenu → 按手柄 Start → CharacterSelect 在 2s 内完成场景加载
- [ ] CharacterSelect → 2 名玩家确认 → MatchLoading → Countdown 在 3s 内完成
- [ ] Countdown 显示 3, 2, 1 各持续 1 秒（±2 帧），然后进入 Battle
- [ ] Battle 中 SignalRoundEnd(winner, false) → BattleEnd → Countdown（多局制循环）
- [ ] Battle 中 SignalRoundEnd(winner, true) → BattleEnd → Results
- [ ] Results → "再来一局" → CharacterSelect 在 0.5s 内完成（不重新加载场景）
- [ ] Results → "退出" → MainMenu（卸载 GameScene）
- [ ] 场景加载超时 5s → 自动回退 MainMenu 并显示错误
- [ ] 玩家手柄断开 → 槽位保留，显示"等待连接"
- [ ] OnStateChanged 事件在状态转换后 1 帧内通知所有监听者
- [ ] PlayerInputManager 跨场景持久化，不需要重新配对设备

## Related Decisions
- ADR-0005: Input System — PlayerInputManager 跨场景持久化
- ADR-0004: Skill System — MatchLoading 阶段初始化 SkillDatabase
- ADR-0002: Dual FSM — MatchLoading 阶段初始化 CombatFSM
- ADR-0001: Physics Timestep — Countdown 期间冻结 3C 输入
- Match Management GDD: 定义 SignalRoundEnd 的 matchOver 逻辑
