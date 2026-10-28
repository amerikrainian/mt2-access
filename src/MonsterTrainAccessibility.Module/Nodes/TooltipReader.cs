using System;
using System.Collections.Generic;
using MonsterTrainAccessibility.Core;
using MonsterTrainAccessibility.Core.Graph;
using MonsterTrainAccessibility.Core.UI;
using MonsterTrainAccessibility.Module.UI;
using UnityEngine;

namespace MonsterTrainAccessibility.Module.Nodes
{
    /// <summary>
    /// What the game shows on hover, never rebuilt: one line per tooltip ("title: body"), in the game's
    /// order, with the game's own de-duplication and text clean-up.
    /// <list type="bullet">
    /// <item><see cref="Lines(Component)"/>: a widget's <see cref="ITooltipProvider"/>s (buttons,
    /// artifacts, meters). The game hands a provider's list to a tooltip container on hover
    /// (<c>ShowNewTooltips</c>); the same rules are applied here as the container applies them:
    /// a tooltip is keyed by its id, else its title, else its body, and a key seen before (or ending
    /// "_CardText" of one seen) or naming "EmptyString" is dropped; the title loses its bold and the
    /// body its upgrade highlights (<c>TooltipUI.Set</c>).</item>
    /// <item><see cref="Container"/>: a tooltip container the game has populated (a card's), read off
    /// the tooltips it instantiated, exactly as drawn. The secondary panel some move to is layout
    /// only and is read like the rest; the game's prompt to show it is not a tooltip and is skipped.</item>
    /// </list>
    /// <see cref="Item"/> is a read-only line made of a widget's tooltips (a meter, a victory marker).
    /// Tooltips are flat lists in this game: a tooltip never opens another; content one generates
    /// (a card a relic creates) is a further entry in the same list.
    /// </summary>
    internal static class TooltipReader
    {
        public static List<string> Lines(Component widget)
        {
            var tooltips = new List<TooltipContent>();
            if (widget == null) return new List<string>();
            foreach (var provider in widget.GetComponentsInChildren<ITooltipProvider>(false))
            {
                try
                {
                    if (provider.Tooltips != null) tooltips.AddRange(provider.Tooltips);
                }
                catch (System.Exception e)
                {
                    CoreLog.Warning("TooltipReader: " + provider.GetType().Name + " threw reading its tooltips: " + e.Message);
                }
            }
            return Lines(tooltips);
        }

        /// <summary>A widget read by its tooltips: the first one's title (else its body) heads the line,
        /// the rest follow it, each its own line in the buffer.</summary>
        public static NodeVtable Item(Component widget)
        {
            Func<string> label = () => { var lines = Lines(widget); return lines.Count > 0 ? lines[0] : null; };
            Func<string> rest = () =>
            {
                var lines = Lines(widget);
                return lines.Count > 1 ? GameNodes.Sentences(lines.GetRange(1, lines.Count - 1)) : null;
            };
            return new NodeVtable
            {
                ControlType = ControlTypes.Text,
                Announcements = new List<NodeAnnouncement> { GameNodes.LabelPart(label), GameNodes.TooltipPart(rest) },
                SearchText = label,
                Details = () => GameNodes.Lines(Lines(widget)),
            };
        }

        /// <summary>A tooltip list as a container would show it.</summary>
        public static List<string> Lines(IEnumerable<TooltipContent> tooltips)
        {
            var lines = new List<string>();
            var keys = new HashSet<string>();
            foreach (var tooltip in tooltips)
            {
                string key = !string.IsNullOrEmpty(tooltip.tooltipId) ? tooltip.tooltipId
                    : string.IsNullOrEmpty(tooltip.title) ? tooltip.body : tooltip.title;
                if (key == null || key.Contains("EmptyString")) continue;
                if (keys.Contains(key) || keys.Contains(key.Replace("_CardText", ""))) continue;
                keys.Add(key);
                string line = Line(CardTextHelper.RemoveBold(tooltip.title), CardTextHelper.ScrubUpgradeHighlights(tooltip.body));
                if (line != null) lines.Add(line);
            }
            return lines;
        }

        /// <summary>The tooltips a container has instantiated, as drawn.</summary>
        public static List<string> Container(TooltipContainer container)
        {
            var lines = new List<string>();
            if (container == null || container.tooltipUIs == null) return lines;
            foreach (var tooltip in container.tooltipUIs)
            {
                if (tooltip == null || tooltip == container.secondaryTooltipPrompt) continue;
                string title = tooltip.titleText != null && tooltip.titleText.gameObject.activeSelf ? tooltip.titleText.text : null;
                string body = tooltip.bodyText != null && tooltip.bodyText.gameObject.activeSelf ? tooltip.bodyText.text : null;
                string line = Line(title, body);
                if (line != null) lines.Add(line);
            }
            return lines;
        }

        private static string Line(string title, string body)
        {
            title = title?.Trim();
            body = body?.Trim();
            if (string.IsNullOrEmpty(title)) return string.IsNullOrEmpty(body) ? null : body;
            return string.IsNullOrEmpty(body) ? title : title + ": " + body;
        }
    }
}
