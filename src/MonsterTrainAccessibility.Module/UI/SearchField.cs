using System;
using System.Collections.Generic;
using MonsterTrainAccessibility.Core.Graph;
using MonsterTrainAccessibility.Core.Strings;
using MonsterTrainAccessibility.Core.UI;
using TMPro;
using Navigation = MonsterTrainAccessibility.Core.UI.Navigation;

namespace MonsterTrainAccessibility.Module.UI
{
    /// <summary>
    /// One of the game's search fields (the Logbook's card and enemy searches), edited with the game's
    /// own editor through <see cref="TextEdit"/>, as Guildrun's compendium search is: a Tab-stop of its
    /// own, since an edit owns the arrows; landing on it starts the edit; Escape or Enter end it with
    /// focus still on the field, which then waits for Enter (or for focus to come back) to be edited
    /// again; Tab and Shift+Tab end it and move on. The game filters as the text changes. A screen
    /// with a search holds one of these and calls <see cref="Update"/> from its <c>OnUpdate</c>.
    /// </summary>
    internal sealed class SearchField
    {
        private readonly ControlId _id;
        private bool _mine;   // the edit under way is this field's
        private bool _rested; // the player ended the edit and focus has not left the field since

        public SearchField(string key)
        {
            Key = key;
            _id = ControlId.Structural(key);
        }

        public string Key { get; }

        public ControlId Id => _id;

        /// <summary>Focus is on this field.</summary>
        public bool Focused
        {
            get { var node = Navigation.FocusedNode; return node != null && Equals(node.Id, _id); }
        }

        /// <summary>"Search, edit, wurm" (or "blank"); Enter edits.</summary>
        public NodeVtable Node(TMP_InputField field, Func<string> label)
        {
            return new NodeVtable
            {
                ControlType = ControlTypes.Edit,
                Announcements = new List<NodeAnnouncement>
                {
                    GameNodes.LabelPart(label),
                    new NodeAnnouncement(() => string.IsNullOrEmpty(field.text) ? Strings.ValueBlank : field.text, kind: AnnouncementKinds.Value),
                },
                SearchText = label,
                OnActivate = () => Begin(field, announce: true),
            };
        }

        /// <summary>Per frame from the screen, for each of its search fields: start the edit as focus
        /// lands on the field, stop this field's edit when focus leaves some other way than the field's
        /// own Tab.</summary>
        public void Update(TMP_InputField field)
        {
            if (!Focused || !Navigation.FocusActive())
            {
                if (_mine) TextEdit.Stop();
                _mine = false;
                _rested = false;
                return;
            }
            if (TextEdit.OwnsKeyboard) { if (_mine) _rested = true; return; }
            _mine = false;
            if (_rested) return;
            if (field != null && field.gameObject.activeInHierarchy) Begin(field, announce: false);
        }

        /// <summary>The screen lost focus or closed: end this field's edit, if one is under way.</summary>
        public void Release()
        {
            if (_mine) TextEdit.Stop();
            _mine = false;
            _rested = false;
        }

        private void Begin(TMP_InputField field, bool announce)
        {
            if (TextEdit.Active) return;
            TextEdit.Begin(field, announce);
            _mine = true;
        }
    }
}
