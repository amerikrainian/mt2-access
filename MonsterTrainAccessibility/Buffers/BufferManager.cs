using System.Collections.Generic;
using MonsterTrainAccessibility.Core;
using MonsterTrainAccessibility.Localization;
using MonsterTrainAccessibility.Presentation.Compendium;
using MonsterTrainAccessibility.Presentation;
using MonsterTrainAccessibility.Presentation.Relics;
using MonsterTrainAccessibility.Presentation.Rewards;
using MonsterTrainAccessibility.Speech;

namespace MonsterTrainAccessibility.Buffers
{
    internal sealed class BufferManager
    {
        public static BufferManager Instance { get; } = new BufferManager();

        private readonly List<LineBuffer> _buffers = new List<LineBuffer>();
        private int _position = -1;

        private BufferManager()
        {
        }

        public LineBuffer CurrentBuffer
        {
            get
            {
                if (_position < 0 || _position >= _buffers.Count)
                {
                    return null;
                }

                LineBuffer buffer = _buffers[_position];
                return buffer.Enabled ? buffer : null;
            }
        }

        public void RegisterDefaults()
        {
            _buffers.Clear();
            _position = -1;
            Add(new LineBuffer("ui"));
            Add(new EventHistoryBuffer("events"));
            Add(new EventHistoryBuffer("monster_quotes"));
            Add(new PresentationBuffer<CardState>("card", PhaseRegistry.Cards));
            Add(new PresentationBuffer<CharacterState>("creature", PhaseRegistry.Creatures));
            Add(new PresentationBuffer<CompendiumEnemyPresentationSource>("compendium_enemy", PhaseRegistry.CompendiumEnemies));
            Add(new PresentationBuffer<RelicPresentationSource>("relic", PhaseRegistry.Relics));
            Add(new PresentationBuffer<RewardPresentationSource>("reward", PhaseRegistry.Rewards));
        }

        public void Shutdown()
        {
            _buffers.Clear();
            _position = -1;
        }

        public void Add(LineBuffer buffer)
        {
            if (buffer != null)
            {
                _buffers.Add(buffer);
            }
        }

        public LineBuffer GetBuffer(string key)
        {
            for (int i = 0; i < _buffers.Count; i++)
            {
                if (string.Equals(_buffers[i].Key, key, System.StringComparison.Ordinal))
                {
                    return _buffers[i];
                }
            }

            return null;
        }

        public void EnableBuffer(string key, bool enabled)
        {
            LineBuffer buffer = GetBuffer(key);
            if (buffer == null)
            {
                return;
            }

            buffer.Enabled = enabled;
            if (!enabled && ReferenceEquals(buffer, CurrentBuffer))
            {
                MoveToPrevious();
            }
            else if (enabled && _position == -1)
            {
                _position = _buffers.IndexOf(buffer);
                buffer.Update();
                buffer.ResetPosition();
            }
        }

        public void SetCurrentBuffer(string key)
        {
            for (int i = 0; i < _buffers.Count; i++)
            {
                LineBuffer buffer = _buffers[i];
                if (!string.Equals(buffer.Key, key, System.StringComparison.Ordinal))
                {
                    continue;
                }

                buffer.Update();
                if (buffer.Count > 0)
                {
                    buffer.ResetPosition();
                }
                _position = i;
                return;
            }
        }

        public void ResetToAlwaysEnabled(HashSet<string> alwaysEnabled)
        {
            _position = -1;
            for (int i = 0; i < _buffers.Count; i++)
            {
                LineBuffer buffer = _buffers[i];
                buffer.Enabled = alwaysEnabled != null && alwaysEnabled.Contains(buffer.Key);
            }
        }

        public bool MoveToNext()
        {
            if (_buffers.Count == 0)
            {
                return false;
            }

            int start = _position < 0 ? _buffers.Count - 1 : _position;
            int i = start;
            do
            {
                i++;
                if (i >= _buffers.Count)
                {
                    i = 0;
                }
                if (_buffers[i].Enabled)
                {
                    _position = i;
                    _buffers[i].Update();
                    if (_buffers[i].Count > 0)
                    {
                        _buffers[i].ResetPosition();
                    }
                    return true;
                }
            } while (i != start);

            return false;
        }

        public bool MoveToPrevious()
        {
            if (_buffers.Count == 0)
            {
                return false;
            }

            int start = _position < 0 ? 0 : _position;
            int i = start;
            do
            {
                i--;
                if (i < 0)
                {
                    i = _buffers.Count - 1;
                }
                if (_buffers[i].Enabled)
                {
                    _position = i;
                    _buffers[i].Update();
                    if (_buffers[i].Count > 0)
                    {
                        _buffers[i].ResetPosition();
                    }
                    return true;
                }
            } while (i != start);

            return false;
        }

        public void NextBuffer()
        {
            MoveToNext();
            ReportCurrentBuffer();
        }

        public void PreviousBuffer()
        {
            MoveToPrevious();
            ReportCurrentBuffer();
        }

        public void NextItem()
        {
            LineBuffer buffer = CurrentBuffer;
            buffer?.MoveToNext();
            ReportCurrentItem(buffer);
        }

        public void PreviousItem()
        {
            LineBuffer buffer = CurrentBuffer;
            buffer?.MoveToPrevious();
            ReportCurrentItem(buffer);
        }

        private void ReportCurrentBuffer()
        {
            LineBuffer buffer = CurrentBuffer;
            if (buffer == null)
            {
                SpeechManager.Output(Message.Localized("ui", "BUFFERS.NO_BUFFERS"));
                return;
            }

            if (buffer.IsEmpty)
            {
                SpeechManager.Output(Message.Localized("ui", "BUFFERS.EMPTY", new { buffer = buffer.Label.Resolve() }));
                return;
            }

            Message item = buffer.CurrentItem;
            string text = item?.Resolve() ?? string.Empty;
            Log.Info("[AccessibilityMod] Buffer: " + buffer.Key + " -> \"" + text + "\"");
            SpeechManager.Output(Message.Localized("ui", "BUFFERS.CURRENT", new { buffer = buffer.Label.Resolve(), item = text }));
        }

        private void ReportCurrentItem(LineBuffer buffer)
        {
            if (buffer == null)
            {
                SpeechManager.Output(Message.Localized("ui", "BUFFERS.NO_BUFFER_SELECTED"));
                return;
            }

            if (buffer.IsEmpty)
            {
                SpeechManager.Output(Message.Localized("ui", "BUFFERS.EMPTY", new { buffer = buffer.Label.Resolve() }));
                return;
            }

            Message item = buffer.CurrentItem;
            if (item != null)
            {
                string text = item.Resolve();
                Log.Info("[AccessibilityMod] Buffer item: " + buffer.Key + "[" + buffer.Position + "] -> \"" + text + "\"");
                SpeechManager.Output(item);
            }
        }
    }
}
