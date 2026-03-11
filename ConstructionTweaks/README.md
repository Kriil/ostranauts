## Construction Tweaks

Small BepInEx/Harmony patches for Ostranauts construction and installation behavior.

### Current patch

- Keeps the inventory UI open when starting an install action from inventory.
- Adds an `Alt` + left click shortcut to start placement for installable inventory items.
- Shows drag-select box dimensions in tiles and lightly highlights tiles inside the current drag box.
- Makes wall placeholders non-blocking by swapping their tile socket adds to a placeholder-only wall support marker.
- Lets wall-mounted installs treat wall placeholders as valid support without turning those placeholders back into real walls.

### Build

Run:

```powershell
dotnet build .\ConstructionTweaks.csproj -c Release
```

The compiled DLL will be written to:

`bin\Release\net472\ConstructionTweaks.dll`
