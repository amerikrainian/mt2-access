using System;
using System.Reflection;
using HarmonyLib;

namespace MonsterTrainAccessibility.Core
{
    internal static class HarmonyHelper
    {
        public static bool PatchIfFound(
            Harmony harmony,
            Type targetType,
            string methodName,
            HarmonyMethod prefix = null,
            HarmonyMethod postfix = null,
            Type[] argTypes = null)
        {
            if (harmony == null || targetType == null || string.IsNullOrEmpty(methodName))
            {
                return false;
            }

            MethodInfo targetMethod = argTypes == null
                ? AccessTools.Method(targetType, methodName)
                : AccessTools.Method(targetType, methodName, argTypes);

            if (targetMethod == null)
            {
                Log.Warn($"Patch target not found: {targetType.FullName}.{methodName}");
                return false;
            }

            try
            {
                harmony.Patch(targetMethod, prefix, postfix);
                Log.Info("Patch applied: " + targetType.FullName + "." + methodName);
                return true;
            }
            catch (Exception ex)
            {
                Log.Warn($"Patch failed for {targetType.FullName}.{methodName}: {ex}");
                return false;
            }
        }
    }
}
