# Story 001: GamePhase FSM — 7 状态有限状态机与原子转换

> **Epic**: 游戏状态管理
> **Status**: Ready
> **Layer**: Foundation
> **Type**: Logic
> **Estimate**: M
> **Manifest Version**: 2026-05-24
> **Last Updated**: 2026-05-24

## Context

**GDD**: `design/gdd/game-state-management.md`
**Requirement**: TR-GST-001 ~ TR-GST-008 (状态机相关)
**ADR Governing Implementation**: ADR-0007: Scene & Game State Management — GamePhase FSM + SignalRoundEnd
**ADR Decision Summary**: GamePhase 7 状态 FSM (MainMenu->CharacterSelect->MatchLoading->Countdown->Battle->BattleEnd->Results), 原子转换, TransitionTo() 包含进入/退出动作, SignalRoundEnd(winnerIndex, matchOver) 支持多局制。
**Engine**: Unity 2022.3.51 LTS | **Risk**: LOW
**Engine Notes**: GameStateManager 在 DontDestroyOnLoad 中, 持有 FSM 状态。

**Control Manifest Rules (Foundation)**:
- Required: GamePhase 必须有且仅有 7 个状态
- Required: 状态转换必须原子化, 转换期间游戏逻辑暂停
- Required: SignalRoundEnd(int winnerIndex, bool matchOver) 接口
- Required: OnStateChanged(GamePhase) 事件在状态转换后 1 帧内通知
- Forbidden: SignalKO (已替换为 SignalRoundEnd)

---

## Acceptance Criteria

- [ ] GamePhase 枚举: MainMenu, CharacterSelect, MatchLoading, Countdown, Battle, BattleEnd, Results (已定义)
- [ ] 状态转换由明确触发条件驱动, 不存在模糊或条件竞争
- [ ] 状态转换原子化: TransitionTo() 执行退出旧状态动作 -> 进入新状态动作
- [ ] 每个状态有进入/退出动作定义 (活跃系统, 冻结系统)
- [ ] SignalRoundEnd(winnerIndex, false): Battle -> BattleEnd -> Countdown (多局循环)
- [ ] SignalRoundEnd(winnerIndex, true): Battle -> BattleEnd -> Results
- [ ] BattleEnd 冻结帧: BattleEndFreezeFrames (60帧/1秒) 后自动转换
- [ ] OnStateChanged 事件在转换完成后触发, 所有监听者 <= 1 帧延迟收到通知
- [ ] BattleEnd 冻结期间第二个 KO 信号被忽略
- [ ] FSM 评估帧耗时 < 0.01ms

---

## Implementation Notes

**来自 ADR-0007 的具体指导**:

1. GameStateManager 在 DontDestroyOnLoad 中, 持有 FSM 状态

2. 每个状态的进入/退出动作:

| 状态 | 进入动作 | 退出动作 | 活跃系统 |
|------|---------|---------|---------|
| MainMenu | 显示主菜单 UI | 隐藏 UI, 开始异步加载 | 仅 UI |
| CharacterSelect | 启用角色选择 UI, 允许加入 | 锁定选择 | PlayerInputManager, UI |
| MatchLoading | 初始化所有战斗系统 | -- | 所有系统初始化 |
| Countdown | 冻结 3C 输入, 显示倒计时 | 解冻输入 | 摄像机, UI |
| Battle | 激活所有战斗系统 | -- | 全部 |
| BattleEnd | 冻结所有输入, 等待冻结帧 | -- | 仅摄像机 |
| Results | 显示结果 UI, 等待选择 | 重置或卸载 | 仅 UI |

3. SignalRoundEnd 行为:
   - `matchOver = false`: BattleEnd 冻结帧后 -> Countdown (不是 Results)
   - `matchOver = true`: BattleEnd 冻结帧后 -> Results

4. **IGameState 接口 (已定义)**:
```csharp
public interface IGameState
{
    GamePhase GetState();
    bool IsBattleActive();
    void SetPlayerCharacter(int playerSlot, string characterId);
    PlayerSlot GetPlayerSlot(int playerSlot);
    IReadOnlyList<PlayerSlot> GetAllPlayerSlots();
    void SignalRoundEnd(int winnerIndex, bool matchOver);
    event Action<GamePhase> OnStateChanged;
    event Action<PlayerSlot> OnPlayerJoined;
    event Action<int> OnPlayerLeft;
    event Action OnAllPlayersReady;
}
```

5. 状态转换逻辑封装在 GameStateManager 中, 不暴露 TransitionTo 给外部

---

## Out of Scope

- 场景加载/卸载的实际执行 (Story 002)
- PlayerSlot 管理 (Story 003)
- 倒计时显示 (Story 004)
- MatchLoading 的 10 步初始化序列 (各系统实现时协调)

---

## QA Test Cases

- **AC-3 (原子转换 MainMenu->CharacterSelect)**:
  - Given: MainMenu 状态
  - When: 触发转换条件 (手柄按 Start)
  - Then: 原子转换到 CharacterSelect, OnStateChanged 触发

- **AC-5 (SignalRoundEnd matchOver=false)**:
  - Given: Battle 状态
  - When: SignalRoundEnd(winner, false)
  - Then: Battle -> BattleEnd -> (60帧后) -> Countdown (多局循环)

- **AC-6 (SignalRoundEnd matchOver=true)**:
  - Given: Battle 状态
  - When: SignalRoundEnd(winner, true)
  - Then: Battle -> BattleEnd -> (60帧后) -> Results

- **AC-7 (BattleEnd 冻结帧)**:
  - Given: BattleEnd 状态
  - When: 等待 BattleEndFreezeFrames (60帧)
  - Then: 自动转换到下一状态

- **AC-8 (OnStateChanged 事件)**:
  - Given: 任意状态转换
  - When: TransitionTo 执行完成
  - Then: OnStateChanged(newState) 触发, 所有监听者 <= 1 帧延迟

- **AC-9 (冻结期间忽略后续 KO)**:
  - Given: BattleEnd 冻结期间
  - When: 第二个 KO 信号到达
  - Then: 忽略, 胜者不改变

---

## Test Evidence

**Story Type**: Logic
**Required evidence**: `tests/unit/game-state/gamephase-fsm_test.cs`
**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: None (Foundation 层, GamePhase 枚举已存在)
- Unlocks: Story 002 (场景管理), Story 003 (PlayerSlot), Story 004 (倒计时), Story 005 (BattleEnd)
