# 07: Character art, Skill effects and performance

**What to build:** The spectacle. Grey boxes are replaced with character art from the developer's free asset packs, animated in code with squash, bounce and flash. Every Skill has three visibly escalating effect tiers (base, upgraded, Master Form), and Rally and Combos are the loudest moments on screen. The game holds 60 frames per second on a mid-range phone. See the spec's user stories 45, 68, 70 and 73.

**Blocked by:** 05 Full cast and balance, 06 Theme, illustration and sound. Also needs the free asset packs from the developer, with licences that allow commercial use.

**Done early (art packs added on 2026-11-16):** every Keeper, Familiar and Hero has animated pixel art from "100 Top Down Monsters Vol 1" (idle, move and attack, facing left or right), sized by each character's measured visible height; art is swappable per asset (Little Keep > Apply Art Packs rebuilds it). Still to do here: three-tier Skill effects, Combo and Rally effects, and the performance pass.

**Status:** ready-for-agent

- [ ] Keeper, Familiars, creatures, Heroes and bosses use single-view sprites from the supplied packs
- [ ] Any character's art can be swapped by replacing an asset, with no layout or code change
- [ ] Characters squash, bounce and flash when they attack, cast, are hit and are knocked out
- [ ] Every Skill has three effect tiers that are clearly bigger and brighter at each step
- [ ] Combos and Rally have their own distinct effects
- [ ] The game holds 60 frames per second on the developer's phone in the busiest Battle
- [ ] Milestone 5 build installed and played on the developer's phone
