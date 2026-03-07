# TESTING_CHECKLIST

## Pre-Test Safety
1. Back up saves.
2. Start from a known-clean `loading_order.json` and add mods incrementally.
3. Keep migration patch mods (`*_Patch`) disabled unless doing explicit save migration.

## Load Validation
1. Launch to main menu, exit, inspect log for JSON/ID errors.
2. Confirm each enabled mod from `loading_order.json` is reported as loaded.
3. Check that no accidental `Zero_Ref`/dev reference data is enabled in normal run.

## Functional Spot Checks
1. EVA flow: suit equip, O2/power, slot behavior, and related interactions.
2. Storage flow: open/close, allowed item classes, installable-vs-portable behavior.
3. Space engineering flow: place/use new module(s), power, interaction loops.
4. Expertise mods: transponder and tow-brace action timing/success behavior.
5. QoL minors: auto-door timing, thermostat pacing, charger capacity, lighting/shadow visuals.
6. Economy/license flow: buy/use/sell license behavior where relevant.

## Migration Patch Workflow
1. Enable target `*_Patch` right after its base mod.
2. Load old save, save as new slot.
3. Disable patch mod.
4. Reload new save and verify no duplicate/missing CO issues.

## Regression Baseline
1. New game start.
2. Existing save load.
3. Dock/undock + basic ship operations.
4. Inventory and interaction UI stability.
5. No recurring error spam in logs during normal play.
