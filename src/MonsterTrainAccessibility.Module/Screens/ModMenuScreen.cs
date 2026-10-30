using System;
using System.Collections.Generic;
using MonsterTrainAccessibility.Core.Graph;
using MonsterTrainAccessibility.Core.Input;
using MonsterTrainAccessibility.Core.Strings;
using MonsterTrainAccessibility.Core.UI;
using MonsterTrainAccessibility.Module.UI;
using Navigation = MonsterTrainAccessibility.Core.UI.Navigation;
using Screen = MonsterTrainAccessibility.Core.Screens.Screen;

namespace MonsterTrainAccessibility.Module.Screens
{
    // The game declares its own InputManager and ScreenManager in the global namespace.
    using InputManager = Core.Input.InputManager;
    using ScreenManager = Core.Screens.ScreenManager;

    /// <summary>
    /// The mod's own menu (Ctrl+Shift+M anywhere): Settings, Event announcements, Sounds, Key help,
    /// Close. An overlay above every game screen, opened and closed by a static flag the poll picks up;
    /// its sub-screens are child screens, so Escape walks back through them to the game.
    /// </summary>
    public sealed class ModMenuScreen : Screen
    {
        private static bool s_open;

        // Opening speaks nothing of its own: the first focus readout names the menu (its context).
        public static void Toggle() => s_open = !s_open;

        public static void Close() => s_open = false;

        public override string Key => "mod.menu";
        public override int Layer => 50;
        public override bool Exclusive => true;
        public override bool IsActive() => s_open;

        public override void Build(GraphBuilder b)
        {
            b.PushContext(Strings.ScreenModMenu, Strings.RoleList);
            b.AddItem(ControlId.Structural("modmenu:settings"), GameNodes.Button(() => Strings.ModSettings, () => PushChild(new ModSettingsScreen())));
            b.AddItem(ControlId.Structural("modmenu:events"), GameNodes.Button(() => Strings.EventSettings, () => PushChild(new EventSettingsScreen())));
            b.AddItem(ControlId.Structural("modmenu:sounds"), GameNodes.Button(() => Strings.ModSounds, () => PushChild(new ModSoundsScreen())));
            b.AddItem(ControlId.Structural("modmenu:keys"), GameNodes.Button(() => Strings.ModKeyHelp, () => PushChild(new KeyHelpScreen())));
            b.AddItem(ControlId.Structural("modmenu:close"), GameNodes.Button(() => Strings.ModClose, Close));
            b.PopContext();
        }

        public override IEnumerable<ElementAction> GetActions()
        {
            yield return new ElementAction(ActionIds.Back, Strings.Get("bind.ui.back"), _ => Close());
        }
    }

    /// <summary>The mod's settings: each a toggle; Enter flips it, the new state spoken once. Values live
    /// in the host's settings store (the BepInEx config), so they survive reloads and restarts.</summary>
    public sealed class ModSettingsScreen : Screen
    {
        public override string Key => "mod.settings";
        public override bool IsActive() => false; // only ever a child of the mod menu

        public override void Build(GraphBuilder b)
        {
            var settings = ModuleMain.Host != null ? ModuleMain.Host.Settings : null;
            b.PushContext(Strings.ModSettings, Strings.RoleList);
            if (settings == null)
            {
                b.AddItem(ControlId.Structural("modsettings:none"), GameNodes.Text(() => Strings.NoTooltip));
                b.PopContext();
                return;
            }
            b.AddItem(ControlId.Structural("modsettings:positions"), Toggle(() => Strings.ModSpeakPositions,
                () => settings.SpeakPositions, v => settings.SpeakPositions = v));
            b.AddItem(ControlId.Structural("modsettings:focus"), Toggle(() => Strings.ModFocusOnLaunch,
                () => settings.FocusModeOnLaunch, v => settings.FocusModeOnLaunch = v));
            b.AddItem(ControlId.Structural("modsettings:upgrades"), Toggle(() => Strings.ModMarkUpgrades,
                () => Preferences.MarkUpgrades, v => Preferences.MarkUpgrades = v));
            b.AddItem(ControlId.Structural("modsettings:places"), Toggle(() => Strings.ModPlacesByNeighbours,
                () => Preferences.PlacesByNeighbours, v => Preferences.PlacesByNeighbours = v));
            b.PopContext();
        }

        // The new state is spoken once, through StateText, as the game's own toggles speak theirs.
        internal static NodeVtable Toggle(Func<string> label, Func<bool> get, Action<bool> set)
        {
            Func<string> state = () => get() ? Strings.StateOn : Strings.StateOff;
            return new NodeVtable
            {
                ControlType = ControlTypes.Toggle,
                Announcements = new List<NodeAnnouncement>
                {
                    GameNodes.LabelPart(label),
                    new NodeAnnouncement(state, live: true, kind: AnnouncementKinds.Value),
                },
                SearchText = label,
                OnActivate = () => set(!get()),
                StateText = state,
            };
        }

        public override IEnumerable<ElementAction> GetActions()
        {
            yield return new ElementAction(ActionIds.Back, Strings.Get("bind.ui.back"), _ => ParentScreen?.RemoveChild(this));
        }
    }
}
