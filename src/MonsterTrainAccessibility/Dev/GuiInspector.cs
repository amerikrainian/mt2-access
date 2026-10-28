using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MonsterTrainAccessibility.Dev
{
    /// <summary>
    /// Raw structural dump of the active uGUI hierarchy for the dev /gui endpoint: every active
    /// GameObject under each root canvas, indented by depth, with its component types, any TMP/legacy
    /// text, and CanvasGroup alpha. Surfaces structure the focus views hide, so an unfamiliar screen can
    /// be reverse-engineered in one shot and then driven with /eval. Reads live state; caches nothing.
    /// Lists GameObject-active objects only; an object can be active yet hidden via CanvasGroup alpha 0.
    /// </summary>
    internal static class GuiInspector
    {
        public static string Describe()
        {
            var sb = new StringBuilder();
            int roots = 0;
            foreach (Canvas canvas in Object.FindObjectsOfType<Canvas>())
            {
                if (!canvas.isRootCanvas || !canvas.gameObject.activeInHierarchy)
                    continue;
                roots++;
                sb.Append("=== canvas: ").Append(canvas.name)
                  .Append("  sort=").Append(canvas.sortingOrder).Append(" ===\n");
                Walk(canvas.transform, 0, sb);
            }
            if (roots == 0)
                sb.Append("(no active root canvas)\n");
            return sb.ToString();
        }

        private static void Walk(Transform t, int depth, StringBuilder sb)
        {
            GameObject go = t.gameObject;
            if (!go.activeInHierarchy)
                return;

            sb.Append(' ', depth * 2).Append(go.name);
            AppendComponents(go, sb);

            var cg = go.GetComponent<CanvasGroup>();
            if (cg != null)
                sb.Append("  alpha=").Append(cg.alpha);

            string text = NodeText(go);
            if (text != null)
                sb.Append("  \"").Append(text).Append('"');

            sb.Append('\n');

            for (int i = 0; i < t.childCount; i++)
                Walk(t.GetChild(i), depth + 1, sb);
        }

        private static void AppendComponents(GameObject go, StringBuilder sb)
        {
            bool any = false;
            foreach (Component c in go.GetComponents<Component>())
            {
                if (c == null)
                    continue; // a missing-script slot
                string name = c.GetType().Name;
                if (name == "Transform" || name == "RectTransform" || name == "CanvasRenderer")
                    continue;
                sb.Append(any ? ", " : "  [").Append(name);
                any = true;
            }
            if (any)
                sb.Append(']');
        }

        private static string NodeText(GameObject go)
        {
            var tmp = go.GetComponent<TMP_Text>();
            if (tmp != null && !string.IsNullOrEmpty(tmp.text))
                return tmp.text;
            var txt = go.GetComponent<Text>();
            if (txt != null && !string.IsNullOrEmpty(txt.text))
                return txt.text;
            return null;
        }
    }
}
