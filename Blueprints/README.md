# Blueprints

Adds a Blueprint workflow for capture-and-place construction layouts.

Current status: the mod can be started from the `blueprint` console command or from a `BLUE` button appended to the PDA `Orders` actions list under `Crew Orders & Building`.

Requirements: BepInEx and Harmony in the local Ostranauts install used by this workspace.

Troubleshooting:
- Look for `Applied Harmony patches for Blueprints.`
- Look for `Registered Blueprint action image from ...`
- Look for `Inserted Blueprint PDA action button after the vanilla jobs actions.`
- Look for `Blueprint PDA button clicked.`
- Look for `Blueprint selection mode started.`

Technical notes:
- The PDA button is injected with a postfix on `GUIPDA.ShowJobPaintUI("actions")`.
- The injected button clones the vanilla `GUIJobItem` prefab so it inherits the existing layout and icon treatment.
- The action uses `GUIActionBlueprint.png` for both the PDA button and the in-world cursor overlay.
