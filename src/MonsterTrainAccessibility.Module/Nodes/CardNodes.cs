using System;
using System.Collections.Generic;
using MonsterTrainAccessibility.Core.Graph;
using MonsterTrainAccessibility.Core.Strings;
using MonsterTrainAccessibility.Core.UI;
using MonsterTrainAccessibility.Module.Interop;
using MonsterTrainAccessibility.Module.UI;
using TMPro;

namespace MonsterTrainAccessibility.Module.Nodes
{
    /// <summary>
    /// A card as the game drew it, wherever a <see cref="CardUI"/> stands (the Logbook, later the
    /// draft, the shop, the hand), from what its face draws; only its type and rarity are named from its
    /// data, in the game's own terms:
    /// <list type="bullet">
    /// <item>the focus line: the name, the card's type in the game's word for it in the role word's
    /// place ("Spell", "Friendly Unit"), then the face's own text objects: the cost, the type plate where
    /// it adds to the type (a unit's subtypes), attack and health while the face shows them, the size a
    /// unit's pips draw, then the card text as rendered (its inline icons read as words at the speech
    /// seam), what an upgrade adds marked;</item>
    /// <item>the buffer's head line ends with the card's type and rarity ("Firestarter, cost 1, Spell,
    /// Common"), the rarity never spoken on landing;</item>
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
            bool elixir = IsElixir(card);
            var parts = new List<NodeAnnouncement>
            {
                GameNodes.LabelPart(name),
                // The card's type in the role word's place: "Firestarter, Spell", not "item".
                new NodeAnnouncement(() => TypeOf(card), kind: AnnouncementKinds.Role) { Spoken = !elixir },
                new NodeAnnouncement(() => Face(card), kind: AnnouncementKinds.Value) { Spoken = !elixir || !FreeToPlay(card) },
            };
            parts.AddRange(TextParts(card, elixir));
            parts.Add(new NodeAnnouncement(() => RarityOf(card), kind: AnnouncementKinds.Review));
            return new NodeVtable
            {
                ControlType = ControlTypes.Item,
                Announcements = parts,
                SearchText = name,
                OnFocus = hover,
                Details = () => GameNodes.Lines(Tooltips(card, lore)),
            };
        }

        /// <summary>A card offered as a choice under a name of its own (a Light Forge upgrade path's):
        /// "Savior, button, Fel, cost 0, Champion, 5x2 attack, 6 health, then the card text"; its tooltips
        /// in the buffer as for any card. Landing shows it as hovered; Enter takes it.</summary>
        public static NodeVtable Offer(Func<string> title, CardUI card, CardTooltipContainer.LoreTooltipSetting lore, Action hover, Action take)
        {
            bool elixir = IsElixir(card);
            var parts = new List<NodeAnnouncement> { GameNodes.LabelPart(title) };
            parts.Add(new NodeAnnouncement(() => Name(card), kind: AnnouncementKinds.Value));
            parts.Add(new NodeAnnouncement(() => TypeOf(card), kind: AnnouncementKinds.Value) { Spoken = !elixir });
            parts.Add(new NodeAnnouncement(() => Face(card), kind: AnnouncementKinds.Value) { Spoken = !elixir || !FreeToPlay(card) });
            parts.AddRange(TextParts(card, elixir));
            parts.Add(new NodeAnnouncement(() => RarityOf(card), kind: AnnouncementKinds.Review));
            return new NodeVtable
            {
                ControlType = ControlTypes.Button,
                Announcements = parts,
                SearchText = title,
                OnFocus = hover,
                OnActivate = take,
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

        // The card's type in the game's word for it on a card ("Spell", "Friendly Unit", "Equipment").
        private static string TypeOf(CardUI card)
        {
            var state = card.GetCardState();
            if (state == null) return null;
            string type = state.GetCardType().GetCardText();
            return string.IsNullOrWhiteSpace(type) ? null : type;
        }

        // The card's rarity in the game's word ("Common"): on review only, ending the buffer's head line.
        private static string RarityOf(CardUI card)
        {
            var state = card.GetCardState();
            return state != null && state.GetRarity() != CollectableRarity.Unset ? GameText.Term("CardRarity_" + state.GetRarity()) : null;
        }

        // The name label the frame draws for the card's type (the frame keeps both; the game picks one).
        private static string Name(CardUI card)
        {
            var frame = card._cardFrame;
            var state = card.GetCardState();
            if (frame == null || state == null) return null;
            return Shown(frame.GetNameLabel(state.GetCardType()));
        }

        // The copies the card stands for when the game draws a count ("x2"), then cost, type plate,
        // attack, health, size: the face's numbers and plate, in the order drawn.
        private static string Face(CardUI card)
        {
            var parts = new List<string>();
            if (card.countRoot != null && card.countRoot.activeSelf) Add(parts, Shown(card.countLabel), null);
            Add(parts, Shown(card._costLabel), Strings.CardCost);
            // The type plate: a unit's subtypes ("Steward"); a spell's says "Spell" again, dropped.
            string plate = card._cardFrame != null ? Shown(card._cardFrame.factionLabel) : null;
            if (!string.Equals(plate?.Trim(), TypeOf(card)?.Trim(), StringComparison.OrdinalIgnoreCase)) Add(parts, plate, null);
            var ability = card._abilityDisplay;
            if (ability != null && ability.gameObject.activeInHierarchy)
                Add(parts, Shown(ability.abilityLabel), Strings.CardAttack);
            Add(parts, Shown(card._healthLabel), Strings.CardHealth);
            var state = card.GetCardState();
            if (state != null && state.GetCardType() == CardType.Monster)
                Add(parts, state.GetSize(false).ToString(), Strings.CardSize);
            return parts.Count > 0 ? string.Join(", ", parts) : null;
        }

        // An elixir (an infusion, a crafted spike): its focus line is its name and what it does, the
        // keywords every elixir carries and a cost of 0 left to the buffer, which keeps the card whole.
        private static bool IsElixir(CardUI card)
        {
            var state = card.GetCardState();
            return state != null && (state.HasTrait(typeof(CardTraitInfusion)) || state.HasTrait(typeof(CardTraitCraftedSpike)));
        }

        private static bool FreeToPlay(CardUI card) => Shown(card._costLabel)?.Trim() == "0";

        // The card text: whole; for an elixir, whole in the buffer and spoken without its keyword
        // paragraph (the text before the card's first paragraph break).
        private static IEnumerable<NodeAnnouncement> TextParts(CardUI card, bool elixir)
        {
            var whole = GameNodes.TooltipPart(() => Text(card));
            if (!elixir)
            {
                yield return whole;
                yield break;
            }
            whole.Spoken = false;
            yield return whole;
            var effect = GameNodes.TooltipPart(() => WithoutKeywords(Text(card)));
            effect.Buffered = false;
            yield return effect;
        }

        private static readonly System.Text.RegularExpressions.Regex ParagraphBreak =
            new System.Text.RegularExpressions.Regex(@"(<size=[^>]*>)?\s*<br\s*/?>\s*<br\s*/?>\s*(</size>)?", System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        private static string WithoutKeywords(string text)
        {
            if (text == null) return null;
            var m = ParagraphBreak.Match(text);
            return m.Success ? text.Substring(m.Index + m.Length) : text;
        }

        // The card text as drawn, with what an upgrade adds marked ("Multistrike 1. new: Ability:
        // Savior."): the game draws it in the card's upgrade colour (CardUI.UpdateUpgradeHighlightText),
        // which speech cannot carry. A colour nested inside the span (an X cost's) does not end it.
        private static string Text(CardUI card)
        {
            string text = Shown(card._cardFrame != null ? card._cardFrame.cardTextLabel : null);
            if (text == null) return null;
            string open = "<color=#" + ShinyShoe.ColorUtil.ToHtmlStringRGBACached(card.upgradedTextColor) + ">";
            var sb = new System.Text.StringBuilder();
            int at = 0;
            while (true)
            {
                int start = text.IndexOf(open, at, StringComparison.OrdinalIgnoreCase);
                if (start < 0) break;
                int end = SpanEnd(text, start + open.Length);
                if (end < 0) break;
                sb.Append(text, at, start - at);
                sb.Append(Strings.CardUpgraded(text.Substring(start + open.Length, end - start - open.Length)));
                at = end + "</color>".Length;
            }
            sb.Append(text, at, text.Length - at);
            return sb.ToString();
        }

        // Where the colour span opened before `from` closes: the "</color>" that balances it.
        private static int SpanEnd(string text, int from)
        {
            int depth = 1;
            for (int i = from; i < text.Length; i++)
            {
                if (string.Compare(text, i, "<color", 0, 6, StringComparison.OrdinalIgnoreCase) == 0) depth++;
                else if (string.Compare(text, i, "</color>", 0, 8, StringComparison.OrdinalIgnoreCase) == 0 && --depth == 0) return i;
            }
            return -1;
        }

        private static void Add(List<string> parts, string value, Func<string, string> format)
        {
            if (string.IsNullOrWhiteSpace(value)) return;
            parts.Add(format != null ? format(value) : value);
        }

        private static string Shown(TMP_Text text)
            => text != null && text.gameObject.activeInHierarchy && !string.IsNullOrWhiteSpace(text.Content()) ? text.Content() : null;
    }
}
