# Combat

Combat is the piece of the game, besides the compendium, that has received the most overhaul. It is worthwhile to read through this section in full before playing, so as to eliminate any confusion and tragic pyre demise on your part.

## How It All Normally Works

In Monster Train 2, combat may take place on any one of the 4 train floors. Enemies enter the train on floor one and are super eager to come up and gaze in awe at your pyre. They do so by moving up to the next floor at the end of each round, assuming they're still alive, of course. Oh, and they might just want to show their appreciation when they reach the topmost room by giving your pyre a couple of good smacks. Just, y'know. Because.

If at any point enemies encounter your summoned units on the train, they exchange blows before moving higher on the train. With the exception of bosses, they do not wait until one side is fully eradicated; the pyre calls!

Enemies generally attack first, followed by your unit. Your frontmost unit, in left-to-right order, gets attacked first, followed by the nextmost, and so on. Likewise, when your monsters retaliate, they hit the front enemy unit, followed by the unit after that, and so on.

This proceeds on all floors, top-to-bottom, until the enemies get to your pyre. At this point, since they have nowhere to go, they'll keep giving it some good pats until either they, or your pyre, dies.

## How the Mod Makes This Accessible

Normally one would need to scroll and switch floors. The mod, for the most part, removes this for you. From bottom to top, the UI order is as follows:

- Your current hand of cards
- Floor 1 (this is where you get some unwelcome passengers)
- Floor 2
- Floor 3
- Floor 4 (this is where your pyre is at)
- Top panel containing buttons like end turn, remaining enemy waves, etc. Most of that information has either a game- or mod-provided shortcut for quicker access.

## Hand and Cards

Left/right arrows can be used to navigate your hand. When a card is focused, you get things like its name, type, cost, and unit statistics. Importantly, however, you do not get every single piece of information, so as to reduce speech clutter.

For example, one keyword the game has is `explosive`, in which case the card will mention said keyword but will not by default give you a description of the effect.

To see the full description, use the buffer navigation keys. There you will learn, for instance, that explosive means that any excess damage is spilled over to the adjacent creatures.

The same pattern holds for your and enemy units; only key pieces of information are narrated, and full details are available within the buffer upon request.

## Floors

Floors can also be navigated with left/right arrows. They expose the full lineup of creatures, yours and otherwise, with your and the enemy's backliners on the edges. In other words, the unit that is most likely to be attacked next turn is adjacent to the first enemy unit within the list from left-to-right.

## Resources

These are listed in [Controls](controls.md) but are repeated here for your convenience. Keep in mind that these are mod-provided and that the game may have more, e.g., `F` to end turn.

- R: read ember.
- Ctrl+R: read forge points.
- Ctrl+G: read gold.
- Ctrl+H: read pyre health.

## Outcomes

Outcome commands summarize predicted combat results:

- I: focused unit outcome.
- Ctrl+I: focused floor outcomes.
- Ctrl+Shift+I: all floor outcomes.

If no relevant floor or unit is focused, these commands do nothing.

## Abilities

Creature and room abilities are represented as focusable elements when the game UI exposes them (which it does on its own schedule, not yours). Pressing confirm on an accessible ability element should activate the corresponding game ability.

## Targeting Things, Oh My!

Some abilities aren't content to just go off into the ether. They demand a target (a victim, usually; occasionally a beneficiary). When you trigger such an ability, the game obligingly enters targeting mode and waits patiently for you to point it at something.

Navigation in targeting mode works exactly as it does elsewhere: left/right arrows to scroll through creatures, and the usual floor navigation to hop between floors. Once your chosen mark is focused, press Enter to confirm and let the carnage (or kindness, who's to say) commence.

Cards play by similar rules. When you summon a unit, you'll be asked to pick a spawn point on the floor. Slot 1 is always the frontmost (right in the enemies' faces), and the numbers count back from there, with the highest slot being whatever's furthest from the fray (cozy, well-defended, lovely view of the action). Same navigation, same Enter to commit.

One thing to keep in mind: most monsters can only swing at things on their own floor, so don't waste your breath courting a target two floors up. The game will politely refuse, and your unit will sit there judging you.
