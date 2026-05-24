# ADR-0014: UI Architecture — Unity UI Toolkit (UXML/USS) + Screen Space Overlay + Event-Driven Data Binding

## Status
Accepted

## Date
2026-05-24

## Engine Compatibility

| Field | Value |
|-------|-------|
| **Engine** | Unity 2022.3.51 LTS |
| **Domain** | UI |
| **Knowledge Risk** | LOW — version is within LLM training data |
| **References Consulted** | `docs/engine-reference/unity/VERSION.md` |
| **Post-Cutoff APIs Used** | None |
| **Verification Required** | Verify UI Toolkit data binding performance with 15+ event subscriptions at 60Hz; verify USS color transitions support HSV-based color coding; verify UIDocument.panelSettings references survive scene transitions when placed in GameScene |

## ADR Dependencies

| Field | Value |
|-------|-------|
| **Depends On** | ADR-0007 (Scene & Game State — GameScene canvas structure, GamePhase FSM, OnStateChanged event), ADR-0008 (Event Architecture — C# event delegates per interface) |
| **Enables** | Battle HUD, Skill Selection Overlay, Countdown/Results UI |
| **Blocks** | Battle HUD implementation, skill draw UI, match result display |
| **Ordering Note** | Presentation layer. Depends on Core systems providing events and data. Can be implemented in parallel with Feature layer systems. |

## Context

### Problem Statement
战斗 HUD 是玩家与游戏系统的核心交互界面，需要实时反映伤害百分比、专注值进度、技能槽位、比分等信息。UI 架构需要支持：事件驱动数据更新、动画效果（脉动、弹跳、颜色渐变）、手柄方向键导航（技能选择）、Screen Space Overlay 渲染（不受摄像机缩放影响）。同时，UI 系统是 Presentation 层最重的组件，需要明确的性能预算（< 0.8ms/帧）。

### Constraints
- 渲染模式：Screen Space - Overlay（不引用摄像机）
- 基准分辨率：1920×1080，Scale With Screen Size
- MVP 仅包含战斗阶段 UI（Countdown → Battle → BattleEnd → Results）
- CharacterSelect UI 和 Match UI 推迟到 VS 阶段（systems-index #18, #19）
- 手柄为主要输入方式——技能选择必须支持方向键导航
- 所有 UI 数据通过事件驱动更新，不轮询
- "中央 70% 战斗区域无遮挡"原则

### Requirements
- 伤害百分比显示（颜色编码 + 弹跳动画）
- 专注值进度条（职业色 + 脉动效果 + 阈值标记）
- 技能槽位（4 槽位 + 图标 + 稀有度边框 + 装备动画）
- 比分区域（比分 + 回合数 + 赛点指示）
- 技能选择叠加层（3 卡牌 + 手柄导航 + 超时）
- KO 通知 + 边缘警告 + 方向指示箭头
- HUD 可见性控制（淡入/淡出）
- 15+ 事件订阅，0.8ms 帧预算

## Decision

采用 **Unity UI Toolkit (UXML/USS) + Screen Space Overlay Canvas + 事件驱动数据绑定 + HUDController MonoBehaviour** 架构。

### 1. 为什么选择 UI Toolkit 而非 UGUI

| 维度 | UI Toolkit | UGUI (Canvas) |
|------|-----------|---------------|
| 数据绑定 | USS custom properties + C# 脚本高效 | 需要手动 Update 或第三方框架 |
| 动画 | USS transitions (内置颜色/大小/透明度过渡) | DOTween 或手动协程 |
| 布局 | Flexbox (自动响应式) | 手动 Anchors + Layout Groups |
| 性能 | 单 draw call 批处理（内部优化） | Canvas batch breaks 需要手动管理 |
| 手柄导航 | 内置焦点系统 (focusController) | 需要手动 Navigation 配置 |
| 样式分离 | UXML 结构 + USS 样式 = 关注点分离 | 代码中混合样式和逻辑 |
| 2022.3 成熟度 | UI Toolkit 已在 2021+ 版本稳定，Production-ready | 成熟但维护成本高 |

**关键决策因素**：
- 技能选择叠加层需要手柄方向键导航——UI Toolkit 内置 `focusController` 直接支持
- USS transitions 实现脉动/弹跳/颜色渐变不需要额外库
- Flexbox 布局自动处理不同分辨率适配
- 战斗 HUD 约 30 个 UI 元素，UGUI 的 Canvas batch 管理反而增加复杂度

### 2. HUD 根结构

```xml
<!-- BattleHUD.uxml -->
<ui:UXML xmlns:ui="UnityEngine.UIElements">
  <ui:VisualElement name="BattleHUD" class="hud-root">
    <!-- 比分区域（顶部中央） -->
    <ui:VisualElement name="ScoreArea" class="score-area">
      <ui:Label name="ScoreText" class="score-text" text="0 - 0"/>
      <ui:Label name="RoundText" class="round-text" text="R1/3"/>
      <ui:Label name="MatchPointIndicator" class="match-point" text="*" />
    </ui:VisualElement>

    <!-- 玩家1 信息区域（左下角） -->
    <ui:VisualElement name="P1InfoArea" class="player-info p1">
      <ui:Label name="P1DamagePercent" class="damage-percent" text="0%"/>
      <ui:VisualElement name="P1FocusBar" class="focus-bar">
        <ui:VisualElement name="P1FocusFill" class="focus-fill"/>
        <ui:VisualElement name="P1ThresholdMarker" class="threshold-marker"/>
      </ui:VisualElement>
      <ui:VisualElement name="P1SkillSlots" class="skill-slots">
        <ui:VisualElement name="P1Slot0" class="skill-slot empty"/>
        <ui:VisualElement name="P1Slot1" class="skill-slot empty"/>
        <ui:VisualElement name="P1Slot2" class="skill-slot empty"/>
        <ui:VisualElement name="P1Slot3" class="skill-slot empty"/>
      </ui:VisualElement>
    </ui:VisualElement>

    <!-- 玩家2 信息区域（右下角，镜像） -->
    <ui:VisualElement name="P2InfoArea" class="player-info p2">
      <!-- 镜像结构 -->
    </ui:VisualElement>

    <!-- KO 通知（中央叠加） -->
    <ui:Label name="KONotification" class="ko-notification" text="KO!"
              style="display: none;"/>

    <!-- 边缘警告（全屏叠加） -->
    <ui:VisualElement name="EdgeWarning" class="edge-warning"
                      style="display: none;"/>

    <!-- 方向指示箭头容器 -->
    <ui:VisualElement name="DirectionArrows" class="direction-arrows"/>
  </ui:VisualElement>
</ui:UXML>
```

### 3. 技能选择叠加层（独立 UXML）

```xml
<!-- SkillSelectionOverlay.uxml -->
<ui:UXML xmlns:ui="UnityEngine.UIElements">
  <ui:VisualElement name="SkillSelection" class="skill-selection-root" style="display: none;">
    <ui:VisualElement name="CardContainer" class="card-container">
      <ui:VisualElement name="Card0" class="skill-card" tabindex="0"/>
      <ui:VisualElement name="Card1" class="skill-card" tabindex="1"/>
      <ui:VisualElement name="Card2" class="skill-card" tabindex="2"/>
    </ui:VisualElement>
    <ui:VisualElement name="TimeoutIndicator" class="timeout-ring"/>
  </ui:VisualElement>
</ui:UXML>
```

手柄方向键导航通过 UI Toolkit 的 `focusController` + `tabindex` 属性实现——左右切换焦点在卡牌间移动。

### 4. HUDController MonoBehaviour（数据绑定中心）

```csharp
public class HUDController : MonoBehaviour
{
    [SerializeField] private UIDocument _hudDocument;
    [SerializeField] private UIDocument _skillSelectionDocument;
    [SerializeField] private HUDTuningData _tuning;

    // UI 元素引用（Initialize 中缓存）
    private Label _p1DamagePercent;
    private VisualElement _p1FocusFill;
    private VisualElement[] _p1SkillSlots;
    // ... P2 mirrors

    // 系统引用（Initialize 中注入）
    private IDamageSystem _damageSystem;
    private IFocusSystem _focusSystem;
    private ISkillEquipmentManager _skillEquipment;
    private IMatchManager _matchManager;
    private IGameState _gameState;
    private ICameraDataProvider _cameraData;

    public void Initialize(/* all system interfaces */)
    {
        CacheUIReferences();
        SubscribeEvents();
        SetInitialState();
    }

    public void Cleanup()
    {
        UnsubscribeEvents();
    }
}
```

### 5. 事件订阅模式

每个事件对应一个专门的更新方法，不做批量刷新：

```csharp
private void SubscribeEvents()
{
    _damageSystem.OnDamagePercentChanged += OnDamagePercentChanged;
    _focusSystem.OnFocusChanged += OnFocusChanged;
    _focusSystem.OnFocusReady += OnFocusReady;
    _skillEquipment.OnSkillEquipped += OnSkillEquipped;
    _skillEquipment.OnSkillUnequipped += OnSkillUnequipped;
    _matchManager.OnRoundEnd += OnRoundEnd;
    _matchManager.OnMatchEnd += OnMatchEnd;
    _gameState.OnStateChanged += OnGameStateChanged;
    // KO event from knockback system
    // Skill draw events
}

private void OnDamagePercentChanged(CharacterId id, float newPercent)
{
    var label = id == CharacterId.P1 ? _p1DamagePercent : _p2DamagePercent;
    int displayValue = Mathf.Max(0, Mathf.FloorToInt(newPercent));
    label.text = displayValue > 999 ? "999+" : $"{displayValue}%";

    // USS custom property 用于颜色编码
    label.RemoveFromClassList("damage-low", "damage-mid", "damage-high", "damage-critical");
    if (displayValue < 50) label.AddToClassList("damage-low");       // 白色
    else if (displayValue < 100) label.AddToClassList("damage-mid");  // 黄色
    else if (displayValue < 150) label.AddToClassList("damage-high"); // 橙色
    else label.AddToClassList("damage-critical");                     // 红色闪烁

    // 弹跳动画通过 USS transition 自动播放
    label.AddToClassList("bounce");
    // 延迟移除 bounce class（由调度器或 coroutine 处理）
}
```

### 6. USS 动画策略

```css
/* 伤害百分比弹跳动画 */
.damage-percent {
    font-size: 56px;
    --unity-text-color: rgb(255, 255, 255);
    transition: font-size 0.15s ease-out, --unity-text-color 0.2s ease;
}
.damage-percent.bounce {
    font-size: 73px; /* 56 * 1.3 */
}
.damage-mid { --unity-text-color: rgb(255, 215, 0); }    /* #FFD700 */
.damage-high { --unity-text-color: rgb(255, 140, 0); }   /* #FF8C00 */
.damage-critical { --unity-text-color: rgb(255, 32, 32); /* #FF2020 */ }

/* 专注值进度条脉动 */
.focus-fill {
    background-color: var(--player-color);
    transition: width 0.2s ease-out;
}
.focus-fill.pulse {
    opacity: 0.7;
    /* 脉动通过 C# 的 schedule.Execute 循环控制 */
}

/* 技能槽位装备动画 */
.skill-slot.equipping .skill-icon {
    scale: 1.2;
    transition: scale 0.25s ease-out;
}
.skill-icon {
    scale: 1.0;
    transition: scale 0.25s ease-out;
}

/* 稀有度边框颜色 */
.skill-card.rarity-common { border-color: rgb(68, 136, 255); }   /* #4488FF */
.skill-card.rarity-rare { border-color: rgb(136, 68, 204); }     /* #8844CC */
.skill-card.rarity-epic { border-color: rgb(255, 184, 0); }      /* #FFB800 */
```

### 7. HUD 可见性控制

```csharp
private void OnGameStateChanged(GamePhase phase)
{
    switch (phase)
    {
        case GamePhase.Countdown:
            ShowHUD(); // 淡入 0.3s
            break;
        case GamePhase.Battle:
            // HUD 已可见，技能选择叠加层可能在此期间显示/隐藏
            break;
        case GamePhase.BattleEnd:
            // HUD 冻结在最终状态
            break;
        case GamePhase.Results:
            HideHUD(); // 淡出 0.3s
            break;
    }
}

private void ShowHUD()
{
    _hudRoot.style.display = DisplayStyle.Flex;
    _hudRoot.style.opacity = 0f;
    // 使用 UI Toolkit 的 schedule 或 DOTween 替代
    _hudRoot.schedule.Execute(() => {
        _hudRoot.style.opacity = 1f;
    }).Every(16).ForDuration(300); // ~0.3s 淡入
}
```

### 8. 技能选择手柄导航

```csharp
private void OnDrawReady(CharacterId id, IReadOnlyList<SkillData> candidates)
{
    _skillSelectionRoot.style.display = DisplayStyle.Flex;
    PopulateCards(candidates);

    // 聚焦第一张卡牌
    var firstCard = _skillSelectionRoot.Q<VisualElement>("Card0");
    firstCard.Focus();

    // 注册导航回调
    firstCard.RegisterCallback<NavigationMoveEvent>(OnCardNavigation);
    firstCard.RegisterCallback<NavigationSubmitEvent>(OnCardSelected);

    // 启动超时倒计时
    StartSelectionTimeout(id, _tuning.SelectionTimeout);
}

private void OnCardNavigation(NavigationMoveEvent evt)
{
    // UI Toolkit 的 focusController 自动处理焦点移动
    // 左右方向键在 tabindex 0/1/2 之间切换
}

private void OnCardSelected(NavigationSubmitEvent evt)
{
    var selectedCard = evt.target as VisualElement;
    int index = selectedCard.tabIndex;
    ConfirmSelection(index);
}
```

### 9. HUDTuningData

```csharp
[CreateAssetMenu(fileName = "HUDTuningData", menuName = "ClassBrawl/HUDTuningData")]
public class HUDTuningData : ScriptableObject
{
    // 伤害百分比
    public float DamageNumberBounceScale = 1.3f;
    public float DamageNumberBounceDuration = 0.15f;

    // 专注值进度条
    public float FocusPulseMinFrequency = 1.0f;
    public float FocusPulseMaxFrequency = 3.0f;
    public float FocusPulseThreshold = 0.8f;

    // 技能槽位
    public float SkillSlotSize = 48f;
    public float SkillEquipAnimDuration = 0.25f;

    // HUD 全局
    public float HudFadeInDuration = 0.3f;
    public float HudFadeOutDuration = 0.3f;
    public float DataStaleTimeout = 5.0f;

    // 技能选择
    public int CandidateCount = 3;
    public float SelectionTimeout = 5.0f;
}
```

### Architecture Diagram

```
┌─ System Events (from Core/Feature layers) ───────────┐
│  OnDamagePercentChanged, OnFocusChanged,              │
│  OnSkillEquipped, OnRoundEnd, OnStateChanged, ...    │
└──────────────────────────────────────────────────────┘
                         ↓ Subscribe in Initialize()
┌─ HUDController (MonoBehaviour, GameScene) ────────────┐
│                                                       │
│  UIDocument → UXML element cache (30+ elements)       │
│  HUDTuningData → animation parameters                 │
│                                                       │
│  Event Handlers → UI Updates:                         │
│    OnDamagePercentChanged → Label.text + USS classes  │
│    OnFocusChanged → ProgressBar width + pulse         │
│    OnSkillEquipped → Slot icon + rarity border        │
│    OnRoundEnd → Score text + match point indicator    │
│    OnStateChanged → HUD visibility (fade in/out)      │
│    OnDrawReady → Skill selection overlay + focus      │
│    OnKO → KO notification animation                   │
│                                                       │
│  Per-Frame Updates (Update):                          │
│    Edge warning alpha (based on player distance to    │
│    blast zone) + Direction arrows (based on camera   │
│    bounds vs player positions)                        │
│                                                       │
└──────────────────────────────────────────────────────┘
         ↓ Renders via
┌─ UIDocument (Screen Space Overlay) ──────────────────┐
│  PanelSettings: 1920×1080 Scale With Screen Size     │
│  BattleHUD.uxml + SkillSelection.uxml                │
│  BattleHUD.uss + SkillSelection.uss                  │
└──────────────────────────────────────────────────────┘
```

### Key Interfaces

HUDController 消费的接口（只读查询 + 事件订阅）：

```csharp
// 事件来源接口（来自 Core/Feature 系统）
public interface IDamageSystemEvents { event Action<CharacterId, float> OnDamagePercentChanged; }
public interface IFocusSystemEvents { event Action<CharacterId, float, float> OnFocusChanged; event Action<CharacterId, int> OnFocusReady; }
public interface ISkillEquipmentEvents { event Action<CharacterId, int, SkillData> OnSkillEquipped; event Action<CharacterId, int> OnSkillUnequipped; }
public interface IMatchEvents { event Action<int, int[]> OnRoundEnd; event Action<int> OnMatchEnd; }

// 数据查询接口（用于 Update 中的轮询场景——边缘警告、方向箭头）
public interface ICameraDataProvider { Vector3 GetCameraPosition(); float GetOrthographicSize(); float GetHalfWidth(); float GetHalfHeight(); }
// IArenaDataProvider.GetBlastZone() 用于边缘警告距离计算
```

## Alternatives Considered

### Alternative 1: UGUI (Canvas + RectTransform)
- **Description**: 使用 Unity 传统 Canvas 系统，TextMeshPro + Image 组件
- **Pros**: 文档丰富，社区经验多，TextMeshPro 文本渲染质量高
- **Cons**: 缺乏内置数据绑定；动画需要 DOTween 或手动协程；手柄导航需要手动配置 Navigation；Canvas batch breaks 需要精心管理层级；样式与逻辑混合在代码中
- **Rejection Reason**: UI Toolkit 的内置焦点系统（手柄导航）、USS transitions（动画）、Flexbox 布局（响应式）为战斗 HUD 提供了更完整的解决方案。UGUI 在数据驱动的动态 UI 场景中维护成本更高

### Alternative 2: UGUI + MVVM 框架（如 Unity MVC）
- **Description**: 使用 UGUI + 第三方数据绑定框架
- **Cons**: 引入第三方依赖；框架学习成本；框架可能与 Unity 2022.3 不兼容
- **Rejection Reason**: 15 个事件的订阅/更新用简单的 C# 回调即可处理，不需要 MVVM 框架的复杂度

### Alternative 3: 混合方案（UI Toolkit for HUD + UGUI for Skill Selection）
- **Description**: HUD 用 UI Toolkit，技能选择叠加层用 UGUI
- **Cons**: 两套 UI 系统并存增加维护成本和包体积；输入路由在两套系统间切换复杂
- **Rejection Reason**: 统一到一个 UI 框架。UI Toolkit 的 focusController 已覆盖手柄导航需求

## Consequences

### Positive
- UI Toolkit UXML/USS 分离结构和样式，与 Web 前端开发模式一致
- USS transitions 提供零代码动画——颜色、大小、透明度过渡仅靠 CSS
- 内置 focusController 直接支持手柄方向键导航（技能选择）
- Flexbox 布局自动适配不同分辨率
- 事件驱动更新保证 UI 只在实际数据变化时更新，不轮询

### Negative
- UI Toolkit 在 Unity 2022.3 中缺少部分高级控件（如环形进度条）——需要自定义 VisualElement
- 调试工具不如 UGUI 的 Inspector 预览直观（UI Toolkit Debugger 功能有限）
- 团队（如果扩展）需要学习 UXML/USS

### Risks
- **UI Toolkit 性能**: 15+ 事件订阅 + 30 个元素更新是否在 0.8ms 内完成 → 缓解: UI Toolkit 单 draw call 批处理在少量元素下性能优异；实际实现后用 Profiler 验证，如果超标优化为 dirty-flag 批量更新
- **环形超时指示器**: UI Toolkit 没有内置环形进度条 → 缓解: 用自定义 VisualElement + IStyle.unityBackgroundImageTintColor 配合圆形 sprite 实现，或简化为线性进度条
- **职业色动态绑定**: 专注值进度条颜色需要根据玩家职业动态设置 → 缓解: 使用 USS custom properties (`--player-color`)，在 Initialize 时通过 `style.SetPropertyValue("--player-color", classData.PrimaryColor)` 设置
- **Input routing conflict**: 技能选择叠加层激活时，需要将手柄输入从游戏逻辑重定向到 UI → 缓解: OnDrawReady 时设置 CombatFSM 输入锁定（IMovementController.FreezeMovement + InputSystem UI 优先级）

## GDD Requirements Addressed

| GDD System | Requirement | How This ADR Addresses It |
|------------|-------------|--------------------------|
| battle-hud.md | "伤害百分比: Floor(DamagePercent)+'%', 颜色编码 4 档" | Label.text + USS class switching |
| battle-hud.md | "伤害百分比弹跳动画: 1.0x→1.3x→1.0x, 0.15s" | USS transition on font-size |
| battle-hud.md | "专注值进度条: 填充比例=FocusPoints/Threshold, 职业色" | ProgressBar width + --player-color |
| battle-hud.md | "专注值脉动: >80% 时 1-3Hz 正弦亮度调制" | Schedule loop + opacity modulation |
| battle-hud.md | "技能槽位: 48×48px, 空槽灰色, 装备显示图标+稀有度边框" | VisualElement + USS class per rarity |
| battle-hud.md | "技能装备动画: 0→1.2x→1.0x, 0.25s" | USS transition on scale |
| battle-hud.md | "比分: P1得分 - P2得分, 回合 R1/n" | Label.text update on event |
| battle-hud.md | "赛点指示: 黄色脉动 '*'" | USS animation + match point class |
| battle-hud.md | "技能选择叠加层: 3 卡牌, 手柄左右导航, 超时 5s" | focusController + tabindex + timeout coroutine |
| battle-hud.md | "HUD 可见性: Countdown 淡入 0.3s, Results 淡出 0.3s" | OnStateChanged handler + opacity transition |
| battle-hud.md | "渲染模式: Screen Space Overlay, 基准 1920×1080" | PanelSettings configuration |
| battle-hud.md | "HUD 性能: < 0.8ms/帧" | Event-driven updates, no polling for main HUD |
| battle-hud.md | "中央 70% 无遮挡" | Flexbox layout with constrained margins |
| skill-draw-system.md | "技能三选一: 不暂停游戏, 悬浮叠加层" | SkillSelectionOverlay.uxml |
| skill-draw-system.md | "超时 5s → 自动选第一个" | SelectionTimeout timer |
| knockback-launch-system.md | "KO 通知 + 屏幕闪光" | KO notification label + edge warning |
| game-state-management.md | "倒计时: 3-2-1 各 1s" | OnStateChanged → countdown display |
| game-state-management.md | "结果显示 + 再来/退出" | Results UI canvas |
| match-management-system.md | "局间比分 + 等待提示" | OnRoundEnd handler |

## Performance Implications
- **CPU**: Event handler per update < 0.02ms; edge warning + direction arrows (Update) < 0.1ms; total < 0.3ms
- **Memory**: HUDController ~2KB; UXML element cache ~1KB; UIDocument internal ~5KB; total < 10KB
- **Load Time**: HUD Initialize (cache refs + subscribe) < 1ms; UXML/USS parse on scene load < 5ms
- **Network**: 不适用

## Migration Plan
不适用——新项目初始架构决策。

## Validation Criteria
- [ ] 伤害百分比正确显示 Floor(DamagePercent) + "%"
- [ ] 0-49% 白色, 50-99% 黄色, 100-149% 橙色, 150%+ 红色
- [ ] 伤害变化时弹跳动画 (1.0x → 1.3x → 1.0x, ~0.15s)
- [ ] 专注值进度条填充比例正确，职业色正确
- [ ] 专注值 > 80% 时脉动效果
- [ ] 技能装备后槽位显示图标 + 稀有度边框
- [ ] 技能选择叠加层支持手柄左右方向键导航
- [ ] 技能选择超时 5s 自动选第一个
- [ ] Countdown 时 HUD 淡入 (0.3s)
- [ ] Results 时 HUD 淡出 (0.3s)
- [ ] KO 通知 "KO!" 文本正确显示
- [ ] Screen Space Overlay，不受摄像机缩放影响
- [ ] 1920×1080 基准分辨率下所有元素位置正确
- [ ] HUDController 帧耗时 < 0.8ms（含事件处理 + Update 轮询）
- [ ] 15+ 事件订阅全部正确响应

## Related Decisions
- ADR-0007: Scene & Game State — GamePhase FSM 驱动 HUD 可见性
- ADR-0008: Event Architecture — C# event delegates 是 HUD 数据更新的唯一通道
- ADR-0005: Input System — 手柄输入重定向到 UI Toolkit focusController
- ADR-0012: Camera Strategy — ICameraDataProvider 供方向箭头和边缘警告查询
- ADR-0011: Arena Platform — IArenaDataProvider.GetBlastZone() 供边缘警告距离计算
