using MonsterTrainAccessibility.Localization;

namespace MonsterTrainAccessibility.Buffers
{
    internal sealed class EventHistoryBuffer : LineBuffer
    {
        public EventHistoryBuffer(string key)
            : base(key)
        {
        }

        protected override void AddSingle(Message item)
        {
            if (Position > 0)
            {
                Position++;
            }

            Contents.Insert(0, item);
        }
    }
}
