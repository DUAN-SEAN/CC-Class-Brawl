# Story: MVP Class Data Instances

> **Epic**: class-system
> **Status**: Ready
> **Layer**: Core
> **Type**: Config/Data
> **Estimate**: S (2 hours)
> **Control Manifest Version**: 2026-05-24

## Context

- **GDD**: `design/gdd/class-system.md` (Formulas sections 1-3)
- **TR Range**: TR-CLS-001
- **Governing ADR**: ADR-0004 (Skill System Data-Driven)
- **Engine**: Unity 2022.3.51 LTS, LOW risk

## Summary

创建三个 MVP 职业的 ClassData ScriptableObject 实例：战士（Warrior）、盗贼（Rogue）、法师（Mage）。按照 GDD Formulas 章节定义的精确数值配置移动属性、基础招式帧数据和视觉身份。每个 SO 资产通过 OnValidate() 验证数据完整性。

## Acceptance Criteria (from GDD)

### 职业差异化
- **GIVEN** 选择了战士职业, **WHEN** 查询移动属性, **THEN** MaxGroundSpeed < 5.0（默认值）且 DashDistance > 2.5（默认值）
- **GIVEN** 选择了盗贼职业, **WHEN** 查询移动属性, **THEN** MaxGroundSpeed > 5.0 且 JumpHeight > 3.5
- **GIVEN** 选择了法师职业, **WHEN** 查询 GroundAttack, **THEN** IsProjectile = true
- **GIVEN** 选择了战士职业, **WHEN** 查询所有招式, **THEN** 所有 IsProjectile = false
- **GIVEN** 选择了盗贼职业, **WHEN** 查询 GroundAttack, **THEN** StartupFrames < 战士的 GroundAttack StartupFrames

### 视觉身份
- **GIVEN** 选择了战士职业, **THEN** SilhouetteScale = 1.2, PrimaryColor = 暖红色系
- **GIVEN** 选择了盗贼职业, **THEN** SilhouetteScale = 0.85, PrimaryColor = 暗绿色系
- **GIVEN** 选择了法师职业, **THEN** SilhouetteScale = 1.0, PrimaryColor = 冷蓝色系

## Implementation Notes (from ADR-0004)

- SO 资产存放在 `Assets/Data/Classes/` 目录
- 文件命名: `WarriorClassData.asset`, `RogueClassData.asset`, `MageClassData.asset`
- 每个职业包含 5 个移动参数、3 个 AttackData、1 个 VisualData
- 法师的 GroundAttack 和 AirAttack 标记 IsProjectile=true
- SkillPoolTags MVP 为空数组（所有职业共享技能池）
- 色彩值已由 art bible 校准

### 战士数值（来自 GDD）
- Movement: MaxGroundSpeed=3.8, MoveAcceleration=40.0, JumpHeight=2.8, MaxAirSpeed=2.8, DashDistance=3.2
- GroundAttack: Startup=8, Active=6, Recovery=14, HitStun=18, Damage=12.0, Knockback=8.0
- AirAttack: Startup=7, Active=5, Recovery=12, HitStun=15, Damage=10.0, Knockback=7.0
- DashAttack: Startup=9, Active=7, Recovery=16, HitStun=22, Damage=15.0, Knockback=12.0
- Visual: BodyType=Bulky, SilhouetteScale=1.2, PrimaryColor=#E84545, SecondaryColor=#F08020

### 盗贼数值（来自 GDD）
- Movement: MaxGroundSpeed=6.5, MoveAcceleration=75.0, JumpHeight=4.2, MaxAirSpeed=4.5, DashDistance=2.0
- GroundAttack: Startup=3, Active=3, Recovery=6, HitStun=8, Damage=4.0, Knockback=2.0
- AirAttack: Startup=3, Active=2, Recovery=7, HitStun=7, Damage=3.0, Knockback=1.5
- DashAttack: Startup=4, Active=4, Recovery=8, HitStun=10, Damage=6.0, Knockback=3.5
- Visual: BodyType=Slim, SilhouetteScale=0.85, PrimaryColor=#2ECC71, SecondaryColor=#203060

### 法师数值（来自 GDD）
- Movement: MaxGroundSpeed=4.8, MoveAcceleration=55.0, JumpHeight=3.5, MaxAirSpeed=3.5, DashDistance=2.5
- GroundAttack: Startup=10, Active=4, Recovery=12, HitStun=12, Damage=7.0, Knockback=4.0, IsProjectile=true
- AirAttack: Startup=8, Active=3, Recovery=10, HitStun=10, Damage=6.0, Knockback=3.5, IsProjectile=true
- DashAttack: Startup=5, Active=5, Recovery=10, HitStun=12, Damage=8.0, Knockback=5.0
- Visual: BodyType=Flowing, SilhouetteScale=1.0, PrimaryColor=#5EADF2, SecondaryColor=#40D0D0

## Out of Scope

- 运行时注入（Story 003）
- UI 展示
- 职业选择流程

## Dependencies

- Story 001 (ClassData SO Creation + Validation) must be DONE

## QA Test Cases

### Config/Data Tests (Smoke Check)

**Test: 战士数据完整性**
- Given: WarriorClassData.asset 加载
- When: 查询所有属性
- Then: MaxGroundSpeed=3.8, 3 个 AttackData 存在，所有 IsProjectile=false, SilhouetteScale=1.2

**Test: 盗贼数据完整性**
- Given: RogueClassData.asset 加载
- When: 查询所有属性
- Then: MaxGroundSpeed=6.5, GroundAttack.StartupFrames=3, SilhouetteScale=0.85

**Test: 法师投射物标记**
- Given: MageClassData.asset 加载
- When: 查询 GroundAttack
- Then: IsProjectile=true
- When: 查询 DashAttack
- Then: IsProjectile=false

**Test: 职业间差异化验证**
- Given: 三个 ClassData SO
- When: 比较关键属性
- Then: 战士最慢最高伤，盗贼最快最低伤，法师有投射物

## Test Evidence

- Smoke check: `production/qa/smoke-class-data.md`
- Test type: Config/Data (ADVISORY)

## Files to Create/Modify

- `Assets/Data/Classes/WarriorClassData.asset` (new — SO instance)
- `Assets/Data/Classes/RogueClassData.asset` (new — SO instance)
- `Assets/Data/Classes/MageClassData.asset` (new — SO instance)
