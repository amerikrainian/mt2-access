using System.Reflection;
using HarmonyLib;
using MonsterTrainAccessibility.Input;
using UnityEngine;
using UnityEngine.EventSystems;
using ModInputManager = MonsterTrainAccessibility.Input.InputManager;
using ModScreenManager = MonsterTrainAccessibility.UI.Screens.ScreenManager;

namespace MonsterTrainAccessibility.Patches
{
    internal static class NavigationInputHooks
    {
        private static readonly FieldInfo StandalonePrevActionTimeField = AccessTools.Field(typeof(StandaloneInputModule), "m_PrevActionTime")!;
        private static readonly FieldInfo StandaloneLastMoveVectorField = AccessTools.Field(typeof(StandaloneInputModule), "m_LastMoveVector")!;
        private static readonly FieldInfo StandaloneConsecutiveMoveCountField = AccessTools.Field(typeof(StandaloneInputModule), "m_ConsecutiveMoveCount")!;
        private static readonly PropertyInfo BaseInputModuleInputProperty = AccessTools.Property(typeof(BaseInputModule), "input")!;

        public static void GameInputModuleBridge_GetAxisRaw_Postfix(string axisName)
        {
            if (string.Equals(axisName, "Horizontal", System.StringComparison.Ordinal) ||
                string.Equals(axisName, "Vertical", System.StringComparison.Ordinal))
            {
                ModScreenManager.CurrentScreen?.BeforeNavigationInput();
            }
        }

        public static bool StandaloneInputModule_SendMoveEventToSelectedObject_Prefix(StandaloneInputModule __instance, ref bool __result)
        {
            ModScreenManager.CurrentScreen?.BeforeNavigationInput();

            if (__instance == null)
            {
                return true;
            }

            BaseInput input = GetInput(__instance);
            Vector2 rawMoveVector = GetRawMoveVector(__instance, input);
            MoveDirection direction = DetermineMoveDirection(rawMoveVector, 0.6f);
            InputAction action = GetMoveAction(direction);
            if (action != null && ModInputManager.ShouldBlockBufferNavigation(action.Key))
            {
                __result = false;
                RecordMove(__instance, rawMoveVector);
                return false;
            }

            if (action == null || !ModScreenManager.HasClaimForAction(action))
            {
                return true;
            }

            if (Mathf.Approximately(rawMoveVector.x, 0f) && Mathf.Approximately(rawMoveVector.y, 0f))
            {
                StandaloneConsecutiveMoveCountField.SetValue(__instance, 0);
                __result = false;
                return false;
            }

            if (!ShouldSendMove(__instance, input, rawMoveVector))
            {
                __result = false;
                return false;
            }

            __result = ModScreenManager.DispatchAction(action, InputActionState.JustPressed);
            RecordMove(__instance, rawMoveVector);
            return false;
        }

        public static bool StandaloneInputModule_SendSubmitEventToSelectedObject_Prefix(StandaloneInputModule __instance, ref bool __result)
        {
            BaseInput input = __instance != null ? GetInput(__instance) : null;
            bool submitDown = __instance != null && GetButtonDown(input, __instance.submitButton);
            if (!submitDown)
            {
                return true;
            }

            GameObject selected = EventSystem.current?.currentSelectedGameObject;
            if (selected != null &&
                selected.GetComponent<global::ShinyShoe.IGameUIComponent>() != null &&
                ModScreenManager.CurrentScreen?.ShouldAcceptGameSelection() != false)
            {
                return true;
            }

            InputAction action = ModInputManager.GetAction("ui_select");
            if (action == null || !ModScreenManager.HasClaimForAction(action))
            {
                return true;
            }

            __result = AccessibleSubmitDispatcher.DispatchOnce(action);
            return false;
        }

        private static InputAction GetMoveAction(MoveDirection direction)
        {
            switch (direction)
            {
                case MoveDirection.Left:
                    return ModInputManager.GetAction("ui_left");
                case MoveDirection.Up:
                    return ModInputManager.GetAction("ui_up");
                case MoveDirection.Right:
                    return ModInputManager.GetAction("ui_right");
                case MoveDirection.Down:
                    return ModInputManager.GetAction("ui_down");
                default:
                    return null;
            }
        }

        private static BaseInput GetInput(BaseInputModule inputModule)
        {
            object value = BaseInputModuleInputProperty.GetValue(inputModule, null);
            return value as BaseInput;
        }

        private static Vector2 GetRawMoveVector(StandaloneInputModule inputModule, BaseInput input)
        {
            Vector2 rawMoveVector = Vector2.zero;
            rawMoveVector.x = input != null ? input.GetAxisRaw(inputModule.horizontalAxis) : UnityEngine.Input.GetAxisRaw(inputModule.horizontalAxis);
            rawMoveVector.y = input != null ? input.GetAxisRaw(inputModule.verticalAxis) : UnityEngine.Input.GetAxisRaw(inputModule.verticalAxis);

            if (GetButtonDown(input, inputModule.horizontalAxis))
            {
                if (rawMoveVector.x < 0f)
                {
                    rawMoveVector.x = -1f;
                }
                if (rawMoveVector.x > 0f)
                {
                    rawMoveVector.x = 1f;
                }
            }

            if (GetButtonDown(input, inputModule.verticalAxis))
            {
                if (rawMoveVector.y < 0f)
                {
                    rawMoveVector.y = -1f;
                }
                if (rawMoveVector.y > 0f)
                {
                    rawMoveVector.y = 1f;
                }
            }

            return rawMoveVector;
        }

        private static bool ShouldSendMove(StandaloneInputModule inputModule, BaseInput input, Vector2 rawMoveVector)
        {
            bool buttonDown = GetButtonDown(input, inputModule.horizontalAxis) || GetButtonDown(input, inputModule.verticalAxis);
            if (buttonDown)
            {
                return true;
            }

            Vector2 lastMoveVector = (Vector2)StandaloneLastMoveVectorField.GetValue(inputModule);
            bool sameDirection = Vector2.Dot(rawMoveVector, lastMoveVector) > 0f;
            int consecutiveMoveCount = (int)StandaloneConsecutiveMoveCountField.GetValue(inputModule);
            float prevActionTime = (float)StandalonePrevActionTimeField.GetValue(inputModule);
            float repeatDelay = sameDirection && consecutiveMoveCount == 1
                ? inputModule.repeatDelay
                : 1f / inputModule.inputActionsPerSecond;

            return Time.unscaledTime > prevActionTime + repeatDelay;
        }

        private static bool GetButtonDown(BaseInput input, string buttonName)
        {
            return input != null ? input.GetButtonDown(buttonName) : UnityEngine.Input.GetButtonDown(buttonName);
        }

        private static MoveDirection DetermineMoveDirection(Vector2 rawMoveVector, float deadZone)
        {
            if (rawMoveVector.sqrMagnitude < deadZone * deadZone)
            {
                return MoveDirection.None;
            }

            if (Mathf.Abs(rawMoveVector.x) > Mathf.Abs(rawMoveVector.y))
            {
                return rawMoveVector.x > 0f ? MoveDirection.Right : MoveDirection.Left;
            }

            return rawMoveVector.y > 0f ? MoveDirection.Up : MoveDirection.Down;
        }

        private static void RecordMove(StandaloneInputModule inputModule, Vector2 rawMoveVector)
        {
            Vector2 lastMoveVector = (Vector2)StandaloneLastMoveVectorField.GetValue(inputModule);
            bool sameDirection = Vector2.Dot(rawMoveVector, lastMoveVector) > 0f;
            int consecutiveMoveCount = (int)StandaloneConsecutiveMoveCountField.GetValue(inputModule);
            if (!sameDirection)
            {
                consecutiveMoveCount = 0;
            }

            StandaloneConsecutiveMoveCountField.SetValue(inputModule, consecutiveMoveCount + 1);
            StandalonePrevActionTimeField.SetValue(inputModule, Time.unscaledTime);
            StandaloneLastMoveVectorField.SetValue(inputModule, rawMoveVector);
        }
    }
}
