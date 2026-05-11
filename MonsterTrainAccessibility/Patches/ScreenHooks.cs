using UnityEngine;
using MonsterTrainAccessibility.UI.Screens;
using ModScreenManager = MonsterTrainAccessibility.UI.Screens.ScreenManager;

namespace MonsterTrainAccessibility.Patches
{
    internal static class ScreenHooks
    {
        public static void UIScreen_SetScreenActive_Postfix(global::UIScreen __instance, bool active)
        {
            if (!active && __instance is global::CompendiumScreen compendium)
            {
                CompendiumChecklistChangeStore.Clear(compendium);
            }

            ModScreenManager.HandleUIScreenStateChanged(__instance, active);
        }

        public static void ScreenTransition_SetActive_Postfix(global::ScreenTransition __instance, bool setActive, GameObject owner, System.Action updateCallback, System.Action finishedCallback)
        {
            ModScreenManager.HandleTransitionStateChanged(__instance, owner, setActive);
        }
    }
}
