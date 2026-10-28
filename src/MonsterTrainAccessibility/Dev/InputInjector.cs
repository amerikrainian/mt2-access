using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MonsterTrainAccessibility.Dev
{
    /// <summary>
    /// Injects logical UI navigation by driving uGUI's own selection, NOT OS synthetic keys: we drive
    /// the game while its window is unfocused, where synthetic keys do not arrive. Directional moves walk
    /// the selected Selectable's navigation links; confirm invokes the selected Button's click. Used only
    /// when our own module is not loaded (a fallback for poking an unmigrated screen).
    /// </summary>
    internal static class InputInjector
    {
        public static string Inject(string verb)
        {
            string v = (verb ?? "").Trim().ToLowerInvariant();
            EventSystem es = EventSystem.current;
            if (es == null)
                return "[no EventSystem] not on a navigable screen\n";

            switch (v)
            {
                case "up": return Move(es, "up");
                case "down": return Move(es, "down");
                case "left": return Move(es, "left");
                case "right": return Move(es, "right");
                case "confirm": case "enter": case "ok": return Confirm(es);
                default: return "[unknown verb] '" + verb + "' - up|down|left|right|confirm\n";
            }
        }

        private static string Move(EventSystem es, string dir)
        {
            GameObject go = es.currentSelectedGameObject;
            Selectable current = go != null ? go.GetComponent<Selectable>() : null;
            if (current == null)
                return dir + ": nothing selected\n";

            Selectable target;
            switch (dir)
            {
                case "up": target = current.FindSelectableOnUp(); break;
                case "down": target = current.FindSelectableOnDown(); break;
                case "left": target = current.FindSelectableOnLeft(); break;
                default: target = current.FindSelectableOnRight(); break;
            }

            if (target == null)
                return dir + ": no selectable that way\n";

            es.SetSelectedGameObject(target.gameObject);
            return dir + " -> " + target.name + "\n";
        }

        private static string Confirm(EventSystem es)
        {
            GameObject go = es.currentSelectedGameObject;
            if (go == null)
                return "confirm: nothing selected\n";
            Button button = go.GetComponent<Button>();
            if (button == null)
                return "confirm: " + go.name + " is not a Button\n";
            button.onClick.Invoke();
            return "confirm -> " + go.name + "\n";
        }
    }
}
