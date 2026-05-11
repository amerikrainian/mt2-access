using System;
using BepInEx.Logging;

namespace MonsterTrainAccessibility.Core
{
    public static class Log
    {
        private const string Prefix = "[MonsterTrainAccessibility]";

        internal static ManualLogSource Source;

        public static void Debug(string msg)
        {
            if (Source != null) Source.LogDebug(msg);
            else Console.WriteLine($"{Prefix} [DEBUG] {msg}");
        }

        public static void Info(string msg)
        {
            if (Source != null) Source.LogInfo(msg);
            else Console.WriteLine($"{Prefix} {msg}");
        }

        public static void Warn(string msg)
        {
            if (Source != null) Source.LogWarning(msg);
            else Console.WriteLine($"{Prefix} [WARN] {msg}");
        }

        public static void Error(string msg)
        {
            if (Source != null) Source.LogError(msg);
            else Console.Error.WriteLine($"{Prefix} [ERROR] {msg}");
        }
    }
}
