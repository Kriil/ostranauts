## Construction Tweaks

Small BepInEx/Harmony patches for Ostranauts construction and installation behavior.

### Current patch

- Keeps the inventory UI open when starting an install action from inventory.

### Build

Run:

```powershell
dotnet build .\ConstructionTweaks.csproj -c Release
```

The compiled DLL will be written to:

`bin\Release\net472\ConstructionTweaks.dll`
