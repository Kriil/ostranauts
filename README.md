Mods for the game Ostranauts.  See README.md in mod folder for a detailed description of the mod.

### RoomEffects Mod
Adds bonus effects to rooms on the player's ships. This mod is intended to add additional incentive to create rooms on your ships besides just increasing its value. Plus it adds a little to the immersion and story telling aspects.

### Construction Tweaks

Small mod for Ostranauts construction and installation behavior.  

## Installation Instructions

### Install BepInEx and FFU-BR

1. Follow the instructions on Discord to install BepInEx and FFU-BR: https://discord.com/channels/302515943945273347/1298265273266212906/1298265273266212906
2. Make sure FFU-BR mod Minor Fixes Plus is installed: https://discord.com/channels/302515943945273347/1312016085243007027/1312016085243007027

### Install Room Effects
1. Download the latest release from GitHub: https://github.com/Kriil/ostranauts
2. Unzip the archive into the main game folder (`..\Steam\steamapps\common\Ostranauts`). `Ostranauts\BepInEx\plugins` should now contain the mods DLL files (e.g. `RoomEffects.dll`) and `Ostranauts\Ostranauts_Data\Mods` should now contain the mods data folders (e.g. `Room_Effects`).
3. The archive contains `Ostranauts\Ostranauts_Data\Mods\loading_order.json`. DO NOT overwrite this file if you already have mods installed. Instead follow the Mod Loading Order Instructions below to add the mod names (use folder name in Mods folder) to the `aLoadOrder` array.
4. Launch the game. After the main menu loads, exit the game. This will create config files in `Ostranauts\BepInEx\config`. Settings are described in the README.md files in each mod'f source folder.

### Mod Loading Order Instructions
1. First in the list should be `core`, then any non-FFU mods (i.e. all mods that don't rely on FFU modding API).
2. Right after them goes `Minor Fixes Plus` mod (to ensure that nothing overwrites and disrupts it).
3. Add `Room_Effects` and `Construction_Tweaks` after `Minor Fixes Plus`
