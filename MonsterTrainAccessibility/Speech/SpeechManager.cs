using System;
using System.Collections.Generic;
using MonsterTrainAccessibility.Core;
using MonsterTrainAccessibility.Localization;

namespace MonsterTrainAccessibility.Speech
{
    public class SpeechManager
    {
        public static SpeechManager Instance { get; private set; }

        private readonly List<ISpeechHandler> _handlers;
        private ISpeechHandler _active;
        private string _lastSpoken;

        public bool IsAvailable => _active != null;

        public string LastSpoken => _lastSpoken;

        internal IReadOnlyList<ISpeechHandler> Handlers => _handlers;

        internal ISpeechHandler ActiveHandler => _active;

        private SpeechManager(IEnumerable<ISpeechHandler> handlers)
        {
            _handlers = new List<ISpeechHandler>(handlers ?? Array.Empty<ISpeechHandler>());
        }

        public static SpeechManager Initialize()
        {
            return InitializeInternal(CreateDefaultHandlers());
        }

        internal static SpeechManager InitializeInternal(IEnumerable<ISpeechHandler> handlers)
        {
            if (Instance != null)
            {
                return Instance;
            }

            Instance = new SpeechManager(handlers);
            Instance.LoadActive();
            return Instance;
        }

        public static void Speak(string text, bool interrupt = true)
        {
            Instance?.SpeakInternal(text, interrupt);
        }

        public static void Speak(Message message, bool interrupt = true)
        {
            Instance?.SpeakInternal(message, interrupt);
        }

        public static void Output(string text, bool interrupt = false)
        {
            Instance?.OutputInternal(text, interrupt);
        }

        public static void Output(Message message, bool interrupt = false)
        {
            Instance?.OutputInternal(message, interrupt);
        }

        public static void Stop()
        {
            Instance?.StopInternal();
        }

        public static void RepeatLast()
        {
            Instance?.RepeatLastInternal();
        }

        public static void Shutdown()
        {
            Instance?.ShutdownInternal();
        }

        private void LoadActive()
        {
            for (int i = 0; i < _handlers.Count; i++)
            {
                ISpeechHandler handler = _handlers[i];
                if (TryLoadHandler(handler))
                {
                    _active = handler;
                    Log.Info("Speech handler active: " + handler.Label);
                    return;
                }
            }

            Log.Warn("SpeechManager: no handler loaded; speech will be silent");
        }

        private void SpeakInternal(string text, bool interrupt)
        {
            string filtered = Prepare(text);
            if (filtered == null)
            {
                return;
            }

            _lastSpoken = filtered;
            Dispatch((handler, payload) => handler.Speak(payload, interrupt), filtered);
        }

        private void SpeakInternal(Message message, bool interrupt)
        {
            if (message == null)
            {
                return;
            }

            SpeakInternal(message.Resolve(), interrupt);
        }

        private void OutputInternal(string text, bool interrupt)
        {
            string filtered = Prepare(text);
            if (filtered == null)
            {
                return;
            }

            _lastSpoken = filtered;
            Dispatch((handler, payload) => handler.Output(payload, interrupt), filtered);
        }

        private void OutputInternal(Message message, bool interrupt)
        {
            if (message == null)
            {
                return;
            }

            OutputInternal(message.Resolve(), interrupt);
        }

        private void StopInternal()
        {
            for (int i = 0; i < _handlers.Count; i++)
            {
                try
                {
                    _handlers[i].Silence();
                }
                catch (Exception ex)
                {
                    Log.Warn(_handlers[i].Label + " silence failed: " + ex);
                }
            }
        }

        private void RepeatLastInternal()
        {
            if (string.IsNullOrEmpty(_lastSpoken) || _active == null)
            {
                return;
            }

            try
            {
                _active.Speak(_lastSpoken, true);
            }
            catch (Exception ex)
            {
                Log.Warn(_active.Label + " repeat failed: " + ex);
            }
        }

        private void ShutdownInternal()
        {
            for (int i = 0; i < _handlers.Count; i++)
            {
                try
                {
                    _handlers[i].Unload();
                }
                catch (Exception ex)
                {
                    Log.Warn(_handlers[i].Label + " unload failed: " + ex);
                }
            }

            Instance = null;
        }

        private static string Prepare(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return null;
            }

            string filtered = TextFilter.Apply(text);
            return string.IsNullOrEmpty(filtered) ? null : filtered;
        }

        private void Dispatch(Func<ISpeechHandler, string, bool> action, string payload)
        {
            if (_active == null)
            {
                return;
            }

            try
            {
                action(_active, payload);
            }
            catch (Exception ex)
            {
                Log.Warn(_active.Label + " speech call failed: " + ex);
            }
        }

        private static bool TryLoadHandler(ISpeechHandler handler)
        {
            try
            {
                return handler.TryLoad() && handler.IsLoaded;
            }
            catch (Exception ex)
            {
                Log.Warn(handler.Label + " load failed: " + ex);
                return false;
            }
        }

        private static IEnumerable<ISpeechHandler> CreateDefaultHandlers()
        {
            return new ISpeechHandler[]
            {
                // Prism is the default speech path. Clipboard remains last-resort fallback only.
                new PrismHandler(),
                new ClipboardHandler()
            };
        }
    }
}
