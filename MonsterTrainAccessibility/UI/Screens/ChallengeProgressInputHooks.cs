using MonsterTrainAccessibility.Core;
using ShinyShoe;

namespace MonsterTrainAccessibility.UI.Screens
{
    internal static class ChallengeProgressInputHooks
    {
        public static bool ChallengeProgressScreen_ApplyScreenInput_Prefix(
            CoreInputControlMapping mapping,
            IGameUIComponent triggeredUI,
            global::InputManager.Controls triggeredMappingID,
            ref bool __result)
        {
            if (mapping?.fake == true || triggeredMappingID.IsCloseInput() || !ShouldBlockChallengeProgressInput())
            {
                return true;
            }

            __result = false;
            return false;
        }

        private static bool ShouldBlockChallengeProgressInput()
        {
            global::InputManager inputManager = global::InputManager.Inst;
            if (inputManager == null || !inputManager.currentInputModeUsesNavigation)
            {
                return false;
            }

            if (IsChallengeProgressScreenActive())
            {
                return true;
            }

            global::ScreenManager screenManager = GameManagers.GetScreenManager();
            return screenManager != null && screenManager.GetScreenActive(global::ScreenName.ChallengeProgress);
        }

        private static bool IsChallengeProgressScreenActive()
        {
            foreach (Screen screen in ScreenManager.WalkScreensDeepestFirst())
            {
                if (screen is ChallengeProgressScreen)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
