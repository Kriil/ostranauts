# ENTRY_POINTS

## Runtime Entry Points
1. `FFU_BR/FFU_BR_Patch_BetterModAPI.cs`
   - `patch_DataHandler.Init()` (`[MonoModReplace]`): primary load/bootstrap hook for FFU core behavior.
2. `FFU_BR/FFU_BR_Patch_BetterModAPI.cs`
   - `SyncLoadMods(...)`, `LoadMod(...)`, `LoadModJsons(...)`, `JsonToData(...)` paths (patched/overridden flow): core FFU data merge behavior.
3. `FFU_BR/FFU_BR_Patch_ChangesMap.cs`
   - `patch_Ship.InitShip(...)`: applies save/template migration map commands at ship initialization.

## Module Entry Points
1. `FFU_BR_Console/*`
   - Console resolver patches (`KeywordGetCond`, command resolvers, inventory open, trigger test/info, repair ship).
2. `FFU_BR_Extended/*`
   - CondTrigger, Interaction, Loot, Sensor, Heater, Slot/Container, GUI inventory patch entry points.
3. `FFU_BR_Fixes/*`
   - Statusbar update hook(s).
4. `FFU_BR_Quality/*`
   - GUI and QoL behavior hooks (inventory layout, quick move, quickbar, alt temp display, HUD).
5. `FFU_BR_Super/*`
   - Chargen skill/trait and Interaction calc-rate hooks.

## Data-Driven Entry Points Consumed by FFU
1. `mod_info.json` extensions
   - `removeIds`
   - `changesMap`
2. Extended JSON fields
   - `strReference` (many DTO types)
   - module-specific extended fields (for example interaction/condtrigger additions in Extended module).

## Load Order Sensitive Points
1. `loading_order.json` in the game Mods folder determines merge order.
2. FFU core must be active before FFU-enhanced data mods expect `strReference`, partial overwrite, and `changesMap` semantics.
3. Save patch helper mods (`*_Patch`) should run only for migration windows, then be disabled per project guidance.
