using System;
using MonsterTrainAccessibility.Core;
using UnityEngine;

namespace MonsterTrainAccessibility.Speech
{
    internal sealed class ClipboardHandler : ISpeechHandler
    {
        public string Key => "clipboard";

        public string Label => "Clipboard";

        public bool IsLoaded { get; private set; }

        public bool TryLoad()
        {
            IsLoaded = true;
            return true;
        }

        public void Unload()
        {
            IsLoaded = false;
        }

        public bool Speak(string text, bool interrupt)
        {
            return Write(text);
        }

        public bool Output(string text, bool interrupt)
        {
            return Write(text);
        }

        public void Silence()
        {
        }

        private static bool Write(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return false;
            }

            try
            {
                GUIUtility.systemCopyBuffer = text;
                return true;
            }
            catch (Exception ex)
            {
                Log.Warn("Clipboard speech failed: " + ex);
                return false;
            }
        }
    }
}
