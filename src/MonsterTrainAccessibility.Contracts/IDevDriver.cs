namespace MonsterTrainAccessibility.Contracts
{
    /// <summary>
    /// A dev-only seam the reloadable module exposes so the host's dev server can drive and inspect the
    /// mod's OWN UI navigation (the game-level injector only sees uGUI selection, which our navigator
    /// bypasses). The host probes the loaded module for this by cast; a module that does not implement
    /// it leaves the dev server on its game-level fallback. Not used in normal play.
    /// </summary>
    public interface IDevDriver
    {
        /// <summary>Dispatch one registered input action by key ("ui.down", "ui.activate", or any mod
        /// hotkey key) exactly as a real key press would route it. Returns a status line, or null when
        /// the action is unknown so the caller can fall back or list what exists.</summary>
        string DispatchAction(string actionKey);

        /// <summary>Describe our navigator's live state: the focused screen, the focus path with labels,
        /// and the focused node. Independent of the game's own selection.</summary>
        string DescribeNav();

        /// <summary>Every registered action key, one per line, for the driver to discover verbs.</summary>
        string ListActions();
    }
}
