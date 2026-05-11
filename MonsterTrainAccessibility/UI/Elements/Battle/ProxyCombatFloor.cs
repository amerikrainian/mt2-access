using MonsterTrainAccessibility.Localization;

namespace MonsterTrainAccessibility.UI.Elements
{
    internal sealed class ProxyCombatFloor : UIElement, INavigationTargetElement
    {
        private readonly int _roomIndex;
        private readonly string _labelKey;
        private readonly bool _visible;
        private readonly IRoomNavigationSource _roomNavigation;

        public ProxyCombatFloor(int roomIndex, string labelKey, bool visible, IRoomNavigationSource roomNavigation)
        {
            _roomIndex = roomIndex;
            _labelKey = labelKey;
            _visible = visible;
            _roomNavigation = roomNavigation;
        }

        public int RoomIndex => _roomIndex;

        public override bool IsVisible => _visible;

        public override Message GetLabel() => Message.Localized("combat", _labelKey);

        public void SelectForNavigation()
        {
            _roomNavigation?.SelectRoom(_roomIndex);
        }
    }
}
