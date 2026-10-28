using System.Collections.Generic;
using MonsterTrainAccessibility.Contracts;
using MonsterTrainAccessibility.Core.Graph;

namespace MonsterTrainAccessibility.Core.Buffers
{
    /// <summary>
    /// The ui buffer's lines for a focused node: its head line (the label, value and state parts of its
    /// readout, never the role word or the "n of m" position), then one line per description part
    /// (<see cref="AnnouncementKinds.Tooltip"/>), then the vtable's <see cref="NodeVtable.Details"/>,
    /// one line per tooltip. A detail that only repeats a head part (an item tooltip whose title is the
    /// item's name, through markup or not) or an earlier detail is folded; blank lines are dropped. Read live on every
    /// buffer keypress. The same conventions as Harkest Dungeon's element buffer lines.
    /// </summary>
    public static class NodeLines
    {
        public static IEnumerable<string> Lines(GraphNode node)
        {
            if (node == null) yield break;
            var head = new List<string>();
            var details = new List<string>();
            var parts = GraphAnnouncer.EffectiveAnnouncements(node);
            for (int i = 0; i < parts.Count; i++)
            {
                var part = parts[i];
                if (part == null || part.Kind == AnnouncementKinds.Role || part.Kind == AnnouncementKinds.Position) continue;
                string text = null;
                try { text = part.Text?.Invoke(); }
                catch (System.Exception e) { CoreLog.Warning("buffer lines: a part failed: " + e.Message); }
                if (string.IsNullOrWhiteSpace(text)) continue;
                if (part.Kind == AnnouncementKinds.Tooltip) details.Add(text);
                else head.Add(text);
            }
            if (head.Count > 0) yield return string.Join(", ", head);

            var headClean = new List<string>();
            foreach (var h in head) headClean.Add(TextFilter.Clean(h));

            IEnumerable<string> own = null;
            if (node.Vtable?.Details != null)
            {
                try { own = node.Vtable.Details(); }
                catch (System.Exception e) { CoreLog.Warning("buffer lines: details failed: " + e.Message); }
            }
            if (own != null) details.AddRange(own);

            // A detail repeating a head part, or an earlier detail (the same tooltip reached twice),
            // is folded: the reader steps through new information only.
            var seen = new HashSet<string>(headClean);
            foreach (var line in details)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                string clean = TextFilter.Clean(line);
                if (string.IsNullOrWhiteSpace(clean) || !seen.Add(clean)) continue;
                yield return line;
            }
        }

        /// <summary>A node's lines for a side buffer, or nothing when it fills none.</summary>
        public static IEnumerable<string> SideLines(GraphNode node, string bufferKey)
        {
            if (node?.Vtable?.SideLines == null) yield break;
            IEnumerable<string> lines = null;
            try { lines = node.Vtable.SideLines(bufferKey); }
            catch (System.Exception e) { CoreLog.Warning("buffer " + bufferKey + ": side lines failed: " + e.Message); }
            if (lines == null) yield break;
            foreach (var line in lines)
                if (!string.IsNullOrWhiteSpace(line)) yield return line;
        }
    }
}
