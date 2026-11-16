# 06: Theme, illustration and sound

**What to build:** The game's own look and feedback. One shared theme (colours, type, panels, buttons) applied to every screen, with warm, slightly muted backgrounds so Skill effects stand out. The Trail becomes a side-scrolling landscape with fog; the Keep becomes an illustrated place whose rooms appear as unlocked. Stand-in sounds play for attacks, Skills, Combos, Rally and menus through one audio module. See the spec's user stories 64 to 69.

**Blocked by:** 03 Keep

**Done early (art packs added on 2026-11-16):** the UiTheme asset exists and is built from Layer Lab's GUI "The Stone" pack (font, panels, buttons, bars, ribbons, icons, background); every screen and the Battle HUD draw through it. Still to do here: the Trail and Keep as illustrated scenes, the Keep building in Battles, and sound.

**Status:** ready-for-agent

- [ ] One theme asset drives colours, type and panel and button styles on every screen; changing it restyles the whole game
- [ ] Every screen respects the safe area in landscape
- [ ] The Trail is drawn as a scrolling landscape with two visible steps, fog beyond and the boss as a distant landmark
- [ ] The Keep is an illustrated scene with rooms that appear when unlocked
- [ ] All sound goes through one audio module; stand-in sounds exist for attacks, Skills, Combos, Rally, hits on the Keep and menus
- [ ] Any visual references the developer has supplied by then are followed; otherwise the default mood in the spec is used
- [ ] Build installed and reviewed on the developer's phone
