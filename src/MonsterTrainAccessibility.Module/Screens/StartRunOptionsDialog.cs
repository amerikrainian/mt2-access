using System.Collections.Generic;
using MonsterTrainAccessibility.Core.Graph;
using MonsterTrainAccessibility.Core.Strings;
using MonsterTrainAccessibility.Core.UI;
using MonsterTrainAccessibility.Module.Interop;
using MonsterTrainAccessibility.Module.Nodes;
using MonsterTrainAccessibility.Module.UI;
using TMPro;
using UnityEngine;
using Screen = MonsterTrainAccessibility.Core.Screens.Screen;

namespace MonsterTrainAccessibility.Module.Screens
{
    /// <summary>
    /// Depart's run types (<see cref="global::StartRunOptionsDialog"/>, opened over the Covenant Outpost).
    /// Opening it speaks its header and line ("Depart. Choose your run type."); then one list in the
    /// dialog's order: New Run while there is no saved run, else Resume Run followed by the saved run as the game draws it on that panel (clans,
    /// Covenant rank, distance, mutators, win streaks, each read by its own tooltips, the Covenant's
    /// effects in the buffer) and New Run; then Daily Challenge, Community Challenges and Back. Focus
    /// lands where the game's own selection starts. Enter presses through the game (New Run over a saved
    /// run asks the game's confirmation first); Escape is the game's: it closes the dialog.
    /// </summary>
    public sealed class StartRunOptionsDialog : Screen
    {
        public override string Key => "game.depart";
        public override int Layer => 5;
        public override bool Exclusive => true;

        private static global::StartRunOptionsDialog Dialog
        {
            get
            {
                var menu = GameScreens.Active<global::MainMenuScreen>(global::ScreenName.MainMenu);
                var dialog = menu != null ? menu.startRunOptionsDialog as global::StartRunOptionsDialog : null;
                return dialog != null && dialog.IsOpen ? dialog : null;
            }
        }

        // Silent while the game loads the screen a choice opened (a challenge's details): it stays open
        // under the load, and would be read again between the game's loading message and that screen.
        // A one-frame load as the dialog opens is none (GameScreens.Loading).
        public override bool IsActive() => Dialog != null && !GameScreens.Loading();

        // Opening speaks the dialog's header and its line ("Depart. Choose your run type."), once: not
        // again when focus comes back from the game's confirmation. The list is unnamed, so the first
        // readout is the option landed on.
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
            var start = RunOptionNodes.Option(b, "depart:new", dialog.standardRunOption);
            var resume = RunOptionNodes.Option(b, "depart:resume", dialog.continueGameOption);
            if (resume != null) RunOptionNodes.SavedRun(b, "depart", dialog.continueGameOption);
            RunOptionNodes.Option(b, "depart:newgame", dialog.newGameOption);
            RunOptionNodes.Option(b, "depart:daily", dialog.dailyChallengeOption);
            RunOptionNodes.Option(b, "depart:community", dialog.customChallengeOption);
            var back = dialog.backButton;
            if (back != null && back.gameObject.activeInHierarchy)
                b.AddItem(ControlId.Structural("depart:back"), GameWidgets.Button(back, () => PauseMenuScreen.Caption(back)));
            b.PopContext();
            // The game's own default: New Run with no saved run, else Resume Run.
            if (start != null || resume != null) b.SetStart(start ?? resume);
        }

        public override IEnumerable<ElementAction> GetActions()
        {
            yield return new ElementAction(ActionIds.Back, Strings.Get("bind.ui.back"),
                _ => GameInput.Send(global::InputManager.Controls.Escape));
        }
    }
}
