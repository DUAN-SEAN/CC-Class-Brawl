# Sprint 1 — 2026-05-24 to 2026-06-06

## Sprint Goal

完成 Foundation 层全部系统，实现角色在场地上的完整移动和游戏状态流转 — 为 Core 战斗层搭建可运行的基础。

## Capacity

- Total days: 10 (2 weeks)
- Buffer (20%): 2 days
- Available: 8 days (~64 hours)

## Tasks

### Must Have (Critical Path)

| ID | Task | Epic | Est. | Dependencies | Story File |
|----|------|------|------|-------------|------------|
| 1-1 | Input Processing | 3c-system | 4h | None | story-001-input-processing.md |
| 1-2 | Ground Movement | 3c-system | 4h | 1-1 | story-002-ground-movement.md |
| 1-3 | Jump System | 3c-system | 4h | 1-1 | story-003-jump-system.md |
| 1-4 | Fast Fall + Terminal Velocity | 3c-system | 2h | 1-3 | story-004-fast-fall-terminal-velocity.md |
| 1-5 | Dash/Dodge | 3c-system | 3h | 1-2 | story-005-dash-dodge.md |
| 1-6 | Platform Interaction | 3c-system | 2h | 1-3, 2-2 | story-006-platform-interaction.md |
| 1-7 | Camera System | 3c-system | 3h | 1-2, 2-4 | story-007-camera-system.md |
| 1-8 | Multiplayer Input Isolation | 3c-system | 2h | 1-1, 3-3 | story-008-multiplayer-input-isolation.md |
| 2-1 | ArenaConfig SO + Validation | arena-platform | 2h | None | story-001-arena-config-so.md |
| 2-2 | Platform Collision Setup | arena-platform | 3h | 2-1 | story-002-platform-collision-setup.md |
| 2-3 | Arena Lifecycle | arena-platform | 3h | 2-1, 2-2 | story-003-arena-lifecycle.md |
| 2-4 | Data Provider Queries | arena-platform | 2h | 2-3 | story-004-data-provider-queries.md |
| 3-1 | GamePhase FSM | game-state-management | 3h | None | story-001-gamephase-fsm.md |
| 3-2 | Scene Management | game-state-management | 4h | 3-1 | story-002-scene-management.md |
| 3-3 | PlayerSlot Management | game-state-management | 3h | 3-1 | story-003-player-slot-management.md |

**Must Have Total: ~44 hours (5.5 days)**

### Should Have

| ID | Task | Epic | Est. | Dependencies | Story File |
|----|------|------|------|-------------|------------|
| 3-4 | Countdown + Input Freeze | game-state-management | 2h | 3-1, 3-2 | story-004-countdown-input-freeze.md |
| 3-5 | BattleEnd + Results Transition | game-state-management | 3h | 3-1, 3-2 | story-005-battleend-results-transition.md |

**Should Have Total: ~5 hours (0.6 days)**

### Nice to Have

暂无。Core 层留给 Sprint 2。

## Carryover from Previous Sprint

无（第一个 Sprint）。

## Risks

| Risk | Probability | Impact | Mitigation |
|------|------------|--------|------------|
| 3C 手感调优耗时超预期 | Medium | Medium | 参数数据驱动，快速迭代 |
| PlatformEffector2D 行为不符合预期 | Low | Medium | 早期验证，备选方案：自定义碰撞 |
| Input System 多手柄配对问题 | Low | High | ADR-0005 已定义方案，早期测试 |

## Dependencies on External Factors

- Unity Input System 手柄测试需要实体手柄或模拟器

## Definition of Done for this Sprint

- [ ] All Must Have tasks completed
- [ ] All tasks pass acceptance criteria
- [ ] QA plan exists (`production/qa/qa-plan-sprint-1.md`)
- [ ] All Logic/Integration stories have passing unit/integration tests
- [ ] Smoke check passed (`/smoke-check sprint`)
- [ ] No S1 or S2 bugs in delivered features
- [ ] Code reviewed and merged

> ⚠️ **No QA Plan**: This sprint was started without a QA plan. Run `/qa-plan sprint`
> before the last story is implemented. The Production → Polish gate requires a QA
> sign-off report, which requires a QA plan.
