using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace MonsterTrainAccessibility.Patches
{
    internal static class KeyMappingInputHooks
    {
        private static readonly FieldInfo CurrentlyRemappingField = AccessTools.Field(typeof(global::KeyMappingScreen), "currentlyRemapping")!;

        private static int _capturedBindingFrame = -1;

        public static void KeyMappingScreen_DetectBindingKeyPress_Prefix(global::KeyMappingScreen __instance, ref bool __state)
        {
            __state = CurrentlyRemappingField.GetValue(__instance) != null;
        }

        public static void KeyMappingScreen_DetectBindingKeyPress_Postfix(global::KeyMappingScreen __instance, bool __state)
        {
            if (__state && CurrentlyRemappingField.GetValue(__instance) == null)
            {
                _capturedBindingFrame = Time.frameCount;
            }
        }

        public static bool ShouldBlockCapturedBindingInputFrame()
        {
            return Time.frameCount == _capturedBindingFrame;
        }
    }
}
