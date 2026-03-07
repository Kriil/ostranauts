# TESTING_CHECKLIST

## Safe Setup
1. Back up save files before enabling new FFU core/module versions.
2. Verify BepInEx + MonoMod loader are installed and log output is enabled.
3. Confirm all expected FFU `.mm.dll` assemblies are in `BepInEx/monomod`.

## Boot Validation
1. Start game to main menu and exit.
2. Check `BepInEx/LogOutput.log` for FFU init/config lines and no fatal exceptions.
3. Confirm FFU config file is generated and readable.

## Data Load Validation
1. Validate mod load order is respected and no duplicate-ID explosions are reported.
2. Test one FFU-enhanced data mod that uses `strReference` and confirm merged fields apply.
3. Test one `removeIds` use-case and verify ID is removed and reusable.

## Runtime Feature Validation
1. Console module: verify `getcond *`, `findcondcos`, `triggertest`, `triggerinfo`, `openinventory`, `repairship` commands.
2. Extended module: verify at least one extended condtrigger/math op and one interaction verbosity case.
3. Quality module: verify inventory sorting/transfer and quickbar behavior.
4. Super module: verify calc-rate or skill/trait behavior toggles.

## Save Compatibility Validation
1. Load an existing save with mapped changes enabled.
2. Verify expected slotted-item remaps and missing-condition sync occurred.
3. Save to a new slot.
4. If using temporary patch mods, disable them after migration and retest load.

## Regression Checks
1. New game start and tutorial progression.
2. Inventory open/close and transfer actions.
3. Typical work interactions (install/repair/uninstall).
4. Ship maneuvering and autopilot basics.
5. Crash-free return to main menu and reload.
