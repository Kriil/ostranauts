# ENTRY_POINTS

## Primary Entry Point
1. `Plugin.cs`
   - `Awake()`:
     - registers all config knobs,
     - creates Harmony instance,
     - applies all patches.
   - `OnDestroy()`:
     - unpatches mod hooks.

## Harmony Patch Entry Points
1. `Patch_Room_CreateRoomSpecs.cs` -> `Room.CreateRoomSpecs` (Postfix)
2. `Patch_Room_AddToRoom.cs` -> `Room.AddToRoom` (Postfix)
3. `Patch_Room_RemoveFromRoom.cs` -> `Room.RemoveFromRoom` (Postfix)
4. `Patch_CondOwner_QueueInteraction.cs` -> `CondOwner.QueueInteraction` (Postfix)
5. `Patch_Interaction_CalcRate.cs` -> `Interaction.CalcRate` (Transpiler + hook)
6. `Patch_Interaction_ApplyLoot.cs` -> `Interaction.ApplyLootCT` / `Interaction.ApplyLootConds` (Prefix replacements)
7. `Patch_Heater_Heat.cs` -> `Heater.Heat` (Prefix)
8. `Patch_GasPump_Respire2.cs` -> `GasPump.Respire2` (Prefix)

## Data Entry Points
1. `mod_data/mod_info.json`
   - mod metadata exposed to Ostranauts mod loader.
2. `mod_data/data/conditions/cds_room_effects.json`
   - custom condition definitions consumed by plugin-set condition amounts.

## Utility/Dispatch Paths
1. `RoomEffectUtils.RefreshRoomBonuses(...)`
   - central room spec dispatch to bonus handlers.
2. `RoomEffectUtils.RefreshShipWideBonuses(...)`
   - central ship-wide engineering bonus recompute.
3. `RoomEffectUtils.ModifyInteractionTriggerAmount(...)` and `.ModifyInteractionCondAmount(...)`
   - central interaction amount modifier chain.
