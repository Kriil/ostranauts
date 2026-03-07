# REPO_MAP

## Overview
`FFU-BR` is a code-first Ostranauts API patch set. It is not a normal standalone Harmony plugin; it is a set of MonoMod patch assemblies that patch `Assembly-CSharp` behavior and expose FFU API features to data mods.

## Structure
- `FFU_BR/`
  - `FFU_Beyond_Reach.csproj`: core FFU build target (`Assembly-CSharp.FFU_BR.mm.dll`).
  - `FFU_BR_Defs.cs`: global config schema and runtime options.
  - `FFU_BR_Patch_BetterModAPI.cs`: replaces DataHandler loading flow; adds `removeIds`, `changesMap`, reference-copy, partial overwrite, array/object/dictionary edit commands.
  - `FFU_BR_Patch_ChangesMap.cs`: save/template migration routines for slotted COs and condition syncing.
  - `FFU_BR_Patch_DataStructs.cs`: extends many JSON DTOs with `strReference` for reference-based inheritance.
  - `FFU_BR_Patch_JsonMapper.cs`: LitJson read/write hardening and compatibility handling.
- `FFU_BR_Console/`
  - `FFU_BR_Base.cs`: exposes module version field in shared FFU API surface.
  - `FFU_BR_Patch_LoadConsole.cs`: command registration/help integration.
  - `FFU_BR_Patch_GetCondPlus.cs`: enhanced `getcond` command behavior.
  - `FFU_BR_Patch_FindByConds.cs`: `findcondcos` command.
  - `FFU_BR_Patch_DoTriggerTest.cs`: `triggertest` command.
  - `FFU_BR_Patch_LogTriggerInfo.cs`: `triggerinfo` command.
  - `FFU_BR_Patch_OpenTargInv.cs`: `openinventory` command + inventory UI patch.
  - `FFU_BR_Patch_RepairCurShip.cs`: `repairship` command.
  - `FFU_BR_Patch_ConsolePlus.cs`: console UI behavior and text handling tweaks.
- `FFU_BR_Extended/`
  - `FFU_BR_Base.cs`: shared config fields for this module.
  - `FFU_BR_Patch_CondTrigPlus.cs`: extended CondTrigger logic (`nMaxDepth`, math ops).
  - `FFU_BR_Patch_HeaterPlus.cs`: heater behavior extension (`StatEmittedTemp` support).
  - `FFU_BR_Patch_InteractionsPlus.cs`: interaction verbosity and room-lookup logging features.
  - `FFU_BR_Patch_InvOpenSorted.cs`: ordered inventory window spawning/sorting.
  - `FFU_BR_Patch_InvRecurseSafe.cs`: recursive slot/container fit safeguards.
  - `FFU_BR_Patch_InvSlotEffects.cs`: inventory/slot effect propagation handling.
  - `FFU_BR_Patch_LootRandom.cs`: random/loot behavior extensions.
  - `FFU_BR_Patch_LootSafeParse.cs`: safer loot def parsing.
  - `FFU_BR_Patch_SensorPlus.cs`: sensor run/setdata extensions.
  - `FFU_BR_Patch_ShipSpecPlus.cs`: ship-spec matching extension.
- `FFU_BR_Fixes/`
  - `FFU_BR_Base.cs`
  - `FFU_BR_Patch_StatusbarModule.cs`: status bar / power observability style fixups.
- `FFU_BR_Quality/`
  - `FFU_BR_Base.cs`
  - `FFU_BR_Patch_HoldBraceRCS.cs`: tow-brace + station-keeping quality behavior.
  - `FFU_BR_Patch_InfoAltTemp.cs`: alternate temperature display.
  - `FFU_BR_Patch_InvOrganized.cs`: inventory window organization layout.
  - `FFU_BR_Patch_QuickBarPinning.cs`: quickbar pinning behavior.
  - `FFU_BR_Patch_QuickMovePlus.cs`: shift-click transfer behavior.
  - `FFU_BR_Patch_SuitHudPlus.cs`: suit HUD and warning behavior.
- `FFU_BR_Super/`
  - `FFU_BR_Base.cs`
  - `FFU_BR_Patch_SuperCalcRate.cs`: super-character multiplier integration into work-rate math.
  - `FFU_BR_Patch_SuperFreeSkill.cs`: free skill/trait mode and chargeden hooks.
- Root build/project files
  - `FFU_Beyond_Reach.sln`, `Directory.Build.props`, module `.csproj` files, `README.md`.

## Practical Architecture (Modder Terms)
- Core FFU module replaces parts of `DataHandler` loading to:
  - support partial JSON edits instead of full file overwrite,
  - support `strReference` cloning/inheritance,
  - support selective ID removal (`removeIds`),
  - support persistent save/template migration (`changesMap`).
- Additional modules are feature packs that patch specific game systems.
- FFU-BR-Data mods depend on this behavior to avoid hard overwrites and to safely patch existing saves/templates.

## Type Classification
- Primary: hybrid code patch mod (BepInEx + MonoMod patch assemblies).
- Not primarily a pure Harmony plugin.
- Intended to power FFU-enhanced data mods.

## Engine Systems Touched (Likely)
- `DataHandler` load/init pipeline and JSON parsing.
- CondOwner / CondTrigger / Interaction runtime checks and logging.
- Loot parsing/application.
- Inventory UI sorting, transfer flow, and slot effect propagation.
- Sensor, heater, ship-spec matching, status UI, quickbar, and character-gen paths.

## Dependencies
- BepInEx 5.x
- MonoMod Loader
- Ostranauts `Assembly-CSharp` (and Unity assemblies)
- LitJson behavior (patched in core)

## Flags (Broken/Obsolete/Suspicious)
- Large number of decompiler reference comment blocks are kept inline; not broken, but noisy and higher maintenance.
- Core patch complexity is high (`FFU_BR_Patch_BetterModAPI.cs` and `FFU_BR_Patch_ChangesMap.cs`), so game-version drift is a likely break vector.
- Module `FFU_BR_Base.cs` files duplicate the same shared def stubs by design for MonoMod partial linking; this looks redundant but is likely intentional.
