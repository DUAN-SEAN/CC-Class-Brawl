# Story 002: 合格牌池构建 — 职业过滤 + 去重

## Epic
skill-draw

## Status
Ready

## Layer
Feature

## Type
Logic

## Estimate
2 hours

## Context
- **GDD**: design/gdd/skill-draw-system.md — Core Rules 2 (Eligible Pool), Edge Cases (牌池构建)
- **ADR**: ADR-0009 Section 3 (牌池构建逻辑), ADR-0004 (ISkillDatabase, SkillData.Tags)
- **Existing Code**: Feature/Formulas/DrawFormulas.cs（已有公式，需确认接口）
- **TR-IDs**: TR-SKW-001 (FSM 状态)
- **Control Manifest**: 2026-05-24

## Acceptance Criteria
- **GIVEN** Warrior 角色且 AlreadyDrawnSkillIds={}, **WHEN** 构建合格牌池, **THEN** 返回恰好 6 个技能（4 Common + 1 Rare + 1 Epic，Tags 包含 "Warrior" 或为空）
- **GIVEN** Mage 角色且 AlreadyDrawnSkillIds={}, **WHEN** 构建合格牌池, **THEN** 返回恰好 6 个技能（5 Common + 1 Epic，Rare 技能数为 0）
- **GIVEN** Warrior 角色且 AlreadyDrawnSkillIds={skill_counter-strike, skill_dash-strike}, **WHEN** 构建合格牌池, **THEN** 返回恰好 4 个技能（已抽取的 2 个被排除）
- **GIVEN** 技能数据库返回含 null 条目的列表, **WHEN** 构建合格牌池, **THEN** null 被过滤，记录警告，只保留有效 SkillData
- **GIVEN** 技能的 SkillDrawWeight 为负数（数据错误）, **WHEN** 构建牌池, **THEN** 钳制到 0.0，该技能被排除在抽取概率之外
- **GIVEN** 某稀有度在职业池中无技能（如 Mage 无 Rare）, **WHEN** 构建牌池, **THEN** 该稀有度不产生候选，其他稀有度权重按比例提升
- **GIVEN** 2 人对战、50 技能数据库（未来扩展）, **WHEN** 执行牌池构建, **THEN** 处理时间 < 0.3ms

## Implementation Notes
- 牌池构建逻辑在 SkillDrawSystem 中实现，作为 Drawing 状态的核心操作
- 过滤流程：(1) ISkillDatabase.GetAllSkills() → (2) 过滤 null → (3) Tags 匹配（空 Tags = 通用技能） → (4) AlreadyDrawnSkillIds 去重 → (5) 过滤 SkillDrawWeight < 0
- 通过 CharacterId 查询职业名（依赖 IClassSystem 或通过 PlayerSlot 数据）
- 使用 LINQ 或手动循环过滤，注意避免每帧 GC 分配（考虑缓存或对象池）
- Mage 无 Rare 场景在权重计算阶段自然处理，不在此 Story 的过滤逻辑中

## Out of Scope
- 加权随机算法（Story 003）
- 状态机管理（Story 001）
- UI 展示

## QA Test Cases
- test_pool_warrior_initial: Warrior 首次 → 6 个技能
- test_pool_mage_initial: Mage 首次 → 6 个技能（无 Rare）
- test_pool_after_draw: Warrior 已抽 2 → 4 个技能
- test_pool_null_filter: 数据库含 null → 过滤掉 null + 记录警告
- test_pool_negative_weight: SkillDrawWeight < 0 → 排除
- test_pool_empty_result: 所有技能已抽完 → 空牌池

## Test Evidence
- 自动化单元测试（Logic story — BLOCKING）
- 测试文件: tests/unit/skill-draw/EligiblePoolTests.cs

## Dependencies
- ISkillDatabase（上游）: GetAllSkills(), GetSkillById()
- IClassSystem / PlayerSlot（间接）: 通过 CharacterId 查询职业名
- Story 001（本 Epic）: DrawState FSM 调用此牌池构建逻辑
