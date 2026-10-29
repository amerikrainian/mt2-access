using System.Collections.Generic;
using MonsterTrainAccessibility.Core.Graph;
using MonsterTrainAccessibility.Core.Strings;
using MonsterTrainAccessibility.Core.UI;
using MonsterTrainAccessibility.Module.Interop;
using MonsterTrainAccessibility.Module.UI;
using ShinyShoe;
using TMPro;
using Screen = MonsterTrainAccessibility.Core.Screens.Screen;

namespace MonsterTrainAccessibility.Module.Screens
{
    /// <summary>
    /// The game's "Menu" (<see cref="PauseDialog"/> on the settings screen), which Escape opens over the
    /// Covenant Outpost and over a run: its buttons as one list in the game's order (in a run the run's
    /// own options come first: Abandon Run, and in a battle its restarts), then the build number. Focus
    /// lands on the game's own default, Settings. Enter presses through the game; Escape closes it as the
    /// game's Escape does. The Settings button opens <see cref="SettingsScreen"/> over it.
    /// </summary>
    public sealed class PauseMenuScreen : Screen
    {
        public override string Key => "game.pause_menu";
        public override int Layer => 20;
        public override bool Exclusive => true;

        private static PauseDialog Dialog
        {
            get
            {
                var screen = GameScreens.Active<global::SettingsScreen>(global::ScreenName.Settings);
                if (screen == null || screen.pauseDialog == null || !screen.pauseDialog.Active) return null;
                return screen.settingsDialog != null && screen.settingsDialog.Active ? null : screen.pauseDialog;
            }
        }

        public override bool IsActive() => Dialog != null;

        public override void Build(GraphBuilder b)
        {
            var dialog = Dialog;
            if (dialog == null) return;
            b.PushContext(DialogTitle(dialog), Strings.RoleList);
            var ids = new HashSet<string>();
            foreach (var button in dialog.GetComponentsInChildren<GameUISelectableButton>(false))
            {
                if (button == dialog._closeButton) continue;
                var b2 = button;
                if (b2.GetComponent<UrlButton>() != null)
                {
                    var link = GameWidgets.Button(b2, () => Strings.PauseDiscord);
                    link.ControlType = ControlTypes.Link;
                    b.AddItem(Id(ids, b2), link);
                    continue;
                }
                if (Caption(b2) == null) continue;
                var id = Id(ids, b2);
                b.AddItem(id, GameWidgets.Button(b2, () => Caption(b2)));
                // Focus lands where the game's own selection does (Settings), never on a run's Abandon Run.
                if (ReferenceEquals(b2, dialog.GetDefaultGameUISelectable())) b.SetStart(id);
            }
            var build = dialog.GetComponentInChildren<BuildNumberLabel>(false);
            var buildText = build != null ? build.GetComponent<TMP_Text>() : null;
            if (buildText != null && !string.IsNullOrWhiteSpace(buildText.text))
                b.AddItem(ControlId.Structural("pause:build"), GameNodes.Text(() => buildText.text));
            b.PopContext();
        }

        public override IEnumerable<ElementAction> GetActions()
        {
            yield return new ElementAction(ActionIds.Back, Strings.Get("bind.ui.back"),
                _ => GameInput.Send(global::InputManager.Controls.Escape));
        }

        // A button's id by its object name, numbered only should two share one.
        private static ControlId Id(HashSet<string> ids, UnityEngine.Component button)
        {
            string key = "pause:" + button.gameObject.name;
            for (int n = 2; !ids.Add(key); n++) key = "pause:" + button.gameObject.name + "#" + n;
            return ControlId.Structural(key);
        }

        /// <summary>A dialog's heading: its "Title" text (the game's dialogs carry no field for it).</summary>
        internal static string DialogTitle(UnityEngine.Component dialog)
        {
            foreach (var text in dialog.GetComponentsInChildren<TMP_Text>(false))
                if (text.gameObject.name == "Title" && !string.IsNullOrWhiteSpace(text.text)) return text.text;
            return null;
        }

        /// <summary>A button's caption: its first text that is not a key hint.</summary>
        internal static string Caption(UnityEngine.Component button)
        {
            foreach (var text in button.GetComponentsInChildren<TMP_Text>(false))
            {
                if (string.IsNullOrWhiteSpace(text.text) || text.GetComponentInParent<GameUIControlMapping>() != null) continue;
                return text.text;
            }
            return null;
        }
    }
}
