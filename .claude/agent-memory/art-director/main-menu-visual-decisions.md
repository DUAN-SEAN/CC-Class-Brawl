---
name: main-menu-visual-decisions
description: 4 user-confirmed design decisions for main menu visual spec (main-menu-visual-design.md)
metadata:
  type: project
---

# Main Menu Visual Design Key Decisions (2026-05-24)

4 decisions confirmed by user for `design/ux/main-menu-visual-design.md`:

1. **CTA button color**: White background + #F0C040 pulsing gold border. White ensures readability (#1A1A2E text on #FFFFFF = 15.4:1). Gold pulse from Art Bible epic rarity color gives ritual feel without being loud.

2. **Game title treatment**: Layered typography -- Chinese "职业对决" in Noto Sans SC 72px SemiBold at 20% opacity, English "CLASS BRAWL" in Exo 2 24px Medium at 15% opacity. Low opacity makes it decorative background rather than focal point.

3. **Top bar button style**: Micro-panel buttons using #2A2A3E small panels + #F0C040 2px border focus state. Consistent with HUD skill icon panel language (Art Bible 7.6).

4. **Quit confirmation dialog width**: 480px. Secondary panel #222240 85%, overlay #1A1A2E 60%. Accommodates both Chinese and English text.

**Why**: These decisions align main menu with established visual language from Art Bible and HUD visual design ([[hud-visual-design-decisions]]), while adapting to the menu's specific needs (decorative title, single CTA, minimal UI).

**How to apply**: When reviewing main menu visual assets, verify alignment with these 4 decisions. Focus indicator uses same #F0C040 border as HUD. Typography scales from Art Bible 7.1 rules.
