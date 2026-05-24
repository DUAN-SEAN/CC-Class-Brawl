# Story 004: Data Validation

> **Epic**: 技能数据库
> **Status**: Ready
> **Layer**: Core
> **Type**: Logic
**Estimate**: S
> **Manifest Version**: 2026-05-24
> **Last Updated**: 2026-05-24

## Context

**GDD**: `design/gdd/skill-database.md`
**Requirement**: `TR-SKD-021~025`
**ADR Governing Implementation**: ADR-0004: Skill System Data-Driven
**ADR Decision Summary**: SkillDatabase 初始化时验证所有 SkillData 的 SkillId 非空且唯一。拒绝加载空 ID、重复 ID、零帧攻击、零 hitbox 的技能。投射物零速度/零寿命也拒绝加载。
**Engine**: Unity 2022.3.51 LTS | **Risk**: LOW

**Control Manifest Rules (Core)**:
- Required: Each SO must implement OnValidate() for editor-time data integrity checks
- Required: SkillDatabase is a single SO holding List<SkillData> with read-only ISkillDatabase interface
- Guardrail: ISkillDatabase query < 0.01ms

---

## Acceptance Criteria

- [ ] SkillDatabase 初始化时验证所有 SkillId 非空——空 ID 的 SkillData 被拒绝加载并记录错误
- [ ] SkillDatabase 初始化时验证所有 SkillId 唯一——重复 ID 的第二个被拒绝加载并记录错误
- [ ] 零帧攻击（StartupFrames + ActiveFrames + RecoveryFrames = 0）的 SkillData 被拒绝加载
- [ ] HitboxSize = (0, 0) 的 SkillData 被拒绝加载
- [ ] 投射物技能 ProjectileSpeed = 0 或 ProjectileLifetime = 0 被拒绝加载
- [ ] RarityWeight 为负数时钳制为 0.0，记录警告
- [ ] 某稀有度下所有技能 RarityWeight 之和为 0 时：该稀有度不会被抽到（合法但警告）
- [ ] 验证失败的技能不进入查询索引，不影响其他有效技能
- [ ] 运行时尝试修改 SkillData 字段被阻止（只读保护）

---

## Implementation Notes

- 在 SkillDatabase 初始化阶段（Awake/OnEnable），遍历 _skills 列表执行验证
- 构建 _skillIndex (Dictionary) 时检查重复 ID
- 使用 HashSet<string> 追踪已见 ID，发现重复时跳过并记录
- 验证失败的 SkillData 添加到 _invalidSkills 列表（调试用），不进入索引
- 只读保护：SkillData 的公共属性只有 get，运行时修改通过属性保护阻止
- 验证逻辑可提取为 SkillDataValidator 静态类，便于单元测试
- 初始化验证总耗时应在 1ms 内（10 个 SO 的检查）

---

## Out of Scope

- SkillData SO 字段定义（Story 001）
- SkillDatabase 查询实现（Story 002）
- 具体 SO 资产创建（Story 003）
- 技能抽取系统验证（skill-draw epic）
- CancelTable 引用的状态名验证（目标状态可能后续添加）

---

## QA Test Cases

- **AC-1**: 空 SkillId 拒绝
  - Given: SkillData 中有 1 个 SkillId=""
  - When: SkillDatabase 初始化
  - Then: 该技能被拒绝，GetSkillCount() 不包含它
  - Edge cases: 多个空 ID 全部被拒绝

- **AC-2**: 重复 SkillId 拒绝
  - Given: 两个 SkillData 的 SkillId 均为 "skill_fireball"
  - When: SkillDatabase 初始化
  - Then: 第一个正常加载，第二个被拒绝并记录错误
  - Edge cases: 三个重复 → 后两个被拒绝

- **AC-3**: 零帧攻击拒绝
  - Given: SkillData 的 StartupFrames=0, ActiveFrames=0, RecoveryFrames=0
  - When: SkillDatabase 初始化
  - Then: 该技能被拒绝加载
  - Edge cases: Startup=1, Active=0, Recovery=0 → 通过（合法）

- **AC-4**: 零 HitboxSize 拒绝
  - Given: SkillData 的 HitboxSize=(0, 0)
  - When: SkillDatabase 初始化
  - Then: 该技能被拒绝加载
  - Edge cases: HitboxSize=(0.001, 0.001) → 通过

- **AC-5**: 投射物零速度拒绝
  - Given: IsProjectile=true, ProjectileSpeed=0
  - When: SkillDatabase 初始化
  - Then: 该技能被拒绝加载
  - Edge cases: IsProjectile=false, Speed=0 → 通过

- **AC-6**: RarityWeight 负数钳制
  - Given: SkillData.RarityWeight = -0.5
  - When: SkillDatabase 初始化
  - Then: RarityWeight 钳制为 0.0，记录警告
  - Edge cases: RarityWeight=0.0 → 通过，不警告

- **AC-7**: 运行时只读保护
  - Given: 数据库初始化完成
  - When: 尝试修改已加载 SkillData 的 BaseDamage
  - Then: 修改被阻止或无效
  - Edge cases: 确认两个角色共享同一 SkillData 引用时互不影响

---

## Test Evidence

**Story Type**: Logic
**Required evidence**: `tests/unit/skill-database/data_validation_test.cs`
**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: Story skill-database/001 (SkillData SO), Story skill-database/002 (SkillDatabase)
- Unlocks: skill-draw epic (可安全查询验证后的技能池)
