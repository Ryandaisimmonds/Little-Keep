# Run saves record choices and replay them

A Run in progress is saved as its seed, starting Familiar, a fingerprint of the content, and the list of choices the Player made (which Stop, which upgrade, each Battle's result, and so on). Loading replays those choices. We chose this over saving the Run's full state as JSON because every random draw comes from the seed, so replaying the choices rebuilds the exact Run with far less code and no risk of the saved state and the rules disagreeing.

## Consequences

- A save only means the same Run against the same content. The fingerprint (ids, order and the numbers that steer random draws) is checked on load, and a mismatch is refused with a message rather than replayed into a different Run. So a content update ends any Run in progress; permanent progress (ticket 03) is saved separately and must survive updates.
- Leaving mid-Battle records the Keep's health at that moment; the Battle itself restarts on resume.
- The spec's "versioned JSON file" now applies to permanent progress only.
