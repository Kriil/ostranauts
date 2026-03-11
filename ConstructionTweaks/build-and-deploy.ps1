# Build the ConstructionTweaks project
Write-Host "Building ConstructionTweaks..." -ForegroundColor Cyan
dotnet build "ConstructionTweaks.csproj" -c Release

# Check if build succeeded
if ($LASTEXITCODE -ne 0) {
    Write-Error "Build failed with exit code $LASTEXITCODE"
    exit $LASTEXITCODE
}

Write-Host "Build succeeded! Deploying mod files..." -ForegroundColor Green

# Create Construction_Tweaks folder if it doesn't exist
$destFolder = "C:\Steam\steamapps\common\Ostranauts\Ostranauts_Data\Mods\Construction_Tweaks"
New-Item -ItemType Directory -Path $destFolder -Force | Out-Null

# Copy mod_data contents
Write-Host "Copying mod_data to Mods folder..."
Copy-Item -Path ".\mod_data\Construction_Tweaks\*" -Destination $destFolder -Recurse -Force

# Copy compiled DLL to BepInEx plugins
Write-Host "Copying DLL to BepInEx plugins..."
Copy-Item -Path ".\bin\Release\net472\ConstructionTweaks.dll" -Destination "C:\Steam\steamapps\common\Ostranauts\BepInEx\plugins\ConstructionTweaks.dll" -Force

Write-Host "Updating loading order in loading_order.json..." -ForegroundColor Cyan

Write-Host "Build and deployment completed successfully!" -ForegroundColor Green
