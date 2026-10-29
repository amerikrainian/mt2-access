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

        public override bool IsActive() => Dialog != null;

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
            var start = Option(b, "new", dialog.standardRunOption);
            var resume = Option(b, "resume", dialog.continueGameOption);
            if (resume != null) SavedRun(b, dialog.continueGameOption);
            Option(b, "newgame", dialog.newGameOption);
            Option(b, "daily", dialog.dailyChallengeOption);
            Option(b, "community", dialog.customChallengeOption);
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

        // A run type: its panel's title ("Resume Run", "Daily Challenge") on the panel's play button.
        private static ControlId Option(GraphBuilder b, string key, GameModeOption option)
        {
            if (option == null || !option.gameObject.activeInHierarchy || option.playButton == null) return null;
            var button = option.playButton.Button;
            if (button == null) return null;
            var id = ControlId.Structural("depart:" + key);
            b.AddItem(id, GameWidgets.Button(button, () => ChallengeProgressScreen.HeaderOf(option, "Title label")));
            return id;
        }

        // The saved run as Resume Run's panel draws it: the mode label when there is one ("Endless
        // Mode"), then each stat the game gives a tooltip, in the panel's order.
        private static void SavedRun(GraphBuilder b, ContinueGameOption option)
        {
            ModeLabel(b, "endless", option.endlessModeLabel);
            ModeLabel(b, "soulsavior", option.soulSaviorModeLabel);
            int i = 0;
            foreach (var provider in option.GetComponentsInChildren<ITooltipProvider>(false))
            {
                var widget = provider as Component;
                if (widget == null || TooltipReader.Lines(widget).Count == 0) continue;
                var id = ControlId.Structural("depart:run:" + widget.gameObject.name + ":" + i++);
                var covenant = widget.GetComponent<ChallengeCovenantUI>();
                b.AddItem(id, covenant != null ? StartingConditionsNodes.Covenant(covenant) : TooltipReader.Item(widget));
            }
        }

        private static void ModeLabel(GraphBuilder b, string key, Transform label)
        {
            if (label == null || !label.gameObject.activeInHierarchy) return;
            var text = label.GetComponentInChildren<TMP_Text>(false);
            if (text == null || string.IsNullOrWhiteSpace(text.Content())) return;
            b.AddItem(ControlId.Structural("depart:mode:" + key), GameNodes.Text(() => text.Content()));
        }
    }
}
