# Ostranauts Decompiled Notes

This is a working map of the decompiled `Assembly-CSharp` export.
It is based on comment pass coverage in the current workspace, not a full re-audit of every class.
Where decompilation leaves gaps, notes are marked with "Likely:" or "Unclear:".

## Core Startup Flow

Likely startup path:

1. `MainMenu.Init()` or `CrewSim`/`Info` calls `DataHandler.Init()`.
2. `DataHandler.Init()` clears and recreates the major registries, reads mod ordering, and queues JSON load work.
3. `Ostranauts.Core.LoadManager` subscribes to `DataHandler.InitComplete`, starts loader threads via `BeginDataHanderLoadThreads()`, then waits in `AfterLoadThreadsFinish()`.
4. Background load parses JSON/text into the `dict*` registries.
5. `LoadManager.AfterLoadThreadsFinish()` runs `DataHandler.PostModLoadMainThread()`.
6. `PostModLoadMainThread()` finishes main-thread-only setup:
   `CondTrigger.PostInit()`, CO/data merges, image/material/runtime caches, lookup-table finalization.
7. `DataHandler.LoadComplete` fires.
8. UI/controllers that deferred on data load (`Info`, menus, likely other UI) finish their own init.

Data flow summary:

- `DataHandler.Init()` builds empty registries.
- `LoadMod()` / JSON parse populate raw DTO dictionaries.
- `PostModLoadMainThread()` converts or links DTOs into runtime-ready structures.
- Runtime systems (`StarSystem`, `ShipSitu`, `Interaction`, `CondOwner`, UI) consume those registries.

## Mod And Data Loading

Confirmed data path rules:

- Base content lives under `Ostranauts_Data/StreamingAssets/`.
- Core JSON lives under `StreamingAssets/data/`.
- Mods are layered after core from `Ostranauts_Data/Mods/`.
- JSON data mods in `Ostranauts_Data/Mods/` are separate from BepInEx code mods in `BepInEx/plugins/`.
- `loading_order.json` lives in `Ostranauts_Data/Mods/` for this build and controls mod order.
- `JsonUserSettings.strPathMods` defaults to `.../Mods/loading_order.json`, and
  `GUIOptions.TryOpenModFolder()` creates the file there if it is missing.
- Later-loaded content can override earlier content by shared `strName` keys.

Likely load order:

1. Core `StreamingAssets`
2. Mod folders in declared order
3. Per-registry merge/override into `DataHandler.dict*`

`DataHandler.aModPaths` is the ordered mod search path list used for file/image lookup.

## Key Subsystems

### DataHandler

`DataHandler` is the central registry and loader.

High-value registries confirmed in code:

- Items and placement:
  `dictItemDefs`, `dictSlots`, `dictSlotEffects`, `dictInstallables`, `dictInstallables2`
- Conditions and CondOwners:
  `dictConds`, `dictCTs`, `dictCondRules`, `dictCOs`, `dictCOSaves`, `dictCOOverlays`, `dictDataCOs`, `dictDataCoCollections`
- Interactions and jobs:
  `dictInteractions`, `dictIAOverrides`, `dictLoot`, `dictJobs`, `dictJobitems`, `dictPledges`
- World and ships:
  `dictShips`, `dictShipSpecs`, `dictStarSystems`, `dictTransit`, `dictParallax`, `dictChargeProfiles`
- Character generation:
  `dictHomeworlds`, `dictCareers`, `dictLifeEvents`, `dictPersonSpecs`
- UI and text:
  `dictStrings`, `dictAds`, `dictHeadlines`, `dictInfoNodes`, `dictPDAAppIcons`, `dictComputerEntries`, `dictManPages`
- Audio and visuals:
  `dictAudioEmitters`, `dictMusic`, `dictMusicTags`, `dictLights`, `dictColors`, `dictJsonColors`
- Economy and social:
  `dictLedgerDefs`, `dictMarketConfigs`, `dictCargoSpecs`, `dictCrimes`
- Plot/tutorial:
  `dictPlots`, `dictPlotBeats`, `dictPlotManager`, `dictPlotBeatOverrides`, `dictTips`

Likely pattern:

- `Json*` DTOs are loaded first.
- Some DTOs are promoted into runtime classes:
  `JsonCond` -> `Condition`
  `JsonInteraction` -> `Interaction`
  `JsonCondOwner` -> `CondOwner`
  `JsonBodyOrbitSave` / `JsonSpawnBodyOrbit` -> `BodyOrbit`
  `JsonShipSitu` -> `ShipSitu`

### Conditions / CondOwners / Overlays

Terminology confirmed by code usage:

- `CondOwner` is the runtime object that can hold Conditions.
  Likely: items, crew, ships, rooms, and installables all derive from or embed this.
- `Condition` is the live runtime state.
- `JsonCond` is the definition/template.
- `CondTrigger` is the conditional rule/test/effect object.
- `CondRule` is the threshold/rule layer built on top of conditions/triggers.
- `COOverlay` / `JsonCOOverlay` are display-state overlays applied to CondOwners.

Likely data relationship:

- Base CondOwner definition from `data/condowners`
- Status rules from condition/trigger/rule data
- Visual presentation from `data/cooverlays`
- Merged runtime presentation via `DataHandler.BuildDataCO()` / `GetDataCO()`

### Interactions

`Interaction` is the live action object used by characters, jobs, ship messages, factions, and pledges.

Confirmed interaction-related pieces:

- `JsonInteraction` is the template
- `JsonInteractionSave` is the save payload
- `InteractionObjectTracker` pools and reuses live `Interaction` instances
- Jobs and pledges materialize interactions from template ids at runtime
- `JsonShipMessage` embeds a saved interaction payload for ship comms

Likely data sources:

- `data/interactions`
- `data/interaction_overrides`
- `data/loot`
- `data/condtrigs`

### Ship / Navigation / Orbit

The navigation stack now maps cleanly:

- `JsonShipSitu` stores ship motion state in saves
- `JsonNavData` stores plotted-path/autopilot state
- `Ostranauts.ShipGUIs.Utilities.NavData` is the live plotted-path runtime object
- `ShipSitu` is the live motion integrator
- `JsonBodyOrbitSave` stores live orbit state
- `JsonSpawnBodyOrbit` stores generated body templates
- `JsonAtmosphere` stores gas/pressure layers on bodies
- `BodyOrbit` is the live orbital model
- `StarSystem` owns body orbits, stations, derelict spawns, factions, and ship messages

Likely runtime loop:

1. `StarSystem` updates body positions by epoch
2. `ShipSitu.TimeAdvance()` advances ship kinematics
3. If nav data is present, `NavData.TimeAdvance()` can override/interpolate planned motion
4. If body/orbit locked, motion follows `BodyOrbit`
5. If free flight, acceleration is integrated directly
6. `GUIOrbitDraw`, `GUIDockSys`, and nav/autopilot systems read the resulting state

### Companies / Shifts / Ledger

Company/economy cluster:

- `JsonCompany` holds roster and company-wide defaults
- `JsonCompanyRules` holds per-member schedule/duty/permission rules
- `JsonShift` defines the schedule slots (`Free`, `Sleep`, `Work`)
- `JsonLedgerDef` defines recurring/scripted money events
- `JsonLedgerLI` stores concrete ledger entries

Likely uses:

- crew scheduling
- payroll
- shore leave / restore / airlock permissions
- salary, fees, and contract payouts

### Save / Load

Confirmed save UI and manager path:

- `GUISaveLoadBase` is the shared save/load panel base
- `GUILoadMenu` lists saves and save-folder path controls
- `GUISaveMenu` creates and overwrites save slots
- `LoadManager` owns save paths, autosaves, archive extraction, and save-info refresh events

Common save payloads already covered:

- `JsonGameSave`
- `JsonStarSystemSave`
- `JsonShipSave`-adjacent ship payloads
- `JsonJobSave`
- `JsonPledgeSave`
- `JsonInteractionSave`
- `JsonCondOwnerSave`
- `JsonAIShipManagerSave`
- `JsonMarketSave`
- `JsonPlotSave`

## JSON Folder To Registry Map

This is partly confirmed from code and partly inferred from naming.

Confirmed or strongly likely:

- `data/items` -> `dictItemDefs`
- `data/slots` -> `dictSlots`
- `data/installables` -> `dictInstallables` / `dictInstallables2`
- `data/interactions` -> `dictInteractions`
- `data/interaction_overrides` -> `dictIAOverrides`
- `data/loot` -> `dictLoot`
- `data/condowners` -> `dictCOs`
- `data/cooverlays` -> `dictCOOverlays`
- `data/conditions` -> `dictConds`
- `data/condtrigs` -> `dictCTs`
- `data/condrules` or similarly named rule data -> `dictCondRules`
- `data/ships` -> `dictShips`
- `data/rooms` -> `dictRoomSpecsTemp` -> `dictRoomSpec`
- `data/careers` -> `dictCareers`
- `data/jobs` -> `dictJobs`
- `data/pledges` -> `dictPledges`
- `data/homeworlds` -> `dictHomeworlds`
- `data/ads` / related headline data -> `dictAds`, `dictHeadlines`
- `data/info` / tutorial node data -> `dictInfoNodes`
- `data/audio`-related json -> `dictAudioEmitters`
- `data/music` -> `dictMusic`
- `data/parallax` -> `dictParallax`
- `data/charges` or similarly named charge data -> `dictChargeProfiles`
- `data/plots` / `data/plotbeats` -> `dictPlots`, `dictPlotBeats`
- `data/racing`-related files -> `dictRaceTracks`, `dictRacingLeagues`

Text/simple-table loaders also feed:

- names (`dictNamesFirst`, `dictNamesLast`, `dictNamesFull`, `dictNamesShip`, etc.)
- strings (`dictStrings`)
- man pages (`dictManPages`)
- trait score lookups (`dictTraitScores`)

## Useful Runtime Pairings

These pairings are useful when tracing decompiled code:

- `JsonCond` <-> `Condition`
- `JsonInteraction` / `JsonInteractionSave` <-> `Interaction`
- `JsonCondOwner` / `JsonCondOwnerSave` <-> `CondOwner`
- `JsonCOOverlay` <-> `COOverlay`
- `JsonShipSitu` <-> `ShipSitu`
- `JsonNavData` <-> `NavData`
- `JsonBodyOrbitSave` / `JsonSpawnBodyOrbit` <-> `BodyOrbit`
- `JsonJob` / `JsonJobSave`
- `JsonPledge` / `JsonPledgeSave`

## Open Questions

- Unclear: some decompiled methods still contain suspicious logic that may be decompiler damage rather than original intent.
  Examples already noted in comments include parts of `MathUtils` and some always-constant UI helpers.
- Unclear: not every `data/` folder name was re-verified directly against loader call sites in this phase.
- Update: the repo-wide metadata cleanup pass has now removed the decompiler metadata comments from the remaining C# files in `src/Assembly-CSharp`, including third-party support code. Targeted intent comments were added across the remaining high-signal gameplay/runtime files as a final pass.

## Recommended Next Passes

1. Deepen method-level comments in already-covered complex systems rather than broad cleanup:
   `Tile`, `GUIInventoryWindow`, `Room`, `GasContainer`, `GUITrade`, `FlyToPath`, and `JumpPointSearch` still have room for denser method-by-method annotation if deeper modding work is planned.
2. Expand this file with a stricter registry-by-registry source table once every `JsonToData` callsite is traced.
3. If desired, do a purely polish-focused pass on comment consistency (indentation, phrasing, uncertainty markers) now that the decompiler metadata has been removed repo-wide.