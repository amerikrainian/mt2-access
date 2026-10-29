using MonsterTrainAccessibility.Module.Interop;

namespace MonsterTrainAccessibility.Module.UI
{
    /// <summary>
    /// The game's Bind Keys screen waiting for the key to bind (<see cref="global::KeyMappingScreen"/>
    /// after Bind or Add): it reads that key from Unity's legacy input itself, untouched by focus mode, so
    /// the mod's keys stand down while it waits (<see cref="OwnsKeyboard"/>, through the same seam a text
    /// field uses), and stay down until the key it took is released, so that key does not also act as
    /// one of ours.
    /// </summary>
    public static class KeyCapture
    {
        private static bool _hold;

        /// <summary>The game is waiting for a key to bind.</summary>
        public static bool Listening
        {
            get
            {
                var screen = GameScreens.Active<global::KeyMappingScreen>(global::ScreenName.KeyMapping);
                return screen != null && screen.currentlyRemapping != null;
            }
        }

        public static bool OwnsKeyboard => _hold || Listening;

        /// <summary>Per frame: hold the keyboard from the wait to the release of the key it took.</summary>
        public static void Tick()
        {
            if (Listening) _hold = true;
            else if (_hold && !UnityEngine.Input.anyKey) _hold = false;
        }
    }
}
