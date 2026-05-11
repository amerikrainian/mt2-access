using MonsterTrainAccessibility.Localization;

namespace MonsterTrainAccessibility.Events
{
    [EventSettings("pyre.armor")]
    internal sealed class PyreArmorEvent : GameEvent
    {
        private readonly int _armor;

        public PyreArmorEvent(int armor)
        {
            _armor = armor;
        }

        public override Message GetMessage()
        {
            return Message.Localized("events", "PYRE.ARMOR", new { amount = _armor });
        }
    }
}
