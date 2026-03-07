# GAME_DATA_CHANGES

## Summary by Mod
1. `Minor_Fixes_Plus`
   - `installables`, `loot` tweaks/fixes.
2. `EVA_Suits_Rework`
   - broad EVA pipeline changes: `conditions`, `conditions_simple`, `condowners`, `condrules`, `condtrigs`, `gasrespires`, `installables`, `interactions`, `items`, `loot`, `powerinfos`, `slots`, `slot_effects`, plus paperdoll images.
3. `EVA_Suits_Rework_Patch`
   - save migration helper metadata.
4. `Storage_Rebalance`
   - storage behavior/content: `conditions_simple`, `condowners`, `condtrigs`, `cooverlays`, `installables`, `items`, image replacements.
5. `Storage_Rebalance_Patch`
   - save migration helper metadata.
6. `Space_Engineering`
   - new module system data across `conditions`, `condowners`, `condtrigs`, `guipropmaps`, `installables`, `interactions`, `items`, `lights`, `loot`, `powerinfos`, `shipspecs`, `tickers`, images.
7. `Exp_Transponders`
   - skill-based transponder workflow via `conditions`, `condtrigs`, `installables`, `interactions`, `loot`.
8. `Exp_Tow_Braces`
   - towing interaction tuning in `interactions`.
9. `Extended_License`
   - license economy flow via `condowners`, `condtrigs`, `interactions`, `ledgerdefs`, `loot`.
10. `Full_Auto_Vents`
   - vent automation via `conditions`, `conditions_simple`, `condowners`, `condtrigs`, `guipropmaps`, `interactions`, `loot`, `tickers`.
11. `Charger_Capacity`
   - charger storage stats in `condowners`.
12. `Sharp_Laser_Torch`
   - tool behavior adjustments in `condowners`.
13. `Sharp_Laser_Torch_Patch`
   - migration helper metadata.
14. `Slow_Auto_Doors`
   - door timing via `interactions`.
15. `Slow_Thermostat`
   - thermostat timing via `interactions`.
16. `Learnable_Skills`
   - progression tables via `careers`, `traitscores`.
17. `Lighter_Shadows`
   - rendering/light-facing item config via `items`.
18. `Glass_Only_EVA`
   - texture-only helmet visuals.
19. `Zero_Dev`
   - dev test content in `ai_training`, `ships`.

## Aggregate Registry Impact
Most commonly touched registries:
- `condowners`, `interactions`, `loot`, `installables`, `condtrigs`, `conditions`.

## Likely Gameplay Systems Affected
- Work action speed/flow
- EVA survivability and equipment slots
- Inventory/storage specialization
- Station/ship systems (vents, thermostats, tow braces, transponders)
- Licensing economy
- Character growth and skill accessibility
- Visual readability (textures/shadows)
