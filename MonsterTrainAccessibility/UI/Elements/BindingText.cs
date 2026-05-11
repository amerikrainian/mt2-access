using System.Collections.Generic;
using MonsterTrainAccessibility.Input;

namespace MonsterTrainAccessibility.UI.Elements
{
    internal static class BindingText
    {
        public static string Summarize(IReadOnlyList<InputBinding> bindings)
        {
            if (bindings == null || bindings.Count == 0)
            {
                return string.Empty;
            }

            List<InputBinding> ordered = InputBindingOrder.Ordered(bindings);
            List<string> names = new List<string>();
            for (int i = 0; i < ordered.Count; i++)
            {
                if (ordered[i] != null)
                {
                    names.Add(ordered[i].DisplayName);
                }
            }

            return string.Join(", ", names.ToArray());
        }
    }
}
