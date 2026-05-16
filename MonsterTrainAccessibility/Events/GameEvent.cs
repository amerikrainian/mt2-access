using MonsterTrainAccessibility.Localization;

namespace MonsterTrainAccessibility.Events
{
    internal abstract class GameEvent
    {
        protected GameEvent(EventSource source = null)
        {
            Source = source;
        }

        public EventSource Source { get; }

        public virtual string BufferKey => "events";

        public abstract Message GetMessage();

        public virtual bool ShouldAnnounce()
        {
            return true;
        }

        public virtual bool ShouldAddToBuffer()
        {
            return true;
        }
    }
}
