# REPO_MAP

## Overview
`src/RoomEffects` is a BepInEx + Harmony hybrid mod with a small companion data pack (`mod_data`) that defines custom conditions used by the plugin.

## Structure
- `Plugin.cs`
  - BepInEx plugin entry point, config binding, Harmony patch registration.
- `RoomEffectUtils.cs`
  - shared helpers for room lookup, bonus application, and interaction amount modifiers.
- Bonus handlers
  - `BonusAirlock.cs`: placeholder/no active bonus logic yet.
  - `BonusBathroom.cs`: bathroom interaction speed bonuses.
  - `BonusBridge.cs`: placeholder/no active bonus logic yet.
  - `BonusEngineering.cs`: engineering room detection and ship-wide work bonus
  - `BonusGalley.cs`: food and hydration rate gains in Galley interactions.
  - `BonusPassenger.cs`: relax/security-chair effects in Passenger rooms.
  - `BonusQuarters.cs`: sleep recovery bonuses for Basic/Luxury quarters.
  - `BonusReactor.cs`: placeholder/no active bonus logic yet.
  - `BonusRecreation.cs`: positive/negative interaction effect shaping in Recreation rooms.
  - `BonusTowing.cs`: tow brace secure speed bonuses.
  - `BonusWellness.cs`: exercise training bonuses.
- Harmony patches
  - `Patch_Room_CreateRoomSpecs.cs`, `Patch_Room_AddToRoom.cs`, `Patch_Room_RemoveFromRoom.cs`
    - refresh room/ship bonus state on room recompute and room membership changes.
  - `Patch_CondOwner_QueueInteraction.cs`
    - modifies queued interaction duration for selected interaction types.
  - `Patch_Interaction_CalcRate.cs`
    - transpiler around `Interaction.CalcRate()` clamp for work-rate bonus integration.
  - `Patch_Interaction_ApplyLoot.cs`
    - prefixes replacing `ApplyLootCT` and `ApplyLootConds` to adjust trigger/cond amounts by room effects.
- Build and metadata
  - `RoomEffects.csproj`, `build.bat`, `README.md`.
- Data package
  - `mod_data/mod_info.json`
  - `mod_data/data/conditions/cds_room_effects.json` (custom condition defs used by plugin logic).

## Practical Architecture (Modder Terms)
- Data file adds condition IDs to game registry.
- Harmony patches detect room context and inject bonuses into existing vanilla work/interaction/ship/atmo systems.
- Most effects are player-ship-gated to avoid changing NPC ships globally.

## Type Classification
- Hybrid:
  - BepInEx plugin (runtime entry + config),
  - Harmony patch mod (method hooks/transpiler),
  - plus a small supporting data mod (`mod_data`).

## Systems Touched (Likely)
- Room spec determination and room membership effects.
- Interaction work-rate, loot trigger/condition application.
- CondOwner queue/ticker timing.
- Ship maneuvering and RCS intake.
- Heater/cooler and gas pump processing.

## Dependencies
- BepInEx
- Harmony (`0Harmony`)
- Ostranauts `Assembly-CSharp`
- UnityEngine

## Flags (Broken/Obsolete/Suspicious)
- `bin/` and `obj/` outputs are present in source tree; likely should be gitignored for cleaner repo hygiene.
- `Patch_Interaction_ApplyLoot.cs` logs `coThem.strName` without null guard; if `coThem` can be null in some interaction paths, this is a potential NRE risk.
- Very verbose runtime logging in multiple patches may be expensive in long sessions.
- `BonusBridge.cs` is intentionally empty, so bridge bonuses are currently a declared but inactive feature.
