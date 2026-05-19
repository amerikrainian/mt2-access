using BepInEx.Configuration;
using MonsterTrainAccessibility.Presentation.Verbosity;

namespace MonsterTrainAccessibility.ModSettings
{
    internal static class VerbositySettings
    {
        public static void Register(ConfigFile config)
        {
            VerbosityRegistry.Initialize(config);
        }
    }
}
