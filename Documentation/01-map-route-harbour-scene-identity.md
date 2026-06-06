# /goal 1 — Map, Route, and Harbour Scene Identity

```text
/goal Improve the current tower defence map and harbour scene presentation.

The game is a water-based tower defence game where enemies are boats travelling along water routes. Towers must be placeable on valid land / non-water tiles, not fixed build nodes.

Focus only on the world/map presentation in this pass.

Tasks:
- Define the first map as a river / harbour water route with land on both sides.
- Use the existing waypoint positions to generate or place water route tiles/segments.
- Make the enemy path visually read as water flow rather than simple route markup.
- Add a clear spawn point at the route start.
- Add a clear base/end point at the route finish.
- Make water visually distinct from buildable land.
- Mark blocked/decorative land separately from buildable land.
- Add route readability markers so the path is obvious at a glance.
- Add a simple harbour/base/core presentation object at the end point.

Do not rework towers, waves, enemies, UI, audio, or upgrade systems in this goal unless strictly needed to support the map changes.

Acceptance criteria:
- The route clearly looks like a water path.
- Land placement areas are obvious.
- Spawn and base/end point are visible.
- The scene better communicates “harbour / river defence”.
- Existing gameplay still runs.
```
