@echo off
setlocal

pushd "%~dp0" || exit /b 1

echo === Building mod (Release) ===
dotnet build -c Release
if errorlevel 1 goto :fail

echo === Building documentation ===
mdbook build docs_src
if errorlevel 1 goto :fail

echo === Creating release zip ===
powershell -NoProfile -ExecutionPolicy Bypass -Command "$zip = Join-Path (Get-Location) 'MonsterTrainAccessibility.zip'; if (Test-Path -LiteralPath $zip) { Remove-Item -LiteralPath $zip -Force }; Push-Location release; Compress-Archive -LiteralPath @('BepInEx', '.doorstop_version', 'doorstop_config.ini', 'prism.dll', 'winhttp.dll') -DestinationPath $zip -CompressionLevel Optimal; Pop-Location"
if errorlevel 1 goto :fail

echo === Adding docs to release zip ===
python scripts\add_docs_to_release.py
if errorlevel 1 goto :fail

echo === Done ===
echo Release zip: MonsterTrainAccessibility.zip
popd
exit /b 0

:fail
set "exit_code=%errorlevel%"
popd
exit /b %exit_code%
