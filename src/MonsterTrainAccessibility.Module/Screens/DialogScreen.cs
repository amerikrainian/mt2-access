using System.Collections.Generic;
using MonsterTrainAccessibility.Contracts;
using MonsterTrainAccessibility.Core.Graph;
using MonsterTrainAccessibility.Core.Strings;
using MonsterTrainAccessibility.Core.UI;
using MonsterTrainAccessibility.Module.Interop;
using MonsterTrainAccessibility.Module.UI;
using Screen = MonsterTrainAccessibility.Core.Screens.Screen;

namespace MonsterTrainAccessibility.Module.Screens
{
    /// <summary>
    /// The game's modal dialog (<see cref="global::DialogScreen"/>: Quit Game's and Erase all save
    /// data's confirmations, notices, Bind Keys' conflicts), its top dialog: its message spoken as it
    /// opens, and again as each dialog that follows it does (Bind Keys asks once per control a key is
    /// taken from); its buttons in the game's captions the only controls. Enter presses through the
    /// game; Escape closes it when the game lets a key close it. A tutorial waiting for the game's own
    /// arrow keys, with no buttons, hears the arrows.
    /// </summary>
    public sealed class DialogScreen : Screen
    {
        public override string Key => "game.dialog";
        public override int Layer => 30;
        public override bool Exclusive => true;

        private static Dialog Top
        {
            get
            {
                var screen = GameScreens.Active<global::DialogScreen>(global::ScreenName.Dialog);
                if (screen == null || !screen.TryPeekTopDialog(out Dialog dialog)) return null;
                return dialog != null && !dialog.IsClosed() && dialog.gameObject.activeInHierarchy ? dialog : null;
            }
        }

        // The dialog whose message was said, and that message as spoken: a dialog that follows (or the
        // same dialog shown again with another message) is said in turn. Compared without its markup,
        // since a tutorial's message reads with the game's <tutpress> tags on its first frame only.
        private Dialog _said;
        private string _saidText;

        public override bool IsActive() => Top != null;

        public override void OnPush()
        {
            _said = null;
            _saidText = null;
        }

        public override void Build(GraphBuilder b)
        {
            var dialog = Top;
            if (dialog == null) return;
            string message = dialog.contentLabel != null ? dialog.contentLabel.Content() : null;
            string spoken = message != null ? TextFilter.Clean(message) : null;
            if (!string.IsNullOrWhiteSpace(spoken) && (dialog != _said || spoken != _saidText))
            {
                // A dialog that follows keeps the buttons' places, so focus stays put: its button is
                // read again after the message, as the first dialog's landing is.
                bool follows = _said != null;
                _said = dialog;
                _saidText = spoken;
                // Basic events: the message goes to the events log, and is said unless turned off.
                if (Core.Events.GameEvents.Report(Core.Events.EventKinds.Basic, message)) ScreenIntro.Say(message);
                var focused = (Core.UI.Navigation.Active as GraphNavigator)?.FocusedNodeId;
                if (follows && focused != null) Core.UI.Navigation.FocusNode(focused);
            }
            b.PushContext(null, Strings.RoleDialog, positions: false);
            Choice(b, "button1", dialog.button1, dialog.button1Label);
            Choice(b, "button2", dialog.button2, dialog.button2Label);
            b.PopContext();
        }

        public override IEnumerable<ElementAction> GetActions()
        {
            var dialog = Top;
            if (dialog == null) yield break;
            if (WaitsForKeys(dialog))
            {
                yield return Forward(UiActions.Up, global::InputManager.Controls.Up);
                yield return Forward(UiActions.Down, global::InputManager.Controls.Down);
                yield return Forward(UiActions.Left, global::InputManager.Controls.Left);
                yield return Forward(UiActions.Right, global::InputManager.Controls.Right);
            }
            if (!dialog.AllowKeyToClose()) yield break;
            yield return new ElementAction(ActionIds.Back, Strings.Get("bind.ui.back"),
                _ => GameInput.Send(global::InputManager.Controls.Escape));
        }

        // A battle tutorial that waits for the game's own key with no button to press ("Press Down now
        // to try it": the focused card moved down, then up; a room-targeted card moved around): the
        // game hears no keys in focus mode, so the arrows go to the dialog as the game's own would.
        private static bool WaitsForKeys(Dialog dialog)
            => dialog.data != null && dialog.data.hideAllButtons && dialog.data.applyScreenInput != null;

        private static ElementAction Forward(string action, global::InputManager.Controls control)
            => new ElementAction(action, Strings.Get("bind." + action), _ => GameInput.Send(control));

        private static void Choice(GraphBuilder b, string key, ShinyShoe.GameUISelectableButton button, TMPro.TMP_Text caption)
        {
            if (button == null || !button.gameObject.activeInHierarchy) return;
            b.AddItem(ControlId.Structural("dialog:" + key), GameWidgets.Button(button, () => caption != null ? caption.Content() : null));
        }
    }
}
