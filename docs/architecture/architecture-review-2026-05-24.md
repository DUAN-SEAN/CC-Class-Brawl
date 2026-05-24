# Architecture Review Report

Date: 2026-05-24
Engine: Unity 2022.3.51 LTS
GDDs Reviewed: 17 (15 MVP + 2 supporting)
ADRs Reviewed: 5 (all Proposed)

---

## Phase 2 Summary: Technical Requirements Extracted

| System | TR Count | Key Domains |
|--------|----------|-------------|
| MOV (3C System) | 52 | Physics, Input, State, Camera, Performance |
| CBT (Combat FSM) | 44 | State, Input, Timing, Rules, Interface |
| ATK (Attack System) | 36 | Data, State, Physics, Rules, Projectile |
| COL (Collision System) | 37 | Physics, Data, Structures, Layer Matrix |
| DMG (Damage Calculation) | 30 | Formula, State, Events, Interface |
| KBL (Knockback Launch) | 35 | Physics, Formula, KO Detection, VFX |
| CLS (Class System) | 35 | SO Data, Validation, Balance, Interface |
| FOC (Focus System) | 23 | Events, Threshold Formula, State Reset |
| SKD (Skill Database) | 25 | SO Data, Validation, Query Interface |
| SKW (Skill Draw) | 33 | FSM, Weighted Random, UI Timing, Events |
| SEQ (Skill Equipment) | 35 | Slot Management, FSM Registration, Input |
| GST (Game State) | 20 | Scene Management, FSM, Player Slots |
| MCH (Match Management) | 22 | Match Format, Score Tracking, Round Reset |
| ARE (Arena/Platform) | 25 | PlatformData, Blast Zone, Camera Bounds |
| HUD (Battle HUD) | 28 | Passive Rendering, Events, UI Animation |
| GCP (Game Concept) | 25 | Scope, Pillars, Platform, Performance |
| **Total** | **505** | |

---

## Traceability Summary

| Metric | Count |
|--------|-------|
| Total systems | 17 (15 MVP + 2 supporting) |
| Systems with good ADR coverage | 5 |
| Systems with partial ADR coverage | 4 |
| Systems with no ADR coverage | 6 |
| Concept/scope docs (no ADR needed) | 2 |
| **Total TRs** | **505** |
| ✅ Covered by existing ADRs | ~151 (30%) |
| ⚠️ Partially covered | ~74 (15%) |
| ❌ Gaps (no ADR) | ~280 (55%) |

### Coverage by System

| # | System | ADR Coverage | Status |
|---|--------|-------------|--------|
| 1 | 3C系统 | ADR-0001 (physics), ADR-0002 (FSM), ADR-0005 (input) | ✅ GOOD — physics/input/FSM covered. Camera strategy (8 TRs) = no ADR |
| 2 | 场地/平台系统 | ADR-0003 (SolidPlatform layer), ADR-0004 (ArenaConfig SO) | ⚠️ PARTIAL — data + collision layer covered. Platform lifecycle, load/unload, validation = no ADR |
| 3 | 游戏状态管理 | ADR-0005 (input device pairing only) | ❌ GAP — scene management, GamePhase FSM, PlayerSlot, scene loading = no ADR |
| 4 | 格斗状态机 | ADR-0002 (dual FSM), ADR-0005 (input buffer) | ✅ GOOD — core FSM + input buffering covered |
| 5 | 职业系统 | ADR-0004 (ClassData SO) | ⚠️ PARTIAL — data layer covered. Runtime injection, validation, visual identity = no ADR |
| 6 | 攻击系统 | ADR-0003 (hitbox lifecycle), ADR-0004 (AttackData struct) | ⚠️ PARTIAL — hitbox + data covered. Projectile system (pooling, lifecycle, collision) = no ADR |
| 7 | 碰撞判定系统 | ADR-0003 (full coverage) | ✅ GOOD — Trigger detection, Layer Matrix, HitEvent, pipeline all covered |
| 8 | 伤害计算系统 | ADR-0004 (AttackData has BaseDamage/BaseKnockback) | ❌ GAP — damage formula, accumulation, OnHitProcessed event = no ADR |
| 9 | 击退与击飞系统 | ADR-0001 (physics basis only) | ❌ GAP — knockback vector, KO detection, decay, Blast Zone checking = no ADR |
| 10 | 专注值系统 | None | ❌ GAP — focus accumulation, threshold formula, unlock event chain = no ADR |
| 11 | 技能数据库 | ADR-0004 (full coverage) | ✅ GOOD — SO, ISkillDatabase, validation, read-only all covered |
| 12 | 技能抽取系统 | ADR-0004 (data layer reference only) | ❌ GAP — DrawState FSM, weighted random, candidate selection, timeout = no ADR |
| 13 | 技能装备管理 | ADR-0002 (RegisterState), ADR-0004 (StateDefinition) | ⚠️ PARTIAL — FSM registration covered. Slot management, input mapping, round reset = no ADR |
| 14 | 对局管理系统 | None | ❌ GAP — match FSM, Bo1/3/5, round lifecycle, reset coordination = no ADR |
| 15 | 战斗HUD | None | ❌ GAP — UI framework choice, event subscription, rendering = no ADR |
| — | game-concept.md | General architecture decisions | N/A — concept/scope doc, no ADR needed |
| — | knockback-launch + damage + class | Cross-referenced by existing ADRs | See individual system rows |

---

## Coverage Gaps (no ADR exists)

### Foundation Layer Gaps

❌ **TR-GST-003/004: Two-Scene Architecture** — `MenuScene` + `GameScene` with async loading
   - Required ADR: Scene Management (ADR-0007 in architecture.md)
   - Domain: Scene Management
   - Engine Risk: LOW

❌ **TR-GST-001: GamePhase FSM** — 7-state global FSM with atomic transitions
   - Required ADR: Game State Management
   - Domain: State
   - Engine Risk: LOW

### Core Layer Gaps

❌ **TR-DMG-003: KnockbackMagnitude Formula** — `BaseKnockbackGrowth × (DamagePercent/100) × BaseKnockback + BaseKnockback`
   - Required ADR: Damage & Knockback Pipeline
   - Domain: Core Formula
   - Engine Risk: LOW (pure computation)

❌ **TR-KBL-004/013: Knockback Vector + KO Detection** — Blast Zone boundary checking every frame
   - Required ADR: Damage & Knockback Pipeline
   - Domain: Physics
   - Engine Risk: LOW

❌ **TR-FOC-003/005: Focus Accumulation + Unlock Event Chain** — OnAttackHit → FocusPoints → OnFocusReady → SkillDraw
   - Required ADR: Focus & Skill Draw Pipeline
   - Domain: Events
   - Engine Risk: LOW

❌ **TR-ATK-007/023: Projectile System** — Independent GameObjects, pooling, lifecycle
   - Required ADR: Projectile System (ADR-0009 in architecture.md)
   - Domain: Physics/Performance
   - Engine Risk: MEDIUM (object pooling patterns)

### Feature Layer Gaps

❌ **TR-SKW-005/006: Weighted Random Draw Algorithm** — No-replacement weighted selection with rarity tiers
   - Required ADR: Focus & Skill Draw Pipeline
   - Domain: Algorithm
   - Engine Risk: LOW

❌ **TR-MCH-001/012: Match FSM + SignalRoundEnd Breaking Change** — Internal FSM + interface change from SignalKO to SignalRoundEnd
   - Required ADR: Match & Round Lifecycle
   - Domain: State
   - Engine Risk: LOW

❌ **TR-SEQ-001/002: Skill Slot Input Mapping** — 4 slots with dual input (keyboard/gamepad)
   - Covered partially by ADR-0005 (Skill1-4 bindings defined). Slot management logic = no ADR.
   - Domain: Input
   - Engine Risk: LOW

### Presentation Layer Gaps

❌ **TR-HUD-019: UI Framework Choice** — UI Toolkit (UXML/USS) vs UGUI
   - Required ADR: UI Architecture (ADR-0008 in architecture.md)
   - Domain: UI
   - Engine Risk: MEDIUM (UI Toolkit maturity in 2022.3)

❌ **TR-MOV-031-041: Camera System** — Multi-player tracking, dynamic orthographic size, arena bounds clamping
   - Required ADR: Camera Strategy (ADR-0011 in architecture.md)
   - Domain: Camera
   - Engine Risk: LOW

---

## Cross-ADR Conflict Detection

### Conflict: ADR-0001 vs Individual GDD Performance Budgets

**Type**: Performance Budget Conflict
**ADR-0001 claims**: "3C + 碰撞 + 击退系统合计帧耗时 < 3ms"
**Individual GDDs claim**: 3C < 2ms + FSM < 0.5ms + ATK < 1.0ms + COL < 0.5ms + DMG < 0.1ms + KBL < 0.1ms = **4.2ms**
**Impact**: If all systems meet their individual budgets, total exceeds ADR-0001's stated 3ms ceiling
**Resolution**: ADR-0001's 3ms budget should be updated to account for all combat pipeline systems (3C+FSM+ATK+COL+DMG+KBL ≈ 4.2ms, still within 16.6ms frame budget at 25%)

### Conflict: SignalKO vs SignalRoundEnd Interface Mismatch

**Type**: Integration Contract Conflict
**architecture.md IGameState claims**: `SignalKO(winnerPlayerSlot)` method
**match-management GDD requires**: `SignalRoundEnd(winnerIndex, matchOver: bool)` — breaking change
**Impact**: Match management system cannot implement Bo3/Bo5 round cycling with current IGameState interface. The `matchOver` parameter is essential for game state to decide whether to loop back to Countdown or proceed to Results.
**Resolution**: Update IGameState interface in architecture.md to `SignalRoundEnd(int winnerIndex, bool matchOver)`. This is flagged as QQ-03 in Open Questions.

### Conflict: StateDefinition — class vs readonly struct

**Type**: Architecture Pattern Conflict
**ADR-0002 defines**: `StateDefinition` as a **class** (in architecture diagram text)
**ADR-0004 defines**: `StateDefinition` as a **readonly struct**
**Impact**: If implemented as class → heap allocation + GC pressure per skill registration. If readonly struct → stack allocated, zero GC. The two ADRs were written to be consistent (ADR-0004 explicitly chose readonly struct), but ADR-0002's early text may cause confusion.
**Resolution**: ADR-0004's choice of `readonly struct` is correct. ADR-0002 should be updated to align. This is a documentation clarification, not a design conflict.

### No Dependency Cycles Detected

Clean dependency graph with no cycles:
```
ADR-0001 (Physics) → no deps → FOUNDATION
  └── enables → ADR-0002, ADR-0003, ADR-0005
ADR-0002 (Dual FSM) → depends on ADR-0001 → FOUNDATION
  └── enables → ADR-0003, ADR-0004, ADR-0005
ADR-0003 (Hitbox/Hurtbox) → depends on ADR-0001, ADR-0002 → CORE
ADR-0004 (Skill Data) → depends on ADR-0002 → CORE
ADR-0005 (Input) → depends on ADR-0001, ADR-0002 → FOUNDATION
```

### ADR Dependency Ordering (Topologically Sorted)

**Foundation** (no dependencies):
1. ADR-0001: Physics Timestep — 60Hz FixedTimestep + Manual Gravity
2. ADR-0002: Dual FSM Architecture — Movement + Combat FSM

**Core** (depends on Foundation):
3. ADR-0003: Hitbox/Hurtbox Detection (requires 0001, 0002)
4. ADR-0004: Skill System Data-Driven (requires 0002)
5. ADR-0005: Input System (requires 0001, 0002)

### ⚠️ All 5 ADRs Are Still "Proposed"

None of the 5 ADRs have been Accepted yet. Per the ADR lifecycle rules: "Never skip Accepted — stories referencing a Proposed ADR are auto-blocked." All 5 ADRs must be moved to Accepted before implementation can begin.

---

## GDD Revision Flags

No GDD revision flags from engine compatibility issues — all ADRs correctly target Unity 2022.3.51 LTS with LOW knowledge risk.

However, the following GDD-to-architecture inconsistencies exist:

| GDD | Assumption | Reality (from ADR/architecture.md) | Action |
|-----|-----------|--------------------------------------|--------|
| match-management-system.md | SignalRoundEnd(winnerIndex, matchOver) required | architecture.md IGameState still has SignalKO(winnerPlayerSlot) | Update architecture.md interface |
| game-state-management.md | 7-state FSM includes BattleEnd handling | match-management-system.md takes over round lifecycle during Battle | Clarify ownership boundary |

---

## Engine Compatibility Issues

### Version Consistency: ✅ PASS
All 5 ADRs consistently reference Unity 2022.3.51 LTS.

### Post-Cutoff APIs: None
All 5 ADRs report `Post-Cutoff APIs Used: None`.

### Deprecated API Check: ✅ PASS
No ADR references deprecated APIs. Key APIs used:
- `Rigidbody2D.velocity` — NOT deprecated
- `Physics2D.autoSyncTransforms` — NOT deprecated in 2022.3
- `OnTriggerEnter2D` — standard Unity callback
- `PlayerInput` (New Input System) — current API
- `ScriptableObject` — stable Unity API

### Engine Risk Assessment: LOW
All ADRs correctly identify their knowledge risk as LOW. Unity 2022.3 LTS is within LLM training data.

### Verification Required (from ADRs)
ADRs list specific verifications that should be performed before implementation:
- ADR-0001: Verify manual gravity formula accuracy with 10-frame drop test
- ADR-0002: Verify Landing + Attacking combination doesn't cause dead lock
- ADR-0003: Verify Layer Collision Matrix prevents unwanted layer interactions
- ADR-0005: Verify PlayerInputManager join behavior with 2 gamepads; verify ReadValue<Vector2> in FixedUpdate returns latest Update value

---

## Architecture Document Coverage

**`docs/architecture/architecture.md` status**: OUTDATED in sections

| Section | Status | Issue |
|---------|--------|-------|
| System Layer Map | ✅ Current | All 15 MVP systems listed |
| Module Ownership | ✅ Current | Interfaces and data ownership correct |
| Dependency Diagram | ✅ Current | Layer dependencies correct |
| API Boundaries | ⚠️ Outdated | `SignalKO` should be `SignalRoundEnd` per match-management |
| Data Flow | ✅ Current | Frame update path and events accurate |
| ADR Audit | ❌ Outdated | Says "Existing ADRs: 0" — should be 5 |
| Required ADRs | ⚠️ Partially outdated | Lists ADR-0001-0005 as "Must Create" — they now exist (Proposed). ADR-0006-0010 still needed |
| Open Questions | ✅ Current | QQ-01 through QQ-06 all still relevant |

### Missing Systems from Architecture.md
All 15 MVP systems from systems-index.md appear in the architecture document. No orphaned systems found.

---

## Verdict: CONCERNS

### What's Working Well
- 5 Foundation/Core ADRs cover the most technically risky systems (physics, FSM, collision, input, data)
- No dependency cycles, clean topological ordering
- Engine compatibility is consistently LOW risk
- Unified AttackData struct eliminates code branching
- Performance budgets are defined at system level (total ~4.2ms of 16.6ms frame)

### Blocking Concerns

1. **All ADRs are Proposed, none Accepted** — stories cannot reference Proposed ADRs
2. **6 Core/Feature systems have no ADR at all** — damage, knockback, focus, skill draw, game state, match management
3. **SignalKO → SignalRoundEnd interface mismatch** — architecture.md and match-management GDD disagree on the IGameState interface
4. **Performance budget accounting** — ADR-0001's 3ms ceiling is inconsistent with individual system budgets totaling 4.2ms

### Required Actions Before PASS

1. **Accept all 5 existing ADRs** (move from Proposed → Accepted)
2. **Create missing ADRs** (prioritized):
   - ADR-0006: Damage & Knockback Pipeline (covers DMG + KBL systems)
   - ADR-0007: Scene & Game State Management (covers GST + scene loading)
   - ADR-0008: Event Architecture — C# Event Delegates per Interface (covers cross-system events)
   - ADR-0009: Focus & Skill Draw Pipeline (covers FOC + SKW)
   - ADR-0010: Match & Round Lifecycle (covers MCH + round reset coordination)
3. **Fix SignalKO interface** in architecture.md
4. **Update ADR Audit section** in architecture.md
5. **Reconcile performance budgets** across ADR-0001 and individual GDDs

### Can Defer (but should create before relevant system)
- ADR-0011: Camera Strategy
- ADR-0012: Projectile System (object pooling)
- ADR-0013: UI Architecture (UI Toolkit vs UGUI)

---

## Performance Budget Summary

| System | Budget | Source |
|--------|--------|--------|
| 3C (Input+Move+Camera) | < 2.0ms | 3c-system.md |
| Combat FSM | < 0.5ms | combat-state-machine.md |
| Attack System | < 1.0ms | attack-system.md |
| Collision System | < 0.5ms | collision-system.md |
| Damage Calculation | < 0.1ms | damage-calculation-system.md |
| Knockback System | < 0.1ms | knockback-launch-system.md |
| Focus System | < 0.1ms | focus-system.md |
| Skill Draw | < 0.5ms | skill-draw-system.md |
| Skill Equipment | < 0.1ms | skill-equipment-management.md |
| Match Management | < 0.05ms | match-management-system.md |
| Input System | < 0.1ms | ADR-0005 |
| HUD Rendering | < 0.8ms | battle-hud.md |
| **Total Estimated** | **~5.85ms** | **35% of 16.6ms frame budget** |

Remaining budget for: rendering, animation, audio, VFX, GC spikes = ~10.75ms (65%)
