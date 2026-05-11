# Troubleshooting And Known Issues

## No Speech

If the mod does not speak:

1. Confirm the release zip was extracted into the Monster Train 2 game folder.
2. Confirm `winhttp.dll`, `doorstop_config.ini`, `prism.dll`, and `BepInEx` are present next to `MonsterTrain2.exe`.
3. Check the BepInEx log:

   ```text
   C:\Program Files (x86)\Steam\steamapps\common\Monster Train 2\BepInEx\LogOutput.log
   ```

4. Look for `Monster Train Accessibility ready`.

## Controller Notes

Controller support is wired through the game's controller input system. If a controller behaves unexpectedly, check Steam Input settings and verify the game itself is receiving the controller correctly. Since the author lacks a controller (BOO!) this path has yet to be tested as thoroughly.

## The Tutorial mentions scrolling and dragging things?

Yeh, it does. Short of abandoning the run you'll keep seeing the popups telling you information that might or might not be helpful. We found it confusing for the most part. You can definitely ignore all the parts that say to try out scrolling or dragging; we do none of that here.


