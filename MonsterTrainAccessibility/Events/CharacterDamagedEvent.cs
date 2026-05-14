using MonsterTrainAccessibility.Localization;

namespace MonsterTrainAccessibility.Events
{
    [EventSettings("character.damage", HasSourceFilter = true)]
    internal sealed class CharacterDamagedEvent : GameEvent
    {
        private readonly string _characterName;
        private readonly int _amount;

        public CharacterDamagedEvent(CharacterState character, int amount)
            : base(EventSource.FromCharacter(character))
        {
            _characterName = Message.RawCleaned(character?.GetName())?.Resolve();
            _amount = amount;
        }

        public override Message GetMessage()
        {
            if (_amount <= 0)
            {
                return null;
            }

            return Message.Localized("events", "CHARACTER.DAMAGED", new { character = _characterName, amount = _amount });
        }
    }
}
