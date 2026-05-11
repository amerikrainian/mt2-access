using MonsterTrainAccessibility.Localization;

namespace MonsterTrainAccessibility.Events
{
    [EventSettings("resource.gold")]
    internal sealed class GoldChangedEvent : GameEvent
    {
        private readonly int _oldGold;
        private readonly int _newGold;

        public GoldChangedEvent(int oldGold, int newGold)
        {
            _oldGold = oldGold;
            _newGold = newGold;
        }

        public override Message GetMessage()
        {
            int delta = _newGold - _oldGold;
            if (delta > 0)
            {
                return Message.Localized("events", "RESOURCE.GOLD_GAINED", new { amount = delta, total = _newGold });
            }

            if (delta < 0)
            {
                return Message.Localized("events", "RESOURCE.GOLD_LOST", new { amount = -delta, total = _newGold });
            }

            return null;
        }
    }
}
