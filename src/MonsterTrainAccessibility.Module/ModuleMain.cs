using System;
using System.Text;
using MonsterTrainAccessibility.Contracts;
using MonsterTrainAccessibility.Core;
using MonsterTrainAccessibility.Core.Graph;
using MonsterTrainAccessibility.Core.Input;
using MonsterTrainAccessibility.Core.Screens;
using MonsterTrainAccessibility.Core.Strings;
using MonsterTrainAccessibility.Core.UI;
using MonsterTrainAccessibility.Module.Input;
using MonsterTrainAccessibility.Module.Screens;
using MonsterTrainAccessibility.Module.UI;
using HarmonyLib;
using UnityEngine;

namespace MonsterTrainAccessibility.Module
{
    // The game declares its own InputManager and ScreenManager in the global namespace.
    using InputManager = Core.Input.InputManager;
    using ScreenManager = Core.Screens.ScreenManager;

    /// <summary>
    /// The reloadable feature module: wires the Core seams to the host, registers the mod's input
    /// actions and screens, owns focus mode, and drives the per-frame loop. Everything here is torn down
    /// in <see cref="Dispose"/> so a fresh generation can hot-swap in without a restart. Core statics are
    /// per-generation (each generation loads a Core of its own), so they need no manual reset.
    /// </summary>
    public sealed class ModuleMain : IModModule, IDevDriver
    {
        private IModHost _host;

        /// <summary>The host of the live generation (settings, logging), for screens that need it.</summary>
        public static IModHost Host { get; private set; }
        private Harmony _harmony;
        // The launch update check: asked once the module is up, its line spoken from Tick when the
        // request lands with a release newer than the running build.
        private readonly UpdateChecker _updateCheck = new UpdateChecker();
        private bool _updateAnnounced;

        public void Load(IModHost host)
        {
            _host = host;
            Host = host;

            // Core seams: logging, speech, engine inputs, focus ownership, announcement wording.
            CoreLog.Info = host.LogInfo;
            CoreLog.Warning = host.LogWarning;
            CoreLog.Error = host.LogError;
            Speech.Speak = (text, interrupt) => host.Speech.Speak(text, interrupt);
            // Game text draws icons inline as markup: every line reads them by the game's own words.
            Speech.IconWords = Interop.GameSprites.Words;
            NavInput.Current = new UnityNavInput();
            Navigation.FocusActive = () => FocusMode.Active;
            InputManager.FocusActive = () => FocusMode.Active;
            InputManager.TextFieldFocused = () => TextEdit.OwnsKeyboard;
            GraphAnnouncer.PositionText = (i, n) => host.Settings.SpeakPositions ? Strings.Position(i, n) : null;
            GraphAnnouncer.ExpandedStateText = Strings.ExpandedState;
            InputBinding.RegisterType("keyboard", KeyboardBinding.Deserialize);
            InputBinding.RegisterType("controller", ControllerBinding.Deserialize);

            // A per-load UNIQUE id so a reload's Dispose unpatches exactly this load's patches: the host
            // loads the new module (which patches) before disposing the old one, and UnpatchSelf removes
            // by owner id, so a fixed id would let the old teardown strip the fresh load's patches.
            _harmony = new Harmony("com.accessibility.monstertrain.module." + Guid.NewGuid().ToString("N"));

            RegisterInput();
            RegisterScreens();
            // The game hooks (the input drivers, the language). Patched by this load's Harmony id,
            // unpatched in Dispose. A failure here is a broken mod: say so, loudly, in the log.
            try { _harmony.PatchAll(typeof(ModuleMain).Assembly); }
            catch (Exception e) { host.LogError("Harmony patching failed: " + e); }

            // The strings follow the game's language: read now on a reload, at the game's first
            // language at boot.
            LanguageSync.Start(host.PluginDir, AnnounceLaunch);

            FocusMode.Set(host.Settings.FocusModeOnLaunch);
            host.LogInfo("Module loaded: " + ScreenManager.Registered.Count + " screens, "
                + InputManager.Actions.Count + " input actions, focus " + (FocusMode.Active ? "on" : "off"));
            _updateCheck.Start(host.ModVersion);
        }

        // "Monster Train Accessibility 0.3.0 loaded", once per launch, in the language the game came up
        // in: the host leaves the line to the module, which has the strings, and LanguageSync calls this
        // when the first language is in place. The host remembers it was said, so a reload stays quiet.
        private void AnnounceLaunch()
        {
            if (_host.LaunchAnnounced) return;
            _host.LaunchAnnounced = true;
            Speech.Say(Strings.ModLoaded(_host.ModVersion));
        }

        private static void RegisterInput()
        {
            // UI navigation keys: live only while a screen declares the UI category and focus mode is
            // on, routed into the navigator by InputManager. Directions and Tab auto-repeat while held.
            // The pad: the D-pad and left stick move, A activates, B backs out, the bumpers cycle groups.
            InputManager.Register(UiActions.Up, "Navigate up", InputCategory.UI)
                .AddBinding(new KeyboardBinding(KeyCode.UpArrow))
                .AddBinding(new ControllerBinding(ControllerInput.DpadUp))
                .AddBinding(new ControllerBinding(ControllerInput.LeftStickUp)).Repeating();
            InputManager.Register(UiActions.Down, "Navigate down", InputCategory.UI)
                .AddBinding(new KeyboardBinding(KeyCode.DownArrow))
                .AddBinding(new ControllerBinding(ControllerInput.DpadDown))
                .AddBinding(new ControllerBinding(ControllerInput.LeftStickDown)).Repeating();
            InputManager.Register(UiActions.Left, "Navigate left", InputCategory.UI)
                .AddBinding(new KeyboardBinding(KeyCode.LeftArrow))
                .AddBinding(new ControllerBinding(ControllerInput.DpadLeft))
                .AddBinding(new ControllerBinding(ControllerInput.LeftStickLeft)).Repeating();
            InputManager.Register(UiActions.Right, "Navigate right", InputCategory.UI)
                .AddBinding(new KeyboardBinding(KeyCode.RightArrow))
                .AddBinding(new ControllerBinding(ControllerInput.DpadRight))
                .AddBinding(new ControllerBinding(ControllerInput.LeftStickRight)).Repeating();
            InputManager.Register(UiActions.Next, "Next control group", InputCategory.UI)
                .AddBinding(new KeyboardBinding(KeyCode.Tab))
                .AddBinding(new ControllerBinding(ControllerInput.RightShoulder)).Repeating();
            InputManager.Register(UiActions.Prev, "Previous control group", InputCategory.UI)
                .AddBinding(new KeyboardBinding(KeyCode.Tab, shift: true))
                .AddBinding(new ControllerBinding(ControllerInput.LeftShoulder)).Repeating();
            InputManager.Register(UiActions.Activate, "Activate", InputCategory.UI)
                .AddBinding(new KeyboardBinding(KeyCode.Return)).AddBinding(new KeyboardBinding(KeyCode.KeypadEnter))
                .AddBinding(new ControllerBinding(ControllerInput.A));
            InputManager.Register(UiActions.Secondary, "Secondary action", InputCategory.UI)
                .AddBinding(new KeyboardBinding(KeyCode.Backspace))
                .AddBinding(new ControllerBinding(ControllerInput.X));
            InputManager.Register(UiActions.Back, "Back", InputCategory.UI)
                .AddBinding(new KeyboardBinding(KeyCode.Escape))
                .AddBinding(new ControllerBinding(ControllerInput.B));
            InputManager.Register(UiActions.Home, "Jump to first", InputCategory.UI)
                .AddBinding(new KeyboardBinding(KeyCode.Home))
                .AddBinding(new ControllerBinding(ControllerInput.DpadUp, ControllerInput.LeftTrigger));
            InputManager.Register(UiActions.End, "Jump to last", InputCategory.UI)
                .AddBinding(new KeyboardBinding(KeyCode.End))
                .AddBinding(new ControllerBinding(ControllerInput.DpadDown, ControllerInput.LeftTrigger));
            InputManager.Register(UiActions.RegionPrev, "Previous section", InputCategory.UI)
                .AddBinding(new KeyboardBinding(KeyCode.UpArrow, alt: true))
                .AddBinding(new ControllerBinding(ControllerInput.DpadUp, ControllerInput.RightTrigger));
            InputManager.Register(UiActions.RegionNext, "Next section", InputCategory.UI)
                .AddBinding(new KeyboardBinding(KeyCode.DownArrow, alt: true))
                .AddBinding(new ControllerBinding(ControllerInput.DpadDown, ControllerInput.RightTrigger));
            // Page turning, answered by the screen that pages (the Logbook) and by nothing else: no handler.
            InputManager.Register(Screens.CompendiumScreen.PagePrev, "Previous page", InputCategory.UI)
                .AddBinding(new KeyboardBinding(KeyCode.PageUp))
                .AddBinding(new ControllerBinding(ControllerInput.DpadLeft, ControllerInput.RightTrigger));
            InputManager.Register(Screens.CompendiumScreen.PageNext, "Next page", InputCategory.UI)
                .AddBinding(new KeyboardBinding(KeyCode.PageDown))
                .AddBinding(new ControllerBinding(ControllerInput.DpadRight, ControllerInput.RightTrigger));
            // Buffer review, the Harkest Dungeon keys: Ctrl+Left/Right switch buffers, Ctrl+Up/Down step
            // lines; on the pad, the right stick.
            Buffers.Init();
            InputManager.Register("buffer.next", "Next buffer", InputCategory.UI, Buffers.Controls.NextBuffer)
                .AddBinding(new KeyboardBinding(KeyCode.RightArrow, ctrl: true))
                .AddBinding(new ControllerBinding(ControllerInput.RightStickRight));
            InputManager.Register("buffer.prev", "Previous buffer", InputCategory.UI, Buffers.Controls.PreviousBuffer)
                .AddBinding(new KeyboardBinding(KeyCode.LeftArrow, ctrl: true))
                .AddBinding(new ControllerBinding(ControllerInput.RightStickLeft));
            InputManager.Register("buffer.line.next", "Next buffer line", InputCategory.UI, Buffers.Controls.NextLine)
                .AddBinding(new KeyboardBinding(KeyCode.UpArrow, ctrl: true))
                .AddBinding(new ControllerBinding(ControllerInput.RightStickUp)).Repeating();
            InputManager.Register("buffer.line.prev", "Previous buffer line", InputCategory.UI, Buffers.Controls.PreviousLine)
                .AddBinding(new KeyboardBinding(KeyCode.DownArrow, ctrl: true))
                .AddBinding(new ControllerBinding(ControllerInput.RightStickDown)).Repeating();

            // Global: always live, so the player can hand the keyboard back to the game and reclaim it.
            InputManager.Register("mod.focus", "Toggle navigation", InputCategory.Global, ToggleFocus)
                .AddBinding(new KeyboardBinding(KeyCode.A, ctrl: true, shift: true))
                .AddBinding(new ControllerBinding(ControllerInput.Back, ControllerInput.RightTrigger));
            InputManager.Register("mod.menu", "Mod menu", InputCategory.Global, ModMenuScreen.Toggle)
                .AddBinding(new KeyboardBinding(KeyCode.M, ctrl: true, shift: true))
                .AddBinding(new ControllerBinding(ControllerInput.Start, ControllerInput.LeftTrigger));
            // The keys that do something here, each runnable from the list.
            InputManager.Register(HelpScreen.ActionKey, "Keys here", InputCategory.Global, HelpScreen.Toggle)
                .AddBinding(new KeyboardBinding(KeyCode.F1))
                .AddBinding(new ControllerBinding(ControllerInput.Back, ControllerInput.LeftTrigger));
        }

        private static void ToggleFocus()
        {
            FocusMode.Toggle();
            Speech.Say(FocusMode.Active ? Strings.FocusOn : Strings.FocusOff, interrupt: true);
            if (FocusMode.Active) Navigation.AnnounceCurrent();
        }

        private static void RegisterScreens()
        {
            ScreenManager.Register(new Screens.IntroStartScreen());
            ScreenManager.Register(new Screens.MainMenuScreen());
            ScreenManager.Register(new Screens.StartRunOptionsDialog());
            ScreenManager.Register(new Screens.PauseMenuScreen());
            ScreenManager.Register(new Screens.SettingsScreen());
            ScreenManager.Register(new Screens.DialogScreen());
            ScreenManager.Register(new Screens.TrainCosmeticsScreen());
            ScreenManager.Register(new Screens.TrainRoomPickerScreen());
            ScreenManager.Register(new Screens.ChallengeProgressScreen());
            ScreenManager.Register(new Screens.ChallengeRewardsScreen());
            ScreenManager.Register(new Screens.CompendiumScreen());
            ScreenManager.Register(new ModMenuScreen());
            ScreenManager.Register(new HelpScreen());
        }

        public void Tick()
        {
            if (!_updateAnnounced && LanguageSync.Resolved && _updateCheck.NewerVersion != null)
            {
                _updateAnnounced = true;
                Speech.Say(Strings.UpdateAvailable(_updateCheck.NewerVersion));
            }
            FocusMode.Tick();
            Safe(TextEdit.Tick, "text edit");
            InputManager.Tick();
            ScreenManager.Tick();
            if (!TextEdit.OwnsKeyboard) Navigation.TickTypeahead();
            Safe(Later.Tick, "later");
            Safe(Buffers.Tick, "buffers");
        }

        // A reader that throws must not take the whole tick (and every other reader) down with it.
        private void Safe(Action tick, string what)
        {
            try { tick(); }
            catch (Exception e) { _host?.LogWarning("[" + what + "] " + e); }
        }

        /// <summary>Undo every persistent game-side effect Load created. Runs on reload (after the new
        /// generation is live) and on shutdown.</summary>
        public void Dispose()
        {
            // On a reload the successor already owns the input: drop our hooks only. On a shutdown,
            // give everything back to the game.
            bool restore = _host == null || !_host.SuccessorLoaded;
            try { TextEdit.Shutdown(); } catch (Exception e) { _host?.LogError("[dispose] text edit: " + e); }
            try { FocusMode.Shutdown(restore); } catch (Exception e) { _host?.LogError("[dispose] focus: " + e); }
            try { Gamepad.Reset(); } catch (Exception e) { _host?.LogError("[dispose] gamepad: " + e); }
            try { ScreenManager.Shutdown(); } catch (Exception e) { _host?.LogError("[dispose] screens: " + e); }
            try { InputManager.Clear(); } catch (Exception e) { _host?.LogError("[dispose] input: " + e); }
            try { LanguageSync.Stop(); } catch (Exception e) { _host?.LogError("[dispose] language: " + e); }
            try { _harmony?.UnpatchSelf(); } catch (Exception e) { _host?.LogError("[dispose] harmony: " + e); }
            _harmony = null;
            _host = null;
            Host = null;
        }

        // ---- IDevDriver: the dev server drives and inspects our navigation through these ----

        public string DispatchAction(string actionKey)
        {
            // Not keys: the dev driver's own verbs.
            if (actionKey != null && actionKey.StartsWith("dev.type:"))
            {
                UnityNavInput.InjectTyped(actionKey.Substring("dev.type:".Length));
                return "typing next frame";
            }
            if (string.IsNullOrEmpty(actionKey) || InputManager.Find(actionKey) == null) return null;
            InputManager.Dispatch(actionKey);
            var nav = Navigation.Active as GraphNavigator;
            var node = nav?.FocusedNode;
            return "fired " + actionKey + (node != null ? " -> " + GraphAnnouncer.ComposeFull(node) : "");
        }

        public string DescribeNav()
        {
            var sb = new StringBuilder();
            sb.Append("focus mode: ").Append(FocusMode.Active ? "on" : "off").Append('\n');
            sb.Append("stack: ");
            foreach (var s in ScreenManager.Stack) sb.Append(s.Key).Append(' ');
            sb.Append('\n');
            var cur = ScreenManager.Current;
            sb.Append("screen: ").Append(cur != null ? cur.Key + " (" + (cur.ScreenName ?? "") + ")" : "(none)").Append('\n');
            var nav = Navigation.Active as GraphNavigator;
            var node = nav?.FocusedNode;
            if (node == null)
            {
                sb.Append("focus: (none)\n");
                return sb.ToString();
            }
            sb.Append("focus: ").Append(node.Id).Append('\n');
            sb.Append("readout: ").Append(GraphAnnouncer.ComposeFull(node)).Append('\n');
            var render = nav.CurrentRender;
            if (render != null)
            {
                sb.Append("nodes (").Append(render.Order.Count).Append("):\n");
                foreach (var n in render.Order)
                    sb.Append(ReferenceEquals(n, node) ? "  > " : "    ").Append(n.Id.StructuralKey)
                      .Append(": ").Append(GraphAnnouncer.LeafText(n)).Append('\n');
            }
            return sb.ToString();
        }

        public string ListActions()
        {
            var sb = new StringBuilder();
            foreach (var a in InputManager.Actions)
                sb.Append(a.Key).Append(" [").Append(a.Category).Append("] ").Append(a.BindingsDisplay).Append('\n');
            return sb.ToString();
        }
    }
}
