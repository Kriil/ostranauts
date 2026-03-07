# ENTRY_POINTS

## Global Entry Points
1. `loading_order.json`
   - Controls which sub-mods load and in what order.
2. `<ModFolder>/mod_info.json`
   - Declares mod metadata and is the first file FFU/vanilla loader consumes in each mod folder.

## Per-Mod Entry Points (Key)
1. `Minor_Fixes_Plus/mod_info.json`
2. `EVA_Suits_Rework/mod_info.json`
3. `EVA_Suits_Rework_Patch/mod_info.json`
4. `Storage_Rebalance/mod_info.json`
5. `Storage_Rebalance_Patch/mod_info.json`
6. `Space_Engineering/mod_info.json`
7. `Exp_Transponders/mod_info.json`
8. `Exp_Tow_Braces/mod_info.json`
9. `Extended_License/mod_info.json`
10. `Full_Auto_Vents/mod_info.json`
11. `Charger_Capacity/mod_info.json`
12. `Sharp_Laser_Torch/mod_info.json`
13. `Sharp_Laser_Torch_Patch/mod_info.json`
14. `Slow_Auto_Doors/mod_info.json`
15. `Slow_Thermostat/mod_info.json`
16. `Learnable_Skills/mod_info.json`
17. `Lighter_Shadows/mod_info.json`
18. `Glass_Only_EVA/mod_info.json`
19. `Zero_Dev/mod_info.json`

## Gameplay Data Entry Paths
- `data/conditions`, `data/conditions_simple`
- `data/condowners`, `data/condtrigs`, `data/condrules`
- `data/interactions`, `data/installables`, `data/loot`
- `data/items`, `data/slots`, `data/slot_effects`, `data/powerinfos`
- optional registry folders such as `data/ledgerdefs`, `data/guipropmaps`, `data/lights`, `data/tickers`, `data/shipspecs`, `data/careers`, `data/traitscores`

## Asset Entry Paths
- `images/**` under specific mods for sprite/paperdoll/UI replacements.

## Notes
- FFU-enabled semantics (partial overwrite/reference/mapping) are expected by many of these packs.
