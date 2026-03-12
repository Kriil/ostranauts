[CmdletBinding()]
param(
    [string]$version = "",
    [string]$buildtype = "",
    [switch]$Help
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Get-Manifest {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ManifestPath
    )

    Get-Content -Path $ManifestPath -Raw | ConvertFrom-Json
}

function Save-Manifest {
    param(
        [Parameter(Mandatory = $true)]
        [object]$Manifest,
        [Parameter(Mandatory = $true)]
        [string]$ManifestPath
    )

    $Manifest | ConvertTo-Json -Depth 10 | Set-Content -Path $ManifestPath -Encoding utf8
}

function Show-Usage {
    Write-Host "Usage:" -ForegroundColor Cyan
    Write-Host "  .\build-release.ps1" -ForegroundColor White
    Write-Host "  .\build-release.ps1 -buildtype <major|minor|fix>" -ForegroundColor White
    Write-Host "  .\build-release.ps1 -version <#.#.#>" -ForegroundColor White
    Write-Host ""
    Write-Host "Behavior:" -ForegroundColor Cyan
    Write-Host "  Builds every mod listed in mods-manifest.json -> mods_build_list." -ForegroundColor White
    Write-Host "  Uses mod-release-version from mods-manifest.json as the archive version." -ForegroundColor White
    Write-Host "  -buildtype increments the current version and saves it back to mods-manifest.json." -ForegroundColor White
    Write-Host "  -version sets an explicit release version and saves it back to mods-manifest.json." -ForegroundColor White
    Write-Host "  Creates a combined archive named Mods-v<version>.zip in the repo root." -ForegroundColor White
    Write-Host ""
    Write-Host "Notes:" -ForegroundColor Cyan
    Write-Host "  -version and -buildtype are mutually exclusive." -ForegroundColor White
    Write-Host "  loading_order.json and each mod_info.json are generated from mods-manifest.json." -ForegroundColor White
    Write-Host ""
    Write-Host "Examples:" -ForegroundColor Cyan
    Write-Host "  .\build-release.ps1" -ForegroundColor White
    Write-Host "  .\build-release.ps1 -buildtype fix" -ForegroundColor White
    Write-Host "  .\build-release.ps1 -buildtype minor" -ForegroundColor White
    Write-Host "  .\build-release.ps1 -version 1.2.0" -ForegroundColor White
}

function Convert-ToDisplayName {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Value
    )

    $withSpaces = $Value -replace "_", " "
    ($withSpaces -creplace '(?<=[a-z0-9])([A-Z])', ' $1').Trim()
}

function Increment-Version {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Version,
        [Parameter(Mandatory = $true)]
        [ValidateSet("major", "minor", "fix")]
        [string]$BuildType
    )

    $parts = $Version -split '\.'
    if ($parts.Count -ne 3) {
        throw "Version '$Version' is not in #.#.# format."
    }

    switch ($BuildType) {
        "major" {
            $parts[0] = ([int]$parts[0] + 1).ToString()
            $parts[1] = "0"
            $parts[2] = "0"
        }
        "minor" {
            $parts[1] = ([int]$parts[1] + 1).ToString()
            $parts[2] = "0"
        }
        "fix" {
            $parts[2] = ([int]$parts[2] + 1).ToString()
        }
    }

    $parts -join "."
}

function Resolve-ReleaseVersion {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ManifestPath,
        [string]$RequestedVersion,
        [string]$BuildType
    )

    if ($RequestedVersion -and $BuildType) {
        throw "Parameters 'version' and 'buildtype' are mutually exclusive. Provide only one."
    }

    if ($RequestedVersion -and $RequestedVersion -notmatch '^\d+\.\d+\.\d+$') {
        throw "Version must be in format #.#.# where # is any integer (for example, 1.0.5)."
    }

    if ($BuildType -and $BuildType -notin @("major", "minor", "fix")) {
        throw "Buildtype must be one of: major, minor, fix."
    }

    $manifest = Get-Manifest -ManifestPath $ManifestPath
    $currentVersion = $manifest."mod-release-version"
    if (-not $currentVersion) {
        throw "mods-manifest.json is missing 'mod-release-version'."
    }

    $newVersion = $currentVersion
    if ($RequestedVersion) {
        $newVersion = $RequestedVersion
    }
    elseif ($BuildType) {
        $newVersion = Increment-Version -Version $currentVersion -BuildType $BuildType
    }

    if ($newVersion -ne $currentVersion) {
        $manifest."mod-release-version" = $newVersion
        Save-Manifest -Manifest $manifest -ManifestPath $ManifestPath
    }

    return $newVersion
}

function Get-ProjectMetadataByModFolder {
    param(
        [Parameter(Mandatory = $true)]
        [string]$WorkspaceRoot,
        [Parameter(Mandatory = $true)]
        [string]$ModFolderName
    )

    foreach ($directory in @(Get-ChildItem -Path $WorkspaceRoot -Directory)) {
        $projectFiles = @(Get-ChildItem -Path $directory.FullName -Filter *.csproj -File -ErrorAction SilentlyContinue)
        if ($projectFiles.Count -ne 1) {
            continue
        }

        $modDataRoot = Join-Path $directory.FullName "mod_data"
        if (-not (Test-Path -Path $modDataRoot)) {
            continue
        }

        $modContentDir = Join-Path $modDataRoot $ModFolderName
        if (-not (Test-Path -Path $modContentDir)) {
            continue
        }

        $projectFile = $projectFiles[0]
        return [pscustomobject]@{
            ProjectRoot = $directory.FullName
            ProjectFile = $projectFile
            ProjectName = [System.IO.Path]::GetFileNameWithoutExtension($projectFile.Name)
            ModFolderName = $ModFolderName
            ModContentDir = $modContentDir
            DisplayName = Convert-ToDisplayName -Value $ModFolderName
        }
    }

    throw "Could not find a project directory for mod folder '$ModFolderName'."
}

function Get-ModInfoFromManifest {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ManifestPath,
        [Parameter(Mandatory = $true)]
        [string]$DisplayName
    )

    $manifest = Get-Manifest -ManifestPath $ManifestPath
    $matchingEntry = $manifest.mod_list | Where-Object {
        $_.mod_info -and $_.mod_info.Count -gt 0 -and $_.mod_info[0].strName -eq $DisplayName
    } | Select-Object -First 1

    if (-not $matchingEntry) {
        throw "No mod_info entry for '$DisplayName' was found in '$ManifestPath'."
    }

    return $matchingEntry.mod_info[0]
}

function Get-LoadingOrderFromManifest {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ManifestPath
    )

    $manifest = Get-Manifest -ManifestPath $ManifestPath
    if (-not $manifest.loading_order -or $manifest.loading_order.Count -eq 0) {
        throw "mods-manifest.json does not contain a loading_order entry."
    }

    return $manifest.loading_order[0]
}

function Find-BuiltDll {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ProjectRoot,
        [Parameter(Mandatory = $true)]
        [string]$DllName
    )

    $dll = Get-ChildItem -Path (Join-Path $ProjectRoot "bin\Release") -Recurse -File -Filter $DllName |
        Sort-Object -Property LastWriteTime -Descending |
        Select-Object -First 1

    if (-not $dll) {
        throw "Could not find built DLL '$DllName' under '$ProjectRoot\bin\Release'."
    }

    return $dll
}

$workspaceRoot = $PSScriptRoot
$manifestPath = Join-Path $workspaceRoot "mods-manifest.json"

if ($Help) {
    Show-Usage
    exit 0
}

$manifest = Get-Manifest -ManifestPath $manifestPath
$modsToBuild = @($manifest.mods_build_list)

if ($modsToBuild.Count -eq 0) {
    throw "mods-manifest.json does not contain any entries in mods_build_list."
}

$releaseVersion = Resolve-ReleaseVersion -ManifestPath $manifestPath -RequestedVersion $version -BuildType $buildtype
$releaseRootName = "Mods-v$releaseVersion"
$releaseArchiveRoot = Join-Path $workspaceRoot "release_archive"
$stagingRoot = $releaseArchiveRoot
$pluginsRoot = Join-Path $stagingRoot "BepInEx\plugins"
$modsRoot = Join-Path $stagingRoot "Ostranauts_Data\Mods"
$zipPath = Join-Path $workspaceRoot "$releaseRootName.zip"

Write-Host "Release version: $releaseVersion" -ForegroundColor Cyan
Write-Host "Preparing release archive staging at $stagingRoot" -ForegroundColor Cyan

try {
    if (Test-Path -Path $stagingRoot) {
        Remove-Item -Path $stagingRoot -Recurse -Force
    }

    if (Test-Path -Path $zipPath) {
        Remove-Item -Path $zipPath -Force
    }

    New-Item -ItemType Directory -Path $pluginsRoot -Force | Out-Null
    New-Item -ItemType Directory -Path $modsRoot -Force | Out-Null

    foreach ($modFolderName in $modsToBuild) {
        $metadata = Get-ProjectMetadataByModFolder -WorkspaceRoot $workspaceRoot -ModFolderName $modFolderName
        $dllName = "$($metadata.ProjectName).dll"

        Write-Host "Building $($metadata.ProjectName)..." -ForegroundColor Cyan
        dotnet build $metadata.ProjectFile.FullName -c Release
        if ($LASTEXITCODE -ne 0) {
            throw "Build failed for $($metadata.ProjectName) with exit code $LASTEXITCODE."
        }

        $builtDll = Find-BuiltDll -ProjectRoot $metadata.ProjectRoot -DllName $dllName
        Write-Host "Copying $dllName to release archive..." -ForegroundColor Cyan
        Copy-Item -Path $builtDll.FullName -Destination (Join-Path $pluginsRoot $dllName) -Force

        $destinationModPath = Join-Path $modsRoot $metadata.ModFolderName
        Write-Host "Copying mod data for $($metadata.ModFolderName)..." -ForegroundColor Cyan
        New-Item -ItemType Directory -Path $destinationModPath -Force | Out-Null
        Copy-Item -Path (Join-Path $metadata.ModContentDir "*") -Destination $destinationModPath -Recurse -Force

        $modInfo = Get-ModInfoFromManifest -ManifestPath $manifestPath -DisplayName $metadata.DisplayName
        $modInfoPath = Join-Path $destinationModPath "mod_info.json"
        $modInfoArray = @($modInfo)
        Write-Host "Generating mod_info.json for $($metadata.ModFolderName)..." -ForegroundColor Cyan
        ConvertTo-Json -InputObject $modInfoArray -Depth 10 | Set-Content -Path $modInfoPath -Encoding utf8
    }

    $loadingOrder = Get-LoadingOrderFromManifest -ManifestPath $manifestPath
    $loadingOrderPath = Join-Path $modsRoot "loading_order.json"
    Write-Host "Generating loading_order.json..." -ForegroundColor Cyan
    ConvertTo-Json -InputObject @($loadingOrder) -Depth 10 | Set-Content -Path $loadingOrderPath -Encoding utf8

    Write-Host "Creating archive $releaseRootName.zip..." -ForegroundColor Cyan
    Compress-Archive -Path (Join-Path $stagingRoot "*") -DestinationPath $zipPath -Force

    if (-not (Test-Path -Path $zipPath)) {
        throw "Failed to create zip file '$zipPath'."
    }

    $zipSizeMb = (Get-Item -Path $zipPath).Length / 1MB
    Write-Host "Build and release archive complete." -ForegroundColor Green
    Write-Host "Archive saved as: $releaseRootName.zip ($('{0:F2}' -f $zipSizeMb) MB)"
}
finally {
    if (Test-Path -Path $stagingRoot) {
        Remove-Item -Path $stagingRoot -Recurse -Force
    }
}
