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

        private static string Shown(TMP_Text text)
        {
            string content = text != null && text.gameObject.activeInHierarchy ? text.Content() : null;
            return string.IsNullOrWhiteSpace(content) ? null : content;
        }
    }
}
