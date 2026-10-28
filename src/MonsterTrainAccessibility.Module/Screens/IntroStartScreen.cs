using System.Collections.Generic;
using MonsterTrainAccessibility.Core.Graph;
using MonsterTrainAccessibility.Core.UI;
using MonsterTrainAccessibility.Module.Interop;
using MonsterTrainAccessibility.Module.UI;
using ShinyShoe;
using TMPro;
using Screen = MonsterTrainAccessibility.Core.Screens.Screen;

namespace MonsterTrainAccessibility.Module.Screens
{
    /// <summary>
    /// The title screen's "Press any key to Start" (<see cref="global::IntroStartScreen"/>). The game
    /// starts on any key itself: its <c>LateUpdate</c> reads Unity's input directly
    /// (<c>CoreInput.IsAnyNonJoystickKeyDown</c>), past the keyboard driver focus mode blanks, so every
    /// key reaches it. The type-ahead stands down here so a letter is not taken for a search. The
    /// prompt is one control: Enter on it starts the game only when the game's own any-key check will
    /// not (a pad's A, which that check ignores and whose driver focus mode blanks; the dev driver's
    /// confirm), so a key press never starts it twice.
    /// </summary>
    public sealed class IntroStartScreen : Screen
    {
        public override string Key => "game.intro_start";
        public override bool AllowsTypeahead => false;

        private static global::IntroStartScreen Game => GameScreens.Active<global::IntroStartScreen>(global::ScreenName.IntroStart);

        public override bool IsActive() => Game != null;

        public override void Build(GraphBuilder b)
        {
            var screen = Game;
            if (screen == null) return;
            b.PushContext(null, null, positions: false);
            b.AddItem(ControlId.Structural("intro:start"), new NodeVtable
            {
                ControlType = ControlTypes.Button,
                Announcements = new List<NodeAnnouncement> { GameNodes.LabelPart(() => Prompt(screen)) },
                OnActivate = () => Start(screen),
            });
            b.PopContext();
        }

        // The prompt the game shows for the current input mode and login state: one of its four label
        // roots is active at a time.
        private static string Prompt(global::IntroStartScreen screen)
        {
            foreach (var root in new[] { screen._labelKeyboardMouse, screen._labelKeyboardMouseLoggedOut, screen._labelGamepad, screen._labelGamepadLoggedOut })
            {
                if (root == null || !root.gameObject.activeInHierarchy) continue;
                var text = root.GetComponentInChildren<TMP_Text>(false);
                if (text != null && !string.IsNullOrWhiteSpace(text.text)) return text.text;
            }
            return null;
        }

        // The game's LateUpdate starts on this frame's key already when it allows no user switching and
        // a non-joystick key went down in keyboard or mouse mode: its own condition, asked of it.
        private static void Start(global::IntroStartScreen screen)
        {
            if (!AppManager.PlatformServices.SupportsFeature(PlatformFeature.AllowUserSwitching) && screen.IsAnyKeyDown()) return;
            screen.DoMainAction();
        }
    }
}
