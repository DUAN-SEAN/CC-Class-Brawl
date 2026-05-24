# Architecture Traceability Index

Last Updated: 2026-05-24
Engine: Unity 2022.3.51 LTS

## Coverage Summary

- Total TRs: ~90+ across 15 systems
- Covered: 15 systems with full ADR coverage (100%)
- Partial: 0 systems
- Gaps: 0 systems

## Full Matrix

| System | GDD | Key TRs | ADR Coverage | Status |
|--------|-----|---------|-------------|--------|
| 3C System | 3c-system.md | TR-MOV-001~052 | ADR-0001, ADR-0002, ADR-0005, ADR-0012 | ✅ |
| Combat State Machine | combat-state-machine.md | TR-CBT-001~044 | ADR-0002 | ✅ |
| Attack System | attack-system.md | TR-ATK-001~035 | ADR-0003, ADR-0004, ADR-0013 | ✅ |
| Collision System | collision-system.md | TR-COL-001~018 | ADR-0003, ADR-0013 | ✅ |
| Damage Calculation | damage-calculation-system.md | TR-DMG-001~021 | ADR-0006 | ✅ |
| Knockback Launch | knockback-launch-system.md | TR-KBL-001~014 | ADR-0006 | ✅ |
| Class System | class-system.md | TR-CLS-001~017 | ADR-0004 | ✅ |
| Focus System | focus-system.md | TR-FOC-001~005 | ADR-0009 | ✅ |
| Skill Database | skill-database.md | TR-SKD-001~004 | ADR-0004 | ✅ |
| Skill Draw | skill-draw-system.md | TR-SKW-001~023 | ADR-0009 | ✅ |
| Skill Equipment | skill-equipment-management.md | TR-SEQ-001~015 | ADR-0009 | ✅ |
| Game State Management | game-state-management.md | TR-GST-001~008 | ADR-0007 | ✅ |
| Match Management | match-management-system.md | TR-MCH-001~012 | ADR-0010 | ✅ |
| Arena/Platform | arena-platform-system.md | TR-ARE-001~010 | ADR-0011 | ✅ |
| Battle HUD | battle-hud.md | TR-HUD-001~021 | ADR-0014 | ✅ |

## Known Gaps

No gaps — all 15 systems have ADR coverage.

## Known Issues — ALL RESOLVED

1. ~~ADR-0002 vs ADR-0004: StateDefinition type mismatch~~ — FIXED: ADR-0002 updated to readonly struct
2. ~~TR-SEQ-015: Stale text~~ — FIXED: Updated to "skills preserved across rounds"
3. ~~ADR-0007: Unity 6+ Awaitable API~~ — FIXED: Replaced with IEnumerator coroutine

## Superseded Requirements

None — no GDD requirements have been removed since the last review.
