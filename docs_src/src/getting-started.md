# Getting Started

After installing the mod, launch Monster Train 2. You will hear "Monster Train Accessibility" and its version once the game is up, then the title screen.

## Basic Navigation

Use the arrow keys, the D-pad or the left stick to move. Enter or A activates the focused control, Escape or B backs out. Tab and Shift+Tab (the bumpers on a controller) move between groups of controls, and typing letters jumps to the matching item in the focused list. The full list is on the [Controls](controls.md) page.

While the mod's navigation is on, the game hears none of your keys except a few in battle. Ctrl+Shift+A hands the keyboard and controller back to the game, and takes them again.

## Help

Press F1 for the keys that do something on the current screen, on the focused control, right now. Move through them with the arrows; Enter on a row runs it. F1 or Escape closes the list.

## Buffers

Many controls have more information than should be spoken every time focus lands on them. The mod puts it into buffers, which you read without moving focus: Ctrl+Up and Ctrl+Down step through the lines, Ctrl+Left and Ctrl+Right switch buffers. On a controller, the right stick. See [Buffers](buffers.md).

## The Mod Menu

Press Ctrl+Shift+M (LT+Start) for the mod menu:

- **Settings**: whether list items speak their position ("3 of 10"), and whether the mod's navigation is on at launch.
- **Sounds**: the volume of each sound the mod plays, with a master volume. Left and Right change a volume, Enter plays the sound.
- **All Keys**: every key the mod has, by category.

Which game events are announced, and which go to the events buffer, is set per event in the config file, `BepInEx\config\com.accessibility.monstertrain.cfg`, in the game folder.
