namespace MonsterTrainAccessibility.Module.Interop
{
    /// <summary>
    /// The game's live screens, through its own registry: <c>ScreenManager.GetScreen</c> and
    /// <c>GetScreenActive</c> by <see cref="ScreenName"/>, the manager reached through
    /// <see cref="AllGameManagers.Instance"/>. Nothing is cached and nothing is scanned: every read asks
    /// the game, so a screen the game tore down reads as absent on the next frame.
    /// </summary>
    internal static class GameScreens
    {
        private static global::ScreenManager Manager => AllGameManagers.Instance?.GetScreenManager();

        /// <summary>The screen of that name while the game has it active and its object is live, else null.</summary>
        public static T Active<T>(ScreenName name) where T : UIScreen
        {
            var manager = Manager;
            if (manager == null || !manager.GetScreenActive(name)) return null;
            var screen = manager.GetScreen(name) as T;
            return screen != null && screen.gameObject.activeInHierarchy ? screen : null;
        }
    }
}
