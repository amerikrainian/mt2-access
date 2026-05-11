using MonsterTrainAccessibility.Localization;

namespace MonsterTrainAccessibility.Events
{
    [EventSettings("pyre.hp")]
    internal sealed class PyreHpEvent : GameEvent
    {
        private readonly int _oldHp;
        private readonly int _newHp;
        private readonly int _totalHp;

        public PyreHpEvent(int oldHp, int newHp, int totalHp)
        {
            _oldHp = oldHp;
            _newHp = newHp;
            _totalHp = totalHp;
        }

        public override Message GetMessage()
        {
            int delta = _newHp - _oldHp;
            if (delta < 0)
            {
                return Message.Localized("events", "PYRE.DAMAGED", new { amount = -delta, hp = _newHp, max = _totalHp });
            }

            if (delta > 0)
            {
                return Message.Localized("events", "PYRE.HEALED", new { amount = delta, hp = _newHp, max = _totalHp });
            }

            return null;
        }
    }
}
