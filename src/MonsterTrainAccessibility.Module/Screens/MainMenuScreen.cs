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
    /// The Covenant Outpost, the game's main menu (<see cref="global::MainMenuScreen"/>): its places as
    /// one list, play first (Depart, the Dimensional Portal, the Rift Station), then the Train Depot and
    /// the Library, the characters who have something to say when there are any, Settings last, and the
    /// account the game shows when the platform has one. Each place reads its caption, then "Locked" or
    /// "disabled" when the game says so, then the line the game shows on hover ("Start a Run"); the Rift
    /// Station adds the game's DLC note when the DLC is missing. Enter submits through the game's input
    /// signal, so the game's own handler runs; Escape is the game's Escape here, which opens Settings.
    /// Focus starts on Depart, and comes back to the place the player left from.
    /// </summary>
    public sealed class MainMenuScreen : Screen
    {
        public override string Key => "game.main_menu";

        // Back from a place (the Library, Settings) the player lands where they left, not on Depart.
        public override bool KeepStateOnPop => true;

        private static global::MainMenuScreen Game
        {
            get
            {
                var screen = GameScreens.Active<global::MainMenuScreen>(global::ScreenName.MainMenu);
                return screen != null && screen.IsInteractable() ? screen : null;
            }
        }

        public override bool IsActive() => Game != null;

        public override void Build(GraphBuilder b)
        {
            var screen = Game;
            if (screen == null) return;
            b.PushContext(GameText.Term("RichPresenceManager_DetailsDescriptionKey_NoRun"), Strings.RoleList);
            Place(b, "depart", screen.startRunButton);
            Place(b, "portal", screen.expertChallengesButton);
            Place(b, "rift", screen.soulSaviorButton, () => RiftNote(screen));
            Place(b, "depot", screen.trainCustomizationButton);
            Place(b, "library", screen.compendiumButton);
            Place(b, "dialogue1", screen.dialogueFirstButton);
            Place(b, "dialogue2", screen.dialogueSecondButton);
            Place(b, "dialogue3", screen.dialogueThirdButton);
            Place(b, "dialogue4", screen.dialogueFourthButton);
            Place(b, "settings", screen.settingsButton, label: () => GameText.Term("ScreenMainMenu_Settings"));
            Account(b, screen);
            b.PopContext();
            b.SetStart(ControlId.Structural("mainmenu:depart"));
        }

        public override IEnumerable<ElementAction> GetActions()
        {
            yield return new ElementAction(ActionIds.Back, GameText.Term("ScreenMainMenu_Settings"),
                _ => GameInput.Send(global::InputManager.Controls.Escape));
        }

        // A place on the menu: its caption (or the given label, for the captionless Settings), the
        // button's state, then its hover line and any note.
        private static void Place(GraphBuilder b, string key, MainMenuButton place,
            System.Func<string> note = null, System.Func<string> label = null)
        {
            var button = place != null ? place.Button : null;
            if (button == null || !button.gameObject.activeInHierarchy) return;
            b.AddItem(ControlId.Structural("mainmenu:" + key), GameWidgets.Button(button,
                label ?? (() => Caption(place)),
                () => Join(HoverLine(place), note?.Invoke())));
        }

        private static string Caption(MainMenuButton place)
            => place.label != null && !string.IsNullOrWhiteSpace(place.label.text) ? place.label.text : null;

        // Every other text the place carries: the line the game fades in on hover ("Start a Run").
        private static string HoverLine(MainMenuButton place)
        {
            foreach (var text in place.GetComponentsInChildren<TMP_Text>(true))
            {
                if (text == null || text == place.label || string.IsNullOrWhiteSpace(text.text)) continue;
                return text.text;
            }
            return null;
        }

        // The game's "Requires ... DLC" note, which it shows on hover over the Rift Station only while
        // the DLC is missing.
        private static string RiftNote(global::MainMenuScreen screen)
        {
            if (AppManager.PlatformServices.IsDlcInstalled(DLC.Railforged) || screen.dlcRequiredIndicator == null) return null;
            var text = screen.dlcRequiredIndicator.GetComponentInChildren<TMP_Text>(true);
            return text != null && !string.IsNullOrWhiteSpace(text.text) ? text.text : null;
        }

        // The signed-in account, where the platform has one: a button when the player may switch it,
        // else the name alone.
        private static void Account(GraphBuilder b, global::MainMenuScreen screen)
        {
            var change = screen.changeUserButton;
            if (change != null && change.gameObject.activeInHierarchy)
                b.AddItem(ControlId.Structural("mainmenu:user"), GameWidgets.Button(change, () => screen.usernameLabel != null ? screen.usernameLabel.text : null));
            var fixedUser = screen.nonSwitchableUserButton;
            if (fixedUser != null && fixedUser.gameObject.activeInHierarchy)
                b.AddItem(ControlId.Structural("mainmenu:user"), GameNodes.Text(() => screen.nonSwitchableUsernameLabel != null ? screen.nonSwitchableUsernameLabel.text : null));
        }

        private static string Join(string first, string second)
        {
            if (string.IsNullOrWhiteSpace(first)) return second;
            if (string.IsNullOrWhiteSpace(second)) return first;
            return first + ". " + second;
        }
    }
}
