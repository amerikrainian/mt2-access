using MonsterTrainAccessibility.Core.Graph;
using MonsterTrainAccessibility.Module.Interop;
using MonsterTrainAccessibility.Module.UI;
using Screen = MonsterTrainAccessibility.Core.Screens.Screen;

namespace MonsterTrainAccessibility.Module.Screens
{
    /// <summary>
    /// The settings the game asks for on its first launch (<see cref="global::FirstTimeSettingsScreen"/>):
    /// opening it says its title ("Settings"); then its entries as Settings reads its own
    /// (<see cref="SettingsScreen.Row"/>: language, the volumes, mute in background, resolution, display
    /// mode), each changed through the game; then Depart, which goes on to the game's intro.
    /// </summary>
    public sealed class FirstTimeSettingsScreen : Screen
    {
        public override string Key => "game.first_time_settings";

        private static global::FirstTimeSettingsScreen Game
            => GameScreens.Active<global::FirstTimeSettingsScreen>(global::ScreenName.FirstTimeSettings);

        public override bool IsActive() => Game != null;

        public override void OnPush()
        {
            var screen = Game;
            if (screen != null) ScreenIntro.Say(ChallengeProgressScreen.HeaderOf(screen, "Title"));
        }

        public override void Build(GraphBuilder b)
        {
            var screen = Game;
            if (screen == null) return;
            b.PushContext(null);
            foreach (var entry in screen.GetComponentsInChildren<SettingsEntry>(false))
                SettingsScreen.Row(b, "firsttime", entry.transform);
            var depart = screen.confirmButton;
            if (depart != null && depart.gameObject.activeInHierarchy)
                b.AddItem(ControlId.Structural("firsttime:depart"), GameWidgets.Button(depart, () => PauseMenuScreen.Caption(depart)));
            b.PopContext();
        }
    }
}
