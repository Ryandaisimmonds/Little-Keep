# 05: Full cast and balance

**What to build:** The first complete version's content, balanced. Six Familiars with about four Skills each (every Skill with a Master Form), about twelve Hero types, three mid-bosses and three bosses, and a full set of story events. A bot plays whole Runs and reports win rate and length, and tuning is adjusted until a Run takes 20 to 25 minutes with sensible win rates on each difficulty tier. See the spec's scope section.

**Blocked by:** 03 Keep, 04 Elements and Combos

**Status:** done (phone play-test outstanding)

- [x] Six Familiars, each with about four Skills spread across the four Elements, every Skill with a Master Form
- [x] Three Familiars owned at the start; the other three each have a Deed or a Bond
- [x] About twelve Hero types with distinct behaviour, some bringing a creature; three mid-bosses and three bosses
- [x] Enough story events and shop stock that consecutive Runs differ
- [x] A Run bot plays whole Runs through the rule modules and reports win rate and Run length per difficulty tier
- [x] Bot results and tuning changes are recorded in the repo
- [x] A typical Run takes 20 to 25 minutes
- [x] All content is data assets the developer can edit in the Inspector
- [ ] Milestone 4 build installed and played on the developer's phone

## Notes

- Familiars: each has four Skills, every one with a Master Form. A Familiar starts each Run knowing its first Skill and learns the rest, in order, from Skill Upgrade offers, the shop and some events.
- Elements are spread across the cast: each Familiar has three or four Elements (Pip has no Frost; Bramble and Biscuit have no Fire; Tide has no Bloom).
- New Skill shapes: Chain (jumps Hero to Hero) and Nova (around the caster; waits until a Hero is close, and Rally holds it back too).
- Heroes: Squire, Militia, Page, Archer, Shieldbearer, Knight (brings Biscuit), Huntsman (brings Bun), Spearman, Cleric (heals; brings Puff), Hedge Wizard, Monk (steadfast), Black Knight (steadfast). Mid-bosses: Captain Brash (calls in militia), Sergeant Shieldwall (steadfast), Archmage Vale (calls in Hedge Wizards). Bosses: Sir Pompous, Lady Gleam (heals her knights), The Paragon.
- Squire and Militia, and Archer and Hedge Wizard, differ by numbers and pacing rather than by a new behaviour.
- Shop stock is drawn from the squad's own upgrades and Skills to learn, plus repairs, so it changes with the squad.
- 14 events, 4 Battles and 2 tough Battles per Chapter, 8 steps per Chapter.
- All content lives in Assets/Game/Editor/ContentSetup.cs as well as in the assets: "Set Up Content" adds what is missing without touching edits; "Rebuild Content From Script" writes everything back.
- Familiar and Hero ids renamed to match their names (pony to biscuit, owl to bun, falconer to huntsman). Progress saved under the old ids is dropped; fine before release.
- Bot results and the tuning log: docs/balance.md. Run lengths average 21 to 24 minutes per tier; won Fierce Runs with full upgrades run about 27.
