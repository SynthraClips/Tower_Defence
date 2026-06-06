# /goal 7 — Waves and Round Balance

```text
/goal Rebalance the first waves and define the initial round structure.

The game already has a WaveDefinition ScriptableObject workflow and starter wave assets. This pass should cleanly rebalance the first set of rounds around the new enemy boat ladder.

Focus only on waves, rounds, pacing, rewards, and round structure.

Tasks:
- Review and rebalance the first five WaveDefinitions.
- Balance around:
  - Round pacing
  - Enemy counts
  - Spawn intervals
  - Reward scaling
  - Boss wave pressure
- Define the initial round structure:
  - First round: 10 weak enemies
  - Second round: 10 medium enemies
  - Each round increases enemy count
  - Weak enemy introduction
  - Medium enemy introduction
  - Hard enemy introduction
  - Boss round timing
  - End-of-round boss cadence
- Decide whether round-complete rewards or points remain in scope.
- Add round-complete points/reward logic if still part of the design.
- Keep wave data easy to edit from ScriptableObjects.

Do not change tower placement, map layout, or UI styling in this goal unless needed to test waves.

Acceptance criteria:
- The first five waves play with sensible pacing.
- Enemy difficulty escalates clearly.
- Rewards support continued tower building.
- Boss pressure feels noticeable but not unfair.
- WaveDefinitions remain editable and reusable.
```
