# Console Commands

This document lists in-game debug console commands found in the local source for this workspace.

## Base Commands (ConsoleResolver)

| Command | What it does |
|---|---|
| `help` | Lists commands, or explains a specific command via `help <command>`. |
| `echo` | Echoes the provided text. |
| `unlockdebug` | Unlocks debug hotkeys and debug overlay. |
| `crewsim` | Shows how long CrewSim has been running. |
| `addcond` | Adds a condition amount to a target CondOwner. Supports `[us]`, `[them]`, name, friendly name, or ID. |
| `getcond` | Lists matching condition values on a target CondOwner. Supports partial condition-name matches. |
| `bugform` | Opens the bug report form URL. |
| `spawn` | Spawns loot/CO by ID near the player (inventory or ground). |
| `verify` | Verifies game JSON files. |
| `kill` | Applies death to the named CondOwner. |
| `addcrew` | Adds random crew to the current ship (optional count). |
| `addnpc` | Adds random NPCs to the current ship (optional count). |
| `damageship` | Applies random damage across current ship items/tiles. |
| `breakinship` | Runs the derelict break-in damage/loot-loss pass on current ship. |
| `meteor` | Spawns meteor hits on current ship (optional count). |
| `oxygen` | Adds oxygen to all people on current ship (optional amount). |
| `toggle` | Toggles debug/photo settings (for example `aoshow`, `aozoom`, `aospread`, `aointensity`, `all`). |
| `ship` | Spawns and teleports player to a named ship template. |
| `shipvis` | Toggles visibility state for a ship by reg ID (`shipvis <True/False> <shipRegID>`). |
| `lookup` | Looks up `ships`, `plots`, or `tutorials`. |
| `plot` | Forces progress/check on a named plot chain. |
| `summon` | Teleports a named NPC to the player ship. |
| `rel` | Adds/sets relationship condition between people. |
| `skywalk` | Teleports player to another ship by reg ID. |
| `detach` | Detaches console logging capture (experimental). |
| `attach` | Re-attaches console logging capture (experimental). |
| `meatstate` | Reads or sets meat simulation mode (`0-5` or named states). |
| `priceflips` | Debug dump comparing buy/sell value flips for barter-valid items. |
| `tutorial` | Adds a tutorial beat by class name. |
| `stopTutorial` / `stop` | Removes a tutorial beat by class name without completing it. |
| `complete` | Marks a tutorial beat as complete by class name. |
| `pda` | PDA debug commands: `open`, `unlock`, `show`, `hide`, `reset`. |
| `rename` | Renames the currently selected non-crew object. |
| `clear` / `clr` | Clears console log lines (optional number). |
| `wipe` | Clears electrical signal connections on selected non-crew object (`soft`, `med`, or hard/default). |

## FFU Console Extensions

These are added/extended by FFU console patches.

| Command | What it does |
|---|---|
| `getcond [target] *` | FFU wildcard: list all conditions on target. |
| `getcond [them]-NUM <filter>` | Targets parent CondOwner `NUM` levels above selected target. |
| `getcond [target] *coParents` | Lists selected target's CondOwner parent chain. |
| `getcond [target] *coRules` | Lists attached CondRules and threshold indices. |
| `getcond [target] *coTickers` | Lists attached tickers and timers. |
| `findcondcos <conds...>` | Lists CO templates matching required/forbidden conditions. |
| `triggerinfo <condtrigger> <mode>` | Prints condtrigger rules (`mode 0` raw, `mode 1` friendly). |
| `triggertest <condtrigger> <condowner>` | Tests trigger against a target/template and logs result. |
| `openinventory` | Opens debug inventory from selected target perspective. |
| `repairship` | Repairs current ship via FFU helper routine. |

## Notes

- Target shortcuts: `[us]` and `player` map to selected crew/player context; `[them]` maps to currently selected tooltip target.
- Names with spaces should be passed with underscores when required by command parsing.
- For FFU builds, `getcond` behavior is replaced by the FFU implementation.

## Useful Spawn Examples

These examples use IDs confirmed in the local data files for this workspace.

| Command | Result |
|---|---|
| `spawn ItmAICargo01` | Spawns an A.I. Cargo module. |
| `spawn ItmCargoLift01` | Spawns a Cargo Lift. |
| `spawn ItmBattery02` | Spawns a ship battery. |
| `spawn ItmRCSCluster01` | Spawns an RCS thruster cluster. |
| `spawn ItmStationNav` | Spawns a Nav Console. |
| `spawn ItmTowingBrace01` | Spawns a Tow Brace. |
| `spawn ItmTowingBrace01Loose` | Spawns a Tow Brace (Loose). |
| `spawn ItmCanisterLH02Loose` | Spawns a D2O Canister (Loose). |
| `spawn ItmCanisterLHe02Loose` | Spawns a Liq. He3 Canister (Loose). |
| `addcrew 3` | Adds 3 random crew to the current ship. |
| `addnpc 3` | Adds 3 random NPCs to the current ship. |
| `lookup ships` | Lists loaded ships as `<regID> : <public name>`. |
| `lookup ships aero` | Filters loaded ships whose public name contains `aero`. |
| `ship Volatile Aero` | Spawns the `Volatile Aero` ship template and teleports you to it. |
| `skywalk OKLG` | Teleports you to the loaded ship with registry ID `OKLG` (example: K-LEG). |
| `shipvis true OKLG` | Forces a loaded ship visible by registry ID. |

Quick usage notes:
- Use `lookup ships` first if you need a registry ID for `skywalk` or `shipvis`.
- Use `ship <public name>` to spawn a ship template by name, not by registry ID.
- If an item has multiple state variants, prefer the base item first, then try variants like `Loose`, `Off`, or `Dmg`.

## Source References

- `game_source/ConsoleResolver.cs`
- `FFU-BR/FFU_BR_Console/FFU_BR_Patch_LoadConsole.cs`
- `FFU-BR/FFU_BR_Console/FFU_BR_Patch_GetCondPlus.cs`
- `FFU-BR/README.md`

# Debug Overlay And Debug Hotkeys

`unlockdebug` enables `CrewSim.bEnableDebugCommands`, which does two things:

1. Lets you toggle the **debug overlay** (the `goCanvasDebug` UI).
- Default key is the **backquote** key (`` ` ``), shown by `help unlockdebug`.
- When opened, it initializes debug panels like **DebugFastTravel** and **DebugRespawnShip**.
- It also turns on room ID labels in-world (`ship.ShowRoomIDs(true)`), and hides them when toggled off.

2. Enables a few **debug-only hotkeys** in code:
- `Q` = rotate camera/ship view CCW (debug-gated)
- `E` = rotate camera/ship view CW (debug-gated)
- `F8` = toggle visual filters (`GameRenderer.SwapMode()`; debug-gated)

If your keybinds were changed, use the Controls menu or run `help unlockdebug` to see the current debug overlay key name.
