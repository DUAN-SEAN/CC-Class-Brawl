# Story 002: Skill Database Implementation

> **Epic**: 技能数据库
> **Status**: Ready
> **Layer**: Core
> **Type**: Logic
> **Estimate**: M
> **Manifest Version**: 2026-05-24
> **Last Updated**: 2026-05-24

## Context

**GDD**: `design/gdd/skill-database.md`
**Requirement**: `TR-SKD-008~015`
**ADR Governing Implementation**: ADR-0004: Skill System Data-Driven
**ADR Decision Summary**: SkillDatabase 是单一 ScriptableObject 持有 List<SkillData>，暴露只读 ISkillDatabase 接口。提供按 ID、稀有度、标签的查询方法。运行时只读，不可修改。
**Engine**: Unity 2022.3.51 LTS | **Risk**: LOW

**Control Manifest Rules (Core)**:
- Required: SkillDatabase is a single SO holding List<SkillData> with read-only ISkillDatabase interface
- Required: All SO data must be read-only at runtime
- Guardrail: ISkillDatabase query < 0.01ms

---

## Acceptance Criteria

- [ ] SkillDatabase MonoBehaviour 实现 ISkillDatabase 接口
- [ ] GetAllSkills() 返回所有已注册 SkillData 的只读列表
- [ ] GetSkillById(SkillId) 按 ID 精确查询，不存在返回 null
- [ ] GetSkillsByRarity(Rarity) 按稀有度筛选返回匹配列表
- [ ] GetSkillsByTag(Tag) 按标签筛选（用于职业专属池过滤）
- [ ] GetSkillCount() 返回技能总数
- [ ] GetTotalWeight(Rarity) 返回指定稀有度的 RarityWeight 总和
- [ ] 初始化时加载所有 SkillData 并构建内部索引（Dictionary<string, SkillData>）
- [ ] 查询接口返回 SkillData 引用（不复制）
- [ ] GetSkillById("skill_fireball") → 返回 Fireball 的 SkillData
- [ ] GetSkillById("skill_nonexistent") → 返回 null

---

## Implementation Notes

- SkillDatabase 使用 [SerializeField] private List<SkillData> _skills 存储数据
- 初始化时（Awake 或 OnEnable）构建 Dictionary<string, SkillData> _skillIndex 和按稀有度的缓存
- ISkillDatabase 接口方法均为只读，不暴露修改能力
- GetSkillsByTag 使用 Tags 数组的 Contains 检查
- GetTotalWeight 遍历指定稀有度的技能，累加 RarityWeight
- 考虑使用 IReadOnlyList<SkillData> 或 List.AsReadOnly() 返回只读列表
- 查询耗时 < 0.01ms 通过 Dictionary O(1) 和预构建索引保证

---

## Out of Scope

- SkillData SO 字段验证（Story 001）
- 具体技能 SO 资产创建（Story 003）
- 跨 SO 重复 ID 和数据一致性验证（Story 004）
- 技能抽取逻辑（skill-draw epic）
- 技能装备管理（skill-equipment epic）

---

## QA Test Cases

- **AC-1**: GetAllSkills
  - Given: 数据库已加载 10 个技能
  - When: 调用 GetAllSkills()
  - Then: 返回包含 10 个 SkillData 的只读列表
  - Edge cases: 空数据库 → 返回空列表

- **AC-2**: GetSkillById 成功
  - Given: 数据库已加载，包含 skill_fireball
  - When: 调用 GetSkillById("skill_fireball")
  - Then: 返回 Fireball 的 SkillData
  - Edge cases: 大小写不匹配 → 返回 null（精确匹配）

- **AC-3**: GetSkillById 不存在
  - Given: 数据库已加载 10 个技能
  - When: 调用 GetSkillById("skill_nonexistent")
  - Then: 返回 null
  - Edge cases: 空字符串 → 返回 null

- **AC-4**: GetSkillsByRarity
  - Given: 数据库已加载 10 个技能（7 Common, 2 Rare, 1 Epic）
  - When: 调用 GetSkillsByRarity(Rare)
  - Then: 返回 2 个 Rare 技能（蓄力重击、影步）
  - Edge cases: 稀有度无匹配 → 返回空列表

- **AC-5**: GetSkillsByTag
  - Given: 数据库已加载，Tags 包含 "warrior" 的有 2 个
  - When: 调用 GetSkillsByTag("warrior")
  - Then: 返回匹配的战士专属技能
  - Edge cases: 无匹配 Tag → 返回空列表

- **AC-6**: GetTotalWeight
  - Given: Common 有 7 个技能，每个 RarityWeight=1.0
  - When: 调用 GetTotalWeight(Common)
  - Then: 返回 7.0
  - Edge cases: 稀有度无技能 → 返回 0.0

---

## Test Evidence

**Story Type**: Logic
**Required evidence**: `tests/unit/skill-database/skill_database_implementation_test.cs`
**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: Story skill-database/001 (SkillData SO 结构), ISkillDatabase 接口 (已有)
- Unlocks: Story 003 (SO 资产), Story 004 (数据验证)
