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
        public static NodeVtable Item(Component widget) => Item(() => Lines(widget));

        /// <summary>The same, from tooltip lines read elsewhere (a hidden widget's own list).</summary>
        public static NodeVtable Item(Func<List<string>> lines)
        {
            Func<string> label = () => { var all = lines(); return all.Count > 0 ? all[0] : null; };
            Func<string> rest = () =>
            {
                var all = lines();
                return all.Count > 1 ? GameNodes.Sentences(all.GetRange(1, all.Count - 1)) : null;
            };
            return new NodeVtable
            {
                ControlType = ControlTypes.Text,
                Announcements = new List<NodeAnnouncement> { GameNodes.LabelPart(label), GameNodes.TooltipPart(rest) },
                SearchText = label,
                Details = () => GameNodes.Lines(lines()),
            };
        }

        /// <summary>The title of a widget's first tooltip, cleaned as drawn: the name of a widget the game
        /// names only by its tooltip (a map node).</summary>
        public static string FirstTitle(Component widget)
        {
            var first = First(widget);
            return first.HasValue ? Clean(CardTextHelper.RemoveBold(first.Value.title), title: true) : null;
        }

        /// <summary>The body of a widget's first tooltip, cleaned as drawn.</summary>
        public static string FirstBody(Component widget)
        {
            var first = First(widget);
            return first.HasValue ? Clean(CardTextHelper.ScrubUpgradeHighlights(first.Value.body), title: false) : null;
        }

        private static TooltipContent? First(Component widget)
        {
            if (widget == null) return null;
            foreach (var provider in widget.GetComponentsInChildren<ITooltipProvider>(false))
            {
                try
                {
                    var tips = provider.Tooltips;
                    if (tips != null && tips.Count > 0) return tips[0];
                }
                catch (System.Exception e)
                {
                    CoreLog.Warning("TooltipReader: " + provider.GetType().Name + " threw reading its tooltips: " + e.Message);
                }
            }
            return null;
        }

        private static string Clean(string text, bool title)
        {
            if (title) text = Interop.GameSprites.WithoutEchoedIcons(text);
            text = text?.Trim();
            return string.IsNullOrEmpty(text) ? null : text;
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
                string line = Format(tooltip.title, tooltip.body);
                // The game ships a few placeholders ("(WIP) Pyre Heart tooltip here", the Pyre Heart header's).
                if (line != null && !line.StartsWith("(WIP)")) lines.Add(line);
            }
            return lines;
        }

        /// <summary>One tooltip's line as a container would draw it: the title without its bold, the body
        /// without its upgrade highlights.</summary>
        public static string Format(string title, string body)
            => Line(CardTextHelper.RemoveBold(title), CardTextHelper.ScrubUpgradeHighlights(body));

        /// <summary>The tooltips a container has instantiated, as drawn.</summary>
        public static List<string> Container(TooltipContainer container)
        {
            var lines = new List<string>();
            if (container == null || container.tooltipUIs == null) return lines;
            foreach (var tooltip in container.tooltipUIs)
            {
                if (tooltip == null || tooltip == container.secondaryTooltipPrompt) continue;
                string title = tooltip.titleText != null && tooltip.titleText.gameObject.activeSelf ? tooltip.titleText.Content() : null;
                string body = tooltip.bodyText != null && tooltip.bodyText.gameObject.activeSelf ? tooltip.bodyText.Content() : null;
                string line = Line(title, body);
                if (line != null) lines.Add(line);
            }
            return lines;
        }

        // A title's keyword icon goes when the title already names it (Valor's icon beside "Valor").
        private static string Line(string title, string body)
        {
            title = Interop.GameSprites.WithoutEchoedIcons(title)?.Trim();
            body = body?.Trim();
            if (string.IsNullOrEmpty(title)) return string.IsNullOrEmpty(body) ? null : body;
            return string.IsNullOrEmpty(body) ? title : title + ": " + body;
        }
    }
}
