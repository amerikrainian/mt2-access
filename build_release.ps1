# Build the distributable mod zip: the vendored BepInEx 5 game-folder layout (BepInEx\core\,
# winhttp.dll, doorstop_config.ini, .doorstop_version) plus the Release plugin output under
# BepInEx\plugins\MonsterTrainAccessibility (the host, Contracts, Core and Module dlls, lang files,
# the sound cues under assets\audio),
# prism.dll next to MonsterTrain2.exe, and the rendered mdbook manual under MT2AccessibilityDocs
# when docs_src exists. The zip root IS the game folder, so the installer (and a manual user)
# extracts it straight into the game dir.
#
# The Roslyn scripting assemblies (and the facades _RoslynFacade adds) in the host's output are the
# Debug-only dev server's and are not shipped: the Release host never instantiates it. No
# BepInEx.cfg is shipped either: BepInEx 5 keeps its console window closed by default, and a
# shipped file would overwrite a player's own settings.
#
# The Release build compiles against the game's managed assemblies and its installed BepInEx, found
# the way Directory.Build.props finds them (-GameDir, Directory.Build.local.props,
# MONSTER_TRAIN_PATH, Steam's registry, the default path).
#
# Adapted from the Non-Visual Calculus installer by Rashad Naqeeb (MIT),
# https://github.com/rashadnaqeeb/NonVisualCalculus

param(
    [string]$GameDir
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$propsPath = Join-Path $scriptDir "Directory.Build.props"
$releaseDir = Join-Path $scriptDir "releases"
$stageDir = Join-Path $scriptDir "obj\release-stage"

[xml]$props = Get-Content $propsPath
$versionNode = $props.SelectSingleNode("/Project/PropertyGroup/Version")
if ($null -eq $versionNode -or [string]::IsNullOrWhiteSpace($versionNode.InnerText)) {
    throw "Could not read Version from $propsPath"
}
$version = $versionNode.InnerText.Trim()

# The vendored loader is a zip whose root is the game-folder layout; its changelog.txt is
# BepInEx's own release notes and is not shipped.
$bepinexZip = Get-ChildItem (Join-Path $scriptDir "third_party\bepinex\BepInEx_win_x64_*.zip") |
    Select-Object -First 1
$prismDll = Join-Path $scriptDir "third_party\prism\prism.dll"
$hostOutDir = Join-Path $scriptDir "src\MonsterTrainAccessibility\bin\Release\netstandard2.1"
$coreOutDir = Join-Path $scriptDir "src\MonsterTrainAccessibility.Core\bin\Release\netstandard2.0"
$moduleOutDir = Join-Path $scriptDir "src\MonsterTrainAccessibility.Module\bin\Release\netstandard2.1"
$docsDir = Join-Path $scriptDir "docs_src"
$langDir = Join-Path $scriptDir "lang"
$zipPath = Join-Path $releaseDir "MonsterTrainAccessibility-v$version.zip"

if ($null -eq $bepinexZip) {
    throw "Vendored BepInEx zip not found under third_party\bepinex"
}
if (-not (Test-Path $prismDll)) {
    throw "Required file not found: $prismDll"
}

Push-Location $scriptDir
try {
    # The dlls are picked from the output dirs by name, so a stale DLL there (a renamed or removed
    # assembly dotnet build no longer owns) would ship. Start them empty.
    foreach ($dir in @($hostOutDir, $coreOutDir, $moduleOutDir)) {
        if (Test-Path $dir) {
            Remove-Item -LiteralPath $dir -Recurse -Force
        }
    }

    $buildArgs = @("MonsterTrainAccessibility.slnx", "-c", "Release", "-v:minimal")
    if ($GameDir) {
        $buildArgs += "-p:GameDir=$GameDir"
    }
    dotnet build @buildArgs
    if ($LASTEXITCODE -ne 0) {
        throw "Release build failed with exit code $LASTEXITCODE"
    }

    $hasDocs = Test-Path (Join-Path $docsDir "book.toml")
    if ($hasDocs) {
        mdbook build $docsDir
        if ($LASTEXITCODE -ne 0) {
            throw "Docs build failed with exit code $LASTEXITCODE"
        }
    }

    $modDlls = @(
        (Join-Path $hostOutDir "MonsterTrainAccessibility.dll"),
        (Join-Path $hostOutDir "MonsterTrainAccessibility.Contracts.dll"),
        (Join-Path $coreOutDir "MonsterTrainAccessibility.Core.dll"),
        (Join-Path $moduleOutDir "MonsterTrainAccessibility.Module.dll")
    )
    foreach ($required in $modDlls) {
        if (-not (Test-Path $required)) {
            throw "Release build output not found: $required"
        }
    }

    if (Test-Path $stageDir) {
        Remove-Item -LiteralPath $stageDir -Recurse -Force
    }
    New-Item -ItemType Directory -Force $stageDir | Out-Null
    New-Item -ItemType Directory -Force $releaseDir | Out-Null

    Expand-Archive -Path $bepinexZip.FullName -DestinationPath $stageDir -Force
    $loaderNotes = Join-Path $stageDir "changelog.txt"
    if (Test-Path $loaderNotes) {
        Remove-Item -LiteralPath $loaderNotes -Force
    }
    foreach ($required in @("winhttp.dll", "doorstop_config.ini", "BepInEx\core\BepInEx.dll")) {
        if (-not (Test-Path (Join-Path $stageDir $required))) {
            throw "The vendored BepInEx zip lacks $required"
        }
    }

    # The same file set the Debug post-build targets deploy, minus the dev server's Roslyn deps.
    $pluginDir = Join-Path $stageDir "BepInEx\plugins\MonsterTrainAccessibility"
    New-Item -ItemType Directory -Force $pluginDir | Out-Null
    foreach ($dll in $modDlls) {
        Copy-Item -LiteralPath $dll -Destination $pluginDir
    }
    Copy-Item -LiteralPath $prismDll -Destination $stageDir

    if (Test-Path $langDir) {
        $langFiles = @(Get-ChildItem (Join-Path $langDir "*.txt"))
        if ($langFiles.Count -gt 0) {
            $stageLangDir = Join-Path $pluginDir "lang"
            New-Item -ItemType Directory -Force $stageLangDir | Out-Null
            Copy-Item -Path $langFiles.FullName -Destination $stageLangDir
        }
    }

    # The sound cues, one wav per cue, under assets\audio\<group>\ beside the dlls.
    $audioDir = Join-Path $scriptDir "assets\audio"
    if (-not (Test-Path $audioDir)) {
        throw "Required folder not found: $audioDir"
    }
    $stageAssetsDir = Join-Path $pluginDir "assets"
    New-Item -ItemType Directory -Force $stageAssetsDir | Out-Null
    Copy-Item -LiteralPath $audioDir -Destination (Join-Path $stageAssetsDir "audio") -Recurse

    if ($hasDocs) {
        # Packaging pattern adapted from SayTheSpire2: https://github.com/bradjrenshaw/say-the-spire2
        Copy-Item -Path (Join-Path $docsDir "book") -Destination (Join-Path $stageDir "MT2AccessibilityDocs") -Recurse
    }

    if (Test-Path $zipPath) {
        Remove-Item -LiteralPath $zipPath -Force
    }
    # The stage's top-level entries, listed with -Force so a hidden file is never left out.
    Compress-Archive -LiteralPath (Get-ChildItem -LiteralPath $stageDir -Force).FullName -DestinationPath $zipPath -Force

    Remove-Item -LiteralPath $stageDir -Recurse -Force

    Write-Host "Release zip: $zipPath"
}
finally {
    Pop-Location
}
