using MonsterTrainAccessibility.Core;
using ShinyShoe;
using UnityEngine;
using ModCompendiumScreen = MonsterTrainAccessibility.UI.Screens.CompendiumScreen;

namespace MonsterTrainAccessibility.Patches
{
    internal static class CompendiumSearchInputHooks
    {
        public static bool InputManager_SelectGameUIComponent_Prefix(IGameUIComponent gameUI, ref bool __result)
        {
            try
            {
                if (!ModCompendiumScreen.ShouldSuppressSearchEditingSelection(gameUI))
                {
                    return true;
                }

                __result = false;
                return false;
            }
            catch (System.Exception ex)
            {
                Log.Info("[AccessibilityMod] Compendium search selection guard failed: " + ex);
                return true;
            }
        }

        public static void InputFieldContainer_ApplyScreenInput_Prefix(
            global::InputFieldContainer __instance,
            CoreInputControlMapping mapping,
            global::InputManager.Controls triggeredMappingID,
            ref bool __state)
        {
            __state = ModCompendiumScreen.IsCurrentSearchInput(__instance) && IsExitInput(mapping, triggeredMappingID);
        }

        public static void InputFieldContainer_ApplyScreenInput_Postfix(
            global::InputFieldContainer __instance,
            bool __state,
            bool __result)
        {
            if (!__state || !__result)
            {
                return;
            }

            ModCompendiumScreen.FocusSearchTrigger(__instance.button);
        }

        private static bool IsExitInput(CoreInputControlMapping mapping, global::InputManager.Controls triggeredMappingID)
        {
            if (mapping == null)
            {
                return false;
            }

            if (mapping.deviceID == InputDeviceType.Keyboard && mapping.IsNotHeld())
            {
                return mapping.keyCode == KeyCode.Tab ||
                    mapping.keyCode == KeyCode.Return ||
                    mapping.keyCode == KeyCode.Escape;
            }

            return mapping.deviceID == InputDeviceType.Gamepad &&
                (triggeredMappingID == global::InputManager.Controls.Close ||
                 triggeredMappingID == global::InputManager.Controls.Cancel);
        }
    }
}
