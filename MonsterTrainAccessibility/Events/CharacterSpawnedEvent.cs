using MonsterTrainAccessibility.Localization;

namespace MonsterTrainAccessibility.Events
{
    [EventSettings("character.spawned", HasSourceFilter = true)]
    internal sealed class CharacterSpawnedEvent : GameEvent
    {
        private readonly string _characterName;
        private readonly int _floor;

        public CharacterSpawnedEvent(CharacterState character)
            : base(EventSource.FromCharacter(character))
        {
            _characterName = Message.RawCleaned(character?.GetName())?.Resolve();
            _floor = character != null ? character.GetCurrentRoomIndex() + 1 : 0;
        }

        public override Message GetMessage()
        {
            return Message.Localized("events", "CHARACTER.SPAWNED", new { character = _characterName, floor = _floor });
        }
    }
}
