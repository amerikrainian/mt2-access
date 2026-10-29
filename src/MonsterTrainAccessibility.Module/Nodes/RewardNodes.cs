using System.Collections.Generic;
using MonsterTrainAccessibility.Core.Graph;
using MonsterTrainAccessibility.Core.UI;
using MonsterTrainAccessibility.Module.Interop;
using MonsterTrainAccessibility.Module.UI;
using TMPro;

namespace MonsterTrainAccessibility.Module.Nodes
{
    /// <summary>
    /// A reward as the game's reward panel (<see cref="RewardDetailsUI"/>) draws it, wherever one stands
    /// (the reward popup, a shop's goods): an artifact by <see cref="RelicNodes.Info"/>, a card by
    /// <see cref="CardNodes"/>, anything else by the title and text the panel shows.
    /// </summary>
    internal static class RewardNodes
    {
        /// <summary>What one reward panel shows: its artifacts, its card, or a reward's own title and text.</summary>
        public static IEnumerable<NodeVtable> Details(RewardDetailsUI details)
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

        private static string Shown(TMP_Text text)
        {
            string content = text != null && text.gameObject.activeInHierarchy ? text.Content() : null;
            return string.IsNullOrWhiteSpace(content) ? null : content;
        }
    }
}
