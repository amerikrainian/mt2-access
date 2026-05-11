using MonsterTrainAccessibility.Localization;

namespace MonsterTrainAccessibility.Events
{
    [EventSettings("character.moved", HasSourceFilter = true)]
    internal sealed class CharacterMovedEvent : GameEvent
    {
        private readonly string _characterName;
        private readonly int _oldFloor;
        private readonly int _newFloor;

        public CharacterMovedEvent(CharacterState character, int oldFloor, int newFloor)
            : base(EventSource.FromCharacter(character))
        {
            _characterName = Message.RawCleaned(character?.GetName())?.Resolve();
            _oldFloor = oldFloor;
            _newFloor = newFloor;
        }

        public override Message GetMessage()
        {
            return Message.Localized("events", "CHARACTER.MOVED", new { character = _characterName, oldFloor = _oldFloor, newFloor = _newFloor });
        }
    }
}
