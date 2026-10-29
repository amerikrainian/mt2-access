using System.Collections.Generic;
using MonsterTrainAccessibility.Core;
using MonsterTrainAccessibility.Core.Graph;
using MonsterTrainAccessibility.Core.Input;
using MonsterTrainAccessibility.Core.Strings;
using MonsterTrainAccessibility.Core.UI;
using MonsterTrainAccessibility.Module.Interop;
using MonsterTrainAccessibility.Module.Nodes;
using MonsterTrainAccessibility.Module.UI;
using ShinyShoe;
using TMPro;
using UnityEngine;
using Screen = MonsterTrainAccessibility.Core.Screens.Screen;

namespace MonsterTrainAccessibility.Module.Screens
{
    /// <summary>
    /// The run history (<see cref="global::RunHistoryScreen"/>, from the menu): the game's Sort choice,
    /// then the page's runs one under another (<see cref="Run"/>), then the page buttons
    /// (<see cref="PaginationNodes"/>; a new page says itself once the game has it), then Back. Enter on
    /// a run opens its summary through the game; the secondary action favorites it or takes the favorite
    /// back, and says which. Nothing is listed while the game fetches a page.
    /// </summary>
    public sealed class RunHistoryScreen : Screen
    {
        private static readonly InputCategory[] Categories = { InputCategory.UI };

        public override string Key => "game.run_history";
        public override int Layer => 6;
        public override bool Exclusive => true;
        public override IReadOnlyList<InputCategory> InputCategories => Categories;

        private string _page;

        private static global::RunHistoryScreen Game
        {
            get
            {
                var screen = GameScreens.Active<global::RunHistoryScreen>(global::ScreenName.RunHistory);
                return screen != null && screen.IsInteractable() ? screen : null;
            }
        }

        public override bool IsActive() => Game != null;

        public override void OnPush()
        {
            _page = null;
            var screen = Game;
            if (screen != null) ScreenIntro.Say(ChallengeProgressScreen.HeaderOf(screen, "Title label"));
        }

        public override void Build(GraphBuilder b)
        {
            var screen = Game;
            var ui = screen != null ? screen.runHistoryUI : null;
            if (ui == null) return;
            b.PushContext(null);
            var sort = ui.sortDropdown;
            if (sort != null && sort.gameObject.activeInHierarchy)
                b.AddItem(ControlId.Structural("history:sort"), GameWidgets.Dropdown(sort, () => FilterNodes.Label(sort.transform.parent)));
            if (!ui.fetchingGameRuns)
            {
                // A new page said once the game has it.
                string page = PaginationNodes.Page(ui.paginationControls);
                if (page != null && page != _page)
                {
                    if (_page != null) Speech.Say(page);
                    _page = page;
                }
                ControlId first = null;
                var header = ui.rowsRoot != null ? ui.rowsRoot.transform.Find("Header row") : null;
                if (ui.currentRows != null)
                    foreach (var row in ui.currentRows)
                    {
                        if (row == null || !row.gameObject.activeInHierarchy || row.GetRunData() == null) continue;
                        var id = ControlId.Structural("history:run:" + row.GetRunData().GetID());
                        b.AddItem(id, Run(row, header));
                        first = first ?? id;
                    }
                PaginationNodes.Add(b, "history:pages", ui.paginationControls);
                if (first != null) b.SetStart(first);
            }
            var back = screen.backButton;
            if (back != null && back.gameObject.activeInHierarchy)
                b.AddItem(ControlId.Structural("history:back"), GameWidgets.Button(back, () => PauseMenuScreen.Caption(back)));
            b.PopContext();
        }

        public override IEnumerable<ElementAction> GetActions()
        {
            yield return new ElementAction(ActionIds.Back, Strings.Get("bind.ui.back"),
                _ => GameInput.Send(global::InputManager.Controls.Escape));
        }

        /// <summary>"2026-10-28 23:34:20, Standard Run, Banished, Pyreborne, Covenant Rank 5, Current
        /// Ring: The Observatory, Score: 16,210, Victory, clean run, favorited": the run as its row draws it, the covenant
        /// (or a Soul Savior run's difficulty) and the distance by their tooltips' titles, the score
        /// under its column's caption, the marks' words; those tooltips in the buffer.</summary>
        private static NodeVtable Run(RunHistoryEntryUI row, Transform header)
        {
            System.Func<string> label = () => Join(
                Shown(row.dateLabel),
                Shown(row.runTypeLabel),
                Visible(row.mainClanUI) ? Shown(row.mainClanUI.GetComponentInChildren<TMP_Text>(false)) : null,
                Visible(row.subClanUI) ? Shown(row.subClanUI.GetComponentInChildren<TMP_Text>(false)) : null,
                Visible(row.covenantUI) ? StartingConditionsNodes.CovenantTitle(row.covenantUI, null) : null,
                Visible(row.difficultyTierUI) ? Difficulty(row.difficultyTierUI).title : null,
                Visible(row.distanceUI) ? Distance(row).title : null,
                Score(row, header),
                Visible(row.victoryIcon) ? Strings.Get("run.victory") : null,
                Visible(row.cleanRunIcon) ? Strings.Get("run.clean") : null);
            var info = row.infoButton;
            return new NodeVtable
            {
                ControlType = ControlTypes.Button,
                Announcements = new List<NodeAnnouncement>
                {
                    GameNodes.LabelPart(label),
                    new NodeAnnouncement(() => Starred(row) ? Strings.Get("run.favorite") : null, kind: AnnouncementKinds.Value),
                },
                SearchText = () => Shown(row.runTypeLabel),
                OnActivate = () => { if (info != null && info.interactable) GameInput.Submit(info); },
                OnSecondary = () => Star(row),
                Details = () =>
                {
                    var lines = new List<string>();
                    if (Visible(row.covenantUI)) lines.AddRange(TooltipReader.Lines(StartingConditionsNodes.CovenantTooltips(row.covenantUI, null)));
                    if (Visible(row.difficultyTierUI)) lines.AddRange(TooltipReader.Lines(new[] { Difficulty(row.difficultyTierUI) }));
                    if (Visible(row.distanceUI)) lines.AddRange(TooltipReader.Lines(new[] { Distance(row) }));
                    return GameNodes.Lines(lines);
                },
            };
        }

        // How far the run got, as the game's distance tooltip words it ("Distance: ..." and what it means):
        // the row's display carries no provider of its own, so the tooltip is made as
        // RunDistanceDisplay.Set makes it where it has one, from the same keys and the same distance.
        private static TooltipContent Distance(RunHistoryEntryUI row)
        {
            var display = row.distanceUI;
            var data = row.GetRunData();
            if (display == null || data == null) return default;
            int distance = data.GetNumBattlesWon() + 1;
            string title = string.Format(GameText.Term(display.distanceTooltipTitleKey) ?? "{0}", GameText.Term("MapRegion_Name" + distance));
            return new TooltipContent(title, GameText.Term(display.distanceTooltipContentKey));
        }

        // A Soul Savior run's difficulty, as its tooltip words it ("Difficulty: ..." and what it means):
        // the provider's own where it has one, else made as DifficultyTierUI.AddDifficultyTierTooltip
        // makes it, from the same display data.
        private static TooltipContent Difficulty(DifficultyTierUI tier)
        {
            var provider = tier.TooltipProvider;
            if (provider != null && provider.Tooltips.Count > 0) return provider.Tooltips[0];
            var display = tier.DifficultyTierDisplayData;
            if (display == null) return default;
            return new TooltipContent(display.GetDifficultyKeyLocalized() + ": " + display.GetDifficultyTierName(tier.DifficultyTier),
                display.GetDifficultyTierDescription(tier.DifficultyTier));
        }

        // "Score: 16,210": the value under the header row's caption.
        private static string Score(RunHistoryEntryUI row, Transform header)
        {
            string value = Shown(row.scoreLabel);
            if (value == null) return null;
            var caption = header != null ? header.Find("Score container/Score label")?.GetComponent<TMP_Text>() : null;
            return Shown(caption) != null ? Shown(caption) + ": " + value : value;
        }

        // Favorite it or take it back, as the game takes it (RunHistoryEntryUI.WasStarButtonTriggered):
        // while the game's own selection is on the row, by its context action; otherwise by the star.
        private static void Star(RunHistoryEntryUI row)
        {
            bool was = Starred(row);
            var input = global::InputManager.Inst;
            if (input != null && input.currentInputModeUsesNavigation && row.infoButton != null && row.infoButton.IsGameUIComponent(input.GetSelectedGameUIComponent()))
                GameInput.Send(global::InputManager.Controls.ContextAction);
            else if (row.starButton != null)
                GameInput.Submit(row.starButton);
            // The game takes it at once: what it did, said (the change to none would otherwise be silence).
            if (Starred(row) != was) Speech.Say(Strings.Get(was ? "run.unfavorite" : "run.favorite"));
            else CoreLog.Warning("RunHistory: the game did not take the favorite toggle");
        }

        private static bool Starred(RunHistoryEntryUI row)
        {
            var data = row.GetRunData();
            var save = AllGameManagers.Instance.GetSaveManager();
            return data != null && save != null && save.RunHistoryManager.IsStarred(data.GetID());
        }

        private static string Join(params string[] parts)
        {
            var kept = new List<string>();
            foreach (var p in parts) if (!string.IsNullOrWhiteSpace(p)) kept.Add(p.Trim());
            return kept.Count > 0 ? string.Join(", ", kept) : null;
        }

        private static bool Visible(Component widget) => widget != null && widget.gameObject.activeInHierarchy;

        private static string Shown(TMP_Text text)
        {
            string content = text != null && text.gameObject.activeInHierarchy ? text.Content() : null;
            return string.IsNullOrWhiteSpace(content) ? null : content.Trim();
        }
    }
}
