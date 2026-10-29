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
using Screen = MonsterTrainAccessibility.Core.Screens.Screen;

namespace MonsterTrainAccessibility.Module.Screens
{
    /// <summary>
    /// The reward popup (<see cref="global::RewardScreen"/>: a battle's enemy artifact as it arrives, a
    /// reward granted on the spot). Opening it says the game's header where it shows one; then each reward
    /// as the game draws it (an artifact by <see cref="RelicNodes.Info"/>, a card by
    /// <see cref="CardNodes"/>, anything else by its title and description), then the game's buttons
    /// (OK, and Skip where the reward can be refused), pressed through the game. Nothing is declared
    /// while the game holds input back as the reward animates in. Escape is the game's.
    /// </summary>
    public sealed class RewardScreen : Screen
    {
        private static readonly InputCategory[] Categories = { InputCategory.Game, InputCategory.UI };

        public override string Key => "game.reward";
        public override int Layer => 12;
        public override bool Exclusive => true;
        public override IReadOnlyList<InputCategory> InputCategories => Categories;

        private static global::RewardScreen Game
        {
            get
            {
                var screen = GameScreens.Active<global::RewardScreen>(global::ScreenName.Reward);
                return screen != null && screen.inputAllowed ? screen : null;
            }
        }

        public override bool IsActive() => Game != null;

        public override void OnPush()
        {
            var screen = Game;
            if (screen != null && screen.headerRoot != null && screen.headerRoot.gameObject.activeInHierarchy && screen.headerRoot.alpha > 0f)
                ScreenIntro.Say(Shown(screen.headerLabel));
        }

        public override void Build(GraphBuilder b)
        {
            var screen = Game;
            if (screen == null) return;
            b.PushContext(null);
            int i = 0;
            if (screen.rewardDetailsUIs != null)
                foreach (var details in screen.rewardDetailsUIs)
                {
                    if (details == null || !details.gameObject.activeInHierarchy) continue;
                    foreach (var vt in Rewards(details))
                        b.AddItem(ControlId.Structural("reward:" + i++), vt);
                }
            Button(b, "collect", screen.collectButton);
            Button(b, "skip", screen.skipButton);
            b.PopContext();
        }

        public override IEnumerable<ElementAction> GetActions()
        {
            yield return new ElementAction(ActionIds.Back, Strings.Get("bind.ui.back"),
                _ => GameInput.Send(global::InputManager.Controls.Escape));
        }

        // What one reward panel shows: its artifacts, its card, or the generic reward's title and text.
        private static IEnumerable<NodeVtable> Rewards(RewardDetailsUI details)
        {
            foreach (var relic in new[] { details.relicUI, details.upgradeUI, details.sinRelicUI, details.mutatorUI })
                if (relic != null && relic.gameObject.activeInHierarchy && Shown(relic.titleLabel) != null)
                    yield return RelicNodes.Info(relic);
            var card = details.cardUI;
            if (card != null && card.gameObject.activeInHierarchy && card.GetCardState() != null)
                yield return CardNodes.Card(card, CardTooltipContainer.LoreTooltipSetting.Preferences, () => GameInput.Hover(card.SelectableUI));
            var generic = details.genericRewardUI;
            if (generic != null && generic.gameObject.activeInHierarchy && Shown(generic.titleLabel) != null)
                yield return new NodeVtable
                {
                    ControlType = ControlTypes.Text,
                    Announcements = new List<NodeAnnouncement>
                    {
                        GameNodes.LabelPart(() => Shown(generic.titleLabel)),
                        GameNodes.TooltipPart(() => Shown(generic.descriptionLabel)),
                    },
                };
        }

        private static void Button(GraphBuilder b, string key, GameUISelectableButton button)
        {
            if (button == null || !button.gameObject.activeInHierarchy || PauseMenuScreen.Caption(button) == null) return;
            b.AddItem(ControlId.Structural("reward:" + key), GameWidgets.Button(button, () => PauseMenuScreen.Caption(button)));
        }

        private static string Shown(TMP_Text text)
        {
            string content = text != null && text.gameObject.activeInHierarchy ? text.Content() : null;
            return string.IsNullOrWhiteSpace(content) ? null : content;
        }
    }
}
