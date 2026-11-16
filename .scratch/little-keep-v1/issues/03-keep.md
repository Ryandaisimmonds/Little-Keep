# 03: Keep

**What to build:** Permanent progress. Between Runs the Player is at the Keep, a home screen whose rooms appear as they are unlocked. Every Run pays an uncapped currency, win or lose, spent on the Keeper's upgrade track. A Familiar book lists every Familiar and how to earn it; the Player owns three at the start, chooses one to begin each Run, and earns the rest through Deeds and Bonds. Three difficulty tiers unlock on wins. Progress is saved automatically. See the spec's user stories 50 to 63 and ADR 0002.

**Blocked by:** 02 Run

**Status:** done (phone play-test outstanding)

- [x] The Keep progress module takes Run results and purchases and reports what is owned and unlocked
- [x] Every finished Run grants currency, win or lose, with no cap
- [x] The Keeper upgrade track can be bought level by level and its effects apply in the next Run
- [x] The Familiar book shows all six Familiars, and for each unowned one the Deed or Bond that unlocks it
- [x] The Player chooses a starting Familiar from those owned
- [x] Completing a Deed grants its Familiar at once; a Bond meter fills on each Befriending and grants the Familiar when full
- [x] Three difficulty tiers, each unlocked by winning the one below
- [x] The save is a versioned file on the device, written automatically, and a save from an older version loads
- [x] The Keep screen starts Runs, resumes a Run in progress, and shows rooms only once unlocked
- [x] No real-money purchases and no random pulls for gameplay Familiars
- [x] Edit-mode tests cover the behaviours above through the Keep progress module's interface only
- [ ] Milestone 3 build installed and played on the developer's phone

## Notes

- Rooms: the Gate and Familiar Book from the start; the Workshop after the first Run; the Watchtower after the first win. They are cards for now; the illustrated Keep comes with ticket 06.
- Keeper upgrade track: four short tracks (walls, claws, purse, horn) rather than one long one.
- Deeds complete as soon as their condition is met mid-Run (Clear Meadow Road grants Tide on clearing Chapter 1).
- Every Run pays at least 5 Gems. Each Run carries a Run number so it can never be paid twice.
- Wild creatures on the Trail are the Bond Familiars (Biscuit, Bun), so Bonds can fill.
- Tiers tuned with the Run bot (it never aims): Cozy 0.95x Hero health, about 6/12 bot wins; Brisk 1.15x HP, 1.1x numbers; Fierce 1.3x HP, 1.2x numbers.
- Deferred: Keeper looks customisation (user story 54) is not in this checklist; suggest it joins ticket 06 or 07.
- A Run saved by an older build is refused when the content has changed (ADR 0003). This update changes content, so any in-progress Run from the last build ends with a notice.
