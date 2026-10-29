using System;
using System.Collections.Generic;
using MonsterTrainAccessibility.Core.Graph;
using MonsterTrainAccessibility.Core.UI;
using MonsterTrainAccessibility.Module.Interop;
using MonsterTrainAccessibility.Module.UI;
using ShinyShoe;
using TMPro;
using UnityEngine;

namespace MonsterTrainAccessibility.Module.Nodes
{
    /// <summary>
    /// The run's HUD widgets as lines, in the game's words: a counter by the game's own title for it
    /// and the number it draws ("Pyre Health, 50 / 50", "Coin, 50"); a stat of the train by what its
    /// tooltip says it is and its number; an artifact by its tooltips. Every line keeps the widget's
    /// tooltips in the buffer. A widget the game lets the player press (the deck, the hoard, the game
    /// speed) is a button pressed through the game.
    /// </summary>
    internal static class HudNodes
    {
        /// <summary>"Coin, 50": a counter by the game's title term and its drawn number; a button when
        /// pressing it does something.</summary>
        public static NodeVtable Count(Component widget, string titleTerm, IGameUIComponent press = null)
        {
            // Some titles draw the counter's icon before its name ("[hoard icon] Dragon's Hoard").
            Func<string> label = () => GameSprites.WithoutEchoedIcons(GameText.Term(titleTerm))?.Trim();
            var value = new NodeAnnouncement(() => Drawn(widget), kind: AnnouncementKinds.Value);
            NodeVtable vt;
            if (press is GameUISelectableButton button) vt = GameWidgets.Button(button, label, null, value);
            else
            {
                vt = new NodeVtable
                {
                    ControlType = ControlTypes.Text,
                    Announcements = new List<NodeAnnouncement> { GameNodes.LabelPart(label), value },
                    SearchText = label,
                };
                if (press != null) vt.OnActivate = () => GameInput.Submit(press);
            }
            vt.Details = () => GameNodes.Lines(TooltipReader.Lines(widget));
            return vt;
        }

        /// <summary>"Game Speed, button, Current speed: Ultra": a toggle the game names in its tooltip.</summary>
        public static NodeVtable Tooltipped(Component widget, IGameUIComponent press)
        {
            Func<string> label = () => TooltipReader.FirstTitle(widget);
            var vt = new NodeVtable
            {
                ControlType = ControlTypes.Button,
                Announcements = new List<NodeAnnouncement>
                {
                    GameNodes.LabelPart(label),
                    new NodeAnnouncement(() => TooltipReader.FirstBody(widget), live: true, kind: AnnouncementKinds.Value),
                },
                SearchText = label,
                Details = () => GameNodes.Lines(TooltipReader.Lines(widget)),
            };
            if (press != null) vt.OnActivate = () => GameInput.Submit(press);
            vt.StateText = () => TooltipReader.FirstBody(widget);
            return vt;
        }

        /// <summary>A train stat: "Ember gained at the start of each turn: 3", its tooltip and the number
        /// the game puts beside its icon.</summary>
        public static NodeVtable TrainStat(TMP_Text number)
        {
            Func<string> label = () =>
            {
                var stat = number.transform.parent;
                string what = TooltipReader.FirstBody(stat) ?? TooltipReader.FirstTitle(stat);
                return what != null ? what.TrimEnd('.', ' ') : null;
            };
            return new NodeVtable
            {
                ControlType = ControlTypes.Text,
                Announcements = new List<NodeAnnouncement>
                {
                    GameNodes.LabelPart(label),
                    new NodeAnnouncement(() => number.Content(), kind: AnnouncementKinds.Value),
                },
                SearchText = label,
            };
        }

        /// <summary>An artifact, a mutator, an enemy's artifact: by its own tooltips, read off the icon
        /// whether or not its page of the collection is on screen.</summary>
        public static NodeVtable Relic(RelicTooltipProvider icon)
            => TooltipReader.Item(() => TooltipReader.Lines(icon.Tooltips ?? new List<TooltipContent>()));

        // The texts a widget draws, in order: its number, however it is laid out. A current-over-maximum
        // count is two lines with the divider drawn between them ("50", "50"), read as "50 / 50".
        private static string Drawn(Component widget)
        {
            var texts = new List<string>();
            foreach (var text in widget.GetComponentsInChildren<TMP_Text>(false))
            {
                if (string.IsNullOrWhiteSpace(text.Content()) || text.GetComponentInParent<GameUIControlMapping>() != null) continue;
                foreach (var line in text.Content().Split('\n'))
                    if (!string.IsNullOrWhiteSpace(line)) texts.Add(line.Trim());
            }
            return texts.Count > 0 ? string.Join(" / ", texts) : null;
        }
    }
}
