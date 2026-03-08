# GAME_DATA_CHANGES

## Data Files Changed by This Mod
1. `mod_data/mod_info.json`
   - Declares mod package metadata and compatibility target.
2. `mod_data/data/conditions/cds_room_effects.json`
   - Adds condition definitions used as room/ship bonus carriers.

## Custom Conditions Introduced
- `StatShipEngineeringWorkBonus`
- `StatRoomHeatSpeedBonus`
- `StatRoomCoolSpeedBonus`
- `StatRoomSleepEfficiencyBonus`
- `StatRoomBathroomSpeedBonus`
- `StatRoomReactorThrusterBonus`
- `StatRoomReactorIntakeBonus`
- `StatRoomTowingSecureSpeedBonus`
- `StatRoomAirlockScrubberSpeedBonus`
- `StatRoomWellnessFitnessBonus`
- `StatRoomWellnessStrengthBonus`
- `StatRoomRecreationPositiveBonus`
- `StatRoomRecreationNegativeReduction`
- `StatRoomGalleySatietyBonus`
- `StatRoomPassengerRelaxBonus`

## Gameplay Systems Influenced by Those Conditions
- Work-rate modifiers (`Interaction.CalcRate` hook).
- Interaction outcome magnitudes (`ApplyLootCT`, `ApplyLootConds` hooks).
- Interaction duration for specific action families (bathroom/towing path).
- Heater/cooler and gas scrubber throughput.
- Ship thrust and RCS intake usage.
- Sleep, recreation, wellness, and dining outcome tuning.
