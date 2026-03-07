# GAME_SOURCE_FILE_EXPLANATION_WITH_FFU

This is the FFU-aware companion to `FILE_EXPLANATIONS.md`.

## Boolean Semantics
- `FileFullyOverwrittenByFFU`: `true` only if FFU replaces the entire game file/class behavior. In this repo, this is **typically false** because FFU patches selected methods, not whole files.
- `MethodFullyOverwrittenByFFU`: `true` when FFU uses a full method replacement pattern (for example explicit `[MonoModReplace]`), or **true** where FFU uses `orig_` wrappers with same-signature replacement methods.

## 1) Boot, Load, and Data Registries

| File | Purpose | Gameplay Impact | Dependencies | Likely Patch Points | FFU Changes | FileFullyOverwrittenByFFU | MethodFullyOverwrittenByFFU |
|---|---|---|---|---|---|---|---|
| `DataHandler.cs` | Central data loader and registry owner. | Defines runtime mod data and merge behavior. | `Json*`, `LitJson`, `LoadManager`, `Installables`, `CondTrigger`. | `Init()`, `LoadMod()`, `LoadModJsons()`, `JsonToData()`, post-load passes. | FFU core replaces boot/load logic to add partial overwrite, `strReference`, `removeIds`, `changesMap`, and sync loading. | false | true (`Init()` explicit; `LoadMod*` / `JsonToData` likely replaced by FFU core flow). |
| `Ostranauts/Core/LoadManager.cs` | Threaded loader orchestration. | Startup sequencing and save-load timing. | `DataHandler`, loader threads. | `BeginDataHanderLoadThreads()`, `AfterLoadThreadsFinish()`. | Not directly replaced by FFU; behavior changes indirectly because `DataHandler` is patched. | false | false |
| `MainMenu.cs` | Main menu startup flow. | Entry to game/load stack. | `DataHandler`, `CrewSim`. | Init/start flow methods. | No direct FFU replacement identified. | false | false |
| `Info.cs` | Data-dependent UI/controller. | Timing of data-driven UI availability. | `DataHandler`, info registries. | Deferred init logic. | No direct FFU replacement identified. | false | false |
| `ModLoader.cs` | Mod utility load behavior. | Mod discovery support behavior. | `DataHandler`, filesystem. | Enumeration/load helpers. | FFU mainly bypasses vanilla behavior via patched `DataHandler` load pipeline. | false | false |
| `JsonModInfo.cs` | Mod metadata schema. | Per-mod metadata and API parameters. | JSON pipeline. | Schema mapping methods. | FFU extends schema with `removeIds` and `changesMap` via patch DTO. | false | true (schema behavior extended/replaced in patched pipeline). |
| `JsonModList.cs` | Load-order schema. | Mod order precedence. | `DataHandler`. | Parse and load-order use sites. | No direct method replacement; consumed by FFU loader logic. | false | false |
| `JsonUserSettings.cs` | User settings including mod path. | Determines mod path defaults and behavior. | `DataHandler`, options UI. | Init/copy helpers. | Not directly replaced; used by FFU boot path. | false | false |

## 2) Core Simulation Objects

| File | Purpose | Gameplay Impact | Dependencies | Likely Patch Points | FFU Changes | FileFullyOverwrittenByFFU | MethodFullyOverwrittenByFFU |
|---|---|---|---|---|---|---|---|
| `CondOwner.cs` | Core entity state and interaction queue owner. | Touches most gameplay actions. | `Condition`, `Interaction`, `Ship`, `Room`, `Container`. | Queue/condition/inventory transitions. | FFU Extended adds inventory slot-effect behavior and sorted inventory traversal helpers through patched class extensions. | false | true (selected methods in FFU Extended patch paths). |
| `Condition.cs` | Runtime condition values. | Drives stats/flags across systems. | `CondOwner`, `CondRule`, `CondTrigger`. | Value update/serialize methods. | No direct FFU full method replacement found. | false | false |
| `CondTrigger.cs` | Rule and trigger engine. | Interaction gating and conditional effects. | `CondOwner`, `DataHandler`, `CondRule`. | `Triggered()`, apply/check helpers. | FFU Extended explicitly replaces `IsBlank()`, `Triggered(...)`, and `RulesInfo` getter; adds `nMaxDepth`, `strMathCond`, `aMathOps`. | false | true |
| `CondRule.cs` | Rule thresholds and display. | Status/rule behavior. | `Condition`, `CondOwner`. | Evaluation and info methods. | No direct FFU method replacement identified. | false | false |
| `Interaction.cs` | Action object lifecycle and effect application. | Core for work/social/device actions. | `CondTrigger`, `Loot`, `CondOwner`, `Relationship`. | Trigger checks, rate calc, loot apply, logging. | FFU Extended replaces `ApplyLogging(...)`; hooks `SetData`, `AddFailReason`, `TriggeredInternal` for verbose/debug behavior. FFU Super patches `CalcRate` behavior for super settings. | false | true (`ApplyLogging` explicit; others likely replaced/wrapped). |
| `Installables.cs` | Builds runtime install interactions from data. | Install/repair/uninstall mechanics. | `JsonInstallable`, `Interaction`. | `Create()` mapping logic. | No direct FFU replacement; heavily affected by FFU interaction/condtrigger extensions. | false | false |
| `Item.cs` | Runtime item behavior wrapper. | Item state/effects in gameplay. | `CondOwner`, `Container`. | State transitions. | No direct FFU replacement identified. | false | false |
| `Container.cs` | Container storage rules. | Determines storage validity and recursion safety. | `CondOwner`, `Slot`. | Allowed/insert/remove methods. | FFU Extended replaces `AllowedCO(...)`, `SetIsInContainer(...)`, `ClearIsInContainer(...)` for recursion safety and inventory effect sync. | false | true |
| `Slots.cs` | Slot orchestration and slot effects. | Equipment/sub-item modifiers. | `Slot`, `CondOwner`. | Slot traversal and effect apply. | FFU Extended alters sorted slot behavior and slot effect propagation via patched slot/container flow. | false | true (indirect via patched methods in `Slot`/`Container`). |
| `Slot.cs` | Single slot fit checks. | Core fit constraints. | `CondOwner`, `Container`. | `CanFit()` and fit logic. | FFU Extended replaces `CanFit(...)`; also patches inventory-open sorting behavior tied to slot ordering. | false | true |
| `Room.cs` | Room classification and room state. | Room-based mechanics and atmosphere context. | `RoomSpec`, `CondOwner`, `Ship`. | `CreateRoomSpecs()`, add/remove flows. | No direct FFU replacement identified. | false | false |
| `Tile.cs` | Grid tile state. | Movement and room linkage. | `Room`, `Ship`, `CondOwner`. | Room add/remove helpers. | No direct FFU replacement identified. | false | false |
| `Ship.cs` | Ship simulation coordinator. | Flight, gas, placement, docking behavior. | `ShipSitu`, `StarSystem`, `CondOwner`, `Room`. | `Maneuver()`, gas usage, init/load. | FFU core patch wraps/replaces `InitShip(...)` flow to apply `changesMap` and save/template sync commands. | false | true (`InitShip` likely replaced/wrapped). |
| `ShipSitu.cs` | Kinematic ship state. | Movement integration. | `Ship`, `NavData`. | Time advance methods. | No direct FFU replacement identified. | false | false |
| `StarSystem.cs` | Global world/system owner. | System-level runtime simulation. | `BodyOrbit`, `Ship`. | Tick/spawn methods. | No direct FFU replacement identified. | false | false |
| `BodyOrbit.cs` | Orbital model. | Celestial map behavior. | `StarSystem`. | Orbit update/serialize. | No direct FFU replacement identified. | false | false |
| `Pathfinder.cs` | Pathing and goal selection. | Character movement reliability. | `Tile`, `CondOwner`. | Goal/set path methods. | No direct FFU replacement identified. | false | false |
| `PathResult.cs` | Path result payload. | Reachability and path length usage. | `Pathfinder`. | Path result fields. | No direct FFU replacement identified. | false | false |

## 3) Atmosphere, Power, and Device Systems

| File | Purpose | Gameplay Impact | Dependencies | Likely Patch Points | FFU Changes | FileFullyOverwrittenByFFU | MethodFullyOverwrittenByFFU |
|---|---|---|---|---|---|---|---|
| `GasContainer.cs` | Gas storage and mass/pressure state. | Atmosphere and fuel usage behavior. | `CondOwner`, `Ship`, devices. | Gas add/remove/sync APIs. | No direct FFU method replacement identified. | false | false |
| `GasPump.cs` | Pump/scrubber transfer behavior. | Atmosphere management throughput. | `GasContainer`, `CondTrigger`, `CondOwner`. | `Respire2()`. | No direct FFU method replacement in core set (RoomEffects mod patches this, not FFU). | false | false |
| `Heater.cs` | Heat/cool processing. | Thermal control gameplay behavior. | `CondOwner`, `GasContainer`, GUI prop maps. | `Heat(...)`. | FFU Extended explicitly replaces `Heat(double fTimePassed)` and adds support for emitted-temp style behavior. | false | true |
| `GasExchange.cs` | Gas transfer/equalization system. | Pressure equalization behavior. | `GasContainer`. | Update flow. | No direct FFU replacement identified. | false | false |
| `GasPressureSense.cs` | Pressure-trigger automation. | Triggered pressure interactions. | `CondOwner`, `Interaction`. | Update logic. | No direct FFU replacement identified. | false | false |
| `Electrical.cs` | Power routing behavior. | Device availability and system readiness. | `CondOwner`, powered systems. | Update/power propagation. | No direct FFU replacement identified. | false | false |
| `Powered.cs` | Generic powered module behavior. | Device command behavior and auto-actions. | `CondOwner`, `Ship`, `Interaction`. | Power-state and command execution. | No direct FFU replacement identified. | false | false |
| `Sensor.cs` | Sensor runtime logic. | Trigger-based automation behavior. | `CondTrigger`, `Interaction`, GUI prop maps. | `Run()`, `SetData(...)`. | FFU Extended explicitly replaces `Run()` and `SetData(...)` for self-check and update-interval enhancements. | false | true |
| `FusionIC.cs` | Reactor logic and controls. | Fuel/heat/reactor operation. | `CondOwner`, `GasContainer`. | Control/update methods. | No direct FFU replacement identified. | false | false |

## 4) Crew, AI, Tasks, and Narrative

| File | Purpose | Gameplay Impact | Dependencies | Likely Patch Points | FFU Changes | FileFullyOverwrittenByFFU | MethodFullyOverwrittenByFFU |
|---|---|---|---|---|---|---|---|
| `CrewSim.cs` | Main gameplay crew controller. | Core crew behavior and command execution. | `CondOwner`, `Interaction`, `Ship`, UI. | Interaction dispatch/time flow. | No direct FFU replacement identified. | false | false |
| `Crew.cs` | Crew-level runtime data. | Crew state handling. | `CondOwner`, `PersonSpec`. | Setup/state methods. | No direct FFU replacement identified. | false | false |
| `AIShipManager.cs` | AI ship behavior and spawn manager. | Encounter and traffic dynamics. | `Ship`, `StarSystem`, `Interaction`. | AI update and queue logic. | No direct FFU replacement identified. | false | false |
| `WorkManager.cs` | Work/task orchestrator. | Work selection/cadence. | `Interaction`, `CondOwner`. | Assignment flow. | No direct FFU replacement identified. | false | false |
| `Task2.cs` | Runtime task representation. | Queue semantics for work. | `Interaction`, `CondOwner`. | Task evaluation. | No direct FFU replacement identified. | false | false |
| `AutoTask.cs` | Task auto-generation helper. | QoL automation behavior. | `Task2`, `Interaction`. | Generation methods. | No direct FFU replacement identified. | false | false |
| `BeatManager.cs` | Narrative beat manager. | Scripted event trigger flow. | `Interaction`, `CrewSim`. | Beat trigger methods. | No direct FFU replacement identified. | false | false |
| `PlotManager.cs` | Plot progression manager. | Story/tutorial progression behavior. | Plot DTOs, `DataHandler`. | Plot eval/update methods. | No direct FFU replacement identified. | false | false |
| `GigManager.cs` | Contract/gig manager. | Mission availability and payout loop. | Jobs/ads/ledger systems. | Gig generation and accept methods. | No direct FFU replacement identified. | false | false |
| `Pledge2.cs` + `Pledge*.cs` | Pledge behavior classes. | Persistent AI goals and behavior loops. | `Interaction`, `CondOwner`, `Ship`. | Queue/run methods. | No direct FFU replacement identified. | false | false |
| `PersonSpec.cs` | Personality/spec matching logic. | Social and AI match behaviors. | Interaction/social systems. | Match/test methods. | No direct FFU replacement identified. | false | false |
| `Relationship.cs` | Relationship state and remembered outcomes. | Long-term social dynamics. | `Interaction`, `CondOwner`. | Store/update relationship data. | No direct FFU replacement identified. | false | false |
| `Social.cs` | Social interaction state handling. | Immediate social resolution behavior. | `Interaction`, `Relationship`. | Resolve/apply methods. | No direct FFU replacement identified. | false | false |

## 5) Economy, Trading, and Progression

| File | Purpose | Gameplay Impact | Dependencies | Likely Patch Points | FFU Changes | FileFullyOverwrittenByFFU | MethodFullyOverwrittenByFFU |
|---|---|---|---|---|---|---|---|
| `Ledger.cs` | Transaction/ledger manager. | Money and recurring costs/rewards. | `JsonLedgerDef`, `CondOwner`. | Add/update transaction logic. | No direct FFU replacement identified. | false | false |
| `LedgerLI.cs` | Ledger line-item model. | Detailed finance record behavior. | `Ledger`. | Serialization/update fields. | No direct FFU replacement identified. | false | false |
| `Trader.cs` | Trade operation helper. | Buy/sell execution behavior. | Market and loot systems. | Validation/apply methods. | No direct FFU replacement identified. | false | false |
| `Loot.cs` | Loot parsing and application. | Reward/chance/condition effect behavior. | `CondTrigger`, `CondOwner`, `DataHandler`. | Parse and loot-get/apply methods. | FFU Extended explicitly replaces `GetCTLoot`, `GetCOLoot`, `GetLootNames`, `ApplyCondLoot`, `GetCondLoot`, and `ParseLootDef` (safe parse + dynamic range behavior). | false | true |
| `LootSpawner.cs` | Loot spawn orchestrator. | Spawn behavior in world/events. | `Loot`, `CondOwner`. | Spawn methods. | No direct FFU replacement identified. | false | false |
| `Ostranauts/Trading/MarketManager.cs` | Market simulation manager. | Economy dynamics. | Market DTOs. | Update/query methods. | No direct FFU replacement identified. | false | false |
| `Ostranauts/Trading/ShipMarket.cs` | Ship market logic. | Ship sale availability/pricing. | Market systems, ship specs. | Generation/pricing methods. | No direct FFU replacement identified. | false | false |
| `Ostranauts/Trading/MarketActor.cs` | Market actor runtime model. | Local market behavior. | Actor config. | Setup/update methods. | No direct FFU replacement identified. | false | false |
| `Ostranauts/Trading/MarketActorConfig.cs` | Market actor config schema. | Price/stock rules. | Data pipeline. | Matching/init methods. | DTO extended by FFU data-struct patch (`strReference`). | false | true (schema extension in patched type). |

## 6) High-Impact UI / Control

| File | Purpose | Gameplay Impact | Dependencies | Likely Patch Points | FFU Changes | FileFullyOverwrittenByFFU | MethodFullyOverwrittenByFFU |
|---|---|---|---|---|---|---|---|
| `GUIInventory.cs` | Inventory window manager. | UX for inventory trees and transfers. | `GUIInventoryItem`, `GUIInventoryWindow`, `Slots`. | Spawn/update methods. | FFU Extended replaces `SpawnInventoryWindow(...)` (sorted open order). FFU Quality likely replaces/wraps `Update()` for organized layout. | false | true |
| `GUIInventoryItem.cs` | Item drag/shift-click behavior. | Item movement and quick transfer. | Inventory and interaction systems. | `OnShiftPointerDown()`, move methods. | FFU Quality patches shift-transfer behavior (quick-move enhancements; wrapper pattern). | false | true |
| `GUIInventoryWindow.cs` | Window position/context logic. | Inventory readability and recursion UX. | `GUIInventory`. | Position/open methods. | FFU Console replaces `WorldPosFromPair(...)` for target inventory opening behavior. | false | true |
| `GUIQuickBar.cs` | Quick interaction bar manager. | Fast action availability UX. | `Interaction`, selected crew. | Start/expand/collapse. | FFU Quality quickbar pinning uses wrapper-style overrides on `Start()` and `ExpandCollapse()` (likely full-method replacement semantics). | false | true |
| `GUIDockSys.cs` | Docking UI/controller. | Docking and maneuver command issuance. | `Ship.Maneuver()`. | Input/command methods. | No direct FFU replacement identified. | false | false |
| `GUIOrbitDraw.cs` | Nav/orbit map UI/control. | Plotting and piloting interactions. | `Ship`, `NavData`, `StarSystem`. | Plot/control methods. | No direct FFU replacement identified. | false | false |
| `GUIReactor.cs` | Reactor panel UI. | Reactor control UX. | `FusionIC`, `CondOwner`. | Control update methods. | No direct FFU replacement identified. | false | false |
| `GUIPDA.cs` | PDA app container UI. | Access to jobs/finance/messages. | PDA sub-apps and data. | App routing methods. | No direct FFU replacement identified. | false | false |
| `GUIComputer2.cs` | Computer terminal UI. | Device/system control access. | `CondOwner`, interactions. | Bind/action methods. | No direct FFU replacement identified. | false | false |
| `GUIHelmet.cs` | Suit HUD. | Survival info and alerts. | `CondOwner`, suit conditions. | UI update methods. | FFU Quality explicitly replaces `UpdateUI(...)` (suit HUD enhancements/notifications). | false | true |
| `GUIOptions.cs` | Settings UI and persistence. | Runtime options + mod path behavior. | `JsonUserSettings`, `DataHandler`. | Apply/save methods. | No direct FFU replacement identified. | false | false |
| `GUIJobs.cs` | Jobs/contracts UI. | Mission selection UX. | Jobs/gigs/ledger systems. | List/action handlers. | No direct FFU replacement identified. | false | false |
| `GUIFinance.cs` | Finance UI. | Payment/debt visibility. | `Ledger`, PDA. | Panel refresh/action methods. | No direct FFU replacement identified. | false | false |
| `ConsoleResolver.cs` | Console parser and command handlers. | Debug/admin command execution. | `ConsoleToGUI`, runtime systems. | Keyword handlers. | FFU Console replaces `KeywordGetCond(...)`; also wraps resolver/help pathways to add FFU commands. | false | true (`KeywordGetCond` explicit; others likely wrapped). |
| `ConsoleToGUI.cs` | Console rendering/input. | Debug UX and output handling. | `ConsoleResolver`. | Draw/update. | FFU Console explicitly replaces `DrawConsole(int window)`. | false | true |
| `Ostranauts/UI/Loading/GUILoadMenu.cs` | Save-load UI. | Save selection flow. | `LoadManager`. | Load list/action handlers. | No direct FFU replacement identified. | false | false |
| `Ostranauts/UI/Loading/GUISaveMenu.cs` | Save UI. | Save create/overwrite flow. | `LoadManager`. | Save action methods. | No direct FFU replacement identified. | false | false |
| `Ostranauts/UI/Loading/GUISaveLoadBase.cs` | Shared save/load base. | Shared save/load behavior. | `LoadManager`. | Refresh/path methods. | No direct FFU replacement identified. | false | false |

## 7) Navigation, Commands, and Ship GUI Subsystems

| File | Purpose | Gameplay Impact | Dependencies | Likely Patch Points | FFU Changes | FileFullyOverwrittenByFFU | MethodFullyOverwrittenByFFU |
|---|---|---|---|---|---|---|---|
| `Ostranauts/ShipGUIs/Utilities/NavData.cs` | Runtime nav plotting data. | Autopilot and route behavior. | `Ship`, star system state. | `TimeAdvance()`. | No direct FFU replacement identified. | false | false |
| `Ostranauts/Ships/Commands/FlyToPath.cs` | Path-follow autopilot command. | Long-range nav behavior. | `Ship`, `NavData`. | `RunCommand()`. | No direct FFU replacement identified. | false | false |
| `Ostranauts/Ships/Commands/FlyTo.cs` | Point-to-point autopilot command. | Basic nav behavior. | `Ship`. | Run/stop methods. | No direct FFU replacement identified. | false | false |
| `Ostranauts/Ships/Commands/HoldStationAutoPilot.cs` | Station keep command. | Precision movement behavior. | `Ship.Maneuver()`. | Run/adjust methods. | FFU Quality modifies hold+brace behavior in related command logic (patch target in quality module). | false | true (in command method patched by FFU Quality). |
| `Ostranauts/Ships/Commands/HoldThrustAutoPilot.cs` | Continuous thrust command. | Long maneuver behavior. | `Ship.Maneuver()`. | Run methods. | No direct FFU replacement identified. | false | false |
| `Ostranauts/Ships/Comms/Comms.cs` | Comms and remote interaction effects. | Encounter outcome effects. | `Interaction`, `Loot`, `Ship`. | ApplyLoot pathways. | No direct FFU replacement identified. | false | false |
| `Ostranauts/ShipGUIs/Trade/GUITrade.cs` | Trade UI. | Trade UX and flow consistency. | `Trader`, market, inventory UI. | Build/confirm methods. | No direct FFU replacement identified. | false | false |
| `Ostranauts/ShipGUIs/Market/GUIShipMarket.cs` | Ship market UI. | Buy/sell flow. | `ShipMarket`, market actors. | Row and transaction handlers. | No direct FFU replacement identified. | false | false |
| `Ostranauts/ShipGUIs/ShipBroker/GUIShipBroker.cs` | Broker UI/controller. | Ship brokerage flow. | Market + preview systems. | Broker handlers. | No direct FFU replacement identified. | false | false |
| `Ostranauts/ShipGUIs/NavStation/NavModCoursePlot.cs` | Nav station course plot module. | Route plotting behavior. | `NavData`, `Ship`. | Plot commit/cancel methods. | No direct FFU replacement identified. | false | false |

## 8) Serialization DTOs (Key Modding Schemas) + FFU Extensions

FFU core extends many DTO classes with `strReference` (and some new fields), enabling reference-copy/inheritance and partial overwrite semantics.

| File | Purpose / Domain | FFU Extension | FileFullyOverwrittenByFFU | MethodFullyOverwrittenByFFU |
|---|---|---|---|---|
| `JsonCond.cs` | Conditions | adds `strReference` | false | true (schema extension in patched type) |
| `JsonCondOwner.cs` | CO templates | adds `strReference`; FFU Extended also adds inventory slot effect field behavior | false | true |
| `JsonInteraction.cs` | Interactions | adds `strReference`; FFU Extended adds `bForceVerbose`, `bRoomLookup` | false | true |
| `JsonInstallable.cs` | Installables | adds `strReference` | false | true |
| `JsonItemDef.cs` | Item defs | adds `strReference` | false | true |
| `JsonRoomSpec.cs` | Room specs | adds `strReference` | false | true |
| `JsonShip.cs` | Ship templates | adds `strReference`; used by FFU changes-map sync logic | false | true |
| `JsonShipSpec.cs` | Ship spec predicates | adds `strReference`; matching logic patched in FFU Extended | false | true |
| `JsonSlot.cs` | Slot defs | adds `strReference`; slot open/sort logic patched | false | true |
| `JsonSlotEffects.cs` | Slot effects | adds `strReference` | false | true |
| `JsonTicker.cs` | Tickers | adds `strReference` | false | true |
| `JsonPowerInfo.cs` | Power metadata | adds `strReference` | false | true |
| `JsonGUIPropMap.cs` | Device GUI map | adds `strReference` | false | true |
| `JsonGasRespire.cs` | Gas respire defs | adds `strReference` | false | true |
| `JsonCareer.cs` | Career defs | adds `strReference` | false | true |
| `JsonHomeworld.cs` | Homeworld defs | adds `strReference` | false | true |
| `JsonLifeEvent.cs` | Life events | adds `strReference` | false | true |
| `JsonPersonSpec.cs` | Person specs | adds `strReference` | false | true |
| `JsonJob.cs` / `JsonJobItems.cs` | Jobs | adds `strReference` | false | true |
| `JsonPledge.cs` | Pledges | adds `strReference` | false | true |
| `JsonLedgerDef.cs` | Economy defs | adds `strReference` | false | true |
| `JsonMarketActorConfig.cs` | Market config | adds `strReference` | false | true |
| `JsonCargoSpec.cs` | Cargo specs | adds `strReference` | false | true |
| `JsonPlot*` / `JsonPlotManagerSettings.cs` | Plot systems | adds `strReference` | false | true |
| `JsonZoneTrigger.cs` | Zone triggers | adds `strReference` | false | true |
| `JsonTip.cs` / `JsonCrime.cs` / `JsonInfoNode.cs` / `JsonAudioEmitter.cs` / `JsonAd.cs` / `JsonHeadline.cs` / `JsonMusic.cs` / `JsonCOOverlay.cs` / `JsonDCOCollection.cs` / `JsonTransit.cs` / `JsonParallax.cs` / `JsonContext.cs` / `JsonChargeProfile.cs` / `JsonWound.cs` / `JsonAttackMode.cs` / `JsonPDAAppIcon.cs` / `JsonRaceTrack.cs` / `JsonRacingLeague.cs` / `JsonInteractionOverride.cs` / `JsonPlotBeatOverride.cs` / `JsonVerbs.cs` / `JsonCustomTokens.cs` / `JsonStarSystemSave.cs` | Various registries | adds `strReference` across registries | false | true |

## 9) FFU Overwrite Inventory (Explicit `[MonoModReplace]` Targets)

These are the high-confidence full method replacements in FFU code:
- `DataHandler.Init`
- `LitJson.JsonMapper.ReadValue`
- `LitJson.JsonMapper.WriteValue`
- `GUIHelmet.UpdateUI`
- `Interaction.ApplyLogging`
- `GUIInventory.Update` (quality module)
- `Heater.Heat`
- `CondTrigger.IsBlank`
- `CondTrigger.Triggered`
- `CondTrigger.RulesInfo` getter
- `GUIInventory.SpawnInventoryWindow`
- `Slot.CanFit`
- `Container.AllowedCO`
- `Container.SetIsInContainer`
- `Container.ClearIsInContainer`
- `Loot.GetCTLoot`
- `Loot.GetCOLoot`
- `Loot.GetLootNames`
- `Loot.ApplyCondLoot`
- `Loot.GetCondLoot`
- `Loot.ParseLootDef`
- `Sensor.Run`
- `Sensor.SetData`
- `ConsoleToGUI.DrawConsole`
- `ConsoleResolver.KeywordGetCond`
- `GUIInventoryWindow.WorldPosFromPair`

## 10) Practical Modding Notes
- FFU usually does **targeted method replacement**, not whole-file replacement.
- The largest behavior shift is in `DataHandler` load semantics (partial overwrite, reference inheritance, changes maps).
- If your mod touches any methods above, assume high compatibility risk and test load order carefully.
