namespace MonsterTrainAccessibility.Contracts
{
    /// <summary>
    /// The mod's settings, declared once here and read through the store on every access so a value
    /// edited in the config file is picked up without a restart. Host-owned (built with its concrete
    /// <see cref="ISettingsStore"/>) and lent to the module through <see cref="IModHost.Settings"/>, so
    /// values survive a module hot-reload. Reloadable code adds typed accessors here only when a setting
    /// must be host-visible; feature-local preferences can key the store directly.
    /// </summary>
    public sealed class ModSettings
    {
        private readonly ISettingsStore _store;

        public ModSettings(ISettingsStore store)
        {
            _store = store;
        }

        /// <summary>The raw store, for reloadable code declaring its own keys.</summary>
        public ISettingsStore Store => _store;

        /// <summary>Whether our keyboard navigation starts engaged at launch (focus mode on).</summary>
        public bool FocusModeOnLaunch
        {
            get => _store.GetBool("focus_mode_on_launch", true);
            set => _store.SetBool("focus_mode_on_launch", value);
        }

        /// <summary>Whether list items append their position ("3 of 10"). Off unless asked for.</summary>
        public bool SpeakPositions
        {
            get => _store.GetBool("speak_positions", false);
            set => _store.SetBool("speak_positions", value);
        }
    }
}
