using MonsterTrainAccessibility.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using ModScreenManager = MonsterTrainAccessibility.UI.Screens.ScreenManager;

namespace MonsterTrainAccessibility.Patches
{
    internal static class SelectionHooks
    {
        public static void GameUISelectableCommonExtensions_OnSelectCommon_Postfix(
            global::ShinyShoe.IGameUISelectableCommon common,
            BaseEventData eventData)
        {
            try
            {
                global::InputManager inputManager = global::InputManager.Inst;
                if (inputManager == null || !inputManager.currentInputModeUsesNavigation)
                {
                    if (Settings.VerboseFocusLogging?.Value == true)
                    {
                        string reason = inputManager == null
                            ? "game input manager unavailable"
                            : "input mode is " + inputManager.currentInputDeviceMode;
                        Log.Info("[AccessibilityMod] Selection hook ignored: " + reason);
                    }
                    return;
                }

                if (ModScreenManager.CurrentScreen?.ShouldAcceptGameSelection() == false)
                {
                    if (Settings.VerboseFocusLogging?.Value == true)
                    {
                        Log.Info("[AccessibilityMod] Selection hook ignored: current screen owns accessibility focus");
                    }
                    return;
                }

                global::ShinyShoe.IGameUIComponent component = common?.GetGameUIComponent();
                GameObject selected = component?.component?.gameObject;
                if (selected == null)
                {
                    if (Settings.VerboseFocusLogging?.Value == true)
                    {
                        Log.Info("[AccessibilityMod] Selection hook ignored: selected component unavailable");
                    }
                    return;
                }

                if (Settings.VerboseFocusLogging?.Value == true)
                {
                    Log.Info("[AccessibilityMod] Selection hook selected: " + selected.name);
                }

                UIManager.NotifyGameObjectSelected(selected);
            }
            catch (System.Exception ex)
            {
                Log.Info("Selection focus hook failed, falling back to frame polling: " + ex);
            }
        }
    }
}
