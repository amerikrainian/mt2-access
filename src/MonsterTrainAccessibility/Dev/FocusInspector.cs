using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MonsterTrainAccessibility.Dev
{
    /// <summary>
    /// Reads the current uGUI focus on demand for the dev /focus endpoint, so the driver can see what the
    /// GAME has selected at any moment, independent of our navigator. Host-side and self-contained, so it
    /// works even when the module failed to load. Reads live state; caches nothing.
    /// </summary>
    internal static class FocusInspector
    {
        public static string Describe()
        {
            var sb = new StringBuilder();

            EventSystem es = EventSystem.current;
            GameObject go = es != null ? es.currentSelectedGameObject : null;

            sb.Append("eventsystem: ")
              .Append(go != null ? go.name : (es == null ? "(no EventSystem)" : "none")).Append('\n');
            if (es != null)
                sb.Append("sendNavigationEvents: ").Append(es.sendNavigationEvents).Append('\n');

            if (go == null)
            {
                sb.Append("text: (nothing selected)\n");
                return sb.ToString();
            }

            Selectable sel = go.GetComponent<Selectable>();
            sb.Append("path: ").Append(HierarchyPath(go)).Append('\n');
            sb.Append("interactable: ").Append(sel != null ? sel.interactable.ToString() : "n/a").Append('\n');
            sb.Append("text: ").Append(LabelText(go)).Append('\n');
            return sb.ToString();
        }

        private static string LabelText(GameObject go)
        {
            var parts = new StringBuilder();
            var labels = go.GetComponentsInChildren<TMP_Text>(true);
            foreach (var label in labels)
            {
                string t = label.text;
                if (string.IsNullOrEmpty(t))
                    continue;
                if (parts.Length > 0)
                    parts.Append(" | ");
                parts.Append(t);
            }
            return parts.Length > 0 ? parts.ToString() : "(no TMP text)";
        }

        internal static string HierarchyPath(GameObject go)
        {
            var names = new List<string>();
            Transform t = go.transform;
            while (t != null)
            {
                names.Add(t.name);
                t = t.parent;
            }
            names.Reverse();
            return string.Join("/", names);
        }
    }
}
