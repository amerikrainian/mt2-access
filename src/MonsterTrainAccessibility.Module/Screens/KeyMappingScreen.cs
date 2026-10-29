using System.Collections.Generic;
using MonsterTrainAccessibility.Core.Graph;
using MonsterTrainAccessibility.Core.Strings;
using MonsterTrainAccessibility.Core.UI;
using MonsterTrainAccessibility.Module.Interop;
using MonsterTrainAccessibility.Module.UI;
using ShinyShoe;
using Screen = MonsterTrainAccessibility.Core.Screens.Screen;

namespace MonsterTrainAccessibility.Module.Screens
{
    /// <summary>
    /// The game's Bind Keys (<see cref="global::KeyMappingScreen"/>, from Settings' Gameplay tab):
    /// opening it says its title; then one list of the game's rows, one per key bound to a control, each
    /// a row under the control's name ("Select", said on coming to the control): the key, the row's Bind
    /// button ("E, button"; "&lt;Unset&gt;" for none; Enter rebinds it), Add, Remove (which the game turns off for an
    /// unset key); then Reset. Bind and Add wait for the key to bind, said as
    /// they do ("Press a key."): the game reads it itself, the mod's keys standing down until it is
    /// released (<see cref="KeyCapture"/>). The game then rebuilds its rows, a control's keys in a new
    /// order, so focus goes to the row of the key just bound and reads it. A key bound elsewhere asks
    /// first, in the game's dialog. Escape is the game's: it closes the screen (and, while it waits, is
    /// the key it binds, as in the game).
    /// </summary>
    public sealed class KeyMappingScreen : Screen
    {
        public override string Key => "game.key_mapping";
        public override int Layer => 25;
        public override bool Exclusive => true;

        private static global::KeyMappingScreen Game => GameScreens.Active<global::KeyMappingScreen>(global::ScreenName.KeyMapping);

        // A bind under way: the control it binds for and the keys the control had before, to find the
        // row of the key it adds once the game has rebuilt its rows; given up after a while.
        private string _binding;
        private HashSet<string> _keysBefore;
        private int _bindingUntil;
        private const int BindingWaitFrames = 60 * 60;

        public override bool IsActive() => Game != null;

        public override void OnPush() => ScreenIntro.Say(GameText.Term("ScreenKeyMapping_Title"));

        public override void Build(GraphBuilder b)
        {
            var screen = Game;
            if (screen == null || screen.rows == null) return;
            b.PushContext(null);
            for (int i = 0; i < screen.rows.Count; i++)
            {
                var row = screen.rows[i];
                if (row == null || !row.gameObject.activeInHierarchy || row.keyMappingData == null) continue;
                var shown = row;
                b.PushContext(row.controlLabel != null ? row.controlLabel.Content() : null);
                b.StartRow();
                // The row's Bind by the key it binds: Enter rebinds that key.
                var bind = GameWidgets.Button(row.rebindButton, () => shown.keyLabel != null ? shown.keyLabel.Content() : null);
                bind.OnActivate = () => Listen(shown, shown.rebindButton);
                var bindId = ControlId.Structural("keys:" + i + ":bind");
                b.AddItem(bindId, bind);
                var add = GameWidgets.Button(row.addButton, () => GameText.Term("ScreenKeyMapping_Add"));
                add.OnActivate = () => Listen(shown, shown.addButton);
                b.AddItem(ControlId.Structural("keys:" + i + ":add"), add);
                b.AddItem(ControlId.Structural("keys:" + i + ":remove"), GameWidgets.Button(row.deleteButton, () => GameText.Term("ScreenKeyMapping_Remove")));
                b.EndRow();
                b.PopContext();
                if (Bound(row)) Core.UI.Navigation.FocusNode(bindId);
            }
            if (_binding != null && !KeyCapture.Listening && UnityEngine.Time.frameCount > _bindingUntil) _binding = null;
            var reset = screen.resetButton;
            if (reset != null && reset.gameObject.activeInHierarchy)
                b.AddItem(ControlId.Structural("keys:reset"), GameWidgets.Button(reset, () => PauseMenuScreen.Caption(reset)));
            b.PopContext();
        }

        public override IEnumerable<ElementAction> GetActions()
        {
            yield return new ElementAction(ActionIds.Back, Strings.Get("bind.ui.back"),
                _ => GameInput.Send(global::InputManager.Controls.Escape));
        }

        // Bind or Add, a frame late: the game takes the first key that goes down once it waits, and the
        // Enter that pressed the button went down this frame.
        private void Listen(KeyMappingRow row, GameUISelectableButton button)
        {
            if (button == null || !button.interactable || row.keyMappingData == null) return;
            var mapping = row.keyMappingData.mapping;
            Later.Frames(1, () =>
            {
                GameInput.Submit(button);
                if (!KeyCapture.Listening) return;
                _binding = mapping.mappingDefName;
                _keysBefore = new HashSet<string>(mapping.GetKeys());
                _bindingUntil = UnityEngine.Time.frameCount + BindingWaitFrames;
                Core.Speech.Say(Strings.KeysPress, interrupt: true);
            }, "key mapping listen");
        }

        // The row of the key the bind under way added, once the game shows it: the bind is done.
        private bool Bound(KeyMappingRow row)
        {
            if (_binding == null || KeyCapture.Listening) return false;
            var data = row.keyMappingData;
            if (data.mapping == null || data.keyDef == null || data.mapping.mappingDefName != _binding || _keysBefore.Contains(data.keyDef.name)) return false;
            _binding = null;
            return true;
        }
    }
}
