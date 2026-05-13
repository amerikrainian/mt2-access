using MonsterTrainAccessibility.Localization;
using MonsterTrainAccessibility.Speech;
namespace MonsterTrainAccessibility.Updates
{
    internal static class StartupAnnouncement
    {
        private static bool _versionAnnounced;
        private static bool _updateAnnounced;

        public static void Reset()
        {
            _versionAnnounced = false;
            _updateAnnounced = false;
        }

        public static void Update()
        {
            if (!_versionAnnounced && ShouldAnnounceOnCurrentScreen())
            {
                _versionAnnounced = true;
                SpeechManager.Output(Message.Localized("ui", "MOD.VERSION_ANNOUNCE", new
                {
                    version = global::MonsterTrainAccessibility.MonsterTrainAccessibility.VERSION
                }));
            }

            if (_versionAnnounced && !_updateAnnounced && !string.IsNullOrEmpty(UpdateChecker.LatestRemoteVersion))
            {
                _updateAnnounced = true;
                SpeechManager.Output(Message.Localized("ui", "MOD.UPDATE_AVAILABLE", new
                {
                    version = UpdateChecker.LatestRemoteVersion
                }));
            }
        }

        private static bool ShouldAnnounceOnCurrentScreen()
        {
            global::MonsterTrainAccessibility.UI.Screens.Screen current =
                global::MonsterTrainAccessibility.UI.Screens.ScreenManager.CurrentScreen;
            return current is global::MonsterTrainAccessibility.UI.Screens.IntroStartScreen ||
                   current is global::MonsterTrainAccessibility.UI.Screens.MainMenuScreen;
        }
    }
}
