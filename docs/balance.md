# Balance

<!-- bot results: written by Little Keep/Run Bot: write balance report -->
## Latest bot results

16 Runs per row. The bot never aims, Rallies when ready, takes the first offer, befriends when there is room and rests when hurt.
Run length = Battle time at 1x speed plus about 6s per Trail choice, 10s per upgrade or Befriending, 12s per event and 15s per shop.

| Tier | Keeper upgrades | Won | Run length | Won Run length | Battle time | Avg Chapter reached | Skills learned per Run | Full squads |
|---|---|---|---|---|---|---|---|---|
| Cozy | 0 | 11/16 | 20.9 min | 20.3 min | 13.8 min | 2.9 | 5.5 | 15/16 |
| Cozy | 2 | 13/16 | 21.7 min | 20.0 min | 14.3 min | 3.0 | 5.8 | 16/16 |
| Brisk | 2 | 10/16 | 21.0 min | 23.4 min | 14.5 min | 2.8 | 5.0 | 13/16 |
| Brisk | 4 | 11/16 | 23.5 min | 22.2 min | 16.0 min | 3.0 | 5.5 | 15/16 |
| Fierce | 4 | 3/16 | 21.8 min | 24.7 min | 16.2 min | 2.4 | 4.1 | 10/16 |
| Fierce | 5 | 4/16 | 24.2 min | 27.2 min | 18.3 min | 2.6 | 4.4 | 11/16 |

Tuning: Hero health x2.05, Hero numbers x1.8, +25% health per Chapter, +6% per step, 8 steps per Chapter.
<!-- end of bot results -->

## Tuning log

Newest first. What changed, and why.

### 2026-11-17 · Ticket 05, full cast

- After review: Captain Brash now calls in militia and Archmage Vale calls in Hedge Wizards, so every mid-boss has its own trick; Rally no longer wastes a Nova with no Hero in reach. Results above are after these changes.

- Starting point with the full cast (four Skills per Familiar, 12 Heroes, 14 events): Cozy 4/12, Runs about 19 minutes, almost every loss at the final boss.
- Hero health per Chapter 35% to 25%: Chapter 3 was a wall the squad could not grow into.
- Steps per Chapter 7 to 8 and Hero health per step 4% to 6%: more Battles and choices per Run, with Heroes keeping pace with the squad's upgrades. (Nine steps let the squad outgrow everything: Cozy 12/12 with shorter Runs.)
- A third wave in every regular and tough Battle: Battle length is mostly walking time per wave, so more Heroes per wave barely lengthened Battles. This took Runs from about 18 to 21 minutes.
- Hero health x2.2 to x2.05 and Hero numbers x1.6 to x1.8: moves some of the difficulty from health into numbers, so Battles last longer without each Hero being a sponge.
- Brisk stays at 1.15x health and 1.1x numbers; Fierce goes from 1.3x to 1.4x health. Win rates are very sensitive to these: Brisk 1.25x and Fierce 1.5x dropped the bot to 4/16 and 1/16, with Fierce Runs ending in Chapter 1.
- The bot never aims, so Players should win more often than these numbers; the aim is for Fierce to stay a stretch with full Keeper upgrades.

### 2026-11-16 · Ticket 04, Elements and Combos

- Elements made every tier easy (12/12, even Fierce). Combo 1.5x to 1.25x, burn 60% to 50%, shock 25% to 20%, Hero health x1.65 to x2.2. Cozy 7/12.

### 2026-11-16 · Ticket 03, the Keep

- Tiers set: Cozy 0.95x health, Brisk 1.15x health and 1.1x numbers, Fierce 1.3x health and 1.2x numbers.

### 2026-11-16 · Ticket 02, the Run

- First tuning: Hero health x1.65, numbers x1.6, +35% per Chapter, +4% per step. Bot 5/10, about 12.5 minutes of Battles per Run.
