using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using MonsterTrainAccessibility.Localization;
using MonsterTrainAccessibility.UI;
using TMPro;

namespace MonsterTrainAccessibility.UI.Elements
{
    internal static class InscryptionPathDescriber
    {
        private static readonly FieldInfo InscryptionPathCodeField = AccessTools.Field(typeof(global::CardUI), "_inscryptionPathCode")!;

        public static List<Message> VisibleDeckBufferBottomParts(global::CardUI cardUI, CardState card)
        {
            Message code = VisibleCode(cardUI);
            return code != null ? new List<Message> { code } : null;
        }

        private static Message VisibleCode(global::CardUI cardUI)
        {
            TMP_Text text = cardUI != null ? InscryptionPathCodeField.GetValue(cardUI) as TMP_Text : null;
            if (text == null || !text.gameObject.activeInHierarchy)
            {
                return null;
            }

            return Message.FromText(AccessibilityText.ReadLocalizedText(text));
        }
    }
}
