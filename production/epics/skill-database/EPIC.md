# Epic: 技能数据库

> **Layer**: Core
> **GDD**: design/gdd/skill-database.md
> **Architecture Module**: 技能数据库 (ISkillDatabase)
> **Status**: Ready
> **Stories**: Not yet created — run `/create-stories skill-database`

## Overview

实现技能数据的定义、存储和查询。每个技能通过 SkillData ScriptableObject 配置，包含 ID、名称、描述、稀有度、标签、StateDefinition（FSM 注入数据）、AttackData（攻击属性）。提供按 ID、稀有度、标签的只读查询接口。MVP 包含 10-15 个技能 SO。系统为只读数据提供者，被技能抽取系统消费。

## Governing ADRs

| ADR | Decision Summary | Engine Risk |
|-----|-----------------|-------------|
| ADR-0004: Skill System Data-Driven | SkillData SO 结构、ISkillDatabase 接口、数据验证、只读查询 | LOW |

## GDD Requirements

| TR Range | Count | ADR Coverage |
|----------|-------|--------------|
| TR-SKD-001 ~ TR-SKD-025 | 25 | ADR-0004 |

Full requirement list: `docs/architecture/tr-registry.yaml` (SKD section)

## Existing Code

| File | Type |
|------|------|
| Core/Enums/Rarity.cs | Enum |
| Core/Data/SkillData.cs | Data struct |
| Core/Interfaces/ISkillDatabase.cs | Interface |

**Status**: 数据结构和接口已定义，SO 实例创建和查询实现待开发。

## Stories

| # | Story | Type | Estimate | Status | Test File |
|---|-------|------|----------|--------|-----------|
| 1 | [SkillData SO Validation](story-001-skilldata-so-validation.md) | Logic | M | Ready | skilldata_so_validation_test.cs |
| 2 | [Skill Database Implementation](story-002-skill-database-implementation.md) | Logic | M | Ready | skill_database_implementation_test.cs |
| 3 | [MVP Skill Instances](story-003-mvp-skill-instances.md) | Logic | L | Ready | mvp_skill_instances_test.cs |
| 4 | [Data Validation](story-004-data-validation.md) | Logic | S | Ready | data_validation_test.cs |

**Total**: 4 stories | Estimate: 1L + 2M + 1S

## Definition of Done

This epic is complete when:
- All stories are implemented, reviewed, and closed via `/story-done`
- All acceptance criteria from `design/gdd/skill-database.md` are verified
- All Logic and Integration stories have passing test files in `tests/`
- All Visual/Feel and UI stories have evidence docs with sign-off in `production/qa/evidence/`

## Next Step

Run `/create-stories skill-database` to break this epic into implementable stories.
