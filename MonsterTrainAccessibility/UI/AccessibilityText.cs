using MonsterTrainAccessibility.Localization;
using TMPro;

namespace MonsterTrainAccessibility.UI
{
    internal static class AccessibilityText
    {
        public static string ReadText(TMP_Text text)
        {
            return Message.Clean(text != null ? text.text : string.Empty);
        }

        public static string ReadLocalizedText(TMP_Text text)
        {
            if (text == null)
            {
                return string.Empty;
            }

            I2.Loc.Localize localize = text.GetComponent<I2.Loc.Localize>();
            if (localize != null)
            {
                string term = !string.IsNullOrWhiteSpace(localize.FinalTerm) ? localize.FinalTerm : localize.Term;
                if (!string.IsNullOrWhiteSpace(term))
                {
                    string translated = AccessibilityLocalizationScope.Run(() =>
                        I2.Loc.LocalizationManager.GetTranslation(term));
                    if (!string.IsNullOrWhiteSpace(translated))
                    {
                        return Message.Clean(translated);
                    }
                }
            }

            return ReadText(text);
        }

        public static string LocalizeTerm(string term)
        {
            if (string.IsNullOrWhiteSpace(term))
            {
                return string.Empty;
            }

            return Message.Clean(AccessibilityLocalizationScope.Run(() => term.Localize()));
        }

        public static string LocalizeTerm(string term, global::ILocalizationParameterContext context)
        {
            if (string.IsNullOrWhiteSpace(term))
            {
                return string.Empty;
            }

            return Message.Clean(AccessibilityLocalizationScope.Run(() => term.Localize(context)));
        }

        public static string ReadTextFromField<T>(object owner, string fieldName, string ownerName) where T : TMP_Text
        {
            if (owner == null)
            {
                return string.Empty;
            }

            T text = Core.ReflectionUtil.GetFieldValue<T>(owner, fieldName, ownerName);
            return ReadLocalizedText(text);
        }

    }
}
