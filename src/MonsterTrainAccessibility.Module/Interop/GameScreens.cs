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

        /// <summary>The screen of that name while the game has it active and its object is live, else null.
        /// A screen the game is only preloading is not open: a preload (<c>PreloadAdditiveScreen</c>, the
        /// battle intro's Minimap, the map's Reward and Deck) sets it active with initialization barred,
        /// loads its assets and closes it again, a few frames the player never sees.</summary>
        public static T Active<T>(ScreenName name) where T : UIScreen
        {
            var manager = Manager;
            if (manager == null || !manager.GetScreenActive(name)) return null;
            var screen = manager.GetScreen(name) as T;
            return screen != null && screen.allowInitialization && screen.gameObject.activeInHierarchy ? screen : null;
        }

        // A load counts once it has lasted a few frames: opening a run dialog has the game report one for
        // a single frame, which is no load to stand down for.
        private const int LoadFrames = 5;
        private static int _loadingSince = -1;

        /// <summary>Whether the game's loading screen is up, and has been for a few frames.</summary>
        public static bool Loading()
        {
            if (!ShinyShoe.Loading.LoadingScreen.IsWorking())
            {
                _loadingSince = -1;
                return false;
            }
            if (_loadingSince < 0) _loadingSince = UnityEngine.Time.frameCount;
            return UnityEngine.Time.frameCount - _loadingSince >= LoadFrames;
        }
    }
}
