using System.Reflection;
using HarmonyLib;
using MonsterTrainAccessibility.Input;
using ShinyShoe;
using ModInputManager = MonsterTrainAccessibility.Input.InputManager;
using ModScreenManager = MonsterTrainAccessibility.UI.Screens.ScreenManager;

namespace MonsterTrainAccessibility.Patches
{
    internal static class ScreenInputGateHooks
    {
        private static readonly FieldInfo InputManagerField = AccessTools.Field(typeof(global::ScreenManager), "inputManager")!;

        public static bool ScreenManager_OnGameUISignaled_Prefix(
            global::ScreenManager __instance,
            CoreInputControlMapping mapping,
            IGameUIComponent triggeredUI)
        {
            if (mapping == null || mapping.fake || !mapping.id.IsEnum())
            {
                return true;
            }

            if (KeyMappingInputHooks.ShouldBlockCapturedBindingInputFrame())
            {
                return false;
            }

            if (ModInputManager.ShouldBlockNativeMapping(mapping))
            {
                return false;
            }

            InputAction action = ResolveAction(__instance, mapping, triggeredUI);
            if (action == null)
            {
                return true;
            }

            bool blocks = ModInputManager.ShouldBlockNativeAction(action);
            if (!blocks)
            {
                return true;
            }

            if (IsDispatchableButtonAction(action) && mapping.IsNotHeld())
            {
                AccessibleSubmitDispatcher.DispatchOnce(action);
            }

            return false;
        }

        private static InputAction ResolveAction(
            global::ScreenManager screenManager,
            CoreInputControlMapping mapping,
            IGameUIComponent triggeredUI)
        {
            global::InputManager inputManager = InputManagerField.GetValue(screenManager) as global::InputManager;
            global::InputManager.Controls control = inputManager != null
                ? inputManager.ResolveTriggeredMappingID(mapping, triggeredUI)
                : (global::InputManager.Controls)mapping.GetID();

            return ModInputManager.GetAction(ActionKey(control));
        }

        private static string ActionKey(global::InputManager.Controls control)
        {
            switch (control)
            {
                case global::InputManager.Controls.Up:
                    return "ui_up";
                case global::InputManager.Controls.Down:
                    return "ui_down";
                case global::InputManager.Controls.Left:
                    return "ui_left";
                case global::InputManager.Controls.Right:
                    return "ui_right";
                case global::InputManager.Controls.Submit:
                case global::InputManager.Controls.AdvanceDialogue:
                    return "ui_accept";
                case global::InputManager.Controls.SkipDialogue:
                    return "ui_select";
                case global::InputManager.Controls.Close:
                case global::InputManager.Controls.Cancel:
                case global::InputManager.Controls.Escape:
                    return "ui_cancel";
                default:
                    return null;
            }
        }

        private static bool IsDispatchableButtonAction(InputAction action)
        {
            return action?.Key == "ui_accept" || action?.Key == "ui_select" || action?.Key == "ui_cancel";
        }
    }
}
