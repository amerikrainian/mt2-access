# build.ps1 - Build Monster Train Accessibility and deploy it into the game. Deploy itself is the host
# project's Debug post-build target (plugin + Core + Module into BepInEx\plugins\MonsterTrainAccessibility,
# prism.dll next to MonsterTrain2.exe); this script checks BepInEx is installed and runs the build. Close
# the game first for a HOST change (its DLL is locked while the game runs); Core/Module changes deploy
# fine with the game running and take effect on POST /reload.

param(
    [switch]$Help,
    [switch]$ModuleOnly
)

if ($Help) {
    Write-Host "Usage: .\build.ps1 [-ModuleOnly] [-Help]"
    Write-Host "  Builds the solution and deploys the mod into the game folder."
    Write-Host "  -ModuleOnly builds just the reloadable Core + Module (the hot loop)."
    Write-Host "  Run setup-bepinex.ps1 once first."
    exit 0
}

$ErrorActionPreference = "Stop"

# The game dir the build itself resolves (Directory.Build.props: Directory.Build.local.props,
# MONSTER_TRAIN_PATH, Steam's registry, the default path), so this check and the deploy agree.
$Game = (dotnet msbuild "$PSScriptRoot\src\MonsterTrainAccessibility\MonsterTrainAccessibility.csproj" -getProperty:GameDir).Trim()
if (-not $Game -or -not (Test-Path "$Game\MonsterTrain2.exe")) {
    Write-Host "ERROR: Monster Train 2 not found (GameDir='$Game'). Set MONSTER_TRAIN_PATH or create Directory.Build.local.props." -ForegroundColor Red
    exit 1
}
if (-not (Test-Path "$Game\BepInEx\core\BepInEx.dll")) {
    Write-Host "ERROR: BepInEx 5 is not installed at $Game\BepInEx. Run setup-bepinex.ps1 first." -ForegroundColor Red
    exit 1
}

if ($ModuleOnly) {
    dotnet build "$PSScriptRoot\src\MonsterTrainAccessibility.Module\MonsterTrainAccessibility.Module.csproj" -c Debug
} else {
    dotnet build "$PSScriptRoot\MonsterTrainAccessibility.slnx" -c Debug
}
if ($LASTEXITCODE -ne 0) {
    Write-Host "Build FAILED." -ForegroundColor Red
    exit 1
}
Write-Host ""
Write-Host "Done. Launch Monster Train 2 through Steam, or POST /reload if it is running." -ForegroundColor Cyan
