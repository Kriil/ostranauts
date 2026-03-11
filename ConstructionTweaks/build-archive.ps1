param(
    [string]$version = "",
    [string]$buildtype = ""
)

# Validation: parameters must be mutually exclusive
if ($version -and $buildtype) {
    Write-Error "Parameters 'version' and 'buildtype' are mutually exclusive. Provide only one."
    exit 1
}

# Validate version format if provided
if ($version) {
    if ($version -notmatch '^\d+\.\d+\.\d+$') {
        Write-Error "Version must be in format #.#.# where # is any integer (e.g., 1.0.5)"
        exit 1
    }
}

# Validate buildtype if provided
if ($buildtype) {
    if ($buildtype -notin @('major', 'minor', 'fix')) {
        Write-Error "Buildtype must be one of: 'major', 'minor', or 'fix'"
        exit 1
    }
}

# Path to mod_info.json
$modInfoPath = ".\mod_data\Construction_Tweaks\mod_info.json"

# Verify mod_info.json exists
if (-not (Test-Path $modInfoPath)) {
    Write-Error "mod_info.json not found at $modInfoPath"
    exit 1
}

# Function to read current version from JSON
function Get-CurrentVersion {
    param([string]$path)
    try {
        $json = Get-Content $path -Raw | ConvertFrom-Json
        return $json[0].strModVersion
    }
    catch {
        Write-Error "Failed to parse mod_info.json: $_"
        exit 1
    }
}

# Function to update version in JSON (preserves formatting)
function Update-Version {
    param([string]$path, [string]$newVersion)
    
    try {
        $content = Get-Content $path -Raw
        # Use regex to replace the version value while preserving formatting
        $content = $content -replace '"strModVersion": "[^"]*"', "`"strModVersion`": `"$newVersion`""
        Set-Content -Path $path -Value $content
    }
    catch {
        Write-Error "Failed to update mod_info.json: $_"
        exit 1
    }
}

# Handle version parameter
if ($version) {
    Write-Host "Updating version to $version..."
    Update-Version -path $modInfoPath -newVersion $version
}

# Handle buildtype parameter
if ($buildtype) {
    $currentVersion = Get-CurrentVersion -path $modInfoPath
    $parts = $currentVersion -split '\.'
    
    if ($parts.Count -ne 3) {
        Write-Error "Current version is not in correct #.#.# format: $currentVersion"
        exit 1
    }
    
    switch ($buildtype) {
        'major' { $parts[0] = [int]$parts[0] + 1; $parts[1] = 0; $parts[2] = 0 }
        'minor' { $parts[1] = [int]$parts[1] + 1; $parts[2] = 0 }
        'fix' { $parts[2] = [int]$parts[2] + 1 }
    }
    
    $newVersion = $parts -join '.'
    Write-Host "Incrementing $buildtype version: $currentVersion -> $newVersion"
    Update-Version -path $modInfoPath -newVersion $newVersion
}

# Get the current version for the archive name
$currentVersion = Get-CurrentVersion -path $modInfoPath
Write-Host "Current version: $currentVersion"

# Build the project
Write-Host "Building ConstructionTweaks in Release configuration..."
dotnet build "ConstructionTweaks.csproj" -c Release
if ($LASTEXITCODE -ne 0) {
    Write-Error "Build failed with exit code $LASTEXITCODE"
    exit 1
}

# Create folder structure
Write-Host "Creating folder structure in release_archive..."
New-Item -ItemType Directory -Path ".\release_archive\BepInEx\plugins" -Force | Out-Null
New-Item -ItemType Directory -Path ".\release_archive\Ostranauts_Data\Mods" -Force | Out-Null

# Copy DLL to BepInEx/plugins
Write-Host "Copying DLL..."
$possibleDllPaths = @(
    ".\bin\Release\ConstructionTweaks.dll",
    ".\bin\Release\net472\ConstructionTweaks.dll",
    ".\bin\Release\net48\ConstructionTweaks.dll",
    ".\bin\Release\net6.0\ConstructionTweaks.dll"
)

$dllFound = $false
foreach ($dllPath in $possibleDllPaths) {
    if (Test-Path $dllPath) {
        Write-Host "Found DLL at: $dllPath"
        Copy-Item -Path $dllPath -Destination ".\release_archive\BepInEx\plugins\ConstructionTweaks.dll" -Force
        $dllFound = $true
        break
    }
}

if (-not $dllFound) {
    Write-Error "DLL not found. Checked the following locations:`n$($possibleDllPaths -join "`n")"
    exit 1
}

# Copy mod_data contents to Ostranauts_Data/Mods
Write-Host "Copying mod data..."
Copy-Item -Path ".\mod_data\*" -Destination ".\release_archive\Ostranauts_Data\Mods" -Recurse -Force

# Zip the release_archive
Write-Host "Creating archive ConstructionTweaks-v$currentVersion.zip..."
$zipPath = ".\ConstructionTweaks-v$currentVersion.zip"
Compress-Archive -Path ".\release_archive\*" -DestinationPath $zipPath -Force

if (Test-Path $zipPath) {
    $zipSize = (Get-Item $zipPath).Length / 1MB
    Write-Host "Build and archive complete!" -ForegroundColor Green
    Write-Host "Archive saved as: ConstructionTweaks-v$currentVersion.zip ($('{0:F2}' -f $zipSize) MB)"
}
else {
    Write-Error "Failed to create zip file"
    exit 1
}
