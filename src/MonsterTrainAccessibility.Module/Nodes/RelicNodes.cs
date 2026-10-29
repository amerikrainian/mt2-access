using System;
using System.Collections.Generic;
using MonsterTrainAccessibility.Core.Graph;
using MonsterTrainAccessibility.Core.UI;
using MonsterTrainAccessibility.Module.UI;
using TMPro;

namespace MonsterTrainAccessibility.Module.Nodes
{
    /// <summary>
    /// An artifact as the game draws it on its card (<see cref="RelicInfoUI"/>: a draft's choice): its
    /// name, the type line where the card shows one, then what it does, all as drawn; the keywords it
    /// uses (its tooltips: "Pyregel: +1 incoming damage per stack.") in the buffer.
    /// </summary>
    internal static class RelicNodes
    {
        /// <summary>An artifact offered as a choice: "Gel Transfusor, button, When any unit dies, …".
        /// Landing shows it as hovered; Enter takes it.</summary>
        public static NodeVtable Offer(RelicInfoUI relic, Action hover, Action take)
        {
            Func<string> name = () => Shown(relic.titleLabel);
            return new NodeVtable
            {
                ControlType = ControlTypes.Button,
                Announcements = new List<NodeAnnouncement>
                {
                    GameNodes.LabelPart(name),
                    new NodeAnnouncement(() => relic.typeLabelRoot != null && relic.typeLabelRoot.activeInHierarchy ? Shown(relic.typeLabel) : null,
                        kind: AnnouncementKinds.Value),
                    GameNodes.TooltipPart(() => Shown(relic.descriptionLabel)),
                },
                SearchText = name,
                OnFocus = hover,
                OnActivate = take,
                Details = () => GameNodes.Lines(TooltipReader.Lines(relic.Tooltips ?? new List<TooltipContent>())),
            };
        }

        /// <summary>An artifact shown for reading (a battle's enemy artifact): "Tough as Nails, Enemy units
        /// enter with Titanskin 1."; the keywords it uses in the buffer.</summary>
        public static NodeVtable Info(RelicInfoUI relic)
        {
            Func<string> name = () => Shown(relic.titleLabel);
            return new NodeVtable
            {
                ControlType = ControlTypes.Text,
                Announcements = new List<NodeAnnouncement>
                {
                    GameNodes.LabelPart(name),
                    GameNodes.TooltipPart(() => Shown(relic.descriptionLabel)),
                },
                SearchText = name,
                Details = () => GameNodes.Lines(TooltipReader.Lines(relic.Tooltips ?? new List<TooltipContent>())),
            };
        }

        /// <summary>An artifact from its state, where no card of it is drawn (a run's artifacts, every page
        /// of them): "Heaven's Light, +2 capacity on each floor…"; its tooltips in the buffer, from the
        /// game's own tooltip generator.</summary>
        public static NodeVtable State(RelicState relic)
        {
            Func<string> name = () => relic.GetName();
            return new NodeVtable
            {
                ControlType = ControlTypes.Text,
                Announcements = new List<NodeAnnouncement>
                {
                    GameNodes.LabelPart(name),
                    GameNodes.TooltipPart(() => relic.GetDescription(AllGameManagers.Instance?.GetRelicManager(), includeDynamicInfo: true)),
                },
                SearchText = name,
                Details = () =>
                {
                    var managers = AllGameManagers.Instance;
                    var tooltips = new List<TooltipContent>();
                    TooltipGenerator.GetRelicTooltips(tooltips, relic, managers?.GetStatusEffectManager(), managers?.GetRelicManager(),
                        TooltipGenerator.OptionalFlags.Lore, managers?.GetSaveManager());
                    return GameNodes.Lines(TooltipReader.Lines(tooltips));
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
