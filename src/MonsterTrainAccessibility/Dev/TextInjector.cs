using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MonsterTrainAccessibility.Dev
{
    /// <summary>
    /// Headless text entry for the dev /type endpoint: a backgrounded window takes no real keys, so we
    /// drive the focused input field directly (legacy InputField or TMP_InputField). Host-side and
    /// self-contained so it works even when the module failed to load.
    /// </summary>
    internal static class TextInjector
    {
        public static string Type(string text)
        {
            EventSystem es = EventSystem.current;
            GameObject go = es != null ? es.currentSelectedGameObject : null;
            if (go == null)
                return "[no field] nothing selected\n";

            var tmp = go.GetComponent<TMP_InputField>();
            if (tmp != null)
            {
                tmp.text += text ?? "";
                tmp.caretPosition = tmp.text.Length;
                return "typed into " + go.name + ": \"" + tmp.text + "\"\n";
            }
            var f = go.GetComponent<InputField>();
            if (f != null)
            {
                f.text += text ?? "";
                f.caretPosition = f.text.Length;
                return "typed into " + go.name + ": \"" + f.text + "\"\n";
            }
            return "[no field] " + go.name + " is not an input field\n";
        }
    }
}
