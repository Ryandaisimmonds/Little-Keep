# 02: Run

**What to build:** A full Run. The Player walks a forking Trail through three Chapters, seeing two steps ahead with fog beyond and the boss's distance. Stops are Battles, tough Battles, rest, shop, story events and wild creatures. After each Battle the Player picks 1 of 3 Skill Upgrades from the squad's own Skills, and a third upgrade makes a Master Form. Creatures freed from Heroes or met on the Trail are befriended, up to four Familiars. The Keep's health carries across the Run, each Chapter has a mid-boss and a boss, the Run can be left and resumed at any point, and it ends in a summary. See the spec's user stories 26 to 49.

**Blocked by:** 01 Battle

**Status:** ready-for-agent

- [ ] The Run module takes a seed, a starting Familiar and a sequence of choices, and the same seed and choices give the same Run
- [ ] The Trail always reaches the boss, forks, and exposes exactly the next two steps plus the boss's distance
- [ ] All six Stop types work: Battle, tough Battle (better reward), rest (repairs the Keep), shop (spends Run earnings), story event (choices with consequences), wild creature
- [ ] Skill Upgrade offers are 3 options drawn only from Skills the squad has, each stating plainly what it changes
- [ ] A Skill's third upgrade makes its Master Form
- [ ] Befriending adds a creature as a Familiar for the rest of the Run; a full squad offers a choice; Heroes are never befriended
- [ ] Three Chapters of about seven Stops, each with a mid-boss and a boss with their own behaviour
- [ ] Keep health persists between Battles; the Run ends when it falls or the last boss is beaten, with a summary screen
- [ ] The build so far can be viewed at any time during a Run
- [ ] A Run in progress is saved after every choice and resumes identically
- [ ] Edit-mode tests cover the behaviours above through the Run module's interface only
- [ ] Milestone 2 build installed and played on the developer's phone
