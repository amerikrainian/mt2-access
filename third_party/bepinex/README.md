# BepInEx (vendored loader)

Mod loader for the game. Source: https://github.com/BepInEx/BepInEx (license: `LICENSE`, MIT).

**Version: 5.4.23.4**, build `BepInEx_win_x64` (Unity Mono), the release asset
`BepInEx_win_x64_5.4.23.4.zip` from https://github.com/BepInEx/BepInEx/releases/tag/v5.4.23.4,
unmodified (sha256 `f881201b79da03e513bf97cdf39607ffa7f9e0d31a519b1aeeca8eb60f8309e7`, the digest
GitHub lists for it). Doorstop 4.4.1. Monster Train 2 is Unity 2022.3 on Mono, so the BepInEx 5
Mono build is the right one; BepInEx 6 is not needed.

## Install (into the game folder)
Run `setup-bepinex.ps1` from the repo root. It finds Monster Train 2 the way the build does and
extracts the zip over it: `winhttp.dll`, `doorstop_config.ini`, `.doorstop_version`, `changelog.txt`
and `BepInEx\core\`. Fully reversible (delete those).

## Release
`build_release.ps1` extracts this zip as the base of the release zip (its `changelog.txt`, BepInEx's
own release notes, is dropped), so a player's install gets exactly this loader.

## Updating
Download the new `BepInEx_win_x64_<version>.zip` from the releases page, replace the zip here, bump the
version and digest above. `build_release.ps1` and `setup-bepinex.ps1` pick up any
`BepInEx_win_x64_*.zip`, so keep exactly one.
