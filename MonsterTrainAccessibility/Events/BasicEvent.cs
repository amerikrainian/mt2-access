using MonsterTrainAccessibility.Localization;

namespace MonsterTrainAccessibility.Events
{
    [EventSettings("basic")]
    internal sealed class BasicEvent : GameEvent
    {
        private readonly Message _message;

        public BasicEvent(Message message)
        {
            _message = message;
        }

        public override Message GetMessage()
        {
            return _message;
        }
    }
}
