# Blueprints

Adds a Blueprint workflow for capture-and-place construction layouts.

Current status: the mod can be started from the `blueprint` console command or from a `BLUE` button appended to the PDA `Orders` actions list under `Crew Orders & Building`.
The PDA `Orders` filter section also includes a blueprint file-name box plus a `Select Blueprint` button for loading a saved blueprint JSON directly into placement mode.

Requirements: BepInEx and Harmony in the local Ostranauts install used by this workspace.

Saved blueprints:
- Default location: `Ostranauts_Data/Mods/Blueprints/saved_blueprints`
- Generated files avoid double `blueprint_` prefixes when the blueprint name already starts with that prefix.
- Captured and newly saved blueprint files now persist the source installed `strCODef` per part so file-based reloads can preserve overlay-backed installable variants.

Troubleshooting:
- Look for `Applied Harmony patches for Blueprints.`
- Look for `Inserted Blueprint PDA action button after the vanilla jobs actions.`
- Look for `Inserted Blueprint selector UI under PDA job filters.`
- Look for `Blueprint PDA button clicked.`
- Look for `Blueprint selection mode started.`
- Look for `Blueprint file selection cancelled.` when testing the no-op cancel path from the file picker.
- Look for `Loaded blueprint from file ...` when testing blueprint placement from an existing JSON file.
- Look for `Blueprint mode cancelled via Escape.` or `Blueprint mode cancelled via right-click.` when confirming the mode exits through the same cancel inputs players use for other order modes.
- Look for `Blueprint capture serialized ... have non-zero saved rotation.` when checking whether item orientation made it into the saved blueprint payload.
- Look for `Blueprint placeholder rotation copied from cursor:`, `Blueprint placement handoff`, and `Blueprint final ModeSwitch rotation applied:` when checking whether queued install placeholders inherited the ghost cursor's facing instead of the installable's default orientation.
- Look for `Blueprint placement mode-switch applied:` when placing overlay-backed blueprint parts that used to show the purple generic placeholder.

Technical notes:
- The PDA button is injected with a postfix on `GUIPDA.ShowJobPaintUI("actions")`.
- The injected button clones the vanilla `GUIJobItem` prefab so it inherits the existing layout and icon treatment.
- The selector UI under `Toggle Affected Item Type(s)` is also built at runtime from cloned `GUIJobItem` instances, so it survives PDA rebuilds without needing a custom prefab asset.
- The action uses `GUIActionBlueprint.png` for both the PDA button and the in-world cursor overlay.
- Selecting a blueprint JSON uses a Windows `OpenFileDialog` pointed at the configured blueprint save directory; cancelling the dialog leaves the current game state untouched.
- Because the mod suppresses the vanilla `CrewSim.MouseHandler` while blueprint mode is active, it must explicitly mirror the normal cancel inputs (`Esc` and right-click) to exit the mode like other order tools.
- Blueprints persist their own item payload with explicit `fRotation` fields instead of relying on nested game `JsonItem` serialization, so saved layouts retain per-part orientation.
- Blueprint placement also copies the chosen rotation onto the install interaction target and the generated placeholder, because some installables otherwise revert to their default facing when the job is queued.
- Blueprint-created placeholders now also reapply their saved rotation during the worker-side `CondOwner.ModeSwitch(...)` that turns a finished placeholder into the real installed object.
- Blueprint rotation applies clockwise to both the overall footprint and each part's own facing; the per-part angle uses the same clockwise step as the offset transform.
- Placement now resolves `JsonCOOverlay.GetModeSwitch(...)` for overlay-backed installables before calling `CrewSim.InstallStart(...)`, matching the vanilla/radial install flow used by `ConstructionTweaks`.
- Blueprint capture preserves the source installed `strCODef` for each part so shared installables such as the air pump family can still pick the correct visual variant during placement.
