using System.Collections.Generic;
using MonsterTrainAccessibility.Input;
using MonsterTrainAccessibility.Localization;
using MonsterTrainAccessibility.UI.Screens;

namespace MonsterTrainAccessibility.Help
{
    internal sealed class HelpScreenBuilder
    {
        private readonly List<HelpActionEntry> _entries = new List<HelpActionEntry>();
        private readonly HashSet<string> _seenKeys = new HashSet<string>(System.StringComparer.Ordinal);

        public IReadOnlyList<HelpActionEntry> Build()
        {
            AddModOwnedBindings();
            _entries.Sort(CompareEntries);
            return _entries;
        }

        private void AddModOwnedBindings()
        {
            IReadOnlyList<InputAction> actions = global::MonsterTrainAccessibility.Input.InputManager.Actions;
            for (int i = 0; i < actions.Count; i++)
            {
                TryAddModOwned(actions[i]);
            }
        }

        private void TryAddModOwned(InputAction action)
        {
            if (action == null ||
                IsKnownGameActionKey(action.Key) ||
                IsHiddenModHelpAction(action.Key) ||
                !_seenKeys.Add(action.Key))
            {
                return;
            }

            Screen target = FindTarget(action);
            if (target == null)
            {
                return;
            }

            Message bindings = HelpText.FormatModBindings(action);
            if (bindings == null)
            {
                return;
            }

            _entries.Add(new HelpActionEntry(Message.Raw(action.Label), bindings));
        }

        private static bool IsKnownGameActionKey(string actionKey)
        {
            switch (actionKey)
            {
                case "ui_up":
                case "ui_down":
                case "ui_left":
                case "ui_right":
                case "ui_home":
                case "ui_end":
                case "ui_accept":
                case "ui_select":
                case "ui_cancel":
                    return true;
                default:
                    return false;
            }
        }

        private static bool IsHiddenModHelpAction(string actionKey)
        {
            return string.Equals(actionKey, "debug_commands", System.StringComparison.Ordinal);
        }

        private static int CompareEntries(HelpActionEntry left, HelpActionEntry right)
        {
            string leftLabel = left?.Label?.Resolve() ?? string.Empty;
            string rightLabel = right?.Label?.Resolve() ?? string.Empty;
            int labelCompare = string.Compare(leftLabel, rightLabel, System.StringComparison.CurrentCultureIgnoreCase);
            if (labelCompare != 0)
            {
                return labelCompare;
            }

            string leftBindings = left?.Bindings?.Resolve() ?? string.Empty;
            string rightBindings = right?.Bindings?.Resolve() ?? string.Empty;
            return string.Compare(leftBindings, rightBindings, System.StringComparison.CurrentCultureIgnoreCase);
        }

        private static Screen FindTarget(InputAction action)
        {
            foreach (Screen screen in global::MonsterTrainAccessibility.UI.Screens.ScreenManager.WalkScreensDeepestFirst())
            {
                if (!screen.HasClaimed(action.Key))
                {
                    continue;
                }

                return screen.IsActionAvailable(action) ? screen : null;
            }

            return null;
        }
    }
}
