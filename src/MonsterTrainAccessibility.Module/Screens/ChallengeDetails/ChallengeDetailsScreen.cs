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
    /// A challenge (<see cref="global::ChallengeDetailsScreen"/>: a community challenge, the daily
    /// challenge). Nothing is declared while the game loads it; once loaded its title, name or date and
    /// creator are said, and the screen is rows, Up and Down between them, Left and Right along:
    /// <list type="bullet">
    /// <item>what it sets: the clans, the Pyre Heart, the covenant, each mutator, in the game's words
    /// (their tooltips in the buffer);</item>
    /// <item>what to do: start (or continue) the run, with the daily's time left and the game's reason
    /// when it is locked, the other day's challenge, clean runs only, the sharing options, Back;</item>
    /// <item>the leaderboard: its tabs, then the entries one under another, each under the game's own
    /// column captions ("NomNom, Rank: 1, Time: 22m, Score: 83,200"), Enter opening its run where the
    /// game allows, then the page buttons (<see cref="PaginationNodes"/>); a new page says itself
    /// ("Page 2 of 6") once the game has it.</item>
    /// </list>
    /// Escape is the game's: back. A challenge being made reads as <see cref="Editable"/> lays out.
    /// </summary>
    public sealed partial class ChallengeDetailsScreen : Screen
    {
        private static readonly InputCategory[] Categories = { InputCategory.UI };

        public override string Key => "game.challenge_details";
        public override int Layer => 6;
        public override bool Exclusive => true;
        public override IReadOnlyList<InputCategory> InputCategories => Categories;

        private string _introduced;
        private string _page;

        private static global::ChallengeDetailsScreen Game
        {
            get
            {
                var screen = GameScreens.Active<global::ChallengeDetailsScreen>(global::ScreenName.ChallengeDetails);
                return screen != null && screen.IsInteractable() ? screen : null;
            }
        }

        public override bool IsActive() => Game != null;

        public override void OnPush()
        {
            _introduced = null;
            _page = null;
        }

        public override void Build(GraphBuilder b)
        {
            var screen = Game;
            if (screen == null || !Loaded(screen)) return;
            // Said once per challenge: a refetch of the same one (clean runs only) is not a new one.
            string shown = screen.challengeData.GetSharecodeOrID() + ":" + Shown(screen.titleLabel);
            if (_introduced != shown)
            {
                _introduced = shown;
                _page = null;
                ScreenIntro.Say(Shown(screen.titleLabel), screen.detailsRoot != null && screen.detailsRoot.activeInHierarchy ? Shown(screen.detailsLabel) : null,
                    Visible(screen.creatorRoot) ? Shown(screen.creatorLabel) : null);
            }
            // A picker the challenge being made has open is all there is until it closes.
            if (screen.editable && Picker(b, screen.detailsUI)) return;
            b.PushContext(null);
            if (screen.editable) Editable(b, screen.detailsUI);
            else Settings(b, screen.detailsUI);
            Actions(b, screen);
            Leaderboard(b, screen.leaderboard);
            b.PopContext();
            // Focus lands on starting the run, or back on what opened the picker just closed.
            if (_opener != null)
            {
                b.SetStart(_opener);
                _opener = null;
            }
            else if (Visible(screen.submitButton)) b.SetStart(ControlId.Structural("challenge:submit"));
        }

        public override IEnumerable<ElementAction> GetActions()
        {
            yield return new ElementAction(ActionIds.Back, Strings.Get("bind.ui.back"),
                _ => GameInput.Send(global::InputManager.Controls.Escape));
        }

        // Loaded: the game's spinner gone, its challenge in, its details shown.
        private static bool Loaded(global::ChallengeDetailsScreen screen)
            => screen.challengeData != null && !Visible(screen.loadingSpinner) && Visible(screen.detailsUI);

        // ---- what it sets ----

        private static void Settings(GraphBuilder b, ChallengeDetailsUI details)
        {
            if (details == null) return;
            var items = new List<KeyValuePair<ControlId, NodeVtable>>();
            if (Visible(details.mainClassInfo)) items.Add(Item("clan:main", StartingConditionsNodes.Clan(details.mainClassInfo)));
            if (Visible(details.subClassInfo)) items.Add(Item("clan:sub", StartingConditionsNodes.Clan(details.subClassInfo)));
            if (Visible(details.pyreHeartInfo)) items.Add(Item("pyre", StartingConditionsNodes.PyreHeart(details.pyreHeartInfo)));
            if (Visible(details.covenantRoot) && details.covenantSelection != null && details.covenantSelection.covenantUI != null)
                items.Add(Item("covenant", StartingConditionsNodes.Covenant(details.covenantSelection.covenantUI)));
            if (Visible(details.mutatorRoot))
            {
                int i = 0;
                foreach (var mutator in details.mutatorRoot.GetComponentsInChildren<MutatorButtonUI>(false))
                    if (mutator.MutatorState != null) items.Add(Item("mutator:" + i++, StartingConditionsNodes.Mutator(mutator)));
            }
            Row(b, null, items);
        }

        // ---- what to do ----

        private static void Actions(GraphBuilder b, global::ChallengeDetailsScreen screen)
        {
            var items = new List<KeyValuePair<ControlId, NodeVtable>>();
            var submit = screen.submitButton;
            if (Visible(submit))
            {
                var button = Visible(submit.continueRunButton) ? submit.continueRunButton : submit.newRunButton;
                var label = Visible(submit.continueRunButton) ? submit.continueRunButtonLabel : submit.newRunButtonLabel;
                if (Visible(button))
                {
                    var vt = GameWidgets.Button(button, () => Shown(label) ?? PauseMenuScreen.Caption(button), null,
                        new NodeAnnouncement(() => Visible(submit.timeLeftRoot) ? Shown(submit.timeLeftLabel) : null, kind: AnnouncementKinds.Value));
                    // A locked start (its content not installed) says why in its tooltip.
                    vt.Details = () => GameNodes.Lines(submit.tooltipProvider != null ? TooltipReader.Lines(submit.tooltipProvider) : new List<string>());
                    items.Add(Item("submit", vt));
                }
            }
            // These two flip their captions as they are pressed ("Today's Challenge", "All Runs"): said when they do.
            Button(items, "yesterday", screen.yesterdaysChallengeButton, live: true);
            Button(items, "clean", screen.cleanRunOnlyButton, live: true);
            var sharing = screen.sharingUI;
            if (Visible(sharing))
            {
                Button(items, "customize", sharing.customizeChallengeButton);
                Button(items, "copy", sharing.copySharecodeButton);
            }
            // Randomize All says what it rolled.
            var randomize = screen.randomizeDetailsButton;
            if (Visible(randomize) && PauseMenuScreen.Caption(randomize) != null)
            {
                var details = screen.detailsUI;
                var vt = GameWidgets.Button(randomize, () => PauseMenuScreen.Caption(randomize));
                vt.OnActivate = () =>
                {
                    GameInput.Submit(randomize);
                    Speech.Say(Rolled(details));
                };
                items.Add(Item("randomize", vt));
            }
            Button(items, "back", screen.backButton);
            Row(b, null, items);
        }

        // ---- the leaderboard ----

        private void Leaderboard(GraphBuilder b, ChallengeDetailsLeaderboardUI board)
        {
            if (!Visible(board)) return;
            var pages = board.paginationControls;
            string page = PaginationNodes.Page(pages);
            bool fetching = board.fetchingLeaderboard || Visible(board.loadingSpinner);
            // A new page said once the game has it (its label waits for the fetch).
            if (!fetching && page != null && page != _page)
            {
                if (_page != null) Speech.Say(page);
                _page = page;
            }
            string title = HeaderText(board.transform, "Text");
            var tabs = new List<KeyValuePair<ControlId, NodeVtable>>();
            Tab(tabs, "global", board.allEntriesButton);
            Tab(tabs, "friends", board.friendsOnlyButton);
            b.PushContext(title, Strings.RoleList);
            // Coming up from the entries lands on the tab shown (the game's activated one), not the first.
            if (tabs.Count > 0)
            {
                var shown = new List<GameUISelectableButton>();
                foreach (var button in new[] { board.allEntriesButton, board.friendsOnlyButton }) if (Visible(button)) shown.Add(button);
                b.StartRow(entry: () => System.Math.Max(0, shown.FindIndex(t => t.state == GameUISelectableButton.State.Activated)));
                foreach (var tab in tabs) b.AddItem(tab.Key, tab.Value);
                b.EndRow();
            }
            if (!fetching && board.currentRows != null)
            {
                foreach (var row in board.currentRows)
                    if (row != null && row.gameObject.activeInHierarchy && Shown(row.nameLabel) != null)
                    {
                        var entry = Item("entry:" + Shown(row.rankLabel) + ":" + Shown(row.nameLabel), Entry(board, row));
                        b.AddItem(entry.Key, entry.Value);
                    }
            }
            PaginationNodes.Add(b, "challenge:pages", pages);
            b.PopContext();
        }

        // "NomNom, Rank: 1, Time: 22m, Score: 83,200": the entry under the board's own column captions, the
        // victory and clean run marks' words; Enter opens its run where the game allows it.
        private static NodeVtable Entry(ChallengeDetailsLeaderboardUI board, LeaderboardRow row)
        {
            System.Func<string> label = () => Join(", ",
                Shown(row.nameLabel),
                Cell(board, "Rank label", row.rankLabel),
                Cell(board, "Time label", row.timeLabel),
                Cell(board, "Score label", row.scoreLabel),
                Visible(row.victoryIcon) ? Strings.Get("run.victory") : null,
                Visible(row.cleanRunIcon) ? Strings.Get("run.clean") : null);
            var summary = row.leaderboardRunSummaryButton;
            var info = summary != null ? summary.InfoButton : null;
            return new NodeVtable
            {
                ControlType = ControlTypes.Button,
                Announcements = new List<NodeAnnouncement>
                {
                    GameNodes.LabelPart(label),
                    GameNodes.DisabledPart(() => Visible(info) && info.interactable && info.state != GameUISelectableButton.State.Disabled),
                },
                SearchText = () => Shown(row.nameLabel),
                OnActivate = () => { if (Visible(info) && info.interactable) GameInput.Submit(info); },
                // Why its run cannot be opened, when it cannot: the game's tooltip on the entry.
                Details = () => GameNodes.Lines(summary != null ? TooltipReader.Lines(summary) : new List<string>()),
            };
        }

        // A tab of the board: landing on it shows it, as with every tab (the shown one is the game's
        // activated button, pressing it again would refetch).
        private static void Tab(List<KeyValuePair<ControlId, NodeVtable>> items, string key, GameUISelectableButton tab)
        {
            if (!Visible(tab)) return;
            System.Func<string> label = () => PauseMenuScreen.Caption(tab);
            System.Action open = () =>
            {
                if (tab.interactable && tab.state != GameUISelectableButton.State.Activated) GameInput.Submit(tab);
            };
            items.Add(Item("tab:" + key, new NodeVtable
            {
                ControlType = ControlTypes.Tab,
                Announcements = new List<NodeAnnouncement> { GameNodes.LabelPart(label) },
                SearchText = label,
                OnActivate = open,
                OnFocus = open,
            }));
        }

        // "Rank: 1": a cell under its column's caption, as the Compendium's leaderboard reads.
        private static string Cell(ChallengeDetailsLeaderboardUI board, string name, TMP_Text cell)
        {
            string value = Shown(cell);
            if (value == null) return null;
            string caption = Column(board, name);
            return caption != null ? caption + ": " + value : value;
        }

        // A column's caption, drawn once over the rows.
        private static string Column(ChallengeDetailsLeaderboardUI board, string name)
        {
            foreach (var text in board.GetComponentsInChildren<TMP_Text>(false))
                if (text.gameObject.name == name && text.GetComponentInParent<LeaderboardRow>() == null) return Shown(text);
            return null;
        }

        // ---- building blocks ----

        private static void Row(GraphBuilder b, string title, List<KeyValuePair<ControlId, NodeVtable>> items)
        {
            if (items.Count == 0) return;
            b.PushContext(title, title != null ? Strings.RoleList : null);
            b.StartRow();
            foreach (var item in items) b.AddItem(item.Key, item.Value);
            b.EndRow();
            b.PopContext();
        }

        private static KeyValuePair<ControlId, NodeVtable> Item(string key, NodeVtable vt)
            => new KeyValuePair<ControlId, NodeVtable>(ControlId.Structural("challenge:" + key), vt);

        private static void Button(List<KeyValuePair<ControlId, NodeVtable>> items, string key, GameUISelectableButton button, bool live = false)
        {
            if (!Visible(button) || PauseMenuScreen.Caption(button) == null) return;
            System.Func<string> caption = () => PauseMenuScreen.Caption(button);
            var vt = GameWidgets.Button(button, caption);
            if (live)
            {
                var parts = new List<NodeAnnouncement>(vt.Announcements);
                parts[0] = new NodeAnnouncement(caption, live: true, kind: AnnouncementKinds.Label);
                vt.Announcements = parts;
            }
            items.Add(Item(key, vt));
        }

        // The first shown text named so under a widget.
        private static string HeaderText(Transform root, string name)
        {
            foreach (var text in root.GetComponentsInChildren<TMP_Text>(false))
                if (text.gameObject.name == name && Shown(text) != null) return Shown(text);
            return null;
        }

        private static string Join(string separator, params string[] parts)
        {
            var kept = new List<string>();
            foreach (var p in parts) if (!string.IsNullOrWhiteSpace(p)) kept.Add(p.Trim());
            return kept.Count > 0 ? string.Join(separator, kept) : null;
        }

        private static GameObject Of(Object widget) => widget as GameObject ?? (widget as Component)?.gameObject;
        private static bool Visible(Object widget) => Of(widget) != null && Of(widget).activeInHierarchy;

        private static string Shown(TMP_Text text)
        {
            string content = text != null && text.gameObject.activeInHierarchy ? text.Content() : null;
            return string.IsNullOrWhiteSpace(content) ? null : content.Trim();
        }
    }
}
