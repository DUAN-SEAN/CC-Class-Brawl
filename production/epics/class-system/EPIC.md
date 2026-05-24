# Epic: 职业系统

> **Layer**: Core
> **GDD**: design/gdd/class-system.md
> **Architecture Module**: 职业系统 (IClassData)
> **Status**: Ready
> **Stories**: Not yet created — run `/create-stories class-system`

## Overview

实现职业数据定义和查询。每个职业通过 ClassData ScriptableObject 配置，包含移动参数、攻击参数、视觉身份（轮廓色、主色调）、技能池标签。系统为纯数据提供者，被 3C 系统、攻击系统、技能数据库等消费。MVP 包含 2-3 个基础职业（战士、法师、盗贼）。

## Governing ADRs

| ADR | Decision Summary | Engine Risk |
|-----|-----------------|-------------|
| ADR-0004: Skill System Data-Driven | ClassData SO 结构、运行时注入、数据验证 | LOW |

## GDD Requirements

| TR Range | Count | ADR Coverage |
|----------|-------|--------------|
| TR-CLS-001 ~ TR-CLS-035 | 35 | ADR-0004 |

Full requirement list: `docs/architecture/tr-registry.yaml` (CLS section)

## Existing Code

| File | Type |
|------|------|
| Core/Data/ClassData.cs | Data struct (SO) |
| Foundation/Data/VisualData.cs | Data struct |
| Core/Interfaces/IClassData.cs | Interface |

**Status**: 数据结构和接口已定义，SO 实例创建和运行时查询待开发。

## Definition of Done

This epic is complete when:
- All stories are implemented, reviewed, and closed via `/story-done`
- All acceptance criteria from `design/gdd/class-system.md` are verified
- All Logic and Integration stories have passing test files in `tests/`
- All Visual/Feel and UI stories have evidence docs with sign-off in `production/qa/evidence/`

## Next Step

Run `/create-stories class-system` to break this epic into implementable stories.
