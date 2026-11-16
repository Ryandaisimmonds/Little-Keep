# 02: Run

**What to build:** A full Run. The Player walks a forking Trail through three Chapters, seeing two steps ahead with fog beyond and the boss's distance. Stops are Battles, tough Battles, rest, shop, story events and wild creatures. After each Battle the Player picks 1 of 3 Skill Upgrades from the squad's own Skills, and a third upgrade makes a Master Form. Creatures freed from Heroes or met on the Trail are befriended, up to four Familiars. The Keep's health carries across the Run, each Chapter has a mid-boss and a boss, the Run can be left and resumed at any point, and it ends in a summary. See the spec's user stories 26 to 49.

**Blocked by:** 01 Battle

**Carried over from the ticket 01 review:**
- Prefactor first: make Skills their own data assets with stable ids (today a Skill's definition sits inside its Fighter), so Skill Upgrades can target a Skill by id. Waves become part of encounter data at the same time.
- The top-centre readout shows only wave progress; add Chapter progress once Chapters exist.
- Auto-aim picks the Hero nearest the Keep, not a weighted "greatest threat". Decide whether that is good enough once bosses and faster Heroes exist.
- Heroes that bring a creature (story 16) and mid-bosses and bosses (story 17) land here.
- Hit flashes, squash and screen shake already exist in grey-box form; ticket 07 replaces them.

**Status:** in progress: built and tested; waiting on the phone play-test

**Deferred from the ticket 02 review (to ticket 05 unless noted):**
- Mid-bosses are scaled-up ordinary Heroes and all bosses share one behaviour (calling in Heroes); give each its own behaviour with the full roster.
- The squad view is reachable from every Run screen but not during a Battle (the Battle shows each Fighter's card).
- Auto-aim stays "nearest the Keep": decided as good enough for now, revisit with faster Heroes.
- Wild creature Stops draw from every Familiar, starters included; ticket 03 should limit them to creatures the Player can Bond with.
- Skipping a Skill Upgrade is allowed (not in the spec); keep unless playtesting says otherwise.

- [x] The Run module takes a seed, a starting Familiar and a sequence of choices, and the same seed and choices give the same Run
- [x] The Trail always reaches the boss, forks, and exposes exactly the next two steps plus the boss's distance
- [x] All six Stop types work: Battle, tough Battle (better reward), rest (repairs the Keep), shop (spends Run earnings), story event (choices with consequences), wild creature
- [x] Skill Upgrade offers are 3 options drawn only from Skills the squad has, each stating plainly what it changes
- [x] A Skill's third upgrade makes its Master Form
- [x] Befriending adds a creature as a Familiar for the rest of the Run; a full squad offers a choice; Heroes are never befriended
- [x] Three Chapters of about seven Stops, each with a mid-boss and a boss with their own behaviour
- [x] Keep health persists between Battles; the Run ends when it falls or the last boss is beaten, with a summary screen
- [x] The build so far can be viewed at any time during a Run
- [x] A Run in progress is saved after every choice and resumes identically
- [x] Edit-mode tests cover the behaviours above through the Run module's interface only
- [ ] Milestone 2 build installed and played on the developer's phone
