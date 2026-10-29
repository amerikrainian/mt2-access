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
    /// The battle's intro (<see cref="global::BattleIntroScreen"/>), one list in the panel's order:
    /// <list type="bullet">
    /// <item>the battle: its name and description ("Heavenly Toll: These Bellringers ..."), landed on
    /// first;</item>
    /// <item>each enemy by the game's own preview of it (<see cref="EnemyNodes.Preview"/>), the boss for
    /// a boss battle;</item>
    /// <item>the trial as one toggle: the enemy artifact it brings, its reward in short and the game's
    /// label for it, then its state ("Tough as Nails, 75 Coins, Trial, toggle, Off"; a boss's Titan trial in the game's own words),
    /// Enter switching it through the game; what the artifact does, its keywords and the trial's reward
    /// ("Reward: Gain 75 Coin.") in the buffer. With no trial to take, a boss's artifacts are lines of
    /// their own (<see cref="RelicNodes.Info"/>);</item>
    /// <item>Start Battle.</item>
    /// </list>
    /// Nothing is declared until the game has played the intro in, when it starts taking the fight.
    /// Escape is the game's: the Menu. The run keys (Tab's HUD) are live here.
    /// </summary>
    public sealed class BattleIntroScreen : Screen
    {
        private static readonly InputCategory[] Categories = { InputCategory.Game, InputCategory.UI };

        public override string Key => "game.battle_intro";
        public override int Layer => 5;
        public override IReadOnlyList<InputCategory> InputCategories => Categories;

        private static global::BattleIntroScreen Game
        {
            get
            {
                var screen = GameScreens.Active<global::BattleIntroScreen>(global::ScreenName.BattleIntro);
                return screen != null && screen.IsInteractable() ? screen : null;
            }
        }

        public override bool IsActive() => Game != null;

        public override void Build(GraphBuilder b)
        {
            var screen = Game;
            if (screen == null || !screen.isFightButtonAvailable) return;
            b.PushContext(null);
            var battle = ControlId.Structural("intro:battle");
            b.AddItem(battle, GameNodes.Text(() => Battle(screen)));
            b.SetStart(battle);
            Enemies(b, screen);
            Trial(b, screen);
            var fight = screen.fightButton;
            if (fight != null && fight.gameObject.activeInHierarchy)
                b.AddItem(ControlId.Structural("intro:fight"), GameWidgets.Button(fight, () => PauseMenuScreen.Caption(fight)));
            b.PopContext();
        }

        public override IEnumerable<ElementAction> GetActions()
        {
            yield return new ElementAction(ActionIds.Back, Strings.Get("bind.ui.back"),
                _ => GameInput.Send(global::InputManager.Controls.Escape));
        }

        // "Heavenly Toll: These Bellringers ...": the name, then the description as the panel draws it.
        private static string Battle(global::BattleIntroScreen screen)
        {
            string name = Shown(screen.battleNameLabel);
            var description = screen.battleDescriptionLabel != null ? screen.battleDescriptionLabel.GetComponent<TMP_Text>() : null;
            string text = Shown(description);
            if (name == null) return text;
            return text == null ? name : string.Format(GameText.Term("TextFormat_Colon") ?? "{0}: {1}", name, text);
        }

        private static void Enemies(GraphBuilder b, global::BattleIntroScreen screen)
        {
            int i = 0;
            var shown = new List<BattleIntroEnemy>();
            if (screen.enemyDisplays != null) shown.AddRange(screen.enemyDisplays);
            if (screen.trueFinalBossDisplays != null) shown.AddRange(screen.trueFinalBossDisplays);
            if (screen.bigBossDisplay != null) shown.Add(screen.bigBossDisplay);
            foreach (var enemy in shown)
            {
                if (enemy == null || !enemy.gameObject.activeInHierarchy || enemy.tooltipProvider == null) continue;
                if (TooltipReader.FirstTitle(enemy.tooltipProvider) == null) continue;
                b.AddItem(ControlId.Structural("intro:enemy:" + i++), EnemyNodes.Preview(enemy));
            }
        }

        // The trial as one control: what it brings and the game's label for it, then its state ("Tough
        // as Nails, Trial, toggle, Off"); what the artifact does, the keywords it uses and the reward in
        // the buffer. A boss battle with no trial to take shows the boss's artifacts as lines instead.
        private static void Trial(GraphBuilder b, global::BattleIntroScreen screen)
        {
            var relics = new List<RelicInfoUI>();
            if (screen.enemyRelicContainer != null && screen.enemyRelicContainer.gameObject.activeInHierarchy)
                relics.AddRange(screen.enemyRelicContainer.GetComponentsInChildren<RelicInfoUI>(false));
            var reward = screen.trialInfoUI != null && screen.trialInfoUI.gameObject.activeInHierarchy ? screen.trialInfoUI : null;
            var toggle = screen.trialToggleButton;
            if (toggle != null && toggle.gameObject.activeInHierarchy)
            {
                var vt = GameWidgets.Toggle(toggle, () =>
                {
                    var names = new List<string>();
                    foreach (var relic in relics)
                        if (Shown(relic.titleLabel) != null) names.Add(Shown(relic.titleLabel));
                    if (reward != null) names.Add(RewardShort(screen, reward));
                    names.Add(TrialLabel(toggle));
                    return string.Join(", ", names.FindAll(n => n != null));
                });
                vt.Details = () =>
                {
                    var lines = new List<string>();
                    foreach (var relic in relics)
                    {
                        lines.Add(Shown(relic.descriptionLabel));
                        lines.AddRange(TooltipReader.Lines(relic.Tooltips ?? new List<TooltipContent>()));
                    }
                    if (reward != null)
                    {
                        lines.Add(Reward(reward));
                        lines.AddRange(TooltipReader.Lines(reward));
                    }
                    return GameNodes.Lines(lines);
                };
                b.AddItem(ControlId.Structural("intro:trial"), vt);
                return;
            }
            for (int i = 0; i < relics.Count; i++)
                b.AddItem(ControlId.Structural("intro:relic:" + i), RelicNodes.Info(relics[i]));
            if (reward != null)
                b.AddItem(ControlId.Structural("intro:reward"), new NodeVtable
                {
                    ControlType = ControlTypes.Text,
                    Announcements = new List<NodeAnnouncement> { GameNodes.LabelPart(() => Reward(reward)) },
                    Details = () => GameNodes.Lines(TooltipReader.Lines(reward)),
                });
        }

        // The label the panel heads the toggle with ("Trial"): the text of its "Title" row, beside it.
        private static string TrialLabel(GameUISelectableToggle toggle)
        {
            for (var t = toggle.transform; t != null; t = t.parent)
            {
                var parent = t.parent;
                if (parent == null) break;
                var title = parent.Find("Title");
                if (title != null && title != t)
                {
                    var label = title.GetComponentInChildren<TMP_Text>(false);
                    if (Shown(label) != null) return Shown(label);
                }
            }
            return null;
        }

        // The reward in short, between what the trial brings and its label: a coin reward as its amount
        // and the game's word for the currency ("75 Coins", the game's own adjusted amount); any other
        // reward (a Titan trial's) as the line the panel draws for it.
        private static string RewardShort(global::BattleIntroScreen screen, TrialInfoUI reward)
        {
            var save = AllGameManagers.Instance.GetSaveManager();
            var data = screen.trialData != null && save != null ? screen.trialData.GetGrantableReward(save.GetCurrentActiveTrialInfo()) : null;
            if (data is GoldRewardData coin && !string.IsNullOrWhiteSpace(coin.RewardDetail))
                return Strings.RewardAmount(coin.RewardDetail, GameText.Term("ScreenRunSummary_Gold"));
            return Shown(reward.rewardTitleLabel);
        }

        // "Reward: Gain 75 Coin.": the panel's two lines, its heading and the reward.
        private static string Reward(TrialInfoUI reward)
        {
            var lines = new List<string>();
            foreach (var text in reward.GetComponentsInChildren<TMP_Text>(false))
                if (Shown(text) != null) lines.Add(Shown(text));
            if (lines.Count == 2) return string.Format(GameText.Term("TextFormat_Colon") ?? "{0}: {1}", lines[0], lines[1]);
            return GameNodes.Sentences(lines);
        }

        private static string Shown(TMP_Text text)
        {
            string content = text != null && text.gameObject.activeInHierarchy ? text.Content() : null;
            return string.IsNullOrWhiteSpace(content) ? null : content;
        }
    }
}
