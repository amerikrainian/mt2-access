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
    /// The Covenant Outpost, the game's main menu (<see cref="global::MainMenuScreen"/>): its places as
    /// one list, play first (Depart, the Dimensional Portal, the Rift Station), then the Train Depot and
    /// the Library, the characters who have something to say when there are any, Settings, the account
    /// the game shows when the platform has one, and the story's progress panel (the next reward's
    /// objective and count) while the game shows it: only while a next reward remains. Each place reads its caption, then "Locked" or
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
            MenuNodes.Place(b, "mainmenu:depart", screen.startRunButton);
            MenuNodes.Place(b, "mainmenu:portal", screen.expertChallengesButton);
            MenuNodes.Place(b, "mainmenu:rift", screen.soulSaviorButton, () => RiftNote(screen));
            MenuNodes.Place(b, "mainmenu:depot", screen.trainCustomizationButton);
            MenuNodes.Place(b, "mainmenu:library", screen.compendiumButton);
            MenuNodes.Place(b, "mainmenu:dialogue1", screen.dialogueFirstButton);
            MenuNodes.Place(b, "mainmenu:dialogue2", screen.dialogueSecondButton);
            MenuNodes.Place(b, "mainmenu:dialogue3", screen.dialogueThirdButton);
            MenuNodes.Place(b, "mainmenu:dialogue4", screen.dialogueFourthButton);
            MenuNodes.Place(b, "mainmenu:settings", screen.settingsButton, label: () => GameText.Term("ScreenMainMenu_Settings"));
            MenuNodes.Account(b, "mainmenu:user", screen.changeUserButton, screen.usernameLabel, screen.nonSwitchableUserButton, screen.nonSwitchableUsernameLabel);
            MenuNodes.Progress(b, "mainmenu:progress", screen.progressionObjectiveUI);
            b.PopContext();
            b.SetStart(ControlId.Structural("mainmenu:depart"));
        }

        public override IEnumerable<ElementAction> GetActions()
        {
            yield return new ElementAction(ActionIds.Back, GameText.Term("ScreenMainMenu_Settings"),
                _ => GameInput.Send(global::InputManager.Controls.Escape));
        }

        // The game's "Requires ... DLC" note, which it shows on hover over the Rift Station only while
        // the DLC is missing.
        private static string RiftNote(global::MainMenuScreen screen)
        {
            if (AppManager.PlatformServices.IsDlcInstalled(DLC.Railforged) || screen.dlcRequiredIndicator == null) return null;
            var text = screen.dlcRequiredIndicator.GetComponentInChildren<TMP_Text>(true);
            return text != null && !string.IsNullOrWhiteSpace(text.Content()) ? text.Content() : null;
        }
    }
}
