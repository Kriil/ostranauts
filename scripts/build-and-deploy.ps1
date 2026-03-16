[CmdletBinding()]
param(
    [string]$Project,
    [string]$GamePath = "C:\Steam\steamapps\common\Ostranauts",
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

    ConvertTo-Json -InputObject $Manifest -Depth 10 | Set-Content -Path $ManifestPath -Encoding utf8
}

function Get-AvailableProjects {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ManifestPath
    )

    $manifest = Get-Manifest -ManifestPath $ManifestPath
    if (-not $manifest.mods_build_list) {
        return @()
    }

    @($manifest.mods_build_list)
}

function Ensure-BuildListProject {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ManifestPath,
        [Parameter(Mandatory = $true)]
        [string]$ProjectEntry
    )

    $manifest = Get-Manifest -ManifestPath $ManifestPath

    if (-not $manifest.mods_build_list) {
        $manifest | Add-Member -MemberType NoteProperty -Name mods_build_list -Value @()
    }

    if (@($manifest.mods_build_list) -contains $ProjectEntry) {
        return
    }

    $manifest.mods_build_list = @($manifest.mods_build_list) + @($ProjectEntry)
    Save-Manifest -Manifest $manifest -ManifestPath $ManifestPath
}

function Get-ProjectMetadata {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ProjectRoot
    )

    $projectFile = Get-ChildItem -Path $ProjectRoot -Filter *.csproj -File | Select-Object -First 1
    if (-not $projectFile) {
        throw "No .csproj file was found in '$ProjectRoot'."
    }

    $modDataRoot = Join-Path $ProjectRoot "mod_data"
    $modContentDirs = @(Get-ChildItem -Path $modDataRoot -Directory)
    if ($modContentDirs.Count -ne 1) {
        throw "Expected exactly one mod payload directory under '$modDataRoot', found $($modContentDirs.Count)."
    }

    [pscustomobject]@{
        ProjectRoot = $ProjectRoot
        ProjectFile = $projectFile
        ProjectName = [System.IO.Path]::GetFileNameWithoutExtension($projectFile.Name)
        ModDataRoot = $modDataRoot
        ModContentDir = $modContentDirs[0]
        ModFolderName = $modContentDirs[0].Name
    }
}

function Find-ProjectRoot {
    param(
        [Parameter(Mandatory = $true)]
        [string]$WorkspaceRoot,
        [Parameter(Mandatory = $true)]
        [string]$ProjectIdentifier
    )

    $candidatePath = $ProjectIdentifier
    if ([System.IO.Path]::IsPathRooted($candidatePath)) {
        $resolvedCandidate = Resolve-Path -Path $candidatePath -ErrorAction SilentlyContinue
        if ($resolvedCandidate) {
            return $resolvedCandidate.Path
        }
    }

    $projectDirPath = Join-Path $WorkspaceRoot $ProjectIdentifier
    $resolvedProjectDir = Resolve-Path -Path $projectDirPath -ErrorAction SilentlyContinue
    if ($resolvedProjectDir) {
        return $resolvedProjectDir.Path
    }

    foreach ($directory in @(Get-ChildItem -Path $WorkspaceRoot -Directory)) {
        $projectFileCount = (Get-ChildItem -Path $directory.FullName -Filter *.csproj -File -ErrorAction SilentlyContinue | Measure-Object).Count
        if ($projectFileCount -ne 1 -or -not (Test-Path -Path (Join-Path $directory.FullName "mod_data"))) {
            continue
        }

        $metadata = Get-ProjectMetadata -ProjectRoot $directory.FullName
        if ($directory.Name -eq $ProjectIdentifier -or $metadata.ProjectName -eq $ProjectIdentifier -or $metadata.ModFolderName -eq $ProjectIdentifier) {
            return $directory.FullName
        }
    }

    return $null
}

function Resolve-ProjectRoot {
    param(
        [Parameter(Mandatory = $true)]
        [string]$WorkspaceRoot,
        [Parameter(Mandatory = $true)]
        [string]$ManifestPath,
        [string]$RequestedProject
    )

    $availableProjects = Get-AvailableProjects -ManifestPath $ManifestPath

    if ($RequestedProject) {
        $resolvedProjectRoot = Find-ProjectRoot -WorkspaceRoot $WorkspaceRoot -ProjectIdentifier $RequestedProject
        if (-not $resolvedProjectRoot) {
            throw "Project '$RequestedProject' was not found."
        }

        $metadata = Get-ProjectMetadata -ProjectRoot $resolvedProjectRoot
        Ensure-BuildListProject -ManifestPath $ManifestPath -ProjectEntry $metadata.ModFolderName
        return $resolvedProjectRoot
    }

    $currentPath = (Get-Location).Path
    $resolvedRoot = (Resolve-Path -Path $WorkspaceRoot).Path

    if ($currentPath -eq $resolvedRoot) {
        throw "When run from the root directory, -Project is required. Available projects: $($availableProjects -join ', ')"
    }

    $cursor = $currentPath
    while ($cursor.StartsWith($resolvedRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
        $projectFileCount = (Get-ChildItem -Path $cursor -Filter *.csproj -File -ErrorAction SilentlyContinue | Measure-Object).Count
        if ($projectFileCount -eq 1 -and (Test-Path -Path (Join-Path $cursor "mod_data"))) {
            $metadata = Get-ProjectMetadata -ProjectRoot $cursor
            Ensure-BuildListProject -ManifestPath $ManifestPath -ProjectEntry $metadata.ModFolderName
            return $cursor
        }

        if ($cursor -eq $resolvedRoot) {
            break
        }

        $parent = Split-Path -Path $cursor -Parent
        if (-not $parent -or $parent -eq $cursor) {
            break
        }

        $cursor = $parent
    }

    throw "Could not determine a project from '$currentPath'. Run the script from a mod project directory or pass -Project. Available projects: $($availableProjects -join ', ')"
}

function Convert-ToDisplayName {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Value
    )

    $withSpaces = $Value -replace "_", " "
    ($withSpaces -creplace '(?<=[a-z0-9])([A-Z])', ' $1').Trim()
}

function Show-Usage {
    param(
        [string[]]$AvailableProjects
    )

    Write-Host "Usage:" -ForegroundColor Cyan
    Write-Host "  From a project directory:" -ForegroundColor Cyan
    Write-Host "    ..\build-and-deploy.ps1 [-GamePath <path>]" -ForegroundColor White
    Write-Host "  From the repo root:" -ForegroundColor Cyan
    Write-Host "    .\build-and-deploy.ps1 -Project <project> [-GamePath <path>]" -ForegroundColor White
    Write-Host "" 
    Write-Host "Accepted -Project values:" -ForegroundColor Cyan
    if ($AvailableProjects -and $AvailableProjects.Count -gt 0) {
        Write-Host "  $($AvailableProjects -join ', ')" -ForegroundColor White
    }
    else {
        Write-Host "  None currently listed in mods-manifest.json" -ForegroundColor White
    }
    Write-Host ""
    Write-Host "Examples:" -ForegroundColor Cyan
    Write-Host "  .\build-and-deploy.ps1 -Project Room_Effects" -ForegroundColor White
    Write-Host "  .\build-and-deploy.ps1 -Project ConstructionTweaks" -ForegroundColor White
    Write-Host "  .\build-and-deploy.ps1 -Project Room_Effects -GamePath D:\Games\Ostranauts" -ForegroundColor White
}

function Increment-Version {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Version
    )

    $parts = $Version.Split(".")
    if ($parts.Count -eq 0) {
        throw "Version '$Version' is not valid."
    }

    $lastIndex = $parts.Count - 1
    $lastValue = 0
    if (-not [int]::TryParse($parts[$lastIndex], [ref]$lastValue)) {
        throw "Version '$Version' must end with an integer."
    }

    $parts[$lastIndex] = ($lastValue + 1).ToString()
    $parts -join "."
}

function Update-Manifest {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ManifestPath,
        [Parameter(Mandatory = $true)]
        [string]$DisplayName,
        [Parameter(Mandatory = $true)]
        [string]$LoadOrderName
    )

    $manifest = Get-Manifest -ManifestPath $ManifestPath

    if (-not $manifest.mod_list) {
        $manifest | Add-Member -MemberType NoteProperty -Name mod_list -Value @()
    }

    $matchingEntry = $manifest.mod_list | Where-Object {
        $_.mod_info -and $_.mod_info.Count -gt 0 -and $_.mod_info[0].strName -eq $DisplayName
    } | Select-Object -First 1

    if ($matchingEntry) {
        $modVersion = Increment-Version -Version $matchingEntry.mod_info[0].strModVersion
        $matchingEntry.mod_info[0].strModVersion = $modVersion
    }
    else {
        $newEntry = [pscustomobject][ordered]@{
            mod_info = @(
                [pscustomobject][ordered]@{
                    strName = $DisplayName
                    strAuthor = "Kriil"
                    strModURL = ""
                    strGameVersion = "0.14.5.20"
                    strModVersion = "1.0.0"
                    strNotes = ""
                }
            )
        }

        $manifest.mod_list = @($manifest.mod_list) + @($newEntry)
        $modVersion = $newEntry.mod_info[0].strModVersion
    }

    if (-not $manifest.loading_order) {
        $manifest | Add-Member -MemberType NoteProperty -Name loading_order -Value @()
    }

    if ($manifest.loading_order.Count -eq 0) {
        $manifest.loading_order = @(
            [pscustomobject][ordered]@{
                strName = "Mod Loading Order"
                strNotes = "Controls the order mods are loaded. 'core' refers to base game data and should usually be first."
                aLoadOrder = @("core")
                aIgnorePatterns = @()
            }
        )
    }

    $loadOrderEntry = $manifest.loading_order[0]
    if (-not $loadOrderEntry.aLoadOrder) {
        $loadOrderEntry.aLoadOrder = @()
    }

    if ($loadOrderEntry.aLoadOrder -notcontains $LoadOrderName) {
        $loadOrderEntry.aLoadOrder = @($loadOrderEntry.aLoadOrder) + @($LoadOrderName)
    }

    Save-Manifest -Manifest $manifest -ManifestPath $ManifestPath
    return $modVersion
}

function Update-PluginVersion {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ProjectRoot,
        [Parameter(Mandatory = $true)]
        [string]$Version
    )

    $pluginFiles = @(Get-ChildItem -Path $ProjectRoot -Recurse -File -Filter "Plugin.cs")
    $pluginVersionPattern = 'public const string PluginVersion = "[^"]+";'

    $matchingPluginFiles = @(
        $pluginFiles | Where-Object {
            $content = Get-Content -Path $_.FullName -Raw
            $content -match $pluginVersionPattern
        }
    )

    if ($matchingPluginFiles.Count -eq 0) {
        throw "Could not find a Plugin.cs file with a PluginVersion constant under '$ProjectRoot'."
    }

    if ($matchingPluginFiles.Count -gt 1) {
        throw "Found multiple Plugin.cs files with a PluginVersion constant under '$ProjectRoot': $($matchingPluginFiles.FullName -join ', ')"
    }

    $pluginPath = $matchingPluginFiles[0].FullName
    $pluginContent = Get-Content -Path $pluginPath -Raw
    $updatedContent = [System.Text.RegularExpressions.Regex]::Replace(
        $pluginContent,
        $pluginVersionPattern,
        "public const string PluginVersion = `"$Version`";",
        1
    )

    if ($updatedContent -ceq $pluginContent) {
        Write-Host "Plugin.cs already matches manifest version $Version" -ForegroundColor DarkGray
        return
    }

    Set-Content -Path $pluginPath -Value $updatedContent -Encoding utf8
    Write-Host "Updated PluginVersion in $pluginPath to $Version" -ForegroundColor Cyan
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

$workspaceRoot = Split-Path -Path $PSScriptRoot -Parent
$manifestPath = Join-Path $workspaceRoot "mods-manifest.json"
$availableProjects = Get-AvailableProjects -ManifestPath $manifestPath

if ($Help) {
    Show-Usage -AvailableProjects $availableProjects
    exit 0
}

$projectRoot = Resolve-ProjectRoot -WorkspaceRoot $workspaceRoot -ManifestPath $manifestPath -RequestedProject $Project
$projectMetadata = Get-ProjectMetadata -ProjectRoot $projectRoot
$projectFile = $projectMetadata.ProjectFile
$projectName = $projectMetadata.ProjectName
$modContentDir = $projectMetadata.ModContentDir
$modFolderName = $projectMetadata.ModFolderName
$displayName = Convert-ToDisplayName -Value $projectName

Write-Host "Project: $projectName" -ForegroundColor Cyan
Write-Host "Project root: $projectRoot" -ForegroundColor Cyan
Write-Host "Updating mods-manifest.json..." -ForegroundColor Cyan
$modVersion = Update-Manifest -ManifestPath $manifestPath -DisplayName $displayName -LoadOrderName $modFolderName
Update-PluginVersion -ProjectRoot $projectRoot -Version $modVersion

Write-Host "Building $($projectFile.Name)..." -ForegroundColor Cyan
dotnet build $projectFile.FullName -c Release
if ($LASTEXITCODE -ne 0) {
    throw "Build failed with exit code $LASTEXITCODE."
}

$builtDll = Get-ChildItem -Path (Join-Path $projectRoot "bin\Release") -Recurse -File -Filter "$projectName.dll" |
    Sort-Object -Property LastWriteTime -Descending |
    Select-Object -First 1

if (-not $builtDll) {
    throw "Could not find built DLL '$projectName.dll' under '$projectRoot\bin\Release'."
}

$modsRoot = Join-Path $GamePath "Ostranauts_Data\Mods"
$pluginsRoot = Join-Path $GamePath "BepInEx\plugins"
$destinationModPath = Join-Path $modsRoot $modFolderName
$destinationDllPath = Join-Path $pluginsRoot $builtDll.Name

Write-Host "Deploying mod data to $destinationModPath" -ForegroundColor Cyan
New-Item -ItemType Directory -Path $destinationModPath -Force | Out-Null
Copy-Item -Path (Join-Path $modContentDir.FullName "*") -Destination $destinationModPath -Recurse -Force

$modInfo = Get-ModInfoFromManifest -ManifestPath $manifestPath -DisplayName $displayName
$destinationModInfoPath = Join-Path $destinationModPath "mod_info.json"
Write-Host "Generating mod_info.json at $destinationModInfoPath" -ForegroundColor Cyan
$modInfoArray = @($modInfo)
ConvertTo-Json -InputObject $modInfoArray -Depth 10 | Set-Content -Path $destinationModInfoPath -Encoding utf8

Write-Host "Deploying DLL to $destinationDllPath" -ForegroundColor Cyan
New-Item -ItemType Directory -Path $pluginsRoot -Force | Out-Null
Copy-Item -Path $builtDll.FullName -Destination $destinationDllPath -Force

Write-Host "Build and deployment completed successfully." -ForegroundColor Green
