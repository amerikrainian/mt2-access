#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
cd "$SCRIPT_DIR"

echo "=== Building mod (Release) ==="
dotnet build -c Release

echo "=== Building documentation ==="
mdbook build docs_src

echo "=== Creating release zip ==="
rm -f MonsterTrainAccessibility.zip
powershell.exe -NoProfile -ExecutionPolicy Bypass -Command "\$zip = Join-Path (Get-Location) 'MonsterTrainAccessibility.zip'; Push-Location release; Compress-Archive -LiteralPath @('BepInEx', '.doorstop_version', 'doorstop_config.ini', 'prism.dll', 'winhttp.dll') -DestinationPath \$zip -CompressionLevel Optimal; Pop-Location"

echo "=== Adding docs to release zip ==="
python scripts/add_docs_to_release.py

echo "=== Done ==="
echo "Release zip: MonsterTrainAccessibility.zip"
