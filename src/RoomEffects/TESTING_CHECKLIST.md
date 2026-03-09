# TESTING_CHECKLIST

## Build/Install Safety
1. Build `RoomEffects` against the same game install used for playtesting.
2. Place DLL in `BepInEx/plugins` and keep `mod_data` in game `Mods` path.
3. Back up saves before first run.

## Startup Validation
1. Launch to main menu and confirm plugin log line appears.
2. Confirm no Harmony patch failure warning for `Interaction.CalcRate` transpiler.
3. Confirm custom conditions from `cds_room_effects.json` load without JSON errors.

## Core Functional Tests
1. Room detection
   - Add/remove installed components to force `Room.CreateRoomSpecs` updates.
   - Verify bonus state changes when room type changes.
2. Engineering
   - Verify work interactions on player ship get expected speed change.
3. Towing/Bathroom
   - Queue relevant interactions and verify duration reductions.
4. Wellness/Recreation/Quarters/Galley/Passenger
   - Verify affected interaction outcomes are scaled as expected.

## Regression Checks
1. Non-player ships should remain unaffected by most bonuses.
2. Normal interactions unrelated to room effects should remain unchanged.
3. No ticker desync after duration adjustments on queued interactions.
4. No null-reference exceptions during interaction logging-heavy paths.

## Performance/Log Noise Check
1. Run a longer session and inspect log size/spam from room-effect messages.
2. If logs are too noisy, consider reducing debug logging in follow-up patch.
