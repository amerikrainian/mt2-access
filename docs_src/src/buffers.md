# Buffers

Buffers are review lists for extra information. They are separate from normal focus navigation: moving focus chooses what is in them; the buffer keys read it without moving focus away.

## Buffer Controls

- Ctrl+Up: next line in the current buffer.
- Ctrl+Down: previous line.
- Ctrl+Right: next buffer.
- Ctrl+Left: previous buffer.
- The right stick does the same on a controller.

Moving focus brings review back to the control buffer, at its first line.

## The Buffers

- **Control**: the focused control in full. Its line as focus reads it, then one line for each part of it and for each of the game's tooltips on it (a card's keywords, a unit's statuses, equipment and abilities). Last come the definitions of the keywords those lines mention that the game itself leaves out.
- **Events**: the game's events as the mod announced them (and the ones you turned off for speech but kept for the buffer), newest first.
- **Monster quotes**: what units say, newest first. Their chatter is not spoken by default; it lands here.

A buffer with nothing in it is skipped.

## Why Buffers Matter

Some controls need short focus text but long detail text. A card in hand speaks its name, type, cost and text; the control buffer holds what each of its keywords means. Shops, rewards, artifacts, Logbook entries and story event choices use the control buffer the same way.
