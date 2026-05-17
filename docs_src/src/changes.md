# Change Log

## 0.2.0

- Fixed a bug where your focus would be stuck after opening settings on rewards screen. You could have gotten yourself unstuck by hitting tab, but you don't have to anymore (QgSama). Silly Unity focus restore.
- Updated existing Chinese translation (QgSama)

## 0.1.9

- We now pick up character chatter with all the related events turned off by default so as to reduce spam. They do, however, go into the new monster quotes buffer so you can review them at your leisure (Boing).
- We now clear any focus one has on room abilities. This should hopefully fix the rare cases of room abilities activating instead of cards (Boing).
- Try and suppress the transient unlock screen briefly shown even when you don't have anything new unlocked. This should get rid of announcements like new clan unlocked (Boing).

## 0.1.8

- Events are now less verbose (Camlorn), e.g.: gold +10 without including total.
- Multiple people asked to disable wrapping in creature lists. It has been done. Additionally, an auditory beep plays whenever you cross from allies to enemies and vice versa.

## 0.1.7

- Floors now read their effects (if any) alongside capacity when requested with `b`, defaulting to name only when just being scrolled over (Matrheine).
- We now announce changes in moon phases as an event (Matrheine). You could previously see it if you go to the top panel above the floor with your pyre, but hopefully it's faster now.
- The train depot screen now reads, although from my investigation its... quite bare. I'm happy to be corrected, however.

## 0.1.6

- In soul savior, pressing `m` while having the HUD open puts you onto your first soul. An odd choice of key, but you can rebind this if you wish (although do note that this is a native game action).
- In the HUD screen, bosses are now collapsed to a horizontal row as opposed to participating in vertical navigation.
- By popular demand/confusion, artifacts read out their primary effects without having to go to the buffer to see them. They do not, however, describe their tooltips, so if you want to see what something like `quick` does you still have to look in the respective buffer.
- Souls now go through the presentation buffers during run preview in case you forgot and want to also remind yourself of their info.
- The skip button is now labeled when equipping souls. I wasn't aware that was an option even. No idea why you'd want to do that, but now you can.
- We no longer register the transient confirm button on soul equip screen, so no more weird vertical cycling in the first column (I don't think people even use confirm since souls equip upon selecting a card).
- The HUD screen is now treated as, well, an actual screen. The game doesn't do that but hopefully we do now, so pressing tab should work anywhere the game allows it to (previously it was a custom opt-in. Seemed like a great idea at the time. It wasn't).
- Slightly rearranged soul savior region selection menu so that region modifier is now its own item rather than being lumped in with rewards.

## 0.1.5

- The mod should respect user's selected language now and not require an explicit language switch
- Added revised Chinese localization by QgSama

## 0.1.4

- The game has been machine-translated into all the languages it supports. Contributions with improvements on that front are welcome.
- We now have an installer! You can grab it from the latest releases page (chaosbringer216)
- Corrected pyre sprite localization to say Pyre health as opposed to just Pyre.
- Story event choices now include all of the available rewards for a particular choice rather than stopping at the first one. This allows one to, e.g., see the Dominion's Eye blight before it's added to their deck. Fortunately this was never a huge deal, but at least it's fixed now.
- Passive artifact effects like purified soul shard no longer spam you when they get triggered (Matrheine).
- Settings tabs are now visible as actual buttons. They probably should have been like this in the first place. You could reach other tabs with brackets, but the game never tells you that fact. This makes them more consistent with screens like soul savior, too.

## 0.1.3

- Card grids no longer allow you to vertically slide down to the next nearest item in a future row (whoops?).
- In combat, pressing page up/down also has the same effect as pressing your arrow keys. The game already had those controls, and it was strange they disagreed (ogomez92)
- When setting up a run, it is now possible to view a clan's experience progress to the next level (ogomez92)
- Creature abilities now collapse both the name and description into one buffer entry (Chaosbringer216)
- Spawn points now announce if there's a unit at that location. This should make it easier to figure out who your summoned unit will be spawned in front of so you don't have to remember quite as much.

## 0.1.2

- Continue tinkering with tutorial screens. All of them are accessible now, we just forgot about the one special covenant case

## 0.1.1

- Make the starting settings screen read. The author may or may not have opened the game over six months ago so he had to wait for a user to run into that one.
- Fixed controllers not interrupting themselves when you scroll through items
- Made some tutorial popups accessible. Hungry dialogues were eating keys!
