## Construction Tweaks

QoL tweaks for construction and installation behavior.

### Features

## Walk through wall placeholders
- Vanilla behavior treats wall placeholders as actual walls, meaning characters can't walk through them.  This mod removes that behavior while still allowing placeholder walls to act as walls for the purpose of installables that require them.

## Drag-Select Dimensions and Highlighting
- Shows the box dimensions as `A x B (Area) Tiles ` while drag-selecting as well as highlighting the tiles that will be affected by drag-select actions.

## Keep Inventory Open on Install
- Keeps the inventory UI open when creating an install task from the inventory.

## Alt-Click Install from Inventory
- Installable inventory items can be Alt-clicked to begin installing the item.  Additionally, a new install command can be issued while the current one is in progress (assuming Keep Inventory Open on Install is enabled).  Note that these install commands are automatically claimed by the character that was selected when they were issued, so it is not necessary to toggle `Autotask` to start them. This does not work for items on the ground due to conflicts with other functionality that occurs when you click in the world.  Drag a dolley with large items in it to use the Alt-click functionality on them.

### Configuration

All features can be enabled or disabled through the BepInEx config for the mod, which will be created as `Ostranauts\BepInEx\config\Construction_Tweaks.cfg` after the game is run once with the mod installed.  Changes to the config require a game restart to take effect.
