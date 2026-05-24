# Story 003: 加权随机无放回选择算法

## Epic
skill-draw

## Status
Ready

## Layer
Feature

## Type
Logic

## Estimate
3 hours

## Context
- **GDD**: design/gdd/skill-draw-system.md — Formulas 1-3 (权重计算 + 三选一候选生成)
- **ADR**: ADR-0009 Section 3 (DrawFormulas 静态类)
- **Existing Code**: Feature/Formulas/DrawFormulas.cs（已有骨架，需完善实现）
- **TR-IDs**: TR-SKW-005 (加权随机无放回)
- **Control Manifest**: 2026-05-24

## Acceptance Criteria
- **GIVEN** Warrior 6 技能牌池（4C+1R+1E）, **WHEN** 计算权重, **THEN** 每个 Common=0.175, Rare=0.200, Epic=0.100，总和=1.0
- **GIVEN** Mage 6 技能牌池（5C+0R+1E）, **WHEN** 计算权重, **THEN** 每个 Common=0.175（0.14/0.80）, Epic=0.125（0.10/0.80），总和=1.0
- **GIVEN** 6 技能牌池, **WHEN** 生成候选, **THEN** 恰好返回 3 个唯一技能，均在牌池中
- **GIVEN** 牌池恰好 2 个技能, **WHEN** 生成候选, **THEN** 返回 2 个候选（二选一模式）
- **GIVEN** 牌池恰好 1 个技能, **WHEN** 生成候选, **THEN** 返回 1 个候选
- **GIVEN** 牌池为空（0 技能）, **WHEN** 尝试生成候选, **THEN** 返回空列表
- **GIVEN** 随机值 R 超过累积权重之和（浮点精度）, **WHEN** 执行加权随机, **THEN** 选择最后一个技能作为回退，无越界错误
- **GIVEN** 2 人对战、50 技能数据库, **WHEN** 执行权重计算+候选生成, **THEN** 总处理时间 < 0.5ms

## Implementation Notes
- DrawFormulas 已存在，需完善 CalculateWeights 和 WeightedSampleWithoutReplacement 方法
- 权重计算: PoolWeight_i = (RarityPoolWeight_i / RarityPoolCount_inPool) * SkillRarityWeight_i，然后归一化
- 稀有度池权重: Common=0.7, Rare=0.2, Epic=0.1（来自技能数据库 GDD，通过 SkillData.Rarity 获取）
- 无放回抽取: 每次抽取后从临时池移除已选技能，重新归一化剩余权重
- 浮点精度安全: 累积权重扫描时，如果 R >= 所有累积权重，回退到最后一个技能
- 使用 System.Random 或 Unity.Random.Range 进行随机数生成，注意不要在热路径中创建新 Random 实例
- DrawFormulas 为纯静态类，100% 可单元测试，不依赖 MonoBehaviour

## Out of Scope
- 牌池构建（Story 002）
- 状态机管理（Story 001）
- 超时逻辑（Story 005）

## QA Test Cases
- test_weights_warrior_6skills: Warrior 6 技能 → 权重总和=1.0, Common=0.175
- test_weights_mage_no_rare: Mage 无 Rare → 归一化正确
- test_sample_returns_3_unique: 6 技能牌池 → 3 个不重复候选
- test_sample_2_skills: 2 技能 → 2 个候选
- test_sample_1_skill: 1 技能 → 1 个候选
- test_sample_0_skills: 空牌池 → 空列表
- test_sample_float_precision: 浮点精度边界 → 不越界
- test_sample_statistical_distribution: 大样本统计 → 概率分布近似正确（10000 次抽取验证 Epic ~10%）

## Test Evidence
- 自动化单元测试（Logic story — BLOCKING）
- 测试文件: tests/unit/skill-draw/DrawFormulasTests.cs（已有，需扩展）
- 注意: DrawFormulasTests.cs 可能已有部分测试用例，需检查并扩展

## Dependencies
- Story 002（本 Epic）: 合格牌池作为输入
- SkillData.Rarity / SkillData.SkillDrawWeight: 从技能数据库获取
