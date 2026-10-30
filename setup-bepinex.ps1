# setup-bepinex.ps1 - Install the vendored BepInEx 5 (third_party/bepinex) into the Monster Train 2
# folder. Idempotent: safe to re-run (e.g. after a game update or a Steam file check wipes it). After
# this, run build.ps1 to deploy the mod. BepInEx 5 on Mono needs no first-launch generation step; the
# build compiles against the game's own managed assemblies.

param(
    [string]$GameDir
)

$ErrorActionPreference = "Stop"

# --- Locate the game install exactly as the build does: Directory.Build.props resolves GameDir
# (-GameDir, Directory.Build.local.props, MONSTER_TRAIN_PATH, Steam's registry, the default path).
$Game = $GameDir
if (-not $Game) {
    $Game = (dotnet msbuild "$PSScriptRoot\src\MonsterTrainAccessibility\MonsterTrainAccessibility.csproj" -getProperty:GameDir).Trim()
}
if (-not $Game -or -not (Test-Path "$Game\MonsterTrain2.exe")) {
    Write-Host "ERROR: Monster Train 2 not found (GameDir='$Game')." -ForegroundColor Red
    Write-Host "Set MONSTER_TRAIN_PATH, pass -GameDir, or create Directory.Build.local.props." -ForegroundColor Red
    exit 1
}

$Zip = Get-ChildItem "$PSScriptRoot\third_party\bepinex\BepInEx_win_x64_*.zip" | Select-Object -First 1
if (-not $Zip) {
    Write-Host "ERROR: vendored BepInEx zip not found under third_party\bepinex" -ForegroundColor Red
    exit 1
}

Write-Host "Installing $($Zip.Name) into $Game ..." -ForegroundColor Cyan

# The zip root is the game-folder layout (winhttp.dll, doorstop_config.ini, .doorstop_version,
# BepInEx\core\), so it extracts straight into the game folder.
Expand-Archive -Path $Zip.FullName -DestinationPath $Game -Force

# --- No BepInEx console window: the mod speaks, and a second window only steals focus from the
# screen reader. BepInEx 5 defaults it off, so only a BepInEx.cfg someone switched on is patched;
# a missing one is left for BepInEx to write with its defaults on the first launch.
$Config = "$Game\BepInEx\config\BepInEx.cfg"
if (Test-Path $Config) {
    $text = [IO.File]::ReadAllText($Config)
    $patched = [regex]::Replace($text, "(\[Logging\.Console\][^\[]*?^Enabled = )true", "`$1false", "Multiline, Singleline")
    if ($patched -ne $text) {
        [IO.File]::WriteAllText($Config, $patched)
        Write-Host "BepInEx console window disabled ([Logging.Console] Enabled = false in BepInEx\config\BepInEx.cfg)." -ForegroundColor Cyan
    }
}

Write-Host ""
Write-Host "BepInEx installed. Now run build.ps1 to deploy the mod, then launch the game through Steam" -ForegroundColor Cyan
Write-Host "(steam.exe -applaunch 2742830)." -ForegroundColor Cyan
