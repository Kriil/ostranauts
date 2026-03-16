# Blueprints

Adds a Blueprint workflow for capture-and-place construction layouts.

Current status: the mod can be started from the `blueprint` console command or from a `BLUE` button appended to the PDA `Orders` actions list under `Crew Orders & Building`.

Requirements: BepInEx and Harmony in the local Ostranauts install used by this workspace.

Saved blueprints:
- Default location: `Ostranauts_Data/Mods/Blueprints/saved_blueprints`
- Generated files avoid double `blueprint_` prefixes when the blueprint name already starts with that prefix.

Troubleshooting:
- Look for `Applied Harmony patches for Blueprints.`
- Look for `Inserted Blueprint PDA action button after the vanilla jobs actions.`
- Look for `Blueprint PDA button clicked.`
- Look for `Blueprint selection mode started.`
- Look for `Blueprint placement mode-switch applied:` when placing overlay-backed blueprint parts that used to show the purple generic placeholder.

Technical notes:
- The PDA button is injected with a postfix on `GUIPDA.ShowJobPaintUI("actions")`.
- The injected button clones the vanilla `GUIJobItem` prefab so it inherits the existing layout and icon treatment.
- The action uses `GUIActionBlueprint.png` for both the PDA button and the in-world cursor overlay.
- Placement now resolves `JsonCOOverlay.GetModeSwitch(...)` for overlay-backed installables before calling `CrewSim.InstallStart(...)`, matching the vanilla/radial install flow used by `ConstructionTweaks`.
- Blueprint capture preserves the source installed `strCODef` for each part so shared installables such as the air pump family can still pick the correct visual variant during placement.
