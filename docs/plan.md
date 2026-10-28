# Rewrite plan: Monster Train 2 on the guildrun-access architecture

## 1. Why

main grew before the architecture did: retained containers rebuilt by string signatures, focus
co-driven with the game's own selection, tooltips reassembled by hand, about 50,700 lines in 422
files. guildrun-access is the model that proved pleasant to work in: an immediate-mode key graph per
screen, Core/Module hot reload, a dev server with a live C# REPL. This branch rebuilds the mod on
that model, using main only as a reference for what exists and where the game keeps it.

Expected savings over a straight port of main (estimates, not measured on the rewrite):

| Source | Lines in main | Expected after |
|---|---|---|
| Signature diffing and rebuild bookkeeping | ~1,300 | none |
| Focus sync, restore and suppression | ~3,600 | ~900 to 1,300 |
| GameObject registry | ~970 | ~550 |
| Framework (UIManager, ScreenManager, containers, generic elements, input, buffers) | ~8,400 | Core plus ~1,500 of glue |
| `AccessTools` field declarations and `Get<T>` calls | 745 declarations | none (publicizer) |
| Hand-built card tooltips (`ProxyCombatCardTooltips`) | 887 | ~150 to 250 for all tooltips |
| Verbosity profiles | ~2,650 with the presentation pipeline | replaced by focus line plus buffers |

## 2. Decisions

1. Guildrun's way wins every conflict with main's conventions.
2. Core is COPIED from guildrun-access, not shared. Extract a shared package later, once both mods
   have stopped changing it.
3. Strings: Guildrun's `Strings` table and `lang/*.txt`, not main's `Message` and JSON. main's 588
   keys mostly die with the rewrite; translations come over per string as it is added (section 6).
4. Speech: Guildrun's behavior. Navigation moves interrupt; entry and feedback queue.
5. Verbosity profiles are not ported. What they did becomes a per-screen choice of what goes in the
   focus line and what goes in a buffer or a glance key.
6. Reflection: the Module references `Assembly-CSharp` publicized. A renamed member is a compile
   error, which is what main's `AccessTools(...)!` rule was protecting.
7. Controller support stays: `ControllerBinding` beside `KeyboardBinding`, fed by the game's own
   gamepad driver.
8. Card play and targeting call the game's methods, not simulated clicks (MT2 reads the mouse through
   the legacy `Input.mousePosition`, which nothing can feed).
9. Tooltips come from the game's pipeline only (section 5).

## 3. Phases

### 3.1 Phase 1: host, dev loop, Core (done)
- Host on BepInEx 5 / Mono: Prism, settings, pump, dev server.
- Hot reload by renaming each generation's Core and Module with Mono.Cecil and loading the bytes.
  Verified live: generation 2 picked up a rebuilt module, the old generation's patches were gone,
  `/eval` compiled against the new generation.
- Roslyn REPL on Mono: works. Needs the four facade DLLs deployed beside it.
- Core copied, Guildrun-specific parts removed (hex grid, sound cues, its strings); 153 tests pass.
- Module skeleton: focus mode over the game's keyboard and gamepad drivers, keyboard and controller
  bindings, the mod menu, the key help, the control buffer, language sync with live switching
  (verified: de-DE and back).

### 3.2 Phase 2: Core adaptations still open
- Sound cues: Guildrun's `Core/Audio` was dropped with its Guildrun-only cue list. Bring the engine
  back when MT2 has its first cue (main's `BoundaryBeepPlayer` is the only one today).
- Text editing (`TextEdit`, `NumberEdit`, `EditEcho` is already in Core) for the compendium search.
- `ScreenAudit` (the dev coverage report) once there are game screens to audit.

### 3.3 Phase 3: readers and registries, before any game screen
1. **Screen registry**: the live game `UIScreen`s, filled by hooks on the game's screen show/hide
   path (main's `RegisterUIScreen` hook is the reference), the equivalent of Guildrun's `GameScopes`.
   No scene scans.
2. **`TooltipReader`** with four entry points, each returning `List<TooltipContent>` in the game's
   order:
   - `For(Component)`: `ITooltipProvider.Tooltips`;
   - `For(CardState)`: a `CardTooltipContainer`'s `SetSelectedCard`, capturing what it builds, then
     `DisableTooltips()`;
   - `For(CharacterState)`: `CharacterTooltipHelper.GetMainTooltipContent` and
     `GetAdditionalTooltipContents`;
   - `For(RelicState)`: `TooltipGenerator.GetRelicTooltips`.
   Before deleting anything, a dev command diffs the game's card list against main's
   `CollectTooltips` for every card in a deck.
3. **Widget nodes**: `GameNodes` factories for `GameUISelectableButton`, toggles, sliders, dropdowns,
   activating through the game's handlers.
4. **Domain nodes**: named factories for cards, units, relics, upgrades, rewards.
5. **Card play spike**: `HandUI.PlayCard` with a targeted spell, a unit, and a room-restricted card.
   Its result decides the battle screen's design.

### 3.4 Phase 4: screens
For each: read main's file for what exists, `/gui` and `/eval` the live screen, write its `Build`,
check it with `/input` and `/speech`.

| Batch | Screens (main's `ScreenRegistration` names) |
|---|---|
| a. Shell | IntroStart, FirstTimeSettings, MainMenu, Settings, pause, dialogs, Credits |
| b. Between battles | RunSetup, SoulSaviorRunSetup, RunOpening (both), Map, SoulSaviorMap, Minimap, RegionSelection, Draft (Draft, RelicChoice, Elixir, SoulChoice, EndlessMutatorDraft), Reward, Merchant, DragonsHoard, StoryEvent, CharacterDialogue, ChampionUpgrade, Unlock, Deck, CardDetails |
| c. Run end | GameOver, RunSummary, RunHistory, SoulProgression |
| d. Meta | Compendium, ChallengeOverview, ChallengeDetails, ChallengeProgress, Soulforge, SoulSavior, TrainCosmetics, KeyMapping |
| e. Battle | Game (BattleScreen), BattleIntro: a `CompositeScreen` of hand, floors, top HUD, hand selection, targeting, victory |

main's `Events/` (combat narration) and `Map/` logic are game knowledge: port them close to verbatim
when their screens come up.

### 3.5 Phase 5: release
- Release packaging: main's `build_release.bat` targeted the single-DLL layout and was removed. Port
  Guildrun's `build_release.ps1` for the `plugins\MonsterTrainAccessibility\` folder layout, Release
  builds only (no dev server, no Roslyn).
- Installer: `installer/src/core/paths.rs` lists main's files (`MOD_DLL`, the System.Text.Json
  family). Point it at the folder layout and drop the old files on upgrade.
- Docs (`docs_src`): rewrite the key list once the screens settle.

## 4. Merging back
The branch merges when every row of main's `ScreenRegistration` is covered, every key in the docs'
key list works or was deliberately dropped, and a full run has been played through. A beta release
from the branch before that would catch regressions no single tester feels.

## 5. Tooltip sources (the game's pipeline)
- Every tooltip the game shows is `List<TooltipContent>` (`title`, `body`).
- All container paths end in `TooltipContainer.InstantiateTooltip(id, ...)?.Set(title, body, ...)`,
  which de-duplicates through `addedTooltipKeys`.
- The game's card order (`CardTooltipContainer.AddTooltipsForCardState`): cost type, traits,
  temporary trait upgrades, effects, the character, upgrade attachment, triggers, permanent and
  temporary upgrades, additional stat tooltips, merge-card tooltips, lore by preference. main's
  collector used its own order and appears to miss several of these.
- Trainworks-Reloaded adds content only (JSON pipelines, registries); it offers nothing to read. Content
  it adds flows through the same pipeline, so reading the pipeline covers modded cards for free.

## 6. Translations
`lang/` holds `en.txt` (the generated template) and one file per game language, named for I2's code:
`de-DE es fr-FR ja ko pt-BR ru zh-CN zh-HK`. The generic strings came from guildrun-access's files
(Korean written new). When a string is added, look for main's key with the same English in
`../monster_traina11y2-main/MonsterTrainAccessibility/Localization/Strings/*.json` and carry its
translations over (main's files: `zh-TW` is the game's `zh-HK`, `pt` is `pt-BR`; named
placeholders become `{0}`-style slots).

## 7. Risks, highest first
1. Card play outside the game's selection layer (phase 3 spike).
2. Leftover subscriptions from old generations after a reload: double announcements. Every game
   `Signal`/event subscription must go through Dispose.
3. Game code that reads keys outside `CoreInputDriverKeyboard` still hears them in focus mode. A grep
   of `game/` for `Input.GetKey*` finds `CoreInput` itself, `KeyMappingScreen`, `CheatManager`,
   `ReplayManager`, the gamepad device shims, and a few `UnityEngine.UI.Extensions` helpers
   (`TabNavigationHelper`, `InputFocus`, `InputFieldEnterSubmit`, `MenuManager`). Check each one when
   its screen is ported.
4. Drift between this Core and guildrun-access's.
