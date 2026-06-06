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
- [x] Medium and hard scene scaffolds now exist as separate route variants.

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
- `Mostly done`

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
- `Mostly done`

Done:
- [x] spending gold triggers popup feedback
- [x] losing lives triggers popup feedback
- [x] popup animation / fade exists
- [x] placement is connected to money-spent feedback
- [x] end-of-path life loss is connected to life-loss feedback
- [x] popup logic now lives in a reusable dedicated system
- [x] UI and world-space popup entry points both exist
- [x] popup colour, text, duration, and movement are configurable from the system surface
- [x] enemy damage / reward feedback can reuse the same popup system

Still needed:
- [ ] add a cleaner shared style preset workflow if multiple popup themes are needed later
- [ ] expand into score / warning / objective popups if those systems stay in scope

### Goal 4 — Enemy Boat Ladder

Document:
- [04-enemy-boat-ladder.md](./04-enemy-boat-ladder.md)

Status:
- `Mostly done`

Done:
- [x] weak / fast-weak / medium / hard / boss boat definitions now exist
- [x] health / speed / reward / escape penalty values are now data-driven
- [x] resistance / armour hook exists through boat definition multipliers and flat armor
- [x] boat identities are connected cleanly to `WaveDefinition` data
- [x] distinct placeholder visuals exist via tint / scale / naming differences
- [x] enemy escape feedback works with the popup system

Still needed:
- [ ] replace tint-only differentiation with stronger bespoke placeholder art when the art pass lands

### Goal 5 — Tower Archetypes

Document:
- [05-tower-archetypes.md](./05-tower-archetypes.md)

Status:
- `Mostly done`

Done:
- [x] Light attack placeholder identity exists
- [x] Heavy attack placeholder identity exists
- [x] Magic placeholder tower class exists
- [x] Air placeholder tower class exists
- [x] tower metadata now carries archetype/display-name direction
- [x] tower metadata now carries damage type and attack preference hooks
- [x] target preference modes now influence target selection
- [x] Magic and Air prefabs are wired into tower placement flow
- [x] all four tower archetypes have distinct stats and placeholder colour identity

Still needed:
- [ ] wire Magic and Air into a clearer visible shop/button layout, not just prefab/shortcut support
- [ ] replace colour-only tower differentiation with stronger bespoke placeholder art
- [ ] full playtest to confirm each tower has clearly different gameplay feel

### Goal 6 — Projectiles and Combat Feedback

Document:
- [06-projectiles-and-combat-feedback.md](./06-projectiles-and-combat-feedback.md)

Status:
- `Partially done`

Done:
- [x] projectile identity now differs across light / heavy / magic / air towers
- [x] damage type now flows from tower to projectile to enemy
- [x] hit feedback now includes enemy damage popups and flash feedback
- [x] splash combat feedback now has its own popup/audio hook

Still needed:
- [ ] add stronger bespoke impact / splash VFX beyond popup-and-audio readability
- [ ] add true anti-air combat validation once flying enemies exist

### Goal 7 — Waves and Round Balance

Document:
- [07-waves-and-round-balance.md](./07-waves-and-round-balance.md)

Status:
- `Mostly done`

Done:
- [x] WaveDefinition workflow exists
- [x] first five starter wave assets exist
- [x] initial rebalance pass was applied toward weak/medium/hard/boss structure
- [x] round 1 is now 10 weak boats
- [x] round 2 is now 10 medium boats
- [x] swift raft pacing is introduced in later early waves
- [x] boss wave reward cadence is wired and improved
- [x] reward scaling is now more deliberate against current tower costs

Still needed:
- [ ] validate the new wave pacing in actual play
- [ ] confirm total round progression
- [ ] final tune reward scaling against real player build patterns

### Goal 8 — HUD, Menus, and Game Flow Polish

Document:
- [08-hud-menus-and-game-flow-polish.md](./08-hud-menus-and-game-flow-polish.md)

Status:
- `Partially done`

Done:
- [x] lives / gold / wave / game over / victory / state HUD items exist
- [x] money spent and lives lost feedback exists in some form
- [x] pause / resume hotkey flow now exists
- [x] restart and return-to-menu hotkeys now exist
- [x] end-state HUD now explains restart/menu flow more clearly
- [x] gameplay scenes now have easy / medium / hard entry-point scaffolds

Still needed:
- [ ] next round / manual start round flow if that remains in scope
- [ ] speed control polish and clearer label treatment
- [ ] visible pause / restart / menu button polish
- [ ] menu / settings scene cleanup

### Goal 9 — Art and Presentation Pass

Document:
- [09-art-and-presentation-pass.md](./09-art-and-presentation-pass.md)

Status:
- `Partially done`

Done:
- [x] route water presentation is significantly stronger than the original debug line
- [x] boats and towers now have stronger placeholder differentiation through colour and scale
- [x] separate medium / hard route shapes now exist as scene scaffolds

Still needed:
- [ ] enemy placeholder art pass beyond tint/scale silhouettes
- [ ] tower placeholder art pass beyond tint/scale silhouettes
- [ ] stronger harbor/base/core visual pass
- [ ] stronger unified visual style

### Goal 10 — Audio Pass

Document:
- [10-audio-pass.md](./10-audio-pass.md)

Status:
- `Partially done`

Done:
- [x] tower / hit / death / splash / base-hit / round feedback hooks now exist in code
- [x] audio fallback routing exists so missing dedicated clips degrade gracefully

Still needed:
- [ ] assign dedicated clips for every new sound event
- [ ] connect background music loop cleanly in gameplay scenes
- [ ] sanity-check mix levels in-editor

### Goal 11 — Final Technical Validation and Cleanup

Document:
- [11-final-technical-validation-and-cleanup.md](./11-final-technical-validation-and-cleanup.md)

Status:
- `Mostly done`

Done:
- [x] Unity 6.4 upgrade/retarget complete
- [x] URP renderer issue fixed
- [x] Safe Mode package/compiler blockers fixed
- [x] input migration completed
- [x] obsolete API cleanup pass started
- [x] asset sorting pass completed
- [x] git repo initialized and revision commits started
- [x] Unity batchmode validation now completes successfully under 6000.4.10f1
- [x] core gameplay loop pieces compile/import together after the latest goal work
- [x] menu scene no longer points its default play target at a missing gameplay scene

Still needed:
- [ ] final warning cleanup pass from a fresh editor-opened interactive session
- [ ] deeper prefab / scene audit after more playtesting

### Goal 12 — Optional First Playable Vertical Slice

Document:
- [12-optional-first-playable-vertical-slice.md](./12-optional-first-playable-vertical-slice.md)

Status:
- `In progress`

Done:
- [x] one readable harbor-style gameplay map exists
- [x] waypoint-driven water route exists
- [x] open land placement exists
- [x] spawn/end markers exist
- [x] four tower archetypes exist in gameplay data
- [x] first enemy ladder exists
- [x] first five balanced wave assets exist
- [x] HUD / money spent / lives lost flow exists

Still needed first:
- [ ] stronger art pass
- [ ] dedicated audio clip pass
- [ ] broader playtest and balance validation across all three level scaffolds

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
3. Do an in-editor playtest pass across easy / medium / hard scene scaffolds and retune waves/tower feel from real gameplay.
4. Land the dedicated placeholder art and audio assets so Goals 9 and 10 move from hooks to presentation.
