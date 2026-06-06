# Tower Defence TODO

This tracker follows the numbered goal documents in the `Documentation` folder.

Primary source of truth:

- [README_goals.md](./README_goals.md)
- [01-map-route-harbour-scene-identity.md](./01-map-route-harbour-scene-identity.md)
- [02-non-water-tile-placement-polish.md](./02-non-water-tile-placement-polish.md)
- [03-reusable-feedback-popup-system.md](./03-reusable-feedback-popup-system.md)
- [04-enemy-boat-ladder.md](./04-enemy-boat-ladder.md)
- [05-tower-archetypes.md](./05-tower-archetypes.md)
- [06-projectiles-and-combat-feedback.md](./06-projectiles-and-combat-feedback.md)
- [07-waves-and-round-balance.md](./07-waves-and-round-balance.md)
- [08-hud-menus-and-game-flow-polish.md](./08-hud-menus-and-game-flow-polish.md)
- [09-art-and-presentation-pass.md](./09-art-and-presentation-pass.md)
- [10-audio-pass.md](./10-audio-pass.md)
- [11-final-technical-validation-and-cleanup.md](./11-final-technical-validation-and-cleanup.md)
- [12-optional-first-playable-vertical-slice.md](./12-optional-first-playable-vertical-slice.md)

## Goal Status

### Goal 1 — Map, Route, and Harbour Scene Identity

Document:
- [01-map-route-harbour-scene-identity.md](./01-map-route-harbour-scene-identity.md)

Status:
- `In progress`

Done:
- [x] Open land placement direction is now supported by gameplay logic.
- [x] Waypoint-driven route presentation exists via `PathRouteVisualizer`.
- [x] Route rendering is stronger than the original debug line.
- [x] Route now has layered bank / water / foam presentation.
- [x] Start of route now has a visible spawn marker.
- [x] Route now has waypoint readability markers.
- [x] End of route now has a clearer harbor/core marker.
- [x] Placement no longer depends on fixed dock/build-node anchors.

Still needed:
- [ ] make the route feel like true water tiles / flowing water, not just layered line art
- [x] add a clearer spawn-point marker
- [ ] push the harbor/base/core end-point visual further beyond the current marker treatment
- [ ] make blocked/decorative land read more clearly
- [ ] strengthen the overall “river / harbour defence” scene identity

### Goal 2 — Non-Water Tile Placement Polish

Document:
- [02-non-water-tile-placement-polish.md](./02-non-water-tile-placement-polish.md)

Status:
- `In progress`

Done:
- [x] old active build-node dependency removed from gameplay flow
- [x] temporary `BuildNode_*` scene roots removed from active scenes
- [x] placement now targets non-water gameplay space instead of fixed anchor nodes
- [x] route corridor is used as the main no-build water exclusion
- [x] map sprites are no longer the primary placement authority
- [x] placement uses board/play-area bounds plus route exclusion
- [x] placement uses the new Input System

Still needed:
- [ ] review edge-case placement failures around route width and tower collider overlap
- [ ] improve visual clarity for valid vs invalid placement
- [ ] verify blocked/decorative land handling once the harbor scene visuals are improved
- [ ] confirm placement behavior on all intended gameplay maps, not just the current test scene

### Goal 3 — Reusable Feedback Popup System

Document:
- [03-reusable-feedback-popup-system.md](./03-reusable-feedback-popup-system.md)

Status:
- `Partially done`

Done:
- [x] spending gold triggers popup feedback
- [x] losing lives triggers popup feedback
- [x] popup animation / fade exists
- [x] placement is connected to money-spent feedback
- [x] end-of-path life loss is connected to life-loss feedback

Still needed:
- [ ] move the popup implementation out of `HUDController` into a reusable dedicated system
- [ ] expose configurable duration, motion, and style more cleanly
- [ ] optionally move relevant popups closer to the world event, not just HUD text anchors
- [ ] support future reward / score / warning popups from the same system

### Goal 4 — Enemy Boat Ladder

Document:
- [04-enemy-boat-ladder.md](./04-enemy-boat-ladder.md)

Status:
- `Not started`

Still needed:
- [ ] finalize weak / fast-weak / medium / hard / boss boat classes
- [ ] assign proper health / speed / reward / escape penalty values
- [ ] add distinct placeholder visuals for each boat type
- [ ] connect boat identities cleanly to current wave data

### Goal 5 — Tower Archetypes

Document:
- [05-tower-archetypes.md](./05-tower-archetypes.md)

Status:
- `Partially done`

Done:
- [x] Light attack placeholder identity exists
- [x] Heavy attack placeholder identity exists
- [x] Magic placeholder tower class exists
- [x] Air placeholder tower class exists
- [x] tower metadata now carries archetype/display-name direction

Still needed:
- [ ] wire Magic and Air into visible shop/UI flow
- [ ] create distinct placeholder visuals for all four tower types
- [ ] finalize targeting / attack preference hook usage
- [ ] confirm each tower has clearly different gameplay feel

### Goal 6 — Projectiles and Combat Feedback

Document:
- [06-projectiles-and-combat-feedback.md](./06-projectiles-and-combat-feedback.md)

Status:
- `Not started`

Still needed:
- [ ] define projectile identities for all four tower archetypes
- [ ] improve hit feedback and impact readability
- [ ] improve water/splash combat feedback

### Goal 7 — Waves and Round Balance

Document:
- [07-waves-and-round-balance.md](./07-waves-and-round-balance.md)

Status:
- `Partially done`

Done:
- [x] WaveDefinition workflow exists
- [x] first five starter wave assets exist
- [x] initial rebalance pass was applied toward weak/medium/hard/boss structure

Still needed:
- [ ] validate the new wave pacing in actual play
- [ ] confirm total round progression
- [ ] integrate proper boss cadence
- [ ] tune reward scaling against tower costs

### Goal 8 — HUD, Menus, and Game Flow Polish

Document:
- [08-hud-menus-and-game-flow-polish.md](./08-hud-menus-and-game-flow-polish.md)

Status:
- `Partially done`

Done:
- [x] lives / gold / wave / game over / victory / state HUD items exist
- [x] money spent and lives lost feedback exists in some form

Still needed:
- [ ] next round / start round flow polish
- [ ] speed control polish
- [ ] pause flow polish
- [ ] restart / fail flow polish
- [ ] menu / settings cleanup

### Goal 9 — Art and Presentation Pass

Document:
- [09-art-and-presentation-pass.md](./09-art-and-presentation-pass.md)

Status:
- `Not started`

Still needed:
- [ ] enemy placeholder art pass
- [ ] tower placeholder art pass
- [ ] route water presentation pass
- [ ] harbor/base/core visual pass
- [ ] stronger unified visual style

### Goal 10 — Audio Pass

Document:
- [10-audio-pass.md](./10-audio-pass.md)

Status:
- `Not started`

Still needed:
- [ ] confirm tower / hit / death / splash / base-hit sound coverage
- [ ] connect background music loop
- [ ] connect round start / round complete feedback

### Goal 11 — Final Technical Validation and Cleanup

Document:
- [11-final-technical-validation-and-cleanup.md](./11-final-technical-validation-and-cleanup.md)

Status:
- `In progress`

Done:
- [x] Unity 6.4 upgrade/retarget complete
- [x] URP renderer issue fixed
- [x] Safe Mode package/compiler blockers fixed
- [x] input migration completed
- [x] obsolete API cleanup pass started
- [x] asset sorting pass completed
- [x] git repo initialized and revision commits started

Still needed:
- [ ] full editor validation after placement stabilization
- [ ] final warning cleanup pass
- [ ] deeper prefab / scene audit after gameplay loop settles

### Goal 12 — Optional First Playable Vertical Slice

Document:
- [12-optional-first-playable-vertical-slice.md](./12-optional-first-playable-vertical-slice.md)

Status:
- `Blocked by earlier goals`

Still needed first:
- [ ] Goal 1 scene identity
- [ ] Goal 4 enemy ladder
- [ ] Goal 5 four tower archetypes fully wired
- [ ] Goal 6 combat feedback
- [ ] Goal 7 playable balance

## Cross-Cutting Notes

- Towers should be placeable on any valid non-water tile.
- Boats use the water route as the path.
- Waypoints should dictate where route-water visuals sit.
- Current tower direction is:
  - Light attack
  - Heavy attack
  - Magic-type attack
  - Air attack
- At least three levels are planned:
  - Easy
  - Medium
  - Hard
- Routes should differ per level.
- A reusable level template / level-building approach is now needed.

## Recommended Immediate Next Work

1. Finish Goal 1 presentation so the route feels like proper water tiles/flow and the harbor/base/core reads clearly.
2. Finish Goal 2 polish by tightening any remaining placement edge cases and making valid placement more obvious.
3. Upgrade Goal 3 from “working” to “reusable system.”
4. Start Goal 4 enemy ladder and Goal 5 full tower wiring in parallel through normal implementation passes.
