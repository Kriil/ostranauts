Mods for the game Ostranauts.  See README.md in mod folder for a detailed description of the mod.

### RoomEffects Mod
Adds bonus effects to rooms on the player's ships. This mod is intended to add additional incentive to create rooms on your ships besides just increasing its value. Plus it adds to the immersion and story telling aspects.

### Construction Tweaks

QoL Tweaks to the construction and installation behavior.  Includes walking through uninstalled wall placeholders and drag-select highlighting with displayed dimensions

### Blueprints

Drag-select installables into a temporary blueprint, queue uninstall tasks, and place install orders from the saved layout.

### Docking Autosave Delay

Delays periodic autosaves while the docking UI is active by pushing the autosave timer back one minute at a time until docking mode ends.

## Installation Instructions

### Install BepInEx and FFU-BR

1. Follow the instructions on Discord to install BepInEx and FFU-BR: https://discord.com/channels/302515943945273347/1298265273266212906/1298265273266212906
2. Make sure FFU-BR mod Minor Fixes Plus is installed: https://discord.com/channels/302515943945273347/1312016085243007027/1312016085243007027

### Install All Mods
1. Download the latest release from GitHub: https://github.com/Kriil/ostranauts/releases
2. Unzip the archive into the main game folder (`..\Steam\steamapps\common\Ostranauts`). `Ostranauts\BepInEx\plugins` should now contain the mod's DLL files (e.g. `RoomEffects.dll`) and `Ostranauts\Ostranauts_Data\Mods` should now contain the mod's data folders (e.g. `Room_Effects`).
3. The archive contains `Ostranauts\Ostranauts_Data\Mods\loading_order.json`. DO NOT overwrite this file if you already have mods installed. Instead follow the Mod Loading Order Instructions below to add the mod to the `aLoadOrder` array.
4. Launch the game. After the main menu loads, exit the game. This will create config files in `Ostranauts\BepInEx\config`. Settings are described in the README.md files in each mod's source folder.

### Mod Loading Order Instructions
1. First in the list should be `core`, then any non-FFU mods (i.e. all mods that don't rely on FFU modding API).
2. Right after them goes `Minor Fixes Plus` mod (to ensure that nothing overwrites and disrupts it).
3. Add `Room_Effects`, `Construction_Tweaks` and `Blueprints` after `Minor Fixes Plus`

### Install a Single Mod
1. Download the latest release from GitHub: https://github.com/Kriil/ostranauts/releases
2. Unzip the archive into a temporary folder.
3. Copy the DLL for the mod you want to install from `BepInEx\plugins` and place it into the main game folder (`..\Steam\steamapps\common\Ostranauts`) in the `BepInEx\plugins` folder
4. Copy the Mods folder for the mod you want to install into the main game folder (`..\Steam\steamapps\common\Ostranauts`) in the `Ostranauts_Data\Mods` folder.
5. The archive contains `Ostranauts\Ostranauts_Data\Mods\loading_order.json`. DO NOT overwrite this file if you already have mods installed. Instead follow the Mod Loading Order Instructions below to add the mod to the `aLoadOrder` array.


## Local Build and Deploy
### Local Build and Deployment Script
If you've cloned this repo, you can manaully build and deploy the mod using the `.\build-and-deploy.ps1` script making sure to set -GamePath to the Ostranauts folder

    .\build-and-deploy.ps1 -Help

    Usage:
    From a project directory:
        ..\build-and-deploy.ps1 [-GamePath <path>]
    From the repo root:
        .\build-and-deploy.ps1 -Project <project> [-GamePath <path>]

    Accepted -Project values:
    Construction_Tweaks, Room_Effects

    Examples:
    .\build-and-deploy.ps1 -Project Room_Effects
    .\build-and-deploy.ps1 -Project ConstructionTweaks
    .\build-and-deploy.ps1 -Project Room_Effects -GamePath D:\Games\Ostranauts

### Manual build
You can build the DLL by running `dontnet build <Project-Name>.csproj -c Release` in the appropriate directory
