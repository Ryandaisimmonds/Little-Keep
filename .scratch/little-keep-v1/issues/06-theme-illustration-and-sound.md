# 06: Theme, illustration and sound

**What to build:** The game's own look and feedback. One shared theme (colours, type, panels, buttons) applied to every screen, with warm, slightly muted backgrounds so Skill effects stand out. The Trail becomes a side-scrolling landscape with fog; the Keep becomes an illustrated place whose rooms appear as unlocked. Stand-in sounds play for attacks, Skills, Combos, Rally and menus through one audio module. See the spec's user stories 64 to 69.

**Blocked by:** 03 Keep

**Done early (art packs added on 2026-11-16):** the UiTheme asset exists and is built from Layer Lab's GUI "The Stone" pack (font, panels, buttons, bars, ribbons, icons, background); every screen and the Battle HUD draw through it. Still to do here: the Trail and Keep as illustrated scenes, the Keep building in Battles, and sound.

**Status:** ready-for-agent

- [x] One theme asset drives colours, type and panel and button styles on every screen; changing it restyles the whole game
- [x] Every screen respects the safe area in landscape
- [x] The Trail is drawn as a scrolling landscape with two visible steps, fog beyond and the boss as a distant landmark
- [x] The Keep is an illustrated scene with rooms that appear when unlocked
- [x] All sound goes through one audio module; stand-in sounds exist for attacks, Skills, Combos, Rally, hits on the Keep and menus
- [x] Any visual references the developer has supplied by then are followed; otherwise the default mood in the spec is used
- [ ] Build installed and reviewed on the developer's phone

## Notes

- The theme asset (UiTheme, Layer Lab "The Stone" pack) now also carries the world colours; Palette reads from it, with fallbacks if a colour or icon is missing.
- Scenery (sky, hills, trees, fog, the boss castle and the Keep's buildings) is pixel art painted in code (Scenery.cs). It is stand-in art until ticket 07 or supplied artwork replaces it.
- Sounds are synthesised in code (Audio.cs) and built up front at start-up. Audio.Override(sound, clip) swaps in a real clip without touching the callers. Sound on/off sits top-left on the Keep and is remembered.
- The Keep and Trail are laid out for 16:9 and squeezed (to no less than 0.7) on narrower screens such as 4:3 tablets; squad and starter cards size to the actual safe width. 4:3 was checked by working out the sizes, not on a device.
- No visual references were supplied, so the spec's default cozy mood is used.
- Still to do: install and review on the phone.

