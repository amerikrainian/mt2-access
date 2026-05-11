using System.Collections.Generic;
using MonsterTrainAccessibility.Input;
using MonsterTrainAccessibility.Localization;

namespace MonsterTrainAccessibility.Help
{
    internal static class HelpText
    {
        public static Message FormatModBindings(InputAction action)
        {
            if (action == null)
            {
                return null;
            }

            IReadOnlyList<InputBinding> bindings = action.Bindings;
            if (bindings == null || bindings.Count == 0)
            {
                return Message.Localized("ui", "HELP.UNBOUND");
            }

            List<Message> parts = new List<Message>(bindings.Count);
            for (int i = 0; i < bindings.Count; i++)
            {
                string displayName = bindings[i]?.DisplayName;
                if (!string.IsNullOrWhiteSpace(displayName))
                {
                    parts.Add(Message.Raw(displayName));
                }
            }

            return parts.Count > 0 ? Message.Join(", ", parts) : Message.Localized("ui", "HELP.UNBOUND");
        }
    }
}
