using System.Collections.Generic;
using MonsterTrainAccessibility.Core.Graph;
using MonsterTrainAccessibility.Core.Strings;
using MonsterTrainAccessibility.Core.UI;
using MonsterTrainAccessibility.Module.Interop;
using MonsterTrainAccessibility.Module.Nodes;
using MonsterTrainAccessibility.Module.UI;
using TMPro;
using Screen = MonsterTrainAccessibility.Core.Screens.Screen;

namespace MonsterTrainAccessibility.Module.Screens
{
    /// <summary>
    /// The run setup (<see cref="global::RunSetupScreen"/>), laid out as Soul Savior's is
    /// (<see cref="RunSetupNodes.Layout"/>: the clans with their champions and starter cards, the pyre
    /// heart, Depart, Settings, Back), with its own parts between: the Covenant rank as a slider stepped
    /// through the game's level buttons; the clan pair's best rank or the game's no-progression note;
    /// the win streaks; the chosen mutators (Enter opens <see cref="RunSetupMutatorsScreen"/>); Add or
    /// Remove Mutators. Escape is the game's: it opens Settings, as on the Covenant Outpost.
    /// </summary>
    public sealed class RunSetupScreen : Screen
    {
        public override string Key => "game.run_setup";
        public override bool KeepStateOnPop => true;

        internal static global::RunSetupScreen Game => RunSetupNodes.Base;

        public override bool IsActive() => Game != null;

        public override void Build(GraphBuilder b)
        {
            var screen = Game;
            if (screen == null || screen.mainClassInfo == null) return;
            RunSetupNodes.Layout(b, new RunSetupNodes.Parts
            {
                Main = screen.mainClassInfo,
                Sub = screen.subClassInfo,
                PyreHeart = screen.pyreHeartInfo,
                PyreHeartButton = screen.pyreHeartButton,
                Mode = sb => Covenant(sb, screen),
                Extras = sb =>
                {
                    Mutators(sb, screen);
                    RunSetupNodes.Button(sb, "mutators", screen.toggleEnableMutatorsButton, () => RunSetupNodes.Text(screen.toggleEnableMutatorsButtonLabel));
                },
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

        // The Covenant rank slider, then what the game shows under it: the clan pair's best rank (or,
        // with mutators on, its note that the run won't count) and the win streaks.
        private static void Covenant(GraphBuilder b, global::RunSetupScreen screen)
        {
            var selector = screen.covenantSelectionUI;
            if (selector == null || !selector.gameObject.activeInHierarchy) return;
            b.AddItem(ControlId.Structural("setup:covenant"), StartingConditionsNodes.CovenantSelector(selector,
                () => ChallengeProgressScreen.HeaderOf(selector.transform.parent, "Text Covenant header")));
            var rank = screen.clanCovenantRankUI;
            if (rank != null && rank.gameObject.activeInHierarchy && rank.contentRoot != null && rank.contentRoot.activeSelf)
                b.AddItem(ControlId.Structural("setup:pairrank"), StartingConditionsNodes.ClanPairRank(rank,
                    () => screen.mainClassInfo.ClassData, () => screen.subClassInfo.ClassData));
            if (screen.covenantDisabledRoot != null && screen.covenantDisabledRoot.activeInHierarchy)
            {
                int i = 0;
                foreach (var text in screen.covenantDisabledRoot.GetComponentsInChildren<TMP_Text>(false))
                {
                    if (string.IsNullOrWhiteSpace(text.Content())) continue;
                    var shown = text;
                    b.AddItem(ControlId.Structural("setup:noprogress:" + i++), GameNodes.Text(() => shown.Content()));
                }
            }
            RunSetupNodes.WinStreak(b, "streak", screen.winStreakUI);
            RunSetupNodes.WinStreak(b, "tfbstreak", screen.trueFinalBossWinStreakUI);
        }

        // The mutator row, one button over the whole row in the game: "Mutators, button, A Simple Plan,
        // Acid Rain", Enter opening the picker. Each chosen one's description is a buffer line, from the
        // preview the game keeps filled for hovering the row (the row's icons carry no tooltips here).
        private static void Mutators(GraphBuilder b, global::RunSetupScreen screen)
        {
            var edit = screen.editMutatorsButton;
            if (screen.mutatorOptionsParent == null || !screen.mutatorOptionsParent.gameObject.activeInHierarchy
                || edit == null || !edit.gameObject.activeInHierarchy) return;
            var vt = GameWidgets.Button(edit, () => StartingConditionsNodes.HeaderOf(screen.mutatorOptionsParent), null,
                new NodeAnnouncement(() =>
                {
                    var names = new List<string>();
                    foreach (var item in Chosen(screen)) names.Add(item.nameLabel.Content());
                    return names.Count > 0 ? string.Join(", ", names) : null;
                }, kind: AnnouncementKinds.Value));
            vt.Details = () =>
            {
                var lines = new List<string>();
                foreach (var item in Chosen(screen))
                {
                    string description = item.descriptionLabel != null ? item.descriptionLabel.Content() : null;
                    lines.Add(string.IsNullOrWhiteSpace(description) ? item.nameLabel.Content() : item.nameLabel.Content() + ": " + description);
                }
                return lines;
            };
            b.AddItem(ControlId.Structural("setup:editmutators"), vt);
        }

        // The chosen mutators' preview entries, in order.
        private static IEnumerable<MutatorPreviewItemUI> Chosen(global::RunSetupScreen screen)
        {
            if (screen.mutatorPreviewListRoot == null) yield break;
            foreach (var item in screen.mutatorPreviewListRoot.GetComponentsInChildren<MutatorPreviewItemUI>(true))
                if (item.gameObject.activeSelf && item.nameLabel != null && !string.IsNullOrWhiteSpace(item.nameLabel.Content())) yield return item;
        }
    }
}
