using System.Collections.Generic;
using MonsterTrainAccessibility.Core.Graph;
using MonsterTrainAccessibility.Core.UI;
using MonsterTrainAccessibility.Module.Interop;
using MonsterTrainAccessibility.Module.UI;
using Screen = MonsterTrainAccessibility.Core.Screens.Screen;

namespace MonsterTrainAccessibility.Module.Screens
{
    /// <summary>
    /// The credits (<see cref="global::CreditsScreen"/>): a video, nothing to read. Opening it says the
    /// game's "Credits"; its one control skips them, as holding the game's cancel key does (the game hears
    /// no key while focus mode is on, so this is the only way out but the video's end). Escape skips too.
    /// </summary>
    public sealed class CreditsScreen : Screen
    {
        public override string Key => "game.credits";

        private static global::CreditsScreen Game => GameScreens.Active<global::CreditsScreen>(global::ScreenName.Credits);

        public override bool IsActive() => Game != null;

        public override void OnPush() => ScreenIntro.Say(GameText.Term("CreditsScreen_Title"));

        public override void Build(GraphBuilder b)
        {
            var screen = Game;
            if (screen == null) return;
            b.AddItem(ControlId.Structural("credits:skip"), GameNodes.Button(() => GameText.Term("Skip"), Skip));
        }

        public override IEnumerable<ElementAction> GetActions()
        {
            yield return new ElementAction(ActionIds.Back, GameText.Term("Skip"), _ => Skip());
        }

        // What the held cancel key reaches: the video stops and the game moves on (the main menu, or
        // where a run's end sends it).
        private static void Skip()
        {
            var screen = Game;
            if (screen != null) screen.GoToNextScreen();
        }
    }
}
