using MonsterTrainAccessibility.Core;
using MonsterTrainAccessibility.Localization;
using MonsterTrainAccessibility.Speech;
using ShinyShoe;
using ModScreenManager = MonsterTrainAccessibility.UI.Screens.ScreenManager;

namespace MonsterTrainAccessibility.Patches
{
    internal static class ForgeToggleHooks
    {
        public static void Hud_ApplyScreenInput_Prefix(ref bool __state)
        {
            __state = GameManagers.GetSaveManager()?.IsForgeToggleActive() == true;
        }

        public static void Hud_ApplyScreenInput_Postfix(
            global::Hud __instance,
            bool __result,
            bool __state,
            CoreInputControlMapping mapping,
            IGameUIComponent triggeredUI,
            global::InputManager.Controls triggeredMappingID)
        {
            if (triggeredMappingID == global::InputManager.Controls.Minimap &&
                __instance != null &&
                __instance.IsHudNavigationEnabled())
            {
                ModScreenManager.FocusFirstHudSoul(__instance);
            }

            if (!__result)
            {
                return;
            }

            SaveManager saveManager = GameManagers.GetSaveManager();
            if (saveManager == null)
            {
                return;
            }

            bool nextState = saveManager.IsForgeToggleActive();
            if (nextState == __state)
            {
                return;
            }

            SpeechManager.Output(Message.Localized("messages", nextState ? "state.on" : "state.off"));
        }
    }
}
