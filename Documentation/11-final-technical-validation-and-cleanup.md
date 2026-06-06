# /goal 11 — Final Technical Validation and Cleanup

```text
/goal Run a final technical validation and cleanup pass after the core gameplay loop stabilises.

Focus on stability, Unity validation, deprecated API cleanup, scene setup, and project hygiene.

Tasks:
- Re-run a full Unity editor validation pass.
- Confirm no compiler errors remain.
- Confirm no Safe Mode blockers remain.
- Confirm the project still targets Unity 6000.4.10f1 correctly.
- Confirm the URP renderer setup is valid.
- Confirm the new Input System is used consistently.
- Confirm obsolete FindObjectOfType usage has not returned.
- Confirm deprecated Rigidbody2D.isKinematic usage has not returned.
- Review scene-level manager bootstrap setup.
- Clean up scene roots after placement/map refactors.
- Remove unused temporary objects, test objects, or obsolete build-node leftovers.
- Confirm assets are organised into the correct folders.
- Confirm the core gameplay loop works:
  - Start round
  - Spawn enemies
  - Place towers
  - Spend gold
  - Damage enemies
  - Earn rewards
  - Lose lives on escape
  - Trigger game over
  - Trigger victory

Acceptance criteria:
- Project compiles cleanly.
- Main scene runs without obvious errors.
- Core gameplay loop is playable.
- No obsolete placement assumptions remain.
- The project is ready for the next feature/design pass.
```
