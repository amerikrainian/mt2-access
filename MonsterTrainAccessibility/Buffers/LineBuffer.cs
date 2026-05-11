using System;
using System.Collections.Generic;
using MonsterTrainAccessibility.Localization;

namespace MonsterTrainAccessibility.Buffers
{
    internal class LineBuffer
    {
        protected readonly List<Message> Contents = new List<Message>();

        public LineBuffer(string key)
        {
            Key = key;
        }

        public string Key { get; }
        public int Position { get; protected set; } = -1;

        private bool _enabled;
        public bool Enabled
        {
            get => _enabled;
            set
            {
                _enabled = value;
                if (!value)
                {
                    ClearBinding();
                }
            }
        }

        public Message Label => Message.Localized("ui", "BUFFERS." + Key.ToUpperInvariant());
        public bool IsEmpty => Contents.Count == 0;
        public int Count => Contents.Count;

        public Message CurrentItem
        {
            get
            {
                if (Contents.Count == 0)
                {
                    return null;
                }
                if (Position < 0)
                {
                    Position = 0;
                }
                if (Position >= Contents.Count)
                {
                    Position = Contents.Count - 1;
                }
                return Contents[Position];
            }
        }

        public void Add(Message item)
        {
            if (item == null)
            {
                return;
            }

            string resolved = item.Resolve();
            if (resolved.IndexOf('\n') < 0)
            {
                AddSingle(item);
                return;
            }

            string[] lines = resolved.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                Message line = Message.RawCleaned(lines[i]);
                if (line != null)
                {
                    AddSingle(line);
                }
            }
        }

        public void AddRaw(string item)
        {
            Add(Message.RawCleaned(item));
        }

        public void Clear()
        {
            Contents.Clear();
            Position = -1;
        }

        protected virtual void AddSingle(Message item)
        {
            Contents.Add(item);
        }

        public bool MoveToNext()
        {
            Update();
            return MoveBackward();
        }

        public bool MoveToPrevious()
        {
            Update();
            return MoveForward(startPosition: Count > 1 ? 1 : 0);
        }

        private bool MoveForward(int startPosition = 0)
        {
            if (Position < 0 && Contents.Count > 0)
            {
                Position = startPosition;
                return true;
            }
            if (Position + 1 >= Contents.Count)
            {
                return false;
            }
            Position++;
            return true;
        }

        private bool MoveBackward()
        {
            if (Position < 0 && Contents.Count > 0)
            {
                Position = 0;
                return true;
            }
            if (Position - 1 < 0)
            {
                return false;
            }
            Position--;
            return true;
        }

        public bool MoveToPosition(int position)
        {
            if (position < 0 || position >= Contents.Count)
            {
                return false;
            }
            Position = position;
            return true;
        }

        public void ResetPosition()
        {
            Position = -1;
        }

        public virtual void Update()
        {
        }

        protected virtual void ClearBinding()
        {
        }

        protected void Repopulate(Action populate)
        {
            int savedPosition = Position;
            Clear();
            populate?.Invoke();
            if (savedPosition >= 0 && savedPosition < Count)
            {
                MoveToPosition(savedPosition);
            }
        }

        protected void AddMessages(IEnumerable<Message> messages)
        {
            if (messages == null)
            {
                return;
            }

            foreach (Message message in messages)
            {
                Add(message);
            }
        }
    }
}
