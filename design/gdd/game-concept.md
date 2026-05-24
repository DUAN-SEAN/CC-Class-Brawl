# Game Concept: 职业对决 (Class Brawl)

*Created: 2026-05-23*
*Status: Draft*

---

## Elevator Pitch

> 一款融合肉鸽随机成长机制的横版格斗游戏。玩家从基础职业起步，在对战中通过积攒专注值随机解锁新技能，每局都创造独一无二的战斗体验。快速、混乱、每局都不一样。

---

## Core Identity

| Aspect | Detail |
| ---- | ---- |
| **Genre** | 平台格斗 + 肉鸽 (Platform Fighter + Roguelike) |
| **Platform** | PC (Steam / Epic) |
| **Target Audience** | 见下方玩家画像 |
| **Player Count** | 本地多人 2-4 人 (MVP: 2 人) |
| **Session Length** | 每局 3-5 分钟 |
| **Monetization** | 买断制 (Premium) |
| **Estimated Scope** | Medium (4-5 months, solo) — 完整版 16-20 周 |
| **Comparable Titles** | 任天堂明星大乱斗、Rivals of Aether、Hades |

---

## Core Fantasy

掌握任何随机技能组合的能力。你不是背会一个角色的固定连招，而是成为一名即兴战士 — 面对任何随机给予的技能组合，都能迅速理解并打出有效配合。每次解锁新技能的高光时刻，以及发现意想不到的技能协同，是核心情感驱动。

---

## Unique Hook

类似大乱斗，但你的招式在对战中不断进化 — 开局只有基础攻击，随着战斗积攒专注值随机解锁新技能，每局都是独一无二的成长体验。随机性不是混乱，而是创造性适应的源泉。

**"And Also" 测试**：像大乱斗 AND ALSO 你的招式库每局都随机进化。

---

## Visual Identity Anchor

**视觉方向**：「能量爆发」

**核心视觉规则**：每个视觉元素都必须传达"能量在不断积累和释放"。

**支持原则**：
1. **清晰可读** — 任何时刻玩家必须能在1秒内判断场上有多少个已解锁技能活跃。特效服务于信息传达，不仅是为了好看。
2. **角色即职业** — 每个角色的轮廓和配色必须一眼就传达其职业身份（战士=宽厚+暖色，法师=纤细+冷色）。
3. **技能解锁要有仪式感** — 每次随机技能解锁时，视觉上必须有一个不可忽略的高光反馈 — 屏幕闪烁、角色发光、技能图标弹出。

**色彩哲学**：基础色调偏暗（深灰/深蓝场地背景），让角色的鲜明色彩和技能特效成为视觉焦点。每个职业有专属主色调，随机技能按稀有度用蓝/紫/金色标记。

---

## Player Experience Analysis (MDA Framework)

### Target Aesthetics (What the player FEELS)

| Aesthetic | Priority | How We Deliver It |
| ---- | ---- | ---- |
| **Sensation** (sensory pleasure) | 2 | 技能解锁的视觉爆发、打击音效、击飞的动态反馈 |
| **Fantasy** (make-believe, role-playing) | 4 | 职业角色身份 — 战士/法师/盗贼的幻想 |
| **Narrative** (drama, story arc) | 5 | 每局对战本身就是一个"从弱到强"的微型叙事弧 |
| **Challenge** (obstacle course, mastery) | 1 | 即兴适应随机技能组合的挑战，精通 = 任何组合都能打 |
| **Fellowship** (social connection) | 3 | 本地多人派对感，"你看到那波操作了吗" |
| **Discovery** (exploration, secrets) | 2 | 发现意想不到的技能协同组合 |
| **Expression** (self-expression, creativity) | 3 | 你的适应风格就是你的表达 — 激进还是保守 |
| **Submission** (relaxation, comfort zone) | N/A | 不适用 |

### Key Dynamics (Emergent player behaviors)

- 玩家会在对战中根据随机获得的技能即兴调整战斗策略
- 玩家会分享和讨论疯狂的技能组合 — 社交传播
- 玩家会对特定"逆天组合"产生期待和兴奋
- 玩家会发展出"通用技能"（走位、时机判断）来弥补不熟悉技能的不足

### Core Mechanics (Systems we build)

1. **平台格斗战斗系统** — 移动、攻击、闪避、击飞判定，类似大乱斗的基础框架
2. **专注值与随机技能解锁** — 通过攻击命中积攒资源，满值时随机抽取新技能加入招式库
3. **职业基础差异** — 每个职业有不同的基础属性和初始招式，定义核心玩法风格
4. **场地系统** — 多层平台场地，影响战斗策略和击飞角度
5. **技能池与协同系统** — 技能之间可能产生协同效果，鼓励探索性游玩

---

## Player Motivation Profile

### Primary Psychological Needs Served

| Need | How This Game Satisfies It | Strength |
| ---- | ---- | ---- |
| **Autonomy** (freedom, meaningful choice) | 每次技能解锁都是一个选择时刻 — 如何使用新技能，何时触发解锁 | Core |
| **Competence** (mastery, skill growth) | 精通 = 面对任何随机组合都能打出好配合，技能天花板极高 | Core |
| **Relatedness** (connection, belonging) | 本地多人社交，分享疯狂时刻的即时体验 | Supporting |

### Player Type Appeal (Bartle Taxonomy)

- [x] **Achievers** (goal completion, collection, progression) — How: 解锁新职业、收集技能组合、每局的"成长弧线"满足感
- [x] **Explorers** (discovery, understanding systems, finding secrets) — How: 探索技能协同组合、发现隐藏的强力搭配
- [ ] **Socializers** (relationships, cooperation, community) — How: 派对社交场景（次要吸引力）
- [ ] **Killers/Competitors** (domination, PvP, leaderboards) — How: 多人对战竞技（核心循环之一，但随机性降低了纯竞争性）

### Flow State Design

- **Onboarding curve**: 第一个职业只有3个基础招式，玩家在30秒内就能开始战斗。第一个技能解锁在前60秒内发生，立即展示核心独特性。
- **Difficulty scaling**: 对手技能水平 + 随机技能稀有度共同构成动态难度。水平相当的对手之间，随机性创造自然的胜负波动。
- **Feedback clarity**: 技能解锁时有明确的视觉和音效反馈；每次攻击的命中判定清晰可见；专注值进度条醒目。
- **Recovery from failure**: 失败一局只需3-5分钟，立刻可以再来。每局之间没有惩罚，每局都是全新的随机组合。

---

## Core Loop

### Moment-to-Moment (30 seconds)
快速移动、攻击、闪避、击飞对手 — 大乱斗式的流畅战斗。同时通过攻击命中积攒"专注值"，满了就触发一次随机技能解锁。每次解锁是一个高光时刻，可能彻底改变战斗方式。核心操作的响应必须即时且流畅，与随机技能带来的策略变化形成"操作 + 策略"双重乐趣。

### Short-Term (5-15 minutes)
一局对局（3-5分钟）。角色从3-4个基础招式开始，随着战斗推进逐步解锁2-4个随机技能。每次解锁都是策略转折点。局末高潮是双方都拥有额外技能时的混乱大混战。"再来一局"的驱动力：每局的能力组合都不同。

### Session-Level (30-120 minutes)
多局对战（3局2胜或5局3胜），每局结束可更换起始职业。玩家实验不同职业和随机技能的搭配，发现意外的协同效果。这是社交分享的核心时段。

### Long-Term Progression
解锁新的起始职业、新的技能池、装饰品。精通的终极形态不是背会一个角色 — 而是"任何随机组合都能打出好配合"。玩家从"精通一个职业"进化到"精通适应本身"。

### Retention Hooks

- **Curiosity**: "下一局会抽到什么技能组合？" "战士 + 传送 + 火球会怎样？"
- **Investment**: 职业精通度、最喜欢的技能组合记录
- **Social**: 朋友间的对战记忆、"你试过那个组合吗"的分享
- **Mastery**: 逐步掌握更多职业和更多技能的使用方法

---

## Game Pillars

### Pillar 1: 秒学秒玩
每个角色和技能必须在几秒内就能理解。不要20连招的复杂输入。

*Design test*: 当简单效果和复杂三重条件效果之间犹豫时，选简单的。

### Pillar 2: 每局都是新故事
随机技能系统是心脏 — 每局都必须让人觉得与上一局截然不同。

*Design test*: 当有人建议缩小技能池来改善平衡时，这个支柱说不行 — 多样性比完美平衡更重要。

### Pillar 3: 高手菜鸟都开心
既是一个派对游戏（休闲玩家享受随机混乱的乐趣），也是一个有深度的竞技游戏（高手即兴适应随机技能组合）。

*Design test*: 当有人建议移除某个"太随机"的元素因为职业玩家不喜欢时，这个支柱说保留 — 派对乐趣优先于竞技纯度。

### Pillar 4: 快速战斗
单局 2-5 分钟。进入、战斗、解锁疯狂组合、结束。不拖泥带水。

*Design test*: 当有人想加漫长的赛前配置阶段时，这个支柱说不行 — 保持干脆。

### Anti-Pillars (What This Game Is NOT)

- **NOT 传统格斗游戏**: 不和街霸比拼帧精确的操作精度，我们是派对友好的即兴格斗
- **NOT 单人游戏**: 多人对战是核心体验，单人模式（如有）是次要的
- **NOT 内容驱动的 RPG**: 没有剧情模式、没有刷经验，多样性来自系统机制而非内容量
- **NOT 拟真模拟**: 拥抱夸张、超越现实的时刻 — 技能可以天马行空

---

## Inspiration and References

| Reference | What We Take From It | What We Do Differently | Why It Matters |
| ---- | ---- | ---- | ---- |
| 任天堂明星大乱斗 | 平台格斗的核心战斗框架、击飞机制、多人派对氛围 | 加入肉鸽随机成长，每局体验不同 | 验证了平台格斗 + 派对乐趣的巨大市场 |
| Rivals of Aether | 独立平台格斗的品质标准、角色差异化设计 | 角色不是固定的 — 通过随机技能动态变化 | 证明了独立平台格斗可以成功 |
| Hades | 肉鸽循环设计、随机能力组合的乐趣、短循环的高重复游玩性 | 肉鸽机制融入对战格斗而非单人地牢 | 验证了随机能力 + 战斗深度的完美结合 |

**Non-game inspirations**: 卡牌游戏的随机抽牌乐趣（每局不同的"手牌"），麻将的策略性随机（随机中的技巧）。

---

## Target Player Profile

| Attribute | Detail |
| ---- | ---- |
| **Age range** | 16-35 |
| **Gaming experience** | Mid-core — 休闲和硬核之间 |
| **Time availability** | 短时间段（15-30 分钟多人对战），但高频重复 |
| **Platform preference** | PC |
| **Current games they play** | 大乱斗、Rivals of Aether、派对动物、Hades |
| **What they're looking for** | 既有派对乐趣又有深度的多人格斗，每局都不重复 |
| **What would turn them away** | 过于复杂的操作门槛、纯随机无策略、需要长时间投入 |

---

## Technical Considerations

| Consideration | Assessment |
| ---- | ---- |
| **Recommended Engine** | Unity — 用户选择，多人游戏支持成熟，2D 管线完善 |
| **Key Technical Challenges** | (1) 技能系统的数据驱动架构 — 每个技能需要独立的逻辑模块可随时注入 (2) 多人同步 — 即使本地多人也需要帧同步 (3) 技能平衡测试工具 — 需要快速迭代测试不同技能组合 |
| **Art Style** | 像素风或简约几何风格 — 快速制作，适合小团队 |
| **Art Pipeline Complexity** | 低到中等 — 角色精灵图 + 特效动画，不需要3D管线 |
| **Audio Needs** | 中等 — 打击音效、技能音效、BGM，可后期补充 |
| **Networking** | MVP 为本地多人（同屏），后续可扩展为在线多人 |
| **Content Volume** | MVP: 2-3 职业、10-15 技能、1-2 场地。完整版: 8-10 职业、40-50 技能、6-8 场地 |
| **Procedural Systems** | 核心就是程序化的随机技能抽取系统 |

---

## Risks and Open Questions

### Design Risks
- 随机技能组合可能出现"必胜"或"必败"的极端情况，破坏对局公平性
- 玩家可能对随机性产生"非技术性失败"的挫败感
- 职业基础差异 + 随机技能叠加后，角色之间的平衡极难预测

### Technical Risks
- 技能系统的模块化架构需要精心设计 — 如果技能之间有交互，复杂度会指数增长
- Unity 本地多人的输入处理可能需要自定义方案（Input System vs 旧输入系统）
- 在线多人（如果后续加）的帧同步在格斗游戏中是硬性要求

### Market Risks
- 平台格斗市场已有大乱斗和多个独立竞品，需要足够独特的卖点
- "随机格斗"的概念可能让硬核格斗社区排斥
- 肉鸽 + 格斗的跨界需要精确的玩家定位

### Scope Risks
- 技能数量和质量之间的权衡 — 50个不平衡的技能不如15个精心设计的
- 角色动画工作量可能被低估（每个职业 + 每个技能的动画）
- "几周"时间线极其紧张，任何技术障碍都会严重影响交付

### Open Questions
- 技能之间应该有多少交互/协同？过多会增加复杂度，过少会失去发现的乐趣
- 专注值的积攒速度应该多快？太快失去策略深度，太慢感受不到成长
- 需要原型验证：随机技能解锁在快节奏格斗中是否真的好玩？还是只是理论上有趣？

---

## MVP Definition

**Core hypothesis**: 玩家能在随机技能进化的格斗对战中保持3分钟以上的持续参与度，并且想立刻再来一局。

**Required for MVP**:
1. 2-3 个基础职业（不同攻击模式和属性）
2. 10-15 个随机技能（足够产生有趣的组合）
3. 本地同屏2人对战
4. 专注值系统 + 随机技能解锁机制
5. 1-2 个基础多层平台场地
6. 击飞判定和胜利条件

**Explicitly NOT in MVP** (defer to later):
- 在线多人
- 单人模式 / AI 对手
- 角色解锁 / 进度系统
- 完整音效 / 音乐
- 教程

### Scope Tiers (if budget/time shrinks)

| Tier | Content | Features | Timeline |
| ---- | ---- | ---- | ---- |
| **MVP** | 2-3 职业、10-15 技能、1-2 场地 | 本地对战 + 核心循环 | 2-3 周 |
| **Vertical Slice** | 4 职业、25 技能、3 场地 | + 基础 UI、音效、教程 | 4-5 周 |
| **Alpha** | 6 职业、35 技能、5 场地 | + AI 对手、在线多人原型 | 8-10 周 |
| **Full Vision** | 8-10 职业、50 技能、8 场地 | + 进度系统、排名、完整打磨 | 16-20 周 |

---

## Next Steps

- [ ] Get concept approval from creative-director
- [ ] Fill in CLAUDE.md technology stack based on engine choice (`/setup-engine`)
- [ ] Create game pillars document (`/design-review` to validate)
- [ ] **Prototype core idea** (`/prototype [core-mechanic]`) — before writing GDDs, validate the concept is worth designing
- [ ] If prototype PROCEEDS: Decompose concept into systems (`/map-systems`)
- [ ] Design each system (`/design-system [system-name]`) — use prototype learnings in Tuning Knobs and Formulas sections
- [ ] Build vertical slice in Pre-Production (`/vertical-slice`) — validate full game loop before committing to Production
- [ ] Validate core loop with playtest (`/playtest-report`)
- [ ] Plan first milestone (`/sprint-plan new`)
