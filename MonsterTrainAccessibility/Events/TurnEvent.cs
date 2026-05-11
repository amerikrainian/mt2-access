using MonsterTrainAccessibility.Localization;

namespace MonsterTrainAccessibility.Events
{
    [EventSettings("combat.turn")]
    internal sealed class TurnEvent : GameEvent
    {
        private readonly int _turn;
        private readonly int _totalWaves;
        private readonly bool _looping;

        public TurnEvent(int turn, int totalWaves, bool looping)
        {
            _turn = turn;
            _totalWaves = totalWaves;
            _looping = looping;
        }

        public override Message GetMessage()
        {
            if (_turn <= 0)
            {
                return Message.Localized("events", "COMBAT.DEPLOYMENT");
            }

            if (_looping || _totalWaves <= 0)
            {
                return Message.Localized("events", "COMBAT.TURN", new { turn = _turn });
            }

            return Message.Localized("events", "COMBAT.TURN_OF", new { turn = _turn, total = _totalWaves });
        }
    }
}
