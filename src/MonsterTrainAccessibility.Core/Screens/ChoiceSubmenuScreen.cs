using System;
using System.Collections.Generic;
using MonsterTrainAccessibility.Core.Graph;
using MonsterTrainAccessibility.Core.UI;

namespace MonsterTrainAccessibility.Core.Screens
{
    // Inside this namespace the sibling namespace MonsterTrainAccessibility.Core.Strings shadows the class.
    using Strings = MonsterTrainAccessibility.Core.Strings.Strings;

    /// <summary>One entry of a <see cref="ChoiceSubmenuScreen"/>.</summary>
    public sealed class ChoiceOption
    {
        public string Label;
        /// <summary>Extra spoken detail after the label (a hero's items, a cost), or null.</summary>
        public string Detail;
        public bool Enabled = true;
        public Action OnSelect;
        /// <summary>The caption of the horizontal row this option belongs to, or null for a line of its
        /// own: consecutive options with one caption form a row ("Logan, Warrior, rank C": the hero,
        /// then "slot 1", "slot 2"), Up/Down keeping the column between rows, the caption spoken when
        /// focus enters the row (unless the option landed on already says it).</summary>
        public string Row;

        public ChoiceOption(string label, Action onSelect, string detail = null, bool enabled = true, string row = null)
        {
            Label = label;
            OnSelect = onSelect;
            Detail = detail;
            Enabled = enabled;
            Row = row;
        }
    }

    /// <summary>
    /// A list of options to pick from (a dropdown's values, "equip this item to which hero", a
    /// control's context actions), pushed as a CHILD SCREEN of whatever screen opened it
    /// (<see cref="Screen.PushChild"/>). As a child it is the focused screen while open and owns the
    /// keyboard; selecting an option or backing out removes it, and ScreenManager re-focuses the parent
    /// on its remembered control automatically. Reusable for any "open a list and pick one" interaction.
    /// The option list is immutable per instance; focus starts on <c>current</c> when one is given, so
    /// opening a value picker reads the selected value first.
    ///
    /// The title is spoken ONCE, as the list context the entry readout enters ("Equip Sword to, list,
    /// Bob, button, 1 of 3"). A picker opened from a control whose label the player just heard (a combo
    /// box's option list) passes <c>speakTitle: false</c>: the options are the screen's root items and
    /// the entry reads just the option landed on ("Windowed Borderless, button, selected, 2 of 4").
    /// </summary>
    public sealed class ChoiceSubmenuScreen : Screen
    {
        private readonly string _title;
        private readonly bool _speakTitle;
        private readonly IReadOnlyList<ChoiceOption> _options;
        private readonly int _current;

        public ChoiceSubmenuScreen(string title, IReadOnlyList<ChoiceOption> options, int current = -1, bool speakTitle = true)
        {
            _title = title;
            _speakTitle = speakTitle;
            _options = options ?? new ChoiceOption[0];
            _current = current;
            Wrap = true;
        }

        /// <summary>Open the submenu as a child of the current screen.</summary>
        public static void Open(string title, IReadOnlyList<ChoiceOption> options, int current = -1, bool speakTitle = true)
        {
            ScreenManager.Current?.PushChild(new ChoiceSubmenuScreen(title, options, current, speakTitle));
        }

        /// <summary>The list's title (dev inspection). Not the spoken screen name: the list context
        /// announces it via the path diff on entry, so a screen-name line would say it twice.</summary>
        public string Title => _title;

        public override string Key => "overlay.choice";
        public override bool IsActive() => false; // never poll-pushed: only ever a child screen

        public override IEnumerable<ElementAction> GetActions()
        {
            // Back closes the submenu without choosing.
            yield return new ElementAction(ActionIds.Back, Strings.Get("bind.ui.back"), _ => Close());
        }

        private void Close() => ParentScreen?.RemoveChild(this);

        public override void Build(GraphBuilder b)
        {
            if (_speakTitle) b.PushContext(_title, Strings.RoleList);
            string row = null;
            for (int i = 0; i < _options.Count; i++)
            {
                int idx = i;
                var option = _options[i];
                if (option.Row != row)
                {
                    if (row != null) { b.EndRow(); b.PopContext(); }
                    row = option.Row;
                    // A row's slots name their own numbers: no positions within it.
                    if (row != null) { b.PushContext(row, positions: false); b.StartRow("choice:rows"); }
                }
                // Snapshots are safe: the submenu is ephemeral (a fresh instance per open, closed by the
                // selection itself), so an option cannot change while it lives.
                b.AddItem(ControlId.Structural("choice:" + i), new NodeVtable
                {
                    ControlType = ControlTypes.Button,
                    Announcements = new List<NodeAnnouncement>
                    {
                        new NodeAnnouncement(() => option.Label, kind: AnnouncementKinds.Label),
                        new NodeAnnouncement(() => option.Detail, kind: AnnouncementKinds.Value),
                        new NodeAnnouncement(() => idx == _current ? Strings.StateSelected : null, kind: AnnouncementKinds.Selected),
                        new NodeAnnouncement(() => option.Enabled ? null : Strings.StateDisabled, kind: AnnouncementKinds.Enabled),
                    },
                    SearchText = () => option.Label,
                    OnActivate = () =>
                    {
                        if (!option.Enabled) return;
                        Close();
                        option.OnSelect?.Invoke();
                    },
                });
            }
            if (row != null) { b.EndRow(); b.PopContext(); }
            if (_speakTitle) b.PopContext();
            if (_current >= 0 && _current < _options.Count)
                b.SetStart(ControlId.Structural("choice:" + _current)); // land on the current option
        }
    }
}
