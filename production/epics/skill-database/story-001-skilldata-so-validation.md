# Story 001: SkillData SO Validation

> **Epic**: 技能数据库
> **Status**: Ready
> **Layer**: Core
> **Type**: Logic
> **Estimate**: M
> **Manifest Version**: 2026-05-24
> **Last Updated**: 2026-05-24

## Context

**GDD**: `design/gdd/skill-database.md`
**Requirement**: `TR-SKD-001~007`
**ADR Governing Implementation**: ADR-0004: Skill System Data-Driven
**ADR Decision Summary**: 所有游戏数据使用 ScriptableObject，运行时只读。SkillData SO 必须实现 OnValidate() 进行编辑器数据完整性检查。AttackData 是统一结构，职业招式和技能共享。
**Engine**: Unity 2022.3.51 LTS | **Risk**: LOW

**Control Manifest Rules (Core)**:
- Required: All SO data must be read-only at runtime; runtime state held by separate system managers
- Required: AttackData is unified struct shared by ClassData and SkillData
- Required: Each SO must implement OnValidate() for editor-time data integrity checks
- Required: StateDefinition must be readonly struct, stack-allocated, zero GC

---

## Acceptance Criteria

- [ ] SkillData ScriptableObject 包含：SkillId (string), DisplayName (string), Description (string), Rarity (enum), RarityWeight (float), Tags (string[]), AttackData (struct), Icon (Sprite), VFXColor (Color)
- [ ] OnValidate() 检查：SkillId 非空、格式为 `skill_[kebab-case]`
- [ ] OnValidate() 检查：Rarity 为合法枚举值（Common/Rare/Epic）
- [ ] OnValidate() 检查：RarityWeight >= 0（负数钳制为 0.0）
- [ ] OnValidate() 检查：AttackData.StartupFrames + ActiveFrames + RecoveryFrames > 0
- [ ] OnValidate() 检查：AttackData.HitboxSize > (0, 0)
- [ ] OnValidate() 检查：投射物技能（IsProjectile=true）时 ProjectileSpeed > 0 且 ProjectileLifetime > 0
- [ ] OnValidate() 检查：非投射物技能时 ProjectileSpeed 和 ProjectileLifetime 不强制验证
- [ ] OnValidate() 检查：ProjectileLifetime=0 时拒绝加载（零寿命投射物无意义）
- [ ] 所有字段使用 [SerializeField] private，通过公共只读属性暴露

---

## Implementation Notes

- SkillData 继承 ScriptableObject，使用 [CreateAssetMenu] 属性方便编辑器创建
- AttackData 使用已有的统一结构（与 ClassData 共享）
- OnValidate() 在 Inspector 修改时自动调用，使用 Debug.LogError/LogWarning 报告问题
- 运行时只读保护：公共属性只有 get，无 set
- Rarity 枚举使用已有的 Core/Enums/Rarity.cs
- Tags 数组为空时视为通用技能（所有职业可抽取）
- DisplayName 和 Description 存储本地化 key，直接文本值也可用于 MVP

---

## Out of Scope

- SkillDatabase 查询实现（Story 002）
- 具体技能 SO 资产创建（Story 003）
- 跨 SO 重复 ID 验证（Story 004）
- 技能装备和运行时状态管理（skill-equipment epic）

---

## QA Test Cases

- **AC-1**: SkillId 验证
  - Given: SkillData SO
  - When: SkillId = "" (空)
  - Then: OnValidate 报错 "SkillId must be non-empty"
  - Edge cases: SkillId = "fireball"（缺 skill_ 前缀）→ 警告格式不符

- **AC-2**: AttackData 帧数验证
  - Given: AttackData.StartupFrames=0, ActiveFrames=0, RecoveryFrames=0
  - When: OnValidate
  - Then: 报错 "Total frames must be > 0"
  - Edge cases: Startup=1, Active=1, Recovery=0 → 通过（合法）

- **AC-3**: HitboxSize 验证
  - Given: HitboxSize = (0, 0)
  - When: OnValidate
  - Then: 报错 "HitboxSize must be > (0, 0)"
  - Edge cases: HitboxSize = (0.1, 0.1) → 通过

- **AC-4**: 投射物字段验证
  - Given: IsProjectile=true, ProjectileSpeed=0
  - When: OnValidate
  - Then: 报错 "ProjectileSpeed must be > 0 for projectile skills"
  - Edge cases: IsProjectile=false, ProjectileSpeed=0 → 无警告

- **AC-5**: RarityWeight 验证
  - Given: RarityWeight = -1.0
  - When: OnValidate
  - Then: RarityWeight 钳制为 0.0，警告
  - Edge cases: RarityWeight = 0.0 → 通过（合法，表示禁用）

---

## Test Evidence

**Story Type**: Logic
**Required evidence**: `tests/unit/skill-database/skilldata_so_validation_test.cs`
**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: Core/Enums/Rarity.cs (已有), Core/Data/SkillData.cs (已有结构)
- Unlocks: Story 002 (数据库实现), Story 003 (SO 资产), Story 004 (数据验证)
