# REPO_MAP

## Overview
`FFU-BR-Data` is a collection of JSON/image mods. Most entries are data mods that rely on FFU-BR API behavior (partial overwrite, `strReference`, `changesMap`, `removeIds`).

## Root Files
- `loading_order.json`: canonical mod load order for this pack.
- `README.md`: install order, save patch workflow, and feature descriptions.
- `Update_References.bat`: utility script for reference maintenance.
- `LICENSE`, workspace metadata.

## Mod Folders (Important)
- `Minor_Fixes_Plus/`: baseline required fix data set for FFU ecosystem.
- `EVA_Suits_Rework/` + `EVA_Suits_Rework_Patch/`: suit overhaul and migration patch.
- `Storage_Rebalance/` + `Storage_Rebalance_Patch/`: storage class/slot rebalance and migration patch.
- `Space_Engineering/`: new module content (maintenance pump and related systems).
- `Exp_Transponders/`, `Exp_Tow_Braces/`: expertise/skill-based behavior packs.
- `Extended_License/`: license flow and ledger updates.
- `Full_Auto_Vents/`: automated vent behavior data.
- `Charger_Capacity/`, `Sharp_Laser_Torch/`, `Slow_Auto_Doors/`, `Slow_Thermostat/`, `Learnable_Skills/`, `Lighter_Shadows/`, `Glass_Only_EVA/`: focused small mods.
- `Zero_Dev/`: development test data.
- `Zero_Ref/`: broad reference dump of base game-style data schemas (documentation/source reference role).

## Practical Architecture (Modder Terms)
- Each subfolder is a standalone mod package with `mod_info.json` and optional `data/` + `images/`.
- `loading_order.json` determines deterministic merge behavior.
- Patch folders (`*_Patch`) are temporary migration helpers for save compatibility workflows.
- `Zero_Ref` acts as a local schema/catalog reference for many registries and is likely not intended for normal load.

## Type Classification
- Primary: FFU-enhanced data mod collection.
- Secondary: some pure data mods (simple value tweaks, texture replacements).
- No compiled plugin entry point in this repo folder.

## Common Important File Types
- `mod_info.json`: mod identity/version + FFU metadata hooks.
- `data/*.json`: gameplay definitions (conditions, condtrigs, interactions, condowners, installables, loot, etc.).
- `images/*.png`: paperdoll/UI/sprite replacements.

## Flags (Broken/Obsolete/Suspicious)
- Multiple mods target game versions around `0.14.3.x` to `0.14.5.x`; version drift against current build is a likely compatibility risk.
- `*_Patch` mods are intended for migration passes; leaving them permanently enabled may produce repeated remap/sync side effects.
- `Zero_Ref` contains huge reference content and many test/dev-like files; loading it in normal play is likely unsafe/noisy.
