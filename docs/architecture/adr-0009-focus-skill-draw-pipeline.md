# ADR-0009: Focus & Skill Draw Pipeline — Event-Driven Roguelike Skill Acquisition

## Status
Accepted

## Date
2026-05-24

## Engine Compatibility

| Field | Value |
|-------|-------|
| **Engine** | Unity 2022.3.51 LTS |
| **Domain** | Core (Focus) + Feature (Draw/Equipment) |
| **Knowledge Risk** | LOW — version is within LLM training data |
| **References Consulted** | `docs/engine-reference/unity/VERSION.md` |
| **Post-Cutoff APIs Used** | None |
| **Verification Required** | None |

## ADR Dependencies

| Field | Value |
|-------|-------|
| **Depends On** | ADR-0004 (Skill System — SkillData SO, ISkillDatabase, ICombatStateProvider.RegisterState), ADR-0006 (Damage Pipeline — OnAttackHit event, AttackData), ADR-0008 (Event Architecture — event delegate pattern) |
| **Enables** | Battle HUD (focus bar, skill slots), audio/VFX (unlock effects), match management (round reset coordination) |
| **Blocks** | Match Management (needs focus/draw reset), Battle HUD (needs focus/skill data) |
| **Ordering Note** | Core/Feature layer. Requires ADR-0004, ADR-0006, ADR-0008. Must be Accepted before ADR-0010. |

## Context

### Problem Statement
专注值系统将命中事件转化为可量化的"专注值"资源，达到阈值后触发技能解锁。技能抽取系统从技能数据库构建合格牌池、执行加权随机选择、提供三选一候选。技能装备管理将选中技能注册到格斗状态机。三个系统形成"命中→积累→解锁→抽取→装备→战斗"的完整肉鸽循环管线，需要明确的数据流、接口边界和局间重置语义。

### Constraints
- 三个系统串联运行，专注值解锁到技能装备必须在同帧完成（除 AwaitingSelection 等待玩家输入）
- 技能选择不暂停游戏，选择超时 5 秒
- MVP 每职业 6 个技能，每局最多 4 次抽取
- 跨局技能保留（对局管理 GDD 明确"已装备技能保留，跨局成长弧线"）
- 专注值每局归零，但 UnlockedCount 和 AlreadyDrawnSkillIds 跨局保留（对局级别，非回合级别）
- 无冷却机制，技能使用通过格斗状态机标准帧结构

### Requirements
- FocusFormulas 纯静态类（可单元测试）
- SkillDrawSystem 内部 FSM（Idle → Drawing → AwaitingSelection → Complete）
- 技能槽位按装备顺序填入（1→2→3→4），无位置选择
- 技能输入通过格斗状态机标准输入缓冲，无独立输入系统
- 回合重置 vs 对局重置区分明确

## Decision

采用 **三系统串联管线 + FocusFormulas/DrawFormulas 纯计算层 + 跨局技能保留 + 回合级/对局级双重重置语义** 架构：

### 1. FocusFormulas — 无状态纯计算

```csharp
public static class FocusFormulas
{
    public static float CalculateFocusGain(float baseDamage, float gainRate)
        => baseDamage * gainRate;

    public static float CalculateUnlockThreshold(
        int unlockedCount, float baseThreshold, float thresholdGrowth)
        => baseThreshold + unlockedCount * thresholdGrowth;

    public static float ClampFocus(float focusPoints, float focusCap)
        => Mathf.Min(focusPoints, focusCap);
}
```

### 2. FocusSystem — 每角色状态持有者

FocusSystem 是 MonoBehaviour，持有每角色专注值运行时状态，订阅 OnAttackHit 事件：

```csharp
public struct FocusRuntimeState
{
    public float FocusPoints;
    public int UnlockedCount;
    public float CurrentThreshold;
}
```

**命中处理（在 OnAttackHit 事件回调中，同帧同步执行）：**

```
AttackSystem.OnAttackHit(attackerIndex, attackData, targetIndex)
  → FocusSystem.HandleAttackHit:

  // 攻击者专注值
  attackerGain = FocusFormulas.CalculateFocusGain(attackData.BaseDamage, FocusGainRate_Attacker)
  attackerState.FocusPoints = ClampFocus(attackerState.FocusPoints + attackerGain, FocusCap)
  → OnFocusChanged(attackerIndex, focusPoints, currentThreshold)
  → if focusPoints >= currentThreshold AND unlockedCount < MaxSkillsPerMatch:
       OnFocusReady(attackerIndex, unlockedCount)
       focusPoints -= currentThreshold
       unlockedCount++
       currentThreshold = FocusFormulas.CalculateUnlockThreshold(unlockedCount, ...)

  // 被击者专注值（同一处理流程）
  defenderGain = FocusFormulas.CalculateFocusGain(attackData.BaseDamage, FocusGainRate_Defender)
  ...（同上，独立判定解锁）
```

**关键设计决策**：攻击者和被击者的专注值更新在同一个 OnAttackHit 回调中完成。两者都可以在同一帧触发 OnFocusReady。

### 3. SkillDrawSystem — 抽取状态机

SkillDrawSystem 是 MonoBehaviour，持有每角色抽取状态：

```csharp
public enum DrawPhase { Idle, Drawing, AwaitingSelection, Complete }

public struct DrawRuntimeState
{
    public DrawPhase Phase;
    public HashSet<string> AlreadyDrawnSkillIds;
    public List<SkillData> CurrentCandidates;
    public int RemainingTimeoutFrames;
}
```

**抽取流程：**

```
FocusSystem.OnFocusReady(playerIndex, unlockedCount)
  → SkillDrawSystem.HandleFocusReady:

  if Phase != Idle: return  // 忽略重复触发

  // 1. 构建合格牌池
  allSkills = ISkillDatabase.GetAllSkills()
  eligiblePool = allSkills.Where(s =>
      (s.Tags.IsEmpty || s.Tags.Contains(playerClassName))
      && !AlreadyDrawnSkillIds.Contains(s.SkillId)
      && s != null)

  if eligiblePool.Count == 0:
      Phase = Idle  // 无技能可抽，不消耗解锁次数
      return

  // 2. 计算权重
  DrawFormulas.CalculateWeights(eligiblePool)  // 归一化

  // 3. 无放回抽取 min(3, poolSize) 个候选
  candidates = DrawFormulas.WeightedSampleWithoutReplacement(eligiblePool, min(3, poolSize))

  if candidates.Count == 1:
      // 自动选择，跳过 AwaitingSelection
      OnSkillDrawn(playerIndex, candidates[0])
      AlreadyDrawnSkillIds.Add(candidates[0].SkillId)
      Phase = Idle
      return

  // 4. 发出候选列表
  CurrentCandidates = candidates
  RemainingTimeoutFrames = SelectionTimeout * 60  // 5s × 60fps = 300 帧
  Phase = AwaitingSelection
  OnDrawReady(playerIndex, candidates)
```

**超时处理（FixedUpdate 中每帧检查）：**

```
foreach player in activePlayers:
    if state.Phase == AwaitingSelection:
        state.RemainingTimeoutFrames--
        if state.RemainingTimeoutFrames <= 0:
            SelectCandidate(playerIndex, 0)  // 自动选择第一个
```

**玩家选择（由 UI 输入处理器调用）：**

```
SkillDrawSystem.SelectCandidate(playerIndex, candidateIndex):
    selected = CurrentCandidates[candidateIndex]
    AlreadyDrawnSkillIds.Add(selected.SkillId)
    OnSkillDrawn(playerIndex, selected)
    CurrentCandidates = null
    Phase = Idle
```

**DrawFormulas 静态类：**

```csharp
public static class DrawFormulas
{
    public static void CalculateWeights(List<SkillData> pool) { ... }
    public static List<SkillData> WeightedSampleWithoutReplacement(
        List<SkillData> pool, int count) { ... }
}
```

### 4. SkillEquipmentManager — 技能槽位管理

SkillEquipmentManager 是 MonoBehaviour，持有每角色技能槽位数组：

```csharp
public enum SlotState { Empty, Equipped }

public struct SkillSlot
{
    public SlotState State;
    public SkillData SkillData;
}
```

**装备流程（在 OnSkillDrawn 回调中，同帧完成）：**

```
SkillDrawSystem.OnSkillDrawn(playerIndex, skillData)
  → SkillEquipmentManager.HandleSkillDrawn:

  slotIndex = FindFirstEmptySlot(playerIndex)  // 1→2→3→4 顺序
  if slotIndex == 0: return  // 无可用槽位（不应发生）

  slots[playerIndex][slotIndex] = { Equipped, skillData }

  // 注册到格斗状态机
  stateDefinition = new StateDefinition {
      StateName = skillData.SkillId,
      StartupFrames = skillData.AttackData.StartupFrames,
      ActiveFrames = skillData.AttackData.ActiveFrames,
      RecoveryFrames = skillData.AttackData.RecoveryFrames,
      CancelTable = skillData.AttackData.CancelTable,
  }
  ICombatStateProvider.RegisterState(stateDefinition)

  OnSkillEquipped(playerIndex, slotIndex, skillData)
```

**技能激活（玩家输入）：**

玩家按技能键（1/2/3/4 或 RB/RT/LB/LT）时：
1. 检查目标槽位是否已装备
2. 已装备：将技能输入写入格斗状态机的输入缓冲
3. 格斗状态机按正常优先级处理（技能攻击 > 基础攻击 > 闪避/跳跃）

技能激活不经过 SkillEquipmentManager——输入直接写入格斗状态机的输入缓冲。SkillEquipmentManager 只在装备/卸载时介入。

### 5. 回合重置 vs 对局重置

**关键区分**（解决技能装备管理 GDD 与对局管理 GDD 的不一致）：

| 重置范围 | FocusSystem | SkillDrawSystem | SkillEquipmentManager |
|---------|-------------|-----------------|----------------------|
| **回合间**（BattleEnd → Countdown） | FocusPoints = 0，UnlockedCount 保留 | 取消待选，AlreadyDrawnSkillIds 保留 | **不重置**（技能保留） |
| **对局间**（Results → CharacterSelect） | FocusPoints = 0，UnlockedCount = 0 | 全部清空，AlreadyDrawnSkillIds 清空 | 全部清空，DeregisterAllSkillStates() |

**设计依据**：对局管理 GDD 明确"已装备技能保留（跨局成长弧线）"。技能装备管理 GDD 的"OnRoundStart: 清空所有槽位"与对局管理 GDD 冲突——以对局管理 GDD 为准。

**回合间重置方法命名**：
```csharp
IFocusSystem.ResetForNewRound(playerIndex)    // FocusPoints=0, UnlockedCount preserved
IFocusSystem.ResetForNewMatch(playerIndex)     // FocusPoints=0, UnlockedCount=0
ISkillDrawSystem.ResetForNewRound(playerIndex) // Cancel pending, keep AlreadyDrawn
ISkillDrawSystem.ResetForNewMatch(playerIndex) // Full reset
ISkillEquipmentManager.ResetForNewMatch(playerIndex) // Clear all, DeregisterAllSkillStates
// 无 ResetForNewRound — 技能跨局保留
```

### 6. 接口定义

```csharp
public interface IFocusSystem
{
    float GetFocusPoints(int playerIndex);
    float GetUnlockThreshold(int playerIndex);
    int GetUnlockedCount(int playerIndex);
    void ResetForNewRound(int playerIndex);
    void ResetForNewMatch(int playerIndex);
    void ResetAllForNewRound();
    void ResetAllForNewMatch();

    event Action<int, int> OnFocusReady;            // (playerIndex, unlockedCount)
    event Action<int, float, float> OnFocusChanged; // (playerIndex, points, threshold)
}

public interface ISkillDrawSystem
{
    DrawPhase GetDrawPhase(int playerIndex);
    IReadOnlyList<SkillData> GetCurrentCandidates(int playerIndex);
    void SelectCandidate(int playerIndex, int candidateIndex);
    void ResetForNewRound(int playerIndex);
    void ResetForNewMatch(int playerIndex);
    void ResetAll();

    event Action<int, IReadOnlyList<SkillData>> OnDrawReady; // (playerIndex, candidates)
    event Action<int, SkillData> OnSkillDrawn;               // (playerIndex, selectedSkill)
}

public interface ISkillEquipmentManager
{
    SkillData GetSkillInSlot(int playerIndex, int slotIndex);
    IReadOnlyList<SkillData> GetEquippedSkills(int playerIndex);
    int GetEquippedCount(int playerIndex);
    void ResetForNewMatch(int playerIndex);
    void ResetAll();

    event Action<int, int, SkillData> OnSkillEquipped; // (playerIndex, slotIndex, skillData)
    event Action<int, int> OnSkillUnequipped;          // (playerIndex, slotIndex)
}
```

### Architecture Diagram

```
┌─ Skill Acquisition Pipeline ─────────────────────────────────────┐
│                                                                    │
│  AttackSystem.OnAttackHit(attacker, attackData, target)           │
│       ↓                                                            │
│  FocusSystem.HandleAttackHit                                      │
│    ├── attacker: FocusPoints += BaseDamage × GainRate_Attacker    │
│    ├── target:    FocusPoints += BaseDamage × GainRate_Defender   │
│    ├── OnFocusChanged (both)                                       │
│    └── if threshold reached: OnFocusReady                          │
│       ↓                                                            │
│  SkillDrawSystem.HandleFocusReady                                 │
│    ├── Build eligible pool (class filter + dedup)                  │
│    ├── Calculate weights (rarity pool)                             │
│    ├── Weighted sample 3 candidates                                │
│    └── if candidates > 1:                                          │
│         OnDrawReady → [AwaitingSelection] → timeout or input       │
│         ↓ SelectCandidate                                          │
│    └── OnSkillDrawn                                                │
│       ↓                                                            │
│  SkillEquipmentManager.HandleSkillDrawn                           │
│    ├── Assign to first empty slot (1→2→3→4)                       │
│    ├── ICombatStateProvider.RegisterState(stateDefinition)         │
│    └── OnSkillEquipped                                             │
│                                                                    │
└────────────────────────────────────────────────────────────────────┘
```

## Alternatives Considered

### Alternative 1: 合并 FocusAndDrawSystem
- **Description**: 将专注值和抽取合并为一个 MonoBehaviour
- **Cons**: 专注值是 Core 层（纯资源计算），抽取是 Feature 层（随机选择+UI交互）。合并后职责不清，测试困难
- **Rejection Reason**: 违反单一职责——专注值积累和技能抽取有不同的变化原因（平衡调整 vs UI 交互）

### Alternative 2: 专注值作为 DamageSystem 的一部分
- **Description**: 在 DamageSystem.ProcessHit 中直接计算专注值
- **Cons**: 伤害计算和专注值积累是不同的关注点，有独立的调参旋钮（FocusGainRate 独立于 BaseDamage）
- **Rejection Reason**: FocusSystem 有自己的状态（UnlockedCount, CurrentThreshold）和事件（OnFocusReady, OnFocusChanged），这些不属于伤害系统

### Alternative 3: 技能直接装备无选择
- **Description**: 取消三选一，FocusReady 时直接随机抽取一个技能
- **Cons**: 失去 Pillar 2（每局都是新故事）中的策略元素——"我从这些中选择"的参与感
- **Rejection Reason**: 三选一是设计核心——抽选过程是"开箱"体验的关键部分

## Consequences

### Positive
- FocusFormulas/DrawFormulas 纯静态类可 100% 单元测试
- 三系统串联但松耦合——每个系统只依赖上游事件，不直接引用上游实现
- 跨局技能保留创造"成长弧线"体验——Bo3 中后期角色越来越多样化
- 技能激活复用格斗状态机标准帧结构，无特殊技能执行路径

### Negative
- AwaitingSelection 期间游戏不暂停——玩家需要在战斗中做选择，可能被打断
- 三系统串联意味着 3 个 OnEnable/OnDisable 订阅对需要维护
- 跨局技能保留可能导致后期局角色过于复杂（4+ 已装备技能 + 新抽取）

### Risks
- **GDD 不一致**: 技能装备管理 GDD "OnRoundStart: 清空所有槽位" 与对局管理 GDD "已装备技能保留" 冲突 → 缓解: ADR 以对局管理 GDD 为准，需更新技能装备管理 GDD
- **选择期间被打断**: 玩家在 AwaitingSelection 时被 KO → 缓解: 回合结束时自动选择第一个候选，确保技能不丢失
- **同帧双解锁**: 攻击者和被击者同时达到阈值 → 缓解: 两者独立处理，SkillDrawSystem 为每个玩家维护独立状态
- **AlreadyDrawnSkillIds 跨局污染**: 如果对局结束但未正确重置 → 缓解: MatchManager 协调对局间全量重置

## GDD Requirements Addressed

| GDD System | Requirement | How This ADR Addresses It |
|------------|-------------|--------------------------|
| focus-system.md | "FocusGain = BaseDamage × FocusGainRate" | FocusFormulas.CalculateFocusGain |
| focus-system.md | "递增阈值: Threshold = Base + n × Growth" | FocusFormulas.CalculateUnlockThreshold |
| focus-system.md | "OnFocusReady, OnFocusChanged 事件" | IFocusSystem events |
| focus-system.md | "FocusCap 钳制" | FocusFormulas.ClampFocus |
| focus-system.md | "攻击者 + 被击者双方获取" | HandleAttackHit processes both |
| focus-system.md | "UnlockedCount 跟踪上限" | FocusRuntimeState.UnlockedCount < MaxSkillsPerMatch |
| skill-draw-system.md | "合格牌池: Tags 过滤 + 已抽取去重" | EligiblePool construction |
| skill-draw-system.md | "三层加权随机: 稀有度→池内权重→归一化" | DrawFormulas.CalculateWeights |
| skill-draw-system.md | "三选一候选 + 5 秒超时" | AwaitingSelection + frame counting |
| skill-draw-system.md | "ISkillDrawSystem 接口" | ISkillDrawSystem definition |
| skill-draw-system.md | "OnDrawReady, OnSkillDrawn 事件" | ISkillDrawSystem events |
| skill-equipment-management.md | "SkillSlot[4] 按顺序填入" | FindFirstEmptySlot 1→4 |
| skill-equipment-management.md | "RegisterState 到格斗状态机" | ICombatStateProvider.RegisterState |
| skill-equipment-management.md | "ISkillEquipmentManager 接口" | ISkillEquipmentManager definition |
| skill-equipment-management.md | "OnSkillEquipped, OnSkillUnequipped 事件" | ISkillEquipmentManager events |
| skill-equipment-management.md | "技能输入通过格斗状态机标准输入缓冲" | No independent input system |
| match-management-system.md | "已装备技能保留（跨局成长弧线）" | ResetForNewRound vs ResetForNewMatch |

## Performance Implications
- **CPU**: FocusSystem per-hit < 0.01ms; SkillDrawSystem pool construction < 0.1ms (6 skills); SkillEquipmentManager per-equip < 0.01ms
- **Memory**: FocusRuntimeState × 2 = ~32B; DrawRuntimeState × 2 = ~200B (HashSet + List); SkillSlot[4] × 2 = ~128B
- **GC**: No allocations during focus gain (float arithmetic). DrawFormulas allocates List during candidate generation (~once per unlock). AlreadyDrawnSkillIds grows by 1 entry per draw.
- **Network**: 不适用

## Migration Plan
不适用——新项目初始架构决策。

**GDD 更新提醒**: 技能装备管理 GDD 的"6. 对局生命周期"节需要更新，将"OnRoundStart: 清空所有槽位"改为"OnRoundStart: 不重置（技能跨局保留）"，与对局管理 GDD 保持一致。

## Validation Criteria
- [ ] FocusGain: Warrior GroundAttack (BaseDamage=12.0, Rate=0.30) → 攻击者 +3.6, 被击者 +1.2
- [ ] 递增阈值: n=0→40.0, n=1→45.0, n=2→50.0, n=3→55.0
- [ ] 解锁触发: FocusPoints=38.0 + Gain=3.6 → 41.6 >= 40.0 → OnFocusReady, 剩余 1.6
- [ ] FocusCap 钳制: FocusPoints=53.0 + Gain=4.0 → Min(57.0, 55.0) = 55.0
- [ ] 牌池构建: Warrior 6 技能，已抽 2 → 牌池 4 技能
- [ ] 候选生成: 6 技能牌池 → 3 个不重复候选
- [ ] 权重计算: 4C+1R+1E → Common=0.175, Rare=0.200, Epic=0.100
- [ ] 超时: 5 秒（300 帧）无输入 → 自动选择第一个
- [ ] 装备: OnSkillDrawn → Slot 1 填入 → FSM RegisterState
- [ ] 双人独立: P1 和 P2 各自独立积累/抽取/装备
- [ ] 回合重置: FocusPoints=0, UnlockedCount 保留, 技能保留
- [ ] 对局重置: 全部归零, DeregisterAllSkillStates
- [ ] 管线帧耗时: 一次命中触发的 FocusSystem 处理 < 0.1ms

## Related Decisions
- ADR-0004: Skill System — SkillData SO, ISkillDatabase, RegisterState
- ADR-0006: Damage Pipeline — OnAttackHit event source
- ADR-0008: Event Architecture — event delegate pattern and signatures
- ADR-0010: Match & Round Lifecycle — round reset coordination
