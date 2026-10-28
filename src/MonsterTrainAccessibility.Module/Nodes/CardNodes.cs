using System;
using System.Collections.Generic;
using MonsterTrainAccessibility.Core.Graph;
using MonsterTrainAccessibility.Core.Strings;
using MonsterTrainAccessibility.Core.UI;
using MonsterTrainAccessibility.Module.UI;
using TMPro;

namespace MonsterTrainAccessibility.Module.Nodes
{
    /// <summary>
    /// A card as the game drew it, wherever a <see cref="CardUI"/> stands (the Logbook, later the
    /// draft, the shop, the hand). Nothing is rebuilt from the card's data:
    /// <list type="bullet">
    /// <item>the focus line is the face's own text objects: the name, the cost, the type plate (the
    /// card type, or a unit's subtypes), attack and health while the face shows them, the size a
    /// unit's pips draw, then the card text as rendered (its inline icons read as words at the speech
    /// seam);</item>
    /// <item>the control buffer holds the card's tooltips, populated in the card's own
    /// <see cref="CardTooltipContainer"/> by the very call hover makes (<c>CardUI.UpdateTooltip</c>,
    /// with the lore setting the screen's hover uses) and read off what it instantiated.</item>
    /// </list>
    /// A locked or unknown card is a <see cref="LockedCardUI"/>: its own tooltip, the game's words.
    /// </summary>
    internal static class CardNodes
    {
        public static NodeVtable Card(CardUI card, CardTooltipContainer.LoreTooltipSetting lore, Action hover)
        {
            Func<string> name = () => Name(card);
            return new NodeVtable
            {
                ControlType = ControlTypes.Item,
                Announcements = new List<NodeAnnouncement>
                {
                    GameNodes.LabelPart(name),
                    new NodeAnnouncement(() => Face(card), kind: AnnouncementKinds.Value),
                    GameNodes.TooltipPart(() => Shown(card._cardFrame != null ? card._cardFrame.cardTextLabel : null)),
                },
                SearchText = name,
                OnFocus = hover,
                Details = () => GameNodes.Lines(Tooltips(card, lore)),
            };
        }

        public static NodeVtable Locked(LockedCardUI card, Action hover)
        {
            Func<string> title = () => { var lines = TooltipReader.Lines(card.Tooltips); return lines.Count > 0 ? lines[0] : null; };
            return new NodeVtable
            {
                ControlType = ControlTypes.Item,
                Announcements = new List<NodeAnnouncement> { GameNodes.LabelPart(title) },
                SearchText = title,
                OnFocus = hover,
                Details = () => GameNodes.Lines(TooltipReader.Lines(card.Tooltips)),
            };
        }

        /// <summary>The card's tooltips: hover's own population of its container, read as drawn.</summary>
        public static List<string> Tooltips(CardUI card, CardTooltipContainer.LoreTooltipSetting lore)
        {
            var container = card.cardTooltipsUI;
            var state = card.GetCardState();
            if (container == null || state == null) return new List<string>();
            container.includeLoreTooltips = lore;
            card.UpdateTooltip(state);
            return TooltipReader.Container(container);
        }

        // The name label the frame draws for the card's type (the frame keeps both; the game picks one).
        private static string Name(CardUI card)
        {
            var frame = card._cardFrame;
            var state = card.GetCardState();
            if (frame == null || state == null) return null;
            return Shown(frame.GetNameLabel(state.GetCardType()));
        }

        // Cost, type plate, attack, health, size: the face's numbers and plate, in the order drawn.
        private static string Face(CardUI card)
        {
            var parts = new List<string>();
            Add(parts, Shown(card._costLabel), Strings.CardCost);
            Add(parts, card._cardFrame != null ? Shown(card._cardFrame.factionLabel) : null, null);
            var ability = card._abilityDisplay;
            if (ability != null && ability.gameObject.activeInHierarchy)
                Add(parts, Shown(ability.abilityLabel), Strings.CardAttack);
            Add(parts, Shown(card._healthLabel), Strings.CardHealth);
            var state = card.GetCardState();
            if (state != null && state.GetCardType() == CardType.Monster)
                Add(parts, state.GetSize(false).ToString(), Strings.CardSize);
            return parts.Count > 0 ? string.Join(", ", parts) : null;
        }

        private static void Add(List<string> parts, string value, Func<string, string> format)
        {
            if (string.IsNullOrWhiteSpace(value)) return;
            parts.Add(format != null ? format(value) : value);
        }

        private static string Shown(TMP_Text text)
            => text != null && text.gameObject.activeInHierarchy && !string.IsNullOrWhiteSpace(text.text) ? text.text : null;
    }
}
