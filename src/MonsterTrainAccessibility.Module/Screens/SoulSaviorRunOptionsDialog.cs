using System.Collections.Generic;
using MonsterTrainAccessibility.Core.Graph;
using MonsterTrainAccessibility.Core.Strings;
using MonsterTrainAccessibility.Core.UI;
using MonsterTrainAccessibility.Module.Interop;
using MonsterTrainAccessibility.Module.Nodes;
using MonsterTrainAccessibility.Module.UI;
using Screen = MonsterTrainAccessibility.Core.Screens.Screen;

namespace MonsterTrainAccessibility.Module.Screens
{
    /// <summary>
    /// The Rift Station's Depart (<see cref="global::SoulSaviorRunOptionsDialog"/>, opened over the Soul
    /// Savior hub when a Soul Savior run is saved). Opening it speaks its header ("Depart"); then one
    /// list: Resume Run, followed by the saved run as its panel draws it (the mode, clans, difficulty,
    /// the bosses, each read by its own tooltips, <see cref="RunOptionNodes"/>), New Run, Back. Focus
    /// starts on Resume Run, the game's own default. Enter presses through the game (New Run over the
    /// saved run starts a new one, as the game does); Escape is the game's: it closes the dialog.
    /// </summary>
    public sealed class SoulSaviorRunOptionsDialog : Screen
    {
        public override string Key => "game.soulsavior.depart";
        public override int Layer => 5;
        public override bool Exclusive => true;

        private static global::SoulSaviorRunOptionsDialog Dialog
        {
            get
            {
                var hub = GameScreens.Active<global::SoulSaviorScreen>(global::ScreenName.SoulSavior);
                var dialog = hub != null ? hub.soulSaviorRunOptionsDialog as global::SoulSaviorRunOptionsDialog : null;
                return dialog != null && dialog.IsOpen ? dialog : null;
            }
        }

        // Silent while the game loads the run a choice started: the dialog stays open under the load.
        public override bool IsActive() => Dialog != null && !GameScreens.Loading();

        public override void OnPush()
        {
            var dialog = Dialog;
            if (dialog == null) return;
            ScreenIntro.Say(ChallengeProgressScreen.HeaderOf(dialog, "Header label"), ChallengeProgressScreen.HeaderOf(dialog, "Description"));
        }

        public override void Build(GraphBuilder b)
        {
            var dialog = Dialog;
            if (dialog == null) return;
            b.PushContext(null);
            var resume = RunOptionNodes.Option(b, "soulsavior:resume", dialog.continueGameOption);
            if (resume != null) RunOptionNodes.SavedRun(b, "soulsavior", dialog.continueGameOption);
            var start = RunOptionNodes.Option(b, "soulsavior:newgame", dialog.newGameOption);
            var back = dialog.backButton;
            if (back != null && back.gameObject.activeInHierarchy)
                b.AddItem(ControlId.Structural("soulsavior:back"), GameWidgets.Button(back, () => PauseMenuScreen.Caption(back)));
            b.PopContext();
            if (resume != null || start != null) b.SetStart(resume ?? start);
        }

        public override IEnumerable<ElementAction> GetActions()
        {
            yield return new ElementAction(ActionIds.Back, Strings.Get("bind.ui.back"),
                _ => GameInput.Send(global::InputManager.Controls.Escape));
        }
    }
}
