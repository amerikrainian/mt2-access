using System.IO;
using BepInEx;
using BepInEx.Logging;
using MonsterTrainAccessibility.Contracts;
using MonsterTrainAccessibility.Dev;
using MonsterTrainAccessibility.Host;
using MonsterTrainAccessibility.Modularity;
using MonsterTrainAccessibility.Speech;
using UnityEngine;

namespace MonsterTrainAccessibility
{
    /// <summary>
    /// The permanent host: only what can never reload. Stands up Prism, the speech pipeline, the settings
    /// store, the dev server, the one pump, and the module loader, then hands per-frame work to the
    /// reloadable side (Core + Module). No feature logic lives here.
    /// </summary>
    [BepInPlugin(Guid, Name, BuildVersion.Value)]
    public sealed class Plugin : BaseUnityPlugin
    {
        // The main branch's GUID: the config file (BepInEx/config/<guid>.cfg) carries over.
        public const string Guid = "com.accessibility.monstertrain";
        public const string Name = "Monster Train Accessibility";
        public const string Version = BuildVersion.Value;

        internal static ManualLogSource Log;

        private PrismBackend _prism;
        private ModuleLoader _loader;
#if DEBUG
        private DevServer _devServer;
#endif

        private void Awake()
        {
            Log = Logger;
            Log.LogInfo($"{Name} {Version} loading");

            // Headless dev runs set MTA_NO_SPEECH=1 so an unattended session does not depend on a running
            // screen reader. Prism init is skipped; spoken text is still captured for /speech, since the
            // dev tap is upstream of the backend.
            bool muteSpeech = System.Environment.GetEnvironmentVariable("MTA_NO_SPEECH") == "1";
            _prism = new PrismBackend(Log);
            if (muteSpeech)
                Log.LogInfo("Speech muted (MTA_NO_SPEECH=1); spoken text still captured for /speech");
            else
                _prism.Initialize();

            var speech = new SpeechPipeline(_prism) { Muted = muteSpeech };

            // Settings persist through our BepInEx config file and are owned here (permanent) so they
            // survive a module hot-reload; the module reads them through IModHost.Settings.
            var settings = new ModSettings(new BepInExSettingsStore(Config));

            string pluginDir = Path.GetDirectoryName(typeof(Plugin).Assembly.Location);
            var host = new ModHost(Log, speech, settings, pluginDir);

            _loader = new ModuleLoader(
                Path.Combine(pluginDir, "MonsterTrainAccessibility.Core.dll"),
                Path.Combine(pluginDir, "MonsterTrainAccessibility.Module.dll"),
                host, Log);

#if DEBUG
            // Dev-only: the loopback server exposes a C# eval REPL; never in a player build. Started first
            // so its speech tap catches the load line.
            _devServer = new DevServer(_loader, Log);
            _devServer.Start();
#endif

            // The "loaded" line is the module's: it has the strings table, and says the line once the
            // game's language is known (IModHost.LaunchAnnounced). The module carries every feature;
            // if it fails to load the player would otherwise get silence with no signal, so say so out
            // loud (the cause is in the log).
            if (!_loader.Load())
                speech.Speak(Name + " features failed to load", interrupt: false);

            // The pump lives on a GameObject of its own that survives scene loads; the chainloader's
            // manager object is not ours to keep alive.
            var pumpObject = new GameObject("MonsterTrainAccessibility.HostPump");
            DontDestroyOnLoad(pumpObject);
            pumpObject.hideFlags = HideFlags.HideAndDontSave;
            HostPump.Loader = _loader;
#if DEBUG
            HostPump.DevServer = _devServer;
#endif
            pumpObject.AddComponent<HostPump>();

            Log.LogInfo($"{Name} loaded");
        }
    }
}
