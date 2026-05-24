# ADR-0004: Skill System Data-Driven Architecture — ScriptableObject + Dynamic FSM Registration

## Status
Accepted

## Date
2026-05-24

## Engine Compatibility

| Field | Value |
|-------|-------|
| **Engine** | Unity 2022.3.51 LTS |
| **Domain** | Core (Data Architecture) |
| **Knowledge Risk** | LOW — version is within LLM training data |
| **References Consulted** | `docs/engine-reference/unity/VERSION.md` |
| **Post-Cutoff APIs Used** | None |
| **Verification Required** | None |

## ADR Dependencies

| Field | Value |
|-------|-------|
| **Depends On** | ADR-0002 (Dual FSM — ICombatStateProvider.RegisterState interface) |
| **Enables** | Skill Draw System, Skill Equipment Management, all data-driven content creation |
| **Blocks** | Skill system pipeline, class system data layer, arena configuration |
| **Ordering Note** | Must be Accepted before skill and class data implementation |

## Context

### Problem Statement
职业对决需要为多种游戏数据（职业参数、技能定义、场地配置、攻击数据）提供数据驱动方案，使设计师可以在 Unity Inspector 中调整数值而无需修改代码。同时，技能系统需要运行时动态地将技能数据注册到格斗状态机，支持"空手开局 → 逐步解锁"的肉鸽循环。

### Constraints
- MVP: 3 个 ClassData + 10 个 SkillData + 1-3 个 ArenaConfig — 总量极小
- 所有游戏数值运行时只读，不可修改
- 攻击系统必须统一处理职业基础招式和技能招式——不应有代码分支区分"来源"
- 技能注册/注销发生在每局开始和每次抽取时，频率低（每局 4-8 次）
- 未来可能迁移到 Addressables（当前 MVP 直接引用）

### Requirements
- 设计师可在 Inspector 中编辑所有游戏数值
- 新增职业/技能/场地只需创建新 SO 资产，不改代码
- 多角色使用同一 SkillData 时互不影响
- 数据在 Editor 和 Build 中行为一致
- 编辑期数据完整性验证（空 ID、零帧攻击等）

## Decision

采用 **ScriptableObject 数据驱动 + 统一 AttackData + 动态 FSM 注册** 架构：

### 1. ScriptableObject 用于所有游戏数据

| 数据类型 | SO 类名 | 内容 | 数量(MVP) |
|---------|---------|------|-----------|
| 职业数据 | ClassData | 移动参数、攻击参数、视觉标识、技能池标签 | 3 |
| 技能数据 | SkillData | AttackData + 稀有度 + 标签 + 图标 + 特效色 | 10 |
| 场地配置 | ArenaConfig | Blast Zone、摄像机边界、平台定义、生成点 | 1-3 |

**运行时只读原则**：所有 SO 数据在加载后不可修改。运行时状态（DamagePercent, FocusPoints, SkillSlot）由各自的系统管理器持有，与 SO 数据完全分离。

**SO 实现约束**：
- `List<T>` 字段不在声明时初始化（让 Unity 序列化器处理）
- 使用标准可序列化类型（int, float, string, enum, Vector2, Color, Sprite 引用）
- Sprite 引用当前为直接引用；迁移到 Addressables 时改为 `AssetReferenceSprite`

### 2. 统一 AttackData 结构

AttackData 定义为独立结构体，被 ClassData SO 和 SkillData SO 共同引用：

```csharp
[Serializable]
public struct AttackData
{
    public string AttackId;
    public int StartupFrames;
    public int ActiveFrames;
    public int RecoveryFrames;
    public int HitStunFrames;
    public float BaseDamage;
    public float BaseKnockback;
    public bool IsProjectile;
    public int HitstopFrames;
    public Vector2 HitboxOffset;
    public Vector2 HitboxSize;
    public float ProjectileSpeed;
    public int ProjectileLifetime;
    public CancelEntry[] CancelTable;
}
```

攻击系统消费 AttackData 时不区分来源——职业基础招式和技能招式走完全相同的代码路径。

### 3. SkillDatabase 作为 SO 集合

一个 SkillDatabase SO 持有 `List<SkillData>` 并提供只读查询接口：

```csharp
public interface ISkillDatabase
{
    IReadOnlyList<SkillData> GetAllSkills();
    SkillData GetSkillById(string skillId);
    IReadOnlyList<SkillData> GetSkillsByRarity(Rarity rarity);
    IReadOnlyList<SkillData> GetSkillsByTag(string tag);
    int GetSkillCount();
}
```

加载方式：MVP 通过 Inspector 直接引用。GameManager / SceneInitializer 在场景加载时持有 SkillDatabase SO 引用，注入到需要查询的系统。

### 4. StateDefinition 为 readonly struct

技能注册到格斗状态机的数据使用 readonly struct（不是 ScriptableObject）：

```csharp
public readonly struct StateDefinition
{
    public readonly string StateId;
    public readonly int StartupFrames;
    public readonly int ActiveFrames;
    public readonly int RecoveryFrames;
    public readonly CancelEntry[] CancelTable;
    public readonly InputType InputMapping;
}
```

理由：StateDefinition 是运行时从 SkillData.AttackData 创建的临时数据，不需要 Unity 序列化，不需要 Inspector 编辑。readonly struct 保证不可变性，栈分配零 GC 压力。

### 5. 动态注册流程

```
OnSkillDrawn(CharacterId, SkillData)
  → EquipmentManager 找到空槽位
  → 从 SkillData.AttackData 创建 StateDefinition
  → ICombatStateProvider.RegisterState(stateDefinition)
  → Combat FSM 将 StateDefinition 存入 Dictionary<string, StateDefinition>
  → 技能输入映射激活

OnRoundStart
  → ICombatStateProvider.DeregisterAllSkillStates()
  → Combat FSM 清除所有技能 StateDefinition
  → 基础攻击定义保留
```

每角色独立的 FSM 字典，两个角色装备同一 SkillData 时各自独立注册，互不影响。SkillData 本身是共享引用（只读），运行时状态独立。

### 6. 数据验证策略

**编辑期**：每个 SO 实现 `OnValidate()` 检查数据完整性：
- SkillId 非空且格式正确
- StartupFrames + ActiveFrames + RecoveryFrames > 0
- HitboxSize > (0, 0)
- 投射物技能 ProjectileSpeed > 0, ProjectileLifetime > 0

**Build 期**：不做运行时验证。信任编辑期验证结果，避免运行时开销。如果数据错误导致异常行为，视为编辑期遗漏的 bug。

### Architecture Diagram

```
┌─ Unity Editor ──────────────────────────────────────┐
│                                                       │
│  Designer creates SOs:                                │
│    ClassData (3) — movement/attack/visual params     │
│    SkillData (10) — attack data + rarity + tags      │
│    ArenaConfig (1-3) — blast zone + platforms        │
│                                                       │
│  OnValidate() → data integrity checks                │
│                                                       │
└───────────────────────────────────────────────────────┘
                    ↓ Build / Play
┌─ Runtime ────────────────────────────────────────────┐
│                                                       │
│  SkillDatabase SO ──→ ISkillDatabase queries          │
│       ↓                                               │
│  SkillDrawSystem ──→ OnSkillDrawn event               │
│       ↓                                               │
│  EquipmentManager ──→ creates StateDefinition (struct)│
│       ↓                                               │
│  CombatFSM ──→ RegisterState(def) → Dictionary       │
│       ↓                                               │
│  AttackSystem ──→ consumes AttackData (unified)      │
│                                                       │
└───────────────────────────────────────────────────────┘
```

### Key Interfaces

- `AttackData` struct — 统一攻击数据格式
- `SkillData : ScriptableObject` — 技能定义 SO
- `ClassData : ScriptableObject` — 职业定义 SO
- `ArenaConfig : ScriptableObject` — 场地配置 SO
- `StateDefinition` readonly struct — FSM 注册数据
- `ISkillDatabase` — 技能只读查询接口

## Alternatives Considered

### Alternative 1: JSON/CSV 外部数据文件
- **Description**: 游戏数值存储在 JSON 或 CSV 文件中，运行时解析加载
- **Pros**: 可用外部工具编辑（Excel、文本编辑器）；版本控制 diff 清晰
- **Cons**: 失去 Unity Inspector 编辑能力；需要自定义解析器和序列化；Sprite/GameObject 引用无法直接存储；需要额外工具链
- **Rejection Reason**: SO 在 Unity Inspector 中原生可编辑，设计师无需离开 Unity。MVP 数据量小（<20 SO），不需要外部工具链的额外复杂度

### Alternative 2: 硬编码在 C# 常量类中
- **Description**: 所有游戏数值定义为 static readonly 字段或常量
- **Pros**: 零加载开销；编译期类型安全；无序列化风险
- **Cons**: 任何数值调整需要重新编译；设计师无法独立调整数值；新增职业/技能必须改代码并重新构建
- **Rejection Reason**: 违反"数据驱动"原则。格斗游戏的平衡调优需要频繁修改数值，编译-构建-测试循环太慢

## Consequences

### Positive
- 设计师在 Inspector 中调整数值，无需程序员介入
- 新增内容（职业/技能/场地）只需创建新 SO 资产
- 统一 AttackData 格式消除代码分支，降低 bug 风险
- SO 引用共享减少内存占用（10 个技能 ~4KB）
- OnValidate 在编辑期捕获数据错误，Build 中零运行时验证开销

### Negative
- SO 在 Inspector 中的编辑体验不如专用工具（如 Excel）适合大规模数值表
- Sprite 引用当前为直接引用，迁移到 Addressables 需要字段类型变更
- SO 数据无法在运行时热重载（需要重新进入 Play Mode）

### Risks
- **共享引用污染**: 如果代码意外修改 SO 字段，影响所有使用该 SO 的角色 → 缓解: 运行时只读原则 + 接口设计不暴露 setter
- **List 初始化覆盖**: SO 的 `List<T>` 字段如果在声明时 new，Unity 序列化器可能覆盖 → 缓解: 不在字段声明时初始化 List
- **Addressables 迁移**: 当前直接引用 Sprite，迁移到 Addressables 需要改为 AssetReference → 缓解: 在 Migration Plan 中记录迁移路径，预留注释

## GDD Requirements Addressed

| GDD System | Requirement | How This ADR Addresses It |
|------------|-------------|--------------------------|
| skill-database.md | "以 Unity ScriptableObject 为载体" | SO 数据驱动架构 |
| skill-database.md | "运行时只读不可修改" | 运行时只读原则 |
| skill-database.md | "AttackData 格式与职业基础招式完全一致" | 统一 AttackData struct |
| skill-database.md | "查询接口供技能抽取系统和装备管理使用" | ISkillDatabase 只读接口 |
| skill-equipment-management.md | "RegisterState(stateDefinition)" | readonly struct StateDefinition + 动态注册 |
| skill-equipment-management.md | "DeregisterAllSkillStates() 在回合重置" | 注册/注销流程 |
| attack-system.md | "统一处理两种来源的 AttackData" | 统一 AttackData 格式 |
| class-system.md | "ClassData (SO)" | SO 数据驱动覆盖职业数据 |
| 3c-system.md | "所有游戏数值存储在 ScriptableObject 中" | SO 作为唯一数据容器 |

## Performance Implications
- **CPU**: Dictionary<string, StateDefinition> 查找 < 1μs；ISkillDatabase 查询 < 0.01ms
- **Memory**: 10 SkillData × ~400B + 3 ClassData × ~300B = ~5KB；StateDefinition struct × 8 (4 skill × 2 char) = 栈分配，零 GC
- **Load Time**: SO 直接引用加载 < 1ms（<20 个资产）
- **Network**: 不适用

## Migration Plan
不适用——新项目初始架构决策。

**Future: Addressables 迁移路径**：
- 将 SkillDatabase 从 Inspector 直接引用改为 Addressables 异步加载
- 将 SkillData.Icon (Sprite) 改为 AssetReferenceSprite
- 将 ClassData 和 ArenaConfig 同理迁移
- 预留：在 SO 字段上添加注释标记未来迁移点

## Validation Criteria
- [ ] 所有 ClassData/SkillData/ArenaConfig 为 ScriptableObject
- [ ] 运行时无代码修改 SO 字段值
- [ ] AttackData 格式在 ClassData 和 SkillData 中完全一致
- [ ] SkillDatabase 查询返回正确的结果（按 ID、稀有度、标签）
- [ ] RegisterState 后技能输入可触发对应攻击
- [ ] DeregisterAllSkillStates 后已注册技能输入不再触发
- [ ] 两个角色装备同一 SkillData 时互不影响
- [ ] OnValidate 捕获空 ID、零帧攻击、零 HitboxSize 等数据错误
- [ ] Editor 和 Build 中数据行为一致

## Related Decisions
- ADR-0002: Dual FSM Architecture — StateDefinition 注册到 CombatFSM 的接口定义
- ADR-0005: Input System — 技能输入映射使用 SO 定义的 InputType
- ADR-0010 (future): Data Container Strategy — 本 ADR 是 SO 策略的具体实施
