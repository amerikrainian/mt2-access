using MonsterTrainAccessibility.Input;
using UnityEngine;
using ModScreenManager = MonsterTrainAccessibility.UI.Screens.ScreenManager;

namespace MonsterTrainAccessibility.Patches
{
    internal static class AccessibleSubmitDispatcher
    {
        private static int _lastSubmitFrame = -1;

        public static bool DispatchOnce(InputAction action)
        {
            if (action == null)
            {
                return false;
            }

            int frame = Time.frameCount;
            if (_lastSubmitFrame == frame)
            {
                return true;
            }

            _lastSubmitFrame = frame;
            return ModScreenManager.DispatchAction(action, InputActionState.JustPressed);
        }
    }
}
