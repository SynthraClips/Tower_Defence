# Tower Defence

Water-based tower defence project built in Unity and C#.

The target game loop is:

- boats travel along water routes
- towers are placed on any valid non-water tile
- players defend the end point from escalating rounds
- tower classes are light, heavy, magic, and air attack

## Current Direction

This project is being shaped toward a river / harbor defence feel inspired by route-based mobile tower defence games, but with its own art direction, balance, tower identities, and progression.

## Current Status

The project already includes:

- modular C# gameplay foundations
- wave definitions via ScriptableObjects
- core game state, wave, enemy, projectile, and HUD systems
- Unity 6.4 compatibility fixes
- URP renderer repair
- Input System migration
- initial TODO planning in [TODO.md](./TODO.md)

## Next Priorities

1. Replace the old build-node-only placement prototype with placement on any non-water tile.
2. Build visible water route presentation from the waypoint path.
3. Re-theme the towers into light, heavy, magic, and air archetypes.
4. Add feedback for money spent and lives lost.

## Repository Notes

This repository should use small revision commits as the project evolves so gameplay, scene work, and technical refactors stay easy to review.
