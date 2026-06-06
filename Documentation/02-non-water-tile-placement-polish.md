# /goal 2 — Non-Water Tile Placement Polish

```text
/goal Polish and stabilise tower placement so towers can be placed on any valid non-water tile.

The old fixed build-node approach has been removed from the core design. Towers should be placeable on buildable land/non-water tiles only.

Focus only on placement logic, placement validation, and placement feedback.

Tasks:
- Confirm tower placement no longer depends on fixed build nodes.
- Ensure towers cannot be placed on water route tiles.
- Ensure towers cannot be placed on blocked/decorative land.
- Ensure towers cannot overlap existing towers.
- Make valid and invalid placement areas visually clear.
- Add hover/preview feedback for valid vs invalid placement.
- Keep placement working with the new Input System.
- Make sure the harbour scene supports open land placement correctly.
- Remove or disable any leftover active gameplay scene roots related to temporary fixed build nodes.

Do not add new tower types, upgrade systems, or enemy balance in this goal.

Acceptance criteria:
- Towers can be placed on valid land tiles.
- Towers cannot be placed on water, blocked tiles, or occupied spaces.
- Placement rules are easy to understand while playing.
- No old build-node dependency remains in the active gameplay flow.
```
