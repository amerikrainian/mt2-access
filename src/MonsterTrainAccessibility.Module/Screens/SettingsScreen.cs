using System.Collections.Generic;
using MonsterTrainAccessibility.Core.Graph;
using MonsterTrainAccessibility.Core.Strings;
using MonsterTrainAccessibility.Core.UI;
using MonsterTrainAccessibility.Module.Interop;
using MonsterTrainAccessibility.Module.UI;
using ShinyShoe;
using TMPro;
using UnityEngine;
using Screen = MonsterTrainAccessibility.Core.Screens.Screen;

namespace MonsterTrainAccessibility.Module.Screens
{
    /// <summary>
    /// The settings (<see cref="SettingsDialog"/>, over the Menu): the section tabs (Audio, Graphics,
    /// Gameplay, Accessibility, Other) as one Tab-stop, landing on a tab opening its section as in
    /// Guildrun, and the open section's rows as the next. The rows are the game's own entries, read in
    /// its order and only while shown (a row the game hides, such as the target framerate under VSync,
    /// is simply not declared): a volume is a slider with "Mute" (Backspace mutes), the scroll
    /// sensitivity a slider, a dropdown a combo box whose options open as a picker, a switch a toggle
    /// in the game's own words for its two states, and the rest buttons (Bind Keys, Erase all save
    /// data). Escape is the game's: back to the Menu.
    /// </summary>
    public sealed class SettingsScreen : Screen
    {
        public SettingsScreen()
        {
            // Tab past the rows comes round to the tabs.
            Wrap = true;
        }

        public override string Key => "game.settings";
        public override int Layer => 21;
        public override bool Exclusive => true;
        public override object InitialFocusStop => "tabs";

        private static SettingsDialog Dialog
        {
            get
            {
                var screen = GameScreens.Active<global::SettingsScreen>(global::ScreenName.Settings);
                var dialog = screen != null ? screen.settingsDialog : null;
                return dialog != null && dialog.Active ? dialog : null;
            }
        }

        public override bool IsActive() => Dialog != null;

        public override void Build(GraphBuilder b)
        {
            var dialog = Dialog;
            if (dialog == null) return;
            b.PushContext(PauseMenuScreen.DialogTitle(dialog), Strings.RoleList, positions: false);

            b.BeginStop("tabs");
            var tabs = new List<SettingsTab>();
            foreach (var tab in dialog.tabs)
                if (tab != null && tab.button != null && tab.button.gameObject.activeInHierarchy) tabs.Add(tab);
            // A vertical move into the row lands on the open tab.
            b.StartRow(entry: () => System.Math.Max(0, tabs.FindIndex(t => t.SectionRoot != null && t.SectionRoot.activeInHierarchy)));
            SettingsTab open = null;
            foreach (var tab in tabs)
            {
                if (tab.SectionRoot != null && tab.SectionRoot.activeInHierarchy) open = tab;
                b.AddItem(ControlId.Structural("settings:tab:" + tab.gameObject.name), Tab(tab));
            }
            b.EndRow();

            b.BeginStop("body");
            if (open != null)
                foreach (Transform row in open.SectionRoot.transform)
                    if (row.gameObject.activeInHierarchy) Row(b, open, row);
            b.PopContext();
        }

        public override IEnumerable<ElementAction> GetActions()
        {
            yield return new ElementAction(ActionIds.Back, Strings.Get("bind.ui.back"),
                _ => GameInput.Send(global::InputManager.Controls.Escape));
        }

        // A section tab: landing on it opens its section, through the game's own tab press. The open
        // tab is not interactable in the game, so landing on it again does nothing.
        /// <summary>A section tab (the game's <see cref="SettingsTab"/>, here and in the Soulforge): its
        /// caption; landing on it opens its section, as pressing it does.</summary>
        internal static NodeVtable Tab(SettingsTab tab)
        {
            System.Action open = () =>
            {
                if (tab.SectionRoot != null && !tab.SectionRoot.activeInHierarchy) GameInput.Submit(tab.button);
            };
            return new NodeVtable
            {
                ControlType = ControlTypes.Tab,
                Announcements = new List<NodeAnnouncement> { GameNodes.LabelPart(() => PauseMenuScreen.Caption(tab.button)) },
                SearchText = () => PauseMenuScreen.Caption(tab.button),
                OnActivate = open,
                OnFocus = open,
            };
        }

        // One of the section's entries, by the control it holds.
        private static void Row(GraphBuilder b, SettingsTab tab, Transform row)
        {
            string id = "settings:" + tab.gameObject.name + ":" + row.gameObject.name;
            System.Func<string> label = () => RowLabel(row);

            var volume = row.GetComponent<VolumeControl>();
            if (volume != null)
            {
                b.AddItem(ControlId.Structural(id), GameWidgets.Volume(volume, label));
                return;
            }
            var scroll = row.GetComponent<ScrollSensitivityControl>();
            if (scroll != null)
            {
                b.AddItem(ControlId.Structural(id), GameWidgets.Slider(scroll.slider, label));
                return;
            }
            var dropdown = row.GetComponentInChildren<GameUISelectableDropdown>(false);
            if (dropdown != null)
            {
                b.AddItem(ControlId.Structural(id), GameWidgets.Dropdown(dropdown, label));
                return;
            }
            var toggle = row.GetComponentInChildren<GameUISelectableToggle>(false);
            if (toggle != null)
            {
                b.AddItem(ControlId.Structural(id), GameWidgets.Toggle(toggle, label));
                return;
            }
            foreach (var button in row.GetComponentsInChildren<GameUISelectableButton>(false))
            {
                var pressed = button;
                b.AddItem(ControlId.Structural(id + ":" + pressed.gameObject.name), GameWidgets.Button(pressed, () => PauseMenuScreen.Caption(pressed)));
            }
        }

        // The entry's own caption: its "Label" text directly under it (the control's texts sit deeper).
        private static string RowLabel(Transform row)
        {
            foreach (Transform child in row)
            {
                if (child.gameObject.name != "Label") continue;
                var text = child.GetComponent<TMP_Text>();
                if (text != null && !string.IsNullOrWhiteSpace(text.Content())) return text.Content();
            }
            return null;
        }
    }
}
