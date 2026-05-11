using MonsterTrainAccessibility.Localization;

namespace MonsterTrainAccessibility.Events
{
    [EventSettings("character.ability", HasSourceFilter = true)]
    internal sealed class UnitAbilityEvent : GameEvent
    {
        private readonly string _characterName;
        private readonly bool _available;

        public UnitAbilityEvent(CharacterState character, bool available)
            : base(EventSource.FromCharacter(character))
        {
            _characterName = Message.RawCleaned(character?.GetName())?.Resolve();
            _available = available;
        }

        public override Message GetMessage()
        {
            return Message.Localized("events", _available ? "ABILITY.AVAILABLE" : "ABILITY.UNAVAILABLE", new { character = _characterName });
        }
    }
}
