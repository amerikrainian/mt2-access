using MonsterTrainAccessibility.Localization;

namespace MonsterTrainAccessibility.Events
{
    [EventSettings("character.removed", HasSourceFilter = true)]
    internal sealed class CharacterRemovedEvent : GameEvent
    {
        private readonly string _characterName;

        public CharacterRemovedEvent(CharacterState character)
            : base(EventSource.FromCharacter(character))
        {
            _characterName = Message.RawCleaned(character?.GetName())?.Resolve();
        }

        public override Message GetMessage()
        {
            return Message.Localized("events", "CHARACTER.REMOVED", new { character = _characterName });
        }
    }
}
