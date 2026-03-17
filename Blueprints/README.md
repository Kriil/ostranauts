# Blueprints

Adds a Blueprint workflow for capture-and-place construction layouts.

## Usage: 
- Click the `BLUE` button appended to the PDA `Orders` actions list under `Crew Orders & Building`.  Then select the area to be uninstalled.  Next move the blueprint shadow to the area you want to reinstall everything.  The area must be completely empty and buildable (i.e. walkable). If you find there isn't enough room to build, you can install a floor some distance away from your ship to expand the buildable area (this is vanilla behavior). You can easily check your buildable area by using the `Zones` PDA app.
- If blueprint saving is toggled on (off by default), the uninstall selection will be saved as a JSON file.  The PDA `Orders` filter section will include a blueprint file-name box plus a `Select Blueprint` button for loading a saved blueprint JSON directly into placement mode. Edit BepInEx\Blueprints.cfg to turn this on.  During testing, it didn't seem particularly useful because you still need all of the items avaialable, which is why it is disabled by default.

## Limitations:
- The installed blueprint requires an area that has no installed items on it and cannot cross between ships.  So no selecting an existing reactor setup and uninstalling/reinstalling on existing flooring, for example.  That will be the next update.
- The blueprint shadow uses the install placeholder system from the vanilla game so it is affected by lighting. You may need to move your character to the desired install area to properly see the blueprint shadow in default view. Its recommended to use another view that doesn't have FOV limitations suchas the price or mass view. 
- If, say, only the equipment filter is toggled when selecting an area to blueprint, then that equipment can be placed down in a clear area, even if the equipment normally requires a wall to be placed.  This will be fixed in the next update.


## Requirements: 
BepInEx and Harmony in the local Ostranauts install used by this workspace. See README.md in repo root for specific installation instructions

## Recommended mods:
Construction Tweaks 
- Enhances drag-select so that it highlights which tiles will be affected by commands like Repair, Haul and Blueprint.
- Lets the player walk through uninstalled wall placeholders, which normally block movement.

## Saved blueprints:
- Default location: `Ostranauts_Data/Mods/Blueprints/saved_blueprints`
- Saving is controlled by `Storage.EnableBlueprintSaving` in the BepInEx config. Default: `false`
- Generated files avoid double `blueprint_` prefixes when the blueprint name already starts with that prefix.
- Captured and newly saved blueprint files now persist the source installed `strCODef` per part so file-based reloads can preserve overlay-backed installable variants.

## Troubleshooting:
- Look for `Applied Harmony patches for Blueprints.`
- Look for `Inserted Blueprint PDA action button after the vanilla jobs actions.`
- Look for `Inserted Blueprint selector UI under PDA job filters.`
- Look for `Blueprint PDA button clicked.`
- Look for `Blueprint selection mode started.`
- Look for `Blueprint file selection cancelled.` when testing the no-op cancel path from the file picker.
- Look for `Blueprint capture completed without writing a JSON file because blueprint saving is disabled.` when confirming capture-only mode.
- Look for `Loaded blueprint from file ...` when testing blueprint placement from an existing JSON file.
- Look for `Blueprint mode cancelled via Escape.` or `Blueprint mode cancelled via right-click.` when confirming the mode exits through the same cancel inputs players use for other order modes.
- Look for `Blueprint capture serialized ... have non-zero saved rotation.` when checking whether item orientation made it into the saved blueprint payload.
- Look for `Blueprint placeholder rotation copied from cursor:`, `Blueprint placement handoff`, and `Blueprint final ModeSwitch rotation applied:` when checking whether queued install placeholders inherited the ghost cursor's facing instead of the installable's default orientation.
- Look for `Blueprint placement mode-switch applied:` when placing overlay-backed blueprint parts that used to show the purple generic placeholder.

## Technical notes:
- The PDA button is injected with a postfix on `GUIPDA.ShowJobPaintUI("actions")`.
- The injected button clones the vanilla `GUIJobItem` prefab so it inherits the existing layout and icon treatment.
- The selector UI under `Toggle Affected Item Type(s)` is also built at runtime from cloned `GUIJobItem` instances, so it survives PDA rebuilds without needing a custom prefab asset.
- The file selector UI is only shown when `Storage.EnableBlueprintSaving` is enabled; with the default `false` setting, Blueprint capture still works but no JSON file is written.
- The action uses `GUIActionBlueprint.png` for both the PDA button and the in-world cursor overlay.
- Selecting a blueprint JSON uses a Windows `OpenFileDialog` pointed at the configured blueprint save directory; cancelling the dialog leaves the current game state untouched.
- Because the mod suppresses the vanilla `CrewSim.MouseHandler` while blueprint mode is active, it must explicitly mirror the normal cancel inputs (`Esc` and right-click) to exit the mode like other order tools.
- Blueprints persist their own item payload with explicit `fRotation` fields instead of relying on nested game `JsonItem` serialization, so saved layouts retain per-part orientation.
- Blueprint placement also copies the chosen rotation onto the install interaction target and the generated placeholder, because some installables otherwise revert to their default facing when the job is queued.
- Blueprint-created placeholders now also reapply their saved rotation during the worker-side `CondOwner.ModeSwitch(...)` that turns a finished placeholder into the real installed object.
- Blueprint rotation applies clockwise to both the overall footprint and each part's own facing; the per-part angle uses the same clockwise step as the offset transform.
- Placement now resolves `JsonCOOverlay.GetModeSwitch(...)` for overlay-backed installables before calling `CrewSim.InstallStart(...)`, matching the vanilla/radial install flow used by `ConstructionTweaks`.
- Blueprint capture preserves the source installed `strCODef` for each part so shared installables such as the air pump family can still pick the correct visual variant during placement.
