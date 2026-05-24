# Story 005: BattleEnd & Results Transition — FreezeFrames, SignalRoundEnd Routing, Results Exit

> **Epic**: 游戏状态管理
> **Status**: Ready
> **Layer**: Foundation
> **Type**: Integration
> **Estimate**: M
> **Manifest Version**: 2026-05-24
> **Last Updated**: 2026-05-24

## Context

**GDD**: `design/gdd/game-state-management.md`
**Requirement**: TR-GST-004, TR-GST-017, TR-GST-020 (BattleEnd/Results 相关)
**ADR Governing Implementation**: ADR-0007: Scene & Game State Management, ADR-0010: Match & Round Lifecycle
**ADR Decision Summary**: BattleEnd 状态冻结 BattleEndFreezeFrames (60帧/1秒) 后转换。SignalRoundEnd(winnerIndex, matchOver) 路由: matchOver=false → BattleEnd → Countdown; matchOver=true → BattleEnd → Results。Results 状态等待玩家选择: "再来" → CharacterSelect (不重新加载场景); "退出" → MainMenu (卸载 GameScene)。
**Engine**: Unity 2022.3.51 LTS | **Risk**: LOW

**Control Manifest Rules**:
- Required: BattleEndFreezeFrames = 60 (安全范围 30-120)
- Required: SignalRoundEnd(int winnerIndex, bool matchOver) 接口
- Required: Results → CharacterSelect 不重新加载场景, 重置状态 < 0.5s
- Required: Results → MainMenu 卸载 GameScene
- Required: GameStateManager 不控制 MatchManager 的内部状态 (职责分离)
- Guardrail: BattleEnd freeze < 1s 默认; Results→CharSelect < 0.5s

---

## Acceptance Criteria

- [ ] Battle 状态收到 SignalRoundEnd(winnerIndex, matchOver) 后转换到 BattleEnd
- [ ] BattleEnd 冻结帧: BattleEndFreezeFrames (60帧) 后自动转换
- [ ] matchOver=false: BattleEnd → Countdown (多局循环, 不经过 Results)
- [ ] matchOver=true: BattleEnd → Results
- [ ] BattleEnd 冻结期间第二个 KO 信号被忽略 (第一个锁定胜者)
- [ ] Results 状态等待玩家选择: "再来一局" 或 "退出到菜单"
- [ ] Results → "再来一局" → CharacterSelect, 不重新加载场景, 过渡 < 0.5s, 角色选择保持
- [ ] Results → "退出到菜单" → MainMenu, GameScene 卸载
- [ ] OnStateChanged 事件在每个状态转换时触发

---

## Implementation Notes

**来自 ADR-0007/ADR-0010 的具体指导**:

1. SignalRoundEnd 行为路由:
   - `matchOver = false`: BattleEnd 冻结帧后 → **Countdown** (不是 Results)
   - `matchOver = true`: BattleEnd 冻结帧后 → **Results**
   - 这将对局格式 (Bo1/Bo3/Bo5) 的决策权交给 MatchManager, GameStateManager 只负责状态流转

2. BattleEnd 状态进入动作: 冻结所有输入, 等待冻结帧
3. BattleEnd 状态: 仅摄像机活跃

4. BattleEnd 冻结帧计数:
   - 在 FixedUpdate 中递减计数器
   - 计数器归零时根据 matchOver 标志转换

5. Results → CharacterSelect ("再来一局"):
   - 不调用 LoadSceneAsync
   - 调用 MatchManager.CoordinateRoundReset (如果是多局中间) 或 ResetAllForNewMatch (如果全新匹配)
   - 角色选择保持 (PlayerSlot 持久化)

6. Results → MainMenu ("退出"):
   - LoadSceneAsync("MenuScene", LoadSceneMode.Single)
   - GameScene 自动卸载
   - 清理 GameScene 系统引用

7. 冻结期间忽略后续 KO:
   - BattleEnd 进入后锁定胜者
   - 后续 SignalRoundEnd 调用被忽略

---

## Out of Scope

- GamePhase FSM 核心逻辑 (Story 001)
- 场景加载细节 (Story 002)
- PlayerSlot 管理 (Story 003)
- 倒计时逻辑 (Story 004)
- MatchManager 的对局逻辑 (match-management epic)
- Results 画面的 UI 实现 (UI epic)

---

## QA Test Cases

- **AC-1 (SignalRoundEnd 路由)**:
  - Given: Battle 状态
  - When: SignalRoundEnd(winner, true)
  - Then: Battle → BattleEnd → (60帧后) → Results

- **AC-3 (matchOver=false 多局循环)**:
  - Given: Battle 状态, 多局制
  - When: SignalRoundEnd(winner, false)
  - Then: Battle → BattleEnd → (60帧后) → Countdown (不经过 Results)

- **AC-5 (冻结期间忽略后续 KO)**:
  - Given: BattleEnd 冻结期间
  - When: 第二个 SignalRoundEnd 到达
  - Then: 忽略, 胜者不变

- **AC-7 (Results→CharSelect 不重新加载)**:
  - Given: Results 状态
  - When: 玩家选"再来一局"
  - Then: 不调用 LoadSceneAsync, 状态重置 < 0.5s, 角色选择保持

- **AC-8 (Results→MainMenu 卸载)**:
  - Given: Results 状态
  - When: 玩家选"退出到菜单"
  - Then: LoadSceneAsync("MenuScene"), GameScene 卸载

---

## Test Evidence

**Story Type**: Integration
**Required evidence**: `tests/integration/game-state/battleend-results-transition_test.cs`
**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: Story 001 (GamePhase FSM), Story 002 (Scene Management), Story 003 (PlayerSlot), Story 004 (Countdown — 多局循环目标)
- Unlocks: None (本 epic 最终 story)
