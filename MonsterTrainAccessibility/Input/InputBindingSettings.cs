using BepInEx.Configuration;
using MonsterTrainAccessibility.Localization;
using MonsterTrainAccessibility.ModSettings;

namespace MonsterTrainAccessibility.Input
{
    internal static class InputBindingSettings
    {
        private const string ConfigSection = "Keybindings";
        private static bool _registered;

        public static void Register(ConfigFile config)
        {
            if (_registered || config == null)
            {
                return;
            }

            _registered = true;
            CategorySetting category = new CategorySetting(
                "keybindings",
                Message.Localized("ui", "KEYBINDINGS.CATEGORY"));

            foreach (InputAction action in InputManager.Actions)
            {
                if (!IsModBindableAction(action))
                {
                    continue;
                }

                category.Add(new BindingSetting(config, ConfigSection, action));
            }

            global::MonsterTrainAccessibility.ModSettings.ModSettings.Register(category);
        }

        public static bool IsModBindableAction(InputAction action)
        {
            if (action == null)
            {
                return false;
            }

            if (action.Key == "debug_commands")
            {
                return false;
            }

            return !action.Key.StartsWith("ui_", System.StringComparison.Ordinal);
        }
    }
}
