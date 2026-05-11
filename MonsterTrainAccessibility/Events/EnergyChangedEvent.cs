using MonsterTrainAccessibility.Localization;

namespace MonsterTrainAccessibility.Events
{
    [EventSettings("resource.energy")]
    internal sealed class EnergyChangedEvent : GameEvent
    {
        private readonly int _energy;

        public EnergyChangedEvent(int energy)
        {
            _energy = energy;
        }

        public override Message GetMessage()
        {
            return Message.Localized("events", "RESOURCE.EMBER", new { amount = _energy });
        }
    }
}
