using BepInEx.Logging;
using MonsterTrainAccessibility.Contracts;

namespace MonsterTrainAccessibility.Modularity
{
    /// <summary>
    /// The host's <see cref="IModHost"/>: routes the module's logging through the BepInEx logger and
    /// hands it the shared speech pipeline and settings. Lives in the host assembly with
    /// Contracts loaded once, so every module generation sees the same interface identity.
    /// </summary>
    internal sealed class ModHost : IModHost
    {
        private readonly ManualLogSource _log;

        public ModHost(ManualLogSource log, SpeechPipeline speech, ModSettings settings, string pluginDir)
        {
            _log = log;
            Speech = speech;
            Settings = settings;
            PluginDir = pluginDir;
        }

        public SpeechPipeline Speech { get; }

        public ModSettings Settings { get; }

        public string PluginDir { get; }

        public string ModVersion => Plugin.Version;

        public bool LaunchAnnounced { get; set; }

        /// <summary>Set by the loader around the old generation's Dispose on a successful reload.</summary>
        public bool SuccessorLoaded { get; internal set; }

        public void LogInfo(string message) => _log.LogInfo(message);
        public void LogWarning(string message) => _log.LogWarning(message);
        public void LogError(string message) => _log.LogError(message);
    }
}
