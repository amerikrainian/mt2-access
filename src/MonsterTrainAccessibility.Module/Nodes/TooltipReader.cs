using System.Collections.Generic;
using MonsterTrainAccessibility.Core;
using UnityEngine;

namespace MonsterTrainAccessibility.Module.Nodes
{
    /// <summary>
    /// What the game would show on hover, read from its own pipeline rather than rebuilt: every
    /// <see cref="ITooltipProvider"/> on a widget and under it, its <see cref="TooltipContent"/> in the
    /// game's order, one line per tooltip ("Covenant Rank 1: Enemies are stronger..."), repeats folded
    /// (the game hangs one provider on a button and another on its content). Cards, units and relics
    /// get entry points of their own as their screens come (docs/plan.md, phase 3).
    /// </summary>
    internal static class TooltipReader
    {
        public static List<string> Lines(Component widget)
        {
            var lines = new List<string>();
            if (widget == null) return lines;
            var seen = new HashSet<string>();
            foreach (var provider in widget.GetComponentsInChildren<ITooltipProvider>(false))
            {
                List<TooltipContent> tooltips;
                try { tooltips = provider.Tooltips; }
                catch (System.Exception e)
                {
                    CoreLog.Warning("TooltipReader: " + provider.GetType().Name + " threw reading its tooltips: " + e.Message);
                    continue;
                }
                if (tooltips == null) continue;
                foreach (var tooltip in tooltips)
                {
                    string line = Line(tooltip);
                    if (line != null && seen.Add(line)) lines.Add(line);
                }
            }
            return lines;
        }

        /// <summary>A tooltip as one line: "title: body", or whichever of the two it has.</summary>
        public static string Line(TooltipContent tooltip)
        {
            string title = tooltip.title?.Trim();
            string body = tooltip.body?.Trim();
            if (string.IsNullOrEmpty(title)) return string.IsNullOrEmpty(body) ? null : body;
            return string.IsNullOrEmpty(body) ? title : title + ": " + body;
        }
    }
}
