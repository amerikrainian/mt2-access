using System.Collections.Generic;
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
    /// data's confirmations, notices), its top dialog read as Guildrun reads one: the message as a line,
    /// landed on first, then the dialog's buttons in the game's captions. Enter presses through the
    /// game; Escape closes it when the game lets a key close it.
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

        public override bool IsActive() => Top != null;

        public override void Build(GraphBuilder b)
        {
            var dialog = Top;
            if (dialog == null) return;
            b.PushContext(null, Strings.RoleDialog, positions: false);
            if (dialog.contentLabel != null && !string.IsNullOrWhiteSpace(dialog.contentLabel.text))
                b.AddItem(ControlId.Structural("dialog:message"), GameNodes.Text(() => dialog.contentLabel.text));
            Choice(b, "button1", dialog.button1, dialog.button1Label);
            Choice(b, "button2", dialog.button2, dialog.button2Label);
            b.PopContext();
        }

        public override IEnumerable<ElementAction> GetActions()
        {
            var dialog = Top;
            if (dialog == null || !dialog.AllowKeyToClose()) yield break;
            yield return new ElementAction(ActionIds.Back, Strings.Get("bind.ui.back"),
                _ => GameInput.Send(global::InputManager.Controls.Escape));
        }

        private static void Choice(GraphBuilder b, string key, ShinyShoe.GameUISelectableButton button, TMPro.TMP_Text caption)
        {
            if (button == null || !button.gameObject.activeInHierarchy) return;
            b.AddItem(ControlId.Structural("dialog:" + key), GameWidgets.Button(button, () => caption != null ? caption.text : null));
        }
    }
}
