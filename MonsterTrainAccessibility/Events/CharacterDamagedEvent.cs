using MonsterTrainAccessibility.Localization;

namespace MonsterTrainAccessibility.Events
{
    [EventSettings("character.damage", HasSourceFilter = true)]
    internal sealed class CharacterDamagedEvent : GameEvent
    {
        private readonly string _characterName;
        private readonly int _amount;
        private readonly int _hp;
        private readonly int _maxHp;

        public CharacterDamagedEvent(CharacterState character, int amount, int hp, int maxHp)
            : base(EventSource.FromCharacter(character))
        {
            _characterName = Message.RawCleaned(character?.GetName())?.Resolve();
            _amount = amount;
            _hp = hp;
            _maxHp = maxHp;
        }

        public override Message GetMessage()
        {
            if (_amount <= 0)
            {
                return null;
            }

            return Message.Localized("events", "CHARACTER.DAMAGED", new { character = _characterName, amount = _amount, hp = _hp, max = _maxHp });
        }
    }
}
