using System;
using System.Collections.Generic;

namespace MonsterTrainAccessibility.Input
{
    internal static class InputBindingOrder
    {
        public static List<InputBinding> Ordered(IReadOnlyList<InputBinding> bindings)
        {
            List<InputBinding> result = new List<InputBinding>();
            if (bindings == null)
            {
                return result;
            }

            for (int i = 0; i < bindings.Count; i++)
            {
                if (bindings[i] != null)
                {
                    result.Add(bindings[i]);
                }
            }

            result.Sort(Compare);
            return result;
        }

        private static int Compare(InputBinding left, InputBinding right)
        {
            int typeCompare = TypeRank(left).CompareTo(TypeRank(right));
            if (typeCompare != 0)
            {
                return typeCompare;
            }

            return string.Compare(left?.ComboName, right?.ComboName, StringComparison.OrdinalIgnoreCase);
        }

        private static int TypeRank(InputBinding binding)
        {
            switch (binding?.Type)
            {
                case "keyboard":
                    return 0;
                case "controller":
                    return 1;
                default:
                    return 2;
            }
        }
    }
}
