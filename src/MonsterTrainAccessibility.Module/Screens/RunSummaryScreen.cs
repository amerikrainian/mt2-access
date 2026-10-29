using System.Collections.Generic;
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
    /// One run's summary (<see cref="global::RunSummaryScreen"/>: from a run's end, the run history, a
    /// challenge's leaderboard), one row per part of it, Up and Down between them, Left and Right along:
    /// <list type="bullet">
    /// <item>the run: who and when, the clans with their levels, the Pyre Heart, the covenant, then the
    /// game's own captions and values (Run Type, Outcome, Time, Bonus, Score), each tooltip in the
    /// buffer;</item>
    /// <item>its artifacts (all of them, not only the page the game shows), mutators and souls;</item>
    /// <item>its deck: each card as any card reads, from the preview the game draws for it, with its
    /// count;</item>
    /// <item>its battles: the battle, its score, the enemy's artifacts it brought;</item>
    /// <item>the game's buttons (the leaderboard's previous and next entries, Generate Challenge, Back).</item>
    /// </list>
    /// The screen's title is said as it opens. Escape is the game's: back.
    /// </summary>
    public sealed class RunSummaryScreen : Screen
    {
        private static readonly InputCategory[] Categories = { InputCategory.UI };

        public override string Key => "game.run_summary";
        public override int Layer => 8;
        public override bool Exclusive => true;
        public override IReadOnlyList<InputCategory> InputCategories => Categories;

        private static global::RunSummaryScreen Game
        {
            get
            {
                var screen = GameScreens.Active<global::RunSummaryScreen>(global::ScreenName.RunSummary);
                return screen != null && screen.IsInteractable() ? screen : null;
            }
        }

        public override bool IsActive() => Game != null;

        public override void OnPush()
        {
            var screen = Game;
            if (screen != null && screen.summaryDetailsUI != null) ScreenIntro.Say(ChallengeProgressScreen.HeaderOf(screen.summaryDetailsUI, "Title Label"));
        }

        public override void Build(GraphBuilder b)
        {
            var screen = Game;
            var details = screen != null ? screen.summaryDetailsUI : null;
            if (details == null) return;
            b.PushContext(null);
            Run(b, details);
            Relics(b, "artifacts", details.artifactsContainer, details.blessingCollection);
            Relics(b, "mutators", details.mutatorContainer, details.mutatorCollection);
            Relics(b, "souls", details.soulsContainer, details.soulsCollection);
            Deck(b, details);
            Battles(b, details);
            var buttons = new List<KeyValuePair<string, GameUISelectableButton>>
            {
                new KeyValuePair<string, GameUISelectableButton>("previous", screen.previousEntryButton),
                new KeyValuePair<string, GameUISelectableButton>("next", screen.nextEntryButton),
                new KeyValuePair<string, GameUISelectableButton>("challenge", screen.createChallengeButton),
                new KeyValuePair<string, GameUISelectableButton>("back", screen.backButton),
            };
            Row(b, null, buttons.FindAll(p => Visible(p.Value) && PauseMenuScreen.Caption(p.Value) != null)
                .ConvertAll(p => Item("button:" + p.Key, GameWidgets.Button(p.Value, () => PauseMenuScreen.Caption(p.Value)))));
            b.PopContext();
        }

        public override IEnumerable<ElementAction> GetActions()
        {
            yield return new ElementAction(ActionIds.Back, Strings.Get("bind.ui.back"),
                _ => GameInput.Send(global::InputManager.Controls.Escape));
        }

        // ---- the run ----

        private static void Run(GraphBuilder b, RunSummaryDetailsUI details)
        {
            var items = new List<KeyValuePair<ControlId, NodeVtable>>();
            Line(items, "who", () => Join(", ", Shown(details.userIdLabel), Shown(details.timeLabel)), null);
            if (Visible(details.sharecodeRoot)) Line(items, "sharecode", () => Captioned(details.sharecodeLabel), null);
            Clan(items, "main", details.mainClanUI);
            Clan(items, "sub", details.subClanUI);
            var pyre = details.pyreHeartUI;
            if (Visible(pyre)) Line(items, "pyre", () => Join(", ", Caption(null, pyre.transform), TooltipReader.FirstTitle(pyre) ?? Shown(pyre.titleLabel)), pyre);
            if (Visible(details.covenantContainer)) Line(items, "covenant", () => Captioned(details.covenantLabel), details.covenantContainer.transform);
            if (Visible(details.difficultyTierContainer) && Visible(details.difficultyTierUI) && TooltipReader.Lines(details.difficultyTierUI).Count > 0)
                items.Add(Item("run:tier", TooltipReader.Item(details.difficultyTierUI)));
            Line(items, "type", () => Captioned(details.runTypeLabel), null);
            if (Visible(details.outcomeContainerDefault))
                Line(items, "outcome", () => Join(", ", Captioned(details.outcomeLabelDefault),
                    Visible(details.cleanRunIcon) ? TooltipReader.FirstTitle(details.cleanRunIcon) : null), details.cleanRunIcon);
            if (Visible(details.outcomeContainerEndless)) Line(items, "outcome:endless", () => Captioned(details.outcomeLabelEndless), null);
            Line(items, "time", () => Captioned(details.runTimeLabel), null);
            if (Visible(details.goldScoreRow)) Line(items, "bonus", () => Texts(details.goldScoreRow), details.goldScoreRow);
            if (Visible(details.scoreContainer)) Line(items, "score", () => Captioned(details.scoreLabel), null);
            Row(b, null, items);
        }

        // "Primary Clan, Banished, level 10": the slot, the clan (its tooltip's title), its level.
        private static void Clan(List<KeyValuePair<ControlId, NodeVtable>> items, string key, ClassIconUI clan)
        {
            if (!Visible(clan)) return;
            Line(items, "clan:" + key, () =>
            {
                string level = clan.classLevelMeter != null && clan.classLevelMeter.gameObject.activeInHierarchy ? Shown(clan.classLevelMeter.levelLabel) : null;
                return Join(", ", Caption(null, clan.transform), TooltipReader.FirstTitle(clan), level != null ? Strings.F("gameover.level", level) : null);
            }, clan);
        }

        // ---- the collections ----

        private static void Relics(GraphBuilder b, string key, Object container, RelicCollectionUI collection)
        {
            if (!Visible(container) || collection == null || collection.relics == null || collection.relics.Count == 0) return;
            var items = new List<KeyValuePair<ControlId, NodeVtable>>();
            for (int i = 0; i < collection.relics.Count; i++)
                if (collection.relics[i] != null) items.Add(Item(key + ":" + i, RelicNodes.State(collection.relics[i])));
            Row(b, SectionTitle(container, collection.transform), items);
        }

        private static void Deck(GraphBuilder b, RunSummaryDetailsUI details)
        {
            if (!Visible(details.cardsContainer) || details.cardUIs == null) return;
            var items = new List<KeyValuePair<ControlId, NodeVtable>>();
            int i = 0;
            foreach (var card in details.cardUIs)
                if (card != null && card.gameObject.activeInHierarchy && card.CardState != null)
                    items.Add(Item("card:" + i++, CardNodes.SummaryItem(card, () => details.cardPreview != null ? details.cardPreview.cardUI : null)));
            Row(b, SectionTitle(details.cardsContainer, details.cardsLayout != null ? details.cardsLayout.transform : null), items);
        }

        // "Heavenly Toll, +1,520, Tough as Nails": the battle, its score, the artifacts its enemy brought;
        // the score's breakdown and those artifacts' text in the buffer.
        private static void Battles(GraphBuilder b, RunSummaryDetailsUI details)
        {
            if (!Visible(details.battlesContainer) || details.battleHistoryItems == null) return;
            var items = new List<KeyValuePair<ControlId, NodeVtable>>();
            int i = 0;
            foreach (var battle in details.battleHistoryItems)
            {
                if (battle == null || !battle.gameObject.activeInHierarchy) continue;
                var shown = battle;
                var sins = new List<RelicIconUI>();
                if (shown.battleSinLayout != null)
                    foreach (var sin in shown.battleSinLayout.GetComponentsInChildren<RelicIconUI>(false)) sins.Add(sin);
                var vt = GameNodes.Text(() =>
                {
                    var parts = new List<string> { Shown(shown.battleNameLabel) };
                    var score = shown.scoreEventUI;
                    if (score != null && score.gameObject.activeInHierarchy) parts.Add(Join(" ", Shown(score.eventLabel), Shown(score.scoreLabel)));
                    foreach (var sin in sins) parts.Add(TooltipReader.FirstTitle(sin));
                    return Join(", ", parts.ToArray());
                });
                vt.Details = () =>
                {
                    var lines = new List<string>();
                    var score = shown.scoreEventUI;
                    if (score != null && score.Tooltips != null)
                        foreach (var t in score.Tooltips) lines.Add(t.body);
                    foreach (var sin in sins) lines.AddRange(TooltipReader.Lines(sin));
                    return GameNodes.Lines(lines);
                };
                items.Add(Item("battle:" + i++, vt));
            }
            Row(b, SectionTitle(details.battlesContainer, details.battleHistoryLayout != null ? details.battleHistoryLayout.transform : null), items);
        }

        // ---- building blocks ----

        // A row under a context: Left and Right along it; Up and Down between rows.
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
            => new KeyValuePair<ControlId, NodeVtable>(ControlId.Structural("summary:" + key), vt);

        private static void Line(List<KeyValuePair<ControlId, NodeVtable>> items, string key, System.Func<string> text, Component tooltips)
        {
            if (string.IsNullOrWhiteSpace(text())) return;
            var vt = GameNodes.Text(text);
            if (tooltips != null) vt.Details = () => GameNodes.Lines(TooltipReader.Lines(tooltips));
            items.Add(Item("run:" + key, vt));
        }

        // "Run Type, Standard Run": a value with the caption the game draws beside it.
        private static string Captioned(TMP_Text value)
        {
            string shown = Shown(value);
            return shown == null ? null : Join(", ", Caption(value), shown);
        }

        // The caption the game heads a value's panel with: its "Header" label, the panel being the first
        // of the value's ancestors that has one.
        private static string Caption(TMP_Text value, Transform root = null)
        {
            for (var t = value != null ? value.transform.parent : root; t != null; t = t.parent)
            {
                var header = t.Find("Header");
                if (header == null) continue;
                foreach (var text in header.GetComponentsInChildren<TMP_Text>(false))
                    if (text != value && Shown(text) != null) return Shown(text);
                return null;
            }
            return null;
        }

        // A section's title: the first text drawn in its container outside its content ("Battles").
        private static string SectionTitle(Object container, Transform content)
        {
            var root = Of(container);
            if (root == null) return null;
            foreach (var text in root.GetComponentsInChildren<TMP_Text>(false))
            {
                if (content != null && text.transform.IsChildOf(content)) continue;
                if (Shown(text) != null) return Shown(text);
            }
            return null;
        }

        private static string Texts(Component widget)
        {
            var parts = new List<string>();
            foreach (var text in widget.GetComponentsInChildren<TMP_Text>(false))
                if (Shown(text) != null) parts.Add(Shown(text));
            return parts.Count > 0 ? string.Join(", ", parts) : null;
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
