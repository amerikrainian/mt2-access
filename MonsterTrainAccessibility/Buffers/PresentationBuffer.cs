using System.Collections.Generic;
using MonsterTrainAccessibility.Localization;
using MonsterTrainAccessibility.Presentation;

namespace MonsterTrainAccessibility.Buffers
{
    internal sealed class PresentationBuffer<TSource> : LineBuffer
    {
        private readonly PresentationPipeline<TSource> _pipeline;
        private readonly List<TSource> _sources = new List<TSource>();
        private List<Message> _prefixLines;
        private List<Message> _suffixLines;

        public PresentationBuffer(string key, PresentationPipeline<TSource> pipeline)
            : base(key)
        {
            _pipeline = pipeline;
        }

        public void Bind(TSource source, IEnumerable<Message> prefixLines = null, IEnumerable<Message> suffixLines = null)
        {
            _sources.Clear();
            if (source != null)
            {
                _sources.Add(source);
            }

            _prefixLines = Copy(prefixLines);
            _suffixLines = Copy(suffixLines);
        }

        public void BindMany(IReadOnlyList<TSource> sources, IEnumerable<Message> prefixLines = null, IEnumerable<Message> suffixLines = null)
        {
            _sources.Clear();
            if (sources != null)
            {
                for (int i = 0; i < sources.Count; i++)
                {
                    if (sources[i] != null)
                    {
                        _sources.Add(sources[i]);
                    }
                }
            }

            _prefixLines = Copy(prefixLines);
            _suffixLines = Copy(suffixLines);
        }

        public override void Update()
        {
            if (_sources.Count == 0)
            {
                return;
            }

            Repopulate(Populate);
        }

        protected override void ClearBinding()
        {
            _sources.Clear();
            _prefixLines = null;
            _suffixLines = null;
            Clear();
        }

        private void Populate()
        {
            for (int i = 0; i < _sources.Count; i++)
            {
                IReadOnlyList<Message> lines = PresentationRenderer.BufferLines(_pipeline.Build(_sources[i]));
                if (i == 0)
                {
                    AddMessagesWithContextAfterFirst(lines, _prefixLines);
                }
                else
                {
                    AddMessages(lines);
                }
            }
            AddMessages(_suffixLines);
        }

        private void AddMessagesWithContextAfterFirst(IReadOnlyList<Message> lines, IEnumerable<Message> contextLines)
        {
            if (lines == null || lines.Count == 0)
            {
                AddMessages(contextLines);
                return;
            }

            Add(lines[0]);
            AddMessages(contextLines);
            for (int i = 1; i < lines.Count; i++)
            {
                Add(lines[i]);
            }
        }

        private static List<Message> Copy(IEnumerable<Message> messages)
        {
            if (messages == null)
            {
                return null;
            }

            List<Message> copy = new List<Message>();
            foreach (Message message in messages)
            {
                if (message != null)
                {
                    copy.Add(message);
                }
            }

            return copy;
        }
    }
}
