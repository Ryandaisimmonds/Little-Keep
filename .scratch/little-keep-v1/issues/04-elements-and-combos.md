# 04: Elements and Combos

**What to build:** Combat depth the Player can read. Every Skill deals one of four Elements (fire, frost, spark, bloom); each leaves its status on a Hero (burn, chill, shock, root); a second Element landing on a Hero that carries another Element's status triggers a visible Combo burst. Tapping any stat, status or Skill shows a plain explanation. See the spec's user stories 21 to 25.

**Blocked by:** 01 Battle

**Status:** done (phone play-test outstanding)

- [x] Each Skill has an Element, set in data
- [x] Each Element applies its own status with its own effect on the Hero
- [x] A hit of a different Element on a Hero carrying a status triggers a Combo that deals burst damage and consumes the status
- [x] Statuses and Combos are visible on Heroes in the Battle
- [x] Tapping a stat, status or Skill anywhere it appears shows a plain-language explanation
- [x] Edit-mode tests cover Elements, statuses and Combos through the Battle simulation's interface only

## Notes

- Statuses: burn (damage over time), chill (slow), shock (takes more damage), root (held in place; bosses for half as long). A Hero carries one status; the same Element refreshes it.
- A Combo bursts onto every Hero close by and blows their statuses away, so one cast makes one Combo, not a chain. A killing hit still sets off its Combo.
- Combo names: Steam, Firework, Wildfire, Shatter, Frostbloom, Thunderbloom.
- Elements: Keeper spark, Pip fire, Bramble and Puff bloom, Biscuit spark, Bun and Tide frost (set by Little Keep/Assign Elements). Ticket 05 may rebalance the spread.
- Statuses on the lane can't be tapped (holding there aims), so the pause panel has a tappable status legend; every Skill shows a tappable Element chip.
- Combo sound comes with ticket 06 (sound).
- Balance: Combo 1.25x, burn 50%, shock 20%, Hero health scale raised 1.65 to 2.2. Bot: Cozy 7/12, Brisk with three Keeper upgrades 5/12.
