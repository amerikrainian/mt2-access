using MonsterTrainAccessibility.Localization;
using MonsterTrainAccessibility.UI;

namespace MonsterTrainAccessibility.Patches
{
    internal static class LocalizationHooks
    {
        public static void LanguageManager_HandleLanguageChanged_Postfix(string languageCodeNew)
        {
            LocalizationManager.ReloadCurrentLanguage(languageCodeNew);
            Message.ResetSpriteLabels();
        }
    }
}
