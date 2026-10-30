# Monster Train Accessibility — Accessibility Mod for Monster Train 2

Screen-reader accessibility mod for blind players, rebuilt from 0.3 on the guildrun-access
architecture: a key graph per screen, Core/Module hot reload, the dev server and REPL. The old mod
(the single-DLL one, up to the tag `v0.2.3`) is the loose reference for what each screen held; it is
never copied wholesale. Speech is the sole interface: a silent failure
is invisible to the player, so every catch logs, and nothing caches game state.

## Game facts
- **Engine**: Unity **2022.3.62f2**, **Mono** (not IL2CPP). Managed code in
  `MonsterTrain2_Data\Managed`; the game is `Assembly-CSharp.dll`, most of it in the global
  namespace, the framework in `ShinyShoe`.
- **Install**: `C:\Program Files (x86)\Steam\steamapps\common\Monster Train 2` (Steam app **2742830**).
  The build resolves it itself (`Directory.Build.props`: `-p:GameDir=`, `MONSTER_TRAIN_PATH`, the
  Steam registry, the default path, or a gitignored `Directory.Build.local.props`).
- **Modding framework**: **BepInEx 5.4.23.4** (Mono), vendored as the release zip in
  `third_party\bepinex` and installed in the game folder by `setup-bepinex.ps1`. HarmonyX in
  `BepInEx\core\0Harmony.dll`, Mono.Cecil beside it.
- **Speech**: Prism v0.16.7 (`third_party\prism\prism.dll`, vendored with its header and licenses:
  `third_party\prism\README.md`; deployed next to `MonsterTrain2.exe`) via
  hand-written P/Invoke in the host (`src/MonsterTrainAccessibility/Speech/`).
- **Input**: the game reads input through `ShinyShoe.CoreInput` and its drivers
  (`CoreInputDriverKeyboard` polls the legacy `UnityEngine.Input.GetKey` per key it tracks,
  `CoreInputDriverGamepad` polls `GamepadManager.gamepads`), plus uGUI's EventSystem for selection.
- **Languages**: I2 Localization. `I2.Loc.LocalizationManager.CurrentLanguageCode` is set by the
  time our plugin loads. The codes: `en-US fr-FR de-DE ru pt-BR zh-CN es zh-HK ko ja`
  (`LocalizationManager.GetAllLanguagesCode(true)`, read live). A switch goes through
  `LanguageManager.HandleLanguageChanged`.
- **Name clashes**: the game declares `InputManager` and `ScreenManager` in the global namespace.
  Module files that use Core's alias them INSIDE the namespace block (a file-level alias conflicts
  with the global type: CS0576):
  `using InputManager = Core.Input.InputManager; using ScreenManager = Core.Screens.ScreenManager;`.
  Our screens keep the game's screen names (`Screens/IntroStartScreen` for the game's
  `IntroStartScreen`), so the correspondence is obvious; the price is that a global game type wins
  name lookup outside our own namespace. Inside a mod screen, write the game's types as `global::`
  (`global::IntroStartScreen`, and `global::ScreenName`, since Core's `Screen.ScreenName` property
  shadows the enum); `ModuleMain` registers screens as `new Screens.IntroStartScreen()`.

## Decompiled reference
- `game/` — the decompiled game (C#, one file per type, gitignored; never committed). Grep it before
  guessing a shape; it is the full source, bodies included.
- The old mod at the tag `v0.2.3`, under `MonsterTrainAccessibility/`: which game fields hold what,
  which hooks fire when, what each screen offers. Read it for facts, never for structure:
  `git show v0.2.3:MonsterTrainAccessibility/<path>`, `git grep <pattern> v0.2.3`, or a throwaway
  `git worktree add <dir> v0.2.3` for a long read (removed after).
- For live behavior use the game itself: `GET /gui` for structure, `POST /eval` for values.

## Tools (`tools/python/`, standard library only; run with `uv run python tools/python/<tool>.py`)
- `dev.py <cmd>` — the dev server from the command line: `launch`, `kill`, `reload`, `nav`,
  `input ui.down ui.activate`, `speech --tail 20`, `log --grep X`, `gui --grep RE --context 3`,
  `eval file.cs`, `wait "<bool expr>"`, `typeinfo Name`, `actions`, `module`, `screenshot out.png`,
  `click [x y]`. `gamelib.py` holds the shared constants (app id, exe, port).
- `audit.py <check>` — the content audits (`Module/Dev/GlossaryAudit`) over every card and artifact:
  `glossary [--all-languages]`, `titles` (no tooltip line says its name twice, titles still reading an
  icon), `compare [--all-languages]` (against the earlier spelling rule), `screen` (what the open
  screen's buffers gain); `--top N`, `--out FILE`. Cards need a screen with a card open (the Logbook).
- `make_cues.py [--force]` — the placeholder sound cues under `assets/audio` (writes the missing ones).
- `carry_translations.py NEWKEY=OLDKEY ...` — a new string's translations from the old mod's JSON
  files at `v0.2.3`, when it had a string with the same English (`zh-TW` is the game's `zh-HK`, `pt`
  is `pt-BR`; named placeholders become `{0}` slots).

## Build & deploy
```
dotnet build MonsterTrainAccessibility.slnx      # everything; deploys host + Core + Module
dotnet build src/MonsterTrainAccessibility.Module/MonsterTrainAccessibility.Module.csproj   # the hot loop
dotnet test src/MonsterTrainAccessibility.Tests/MonsterTrainAccessibility.Tests.csproj      # Core + Contracts
```
Debug builds deploy to `<game>\BepInEx\plugins\MonsterTrainAccessibility\` (host, Contracts, Roslyn
and its facades, Core, Module, `lang\`) and `prism.dll` next to `MonsterTrain2.exe`. The host deploy
deletes main's single-DLL layout (`plugins\MonsterTrainAccessibility.dll` and its deps.json): same
assembly name and plugin GUID, they must never load side by side. Deploying main again means
removing `plugins\MonsterTrainAccessibility\` first.
- **A host or Contracts change needs the game closed** (their DLLs are locked) and a relaunch.
- **Core/Module changes need no restart**: the module-only build copies those two DLLs (and `lang\`),
  then `curl -X POST 127.0.0.1:8772/reload`. `GET /module` shows the generation and the DLL write
  times, so a stale deploy is visible.
- The build must have **0 warnings**. Check with `dotnet build ... 2>&1 | tail -5` (warnings print
  late; never grep for them).
- Launch: `"/c/Program Files (x86)/Steam/steam.exe" -applaunch 2742830`, then wait with
  `curl -s --retry 60 --retry-connrefused --retry-delay 2 --retry-all-errors http://127.0.0.1:8772/health`.
  Kill from the Bash tool: `MSYS_NO_PATHCONV=1 taskkill.exe /F /IM MonsterTrain2.exe`.
- First-time setup: `.\setup-bepinex.ps1` (extracts the vendored BepInEx over the game folder the
  build resolves), then build. `.\build.ps1 [-ModuleOnly]` checks BepInEx is there and runs the
  Debug build.
- Release builds (`-c Release`) compile without deploying.

## Releases (Guildrun's tooling, the dd2a11y / Non-Visual Calculus pattern)
The version lives in `Directory.Build.props` alone (`<Version>`; the host's `BuildVersion` constant
is generated from it). A release is: bump the version, add a `## Vx.y.z` section to `CHANGELOG.md`
(the release notes are read from it; a missing file or an empty section fails), commit, tag `vx.y.z`
and push the tag, then:
- `build_release.ps1 [-GameDir X]` — `releases\MonsterTrainAccessibility-vX.Y.Z.zip`: the vendored
  BepInEx 5 zip's game-folder layout (`BepInEx\core\`, `winhttp.dll`, `doorstop_config.ini`,
  `.doorstop_version`; its `changelog.txt` dropped), the four mod DLLs (host, Contracts, Core, Module)
  from a clean Release build under `BepInEx\plugins\MonsterTrainAccessibility`, `lang\*.txt` beside
  them, `prism.dll` at the root, and the mdbook manual (`docs_src`) as `MT2AccessibilityDocs`. The
  DLLs are picked by name: the dev server's Roslyn assemblies and `_RoslynFacade`s are Debug-only and
  are NOT shipped (the Release host compiles the dev server out with `#if DEBUG`). No `BepInEx.cfg`
  (BepInEx 5 keeps its console off by default) and no pdb. The zip root is the game folder: a manual
  user extracts it over the game dir.
- `build-installer.ps1` — `releases\MonsterTrainAccessibilityInstaller.exe` from `installer\` (Rust +
  wxWidgets: needs cargo, libclang, ninja; `test-installer.ps1` runs its unit tests, which touch only
  temp dirs; `cargo run --release --example cli` is the CLI without the elevation manifest). It finds
  the Steam install (`MONSTER_TRAIN_PATH`, registry, library folders), downloads the newest release's
  zip from GitHub, verifies the digest, installs with backups and an install manifest
  (`BepInEx\config\MonsterTrainAccessibility\install.json`), updates, repairs, uninstalls. The mod
  never writes installer state; the installed version is the manifest's. Its game facts are the
  constants in `installer\src\core\paths.rs` (exe, folder name, the Mono marker
  `MonsterTrain2_Data/Managed/Assembly-CSharp.dll`, the plugin path, the releases URL:
  `amerikrainian/mt2-access`). An install over main's single-DLL layout (`core\legacy.rs`, evidence:
  `plugins\MonsterTrainAccessibility.dll`) adopts that layout's files (`LEGACY_FILES`, the legacy
  installer's `%APPDATA%\MonsterTrainAccessibility\install_manifest.json` list, the old manual's
  pages) as the prior install's, so they are pruned without backups, then deletes that `%APPDATA%`
  state.
- `create-release.ps1 vX.Y.Z` — the GitHub release (gh) for the pushed tag, uploading the zip and the
  installer with the CHANGELOG section as notes. The installer matches assets by the exact name
  `MonsterTrainAccessibility-vX.Y.Z.zip`, so tags are strict three-part versions; main's releases
  (v0.2.3 and before) carry an unversioned `MonsterTrainAccessibility.zip` the new installer ignores.
`releases\`, `installer\target\` and `docs_src\book\` are gitignored.

## Logs
Our lines go through the BepInEx logger with a `[Monster Train Accessibility]` source into
`<game>\BepInEx\LogOutput.log` (truncated each launch), and in-band via `GET /log?since=N&grep=S`.

## Navigation strategy (decided)
**Custom keyboard and gamepad navigation over the live uGUI widgets**, NOT the game's own selection.
A key graph per screen is built from the live views (`Build(GraphBuilder)`, immediate mode: declared
fresh every render, focus persisting by `ControlId` identity), labels are read from TMP text at speak
time, and activation goes through the widget's own event or the controller method a click would
reach, so the game's handler runs as for a click. Card play and targeting call the game's methods
directly (`HandUI.PlayCard` and kin), never simulated mouse input: MT2 reads the mouse through the
legacy `Input.mousePosition`, which nothing can feed.

While focus mode is on (`Module/UI/FocusMode`), the game hears no keys and no pad: its keyboard and
gamepad drivers are fed "nothing held" in place of their own poll (Harmony prefixes on their
`OnLateUpdate`), and the EventSystem stops sending navigation events with its selection cleared. The
pads are still polled, for our controller bindings (`Module/Input/Gamepad`). The mouse is untouched.
Ctrl+Shift+A hands everything back.

**No scene scans in feature code.** `FindObjectOfType` and kin are for the dev server only. Screens
find their live game screen through the game's own registry: `Module/Interop/GameScreens.Active<T>(ScreenName)`
asks `AllGameManagers.Instance.GetScreenManager()` for `GetScreenActive` and `GetScreen` on every read
(the role Guildrun's `GameScopes` plays; here the game keeps it for us).

## Architecture — HOST/MODULE split
Five projects, all under `src/`:
- **`MonsterTrainAccessibility.Contracts`** (netstandard2.0, PERMANENT): `IModHost`, `IModModule`,
  `IDevDriver`, `ISpeechBackend`, `SpeechPipeline`, `ISettingsStore`/`ModSettings`, `TextFilter`.
  Everything the host holds is typed here.
- **`MonsterTrainAccessibility`** (netstandard2.1, PERMANENT host; **changing it needs a game
  restart, keep it minimal**): `Plugin` (BepInEx 5 entry; GUID `com.accessibility.monstertrain`,
  main's, so the config file carries over), `Speech/` (Prism), `Host/HostPump` (the one
  MonoBehaviour), `Modularity/ModuleLoader`, `BepInExSettingsStore`, `Dev/` (dev server, REPL).
  The host must NEVER reference Core or Module at compile time.
- **`MonsterTrainAccessibility.Core`** (netstandard2.0, RELOADABLE, engine-free, unit-tested):
  `Graph/` (`KeyGraph`, `GraphBuilder`, `GraphAnnouncer`, `ControlId`), `UI/` (`GraphNavigator`,
  `GraphSheet`, `ControlTypes`, `TypeAheadSearch`, `KeyHelp`), `Input/` (`InputManager`
  categories + chord shadowing), `Screens/` (`Screen`, `CompositeScreen` + `ScreenSection`,
  `ScreenManager`), `Buffers/`, `Strings/`. Engine seams are statics the module sets in Load:
  `CoreLog`, `Speech.Speak`, `NavInput.Current`, `Navigation.FocusActive`,
  `InputManager.FocusActive`, `GraphAnnouncer.PositionText/ExpandedStateText`.
- **`MonsterTrainAccessibility.Module`** (netstandard2.1, RELOADABLE, engine-coupled; **day-to-day
  feature work goes here**): `ModuleMain` (seams, input registration, screen registration, Tick,
  `IDevDriver`), `Input/` (`KeyboardBinding`, `ControllerBinding`, `Gamepad`, `UnityNavInput`),
  `UI/` (`FocusMode`, `GameNodes`, `Buffers`, `Later`), `Screens/` (mod menu, key help),
  `LanguageSync`, `UpdateChecker`. References `Assembly-CSharp` **publicized**
  (`BepInEx.AssemblyPublicizer.MSBuild`): private members read as plain fields
  (`screen.collectButton`), so no `AccessTools` lookups, and a member the game renames is a compile
  error.
- **`MonsterTrainAccessibility.Tests`** (net8.0 xUnit): Core + Contracts only; runs serially.

**Hot reload on Mono.** Unity's Mono cannot unload an assembly, so `ModuleLoader` loads each
generation as NEW assemblies: it reads the Core and Module DLL bytes, renames them with Mono.Cecil to
`MonsterTrainAccessibility.Core.gN` / `.Module.gN`, points Module's Core reference at the renamed
Core, and `Assembly.Load`s the bytes (the files stay unlocked). Old generations stay loaded and idle
after their Dispose: a leak only development pays (a player's game loads generation 1 alone). The
new generation loads and patches BEFORE the old one is disposed (a failed reload leaves the running
one untouched), with `IModHost.SuccessorLoaded` set around the old Dispose so it drops its hooks
without handing input back to the game. Consequences:
- Module Harmony uses a per-load GUID id and `UnpatchSelf()` in Dispose.
- **Every subscription to a game event or `Signal` goes away in Dispose.** A forgotten one keeps the
  old generation running beside the new one: double announcements after a reload.
- No MonoBehaviour types in the Module (a component type would pin its generation); the host's pump
  drives `IModModule.Tick`.
- Core statics are per-generation (each generation has its own Core).

Rules: new feature/screen/adapter/patch code goes in Module (Core when engine-free). Only entry,
native handles, sockets, and the pump go in the host.

## Dev driver (Debug only; on by default, `MTA_NO_DEV=1` disables, `MTA_DEV_PORT` sets the port)
Loopback HTTP on `http://127.0.0.1:8772`, driven with curl. `MTA_NO_SPEECH=1` skips Prism for
headless runs; spoken text is still captured.
- `POST /input` body = `up|down|left|right|confirm|back|tab|prev|home|end|secondary|read` or any
  registered action key (`ui.down`, `mod.menu`): routed through the module's real dispatch, exactly
  like a key press; the response echoes the new focus readout. `GET /actions` lists the keys.
  `dev.type:TEXT` types TEXT for the type-ahead on the next frame.
- `GET /nav` our navigator's state (focus mode, screen stack, focused node, every node in the render).
- `GET /gui` raw uGUI hierarchy (paths, components, TMP text, CanvasGroup alpha); `GET /focus` the
  game's own uGUI selection.
- `GET /speech?since=N` what the mod spoke, tagged interrupt/queue and the speaking class
  (`&wait=MS` long-polls); `GET /log?since=N&grep=S`.
- `POST /eval` C# against the live game (Roslyn scripting on Mono; state persists). It references
  every loaded assembly, the whole Managed folder, and the LIVE generation's Core and Module, so
  `MonsterTrainAccessibility.Module.UI.FocusMode.Active` works; a reload resets the session. The
  REPL sees `Assembly-CSharp` as shipped, not publicized: private members need reflection there.
  `POST /wait?timeout=MS` polls a bool expression per frame; `GET /typeinfo?name=X`;
  `GET /screenshot`.
- `POST /reload`, `GET /module` (generation, DLL write times, Harmony patch table), `GET /health`.
- **Before pressing anything through the driver, read the game's handler for it.** The Menu's Quit
  Game quits on the spot outside a story event (`PauseDialog.ConfirmQuit` only confirms there), and a
  driver press closed the game that way. Prefer probing a dialog with one the REPL raises
  (`ScreenManager.ShowConfirmationDialog("test", null, null)`) over a real destructive button, and
  restore every setting a test changes.
- Roslyn's netstandard2.0 build needs `System.Memory`, `System.Buffers`, `System.Numerics.Vectors`
  and `System.Threading.Tasks.Extensions`, which Unity's Mono does not ship; the host deploy copies
  them from the NuGet cache (`_RoslynFacade` in the host csproj).

## Keys (focus mode on at launch; Ctrl+Shift+A toggles it)
F1 is the key help: the keys that would do something RIGHT NOW (`Module/Screens/HelpScreen`,
`Core/UI/KeyHelp`); Enter on a row runs it. Ctrl+Shift+M the mod menu. Arrows navigate, Tab/Shift+Tab
cycle control groups, Enter activates, Backspace is the secondary action, Escape backs out,
Home/End jump, Alt+Up/Down jump sections, typing letters searches the focused group, Ctrl+arrows
review the buffers.

Every action carries a gamepad binding beside its key (`ControllerBinding`; a trigger is the only
modifier, matched exactly so LT+A never also fires A): D-pad and left stick move, A activates, B
backs out, X is the secondary action, LB/RB cycle groups, LT+D-pad Up/Down Home/End, RT+D-pad
Up/Down sections, the right stick reviews buffers, LT+Back the key help, LT+Start the mod menu,
RT+Back toggles focus mode.

A text field (the Logbook's searches) is edited with the game's own editor, started on landing
(`Module/UI/SearchField`, `Module/UI/TextEdit`): the mod's keys stand down while it lasts; Enter or
Escape end it with focus kept (Enter edits again), Tab and Shift+Tab end it and move on. A single-line
field has no use for Up and Down, so they end it too and move to the control above or below: it sits
on a line of its own among its stop's controls (Left and Right are the caret's). The pad has the same
ways out (the shoulders, the D-pad and left stick, B as Escape). The game's Bind Keys waiting for a
key reads it from Unity's legacy input itself: the mod's keys stand down the same way until that key
is released (`Module/UI/KeyCapture`), and Bind/Add press a frame late so the Enter that pressed them is
not the key bound.

A key that is silent somewhere must say so with `.When(...)`, so the key help lists it only where it
does something; a screen offers its own keys through `GetActions`.

## Buffers (the Harkest Dungeon pattern: `Core/Buffers`, `Module/UI/Buffers`)
Review lists for the information a focus announcement leaves out, read live on every keypress:
Ctrl+Right/Left switch buffers, Ctrl+Up/Down step lines. Empty buffers are skipped; a focus change
re-homes review to the control's buffer. So far only **control** (the focused node's head line, then
one line per description part and per `NodeVtable.Details` tooltip, then the definitions those lines
mention that the game left out: the glossary, below). The game's buffers (card, unit, floor, combat
log) join as their screens are ported. Conventions: a tooltip is ONE line, never joined with others; a
node's details go in `Details = () => ...`, never spoken directly.

**The glossary** (`Core/Text/Glossary`, `Module/Nodes/GameGlossary`, hooked in through
`NodeLines.Definitions`): every definition the game can draw, read from its registries (status effects,
unit and card triggers, every `CardTraitState`, the `X_TooltipTitle`/`X_TooltipText` and
`HudTooltip_X_Title`/`_Body` term pairs), so a keyword a patch or a mod adds is in it without code.
Each definition has an id the same in every language, and its links to other definitions are read
from its body's SOURCE text, never from the translation's wording: the replacement tokens (`[armor]`,
mapped by what the token renders in English, never by its spelling: the status id `buff` is Rage) in
this language's text and the English, plus the English text's own bold. So Russian Valor links Armor
though it writes "броню" by hand. In a node's own text, a bold span is a mention when it is exactly a
definition's name (every token renders so). Inflected forms match only in English (the source text);
another language's spelling is never guessed at, since its words can be another keyword's exact name
(German "Verzauberung" is Incant, Traditional Chinese 強化 is Enhance). The closure of what a node
mentions and does not show is appended to the control buffer, never spoken on focus. A panel declared a
node per line (an enemy's details) sets `NodeVtable.Panel` on each line, so what one line defines is
not added again to another's buffer.
`uv run python tools/python/audit.py glossary` checks it against all content: every tooltip title the
game draws must be an entry, and what each card and artifact gains is listed; `--all-languages` repeats
it per language, `titles` checks the lines as speech reads them. Rerun them after a game patch or with
new mods.

## Languages (`Module/LanguageSync`, `Core/Strings`, `lang/`)
The mod's own words follow the GAME's language, never the system's: I2's current language code,
read at module load and again after every `LanguageManager.HandleLanguageChanged` (a Harmony
postfix; nothing is polled). `lang/<code>.txt` beside the plugin, found by `LanguageFiles.Candidates`
(the code, its lowercase, the bare language): `en-US` falls back to `en.txt`. The launch line is the
module's, spoken once the first language is in place. `Strings.LoadTranslation` swaps the whole file
and reports what it set aside, which `LanguageSync` logs. `lang/en.txt` is `Strings.DumpTemplate()`:
after adding or rewording a string run the tests once with `MTA_WRITE_LANG_TEMPLATE=1`, then add the
key to every other file (`LangFilesTests`: one file per game language, complete, same slots as the
English). Game terms are the game's own: read them from I2 (`LocalizationManager.GetTranslation`).

## Sounds (`Core/Audio`, `Module/Audio`, `assets/audio`; Guildrun's, from dd2a11y)
- **Cues** (`Core/Audio/AudioCue`): one enum value per sound, one file per cue at
  `assets/audio/<group>/<snake_case name>.wav` (`AudioCues.RelativePath`); a real sound replaces a
  placeholder 1:1 by file name. So far main's boundary beeps: `TeamFriendly` and `TeamEnemy`, played by
  the battle when a move along a floor crosses from one side's unit to the other's
  (`BattleScreen.TeamBoundary`, from `Row`'s focus hook; a floor change is silent, as on main).
- **Engine** (`Module/Audio/UnityCueEngine`): MT2 plays everything through Unity audio (its
  `CoreAudioSystem` pools AudioSources into an AudioMixer). Ours is a hidden object with four
  AudioSources of our own (never the game's pool, so our pan and pitch never stay on a game source),
  sent into the game's SFX mixer group (`SoundManager.audioSystem.AudioSystemData.MainSfxMixerGroup`)
  so the SFX slider applies; clips decoded by `Core/Audio/WavFile` (PCM 8/16/24/32, float) on first play
  and kept; object and clips destroyed in Dispose. An AudioSource cannot amplify, so volumes stop at
  100 percent (`SoundVolume.MaxVolume`).
- **Settings** (`Core/Audio/SoundSettings`, store keys `sound_*`): a master volume and per cue a volume
  stored as a signed offset from it, plus an interval (least seconds between plays, blank for every
  event). `ModuleMain.Cues` (`CuePlayer`) is the one door every cue goes through. The mod menu's
  Sounds (`Screens/ModSoundsScreen`) lists the master and a volume row per cue (Left/Right step, Enter
  previews); Guildrun's interval rows and its `NumberEdit` wait for a cue that fires often.
- The module-only build deploys `assets/audio`; `build_release.ps1` ships it beside the dlls.

## Hard rules
- **All speech through `Speech.Say`** (Core) -> the host `SpeechPipeline`; never call Prism directly.
  Navigation moves interrupt; screen entry and feedback queue.
- **Never cache game state.** Hold live widgets and read them at speak time.
- **Reuse game text** (already localized) wherever it exists; every mod-authored word lives in
  `Core/Strings/Strings.cs` (keyed, overridable by `lang/<code>.txt`), never inline in a Say call.
- **Never look a term up through I2 directly** (`.Localize()`, `LocalizationManager.GetTranslation`),
  in the module or in a REPL script: use `Module/Interop/GameText` (`Term`, `TryTerm`, `Source`). I2
  caches a term's parsed parameters on the first lookup, and one without the game's context drops
  `[effect0.power]` from that term's game text for the rest of the session ("Deal damage", no number,
  on the card as drawn). `GameText` undoes its own parse. Calling the game's own functions is fine. A
  poisoned session is cleared by setting every `TermData.ParsedCache` to null (or a restart).
- **No silent failures**: every catch logs through `CoreLog` / the host logger.
- **Announcements** (mod-authored text only; never reword game text): distinguishing word first, no
  nav hints, no fluff, never less information. Users are expert screen-reader users.
- **Tooltips come from the game's own pipeline**, never rebuilt by hand: `ITooltipProvider.Tooltips`
  for widgets, `CardTooltipContainer.SetSelectedCard` for cards, `CharacterTooltipHelper` for units,
  `TooltipGenerator` for relics, all yielding `TooltipContent` in the game's order.
  The one addition is the glossary's: definitions the game omits, each drawn by the game's own
  function for it, appended to the buffer after the game's tooltips.
- **Element factories are named**: a domain object (card, unit, relic) gets a static factory class
  returning its `NodeVtable` (Guildrun's `Nodes/` style); inline lambdas are for one-offs.
- **Harmony**: attribute patches applied by `PatchAll` under the generation's id; patch a
  non-virtual chokepoint or the concrete override, never a base-class virtual.
- **0 warnings**; no `#pragma` or `[SuppressMessage]` to get there.
- In Module files with `using UnityEngine;`, alias `Screen` and `Navigation` to the Core types; alias
  `InputManager` and `ScreenManager` inside the namespace (see Game facts).
- Beware `int.MinValue` "long ago" sentinels: `frameCount - int.MinValue` overflows negative and
  disables a throttle forever. Use `-Interval` or `int.MinValue / 2`.
