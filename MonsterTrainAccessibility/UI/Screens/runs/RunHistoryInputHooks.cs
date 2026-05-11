using MonsterTrainAccessibility.Core;
using ShinyShoe;

namespace MonsterTrainAccessibility.UI.Screens
{
    internal static class RunHistoryInputHooks
    {
        public static bool RunHistoryUI_ApplyScreenInput_Prefix(
            CoreInputControlMapping mapping,
            IGameUIComponent triggeredUI,
            global::InputManager.Controls triggeredMappingID,
            ref bool __result)
        {
            if (!ShouldBlockRunHistoryInput())
            {
                return true;
            }

            __result = false;
            return false;
        }

        public static bool RunHistoryScreen_ApplyScreenInput_Prefix(
            CoreInputControlMapping mapping,
            IGameUIComponent triggeredUI,
            global::InputManager.Controls triggeredMappingID,
            ref bool __result)
        {
            if (triggeredMappingID.IsCloseInput())
            {
                return true;
            }

            if (!ShouldBlockRunHistoryInput())
            {
                return true;
            }

            __result = false;
            return false;
        }

        private static bool ShouldBlockRunHistoryInput()
        {
            global::InputManager inputManager = global::InputManager.Inst;
            if (inputManager == null || !inputManager.currentInputModeUsesNavigation)
            {
                return false;
            }

            if (IsRunHistoryScreenActive())
            {
                return true;
            }

            global::ScreenManager screenManager = GameManagers.GetScreenManager();
            return screenManager != null && screenManager.GetScreenActive(global::ScreenName.RunHistory);
        }

        private static bool IsRunHistoryScreenActive()
        {
            foreach (Screen screen in ScreenManager.WalkScreensDeepestFirst())
            {
                if (screen is RunHistoryScreen)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
