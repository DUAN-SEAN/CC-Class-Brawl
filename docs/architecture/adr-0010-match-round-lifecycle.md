# ADR-0010: Match & Round Lifecycle — BoN Format + Round Reset Coordination

## Status
Accepted

## Date
2026-05-24

## Engine Compatibility

| Field | Value |
|-------|-------|
| **Engine** | Unity 2022.3.51 LTS |
| **Domain** | Feature (Match Orchestration) |
| **Knowledge Risk** | LOW — version is within LLM training data |
| **References Consulted** | `docs/engine-reference/unity/VERSION.md` |
| **Post-Cutoff APIs Used** | None |
| **Verification Required** | None |

## ADR Dependencies

| Field | Value |
|-------|-------|
| **Depends On** | ADR-0007 (Scene & Game State — IGameState, SignalRoundEnd, GamePhase FSM), ADR-0006 (Damage Pipeline — IKnockbackSystem.OnKO), ADR-0009 (Focus & Skill Draw — reset interfaces), ADR-0008 (Event Architecture — event pattern) |
| **Enables** | Battle HUD (scores, round display), Results UI, audio (round/match end sounds) |
| **Blocks** | Results UI, inter-round flow, multi-round match progression |
| **Ordering Note** | Feature layer. Must be Accepted before UI implementation and match flow coding. |

## Context

### Problem Statement
对局管理系统是比赛层级的编排器，负责追踪多局对战的回合进程——从第一局到最终胜负判定。它接收击退系统的 KO 信号，判定单局胜负，累积比分，协调回合间数据重置，并通知游戏状态管理进行状态流转。核心挑战在于：多局制（Bo1/Bo3/Bo5）的比分逻辑、双 KO 处理、回合间数据重置的精确协调（部分重置、部分保留）、以及与 GameStateManager SignalRoundEnd 接口的正确对接。

### Constraints
- MatchManager 是 GameScene 内的 MonoBehaviour（非 DontDestroyOnLoad——场景卸载时一起销毁）
- GameStateManager 在 DontDestroyOnLoad 中，跨场景持久化
- 回合间不重新加载场景——GameScene 常驻，纯数据重置
- 回合间已装备技能保留（ADR-0009 确立的跨局成长弧线）
- 双 KO 同帧处理——两个 OnKO 事件在同一帧到达
- 冻结帧由 GameStateManager 管理（BattleEnd 阶段），MatchManager 不控制冻结

### Requirements
- Bo1/Bo3/Bo5 支持，赛制从 CharacterSelect 配置
- WinsNeeded = Ceil(MatchFormat / 2.0)
- 回合间：伤害、专注值、位置、速度、击退、格斗状态重置；技能保留
- 对局间：全部重置
- 双 KO 时双方各 +1 分，可能导致平局
- 内部状态机防止异常状态下的事件处理

## Decision

采用 **MatchManager 编排器 + MatchFormulas 纯计算 + 内部 FSM + 回合重置协调** 架构：

### 1. MatchFormulas — 无状态纯计算

```csharp
public static class MatchFormulas
{
    public static int CalculateWinsNeeded(int matchFormat)
        => Mathf.CeilToInt(matchFormat / 2f);

    public static int CalculateMaxRounds(int winsNeeded)
        => winsNeeded * 2 - 1;

    public static bool IsMatchOver(int[] scores, int winsNeeded)
        => scores[0] >= winsNeeded || scores[1] >= winsNeeded;

    public static bool IsDraw(int[] scores, int winsNeeded)
        => scores[0] >= winsNeeded && scores[1] >= winsNeeded;

    public static int? GetWinner(int[] scores, int winsNeeded)
    {
        if (!IsMatchOver(scores, winsNeeded)) return null;
        if (IsDraw(scores, winsNeeded)) return null;
        return scores[0] >= winsNeeded ? 0 : 1;
    }

    public static int ClampMatchFormat(int format)
    {
        if (format <= 0) return 1;
        if (format % 2 == 0) return format + 1;  // 2→3, 4→5
        return Mathf.Min(format, 5);
    }
}
```

### 2. MatchPhase 内部 FSM

```csharp
public enum MatchPhase
{
    Inactive,          // 未初始化或已结束
    WaitingForBattle,  // 等待 GameState 进入 Battle
    RoundInProgress,   // 战斗进行中，监听 KO
    RoundResolved,     // KO 已处理，等待 BattleEnd 冻结帧结束
    MatchComplete      // 比赛结束，等待 GameState 进入 Results
}
```

**状态转换：**

```
Inactive ──Initialize(config)──→ WaitingForBattle
WaitingForBattle ──OnStateChanged(Battle)──→ RoundInProgress
RoundInProgress ──OnKO──→ RoundResolved
RoundResolved ──OnStateChanged(Countdown)──→ WaitingForBattle  [matchOver=false]
RoundResolved ──OnStateChanged(Results)──→ MatchComplete       [matchOver=true]
```

**状态保护**：
- RoundResolved 状态下收到的额外 OnKO 被忽略
- WaitingForBattle 状态下收到的 OnKO 被忽略
- MatchComplete 状态下所有事件被忽略

### 3. KO 处理与比分更新

```csharp
void HandleKO(int playerIndex, Vector2 koDirection)
{
    if (_phase != MatchPhase.RoundInProgress) return;

    // 被 KO 的玩家的对手获胜
    int winnerIndex = 1 - playerIndex;  // MVP 2人对战
    _roundWinners.Add(winnerIndex);
    _koCountThisFrame++;

    // 帧内双 KO 判定（两个 OnKO 在同一帧到达）
    // 第一个 KO 时设 phase = RoundResolved 并标记 _pendingKO
    // 如果同一帧有第二个 KO，_koCountThisFrame == 2
}
```

**帧内处理流程（FixedUpdate 末尾）：**

```
if (_koCountThisFrame > 0):
    // 处理所有本帧 KO
    if _koCountThisFrame == 1:
        // 单 KO：胜者得 1 分
        scores[_roundWinners[0]]++
    else if _koCountThisFrame >= 2:
        // 双 KO：双方各 +1
        scores[0]++
        scores[1]++

    currentRound++
    _koCountThisFrame = 0

    matchOver = MatchFormulas.IsMatchOver(scores, winsNeeded)
    _gameState.SignalRoundEnd(winnerIndex, matchOver)

    // 如果双 KO 导致 matchOver，winnerIndex 传 null 的处理
    // SignalRoundEnd 签名是 (int winnerIndex, bool matchOver)
    // 双 KO 但 matchOver=false: 传 0 或任意值（GameStateManager 不关心）
    // 双 KO 且 matchOver=true: 传任意值，Results 画面显示平局

    _phase = MatchPhase.RoundResolved
```

**关于双 KO 的 winnerIndex**：

SignalRoundEnd 的 `winnerIndex` 在双 KO 时语义不清。解决：
- 如果 matchOver=false：传 0（任意值，GameStateManager 只看 matchOver）
- 如果 matchOver=true（双 KO 导致平局）：传 -1（约定 -1 表示平局），Results UI 可据此显示平局

建议在 IGameState 接口文档中补充约定：`winnerIndex = -1 表示平局`。

### 4. 回合重置协调

当 MatchManager 检测到 GameState 进入 Countdown（且自身处于 RoundResolved）时，执行回合间重置：

```csharp
void HandleStateChanged(GamePhase newPhase)
{
    if (newPhase == GamePhase.Countdown && _phase == MatchPhase.RoundResolved)
    {
        CoordinateRoundReset();
        _phase = MatchPhase.WaitingForBattle;
    }
    else if (newPhase == GamePhase.Battle && _phase == MatchPhase.WaitingForBattle)
    {
        _phase = MatchPhase.RoundInProgress;
    }
    else if (newPhase == GamePhase.Results && _phase == MatchPhase.RoundResolved)
    {
        _phase = MatchPhase.MatchComplete;
    }
}

void CoordinateRoundReset()
{
    for (int i = 0; i < _playerCount; i++)
    {
        _damageSystem.ResetDamage(i);
        _focusSystem.ResetForNewRound(i);
        _skillDrawSystem.ResetForNewRound(i);
        // SkillEquipmentManager 不重置——技能跨局保留
        _movementControllers[i].ResetPosition(_arena.GetSpawnPoints()[i]);
        _movementControllers[i].SetVelocity(Vector2.zero);
        _combatFSMs[i].ResetToIdle(i);
        _knockbackSystem.ResetKnockback(i);
    }
}
```

**初始化顺序（MatchLoading 阶段，扩展 ADR-0007 的 10 步序列）：**

```
1. ArenaSystem.Initialize(arenaId)
2. ClassSystem.Initialize(playerSlots)
3. CharacterControllers.Initialize()
4. CombatFSM.Initialize()
5. DamageSystem.Initialize(playerCount)
6. KnockbackSystem.Initialize(playerCount)
7. FocusSystem.Initialize(playerCount)
8. SkillDatabase.Initialize()
9. SkillDrawSystem.Initialize(playerCount)
10. SkillEquipmentManager.Initialize(playerCount)
11. HUD.Initialize()
12. MatchManager.Initialize(matchConfig)
13. TransitionTo(Countdown)
```

### 5. 对局间重置（Results → CharacterSelect）

当 GameStateManager 从 Results 回到 CharacterSelect（"再来一局"）时，MatchManager 需要全量重置：

```
MatchManager.Reset():
    scores = [0, 0]
    currentRound = 0
    phase = Inactive
    _focusSystem.ResetAllForNewMatch()
    _skillDrawSystem.ResetAll()
    _skillEquipmentManager.ResetAll()  // DeregisterAllSkillStates
```

注意：Results → CharacterSelect 不重新加载场景（ADR-0007），所以 MatchManager 仍在内存中。Reset 由 GameStateManager 的 CharacterSelect 进入动作触发（或通过事件通知）。

### 6. 接口与数据结构

```csharp
public struct MatchConfig
{
    public int MatchFormat;  // {1, 3, 5}
    public int PlayerCount;
}

public struct MatchState
{
    public MatchPhase Phase;
    public int[] Scores;
    public int CurrentRound;
    public int WinsNeeded;
    public int MaxRounds;
    public int PlayerCount;
}

public interface IMatchManager
{
    void Initialize(MatchConfig config);
    void Reset();  // Full match reset
    MatchState GetMatchState();
    int[] GetScores();
    int GetCurrentRound();

    event Action<int, int[]> OnRoundEnd;  // (winnerIndex, scores)
    event Action<int?> OnMatchEnd;        // (winnerIndex or null for draw)
}
```

### Architecture Diagram

```
┌─ Match Lifecycle (Bo3 example) ───────────────────────────────────┐
│                                                                    │
│  MatchLoading:                                                     │
│    MatchManager.Initialize(format=3) → WinsNeeded=2, scores=[0,0] │
│                                                                    │
│  Round 1:                                                          │
│    Countdown → Battle → [KO detected]                              │
│    KnockbackSystem.OnKO → MatchManager.HandleKO                    │
│      → scores=[1,0], matchOver=false                               │
│      → IGameState.SignalRoundEnd(0, false)                         │
│      → GameState: Battle → BattleEnd (freeze) → Countdown          │
│      → MatchManager.CoordinateRoundReset()                         │
│        (damage=0, focus=0, position=spawn, skills KEPT)            │
│                                                                    │
│  Round 2:                                                          │
│    Battle → [KO detected]                                          │
│      → scores=[2,0], matchOver=true                                │
│      → IGameState.SignalRoundEnd(0, true)                          │
│      → GameState: Battle → BattleEnd (freeze) → Results            │
│                                                                    │
│  Results → "再来" → CharacterSelect → MatchManager.Reset()         │
│  Results → "退出" → MenuScene (GameScene unloads)                  │
│                                                                    │
└────────────────────────────────────────────────────────────────────┘
```

## Alternatives Considered

### Alternative 1: MatchManager 作为 GameStateManager 的一部分
- **Description**: 将对局管理逻辑合并到 GameStateManager 中
- **Cons**: GameStateManager 职责过重（场景管理 + 状态流转 + 对局逻辑）。对局管理有独立的内部 FSM 和大量调参旋钮
- **Rejection Reason**: 对局管理是 Feature 层关注点（BoN 格式、比分策略、重置协调），与 Foundation 层的游戏状态管理（场景加载、GamePhase FSM）有不同的变化原因

### Alternative 2: 回合间重新加载场景
- **Description**: 每个回合结束时重新加载 GameScene
- **Cons**: 违反 Pillar 4（快速战斗）——场景加载增加 2-3 秒等待。GameScene 设计为常驻（ADR-0007）
- **Rejection Reason**: ADR-0007 明确"Results → CharacterSelect 不加载场景，仅重置 GameScene 内状态"。回合间更不应重新加载

### Alternative 3: 无内部 FSM，使用布尔标志
- **Description**: 用 _roundInProgress, _matchOver 等布尔标志代替 MatchPhase enum
- **Cons**: 状态组合爆炸——N 个布尔标志有 2^N 种组合，其中大部分是非法状态。内部 FSM 保证只有合法状态转换
- **Rejection Reason**: 对局管理有 5 个明确状态和严格转换规则，内部 FSM 防止异常事件处理（如 RoundResolved 时忽略额外 KO）

## Consequences

### Positive
- MatchFormulas 纯静态类可 100% 单元测试（比分逻辑、胜负判定）
- 内部 FSM 保证事件处理的正确性——异常状态下的 KO 事件被忽略
- SignalRoundEnd 接口将"比赛是否结束"的决策权交给 MatchManager，GameStateManager 只负责状态流转
- 跨局技能保留实现"成长弧线"——从白板到满载的成长感

### Negative
- MatchManager 持有 6+ 个系统引用用于回合重置协调——初始化依赖较多
- 双 KO 的 winnerIndex 语义需要额外约定（-1 = 平局）
- 回合重置协调是同步调用——如果某个系统的 Reset 方法出错，后续系统不会被重置

### Risks
- **重置协调失败**: 如果某个系统（如 FocusSystem）在 ResetForNewRound 时抛异常，后续系统不会被重置 → 缓解: 每个 Reset 调用 try-catch，记录错误但不中断后续重置
- **SignalRoundEnd 时序**: MatchManager 在 FixedUpdate 中调用 SignalRoundEnd，GameStateManager 的状态转换可能延迟一帧 → 缓解: 这与 ADR-0008 的"同帧多事件处理"设计一致，1 帧延迟可接受
- **MatchManager 场景销毁**: Results → MenuScene 时 GameScene 卸载，MatchManager 被销毁 → 缓解: MatchManager 不在 DontDestroyOnLoad 中，场景卸载时自然清理。PlayerSlot 数据在 GameStateManager 中持久化

## GDD Requirements Addressed

| GDD System | Requirement | How This ADR Addresses It |
|------------|-------------|--------------------------|
| match-management-system.md | "Bo1/Bo3/Bo5 支持" | MatchConfig.MatchFormat + MatchFormulas |
| match-management-system.md | "WinsNeeded = Ceil(MatchFormat/2)" | MatchFormulas.CalculateWinsNeeded |
| match-management-system.md | "双 KO: 双方各 +1, 可能平局" | Frame-end KO processing with count tracking |
| match-management-system.md | "IsMatchOver/IsDraw 判定" | MatchFormulas.IsMatchOver/IsDraw |
| match-management-system.md | "SignalRoundEnd(winner, matchOver)" | IGameState.SignalRoundEnd call |
| match-management-system.md | "回合间数据重置表" | CoordinateRoundReset() |
| match-management-system.md | "已装备技能保留" | SkillEquipmentManager NOT reset between rounds |
| match-management-system.md | "OnRoundEnd, OnMatchEnd 事件" | IMatchManager events |
| match-management-system.md | "内部状态机: Inactive→WaitingForBattle→RoundInProgress→RoundResolved→MatchComplete" | MatchPhase enum + transitions |
| match-management-system.md | "异常状态忽略额外 KO" | Phase guard in HandleKO |
| match-management-system.md | "IMatchManager 接口" | IMatchManager definition |
| game-state-management.md | "SignalRoundEnd: matchOver=false→Countdown, matchOver=true→Results" | MatchManager calls SignalRoundEnd based on match state |
| focus-system.md | "新一局开始时 FocusPoints=0, UnlockedCount=0" | ResetForNewMatch (对局间) |
| skill-draw-system.md | "新一局开始时 AlreadyDrawnSkillIds 清空" | ResetForNewMatch (对局间) |

## Performance Implications
- **CPU**: MatchManager per-frame = ~0.01ms (only active during state transitions and KO events); CoordinateRoundReset = ~0.1ms (6 system resets × 2 players)
- **Memory**: MatchRuntimeState = ~64B; scores array = 8B; negligible
- **GC**: No allocations during match processing (array reuse, struct operations)
- **Network**: 不适用

## Migration Plan
不适用——新项目初始架构决策。

## Validation Criteria
- [ ] Bo3 初始化: format=3 → WinsNeeded=2, MaxRounds=3, scores=[0,0]
- [ ] 单 KO 处理: P2 被 KO → scores=[1,0], round=2, SignalRoundEnd(0, false)
- [ ] 双 KO 处理: 同帧两个 KO → scores 各 +1
- [ ] 比赛结束: scores=[2,1] → IsMatchOver=true, winner=P1, SignalRoundEnd(0, true)
- [ ] 比赛平局: Bo3 scores=[2,2] → IsDraw=true, SignalRoundEnd(-1, true)
- [ ] 回合重置: damage=0, focus=0, position=spawn, skills KEPT
- [ ] 技能保留: 第 1 局装备 2 技能 → 第 2 局仍可用
- [ ] 异常忽略: RoundResolved 时收到 OnKO → 忽略
- [ ] 异常忽略: MatchComplete 时收到任何事件 → 忽略
- [ ] 对局间全量重置: Results → CharacterSelect → skills cleared, FSM DeregisterAll
- [ ] MatchManager 处理耗时 < 0.05ms/frame（正常帧）
- [ ] 回合重置协调耗时 < 0.5ms（含所有系统 Reset 调用）

## Related Decisions
- ADR-0007: Scene & Game State — SignalRoundEnd, GamePhase FSM, DontDestroyOnLoad
- ADR-0006: Damage Pipeline — IKnockbackSystem.OnKO event
- ADR-0009: Focus & Skill Draw — reset interface definitions (ResetForNewRound/ResetForNewMatch)
- ADR-0008: Event Architecture — event subscription pattern
