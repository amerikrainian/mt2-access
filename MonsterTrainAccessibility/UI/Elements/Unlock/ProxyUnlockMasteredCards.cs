using System.Collections.Generic;
using MonsterTrainAccessibility.Buffers;
using MonsterTrainAccessibility.Localization;
using MonsterTrainAccessibility.Util;

namespace MonsterTrainAccessibility.UI.Elements
{
    internal sealed class ProxyUnlockMasteredCards : GameObjectElement
    {
        private readonly IReadOnlyList<CardData> _cards;

        public ProxyUnlockMasteredCards(UnityEngine.GameObject target, IReadOnlyList<CardData> cards)
            : base(target, label: null)
        {
            _cards = cards;
        }

        public override bool IsVisible => (Target == null || Target.activeInHierarchy) && CountCards() > 0;

        public override Message GetLabel()
        {
            List<Message> names = CardNames();
            return names.Count > 0
                ? Message.Localized("ui", "UNLOCK.MASTERED_CARDS", new { cards = Message.Join(", ", names).Resolve() })
                : null;
        }

        public override Message GetTooltip()
        {
            List<Message> parts = new List<Message>();
            if (_cards == null)
            {
                return null;
            }

            for (int i = 0; i < _cards.Count; i++)
            {
                CardData card = _cards[i];
                if (card == null)
                {
                    continue;
                }

                CardState state = ProxyUnlockContent.NewCardState(card);
                MessageList.Add(parts, Message.Join(": ", Message.RawCleaned(card.GetName()), ProxyCombatCard.Description(state)));
            }

            return parts.Count > 0 ? Message.JoinLines(parts) : null;
        }

        internal override string HandleBuffers(BufferManager buffers)
        {
            LineBuffer buffer = buffers?.GetBuffer("ui");
            if (buffer == null)
            {
                return base.HandleBuffers(buffers);
            }

            buffer.Clear();
            List<Message> names = CardNames();
            for (int i = 0; i < names.Count; i++)
            {
                buffer.Add(names[i]);
            }
            buffers.EnableBuffer("ui", true);
            return "ui";
        }

        private List<Message> CardNames()
        {
            List<Message> names = new List<Message>();
            if (_cards == null)
            {
                return names;
            }

            for (int i = 0; i < _cards.Count; i++)
            {
                CardData card = _cards[i];
                MessageList.Add(names, Message.RawCleaned(card?.GetName()));
            }

            return names;
        }

        private int CountCards()
        {
            if (_cards == null)
            {
                return 0;
            }

            int count = 0;
            for (int i = 0; i < _cards.Count; i++)
            {
                if (_cards[i] != null)
                {
                    count++;
                }
            }
            return count;
        }
    }
}
