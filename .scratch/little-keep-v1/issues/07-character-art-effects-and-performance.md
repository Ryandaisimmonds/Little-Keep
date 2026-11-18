# 07: Character art, Skill effects and performance

**What to build:** The spectacle. Grey boxes are replaced with character art from the developer's free asset packs, animated in code with squash, bounce and flash. Every Skill has three visibly escalating effect tiers (base, upgraded, Master Form), and Rally and Combos are the loudest moments on screen. The game holds 60 frames per second on a mid-range phone. See the spec's user stories 45, 68, 70 and 73.

**Blocked by:** 05 Full cast and balance, 06 Theme, illustration and sound. Also needs the free asset packs from the developer, with licences that allow commercial use.

**Done early (art packs added on 2026-11-16):** every Keeper, Familiar and Hero has animated pixel art from "100 Top Down Monsters Vol 1" (idle, move and attack, facing left or right), sized by each character's measured visible height; art is swappable per asset (Little Keep > Apply Art Packs rebuilds it). Still to do here: three-tier Skill effects, Combo and Rally effects, and the performance pass.

**Status:** ready-for-agent

- [x] Keeper, Familiars, creatures, Heroes and bosses use single-view sprites from the supplied packs
- [x] Any character's art can be swapped by replacing an asset, with no layout or code change
- [x] Characters squash, bounce and flash when they attack, cast, are hit and are knocked out
- [x] Every Skill has three effect tiers that are clearly bigger and brighter at each step
- [x] Combos and Rally have their own distinct effects
- [ ] The game holds 60 frames per second on the developer's phone in the busiest Battle
- [ ] Milestone 5 build installed and played on the developer's phone

## Notes

- **Effects:** glowing particles (Unity particle systems, one per kind, additive `LittleKeep/FxGlow` shader). Each Element has its own look: Fire embers rise, Frost shards tumble, Spark streaks, Bloom petals drift. Recipes live in `Effects.cs`.
- **Three tiers:** every Skill effect steps up from base to upgraded to Master Form: area bursts, each hit of a Volley or Chain, Chain bolts, the cast at the caster, and projectile trails. Upgraded adds a second ring, a glowing core and twinkles; Master Form adds a white shockwave, a rising column of light and a colour flash. Hit and Zap events now carry the Skill's growth (new test).
- **Combo:** a two-coloured spiral, both Elements' particles, a white flash and a 0.09 s hit-stop. **Rally:** a Rally-colour flash, rings down the lane, stars off every Fighter, confetti and "Rally!". The hit-stop (the Battle slows to a crawl for a moment) was not asked for; it is there to make these the loudest moments, and is easy to remove if it feels wrong.
- **Feel:** a white flash on hit and cast, a hop on cast, Rally and recovery, a gentle idle bob, a poof when knocked out, and beaten Heroes flatten and fade instead of vanishing.
- **Swappable effect art:** `Content/Effects/EffectArt.asset` holds the glow material and one sprite slot per particle kind. Empty slots use shapes drawn in code.
- **Performance:** the Battle loop allocates nothing per frame (reused event list, pooled rings and words, no boxing in sound lookups, particle systems found by array). Particles are capped at 250 per kind. Measured in the editor: rules, view, effects and HUD together take well under 1 ms a frame. The busiest moment (Rally with every Skill in Master Form) peaks at about 1,200 particles for an instant.
- **Checking 60 fps on the phone:** development builds show a frame-rate readout at the bottom of the screen and a "Stress test" button on the Keep: the last boss twice over against a full squad with every Skill in Master Form. Neither is compiled into a release build. The editor can't measure phone frame rate, so the 60 fps item stays open until checked on the phone.
- **Phone check still needed:** the glow shader is proven in the editor (Metal); its look on the phone's GPU needs confirming.

