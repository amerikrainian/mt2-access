namespace MonsterTrainAccessibility.Contracts
{
    /// <summary>
    /// The services the permanent host lends to a reloadable module: a logging seam, the shared speech
    /// pipeline, the mod settings, and where the mod's files live. The host implements this; the module
    /// receives it in <see cref="IModModule.Load"/> and calls back through it. Everything here is typed by
    /// this assembly so host and module agree on identity across the load-context boundary.
    /// </summary>
    public interface IModHost
    {
        void LogInfo(string message);
        void LogWarning(string message);
        void LogError(string message);

        /// <summary>The running mod version ("0.1.0"), for the launch line.</summary>
        string ModVersion { get; }

        /// <summary>The single funnel for everything the mod says (the host owns its lifetime).</summary>
        SpeechPipeline Speech { get; }

        /// <summary>The mod's settings, owned by the host so they outlive a module reload.</summary>
        ModSettings Settings { get; }

        /// <summary>The plugin folder (where lang/ and assets/ deploy beside the DLLs).</summary>
        string PluginDir { get; }

        /// <summary>Whether this launch's "loaded" line has been spoken. The module speaks it, once the
        /// game's language is known, and sets this; the host holds it so a reloaded module does not
        /// say it again.</summary>
        bool LaunchAnnounced { get; set; }

        /// <summary>True while a module is being disposed because a newer generation has already loaded
        /// and taken over the game state it suppressed (the keyboard, the EventSystem): restore nothing
        /// then, only drop hooks. False on a shutdown, where everything must be given back.</summary>
        bool SuccessorLoaded { get; }
    }
}
