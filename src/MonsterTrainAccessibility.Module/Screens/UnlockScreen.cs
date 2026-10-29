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
    /// What a run unlocked (<see cref="global::UnlockScreen"/>: a clan's level up, a new clan, a card's
    /// mastery, a feature), one unlock at a time. As each appears its header and the game's instructions
    /// are said ("Banished Level 4!"); once the game has played it in, the list holds what it unlocked,
    /// read as it is everywhere else (a card by <see cref="CardNodes"/>, an artifact by
    /// <see cref="RelicNodes"/>, a feature by its title and text, the cards mastered), the clan's level
    /// where shown, then Collect, which moves on to the next. Focus lands on the first thing unlocked.
    /// </summary>
    public sealed class UnlockScreen : Screen
    {
        private static readonly InputCategory[] Categories = { InputCategory.UI };

        public override string Key => "game.unlock";
        public override int Layer => 20;
        public override bool Exclusive => true;
        public override IReadOnlyList<InputCategory> InputCategories => Categories;

        private int _said = -1;

        private static global::UnlockScreen Game => GameScreens.Active<global::UnlockScreen>(global::ScreenName.Unlock);

        public override bool IsActive() => Game != null;

        public override void OnPush() => _said = -1;

        public override void Build(GraphBuilder b)
        {
            var screen = Game;
            // Each unlock is played in, then its content made interactive: nothing until then.
            if (screen == null || screen.currentItem == null || screen.contentExceptButtonCanvasGroup == null
                || !screen.contentExceptButtonCanvasGroup.interactable || screen.collectButton == null || !screen.collectButton.gameObject.activeInHierarchy)
                return;
            int index = screen.currentIndex;
            if (_said != index)
            {
                _said = index;
                var explained = Visible(screen.cardMasteryUI) ? Shown(screen.cardMasteryUI.headerLabel) : null;
                ScreenIntro.Say(Shown(screen.headerLabel), Shown(screen.instructionsLabel), explained);
            }
            string key = "unlock:" + index + ":";
            b.PushContext(null);
            ControlId start = null;
            var details = screen.rewardDetailsUI;
            if (Visible(details))
            {
                int i = 0;
                foreach (var vt in RewardNodes.Details(details))
                {
                    var id = ControlId.Structural(key + "reward:" + i++);
                    b.AddItem(id, vt);
                    start = start ?? id;
                }
            }
            var feature = screen.unlockDetailsUI;
            if (Visible(feature) && Shown(feature.titleLabel) != null)
            {
                var id = ControlId.Structural(key + "feature");
                b.AddItem(id, new NodeVtable
                {
                    ControlType = ControlTypes.Text,
                    Announcements = new List<NodeAnnouncement>
                    {
                        GameNodes.LabelPart(() => Shown(feature.titleLabel)),
                        GameNodes.TooltipPart(() => Shown(feature.descriptionLabel)),
                    },
                });
                start = start ?? id;
            }
            var mastery = screen.cardMasteryUI;
            if (Visible(mastery) && mastery.cardUIs != null)
            {
                b.PushContext(null);
                int i = 0;
                foreach (var card in mastery.cardUIs)
                {
                    if (card == null || !card.gameObject.activeInHierarchy || card.GetCardState() == null) continue;
                    var c = card;
                    var id = ControlId.Structural(key + "mastered:" + i++);
                    b.AddItem(id, CardNodes.Card(c, CardTooltipContainer.LoreTooltipSetting.Preferences, () => GameInput.Hover(c.SelectableUI)));
                    start = start ?? id;
                }
                if (Shown(mastery.moreCardsLabel) != null)
                    b.AddItem(ControlId.Structural(key + "more"), GameNodes.Text(() => Shown(mastery.moreCardsLabel)));
                b.PopContext();
            }
            var meter = screen.classLevelMeter;
            if (Visible(meter) && Shown(meter.levelLabel) != null)
                b.AddItem(ControlId.Structural(key + "level"), new NodeVtable
                {
                    ControlType = ControlTypes.Text,
                    Announcements = new List<NodeAnnouncement> { GameNodes.LabelPart(() => Strings.F("gameover.level", Shown(meter.levelLabel))) },
                    Details = () => GameNodes.Lines(TooltipReader.Lines(meter)),
                });
            var collect = screen.collectButton;
            var collectId = ControlId.Structural(key + "collect");
            b.AddItem(collectId, GameWidgets.Button(collect, () => PauseMenuScreen.Caption(collect)));
            b.PopContext();
            b.SetStart(start ?? collectId);
        }

        private static bool Visible(Component widget) => widget != null && widget.gameObject.activeInHierarchy;

        private static string Shown(TMP_Text text)
        {
            string content = text != null && text.gameObject.activeInHierarchy ? text.Content() : null;
            return string.IsNullOrWhiteSpace(content) ? null : content.Trim();
        }
    }
}
