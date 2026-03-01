# Ostranauts Modding + Decompiled Code Explorer

This workspace is for **understanding and modding Ostranauts** using two complementary approaches:

1) **Data / JSON modding** — add/override content by placing JSON files in `Ostranauts_Data/Mods/` and controlling order via `loading_order.json`.
2) **C# code modding** — build BepInEx plugins that patch/extend compiled game code at runtime using Harmony.

This repo also contains a **decompiled export** of Ostranauts’ `Assembly-CSharp.dll` (Unity game logic) into a VS solution for exploration in VS Code, with comments rewritten into plain English.

---

## Key folders in an Ostranauts install (conceptual)

- `Ostranauts_Data/Managed/`  
  Contains managed assemblies (including `Assembly-CSharp.dll`) — the main game logic.
- `Ostranauts_Data/StreamingAssets/`  
  Contains base game data and assets:
  - `data/` JSON definitions (items, interactions, conditions, ships, etc.)
  - `images/`, `audio/`, `mesh/` assets
- `Ostranauts_Data/Mods/`  
  Data/JSON mods live here (NOT the same as BepInEx plugins)
- `BepInEx/monomod/`  
  Code mods live here. 
---

## How JSON data modding works

Base game data is loaded from:

- `StreamingAssets/data/` (aka **core**)

Then mods load in the order defined by:

- `Ostranauts_Data/loading_order.json`

Later mods override earlier mods when they define the same `strName` (case-sensitive).

```mermaid
flowchart TD
  Core[StreamingAssets/data (core)] --> Loader[Data loader]
  Loader --> GameRegistries[In-memory registries\n(dictItemDefs, etc.)]

  subgraph Mods[Mods loaded via loading_order.json]
    M1[Mods/ModA/data/...]
    M2[Mods/ModB/data/...]
  end

  Core --> M1 --> M2 --> GameRegistries


  flowchart LR
  Item[Item / object] -->|is a| CondOwner
  Character[Crew / character] -->|is a| CondOwner
  Ship[Ship / room / system] -->|is a| CondOwner

  CondOwner --> Condition[Condition(s)]
  CondOwner --> COOverlay[COOverlay(s)\n(friendly text + visuals)]

  Interaction -->|applies| CondTrigs[CondTrigs / triggers]
  CondTrigs -->|add/remove| Condition