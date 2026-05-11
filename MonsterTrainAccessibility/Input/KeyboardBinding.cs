using System;
using UnityEngine;

namespace MonsterTrainAccessibility.Input
{
    public class KeyboardBinding : InputBinding
    {
        public override string Type => "keyboard";
        public KeyCode Keycode { get; }
        public bool Ctrl { get; }
        public bool Shift { get; }
        public bool Alt { get; }

        public KeyboardBinding(KeyCode keycode, bool ctrl = false, bool shift = false, bool alt = false)
        {
            Keycode = keycode;
            Ctrl = ctrl;
            Shift = shift;
            Alt = alt;
        }

        public bool Matches(KeyCode keycode, bool ctrl, bool shift, bool alt)
        {
            return keycode == Keycode
                && ctrl == Ctrl
                && shift == Shift
                && alt == Alt;
        }

        public bool Matches(KeyCode keycode)
        {
            bool ctrl = UnityEngine.Input.GetKey(KeyCode.LeftControl) || UnityEngine.Input.GetKey(KeyCode.RightControl);
            bool shift = UnityEngine.Input.GetKey(KeyCode.LeftShift) || UnityEngine.Input.GetKey(KeyCode.RightShift);
            bool alt = UnityEngine.Input.GetKey(KeyCode.LeftAlt) || UnityEngine.Input.GetKey(KeyCode.RightAlt);
            return Matches(keycode, ctrl, shift, alt);
        }

        public override string TypeLabel => "Keyboard";
        public override string ComboName
        {
            get
            {
                string parts = "";
                if (Ctrl) parts += "Ctrl+";
                if (Shift) parts += "Shift+";
                if (Alt) parts += "Alt+";
                return parts + GetDisplayName(Keycode);
            }
        }

        public override string Serialize()
        {
            string parts = "";
            if (Ctrl) parts += "Ctrl+";
            if (Shift) parts += "Shift+";
            if (Alt) parts += "Alt+";
            return parts + Keycode.ToString();
        }

        private static string GetDisplayName(KeyCode keycode)
        {
            switch (keycode)
            {
                case KeyCode.UpArrow:
                    return "Up";
                case KeyCode.DownArrow:
                    return "Down";
                case KeyCode.LeftArrow:
                    return "Left";
                case KeyCode.RightArrow:
                    return "Right";
                case KeyCode.Return:
                    return "Enter";
                default:
                    return keycode.ToString();
            }
        }

        public static KeyboardBinding Parse(string s)
        {
            bool ctrl = false, shift = false, alt = false;
            string remaining = s;

            while (remaining.Contains("+"))
            {
                int idx = remaining.IndexOf('+');
                string mod = remaining.Substring(0, idx);
                remaining = remaining.Substring(idx + 1);

                switch (mod)
                {
                    case "Ctrl": ctrl = true; break;
                    case "Shift": shift = true; break;
                    case "Alt": alt = true; break;
                }
            }

            KeyCode key;
            if (Enum.TryParse(remaining, out key))
            {
                return new KeyboardBinding(key, ctrl, shift, alt);
            }

            return null;
        }
    }
}
