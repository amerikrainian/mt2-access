using System.Collections.Generic;
using MonsterTrainAccessibility.Localization;

namespace MonsterTrainAccessibility.Map
{
    internal static class MapNodeAnnouncementFormatter
    {
        public static Message DescribeNode(MapNode node, IReadOnlyList<MapNode> rowNodes, bool includeChoicePrefix)
        {
            if (node == null)
            {
                return null;
            }

            List<Message> parts = new List<Message>();
            if (includeChoicePrefix && rowNodes != null && rowNodes.Count > 1)
            {
                parts.Add(Message.Localized("map_nav", "NAV.CHOICE"));
            }

            string type = node.GetDisplayName();
            string details = node.GetDetails();
            parts.Add(Message.RawCleaned(type));

            if (!string.IsNullOrWhiteSpace(details))
            {
                parts.Add(Message.RawCleaned(details));
            }

            return Message.Join(", ", parts);
        }

        public static Message NoForward() => Message.Localized("map_nav", "NAV.NO_FORWARD");

        public static Message NoBackward() => Message.Localized("map_nav", "NAV.NO_BACKWARD");
    }
}
