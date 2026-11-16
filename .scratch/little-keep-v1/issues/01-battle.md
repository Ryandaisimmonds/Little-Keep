# 01: Battle

**What to build:** A grey-box Battle playable on the phone. After project setup, the Player sees a side-on landscape lane with the Keep on the left. The Keeper and two Familiars attack on their own; holding a finger on the lane aims the Keeper; Skills cast themselves; the Rally button casts them all at once. Three Hero types arrive in waves, hit back, and damage the Keep if they reach it. The Battle is won when the waves are cleared and lost when the Keep falls. See the spec's Battle user stories (1 to 20) and layout (65, 67).

**Blocked by:** None (can start immediately)

**Status:** ready-for-agent

- [ ] Project is set to Android, landscape only, with Little Keep's own app identifier and product name, and the whole project is committed
- [ ] Game rules live in their own assembly with no dependency on scenes; presentation reads rule state and events
- [ ] The Battle simulation runs from a squad, a wave list, a seed and Player input, and the same seed gives the same Battle
- [ ] The Keeper auto-aims at the greatest threat; a held aim point overrides it; releasing restores auto-aim
- [ ] Two Familiars attack on their own; a Knock-out recovers after a few seconds
- [ ] Skills cast themselves when ready; Rally casts every Skill once and has a long visible cooldown
- [ ] Heroes arrive in waves with a wave counter; three Hero types behave differently and are editable data assets
- [ ] A Hero reaching the Keep reduces its health; zero health loses the Battle; clearing all waves wins it
- [ ] Battle screen follows the agreed layout, respects the safe area, and has pause and free speed-up
- [ ] Edit-mode tests cover the behaviours above through the Battle simulation's interface only
- [ ] Milestone 1 build installed and played on the developer's phone
