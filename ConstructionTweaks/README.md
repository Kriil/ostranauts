## Construction Tweaks

Small BepInEx/Harmony patches for Ostranauts construction and installation behavior.

### Current patch

- Keeps the inventory UI open when starting an install action from inventory.
- Adds an `Alt` + left click shortcut to start placement for installable inventory items.
- Shows drag-select box dimensions in tiles and lightly highlights tiles inside the current drag box.
- Prevents crew from physically colliding with non-installed placeholders while leaving placeholder install behavior intact.
- Allows crew to path through tiles blocked only by non-installed placeholders, such as wall placeholders.

### Build

Run:

```powershell
dotnet build .\ConstructionTweaks.csproj -c Release
```

The compiled DLL will be written to:

`bin\Release\net472\ConstructionTweaks.dll`
