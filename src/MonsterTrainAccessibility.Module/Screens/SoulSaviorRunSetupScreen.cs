using System.Collections.Generic;
using MonsterTrainAccessibility.Core.Graph;
using MonsterTrainAccessibility.Core.UI;
using MonsterTrainAccessibility.Module.Interop;
using MonsterTrainAccessibility.Module.Nodes;
using MonsterTrainAccessibility.Module.UI;
using Screen = MonsterTrainAccessibility.Core.Screens.Screen;

namespace MonsterTrainAccessibility.Module.Screens
{
    /// <summary>
    /// Soul Savior's run setup (<see cref="global::SoulSaviorRunSetupScreen"/>), laid out as the base one
    /// is (<see cref="RunSetupNodes.Layout"/>: the clans with their champions and starter cards, the pyre
    /// heart, Depart, Settings, Back), with its own parts between: the difficulty ("Difficulty, button,
    /// Bloom", what it means, Enter opening <see cref="RunSetupDifficultyPickerScreen"/>) and the win
    /// streak; then the souls ("Souls, button", the chosen souls by name, or the game's "Randomize Souls"
    /// while they are left to chance; each chosen one's description a buffer line; Enter opening
    /// <see cref="SoulSelectionScreen"/>). Escape is the game's: it opens Settings.
    /// </summary>
    public sealed class SoulSaviorRunSetupScreen : Screen
    {
        public override string Key => "game.soulsavior.run_setup";
        public override bool KeepStateOnPop => true;

        public override bool IsActive() => RunSetupNodes.SoulSavior != null;

        public override void Build(GraphBuilder b)
        {
            var screen = RunSetupNodes.SoulSavior;
            if (screen == null || screen.mainClassInfo == null) return;
            RunSetupNodes.Layout(b, new RunSetupNodes.Parts
            {
                Main = screen.mainClassInfo,
                Sub = screen.subClassInfo,
                PyreHeart = screen.pyreHeartInfo,
                PyreHeartButton = screen.pyreHeartButton,
                Mode = sb =>
                {
                    Difficulty(sb, screen);
                    RunSetupNodes.WinStreak(sb, "streak", screen.winStreakUI);
                },
                Extras = sb => Souls(sb, screen),
                Start = screen.startButton,
                Settings = screen.settingsButton,
                Back = screen.backButton,
            });
        }

        public override IEnumerable<ElementAction> GetActions()
        {
            yield return new ElementAction(ActionIds.Back, GameText.Term("ScreenMainMenu_Settings"),
                _ => GameInput.Send(global::InputManager.Controls.Escape));
        }

        // The difficulty: its header and the tier's name, then what its tooltip says; the tooltips in the buffer.
        private static void Difficulty(GraphBuilder b, global::SoulSaviorRunSetupScreen screen)
        {
            var info = screen.difficultyTierInfo;
            var button = screen.difficultyTierButton;
            if (info == null || button == null || !button.gameObject.activeInHierarchy) return;
            var vt = GameWidgets.Button(button, () => ChallengeProgressScreen.HeaderOf(info, "Text Difficulty Tier header"),
                () => RunSetupNodes.TooltipBody(button),
                new NodeAnnouncement(() => RunSetupNodes.Text(info.difficultyLabel), kind: AnnouncementKinds.Value));
            vt.Details = () => GameNodes.Lines(TooltipReader.Lines(button));
            b.AddItem(ControlId.Structural("setup:difficulty"), vt);
        }

        // The souls, one button over the row in the game: the chosen ones' names (the game's preview list,
        // filled for hovering the row), or "Randomize Souls" while they are left to chance.
        private static void Souls(GraphBuilder b, global::SoulSaviorRunSetupScreen screen)
        {
            var edit = screen.editSoulsButton;
            if (edit == null || !edit.gameObject.activeInHierarchy) return;
            // The "Souls" header sits beside the button, not on it.
            var vt = GameWidgets.Button(edit, () => StartingConditionsNodes.HeaderOf(edit.transform.parent), null,
                new NodeAnnouncement(() =>
                {
                    if (screen.soulIds.Contains("random")) return GameText.Term("ScreenSoulSelection_RandomizeSouls");
                    var names = new List<string>();
                    foreach (var item in Chosen(screen)) names.Add(item.nameLabel.Content());
                    return names.Count > 0 ? string.Join(", ", names) : null;
                }, kind: AnnouncementKinds.Value));
            vt.Details = () =>
            {
                var lines = new List<string>();
                if (screen.soulIds.Contains("random")) return lines;
                foreach (var item in Chosen(screen))
                {
                    string description = item.descriptionLabel != null ? item.descriptionLabel.Content() : null;
                    lines.Add(string.IsNullOrWhiteSpace(description) ? item.nameLabel.Content() : item.nameLabel.Content() + ": " + description);
                }
                return lines;
            };
            b.AddItem(ControlId.Structural("setup:souls"), vt);
        }

        private static IEnumerable<SoulPreviewItemUI> Chosen(global::SoulSaviorRunSetupScreen screen)
        {
            if (screen.soulPreviewListRoot == null) yield break;
            foreach (var item in screen.soulPreviewListRoot.GetComponentsInChildren<SoulPreviewItemUI>(true))
                if (item.gameObject.activeSelf && item.nameLabel != null && !string.IsNullOrWhiteSpace(item.nameLabel.Content())) yield return item;
        }
    }
}
