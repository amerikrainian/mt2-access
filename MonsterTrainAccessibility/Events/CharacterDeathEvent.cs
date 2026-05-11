using MonsterTrainAccessibility.Localization;

namespace MonsterTrainAccessibility.Events
{
    [EventSettings("character.died", HasSourceFilter = true)]
    internal sealed class CharacterDeathEvent : GameEvent
    {
        private readonly string _characterName;

        public CharacterDeathEvent(CharacterState character)
            : base(EventSource.FromCharacter(character))
        {
            _characterName = Message.RawCleaned(character?.GetName())?.Resolve();
        }

        public override Message GetMessage()
        {
            return Message.Localized("events", "CHARACTER.DIED", new { character = _characterName });
        }
    }
}
