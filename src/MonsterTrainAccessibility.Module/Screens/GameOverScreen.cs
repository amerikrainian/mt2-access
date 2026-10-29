using System.Collections.Generic;
using MonsterTrainAccessibility.Core.Graph;
using MonsterTrainAccessibility.Core.Input;
using MonsterTrainAccessibility.Core.Strings;
using MonsterTrainAccessibility.Core.UI;
using MonsterTrainAccessibility.Module.Interop;
using MonsterTrainAccessibility.Module.Nodes;
using MonsterTrainAccessibility.Module.UI;
using ShinyShoe;
using ShinyShoe.Loading;
using TMPro;
using UnityEngine;
using Screen = MonsterTrainAccessibility.Core.Screens.Screen;

namespace MonsterTrainAccessibility.Module.Screens
{
    /// <summary>
    /// A run's end (<see cref="global::GameOverScreen"/>: victory or defeat). Nothing is said or declared
    /// while the game animates it in (the score counting up, the clans' experience filling, the screens
    /// it opens on the way: a clan's level up, the Logbook's new entries), so what is read is the outcome,
    /// not a count in progress. Then the banner ("DEFEAT"), and one list: the accolades and streaks; the
    /// score, the bonus, the endless battles won; each battle's score; each clan with its level and
    /// experience ("Allied Clan, Pyreborne, 9, 16,597/35,000"); the progression the run made; the run's
    /// details (its type, covenant, distance, points); then the game's buttons (Back to Outpost, New Run,
    /// Run Summary, Endless). Each line's tooltips in the buffer. Escape is the game's: it leaves.
    /// </summary>
    public sealed class GameOverScreen : Screen
    {
        private static readonly InputCategory[] Categories = { InputCategory.UI };

        public override string Key => "game.game_over";
        public override int Layer => 5;
        public override IReadOnlyList<InputCategory> InputCategories => Categories;

        private bool _sawAnimating;
        private bool _announced;

        private static global::GameOverScreen Game => GameScreens.Active<global::GameOverScreen>(global::ScreenName.GameOver);

        public override bool IsActive() => Game != null;

        public override void OnPush()
        {
            _sawAnimating = false;
            _announced = false;
        }

        public override void Build(GraphBuilder b)
        {
            var screen = Game;
            if (screen == null || !Settled(screen)) return;
            if (!_announced)
            {
                _announced = true;
                ScreenIntro.Say(Shown(screen.titleLabel));
            }
            b.PushContext(null);
            int i = 0;
            foreach (var accolade in screen.statHighlightUIs)
                if (accolade != null && accolade.gameObject.activeInHierarchy)
                    Line(b, "accolade:" + i++, () => Join(": ", Shown(accolade.headerLabel), Shown(accolade.accoladeLabel)), null);
            foreach (var streak in new[] { screen.winStreakUI, screen.trueFinalBossWinStreakUI, screen.dailyChallengeRunStreakUI })
                if (Visible(streak)) Line(b, "streak:" + i++, () => Texts(streak), streak);
            Section(b, "score", screen.finalScoreStatAnimatedUI, () => Shown(screen.finalScoreStatLabel), null);
            Section(b, "bonus", screen.goldAnimatedUI, () => Texts(screen.goldUI), screen.goldUI != null ? screen.goldUI.tooltipProvider : null);
            Section(b, "endless", screen.battleScoresEndlessAnimatedUI, () => Shown(screen.battleScoresEndlessLabel), null);
            Battles(b, screen);
            Clan(b, "main", screen.mainClassInfo);
            Clan(b, "sub", screen.subClassInfo);
            Objectives(b, screen);
            RunInfo(b, screen.runInfoUI);
            Button(b, "back", screen.backButton);
            Button(b, "restart", screen.restartButton);
            Button(b, "summary", screen.runSummaryButton);
            Button(b, "endless", screen.endlessButton);
            b.PopContext();
        }

        public override IEnumerable<ElementAction> GetActions()
        {
            yield return new ElementAction(ActionIds.Back, Strings.Get("bind.ui.back"),
                _ => GameInput.Send(global::InputManager.Controls.Escape));
        }

        // The animation done: the game's own coroutine seen running and stopped. It is also absent before
        // it starts (the loading screen still up), so absent alone proves nothing; a score already at its
        // final count does (the mod reloaded after the animation).
        private bool Settled(global::GameOverScreen screen)
        {
            if (LoadingScreen.IsDisplaying()) return false;
            if (screen.animationCoroutine != null)
            {
                _sawAnimating = true;
                return false;
            }
            return _sawAnimating || ScoreShown(screen);
        }

        private static bool ScoreShown(global::GameOverScreen screen)
        {
            string shown = Shown(screen.finalScoreStatLabel);
            if (shown == null) return screen.battleScoresEndlessAnimatedUI != null && screen.battleScoresEndlessAnimatedUI.gameObject.activeInHierarchy;
            var digits = new System.Text.StringBuilder();
            foreach (char c in shown) if (char.IsDigit(c)) digits.Append(c);
            return digits.ToString() == screen.finalScore.ToString();
        }

        // "Score, 16,210": a section by its header and what it shows, while the game shows it.
        private static void Section(GraphBuilder b, string key, Component section, System.Func<string> value, Component tooltips)
        {
            if (!Visible(section) || value() == null) return;
            string header = ChallengeProgressScreen.HeaderOf(section, "Header Label");
            Line(b, key, () => Join(", ", header, value()), tooltips);
        }

        private static void Battles(GraphBuilder b, global::GameOverScreen screen)
        {
            if (!Visible(screen.battleScoresStandardAnimatedUI)) return;
            b.PushContext(ChallengeProgressScreen.HeaderOf(screen.battleScoresStandardAnimatedUI, "Header Label"), Strings.RoleList);
            int i = 0;
            foreach (var battle in screen.battleScoreUIs)
            {
                if (battle == null || !battle.interactable || !battle.gameObject.activeInHierarchy) continue;
                var shown = battle;
                // The game draws a battle as its score alone: named by its place in the run.
                int number = (shown.data != null ? shown.data.battleIndex : i) + 1;
                Line(b, "battle:" + i++, () => Join(", ", Strings.F("gameover.battle", number), TooltipReader.FirstTitle(shown) ?? Shown(shown.scoreLabel)),
                    shown, firstLine: false);
            }
            b.PopContext();
        }

        // "Allied Clan, Pyreborne, 9, 16,597/35,000": the panel's header, the clan, its level and the
        // meter's own words; what the next level brings in the buffer.
        private static void Clan(GraphBuilder b, string key, GameOverClassInfoUI clan)
        {
            if (!Visible(clan) || Shown(clan.classNameLabel) == null) return;
            var meter = clan.GetClassLevelMeterUI();
            Line(b, "clan:" + key, () =>
            {
                var parts = new List<string> { ChallengeProgressScreen.HeaderOf(clan, "Header Label"), Shown(clan.classNameLabel) };
                if (Visible(meter))
                {
                    string level = Shown(meter.levelLabel);
                    if (level != null) parts.Add(Strings.F("gameover.level", level));
                    // The meter's own words beside it: the experience ("16,597/35,000") or "Max Level".
                    foreach (var text in meter.GetComponentsInChildren<TMP_Text>(false))
                        if (text != meter.levelLabel && Shown(text) != null) parts.Add(Shown(text));
                }
                return Join(", ", parts.ToArray());
            }, meter);
        }

        // The progression the run made: the objectives the game fills (it scales the unused ones to nothing).
        private static void Objectives(GraphBuilder b, global::GameOverScreen screen)
        {
            int i = 0;
            foreach (var objective in screen.progressionObjectiveUIs)
            {
                if (objective == null || !objective.gameObject.activeInHierarchy || objective.transform.localScale.x <= 0.01f) continue;
                var shown = objective;
                Line(b, "objective:" + i++, () => Join(", ",
                    shown.titleRoot != null && shown.titleRoot.activeInHierarchy ? Shown(shown.titleLabel) : null,
                    shown.descriptionRoot != null && shown.descriptionRoot.activeInHierarchy ? Shown(shown.descriptionLabel) : null,
                    Shown(shown.numericLabel)), null);
            }
        }

        // The run's details: its type, points and time as drawn; the rest by their tooltips.
        private static void RunInfo(GraphBuilder b, global::RunInfo info)
        {
            if (!Visible(info)) return;
            Line(b, "run:type", () => Shown(info.runTypeLabel), info.classesUI);
            Line(b, "run:points", () => Shown(info.pointsLabel), null);
            Line(b, "run:time", () => Shown(info.playtimeLabel), null);
            int i = 0;
            foreach (Component part in new Component[] { info.covenantUI, info.difficultyTierUI, info.distanceUI, info.endlessDistanceUI, info.winStreakUI, info.trueFinalBossWinStreakUI })
                if (Visible(part) && TooltipReader.Lines(part).Count > 0) b.AddItem(ControlId.Structural("gameover:run:" + i++), TooltipReader.Item(part));
        }

        // A line read live; its widget's tooltips in the buffer (without the first, when the line already
        // says it: a battle's score).
        private static void Line(GraphBuilder b, string key, System.Func<string> text, Component tooltips, bool firstLine = true)
        {
            if (string.IsNullOrWhiteSpace(text())) return;
            var vt = GameNodes.Text(text);
            if (tooltips != null) vt.Details = () =>
            {
                var lines = TooltipReader.Lines(tooltips);
                if (!firstLine && lines.Count > 0 && tooltips is ITooltipProvider provider && provider.Tooltips.Count > 0)
                    lines[0] = provider.Tooltips[0].body;
                return GameNodes.Lines(lines);
            };
            b.AddItem(ControlId.Structural("gameover:" + key), vt);
        }

        private static void Button(GraphBuilder b, string key, GameUISelectableButton button)
        {
            if (!Visible(button) || PauseMenuScreen.Caption(button) == null) return;
            b.AddItem(ControlId.Structural("gameover:" + key), GameWidgets.Button(button, () => PauseMenuScreen.Caption(button)));
        }

        // Every text a widget draws, in order ("440", "+220"; "9", "16,597/35,000").
        private static string Texts(Component widget)
        {
            if (widget == null) return null;
            var parts = new List<string>();
            foreach (var text in widget.GetComponentsInChildren<TMP_Text>(false))
            {
                if (text.gameObject.name == "Header Label") continue;
                string s = Shown(text);
                if (s != null) parts.Add(s);
            }
            return parts.Count > 0 ? string.Join(", ", parts) : null;
        }

        private static string Join(string separator, params string[] parts)
        {
            var kept = new List<string>();
            foreach (var p in parts) if (!string.IsNullOrWhiteSpace(p)) kept.Add(p.Trim());
            return kept.Count > 0 ? string.Join(separator, kept) : null;
        }

        private static bool Visible(Component widget) => widget != null && widget.gameObject.activeInHierarchy;

        private static string Shown(TMP_Text text)
        {
            string content = text != null && text.gameObject.activeInHierarchy ? text.Content() : null;
            return string.IsNullOrWhiteSpace(content) ? null : content.Trim();
        }
    }
}
