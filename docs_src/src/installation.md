# Installation

## Installer

The recommended install path is the installer. Download `MonsterTrainAccessibilityInstaller.exe` from the
[latest releases page](https://github.com/amerikrainian/mt2-access/releases/latest), run it, and choose your Monster Train 2 game folder when prompted.

The default Steam path on Windows, which the installer assumes to be the case, is:

```text
C:\Program Files (x86)\Steam\steamapps\common\Monster Train 2
```

After installation, launch Monster Train 2. If installation worked, the mod should initialize when the game starts and begin speaking UI focus.

## Manual Installation

1. Download the latest `MonsterTrainAccessibility-vX.Y.Z.zip` release (X.Y.Z is the version).
2. Extract the zip into your Monster Train 2 game folder.
3. The default Steam path on Windows is:

   ```text
   C:\Program Files (x86)\Steam\steamapps\common\Monster Train 2
   ```

4. After extraction, the game folder should contain files such as `winhttp.dll`, `doorstop_config.ini`, `prism.dll`, and a `BepInEx` folder, with the mod in `BepInEx\plugins\MonsterTrainAccessibility`.
5. Launch Monster Train 2.

If installation worked, the mod should initialize when the game starts and begin speaking UI focus.

## Updating

To update, either launch the installer and hit update or, if doing a manual install extract the latest release zip over the existing game folder and replace files when prompted.

Versions before 0.3.0 installed the mod as a single file, `BepInEx\plugins\MonsterTrainAccessibility.dll`. The installer removes it on update. Updating by hand, delete these from `BepInEx\plugins` first, or the old and new versions load side by side:

- `MonsterTrainAccessibility.dll`, `MonsterTrainAccessibility.deps.json` and `MonsterTrainAccessibility.pdb`
- the `Localization` folder
- `System.Text.Json.dll`, `System.Text.Encodings.Web.dll`, `System.Memory.dll`, `System.Buffers.dll`, `System.Runtime.CompilerServices.Unsafe.dll`, `Microsoft.Bcl.AsyncInterfaces.dll`, `System.Numerics.Vectors.dll` and `System.Threading.Tasks.Extensions.dll`, unless another mod needs them

## Uninstalling

The installer has an uninstall option, but if you'd like to manually remove the mod, remove the following from the game's directory.

- `winhttp.dll`
- `doorstop_config.ini`
- `.doorstop_version`
- `prism.dll`
- the `BepInEx\plugins\MonsterTrainAccessibility` folder
- the `MT2AccessibilityDocs` folder
