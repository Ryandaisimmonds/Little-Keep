# 04: Elements and Combos

**What to build:** Combat depth the Player can read. Every Skill deals one of four Elements (fire, frost, spark, bloom); each leaves its status on a Hero (burn, chill, shock, root); a second Element landing on a Hero that carries another Element's status triggers a visible Combo burst. Tapping any stat, status or Skill shows a plain explanation. See the spec's user stories 21 to 25.

**Blocked by:** 01 Battle

**Status:** ready-for-agent

- [ ] Each Skill has an Element, set in data
- [ ] Each Element applies its own status with its own effect on the Hero
- [ ] A hit of a different Element on a Hero carrying a status triggers a Combo that deals burst damage and consumes the status
- [ ] Statuses and Combos are visible on Heroes in the Battle
- [ ] Tapping a stat, status or Skill anywhere it appears shows a plain-language explanation
- [ ] Edit-mode tests cover Elements, statuses and Combos through the Battle simulation's interface only
