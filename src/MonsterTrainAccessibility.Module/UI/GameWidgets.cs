using System;
using System.Collections.Generic;
using MonsterTrainAccessibility.Core.Graph;
using MonsterTrainAccessibility.Core.Screens;
using MonsterTrainAccessibility.Core.Strings;
using MonsterTrainAccessibility.Core.UI;
using MonsterTrainAccessibility.Module.Interop;
using ShinyShoe;
using TMPro;

namespace MonsterTrainAccessibility.Module.UI
{
    /// <summary>
    /// Node factories for the game's own widgets (the ShinyShoe <c>GameUISelectable*</c> family and the
    /// controls built on them), in the shared spoken conventions of <see cref="GameNodes"/>. Every one
    /// acts through the game's own path: a press is a Submit on the game's input signal
    /// (<see cref="GameInput"/>), which its screen's <c>TryTrigger</c> answers as for a key press; a
    /// slider moves through its <c>ChangeValue</c>, the setter the game's own keys use. State is the
    /// game's wording wherever the widget shows one ("High" / "Low", "Locked").
    /// </summary>
    public static class GameWidgets
    {
        /// <summary>A button: the label given, the game's "Locked" when the button is locked, "disabled"
        /// when it is otherwise out of use (a locked button is not interactable either: the lock says
        /// why), then the description. Enter presses it.</summary>
        public static NodeVtable Button(GameUISelectableButton button, Func<string> label, Func<string> description = null)
        {
            return new NodeVtable
            {
                ControlType = ControlTypes.Button,
                Announcements = new List<NodeAnnouncement>
                {
                    GameNodes.LabelPart(label),
                    LockedPart(button),
                    GameNodes.DisabledPart(() => Usable(button)),
                    GameNodes.TooltipPart(description),
                },
                SearchText = label,
                OnActivate = () => GameInput.Submit(button),
            };
        }

        /// <summary>A two-state toggle: "label, toggle, On" in the game's own words for the two states
        /// (its on and off captions: "High" / "Low" on the quality switch), our "on" / "off" where it
        /// shows none. Enter flips it through the game, which saves the preference.</summary>
        public static NodeVtable Toggle(GameUISelectableToggle toggle, Func<string> label)
        {
            Func<string> state = () => StateOf(toggle);
            return new NodeVtable
            {
                ControlType = ControlTypes.Toggle,
                Announcements = new List<NodeAnnouncement>
                {
                    GameNodes.LabelPart(label),
                    new NodeAnnouncement(state, live: true, kind: AnnouncementKinds.Value),
                    LockedPart(toggle),
                    GameNodes.DisabledPart(() => Usable(toggle)),
                },
                SearchText = label,
                OnActivate = () => GameInput.Submit(toggle),
                StateText = state,
            };
        }

        /// <summary>A dropdown as a combo box: "label, combo box, current option". Enter opens its
        /// options as a picker landing on the current one; Enter there chooses through the game (the
        /// option's own Submit while the list is open, exactly the game's choose path), Escape leaves the
        /// value alone. Left and right navigate, never cycle.</summary>
        public static NodeVtable Dropdown(GameUISelectableDropdown dropdown, Func<string> label)
        {
            Func<string> current = () => dropdown.valueLabel != null ? dropdown.valueLabel.text : null;
            return new NodeVtable
            {
                ControlType = ControlTypes.ComboBox,
                Announcements = new List<NodeAnnouncement>
                {
                    GameNodes.LabelPart(label),
                    new NodeAnnouncement(current, live: true, kind: AnnouncementKinds.Value),
                    LockedPart(dropdown),
                    GameNodes.DisabledPart(() => Usable(dropdown)),
                },
                SearchText = label,
                OnActivate = () => OpenPicker(dropdown, label),
                StateText = current,
            };
        }

        /// <summary>A slider: "label, slider, value" as the game prints it. Left and right move it a
        /// tenth of its range through the game's own setter, which saves and plays the tick.</summary>
        public static NodeVtable Slider(SelectableSliderHelper slider, Func<string> label, params NodeAnnouncement[] extra)
        {
            Func<string> value = () => slider.valueLabel != null ? slider.valueLabel.text : slider.Value.ToString();
            var parts = new List<NodeAnnouncement>
            {
                GameNodes.LabelPart(label),
                new NodeAnnouncement(value, live: true, kind: AnnouncementKinds.Value),
            };
            parts.AddRange(extra);
            return new NodeVtable
            {
                ControlType = ControlTypes.Slider,
                Announcements = parts,
                SearchText = label,
                OnAdjust = (sign, large) =>
                {
                    int step = Math.Max(1, (slider.max - slider.min) / 10);
                    slider.ChangeValue(slider.Value + sign * step);
                },
                StateText = value,
            };
        }

        /// <summary>A volume: its slider, then "Mute" while muted (the game's word), Backspace muting
        /// and unmuting through the control's own toggle (the game's secondary action on the slider);
        /// moving the slider unmutes, as in the game.</summary>
        public static NodeVtable Volume(VolumeControl volume, Func<string> label)
        {
            var vt = Slider(volume.slider, label,
                new NodeAnnouncement(() => volume.muted ? GameText.Term("ScreenKeyMapping_Control_Mute") : null,
                    live: true, kind: AnnouncementKinds.Value));
            vt.OnSecondary = volume.ToggleMuted;
            return vt;
        }

        // The game's "Locked" while the widget is locked.
        private static NodeAnnouncement LockedPart(GameUISelectableButton button)
            => new NodeAnnouncement(() => IsLocked(button) ? GameText.Term("Locked") : null, kind: AnnouncementKinds.Enabled);

        private static bool IsLocked(GameUISelectableButton button) => button.state == GameUISelectableButton.State.Locked;

        // Usable, or locked (the lock is announced instead of "disabled").
        private static bool Usable(GameUISelectableButton button)
            => IsLocked(button) || (button.interactable && button.state != GameUISelectableButton.State.Disabled);

        private static string StateOf(GameUISelectableToggle toggle)
        {
            TMP_Text caption = toggle.isOn ? toggle.onLabel : toggle.offLabel;
            if (caption != null && !string.IsNullOrWhiteSpace(caption.text)) return caption.text;
            return toggle.isOn ? Strings.StateOn : Strings.StateOff;
        }

        // The dropdown's options as a picker. Choosing opens the game's list (its own flag) and submits
        // the option, so the game sets, saves and closes it as for a click on the entry.
        private static void OpenPicker(GameUISelectableDropdown dropdown, Func<string> label)
        {
            if (!dropdown.CanBeActivated()) return;
            var entries = dropdown.entries;
            if (entries == null || entries.Count == 0) return;
            string currentText = dropdown.valueLabel != null ? dropdown.valueLabel.text : null;
            int current = -1;
            var choices = new List<ChoiceOption>();
            for (int i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                if (entry == null || !entry.gameObject.activeSelf) continue;
                string text = entry.label != null ? entry.label.text : null;
                if (string.IsNullOrWhiteSpace(text)) continue;
                if (current < 0 && text == currentText) current = choices.Count;
                var option = entry.GetComponent<GameUISelectableButton>();
                choices.Add(new ChoiceOption(text, () =>
                {
                    if (option == null || !dropdown.CanBeActivated()) return;
                    dropdown.open = true;
                    GameInput.Submit(option);
                }));
            }
            if (choices.Count == 0) return;
            // The combo box just read its own label: the picker speaks only the option landed on.
            ChoiceSubmenuScreen.Open(label != null ? label() : null, choices, current, speakTitle: false);
        }
    }
}
