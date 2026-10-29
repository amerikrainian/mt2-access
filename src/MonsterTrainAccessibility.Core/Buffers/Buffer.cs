using System;
using System.Collections.Generic;

namespace MonsterTrainAccessibility.Core.Buffers
{
    /// <summary>
    /// A named, ordered list of text lines the user reviews on demand (Ctrl+arrows), independent of
    /// the auto-spoken focus announcement. Lines come from a live source delegate re-read on every
    /// buffer keypress, so content never goes stale; the cursor survives a re-read when still in
    /// range. Detail is not nested: one focused control explodes into several flat lines (its own
    /// line, then one line per tooltip), and the user steps line by line.
    /// Ported from Harkest Dungeon (dd2a11y).
    /// </summary>
    public sealed class Buffer
    {
        public string Key { get; }

        private readonly Func<string> _label;
        private readonly List<string> _lines = new List<string>();
        private Func<IEnumerable<string>> _source;

        public int Position { get; private set; }

        /// <summary>When true, switching to this buffer jumps to the last line (an event log).</summary>
        public bool FollowLatest { get; set; }

        public Buffer(string key, Func<string> label)
        {
            Key = key;
            _label = label;
        }

        /// <summary>The buffer's spoken name, localized at speak time.</summary>
        public string Label => _label();

        public bool IsEmpty
        {
            get
            {
                Refresh();
                return _lines.Count == 0;
            }
        }

        /// <summary>Bind the live source this buffer reads (null detaches, emptying it). Resets the
        /// cursor: a new source is new content.</summary>
        public void SetSource(Func<IEnumerable<string>> source)
        {
            _source = source;
            Position = 0;
            Refresh();
        }

        /// <summary>Back to the first line: the source stayed, its subject changed (a new focus).</summary>
        public void Reset()
        {
            Position = 0;
            Refresh();
        }

        /// <summary>Re-read the source, preserving the cursor when still in range.</summary>
        public void Refresh()
        {
            int saved = Position;
            _lines.Clear();
            if (_source != null)
            {
                IEnumerable<string> lines = null;
                try { lines = _source(); }
                catch (Exception e) { CoreLog.Warning("buffer " + Key + ": source failed: " + e.Message); }
                if (lines != null)
                    foreach (var line in lines)
                        if (!string.IsNullOrEmpty(line)) _lines.Add(line);
            }
            Position = saved < _lines.Count ? saved : 0;
        }

        /// <summary>Every line the source reads now (a re-read; the cursor stays).</summary>
        public IReadOnlyList<string> Lines
        {
            get
            {
                Refresh();
                return _lines;
            }
        }

        public string CurrentLine
        {
            get
            {
                Refresh();
                return _lines.Count == 0 ? null : _lines[Position];
            }
        }

        public int Count
        {
            get
            {
                Refresh();
                return _lines.Count;
            }
        }

        public bool MoveNext()
        {
            Refresh();
            if (Position + 1 >= _lines.Count) return false;
            Position++;
            return true;
        }

        public bool MovePrevious()
        {
            Refresh();
            if (Position == 0) return false;
            Position--;
            return true;
        }

        /// <summary>Lines went in on top of the source (a newest-first log): a cursor that has stepped
        /// back keeps to the line it was on; one on the top line stays on top, at the newest.</summary>
        public void LinesInserted(int count)
        {
            if (Position > 0) Position += count;
        }

        public void MoveToEnd()
        {
            Refresh();
            Position = _lines.Count == 0 ? 0 : _lines.Count - 1;
        }
    }
}
