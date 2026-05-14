using MonsterTrainAccessibility.Localization;

namespace MonsterTrainAccessibility.Events
{
    [EventSettings("combat.moon_phase")]
    internal sealed class MoonPhaseChangedEvent : GameEvent
    {
        private readonly PlayerManager.MoonPhase _phase;

        public MoonPhaseChangedEvent(PlayerManager.MoonPhase phase)
        {
            _phase = phase;
        }

        public override Message GetMessage()
        {
            return Message.Localized("events", "COMBAT.MOON_PHASE_CHANGED", new
            {
                phase = Message.FromText(_phase.LocalizedName())?.Resolve()
            });
        }
    }
}
