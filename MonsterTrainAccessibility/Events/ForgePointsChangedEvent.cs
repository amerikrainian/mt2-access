using MonsterTrainAccessibility.Localization;

namespace MonsterTrainAccessibility.Events
{
    [EventSettings("resource.forge_points")]
    internal sealed class ForgePointsChangedEvent : GameEvent
    {
        private readonly int _oldPoints;
        private readonly int _newPoints;

        public ForgePointsChangedEvent(int oldPoints, int newPoints)
        {
            _oldPoints = oldPoints;
            _newPoints = newPoints;
        }

        public override Message GetMessage()
        {
            if (_newPoints != _oldPoints)
            {
                return Message.Localized("events", "RESOURCE.FORGE_POINTS", new { amount = _newPoints });
            }

            return null;
        }
    }
}
