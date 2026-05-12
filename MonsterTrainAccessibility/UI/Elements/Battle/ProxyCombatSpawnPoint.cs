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
        public override Message GetLabel()
        {
            int position = _spawnPoint != null ? _spawnPoint.GetIndexInRoom() + 1 : 0;
            CharacterState character = _spawnPoint?.GetCharacterState();
            Message label = Message.Localized("combat", "SPAWN_POINT.POSITION", new { position });
            Message characterName = Message.RawCleaned(character?.GetName());
            return characterName != null
                ? Message.Localized("combat", "SPAWN_POINT.OCCUPIED", new
                {
                    spawnPoint = label.Resolve(),
                    character = characterName.Resolve()
                })
                : label;
        }

        public void SelectForNavigation()
        {
            _roomNavigation?.SelectRoom(_roomIndex);
        }
    }
}
