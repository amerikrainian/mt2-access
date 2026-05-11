using System.Collections.Generic;
using MonsterTrainAccessibility.UI.Screens;

namespace MonsterTrainAccessibility.Patches
{
    internal static class CompendiumChecklistHooks
    {
        public static void UnlockScreen_GetFeatureUnlockItems_Postfix(
            List<global::UnlockScreen.UnlockDisplayData> __result)
        {
            CompendiumChecklistChangeStore.CaptureMasteredCards(__result);
        }

        public static void CompendiumScreen_ShowChecklistChanges_Postfix(
            global::CompendiumScreen __instance,
            IReadOnlyList<ChecklistChangeData> changes)
        {
            CompendiumChecklistChangeStore.Set(__instance, changes);
        }
    }
}
