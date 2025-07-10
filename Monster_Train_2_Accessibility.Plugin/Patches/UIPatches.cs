using HarmonyLib;
using ShinyShoe;
using UnityEngine.EventSystems;

namespace Monster_Train_2_Accessibility.Plugin
{
    [HarmonyPatch(typeof(GameUISelectableButton), "OnSelect")]
    public static class GameUISelectableButton_OnSelect_Patch
    {
        public static void Postfix(GameUISelectableButton __instance, BaseEventData eventData)
        {
            var textComponent = __instance.GetComponentInChildren<TMPro.TextMeshProUGUI>();
            if (textComponent != null)
            {
                string buttonText = textComponent.text;
                ScreenReader.Speak(buttonText);
            }
        }
    }
}
