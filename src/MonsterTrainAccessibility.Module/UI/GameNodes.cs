using System;
using System.Collections.Generic;
using MonsterTrainAccessibility.Core.Graph;
using MonsterTrainAccessibility.Core.Strings;
using MonsterTrainAccessibility.Core.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MonsterTrainAccessibility.Module.UI
{
    /// <summary>
    /// Node factories for graph-native screens over the game's uGUI + TextMeshPro widgets: assemble
    /// <see cref="NodeVtable"/>s with the shared spoken conventions (role words, disabled/selected state,
    /// "n of m" positions) and route activation through the widget's own event (the game's click handler
    /// runs exactly as a mouse click would). Every label reads LIVE from the widget at speak time.
    /// </summary>
    public static class GameNodes
    {
        /// <summary>The label part (always first in the standard order).</summary>
        public static NodeAnnouncement LabelPart(Func<string> label)
            => new NodeAnnouncement(label, kind: AnnouncementKinds.Label);

        /// <summary>The disabled-state part: silent while enabled, "disabled" otherwise. Read on landing
        /// and on request, not live: the game grays a button out for the instant after it is pressed
        /// (Proceed, Quit to Menu), which would otherwise be announced as the control going dead.</summary>
        public static NodeAnnouncement DisabledPart(Func<bool> enabled)
            => new NodeAnnouncement(() => enabled == null || enabled() ? null : Strings.StateDisabled,
                live: false, kind: AnnouncementKinds.Enabled);

        /// <summary>The selected-state part: "selected" when selected, silent otherwise; LIVE.</summary>
        public static NodeAnnouncement SelectedPart(Func<bool> selected)
            => new NodeAnnouncement(() => selected != null && selected() ? Strings.StateSelected : null,
                live: true, kind: AnnouncementKinds.Selected);

        /// <summary>The spoken description part, read after the value.</summary>
        public static NodeAnnouncement TooltipPart(Func<string> description)
            => new NodeAnnouncement(description, kind: AnnouncementKinds.Tooltip);

        /// <summary>Detail lines for the ui buffer from single texts: the non-blank ones, in order
        /// (a null or empty text is simply no line).</summary>
        public static IEnumerable<string> Lines(params string[] texts)
        {
            if (texts == null) yield break;
            foreach (var t in texts)
                if (!string.IsNullOrWhiteSpace(t)) yield return t;
        }

        /// <summary>Detail lines from a list (one per tooltip), the blanks dropped.</summary>
        public static IEnumerable<string> Lines(IEnumerable<string> lines)
        {
            if (lines == null) yield break;
            foreach (var t in lines)
                if (!string.IsNullOrWhiteSpace(t)) yield return t;
        }

        /// <summary>A plain read-only text line.</summary>
        public static NodeVtable Text(Func<string> text) => new NodeVtable
        {
            ControlType = ControlTypes.Text,
            Announcements = new[] { LabelPart(text) },
            SearchText = text,
        };

        /// <summary>A heading line ("text, heading").</summary>
        public static NodeVtable Heading(Func<string> text) => new NodeVtable
        {
            ControlType = ControlTypes.Text,
            Announcements = new List<NodeAnnouncement>
            {
                LabelPart(text),
                new NodeAnnouncement(() => Strings.RoleHeading, kind: AnnouncementKinds.Role),
            },
            SearchText = text,
        };

        /// <summary>A push button backed by a closure: "label, button[, disabled][, n of m]". A disabled
        /// button consumes activation silently.</summary>
        public static NodeVtable Button(Func<string> label, Action activate, Func<bool> enabled = null,
            ControlType type = null)
        {
            var anns = new List<NodeAnnouncement>
            {
                LabelPart(label),
                DisabledPart(enabled),
            };
            return new NodeVtable
            {
                ControlType = type ?? ControlTypes.Button,
                Announcements = anns,
                SearchText = label,
                OnActivate = () =>
                {
                    if (enabled != null && !enabled()) return;
                    activate?.Invoke();
                },
            };
        }

        /// <summary>A game button (<see cref="ShinyShoe.GameUISelectableButton"/>): the label given, the
        /// game's "Locked" when the button is locked, "disabled" when it is otherwise out of use (a locked
        /// button is not interactable either: the lock says why), then the description. Activation is a
        /// Submit through the game's input signal (<see cref="Interop.GameInput"/>), since the game
        /// triggers these from screen input, not from onClick.</summary>
        public static NodeVtable GameButton(ShinyShoe.GameUISelectableButton button, Func<string> label, Func<string> description = null)
        {
            bool Locked() => button.state == ShinyShoe.GameUISelectableButton.State.Locked;
            return new NodeVtable
            {
                ControlType = ControlTypes.Button,
                Announcements = new List<NodeAnnouncement>
                {
                    LabelPart(label),
                    new NodeAnnouncement(() => Locked() ? Interop.GameText.Term("Locked") : null, kind: AnnouncementKinds.Enabled),
                    DisabledPart(() => Locked() || (button.interactable && button.state != ShinyShoe.GameUISelectableButton.State.Disabled)),
                    TooltipPart(description),
                },
                SearchText = label,
                OnActivate = () => Interop.GameInput.Submit(button),
            };
        }

        /// <summary>A uGUI Button: label from its TMP child (or the given override), enabled from
        /// interactable, activation through its onClick so the game's own handler runs.</summary>
        public static NodeVtable Button(UnityEngine.UI.Button button, Func<string> label = null, ControlType type = null)
        {
            var vt = Button(label ?? (() => LabelOf(button)), () => button.onClick.Invoke(),
                () => button != null && button.interactable, type);
            return vt;
        }

        /// <summary>A uGUI Toggle: "label, toggle, on/off"; activation flips it through isOn so the game's
        /// onValueChanged runs.</summary>
        public static NodeVtable Toggle(UnityEngine.UI.Toggle toggle, Func<string> label = null)
        {
            Func<string> lbl = label ?? (() => LabelOf(toggle));
            return new NodeVtable
            {
                ControlType = ControlTypes.Toggle,
                Announcements = new List<NodeAnnouncement>
                {
                    LabelPart(lbl),
                    new NodeAnnouncement(() => toggle.isOn ? Strings.StateOn : Strings.StateOff,
                        live: true, kind: AnnouncementKinds.Value),
                    DisabledPart(() => toggle.interactable),
                },
                SearchText = lbl,
                OnActivate = () => { if (toggle.interactable) toggle.isOn = !toggle.isOn; },
                StateText = () => toggle.isOn ? Strings.StateOn : Strings.StateOff,
            };
        }

        /// <summary>A uGUI Slider: "label, slider, value"; left/right adjust by a step (a tenth of the
        /// range, or 1 for whole-number sliders).</summary>
        public static NodeVtable Slider(UnityEngine.UI.Slider slider, Func<string> label, Func<string> valueText = null)
        {
            Func<string> val = valueText ?? (() => slider.wholeNumbers
                ? ((int)slider.value).ToString()
                : Math.Round(slider.value, 2).ToString(System.Globalization.CultureInfo.InvariantCulture));
            return new NodeVtable
            {
                ControlType = ControlTypes.Slider,
                Announcements = new List<NodeAnnouncement>
                {
                    LabelPart(label),
                    new NodeAnnouncement(val, live: true, kind: AnnouncementKinds.Value),
                    DisabledPart(() => slider.interactable),
                },
                SearchText = label,
                OnAdjust = (sign, large) =>
                {
                    if (!slider.interactable) return;
                    float step = slider.wholeNumbers ? 1f : (slider.maxValue - slider.minValue) / 10f;
                    if (large) step *= 5f;
                    slider.value = Mathf.Clamp(slider.value + sign * step, slider.minValue, slider.maxValue);
                },
                StateText = val,
            };
        }

        /// <summary>A tab in a toggle-group strip (a Toggle whose caption is the tab name): "label, tab
        /// [, selected]"; activation selects it (the game switches pages on isOn).</summary>
        public static NodeVtable Tab(UnityEngine.UI.Toggle toggle, Func<string> label = null)
            => Choice(toggle, ControlTypes.Tab, label);

        /// <summary>A toggle of a mutually exclusive group that is a choice rather than a view (battle
        /// speed, a difficulty tier, a filter): "label, radio button[, selected]". Like a tab, landing on
        /// it selects it.</summary>
        public static NodeVtable Radio(UnityEngine.UI.Toggle toggle, Func<string> label = null)
            => Choice(toggle, ControlTypes.RadioButton, label);

        // A tab or radio button: selected by landing on it as well as by Enter, never while disabled.
        // A tab is always the selected one once landed on, so it carries no selected state.
        private static NodeVtable Choice(UnityEngine.UI.Toggle toggle, ControlType type, Func<string> label)
        {
            Func<string> lbl = label ?? (() => LabelOf(toggle));
            Action select = () => { if (toggle != null && toggle.interactable && !toggle.isOn) toggle.isOn = true; };
            var parts = new List<NodeAnnouncement> { LabelPart(lbl) };
            if (type != ControlTypes.Tab) parts.Add(SelectedPart(() => toggle.isOn));
            parts.Add(DisabledPart(() => toggle.interactable));
            return new NodeVtable
            {
                ControlType = type,
                Announcements = parts,
                SearchText = lbl,
                OnActivate = select,
                OnFocus = select,
            };
        }

        /// <summary>A TMP dropdown as a combo box: "label, combo box, current option"; Enter opens the
        /// options as a list landing on the current one, where Enter picks (the game applies it through
        /// onValueChanged) and Escape leaves the value alone. Left/right navigate, never adjust.</summary>
        public static NodeVtable Dropdown(TMP_Dropdown dropdown, Func<string> label)
        {
            Func<string> current = () =>
            {
                var caption = dropdown.captionText;
                if (caption != null && !string.IsNullOrEmpty(caption.text)) return caption.text;
                var options = dropdown.options;
                int v = dropdown.value;
                return options != null && v >= 0 && v < options.Count ? options[v].text : null;
            };
            return new NodeVtable
            {
                ControlType = ControlTypes.ComboBox,
                Announcements = new List<NodeAnnouncement>
                {
                    LabelPart(label),
                    new NodeAnnouncement(current, live: true, kind: AnnouncementKinds.Value),
                    DisabledPart(() => dropdown.interactable),
                },
                SearchText = label,
                OnActivate = () => OpenOptions(dropdown, label),
            };
        }

        // The dropdown's options as a child screen: a fresh snapshot per open, the current one marked.
        private static void OpenOptions(TMP_Dropdown dropdown, Func<string> label)
        {
            if (!dropdown.interactable) return;
            var options = dropdown.options;
            int count = options != null ? options.Count : 0;
            if (count == 0) return;
            var choices = new List<Core.Screens.ChoiceOption>();
            for (int i = 0; i < count; i++)
            {
                int index = i;
                string text = options[i].text;
                choices.Add(new Core.Screens.ChoiceOption(string.IsNullOrWhiteSpace(text) ? index.ToString() : text, () =>
                {
                    if (!dropdown.interactable || dropdown.value == index) return;
                    dropdown.value = index;
                    dropdown.RefreshShownValue();
                }));
            }
            // The combo box just read its own label: the list speaks only the option landed on.
            Core.Screens.ChoiceSubmenuScreen.Open(label != null ? label() : null, choices, dropdown.value, speakTitle: false);
        }

        /// <summary>The caption of a settings-style row: the TMP text named "Title" found by walking up
        /// from the widget through its row (a NamePanel/Title beside a SelectionHolder). Null when none.</summary>
        public static string RowLabel(Component widget, int levels = 4)
        {
            if (widget == null) return null;
            Transform t = widget.transform.parent;
            for (int i = 0; i < levels && t != null; i++, t = t.parent)
            {
                foreach (var tmp in t.GetComponentsInChildren<TMP_Text>(true))
                {
                    if (tmp == null || !string.Equals(tmp.gameObject.name, "Title", StringComparison.OrdinalIgnoreCase)) continue;
                    if (tmp.transform.IsChildOf(widget.transform)) continue; // the widget's own caption
                    if (!string.IsNullOrWhiteSpace(tmp.text)) return tmp.text;
                }
            }
            return null;
        }

        /// <summary>The visible TMP text under a widget, or its object name when it has none.</summary>
        public static string LabelOf(Component widget)
        {
            if (widget == null) return null;
            var tmp = widget.GetComponentInChildren<TMP_Text>(true);
            if (tmp != null && !string.IsNullOrEmpty(tmp.text)) return tmp.text;
            var legacy = widget.GetComponentInChildren<UnityEngine.UI.Text>(true);
            if (legacy != null && !string.IsNullOrEmpty(legacy.text)) return legacy.text;
            return widget.gameObject.name;
        }

        /// <summary>Whether a widget is both active in the hierarchy and worth listing.</summary>
        public static bool IsShown(Component widget)
            => widget != null && widget.gameObject != null && widget.gameObject.activeInHierarchy;
    }
}
