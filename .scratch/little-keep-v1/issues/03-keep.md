# 03: Keep

**What to build:** Permanent progress. Between Runs the Player is at the Keep, a home screen whose rooms appear as they are unlocked. Every Run pays an uncapped currency, win or lose, spent on the Keeper's upgrade track. A Familiar book lists every Familiar and how to earn it; the Player owns three at the start, chooses one to begin each Run, and earns the rest through Deeds and Bonds. Three difficulty tiers unlock on wins. Progress is saved automatically. See the spec's user stories 50 to 63 and ADR 0002.

**Blocked by:** 02 Run

**Status:** ready-for-agent

- [ ] The Keep progress module takes Run results and purchases and reports what is owned and unlocked
- [ ] Every finished Run grants currency, win or lose, with no cap
- [ ] The Keeper upgrade track can be bought level by level and its effects apply in the next Run
- [ ] The Familiar book shows all six Familiars, and for each unowned one the Deed or Bond that unlocks it
- [ ] The Player chooses a starting Familiar from those owned
- [ ] Completing a Deed grants its Familiar at once; a Bond meter fills on each Befriending and grants the Familiar when full
- [ ] Three difficulty tiers, each unlocked by winning the one below
- [ ] The save is a versioned file on the device, written automatically, and a save from an older version loads
- [ ] The Keep screen starts Runs, resumes a Run in progress, and shows rooms only once unlocked
- [ ] No real-money purchases and no random pulls for gameplay Familiars
- [ ] Edit-mode tests cover the behaviours above through the Keep progress module's interface only
- [ ] Milestone 3 build installed and played on the developer's phone
