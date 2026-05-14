using MonsterTrainAccessibility.Localization;

namespace MonsterTrainAccessibility.Events
{
    [EventSettings("pyre.hp")]
    internal sealed class PyreHpEvent : GameEvent
    {
        private readonly int _oldHp;
        private readonly int _newHp;

        public PyreHpEvent(int oldHp, int newHp)
        {
            _oldHp = oldHp;
            _newHp = newHp;
        }

        public override Message GetMessage()
        {
            int delta = _newHp - _oldHp;
            if (delta < 0)
            {
                return Message.Localized("events", "PYRE.DAMAGED", new { amount = -delta });
            }

            if (delta > 0)
            {
                return Message.Localized("events", "PYRE.HEALED", new { amount = delta });
            }

            return null;
        }
    }
}
