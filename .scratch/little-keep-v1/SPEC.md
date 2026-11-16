---
title: Little Keep, first complete version
status: ready-for-agent
created: 2026-11-16
---

# Little Keep: first complete version

Vocabulary follows `CONTEXT.md`. Decisions in `docs/adr/` (0001 side-on landscape, 0002 no paid random pulls) are binding. Mechanics reference for the game that inspired this: `research/dungeon-squad.md` in the DS Squad Claude project.

## Problem Statement

The Player enjoys Dungeon Squad but wants their own version of it. The original gates progress behind random resources, leans on sexual content, explains its systems poorly, has long runs and stingy rewards after a loss. The Player wants a game that keeps the depth of its runs and the pleasure of a squad that grows as you play, but is easier to pick up, friendly to everyone, and has its own cute, cozy look with bright Skill effects that get louder as they grow. An earlier grey-box prototype (the DS Squad Unity project) drifted to a portrait top-down lane and a single upgrade draft, and mixed game rules into scene code; it is kept only as a reference.

## Solution

Little Keep: a cute, cozy lane-defence roguelite for Android, played in landscape. The Player is the Keeper and, with a squad of Familiars, defends the Keep from pompous invading Heroes. Battles play themselves, with the Player able to take over the Keeper's aim by holding a finger on the lane and to trigger one ability, Rally. A Run is three Chapters walked along a Trail that shows only two steps ahead. After each Battle the Player picks a Skill Upgrade; three upgrades turn a Skill into its Master Form. Creatures freed from Heroes or met on the Trail are befriended and join the squad. Between Runs the Player returns to an illustrated Keep where permanent progress lives; every gameplay Familiar is earned through a Deed or a Bond, never through luck or money.

## User Stories

### Battle

1. As a Player, I want Battles shown side-on in landscape with the Keep on the left and Heroes entering from the right, so that I can read the whole fight at a glance.
2. As a Player, I want the Keeper and every Familiar to attack on their own at sensible targets, so that I can play without constant input.
3. As a Player, I want to hold a finger anywhere on the lane to make the Keeper aim there, so that I can focus a dangerous Hero myself.
4. As a Player, I want the Keeper to return to automatic aim when I lift my finger, so that I never have to switch modes.
5. As a Player, I want every Skill to cast itself when it is ready, so that I am never punished for forgetting a button.
6. As a Player, I want one Rally button that makes every Skill in the squad cast at once, so that I have a moment in each Battle that is mine to time.
7. As a Player, I want Rally to be on a long, visible cooldown, so that I know when it is available.
8. As a Player, I want Heroes to arrive in waves, so that a Battle has rhythm and pauses.
9. As a Player, I want to see how many waves remain, so that I know how close the Battle is to ending.
10. As a Player, I want Heroes that reach the Keep to damage it, so that letting them through matters.
11. As a Player, I want the Keep's health always visible, so that I know how much danger I am in.
12. As a Player, I want the Keep's health to carry from Battle to Battle within a Run, so that every Battle matters to the whole Run.
13. As a Player, I want the Run to end only when the Keep falls, so that one bad moment does not end everything.
14. As a Player, I want a Keeper or Familiar who is beaten to suffer a Knock-out and get back up after a few seconds, so that nobody on my side dies.
15. As a Player, I want different Hero types (fast, tough, shielded, ranged, healer, and others) to behave differently, so that Battles ask for different answers.
16. As a Player, I want some Heroes to arrive with a creature (a mount, pet or summoned beast), so that I can see who I might befriend.
17. As a Player, I want a mid-boss and a boss in each Chapter with their own behaviour, so that Chapters build to a peak.
18. As a Player, I want to pause a Battle at any time, so that I can put the phone down.
19. As a Player, I want speed-up available from the very start at no cost, so that easy Battles do not drag.
20. As a Player, I want a clear win or lose moment at the end of a Battle, so that I know what happened.

### Elements and Combos

21. As a Player, I want each Skill to deal one of four Elements (fire, frost, spark, bloom), so that my squad has a recognisable character.
22. As a Player, I want each Element to leave its own status on a Hero (burn, chill, shock, root), so that I can see what my squad is doing.
23. As a Player, I want a Combo burst when a second Element hits a Hero carrying another Element's status, so that mixing Elements is rewarded.
24. As a Player, I want Combos to be visible and loud, so that I notice and enjoy them.
25. As a Player, I want to tap any status, stat or Skill to read a plain explanation, so that I never need a wiki.

### Run and Trail

26. As a Player, I want to choose one Familiar I own to start a Run alongside the Keeper, so that each Run starts with a different flavour.
27. As a Player, I want a Run to be three Chapters of about seven Stops each, so that a full Run takes 20 to 25 minutes.
28. As a Player, I want the Trail drawn as a side-scrolling path through the Chapter, so that travelling feels like a journey and not a menu.
29. As a Player, I want to see only the next two steps on the Trail with fog beyond, so that I can plan a little without solving the whole route.
30. As a Player, I want the Chapter's boss visible as a distant landmark with a count of steps remaining, so that I know how far I have to go.
31. As a Player, I want the Trail to fork, so that I choose between different Stops.
32. As a Player, I want Battle Stops, so that I can fight and earn a Skill Upgrade.
33. As a Player, I want tough Battle Stops with a better reward, so that I can take a risk for more.
34. As a Player, I want rest Stops that repair the Keep, so that I can recover.
35. As a Player, I want shop Stops where I spend what I earned in the Run, so that I can steer my build.
36. As a Player, I want story event Stops with choices and consequences, so that Runs have surprises.
37. As a Player, I want wild creature Stops, so that I can befriend a creature without a Battle.
38. As a Player, I want each Run's Trail to be different, so that Runs do not repeat.
39. As a Player, I want to leave the game at any point and resume the Run exactly where I was, so that I can play in short sittings.
40. As a Player, I want a summary at the end of a Run (how far I got, my squad, my Skills, what I earned), so that I can see what worked.

### Growth within a Run

41. As a Player, I want to pick 1 of 3 Skill Upgrades after each Battle, so that I shape my build.
42. As a Player, I want the Skill Upgrades offered to come from the Skills my squad actually has, so that every offer is relevant.
43. As a Player, I want each offer to say plainly which Skill it changes and how, so that I can choose quickly.
44. As a Player, I want a Skill that has received three Skill Upgrades to become its Master Form, so that committing to a Skill pays off.
45. As a Player, I want a Skill to look visibly bigger and brighter at each of three tiers (base, upgraded, Master Form), so that my power shows on screen.
46. As a Player, I want to befriend a creature freed from a defeated Hero, so that the squad grows from the fights I win.
47. As a Player, I want a befriended creature to join as a Familiar for the rest of the Run, up to four Familiars, so that the squad fills out as I go.
48. As a Player, I want a choice of what to do when the squad is full and a new creature is on offer, so that a full squad is not a dead end.
49. As a Player, I want to see my whole build (squad, Skills, upgrades taken) at any time during a Run, so that I can plan.

### The Keep and permanent progress

50. As a Player, I want to return to an illustrated Keep between Runs, so that the game has a home.
51. As a Player, I want rooms of the Keep to appear as I unlock them, so that my progress has a visible shape.
52. As a Player, I want to earn one currency from every Run, win or lose, with no cap, so that every Run moves me forward.
53. As a Player, I want a Keeper upgrade track I spend that currency on, so that my Keeper grows stronger over time.
54. As a Player, I want to customise how my Keeper looks, so that the Keeper feels like mine.
55. As a Player, I want a Familiar book showing every Familiar, owned or not, so that I know what exists.
56. As a Player, I want each unowned Familiar to show the Deed or Bond that unlocks it, so that I always know what to do next.
57. As a Player, I want a Deed to unlock its Familiar the moment I complete it, so that achievements feel rewarding.
58. As a Player, I want a Bond meter to fill each time I befriend that kind of creature, and to own it permanently when the meter is full, so that the creatures I keep meeting become mine.
59. As a Player, I want to start with three Familiars owned and earn three more, so that there is something to work towards from the first Run.
60. As a Player, I want three difficulty tiers that unlock as I win, so that the game keeps challenging me.
61. As a Player, I want random pulls, if they exist at all, to give only cosmetics and to cost only earned currency, so that nothing that matters is left to luck or money.
62. As a Player, I want my progress saved on the device automatically, so that I never lose it.
63. As a Player, I want my save to survive game updates, so that updating never costs me progress.

### Look, layout and sound

64. As a Player, I want warm, slightly muted backgrounds and characters with saturated Skill effects, so that the effects are the loudest thing on screen.
65. As a Player, I want the Keep's health top left, Chapter and wave progress top centre, pause and speed top right, squad portraits with Skill cooldowns bottom left and the Rally button bottom right, so that both thumbs rest clear of small targets and the middle of the screen stays free.
66. As a Player, I want every screen to share one look (colours, type, panels, buttons), so that the game feels like one thing.
67. As a Player, I want the layout to respect notches and rounded corners, so that nothing is hidden on my phone.
68. As a Player, I want characters to bounce, squash and flash when they act and are hit, so that static art feels alive.
69. As a Player, I want sound for attacks, Skills, Combos, Rally and menus, so that the game has feedback.
70. As a Player, I want the game to hold 60 frames per second on a mid-range phone, so that it feels smooth.
71. As a Player, I want content suitable for everyone, so that I can play or share it anywhere.

### The developer (Ryan) and the building agent

72. As the developer, I want every Familiar, Hero, Skill, Skill Upgrade, Stop and tuning number to be a data asset editable in the Unity Inspector, so that I can change content without code.
73. As the developer, I want to swap any character's or effect's art by replacing an asset, so that free packs can be replaced by my own art later without touching layouts.
74. As the developer, I want each milestone to end in a build I install on my Android phone over USB, so that I judge feel on the real device.
75. As the developer, I want a bot that plays whole Runs and reports win rates and Run lengths, so that balance changes can be checked without playing by hand.
76. As the building agent, I want game rules that run without a scene or graphics, so that I can test them automatically.
77. As the developer, I want the project in git with a Unity-appropriate ignore file, so that every step can be undone.

## Implementation Decisions

### Platform and project

- Unity 6000.6.5f1, Universal Render Pipeline 2D, Android target, landscape only (ADR 0001). App identifier and product name set to Little Keep's own.
- The old DS Squad project is a reference for ideas only; no code is copied wholesale.

### Shape

- **Rules are separate from scenes.** All game rules live in plain C# with no dependency on scenes, GameObjects or frame timing, in their own assembly. Scenes and UI are a presentation layer that reads rule state and sends Player input in. This was the main structural fault of the old prototype.
- Three rule modules, each with one small interface, which are also the test seams:
  - **Battle simulation.** Input: a squad (Keeper plus Familiars with their Skills and upgrades), a list of Hero waves, a seed, and Player input over time (aim point or none, Rally presses). Stepped forward by a fixed time step. Output: a readable state (positions, health, statuses, Knock-outs, Keep health, wave progress) and a stream of events (attack, Skill cast, hit, status applied, Combo, Knock-out, Hero reached Keep, creature freed, win, lose) that the presentation layer turns into visuals and sound.
  - **Run.** Input: a seed, the starting Familiar, the Player's permanent progress, and a sequence of choices (Stop taken, Skill Upgrade picked, creature befriended or passed, shop purchase, event option). Output: Run state (Chapter, position on the Trail, the two visible steps, Keep health, squad, upgrades, Run currency) and, on ending, a Run result. Owns Trail generation, the Skill Upgrade offer, Master Form promotion, Befriending and the full-squad choice, shops, events and mid-Run save and resume.
  - **Keep progress.** Input: Run results and purchases. Output: what is owned and unlocked. Owns the earned currency, the Keeper upgrade track, Deeds, Bonds, difficulty tiers and the save file.
- All randomness comes from a seed passed in, so any Battle or Run can be replayed exactly.

### Content as data

- Familiars, Heroes, creatures, Skills, Skill Upgrades, Elements and statuses, encounters and waves, Stop weights, events, shop stock, Deeds, Bond sizes, upgrade tracks, difficulty tiers and tuning numbers are ScriptableObject assets with stable ids used in saves.
- A Skill Upgrade targets a Skill by id, never a named character (the old prototype hard-wired upgrades to two characters by name).
- A Skill has three visual tiers; the tier is derived from its upgrade count.
- Tuning values not fixed by this spec (currency name, Bond sizes, shop prices, rest amount, difficulty names, cooldowns) are set by the building agent as data and adjusted through playtesting.

### Battle rules

- Keeper and Familiars stand in front of the Keep and attack automatically. Auto-aim picks the Hero that is the greatest threat to the Keep; a held aim point overrides it for the Keeper only.
- Skills cast themselves when ready. Rally casts every ready-or-not Skill once and has its own long cooldown.
- Four Elements, each with one status. A Combo triggers when a hit of one Element lands on a Hero carrying a different Element's status; it deals burst damage and consumes the status.
- Knock-outs last a few seconds, then the character returns. Keep health persists across the Run; zero ends the Run.

### Run rules

- Three Chapters of about seven Stops, each with a mid-boss and a boss. Stop types: Battle, tough Battle, rest, shop, story event, wild creature. The Trail forks; the Player sees the next two steps and the boss's distance.
- After a Battle: 1 of 3 Skill Upgrades from the squad's own Skill pools. Three upgrades on a Skill make its Master Form.
- Squad: Keeper plus up to four Familiars; starts with one chosen owned Familiar. Creatures are befriended from defeated Heroes that brought one and from wild creature Stops. Heroes are never befriended.
- No gear or crafting.

### Permanent progress

- One earned currency, granted win or lose, uncapped. Spent on the Keeper upgrade track.
- Six Familiars: three owned at the start, three earned by Deed or Bond (ADR 0002). Any random pull is cosmetic only and paid with earned currency. No real-money purchases in this version; the economy is structured so an up-front price, a one-off unlock or direct purchases could be added later.
- Three difficulty tiers unlocking on wins.
- Saves are a versioned JSON file on the device, with migration on load. A Run in progress is saved after every choice.

### Presentation

- Standard Unity UI with one shared theme asset (colours, type, panel and button styles) used by every screen. Layouts respect the device safe area.
- Battle screen layout as in user story 65.
- The Keep is an illustrated scene whose rooms are buttons that appear when unlocked.
- Characters are single-view sprites animated in code (squash, bounce, flash). Skill effects are particle and shader effects with three intensity tiers. Free asset packs stand in until the developer's own art; their licences must allow commercial use.
- One audio module in front of all sound, with stand-in sounds until a licensed pack is chosen.

### Scope of the first complete version

- 1 Keeper, 6 Familiars with about 4 Skills each (every Skill with a Master Form), about 12 Hero types, 3 mid-bosses and 3 bosses, 3 Chapters, 3 difficulty tiers.

### Milestones (each ends in a phone build)

1. **Battle:** project setup; grey-box landscape Battle with the Keeper (auto-attack, hold-to-aim, Rally), 2 Familiars, 3 Hero types, Keep health, win and lose.
2. **Run:** the Trail, all Stop types, Skill Upgrades, Master Forms, Befriending, three Chapters with bosses, mid-Run save.
3. **Keep:** the Keep screen, currency, Keeper upgrade track, Deeds, Bonds, Familiar book, saves, difficulty tiers.
4. **Content:** full cast, Elements and Combos across all Skills, events, balance with the Run bot.
5. **Look and sound:** themed UI, character art from free packs, three-tier Skill effects, audio. Theme work may start alongside milestone 2.

## Testing Decisions

- A good test drives a rule module through its interface with a seed and inputs and checks only what comes out (state and events). It never inspects private state, scenes or frame timing, and it survives any internal rewrite.
- Tested modules: Battle simulation, Run, Keep progress. These three seams are the only ones; nothing below them gets its own tests unless a bug shows the need.
- Tests run in Unity's edit-mode test runner (the test framework is already in the project) and need no scene.
- Examples of behaviour to cover: a Hero reaching the Keep reduces its health; a held aim point changes the Keeper's target and releasing restores auto-aim; Rally casts every Skill; a second Element on a status triggers a Combo; a Knock-out ends after its time; the same seed gives the same Battle; the Trail always reaches the boss and shows exactly two steps; a third upgrade makes a Master Form; a full squad offers a choice; a saved Run resumes identically; a full Bond grants the Familiar; a save from an older version loads.
- A Run bot plays whole Runs through the Battle simulation and Run seams and reports win rate and length per difficulty tier. It is a balance tool, not a pass or fail test.
- Presentation (layout, effects, animation, sound, feel, frame rate) is not tested automatically; the developer judges it on the phone at each milestone.
- Prior art: none in this project. The old prototype's play-through bot is the model for the Run bot.

## Out of Scope

- Gear, crafting and charms.
- Paid random pulls of any kind, and all real-money purchases, ads and store listing work.
- Cloud saves, accounts, leaderboards, multiplayer.
- iOS and any platform other than Android.
- Portrait play.
- The developer's own final character art and a licensed soundtrack (free packs and stand-ins are in scope).
- A second layer of boons from patrons, seasonal content, extra modes, and more than three difficulty tiers.
- Story beyond event text and short Familiar descriptions.
- Languages other than English.

## Further Notes

- Not yet decided, and not blocking: the developer's visual references (the default mood is in user story 64), the final name for publishing, and the monetisation model.
- If the game goes public: copy nothing from Dungeon Squad beyond general mechanics (no names, characters, text, art or sound), and take advice before release. Check every free asset pack's licence.
- Git commits from the agent's shell need delete permission on the project folder (git removes its own lock files).
- The tracker is local files under `.scratch/` until a GitHub account is linked; tickets can then become issues.
