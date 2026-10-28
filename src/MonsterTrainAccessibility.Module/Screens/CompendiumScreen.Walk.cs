using System.Collections.Generic;
using MonsterTrainAccessibility.Core.Graph;
using MonsterTrainAccessibility.Core.Strings;
using MonsterTrainAccessibility.Core.UI;
using MonsterTrainAccessibility.Module.Interop;
using MonsterTrainAccessibility.Module.Nodes;
using MonsterTrainAccessibility.Module.UI;
using ShinyShoe;
using TMPro;
using UnityEngine;

namespace MonsterTrainAccessibility.Module.Screens
{
    public sealed partial class CompendiumScreen
    {
        // ---- the other sections: what is drawn, in the game's order ----

        // The sections laid out as panels of texts and widgets (Clans, Mastery Card Frames, Progress
        // Record): every shown text and every widget, in hierarchy order. A clan's progress and a frame
        // choice read through their own nodes; a widget with a tooltip reads its title (else its
        // caption) and keeps the tooltips in the buffer; a button presses through the game; a dropdown
        // or a toggle is the shared widget; a card reads as a card.
        private static void Walk(GraphBuilder b, Transform t, string path, Transform skip = null)
        {
            if (!t.gameObject.activeInHierarchy || t == skip) return;
            if (t.GetComponent<GameUIControlMapping>() != null) return; // a key hint
            string id = path + "/" + t.GetSiblingIndex();

            var card = t.GetComponent<CardUI>();
            if (card != null && card.GetCardState() != null)
            {
                b.AddItem(ControlId.Structural(id), CardNodes.Card(card, CardTooltipContainer.LoreTooltipSetting.Always, null));
                return;
            }
            var clan = t.GetComponent<ClanChecklistSection>();
            if (clan != null)
            {
                ClanNodes.Progress(b, id, clan, AllGameManagers.Instance.GetSaveManager());
                return;
            }
            var meter = t.GetComponent<CovenantRankMeter>();
            if (meter != null)
            {
                // The rank by the meter's tooltip ("Covenant Rank 5/10"), then the player's and the
                // friends' markers; the scale's tick numbers say nothing the rank does not.
                if (meter.tooltipProvider != null)
                    b.AddItem(ControlId.Structural(id + "/rank"), TooltipReader.Item(meter.tooltipProvider));
                int m = 0;
                foreach (var marker in meter.GetComponentsInChildren<AvatarProgressIndicator>(false))
                    Walk(b, marker.transform, id + "/marker" + m++, skip);
                return;
            }
            var board = t.GetComponent<CompendiumStatsLeaderboardPage>();
            if (board != null)
            {
                Leaderboard(b, id, board);
                return;
            }
            var stat = t.GetComponent<RunStatRow>();
            if (stat != null)
            {
                // "Cards drawn: 359", or the name alone where the game shows no value.
                b.AddItem(ControlId.Structural(id), GameNodes.Text(() =>
                {
                    string value = Spoken(stat.statValueLabel) ? stat.statValueLabel.text : null;
                    return value != null ? stat.statNameLabel.text + ": " + value : stat.statNameLabel.text;
                }));
                return;
            }
            var frame = t.GetComponent<CardFrameOptionUI>();
            if (frame != null)
            {
                b.AddItem(ControlId.Structural(id), CardFrameNodes.Option(frame));
                return;
            }
            var dropdown = t.GetComponent<GameUISelectableDropdown>();
            if (dropdown != null)
            {
                b.AddItem(ControlId.Structural(id), GameWidgets.Dropdown(dropdown, () => FilterNodes.Label(t.parent != null ? t.parent : t)));
                return;
            }
            var toggle = t.GetComponent<GameUISelectableToggle>();
            if (toggle != null)
            {
                b.AddItem(ControlId.Structural(id), GameWidgets.Toggle(toggle, () => FilterNodes.Label(t.parent != null ? t.parent : t)));
                return;
            }
            var provider = t.GetComponent<ITooltipProvider>();
            var button = t.GetComponent<GameUISelectableButton>();
            if (provider != null || button != null)
            {
                var widget = t;
                // A widget with a tooltip reads by it (its title, else its body; the body after a title):
                // what it means is there, while its drawn caption is often a bare number or an icon's.
                System.Func<string> label = () => TooltipLabel(widget.GetComponent<ITooltipProvider>()) ?? Caption(widget);
                System.Func<string> description = () => TooltipDescription(widget.GetComponent<ITooltipProvider>());
                if (label() != null || provider != null)
                {
                    var vt = button != null
                        ? GameWidgets.Button(button, label, description)
                        : new NodeVtable
                        {
                            ControlType = ControlTypes.Item,
                            Announcements = new List<NodeAnnouncement> { GameNodes.LabelPart(label), GameNodes.TooltipPart(description) },
                            SearchText = label,
                        };
                    var selectable = t.GetComponent<IGameUIComponent>();
                    if (selectable != null) vt.OnFocus = () => GameInput.Hover(selectable);
                    vt.Details = () => GameNodes.Lines(TooltipReader.Lines(widget));
                    b.AddItem(ControlId.Structural(id), vt);
                    return;
                }
            }
            var text = t.GetComponent<TMP_Text>();
            if (Spoken(text))
                b.AddItem(ControlId.Structural(id), GameNodes.Text(() => text.text));
            for (int i = 0; i < t.childCount; i++)
            {
                var child = t.GetChild(i);
                // A caption followed by its number ("Cards drawn", "359"; "Level", "10") is one line.
                var label = PlainText(child);
                var value = i + 1 < t.childCount ? PlainText(t.GetChild(i + 1)) : null;
                if (label != null && value != null && IsNumber(value.text))
                {
                    var l = label;
                    var v = value;
                    b.AddItem(ControlId.Structural(id + "/" + i), GameNodes.Text(() => l.text + ": " + v.text));
                    i++;
                    continue;
                }
                Walk(b, child, id, skip);
            }
        }

        // A shown text with nothing beneath it: a bare caption or value.
        private static TMP_Text PlainText(Transform t)
        {
            if (!t.gameObject.activeInHierarchy) return null;
            foreach (Transform child in t)
                if (child.GetComponent<TMP_SubMeshUI>() == null) return null; // a text's own font sub-meshes are not content
            var text = t.GetComponent<TMP_Text>();
            return Spoken(text) ? text : null;
        }

        // A text that says something once its markup and invisible characters are gone.
        private static bool Spoken(TMP_Text text)
            => text != null && text.enabled && text.gameObject.activeInHierarchy
               && Contracts.TextFilter.Clean(text.text).Length > 0;

        // A count or amount as the game prints one: digits with separators, a fraction, a percentage.
        private static bool IsNumber(string text)
        {
            string clean = Contracts.TextFilter.Clean(text);
            if (clean.Length == 0) return false;
            bool digit = false;
            foreach (char c in clean)
            {
                if (char.IsDigit(c)) { digit = true; continue; }
                if (c == ',' || c == '.' || c == '/' || c == '%' || c == '+' || c == '-' || c == ' ' || c == ':' || c == '(' || c == ')') continue;
                return false;
            }
            return digit;
        }

        private static string TooltipLabel(ITooltipProvider provider)
        {
            var tips = provider?.Tooltips;
            if (tips == null || tips.Count == 0) return null;
            string title = CardTextHelper.RemoveBold(tips[0].title);
            return !string.IsNullOrWhiteSpace(title) ? title : CardTextHelper.ScrubUpgradeHighlights(tips[0].body);
        }

        private static string TooltipDescription(ITooltipProvider provider)
        {
            var tips = provider?.Tooltips;
            if (tips == null || tips.Count == 0 || string.IsNullOrWhiteSpace(tips[0].title)) return null;
            return CardTextHelper.ScrubUpgradeHighlights(tips[0].body);
        }

        // A widget's own shown texts, joined: its caption.
        private static string Caption(Transform widget)
        {
            var parts = new List<string>();
            foreach (var text in widget.GetComponentsInChildren<TMP_Text>(false))
            {
                if (string.IsNullOrWhiteSpace(text.text) || text.GetComponentInParent<GameUIControlMapping>() != null) continue;
                parts.Add(text.text.Trim());
            }
            return parts.Count > 0 ? string.Join(", ", parts) : null;
        }
    }
}
