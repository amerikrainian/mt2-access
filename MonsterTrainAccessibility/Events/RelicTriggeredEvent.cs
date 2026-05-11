using MonsterTrainAccessibility.Localization;

namespace MonsterTrainAccessibility.Events
{
    [EventSettings("relic.triggered")]
    internal sealed class RelicTriggeredEvent : GameEvent
    {
        private readonly Message _relic;

        public RelicTriggeredEvent(RelicState relic)
            : base(EventSource.FromRelic(relic))
        {
            _relic = Message.RawCleaned(relic?.GetName());
        }

        public override Message GetMessage()
        {
            if (_relic == null)
            {
                return null;
            }

            return Message.Localized("events", "RELIC.TRIGGERED", new { relic = _relic.Resolve() });
        }
    }
}
