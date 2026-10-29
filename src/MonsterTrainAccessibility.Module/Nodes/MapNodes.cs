using System;
using System.Collections.Generic;
using MonsterTrainAccessibility.Core.Graph;
using MonsterTrainAccessibility.Core.Strings;
using MonsterTrainAccessibility.Core.UI;
using MonsterTrainAccessibility.Module.Map;
using MonsterTrainAccessibility.Module.UI;

namespace MonsterTrainAccessibility.Module.Nodes
{
    /// <summary>
    /// A stop on a run's route as the map says it: its name and what it offers, both the game's own
    /// tooltip ("Light Forge", "Upgrade your Champion."; a boss battle by its name and story), and
    /// "visited" for a node already visited in the ring the train is in. The same words whether the
    /// stop is a choice the player stands on or one the route preview passes.
    /// </summary>
    internal static class MapNodes
    {
        /// <summary>A choice: "Light Forge, button, Upgrade your Champion."; landing on it shows it as
        /// hovered, Enter takes it.</summary>
        public static NodeVtable Choice(MapStop stop, IRunRoute route, Func<int> currentRing, Action landed)
        {
            Func<string> name = () => Name(stop);
            return new NodeVtable
            {
                ControlType = ControlTypes.Button,
                Announcements = new List<NodeAnnouncement>
                {
                    GameNodes.LabelPart(name),
                    new NodeAnnouncement(() => State(stop, currentRing()), kind: AnnouncementKinds.Value),
                    GameNodes.TooltipPart(() => TooltipReader.FirstBody(stop.Widget)),
                },
                SearchText = name,
                OnFocus = () =>
                {
                    landed?.Invoke();
                    route.Preview(stop);
                },
                OnActivate = () => route.Choose(stop),
                Details = () => GameNodes.Lines(TooltipReader.Lines(stop.Widget)),
            };
        }

        /// <summary>The stop in one line, for the route preview: "Light Forge, Upgrade your Champion."</summary>
        public static string Describe(MapStop stop, int currentRing)
        {
            var parts = new List<string>();
            foreach (var part in new[] { Name(stop), State(stop, currentRing), TooltipReader.FirstBody(stop.Widget) })
                if (!string.IsNullOrWhiteSpace(part)) parts.Add(part);
            return parts.Count > 0 ? string.Join(", ", parts) : null;
        }

        private static string Name(MapStop stop) => TooltipReader.FirstTitle(stop.Widget);

        // Visited is the game's mark on the ring the train is in; elsewhere it means nothing yet.
        private static string State(MapStop stop, int currentRing)
            => !stop.IsBattle && stop.Ring == currentRing && stop.Node.visited ? Strings.StateVisited : null;
    }
}
