using MonsterTrainAccessibility.ModSettings;

namespace MonsterTrainAccessibility.UI.Elements
{
    internal sealed class RowContainer : ListContainer
    {
        public RowContainer(string label)
        {
            ContainerLabel = label;
            AnnounceName = true;
            AnnouncePosition = true;
            NavigationAxis = NavigationAxis.Horizontal;
        }

        public Setting Tag { get; set; }
    }
}
