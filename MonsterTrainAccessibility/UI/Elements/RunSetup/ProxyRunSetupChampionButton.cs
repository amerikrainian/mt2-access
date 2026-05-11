using System;
using System.Collections.Generic;
using MonsterTrainAccessibility.Buffers;
using MonsterTrainAccessibility.Localization;
using MonsterTrainAccessibility.Presentation;
using MonsterTrainAccessibility.Util;
using ShinyShoe;

namespace MonsterTrainAccessibility.UI.Elements
{
    internal sealed class ProxyRunSetupChampionButton : GameObjectElement
    {
        private readonly GameUISelectableButton _button;
        private readonly global::RunSetupClassLevelInfoUI _info;
        private readonly SaveManager _saveManager;
        private readonly Func<Message> _label;

        public ProxyRunSetupChampionButton(
            GameUISelectableButton button,
            global::RunSetupClassLevelInfoUI info,
            SaveManager saveManager,
            Func<Message> label)
            : base(
                button != null ? button.gameObject : null,
                typeKey: "button",
                label: null)
        {
            _button = button;
            _info = info;
            _saveManager = saveManager;
            _label = label;
        }

        public override bool IsVisible => _button != null && _button.gameObject.activeInHierarchy;
        public override Message GetLabel() => _label != null ? _label() : null;
        public override Message GetStatusString() => GameButtonElement.StateMessage(_button);

        public override Message GetTooltip()
        {
            List<CardState> cards = PreviewCards();
            if (cards.Count == 0)
            {
                return null;
            }

            List<Message> parts = new List<Message>();
            for (int i = 0; i < cards.Count; i++)
            {
                MessageList.Add(parts, ProxyCombatCard.FocusSummary(cards[i]));
                MessageList.Add(parts, ProxyCombatCard.AccessibilitySummary(cards[i]));
            }

            return parts.Count > 0 ? Message.JoinLines(MessageList.Dedupe(parts)) : null;
        }

        internal override string HandleBuffers(BufferManager buffers)
        {
            base.HandleBuffers(buffers);
            PresentationBuffer<CardState> buffer = buffers?.GetBuffer("card") as PresentationBuffer<CardState>;
            if (buffer == null)
            {
                return "ui";
            }

            List<CardState> cards = PreviewCards();
            if (cards.Count == 0)
            {
                return "ui";
            }

            buffer.BindMany(cards);
            buffers.EnableBuffer("card", true);
            return "card";
        }

        private List<CardState> PreviewCards()
        {
            List<CardState> cards = new List<CardState>();
            if (_info == null || _saveManager == null)
            {
                return cards;
            }

            List<CardData> cardData = _info.GetCardsForCardDetails(showChampFirst: _info.IsMainClass);
            if (cardData == null)
            {
                return cards;
            }

            AllGameData allGameData = _saveManager.GetAllGameData();
            for (int i = 0; i < cardData.Count; i++)
            {
                CardData resolved = ResolveCardData(allGameData, cardData[i]);
                if (resolved != null)
                {
                    cards.Add(new CardState(resolved, _saveManager));
                }
            }

            return cards;
        }

        private static CardData ResolveCardData(AllGameData allGameData, CardData cardData)
        {
            if (cardData == null)
            {
                return null;
            }

            CardData resolved = allGameData?.FindCardDataByName(cardData.GetAssetKey());
            return resolved ?? cardData;
        }
    }
}
