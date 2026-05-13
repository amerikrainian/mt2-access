using BepInEx.Configuration;
using MonsterTrainAccessibility.Localization;
using MonsterTrainAccessibility.ModSettings;

namespace MonsterTrainAccessibility.Updates
{
    internal static class UpdateSettings
    {
        private const string Section = "Updates";
        private static bool _registered;

        public static BoolSetting CheckUpdatesOnLaunch { get; private set; }

        public static void Register(ConfigFile config)
        {
            if (_registered || config == null)
            {
                return;
            }

            _registered = true;

            CategorySetting category = new CategorySetting(
                "updates",
                Message.Localized("ui", "UPDATE_SETTINGS.CATEGORY"));

            CheckUpdatesOnLaunch = new BoolSetting(
                config,
                Section,
                "CheckUpdatesOnLaunch",
                Message.Localized("ui", "UPDATE_SETTINGS.CHECK_ON_LAUNCH"),
                true,
                "Checks GitHub for a newer Monster Train 2 Accessibility release when the mod starts.");

            category.Add(CheckUpdatesOnLaunch);
            global::MonsterTrainAccessibility.ModSettings.ModSettings.Register(category);
        }
    }
}
