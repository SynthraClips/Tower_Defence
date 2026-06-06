# /goal 4 — Enemy Boat Ladder

```text
/goal Create and configure the first enemy boat class ladder.

Enemies are boats travelling along water routes. They should visually and mechanically read as weak, medium, hard, and boss enemy types.

Focus only on enemy classes, stats, placeholders, and escape feedback hooks.

Tasks:
- Finalise the first boat classes:
  - Weak boat
  - Extra-fast weak boat / raft variant if useful for pacing
  - Medium boat
  - Hard boat
  - Boss boat
- Assign each enemy type:
  - Speed
  - Health / hit points
  - Reward / prize
  - Resistance or armour hook
  - Point or life damage on escape
- Add distinct placeholder sprites or silhouettes for each boat class.
- Make weak, medium, hard, and boss boats visually different.
- Ensure enemy escape triggers life loss correctly.
- Ensure escape feedback works with the popup system if already present.
- Keep enemy definitions easy to rebalance later.

Do not overhaul waves or tower combat in this goal beyond what is needed to support the enemy classes.

Acceptance criteria:
- Each enemy class has clear stats.
- Each boat type is visually distinguishable.
- Escaped enemies apply the correct life/point penalty.
- Enemy data can be reused by WaveDefinitions.
```
