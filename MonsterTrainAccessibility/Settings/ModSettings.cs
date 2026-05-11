using MonsterTrainAccessibility.Localization;

namespace MonsterTrainAccessibility.ModSettings
{
    internal static class ModSettings
    {
        public static CategorySetting Root { get; } =
            new CategorySetting("root", Message.Localized("ui", "MOD_SETTINGS.TITLE"));

        public static void Register(Setting setting)
        {
            Root.Add(setting);
        }
    }
}
