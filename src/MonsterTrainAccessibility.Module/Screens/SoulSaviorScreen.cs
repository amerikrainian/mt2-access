using System.Collections.Generic;
using MonsterTrainAccessibility.Core.Graph;
using MonsterTrainAccessibility.Core.Strings;
using MonsterTrainAccessibility.Core.UI;
using MonsterTrainAccessibility.Module.Interop;
using MonsterTrainAccessibility.Module.Nodes;
using MonsterTrainAccessibility.Module.UI;
using Screen = MonsterTrainAccessibility.Core.Screens.Screen;

namespace MonsterTrainAccessibility.Module.Screens
{
    /// <summary>
    /// The Rift Station, Soul Savior's hub (<see cref="global::SoulSaviorScreen"/>): its places as one
    /// list, as the Covenant Outpost's are (<see cref="MenuNodes"/>), play first: Depart ("Start a Soul
    /// Savior Run"), the Soulforge, back to the Covenant Outpost, the characters who have something to
    /// say when there are any, Settings, the account the game shows when the platform has one; then the
    /// progress panel ("Next Dialogue. Unlock All Souls. 11/33", <see cref="ProgressionReader"/>) while
    /// the game shows it. Enter submits through the game's input signal: Depart opens the run dialog over
    /// a saved run (<see cref="SoulSaviorRunOptionsDialog"/>), else starts one. Escape is the game's
    /// Escape here, which opens Settings. Focus starts on Depart, and comes back to the place the player
    /// left from.
    /// </summary>
    public sealed class SoulSaviorScreen : Screen
    {
        public override string Key => "game.soulsavior";

        // Back from a place (the Soulforge, Settings) the player lands where they left, not on Depart.
        public override bool KeepStateOnPop => true;

        private static global::SoulSaviorScreen Game
        {
            get
            {
                var screen = GameScreens.Active<global::SoulSaviorScreen>(global::ScreenName.SoulSavior);
                return screen != null && screen.IsInteractable() ? screen : null;
            }
        }

        public override bool IsActive() => Game != null;

        public override void Build(GraphBuilder b)
        {
            var screen = Game;
            if (screen == null) return;
            b.PushContext(GameText.Term("ScreenMainMenu_TheRiftStation"), Strings.RoleList);
            MenuNodes.Place(b, "soulsavior:depart", screen.startSoulSaviorRunButton);
            MenuNodes.Place(b, "soulsavior:soulforge", screen.soulforgeButton);
            MenuNodes.Place(b, "soulsavior:outpost", screen.mainMenuButton);
            MenuNodes.Place(b, "soulsavior:dialogue1", screen.dialogueFirstButton);
            MenuNodes.Place(b, "soulsavior:dialogue2", screen.dialogueSecondButton);
            MenuNodes.Place(b, "soulsavior:settings", screen.settingsButton, label: () => GameText.Term("ScreenMainMenu_Settings"));
            MenuNodes.Account(b, "soulsavior:user", screen.changeUserButton, screen.usernameLabel, screen.nonSwitchableUserButton, screen.nonSwitchableUsernameLabel);
            var progress = screen.progressionObjectiveUI;
            if (progress != null && progress.gameObject.activeInHierarchy && ProgressionReader.Line(progress) != null)
                b.AddItem(ControlId.Structural("soulsavior:progress"), GameNodes.Text(() => ProgressionReader.Line(progress)));
            b.PopContext();
            b.SetStart(ControlId.Structural("soulsavior:depart"));
        }

        public override IEnumerable<ElementAction> GetActions()
        {
            yield return new ElementAction(ActionIds.Back, GameText.Term("ScreenMainMenu_Settings"),
                _ => GameInput.Send(global::InputManager.Controls.Escape));
        }
    }
}
