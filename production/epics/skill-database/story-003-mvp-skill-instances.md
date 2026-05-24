# Story 003: MVP Skill Instances

> **Epic**: 技能数据库
> **Status**: Ready
> **Layer**: Core
> **Type**: Logic
> **Estimate**: L
> **Manifest Version**: 2026-05-24
> **Last Updated**: 2026-05-24

## Context

**GDD**: `design/gdd/skill-database.md`
**Requirement**: `TR-SKD-016~020`
**ADR Governing Implementation**: ADR-0004: Skill System Data-Driven
**ADR Decision Summary**: MVP 包含 10 个技能 SO 资产，按稀有度分布为 7 Common + 2 Rare + 1 Epic。每个技能包含完整的 AttackData、稀有度、标签。通过 SkillDatabase SO 统一管理。
**Engine**: Unity 2022.3.51 LTS | **Risk**: LOW

**Control Manifest Rules (Core)**:
- Required: All game data must use ScriptableObject
- Required: AttackData is unified struct shared by ClassData and SkillData
- Required: Each SO must implement OnValidate() for editor-time data integrity checks

---

## Acceptance Criteria

- [ ] 创建 10 个 SkillData ScriptableObject 资产，通过 OnValidate() 验证
- [ ] Common (7): skill_counter-strike, skill_spinning-kick, skill_dash-strike, skill_shield-bash, skill_double-slash, skill_ice-arrow, skill_fireball
- [ ] Rare (2): skill_power-strike, skill_shadow-step
- [ ] Epic (1): skill_meteor
- [ ] 战士专属 (Tags=["warrior"]): skill_shield-bash, skill_power-strike
- [ ] 盗贼专属 (Tags=["rogue"]): skill_double-slash, skill_shadow-step
- [ ] 法师专属 (Tags=["mage"]): skill_ice-arrow, skill_fireball
- [ ] 通用 (Tags=[]): skill_counter-strike, skill_spinning-kick, skill_dash-strike, skill_meteor
- [ ] 每个技能的 AttackData 字段值与 GDD 技能数据总览表一致
- [ ] 所有 SkillId 全局唯一、格式正确
- [ ] 投射物技能（ice-arrow, fireball）的 ProjectileSpeed 和 ProjectileLifetime > 0
- [ ] 稀有度权重：CommonPoolWeight=0.7, RarePoolWeight=0.2, EpicPoolWeight=0.1 总和=1.0

---

## Implementation Notes

- 在 Assets/Data/Skills/ 目录下创建 10 个 SkillData .asset 文件
- 使用 [CreateAssetMenu(fileName = "SkillData", menuName = "Class Brawl/SkillData")] 自动创建
- 每个技能的数据严格按照 GDD "技能数据总览表" 和各技能详细数据表
- VFXColor 按稀有度：Common=#4080FF, Rare=#A040FF, Epic=#FFB020
- Icon 字段 MVP 可暂设为 null（待美术资源）
- DisplayName 和 Description MVP 使用中文直文本
- 验证每个技能通过 OnValidate() 无错误
- 创建 SkillDatabase SO 资产引用全部 10 个 SkillData

---

## Out of Scope

- SkillData SO 验证逻辑（Story 001）
- SkillDatabase 查询实现（Story 002）
- 重复 ID 检测（Story 004）
- 技能图标 Sprite 资产（美术资源）
- 技能视觉特效实现（Presentation 层）

---

## QA Test Cases

- **AC-1**: 10 个技能加载
  - Given: SkillDatabase 已初始化
  - When: 查询所有技能
  - Then: 恰好返回 10 个 SkillData
  - Edge cases: 确认无遗漏

- **AC-2**: 稀有度分布
  - Given: 10 个技能已加载
  - When: 按稀有度统计
  - Then: Common=7, Rare=2, Epic=1
  - Edge cases: 每个 Rarity 值唯一对应正确的技能

- **AC-3**: 战士技能池
  - Given: 战士职业查询可用技能池
  - When: 过滤 Tags 匹配（通用 + warrior）
  - Then: 返回 6 个技能（#1,#2,#3,#4,#5,#10）
  - Edge cases: 确认不包含盗贼/法师专属

- **AC-4**: 盗贼技能池
  - Given: 盗贼职业查询可用技能池
  - When: 过滤 Tags 匹配（通用 + rogue）
  - Then: 返回 6 个技能（#1,#2,#3,#6,#7,#10）

- **AC-5**: 法师技能池
  - Given: 法师职业查询可用技能池
  - When: 过滤 Tags 匹配（通用 + mage）
  - Then: 返回 6 个技能（#1,#2,#3,#8,#9,#10）

- **AC-6**: 数据一致性
  - Given: 所有 10 个技能
  - When: 检查 AttackData
  - Then: StartupFrames + ActiveFrames + RecoveryFrames > 0, HitboxSize > (0,0)
  - Edge cases: 投射物技能 ProjectileSpeed > 0

---

## Test Evidence

**Story Type**: Logic
**Required evidence**: `tests/unit/skill-database/mvp_skill_instances_test.cs`
**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: Story skill-database/001 (SkillData SO), Story skill-database/002 (SkillDatabase)
- Unlocks: Story 004 (数据验证), skill-draw epic (查询技能池)
