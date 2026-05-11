using System;
using MonsterTrainAccessibility.Localization;

namespace MonsterTrainAccessibility.UI.Elements
{
    internal sealed class ProxyCombatSpawnPoint : UIElement, INavigationTargetElement
    {
        private readonly SpawnPoint _spawnPoint;
        private readonly int _roomIndex;
        private readonly IRoomNavigationSource _roomNavigation;

        public ProxyCombatSpawnPoint(SpawnPoint spawnPoint, int roomIndex, IRoomNavigationSource roomNavigation)
        {
            _spawnPoint = spawnPoint;
            _roomIndex = roomIndex;
            _roomNavigation = roomNavigation;
        }

        public SpawnPoint SpawnPoint => _spawnPoint;
        public override bool IsVisible => _spawnPoint != null;
        public override Message GetLabel() => Message.Localized("combat", "SPAWN_POINT");
        public override Message GetStatusString() => Message.Localized("combat", "SPAWN_POINT.POSITION", new { position = _spawnPoint != null ? _spawnPoint.GetIndexInRoom() + 1 : 0 });

        public void SelectForNavigation()
        {
            _roomNavigation?.SelectRoom(_roomIndex);
        }
    }
}
