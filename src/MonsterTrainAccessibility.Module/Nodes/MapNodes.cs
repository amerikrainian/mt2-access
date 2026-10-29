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
    /// stop is a choice the player stands on or one the route preview passes. The whole run's map
    /// (the Minimap) reads its nodes and battles the same way.
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

        /// <summary>A node on the whole run's map (<see cref="MinimapNodeMarker"/>): its name as drawn,
        /// then what it offers, its tooltip.</summary>
        public static NodeVtable Marker(MinimapNodeMarker marker)
        {
            Func<string> name = () =>
            {
                string drawn = marker.label != null ? marker.label.Content() : null;
                return !string.IsNullOrWhiteSpace(drawn) ? drawn : marker.mapNodeData?.GetTooltipTitle();
            };
            return new NodeVtable
            {
                ControlType = ControlTypes.Text,
                Announcements = new List<NodeAnnouncement>
                {
                    GameNodes.LabelPart(name),
                    GameNodes.TooltipPart(() => TooltipReader.FirstBody(marker.tooltipProvider)),
                },
                SearchText = name,
            };
        }

        /// <summary>A ring's closing battle on the whole run's map: "Battle, A foe awaits you.", a boss by
        /// its name and story.</summary>
        public static NodeVtable MinimapBattle(MinimapBattleNode battle)
        {
            Func<string> name = () => TooltipReader.FirstTitle(battle.tooltipProvider);
            return new NodeVtable
            {
                ControlType = ControlTypes.Text,
                Announcements = new List<NodeAnnouncement>
                {
                    GameNodes.LabelPart(name),
                    GameNodes.TooltipPart(() => TooltipReader.FirstBody(battle.tooltipProvider)),
                },
                SearchText = name,
                Details = () => GameNodes.Lines(TooltipReader.Lines(battle.tooltipProvider)),
            };
        }

        private static string Name(MapStop stop) => TooltipReader.FirstTitle(stop.Widget);

        // Visited is the game's mark on the ring the train is in; elsewhere it means nothing yet.
        private static string State(MapStop stop, int currentRing)
            => !stop.IsBattle && stop.Ring == currentRing && stop.Node.visited ? Strings.StateVisited : null;
    }
}
