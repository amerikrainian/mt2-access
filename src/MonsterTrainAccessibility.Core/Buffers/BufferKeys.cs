namespace MonsterTrainAccessibility.Core.Buffers
{
    /// <summary>
    /// The registered buffer keys, in cycling order. The ui buffer carries the focused control's own
    /// lines (<see cref="NodeLines"/>: a unit's or a card's head line, then one line per part and
    /// tooltip); the events buffer is the game's event history, newest first; the quotes buffer the
    /// units' chatter, newest first.
    /// </summary>
    public static class BufferKeys
    {
        public const string Ui = "ui";
        public const string Events = "events";
        public const string Quotes = "quotes";
    }
}
