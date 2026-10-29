using MonsterTrainAccessibility.Core.Graph;
using MonsterTrainAccessibility.Core.Strings;
using MonsterTrainAccessibility.Module.Interop;
using MonsterTrainAccessibility.Module.Nodes;
using MonsterTrainAccessibility.Module.UI;
using Screen = MonsterTrainAccessibility.Core.Screens.Screen;

namespace MonsterTrainAccessibility.Module.Screens
{
    /// <summary>
    /// The souls' progress at a Soul Savior run's end (<see cref="global::SoulProgressionScreen"/>):
    /// opening it says the game's header; then the souls the run moved on, in the game's order, each
    /// through <see cref="SoulNodes.Progress"/>; then Collect, once the game shows it (the progress
    /// has played out). Collect goes on through the game.
    /// </summary>
    public sealed class SoulProgressionScreen : Screen
    {
        public override string Key => "game.soul_progression";
        public override int Layer => 10;
        public override bool Exclusive => true;

        private static global::SoulProgressionScreen Game
            => GameScreens.Active<global::SoulProgressionScreen>(global::ScreenName.SoulProgression);

        public override bool IsActive() => Game != null;

        public override void OnPush()
        {
            var screen = Game;
            if (screen != null && screen.headerLabel != null) ScreenIntro.Say(screen.headerLabel.Content());
        }

        public override void Build(GraphBuilder b)
        {
            var screen = Game;
            if (screen == null) return;
            b.PushContext(null);
            var items = screen.soulProgressionUI != null ? screen.soulProgressionUI.soulProgressItemUIs : null;
            if (items != null)
                for (int i = 0; i < items.Count; i++)
                {
                    var item = items[i];
                    if (item == null || !item.gameObject.activeInHierarchy) continue;
                    int at = i;
                    b.AddItem(ControlId.Structural("souls:progress:" + i), SoulNodes.Progress(item,
                        () => screen.soulsUnlockProgress != null && at < screen.soulsUnlockProgress.Count
                            ? screen.soulsUnlockProgress[at] : (global::GameOverScreen.SoulUnlockProgressInfo?)null));
                }
            var collect = screen.collectButton;
            if (collect != null && collect.gameObject.activeInHierarchy)
                b.AddItem(ControlId.Structural("souls:collect"), GameWidgets.Button(collect, () => PauseMenuScreen.Caption(collect)));
            b.PopContext();
        }
    }
}
