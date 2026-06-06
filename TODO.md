# Tower Defence TODO

## Vision

Water-based tower defence inspired by river/route defence pacing, but with original visuals, balance, towers, enemies, and presentation.

Core direction:

- Enemies are boats.
- Boats travel on water routes.
- Water tiles should visually follow the waypoint path.
- Towers are placed on any non-water tile.
- The first target gameplay loop is simple, readable, and expandable.

## Clarified Design Rules

These replace earlier assumptions about docks/platform-only placement.

- Towers are not restricted to docks, piers, or fixed platforms.
- Towers should be placeable on valid land / non-water tiles.
- Water route visuals should be generated or placed to match waypoint flow.
- Enemies should visually read as weak / medium / hard / boss boats.
- Tower classes should currently be:
  - Light attack
  - Heavy attack
  - Magic-type attack
  - Air attack

## Current Status

### Done

- [x] Project retargeted to Unity `6000.4.10f1`.
- [x] URP default renderer issue fixed.
- [x] Safe Mode compiler blockers removed.
- [x] Input migrated to the new Input System.
- [x] Core gameplay scripts compile in the current Unity-generated project flow.
- [x] WaveDefinition ScriptableObject workflow added.
- [x] Starter wave assets created.
- [x] HUD wired for lives, gold, wave, game over, victory, and state text.
- [x] Project-owned assets sorted into `Assets/Art`, `Assets/Audio`, and `Assets/Prefabs`.
- [x] Build-node prototype created for the earlier dock/platform assumption.

### Done But Needs Rework

- [ ] Build-node-only placement system
  - Current state: prototype exists with `BuildNode` objects and node-based placement checks.
  - Problem: this no longer matches the intended design.
  - Replace with land-tile / non-water placement validation.

- [ ] Harbor scene presentation pass
  - Current state: placeholder map logic exists, but the scene direction still reflects the earlier node-placement assumption.
  - Rework to support:
    - water route tiles from waypoints
    - open land placement
    - stronger harbor / river defence identity

## Gameplay Systems

### World / Map

- [ ] Define the first map as a river / harbor water route with land on both sides.
- [ ] Use waypoint positions to define where water route tiles or segments sit.
- [ ] Add a clear spawn point at the route start.
- [ ] Add a clear base / end point at the route finish.
- [ ] Make water visually distinct from buildable land.
- [ ] Mark blocked / decorative land separately from buildable land.
- [ ] Add a visible base / core / end target presentation object.
- [ ] Add route readability markers so the path is obvious at a glance.

### Enemy System

- [ ] Finalize first boat classes:
  - [ ] Weak boat
  - [ ] Medium boat
  - [ ] Hard boat
  - [ ] Boss boat
- [ ] Assign each enemy:
  - [ ] speed
  - [ ] health / hit points
  - [ ] reward / prize
  - [ ] resistance or armor hook
  - [ ] point or life damage on escape
- [ ] Add distinct placeholder sprites or silhouettes per boat class.
- [ ] Add end-of-path life loss feedback:
  - [ ] floating `-x` lives text
  - [ ] base hit feedback

### Tower System

- [ ] Replace placeholder tower identity with the current intended archetypes:
  - [ ] Light attack tower
  - [ ] Heavy attack tower
  - [ ] Magic-type tower
  - [ ] Air attack tower
- [ ] Give each tower:
  - [ ] range
  - [ ] rate of fire
  - [ ] cost
  - [ ] sell ratio
  - [ ] damage type
  - [ ] upgrade path
  - [ ] cooldown
  - [ ] target preference hooks
- [ ] Make tower visuals clearly different even before final art exists.
- [ ] Add placement on any valid non-water tile.
- [ ] Remove dependency on fixed build nodes for core placement flow.
- [ ] Add money-spent feedback on placement:
  - [ ] floating `-x` money text
  - [ ] optional sound / pop animation

### Combat / Projectiles

- [ ] Match projectile style to the four tower archetypes.
- [ ] Light attack projectile behavior
- [ ] Heavy attack projectile behavior
- [ ] Magic projectile behavior
- [ ] Air-targeting projectile behavior
- [ ] Add clearer hit feedback on enemies.
- [ ] Add splash / water impact feedback where appropriate.

### Waves / Rounds

- [ ] Review and rebalance the first five WaveDefinitions around:
  - [ ] round pacing
  - [ ] enemy counts
  - [ ] spawn intervals
  - [ ] reward scaling
  - [ ] boss wave pressure
- [ ] Define round structure more clearly:
  - [ ] total rounds
  - [ ] weak round introduction
  - [ ] medium round introduction
  - [ ] hard round introduction
  - [ ] boss round timing
- [ ] Add round-complete points or reward logic if points remain part of the design.

### Lives / Health / Points

- [ ] Decide whether the player uses:
  - [ ] lives only
  - [ ] health only
  - [ ] both lives and health
- [ ] Standardize end-point penalty values for escaped enemies.
- [ ] Add visible floating `-x` feedback when lives are lost.
- [ ] Clarify the points system:
  - [ ] kill points
  - [ ] round completion points
  - [ ] boss points
  - [ ] score / high score usage

## UI / UX

### Core HUD

- [x] Lives display
- [x] Gold display
- [x] Wave display
- [x] Game over text
- [x] Victory text
- [x] State text
- [ ] Next round / start round button polish
- [ ] Round speed button polish
- [ ] Pause button polish
- [ ] Restart button polish
- [ ] Money spent `-x` popup
- [ ] Lives lost `-x` popup

### Menus

- [ ] Main menu polish
- [ ] Settings flow review
- [ ] End screen
- [ ] Restart on fail flow
- [ ] Exit / end button behavior
- [ ] High score display if kept in scope
- [ ] Game naming / branding pass

## Art / Presentation

- [ ] Enemy boat sprites
- [ ] Tower sprites for all four tower types
- [ ] Water route visuals
- [ ] Land / non-water buildable tile visuals
- [ ] Harbor / base / core visual
- [ ] Route marker visuals
- [ ] UI graphics consistency pass
- [ ] Graphics style pass to make the game feel like one coherent project

## Audio

- [ ] Placement sound
- [ ] Light tower fire sound
- [ ] Heavy tower fire sound
- [ ] Magic tower fire sound
- [ ] Air tower fire sound
- [ ] Boat hit sound
- [ ] Boat death sound
- [ ] Water / splash impact sound
- [ ] Base hit sound
- [ ] Round start sound
- [ ] Round complete sound
- [ ] Background loop

## Technical / Refactor

- [x] Replace obsolete `FindObjectOfType` usage.
- [x] Replace deprecated `Rigidbody2D.isKinematic`.
- [x] Migrate gameplay input off the legacy Input Manager API.
- [ ] Replace build-node placement with non-water-tile validation.
- [ ] Add reusable feedback popup system for money and life deltas.
- [ ] Consider scene-level manager bootstrap cleanup after gameplay loop stabilizes.
- [ ] Re-run a full Unity editor validation pass after the placement refactor.

## Recommended Next Execution Order

1. Replace build-node-only placement with non-water-tile placement.
2. Add visible water route tiles based on waypoint flow.
3. Add a simple harbor / base end-point visual.
4. Re-theme the current towers into:
   - light
   - heavy
   - magic
   - air
5. Add `-x` money and `-x` lives feedback popups.
6. Rebalance the first five rounds around the new enemy ladder.
7. Add enemy art / placeholder silhouettes and stronger route readability.

## Notes

- The current project is no longer blocked by the earlier Safe Mode package/compiler issue.
- The current biggest design mismatch is the old node-based placement assumption.
- Before more scene polish, placement rules should be corrected to "any non-water tile".
