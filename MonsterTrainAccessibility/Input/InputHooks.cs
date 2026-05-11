using MonsterTrainAccessibility.Core;

namespace MonsterTrainAccessibility.Input
{
    internal static class InputHooks
    {
        public static bool CoreInputDriverKeyboard_OnLateUpdate_Prefix()
        {
            UIManager.Tick();
            return !InputManager.Poll();
        }

        public static void CoreInputDriverGamepad_OnLateUpdate_Postfix(global::ShinyShoe.CoreInputDriverGamepad __instance)
        {
            InputManager.PollController(__instance);
        }
    }
}
