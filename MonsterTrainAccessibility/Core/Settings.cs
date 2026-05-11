using BepInEx.Configuration;

namespace MonsterTrainAccessibility.Core
{
    internal static class Settings
    {
        private const string DiagnosticsSection = "Diagnostics";
        private const string DebugSection = "Debug";

        public static ConfigEntry<bool> VerboseFocusLogging { get; private set; }
        public static ConfigEntry<bool> DebugCommandsEnabled { get; private set; }

        public static void Initialize(ConfigFile config)
        {
            if (config == null || VerboseFocusLogging != null)
            {
                return;
            }

            VerboseFocusLogging = config.Bind(
                DiagnosticsSection,
                "VerboseFocusLogging",
                false,
                new ConfigDescription("Logs every focus transition to LogOutput.log. Enable while debugging screen reader focus issues."));

            DebugCommandsEnabled = config.Bind(
                DebugSection,
                "DebugCommandsEnabled",
                true,
                new ConfigDescription("Enables Ctrl+Shift+D screen-reader debug commands for deterministic accessibility testing."));
        }
    }
}
