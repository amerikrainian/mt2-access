using ModScreenManager = MonsterTrainAccessibility.UI.Screens.ScreenManager;

namespace MonsterTrainAccessibility.UI.Screens
{
    internal static class BattleAbilityHooks
    {
        public static void AbilityCounterUI_MoveCameraToNearestAbilityUnit_Postfix()
        {
            try
            {
                BattleScreen battleScreen = ModScreenManager.CurrentScreen as BattleScreen;
                if (battleScreen == null)
                {
                    return;
                }

                battleScreen.OnGameAbilityCounterInvoked();
            }
            catch (System.Exception ex)
            {
                Core.Log.Info("[AccessibilityMod] Ability counter game hook failed: " + ex);
            }
        }

        public static bool HandUI_InputSubmit_Prefix(global::HandUI __instance, global::ShinyShoe.CoreInputControlMapping mapping)
        {
            try
            {
                if (mapping == null || !mapping.IsNotHeld())
                {
                    return true;
                }

                BattleScreen battleScreen = ModScreenManager.CurrentScreen as BattleScreen;
                battleScreen?.PrepareTargetingForNativeSubmit(__instance);
                if (battleScreen?.PrepareFocusedCardForNativeSubmit(__instance) == false)
                {
                    return false;
                }
            }
            catch (System.Exception ex)
            {
                Core.Log.Info("[AccessibilityMod] Ability submit prep failed: " + ex);
            }

            return true;
        }
    }
}
