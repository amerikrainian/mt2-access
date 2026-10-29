using System.Collections.Generic;
using MonsterTrainAccessibility.Core.Graph;
using MonsterTrainAccessibility.Core.Strings;
using MonsterTrainAccessibility.Core.UI;
using MonsterTrainAccessibility.Module.Interop;
using MonsterTrainAccessibility.Module.Nodes;
using MonsterTrainAccessibility.Module.UI;
using ShinyShoe;
using TMPro;
using Screen = MonsterTrainAccessibility.Core.Screens.Screen;

namespace MonsterTrainAccessibility.Module.Screens
{
    /// <summary>
    /// The run setup's mutator picker (<see cref="MutatorSelectionDialog"/>). Opening it says what it is,
    /// the game's instructions and its warning; then three Tab-stops:
    /// <list type="bullet">
    /// <item>every mutator as a toggle
    /// (<see cref="StartingConditionsNodes.MutatorChoice"/>; the game appears them a few per frame,
    /// and each is listed as it appears);</item>
    /// <item>the chosen ones in the game's order, each read off its own button in the list: the game's
    /// tooltips on the chosen slots localize the description without the card-effect context, so their
    /// numbers come out blank ("cost + ember"); the button draws the full text;</item>
    /// <item>Remove All Mutators and Randomize Mutators, which both close the picker as in the game.</item>
    /// </list>
    /// Escape is the game's: it closes the picker, keeping the choice.
    /// </summary>
    public sealed class RunSetupMutatorsScreen : Screen
    {
        public override string Key => "game.run_setup.mutators";
        public override int Layer => 5;
        public override bool Exclusive => true;
        public override object InitialFocusStop => "mutators";

        private static MutatorSelectionDialog Dialog
        {
            get
            {
                var screen = RunSetupScreen.Game;
                var dialog = screen != null ? screen.mutatorSelectionDialog : null;
                return dialog != null && dialog.IsOpen ? dialog : null;
            }
        }

        public override bool IsActive() => Dialog != null;

        // Opening says what the picker is, the game's instructions and its warning ("Mutators. Choose up
        // to 3 mutators to customize your run. Runs customized with mutators don't count for ...").
        public override void OnPush()
        {
            var dialog = Dialog;
            if (dialog == null) return;
            var lines = new List<string> { GameText.Term("ScreenRunSetup_Mutators") };
            if (dialog.instructionsLabel != null)
            {
                lines.Add(dialog.instructionsLabel.Content());
                foreach (var text in dialog.instructionsLabel.transform.parent.GetComponentsInChildren<TMP_Text>(false))
                    if (text != dialog.instructionsLabel && text.GetComponentInParent<GameUIControlMapping>() == null) lines.Add(text.Content());
            }
            ScreenIntro.Say(lines.ToArray());
        }

        public override void Build(GraphBuilder b)
        {
            var dialog = Dialog;
            if (dialog == null || dialog.mutatorButtons == null) return;
            b.PushContext(null, null, positions: false);

            b.BeginStop("mutators");
            b.PushContext(null);
            foreach (var mutator in dialog.mutatorButtons)
            {
                if (mutator == null || mutator.MutatorState == null || !mutator.gameObject.activeInHierarchy || mutator.Button == null) continue;
                b.AddItem(ControlId.Structural("mutators:" + mutator.MutatorState.GetRelicDataID()), StartingConditionsNodes.MutatorChoice(mutator));
            }
            b.PopContext();

            b.BeginStop("chosen");
            if (dialog.mutatorPreviewRoot != null && dialog.mutatorPreviewRoot.gameObject.activeInHierarchy)
            {
                b.PushContext(null);
                foreach (var id in dialog.chosenMutators)
                {
                    var chosen = ButtonFor(dialog, id);
                    if (chosen != null) b.AddItem(ControlId.Structural("mutators:chosen:" + id), StartingConditionsNodes.Mutator(chosen));
                }
                b.PopContext();
            }

            b.BeginStop("actions");
            Button(b, "removeall", dialog.removeAllButton, "ScreenClassSelection_RemoveAllMutatorsButton");
            Button(b, "randomize", dialog.randomizeButton, "ScreenClassSelection_RandomizeMutators");
            b.PopContext();
        }

        public override IEnumerable<ElementAction> GetActions()
        {
            yield return new ElementAction(ActionIds.Back, Strings.Get("bind.ui.back"),
                _ => GameInput.Send(global::InputManager.Controls.Escape));
        }

        // A chosen mutator's own button in the list.
        private static MutatorButtonUI ButtonFor(MutatorSelectionDialog dialog, string id)
        {
            foreach (var mutator in dialog.mutatorButtons)
                if (mutator != null && mutator.MutatorState != null && mutator.MutatorState.GetRelicDataID() == id) return mutator;
            return null;
        }

        // A button by its caption, else the game's term for it (these draw an icon and a key hint).
        private static void Button(GraphBuilder b, string key, GameUISelectableButton button, string term)
        {
            if (button == null || !button.gameObject.activeInHierarchy) return;
            b.AddItem(ControlId.Structural("mutators:" + key), GameWidgets.Button(button, () => PauseMenuScreen.Caption(button) ?? GameText.Term(term)));
        }
    }
}
