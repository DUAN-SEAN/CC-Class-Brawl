---
name: hud-visual-design-decisions
description: 6 user-confirmed design decisions for battle HUD visual spec (hud-visual-design.md)
metadata:
  type: project
---

# HUD Visual Design Key Decisions (2026-05-24)

6 decisions confirmed by user for `design/ux/hud-visual-design.md`:

1. **Layout**: Bottom-corner layout from hud.md (P1 left-bottom, P2 right-bottom), NOT Art Bible 7.4 top layout. hud.md is authoritative for battle HUD layout.

2. **Animation easing**: EaseOutBack ONLY for damage% bounce and KO popup. Everything else uses Cubic ease-out. Art Bible 7.3 generally prohibits Back easing, but 2 specific use-cases were approved for combat impact feel.

3. **Danger colors**: Yellow #FFD700, Orange #FF8C00, Red-Flash #FF2020 are HUD-specific info-encoding colors. Not in Art Bible 4.1 base palette. Only for HUD, never in battle effects.

4. **Damage% font size**: Fixed 56px Medium (not a range). Single value for precise layout calculations.

5. **Info area width**: ~540px per side (was ~350px in hud.md). Expanded to fit 48px skill slots + 56px damage% + 140px focus bar.

6. **Rarity borders**: Unified to Art Bible 7.2 values (Common #FFFFFF 1px, Rare #9B59B6 2px+glow, Epic #F0C040 3px+glow). Old battle-hud GDD values (#4488FF, #8844CC, #FFB800) were overridden.

**Why**: These decisions resolve conflicts between Art Bible, hud.md UX spec, and battle-hud GDD. Art Bible is the visual source of truth; hud.md is the UX authority; battle-hud GDD provides technical formulas.

**How to apply**: When reviewing HUD visual assets, verify alignment with these 6 decisions. Any future HUD changes should reference these as precedent.
