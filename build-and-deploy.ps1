# Build the RoomEffects project
Write-Host "Building RoomEffects..." -ForegroundColor Cyan
dotnet build "src\RoomEffects\RoomEffects.csproj" -c Release

# Check if build succeeded
if ($LASTEXITCODE -ne 0) {
    Write-Error "Build failed with exit code $LASTEXITCODE"
    exit $LASTEXITCODE
}

Write-Host "Build succeeded! Deploying mod files..." -ForegroundColor Green

# Copy mod_data contents
Write-Host "Copying mod_data to Mods folder..."
Copy-Item -Path "C:\Users\Kriil\Modding\Ostranauts\mods_source\src\RoomEffects\mod_data\*" -Destination "C:\Steam\steamapps\common\Ostranauts\Ostranauts_Data\Mods\Room_Effects" -Recurse -Force

# Copy compiled DLL to BepInEx plugins
Write-Host "Copying DLL to BepInEx plugins..."
Copy-Item -Path "C:\Users\Kriil\Modding\Ostranauts\mods_source\src\RoomEffects\bin\Release\net472\RoomEffects.dll" -Destination "C:\Steam\steamapps\common\Ostranauts\BepInEx\plugins\RoomEffects.dll" -Force

Write-Host "Build and deployment completed successfully!" -ForegroundColor Green
