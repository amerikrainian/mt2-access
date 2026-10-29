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
    /// A battle won (<see cref="VictoryUI"/>, over the battle's own screen). Opening it says the game's
    /// banner ("Victory!"); then the score as the game draws it, one line per entry ("Battle won,
    /// +1,000", "4 damage taken, -8%"), what each entry is for in the buffer, then the total; then the
    /// spoils, each by its tooltip ("Gain 50 Coin.", "Clan Pack: Gain a Clan Pack."); then Collect,
    /// which opens each spoil in turn through the game (the reward, the card draft). Escape collects,
    /// as in the game. Nothing is declared until the game shows Collect, nor while it is
    /// disabled (the spoils being handed out).
    /// </summary>
    public sealed class VictoryScreen : Screen
    {
        private static readonly InputCategory[] Categories = { InputCategory.Game, InputCategory.UI };

        public override string Key => "game.victory";
        public override int Layer => 4;
        public override bool Exclusive => true;
        public override IReadOnlyList<InputCategory> InputCategories => Categories;

        private static VictoryUI Victory
        {
            get
            {
                var screen = GameScreens.Active<GameScreen>(global::ScreenName.Game);
                var victory = screen != null ? screen.victoryUI : null;
                return victory != null && victory.gameObject.activeInHierarchy ? victory : null;
            }
        }

        public override bool IsActive() => Victory != null;

        public override void OnPush()
        {
            var victory = Victory;
            if (victory != null) ScreenIntro.Say(ChallengeProgressScreen.HeaderOf(victory, "Text Header"));
        }

        public override void Build(GraphBuilder b)
        {
            var victory = Victory;
            if (victory == null) return;
            var collect = victory.collectButton;
            // Collect disabled: the spoils are being handed out (a draft between two), nothing to say.
            if (collect == null || !collect.gameObject.activeInHierarchy || !collect.interactable) return;
            Score(b, victory);
            Spoils(b, victory);
            b.PushContext(null);
            b.AddItem(ControlId.Structural("victory:collect"), GameWidgets.Button(collect, () => Shown(victory.collectButtonLabel)));
            b.PopContext();
        }

        public override IEnumerable<ElementAction> GetActions()
        {
            yield return new ElementAction(ActionIds.Back, Strings.Get("bind.ui.back"),
                _ => GameInput.Send(global::InputManager.Controls.Escape));
        }

        private static void Score(GraphBuilder b, VictoryUI victory)
        {
            if (victory.scoreEntryContainer == null) return;
            b.PushContext(Shown(victory.scoreSectionHeaderLabel), Strings.RoleList);
            int i = 0;
            foreach (var entry in victory.scoreEntryContainer.GetComponentsInChildren<ScoreEntryUI>(false))
            {
                var e = entry;
                b.AddItem(ControlId.Structural("victory:score:" + i++), Line(
                    () => Shown(e.textContentLabel), () => Shown(e.scoreValueLabel), e.tooltipProvider));
            }
            if (Shown(victory.totalScoreLabel) != null)
                b.AddItem(ControlId.Structural("victory:total"), Line(
                    () => Shown(victory.totalScoreLabel), () => Shown(victory.totalScoreAmountLabel), null));
            b.PopContext();
        }

        private static void Spoils(GraphBuilder b, VictoryUI victory)
        {
            var container = victory.rewardsContainer;
            if (container == null || !container.gameObject.activeInHierarchy) return;
            b.PushContext(ChallengeProgressScreen.HeaderOf(container, "Header label"), Strings.RoleList);
            int i = 0;
            foreach (var reward in container.GetComponentsInChildren<RewardItemUI>(false))
                if (TooltipReader.Lines(reward).Count > 0) b.AddItem(ControlId.Structural("victory:spoil:" + i++), TooltipReader.Item(reward));
            b.PopContext();
        }

        // "Battle won, +1,000": the entry and its score; what it is for in the buffer.
        private static NodeVtable Line(System.Func<string> label, System.Func<string> value, Component tooltips)
        {
            return new NodeVtable
            {
                ControlType = ControlTypes.Text,
                Announcements = new List<NodeAnnouncement>
                {
                    GameNodes.LabelPart(label),
                    new NodeAnnouncement(value, kind: AnnouncementKinds.Value),
                },
                SearchText = label,
                Details = tooltips != null ? () => GameNodes.Lines(TooltipReader.Lines(tooltips)) : (System.Func<IEnumerable<string>>)null,
            };
        }

        private static string Shown(TMP_Text text)
        {
            string content = text != null && text.gameObject.activeInHierarchy ? text.Content() : null;
            return string.IsNullOrWhiteSpace(content) ? null : content.Trim();
        }
    }
}
