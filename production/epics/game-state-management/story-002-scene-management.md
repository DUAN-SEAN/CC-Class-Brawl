# Story 002: Scene Management — MenuScene + GameScene Async Load/Unload

> **Epic**: 游戏状态管理
> **Status**: Ready
> **Layer**: Foundation
> **Type**: Integration
> **Estimate**: M
> **Manifest Version**: 2026-05-24
> **Last Updated**: 2026-05-24

## Context

**GDD**: `design/gdd/game-state-management.md`
**Requirement**: TR-GST-009 ~ TR-GST-014 (场景管理相关)
**ADR Governing Implementation**: ADR-0007: Scene & Game State Management
**ADR Decision Summary**: 两场景架构 (MenuScene + GameScene), GameScene 常驻, SceneManager.LoadSceneAsync + AllowSceneActivation 超时控制, DontDestroyOnLoad 管理 PlayerInputManager/GameStateManager/GameManager。
**Engine**: Unity 2022.3.51 LTS | **Risk**: LOW

**Control Manifest Rules**:
- Required: 必须使用且仅使用 2 个 Unity 场景: MenuScene + GameScene
- Required: GameScene 加载后保持常驻, 局间回到 CharacterSelect 不重新加载场景
- Required: PlayerInputManager 使用 DontDestroyOnLoad 跨场景持久化
- Required: 异步加载使用 Time.unscaledDeltaTime 追踪超时
- Required: 系统初始化按严格 10 步序列执行, 失败回退 CharacterSelect
- Guardrail: MenuScene load < 0.5s; GameScene first load < 2s; CharSelect→Countdown < 3s

---

## Acceptance Criteria

- [ ] MainMenu → CharacterSelect: SceneManager.LoadSceneAsync("GameScene") 在 SceneLoadTimeout (5.0s) 内完成
- [ ] GameScene 加载后常驻 — Results → CharacterSelect 不重新加载场景, 仅重置状态, 过渡 < 0.5s
- [ ] Results → MainMenu: SceneManager.LoadSceneAsync("MenuScene") 正确卸载 GameScene
- [ ] 异步加载使用 allowSceneActivation=false 手动控制激活, progress 在 0.9f 时等待激活许可
- [ ] 加载超时 (SceneLoadTimeout=5.0s) 自动回退 MainMenu 状态并显示错误提示
- [ ] 加载期间玩家按返回可取消操作, 回到 MainMenu
- [ ] PlayerInputManager 跨场景持久化 (DontDestroyOnLoad), 不需要重新配对设备
- [ ] MatchLoading 阶段按 10 步序列初始化所有战斗系统, MatchInitTimeout (3.0s) 内完成

---

## Implementation Notes

**来自 ADR-0007 的具体指导**:

1. 使用协程实现异步加载 + 超时:
```csharp
IEnumerator LoadGameSceneWithTimeout(float timeoutSeconds)
{
    var asyncOp = SceneManager.LoadSceneAsync("GameScene");
    asyncOp.allowSceneActivation = false;

    float elapsed = 0f;
    while (asyncOp.progress < 0.9f)
    {
        elapsed += Time.unscaledDeltaTime;
        if (elapsed > timeoutSeconds)
        {
            Debug.LogError("Scene load timeout");
            TransitionTo(GamePhase.MainMenu);
            yield break;
        }
        yield return null;
    }

    asyncOp.allowSceneActivation = true;
    yield return null;
}
```

2. 使用 Time.unscaledDeltaTime (非 Time.deltaTime) — 场景加载期间游戏时间可能暂停

3. DontDestroyOnLoad 管理器: PlayerInputManager, GameStateManager, GameManager

4. GameScene 内系统引用: GameStateManager 持有 GameScene 系统的接口引用 (非直接 MonoBehaviour 引用), 场景加载后重新绑定

5. MatchLoading 初始化 10 步序列:
   ArenaSystem → ClassSystem → CharacterControllers → CombatFSM → DamageSystem → KnockbackSystem → FocusSystem → SkillDatabase → HUD → TransitionTo(Countdown)

---

## Out of Scope

- GamePhase FSM 本身 (Story 001)
- PlayerSlot 数据管理 (Story 003)
- 倒计时显示与输入冻结 (Story 004)
- BattleEnd 冻结帧与结果画面过渡 (Story 005)
- 各系统的具体初始化逻辑 (各自 epic 中的 story 负责)

---

## QA Test Cases

- **AC-1 (MenuScene→GameScene 异步加载)**:
  - Given: MainMenu 状态
  - When: 手柄按 Start 触发加载
  - Then: LoadSceneAsync("GameScene") 在 5.0s 内完成, 状态转换到 CharacterSelect
  - Edge cases: 加载超时回退 MainMenu

- **AC-2 (GameScene 常驻)**:
  - Given: Results 状态, 玩家选"再来一局"
  - When: 状态转换到 CharacterSelect
  - Then: 不调用 LoadSceneAsync, 仅重置 GameScene 内状态, 过渡 < 0.5s

- **AC-3 (Results→MainMenu 卸载)**:
  - Given: Results 状态, 玩家选"退出到菜单"
  - When: LoadSceneAsync("MenuScene") 执行
  - Then: GameScene 正确卸载, 状态回到 MainMenu

- **AC-5 (加载超时)**:
  - Given: LoadSceneAsync 超过 5.0s
  - When: timeoutSeconds 已过
  - Then: 自动 TransitionTo(MainMenu), 显示错误

- **AC-6 (加载取消)**:
  - Given: 异步加载进行中
  - When: 玩家按返回键
  - Then: 取消加载, 回到 MainMenu

- **AC-7 (PlayerInputManager 持久化)**:
  - Given: PlayerInputManager 在 DontDestroyOnLoad 中
  - When: 场景切换
  - Then: 设备配对保持, 不需要重新加入

---

## Test Evidence

**Story Type**: Integration
**Required evidence**: `tests/integration/game-state/scene-management_test.cs`
**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: Story 001 (GamePhase FSM — TransitionTo 接口)
- Unlocks: Story 003 (PlayerSlot), Story 004 (Countdown), Story 005 (BattleEnd)
