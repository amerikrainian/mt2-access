using MonsterTrainAccessibility.Localization;
using TMPro;
using UnityEngine;

namespace MonsterTrainAccessibility.UI
{
    internal static class AuthoredLabelReader
    {
        public static Message ReadMessage(Component owner)
        {
            return Message.RawCleaned(Read(owner));
        }

        public static string Read(Component owner)
        {
            return Read(owner != null ? owner.transform : null);
        }

        public static string Read(GameObject owner)
        {
            return Read(owner != null ? owner.transform : null);
        }

        public static string Read(Transform owner)
        {
            TMP_Text text = FindBestText(owner);
            return AccessibilityText.ReadLocalizedText(text);
        }

        private static TMP_Text FindBestText(Transform owner)
        {
            if (owner == null)
            {
                return null;
            }

            TMP_Text[] texts = owner.GetComponentsInChildren<TMP_Text>(includeInactive: true);
            TMP_Text best = null;
            int bestScore = int.MinValue;
            bool ownerActive = owner.gameObject.activeInHierarchy;

            for (int i = 0; i < texts.Length; i++)
            {
                TMP_Text text = texts[i];
                if (text == null || IsButtonHintText(text.transform, owner))
                {
                    continue;
                }

                if (!text.enabled || ownerActive && !text.gameObject.activeInHierarchy)
                {
                    continue;
                }

                string value = AccessibilityText.ReadLocalizedText(text);
                if (!IsReadable(value))
                {
                    continue;
                }

                int score = Score(text);
                if (score > bestScore)
                {
                    best = text;
                    bestScore = score;
                }
            }

            return best;
        }

        private static int Score(TMP_Text text)
        {
            int score = 0;

            if (text.gameObject.activeInHierarchy)
            {
                score += 100;
            }

            if (text.enabled)
            {
                score += 50;
            }

            if (HasLocalizationTerm(text))
            {
                score += 30;
            }

            if (text.transform.name.IndexOf("label", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                score += 5;
            }

            if (text.transform.name.IndexOf("locked", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                score -= 50;
            }

            return score;
        }

        private static bool HasLocalizationTerm(TMP_Text text)
        {
            I2.Loc.Localize localize = text != null ? text.GetComponent<I2.Loc.Localize>() : null;
            return localize != null &&
                (!string.IsNullOrWhiteSpace(localize.FinalTerm) || !string.IsNullOrWhiteSpace(localize.Term));
        }

        private static bool IsButtonHintText(Transform text, Transform owner)
        {
            for (Transform current = text; current != null && current != owner; current = current.parent)
            {
                if (current.name.IndexOf("button hint", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                    current.name.IndexOf("key label", System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsReadable(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            for (int i = 0; i < value.Length; i++)
            {
                if (char.IsLetter(value[i]))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
