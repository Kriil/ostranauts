# Build the RoomEffects project
Write-Host "Building RoomEffects..." -ForegroundColor Cyan
dotnet build "RoomEffects.csproj" -c Release

# Check if build succeeded
if ($LASTEXITCODE -ne 0) {
    Write-Error "Build failed with exit code $LASTEXITCODE"
    exit $LASTEXITCODE
}

Write-Host "Build succeeded! Deploying mod files..." -ForegroundColor Green

# Create Room_Effects folder if it doesn't exist
$destFolder = "C:\Steam\steamapps\common\Ostranauts\Ostranauts_Data\Mods\Room_Effects"
New-Item -ItemType Directory -Path $destFolder -Force | Out-Null

# Copy mod_data contents
Write-Host "Copying mod_data to Mods folder..."
Copy-Item -Path ".\mod_data\Room_Effects\*" -Destination $destFolder -Recurse -Force

# Copy compiled DLL to BepInEx plugins
Write-Host "Copying DLL to BepInEx plugins..."
Copy-Item -Path ".\bin\Release\net472\RoomEffects.dll" -Destination "C:\Steam\steamapps\common\Ostranauts\BepInEx\plugins\RoomEffects.dll" -Force

Write-Host "Build and deployment completed successfully!" -ForegroundColor Green
