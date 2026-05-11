using System;
using System.Collections.Generic;
using MonsterTrainAccessibility.Buffers;
using MonsterTrainAccessibility.Core;
using MonsterTrainAccessibility.Localization;
using MonsterTrainAccessibility.Speech;
using UnityEngine;

namespace MonsterTrainAccessibility.Events
{
    internal static class EventDispatcher
    {
        private static readonly List<PendingEvent> Pending = new List<PendingEvent>();

        public static void Enqueue(GameEvent evt)
        {
            if (evt == null)
            {
                return;
            }

            Pending.Add(new PendingEvent(evt, Time.frameCount));
        }

        public static void Flush()
        {
            if (Pending.Count == 0)
            {
                return;
            }

            bool suppressSpeech = ReplayAccessibilityState.IsSuppressed;
            for (int i = 0; i < Pending.Count; i++)
            {
                GameEvent evt = Pending[i].Event;
                bool shouldAnnounce = EventRegistry.ShouldAnnounce(evt);
                bool shouldBuffer = EventRegistry.ShouldBuffer(evt);
                if (!shouldAnnounce && !shouldBuffer)
                {
                    continue;
                }

                Message message;
                try
                {
                    message = evt.GetMessage();
                }
                catch (Exception ex)
                {
                    Log.Info("[AccessibilityMod] Combat event message failed: " + ex);
                    continue;
                }

                if (message == null)
                {
                    continue;
                }

                string text = message.Resolve();
                if (string.IsNullOrWhiteSpace(text))
                {
                    continue;
                }

                if (shouldBuffer)
                {
                    LineBuffer events = BufferManager.Instance.GetBuffer("events");
                    if (events != null)
                    {
                        events.Add(message);
                        BufferManager.Instance.EnableBuffer("events", true);
                    }
                }

                if (shouldAnnounce && !suppressSpeech)
                {
                    SpeechManager.Output(text, interrupt: false);
                }
            }

            Pending.Clear();
        }

        public static void Clear()
        {
            Pending.Clear();
        }

        private readonly struct PendingEvent
        {
            public readonly GameEvent Event;
            public readonly int Frame;

            public PendingEvent(GameEvent evt, int frame)
            {
                Event = evt;
                Frame = frame;
            }
        }
    }
}
