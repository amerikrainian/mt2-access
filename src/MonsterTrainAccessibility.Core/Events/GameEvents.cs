using System;
using System.Collections.Generic;
using MonsterTrainAccessibility.Contracts;

namespace MonsterTrainAccessibility.Core.Events
{
    /// <summary>
    /// A log of event lines, newest first (the history the events buffer reviews). Bounded: the oldest
    /// lines fall off past <see cref="Capacity"/>. <see cref="Added"/> tells a reviewing buffer how many
    /// lines went in on top, so its cursor stays on the line it was on.
    /// </summary>
    public sealed class EventLog
    {
        public const int Capacity = 500;
        private readonly List<string> _lines = new List<string>();

        public IReadOnlyList<string> Lines => _lines;

        /// <summary>Raised with the number of lines put on top.</summary>
        public event Action<int> Added;

        /// <summary>One event's text; a multi-line text goes in as its lines in their own order.</summary>
        public void Add(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return;
            var parts = new List<string>();
            foreach (var line in text.Split('\n'))
                if (!string.IsNullOrWhiteSpace(line)) parts.Add(line.Trim());
            if (parts.Count == 0) return;
            _lines.InsertRange(0, parts);
            if (_lines.Count > Capacity) _lines.RemoveRange(Capacity, _lines.Count - Capacity);
            Added?.Invoke(parts.Count);
        }

        public void Clear() => _lines.Clear();
    }

    /// <summary>
    /// The event pipeline: game hooks raise events as they happen; once a frame they are flushed in the
    /// order raised, each spoken (queued, never interrupting) and logged as its kind's settings say.
    /// Speech is held back while the game replays (an undo, a continued run), the log still kept.
    /// </summary>
    public static class GameEvents
    {
        private struct Pending
        {
            public EventKind Kind;
            public string Text;
            public EventSide Side;
            public string Item;
        }

        private static readonly List<Pending> _pending = new List<Pending>();

        public static EventLog Events { get; } = new EventLog();
        public static EventLog Quotes { get; } = new EventLog();

        /// <summary>The settings the kinds read (the host's store); null reads the defaults.</summary>
        public static Func<ISettingsStore> Store = () => null;

        /// <summary>Whether the game is replaying (speech held back).</summary>
        public static Func<bool> Suppressed = () => false;

        /// <summary>Whether an event of a kind is dropped altogether (a battle setting itself up says
        /// nothing but its turn and phase).</summary>
        public static Func<EventKind, bool> Muted = _ => false;

        /// <summary>An event to speak and log at the next flush. <paramref name="item"/> is what it is
        /// about, for a kind whose items are set one by one (an artifact's asset name), or null.</summary>
        public static void Raise(EventKind kind, string text, EventSide side = EventSide.None, string item = null)
        {
            if (kind == null || string.IsNullOrWhiteSpace(text) || Muted(kind)) return;
            _pending.Add(new Pending { Kind = kind, Text = text, Side = side, Item = item });
        }

        /// <summary>Speak and log everything raised since the last flush, in order.</summary>
        public static void Flush()
        {
            if (_pending.Count == 0) return;
            var batch = _pending.ToArray();
            _pending.Clear();
            var store = Store();
            bool suppressed = Suppressed();
            foreach (var e in batch)
            {
                if (!e.Kind.Passes(store, e.Side)) continue;
                if (e.Kind.Buffers(store) && e.Kind.ItemBuffers(store, e.Item)) (e.Kind.Log == EventLogKind.Quotes ? Quotes : Events).Add(e.Text);
                if (!suppressed && e.Kind.Announces(store) && e.Kind.ItemAnnounces(store, e.Item)) Speech.Say(e.Text);
            }
        }

        /// <summary>
        /// An event its screen speaks itself, at the moment it chooses (a dialog's message ahead of its
        /// buttons): logged now as its kind's settings say; true when the kind is to be spoken. Never muted
        /// or held back for a replay: it is what the screen shows now, not a replayed event.
        /// </summary>
        public static bool Report(EventKind kind, string text, EventSide side = EventSide.None)
        {
            if (kind == null || string.IsNullOrWhiteSpace(text)) return false;
            var store = Store();
            if (!kind.Passes(store, side)) return false;
            if (kind.Buffers(store)) (kind.Log == EventLogKind.Quotes ? Quotes : Events).Add(text);
            return kind.Announces(store);
        }

        /// <summary>Drop what was raised and not yet flushed (a teardown).</summary>
        public static void Discard() => _pending.Clear();
    }
}
