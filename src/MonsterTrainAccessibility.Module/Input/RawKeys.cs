using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MonsterTrainAccessibility.Module.Input
{
    /// <summary>
    /// The keyboard read key by key from Unity's legacy Input, for a rebind waiting for whichever key the
    /// player presses: every key below the mouse buttons, the modifiers apart.
    /// </summary>
    internal static class RawKeys
    {
        private static readonly KeyCode[] Keys = ((KeyCode[])Enum.GetValues(typeof(KeyCode)))
            .Where(k => k != KeyCode.None && k < KeyCode.Mouse0).Distinct().ToArray();

        private static readonly HashSet<KeyCode> Modifiers = new HashSet<KeyCode>
        {
            KeyCode.LeftShift, KeyCode.RightShift, KeyCode.LeftControl, KeyCode.RightControl,
            KeyCode.LeftAlt, KeyCode.RightAlt, KeyCode.AltGr,
            KeyCode.LeftCommand, KeyCode.RightCommand, KeyCode.LeftWindows, KeyCode.RightWindows,
        };

        public static bool IsModifier(KeyCode key) => Modifiers.Contains(key);

        /// <summary>Whether any key is down.</summary>
        public static bool AnyHeld
        {
            get
            {
                foreach (var key in Keys)
                    if (UnityEngine.Input.GetKey(key)) return true;
                return false;
            }
        }

        /// <summary>The first key that is not a modifier to go down this frame, or null.</summary>
        public static KeyCode? FirstPressed()
        {
            foreach (var key in Keys)
                if (!IsModifier(key) && UnityEngine.Input.GetKeyDown(key)) return key;
            return null;
        }

        public static bool CtrlHeld => UnityEngine.Input.GetKey(KeyCode.LeftControl) || UnityEngine.Input.GetKey(KeyCode.RightControl);
        public static bool ShiftHeld => UnityEngine.Input.GetKey(KeyCode.LeftShift) || UnityEngine.Input.GetKey(KeyCode.RightShift);
        public static bool AltHeld => UnityEngine.Input.GetKey(KeyCode.LeftAlt) || UnityEngine.Input.GetKey(KeyCode.RightAlt);
    }
}
