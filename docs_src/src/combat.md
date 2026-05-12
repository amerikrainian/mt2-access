# Combat

Combat is the piece of the game, besides the compendium, that has received the most overhaul. It is worthwhile to read through this section in full before playing, so as to eliminate any confusion and tragic pyre demise on your part.

## How It All Normally Works

Combat unfolds across the train's four floors. Your pyre sits proudly at the top of floor 4, minding its own business. Enemies appear at the bottom of floor 1 with one ambition in their tiny ill-fated hearts: climb the train and bonk your pyre. Your job is to make sure they don't get that far.

The rhythm goes like this:

1. **A wave spawns on floor 1.** Enemies line up shoulder-to-shoulder, looking purposeful.
2. **Wherever your units and theirs share a floor, they trade blows.** One round of combat per turn; nobody waits around for full annihilation before the survivors continue their journey. (Bosses, the dramatic sort, make exceptions for themselves.)
3. **Enemies swing first, in front-to-back order. Each one targets your frontmost living unit.** That means a single beleaguered frontliner can get piled on by the whole enemy lineup before anyone on your side gets a word in edgewise. Only if the frontmost falls does the next attack cascade to whoever is now in front.
4. **Then your units retaliate, also in front-to-back order, each targeting the enemy's frontmost living unit.** Same piling-on rule, just in the other direction.
5. **Survivors climb.** At the end of the round, any enemy still standing trudges up to the next floor and gears up to do it all again with whoever you've stationed there.
6. **At the pyre, the pleasantries end.** Enemies who reach floor 4 with nothing left to fight will whack the pyre directly, round after round, until either they or it ceases to exist.

### An Example, Because Words Are Hard

Picture floor 1. You've placed two units, in left-to-right order:

- **Spear steward** (back)
- **Shield steward** (front, nearest the action)

A wave arrives with three enemies, in left-to-right order:

- **Fallen champion** (front, nearest your line)
- **Ominous acolyte**
- **Collector** (back, leaning casually against the wall)

Enemies attack first:

1. The **Fallen champion** swings at your **Shield steward** (your frontmost).
2. The **Ominous acolyte** also swings at your **Shield steward**. (Shield is still the frontmost; the Acolyte doesn't get to pick a different target just because someone else already had a go.)

Then your units retaliate, Shield first (frontmost on your side), then Spear:

3. **Shield steward** swings back at the **Fallen champion** (their frontmost).
4. **Spear steward** swings next, and here things branch:
   - **If the Fallen champion survived Shield's attack**, Spear hits the Fallen champion. The pile-on continues.
   - **If the Fallen champion died**, the Ominous acolyte is now the enemy's frontmost, and Spear hits the Acolyte instead.

And the Collector? The Collector watches the whole thing serenely from the back, entirely unscathed. Your line ran out of attackers before anyone got around to it. This is the price of fielding only two units; the Collector will be very much involved next turn.

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

### Reading Spawn Points

As you scroll through spawn points, you'll often hear something like *"spawn point 2, spear steward."* Do not panic on the spear steward's behalf; you are not about to overwrite them. The convention is that the named unit is whoever currently occupies that slot, and committing the spawn places your new arrival *in front of* them. That unit, and anyone ahead of them, will politely shuffle one step back to make room for the newcomer. Empty slots, having no current resident to name, simply announce themselves by number.

A worked example. Suppose you already have two spear stewards on the floor, at points 1 and 2, and you're about to summon a fresh shield steward. Reading the line left-to-right (back to front, as established):

- **Spawn at point 1** (currently a spear steward): the shield slips into the frontmost slot, both spears shuffle back a step. Line becomes spear -> spear -> shield.
- **Spawn at point 2** (also a spear steward): the shield wedges in between; the rearward spear gets bumped one further back. Line becomes spear -> shield -> spear.
- **Spawn at point 3** (empty, so no name announced): the shield settles into the back, and nobody has to move. Line becomes shield -> spear -> spear.

Same unit, same floor, three entirely different roles in the choreography. Pick accordingly.

