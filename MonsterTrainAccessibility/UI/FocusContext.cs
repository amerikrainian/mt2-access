using System.Collections.Generic;
using MonsterTrainAccessibility.Localization;
using MonsterTrainAccessibility.UI.Elements;

namespace MonsterTrainAccessibility.UI
{
    public sealed class FocusContext
    {
        private List<Container> _lastPath = new List<Container>();

        public Message BuildAnnouncement(UIElement element)
        {
            if (element == null)
            {
                return null;
            }

            List<Container> newPath = BuildPath(element);
            int divergeIndex = FindDivergenceIndex(_lastPath, newPath);
            _lastPath = newPath;

            List<Message> parts = new List<Message>();

            for (int i = divergeIndex; i < newPath.Count; i++)
            {
                Container container = newPath[i];
                if (container.AnnounceName && !string.IsNullOrEmpty(container.ContainerLabel))
                {
                    parts.Add(Message.Raw(container.ContainerLabel));
                }
            }

            Message focusMessage = element.GetFocusMessage();
            if (focusMessage != null)
            {
                parts.Add(focusMessage);
            }

            Container parent = element.Parent;
            if (parent != null && parent.AnnouncePosition)
            {
                Message posMsg = parent.GetPositionString(element);
                if (posMsg != null)
                {
                    parts.Add(posMsg);
                }
            }

            if (parts.Count == 0)
            {
                return null;
            }

            return Message.Join(", ", parts);
        }

        public void Reset()
        {
            _lastPath.Clear();
        }

        private static List<Container> BuildPath(UIElement element)
        {
            List<Container> path = new List<Container>();
            Container current = element.Parent;
            while (current != null)
            {
                path.Add(current);
                current = current.Parent;
            }

            path.Reverse();
            return path;
        }

        private static int FindDivergenceIndex(List<Container> oldPath, List<Container> newPath)
        {
            int min = oldPath.Count < newPath.Count ? oldPath.Count : newPath.Count;
            for (int i = 0; i < min; i++)
            {
                if (!SameContainer(oldPath[i], newPath[i]))
                {
                    return i;
                }
            }

            return newPath.Count > oldPath.Count ? oldPath.Count : newPath.Count;
        }

        private static bool SameContainer(Container oldContainer, Container newContainer)
        {
            if (ReferenceEquals(oldContainer, newContainer))
            {
                return true;
            }

            if (oldContainer == null || newContainer == null)
            {
                return false;
            }

            return oldContainer.GetType() == newContainer.GetType() &&
                string.Equals(oldContainer.ContainerLabel, newContainer.ContainerLabel, System.StringComparison.Ordinal);
        }
    }
}
