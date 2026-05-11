using MonsterTrainAccessibility.Core;
using MonsterTrainAccessibility.Localization;
using MonsterTrainAccessibility.Speech;
using ShinyShoe;

namespace MonsterTrainAccessibility.Patches
{
    internal static class ForgeToggleHooks
    {
        public static void Hud_ApplyScreenInput_Prefix(ref bool __state)
        {
            __state = GameManagers.GetSaveManager()?.IsForgeToggleActive() == true;
        }

        public static void Hud_ApplyScreenInput_Postfix(
            bool __result,
            bool __state,
            CoreInputControlMapping mapping,
            IGameUIComponent triggeredUI,
            global::InputManager.Controls triggeredMappingID)
        {
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
