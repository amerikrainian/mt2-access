using System;
using MonsterTrainAccessibility.Core;
using ModChallengeDetailsScreen = MonsterTrainAccessibility.UI.Screens.ChallengeDetailsScreen;

namespace MonsterTrainAccessibility.Patches
{
    internal static class ChallengeDetailsLoadHooks
    {
        public static void ChallengeDetailsScreen_SetLoading_Postfix(global::ChallengeDetailsScreen __instance, bool loading)
        {
            try
            {
                ModChallengeDetailsScreen.HandleNativeLoadingChanged(__instance, loading);
            }
            catch (Exception ex)
            {
                Log.Info("[AccessibilityMod] Challenge details loading hook failed: " + ex);
            }
        }

        public static void ChallengeDetailsScreen_HandleChallengeData_Postfix(global::ChallengeDetailsScreen __instance)
        {
            try
            {
                ModChallengeDetailsScreen.HandleNativeChallengeDataReady(__instance);
            }
            catch (Exception ex)
            {
                Log.Info("[AccessibilityMod] Challenge details ready hook failed: " + ex);
            }
        }
    }
}
