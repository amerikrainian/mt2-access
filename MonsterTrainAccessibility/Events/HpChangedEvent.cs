using MonsterTrainAccessibility.Localization;

namespace MonsterTrainAccessibility.Events
{
    [EventSettings("character.hp", HasSourceFilter = true)]
    internal sealed class HpChangedEvent : GameEvent
    {
        private readonly string _characterName;
        private readonly int _oldHp;
        private readonly int _newHp;

        public HpChangedEvent(CharacterState character, int oldHp, int newHp)
            : base(EventSource.FromCharacter(character))
        {
            _characterName = Message.RawCleaned(character?.GetName())?.Resolve();
            _oldHp = oldHp;
            _newHp = newHp;
        }

        public override Message GetMessage()
        {
            int delta = _newHp - _oldHp;
            if (delta < 0)
            {
                return Message.Localized("events", "CHARACTER.DAMAGED", new { character = _characterName, amount = -delta });
            }

            if (delta > 0)
            {
                return Message.Localized("events", "CHARACTER.HEALED", new { character = _characterName, amount = delta });
            }

            return null;
        }
    }
}
