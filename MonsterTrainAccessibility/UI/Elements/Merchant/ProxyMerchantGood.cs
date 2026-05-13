using System.Collections.Generic;
using System;
using MonsterTrainAccessibility.Buffers;
using MonsterTrainAccessibility.Localization;
using MonsterTrainAccessibility.Util;
using ShinyShoe;

namespace MonsterTrainAccessibility.UI.Elements
{
    internal sealed class ProxyMerchantGood : GameObjectElement
    {
        private readonly MerchantGoodUIBase _good;
        private readonly MerchantData _merchantData;
        private GrantableRewardData _cachedRewardData;
        private UIElement _innerProxy;

        public ProxyMerchantGood(MerchantGoodUIBase good, MerchantData merchantData)
            : base(
                good?.SelectableUI,
                typeKey: "button",
                label: null)
        {
            _good = good;
            _merchantData = merchantData;
        }

        public MerchantGoodUIBase Good => _good;
        public MerchantData MerchantData => _merchantData;
        public override bool IsVisible => IsMerchantGoodVisible(_good);
        public override Message GetLabel() => FocusLabel(GetInnerProxy(), Status(_good, _merchantData));
        public override string GetTypeKey() => GetInnerProxy()?.GetTypeKey() ?? "button";
        public override string GetSubtypeKey() => GetInnerProxy()?.GetSubtypeKey();
        public override Message GetExtrasString() => GetInnerProxy()?.GetExtrasString();
        public override Message GetStatusString() => GetInnerProxy() == null ? Status(_good, _merchantData) : null;
        public override Message GetTooltip() => GetInnerProxy()?.GetTooltip();

        internal override string HandleBuffers(BufferManager buffers)
        {
            UIElement inner = GetInnerProxy();
            if (inner == null)
            {
                return base.HandleBuffers(buffers);
            }

            List<Message> merchantMessages = MerchantBufferMessages(_good, _merchantData);
            if (inner is ProxyCombatCard card)
            {
                return card.HandleBuffers(buffers, merchantMessages);
            }

            if (inner is ProxyRelicInfo relic)
            {
                return relic.HandleBuffers(buffers, merchantMessages);
            }

            if (inner is ProxyRewardItem reward)
            {
                return reward.HandleBuffers(buffers, merchantMessages);
            }

            return inner.HandleBuffers(buffers);
        }

        private UIElement GetInnerProxy()
        {
            GrantableRewardData data = _good?.GoodState?.RewardData;
            if (ReferenceEquals(data, _cachedRewardData))
            {
                return _innerProxy;
            }

            _cachedRewardData = data;
            _innerProxy = CreateInnerProxy(data);
            return _innerProxy;
        }

        private static UIElement CreateInnerProxy(GrantableRewardData data)
        {
            if (data is CardRewardData cardReward)
            {
                CardData cardData = cardReward.GetCardData();
                if (cardData == null)
                {
                    return null;
                }

                SaveManager saveManager = cardReward.saveManager ?? AllGameManagers.Instance?.OrNull()?.GetSaveManager();
                return ProxyCombatCard.FromState(new CardState(cardData, saveManager));
            }

            if (data is RelicRewardData relicReward)
            {
                return ProxyRelicInfo.FromRelicData(relicReward.GetRelicData(), includeDynamicInfo: true);
            }

            return ProxyRewardItem.FromData(data);
        }

        private static Message FocusLabel(UIElement inner, Message merchantStatus)
        {
            Message innerLabel = inner?.GetLabel();
            if (innerLabel == null)
            {
                return merchantStatus;
            }

            if (merchantStatus == null)
            {
                return innerLabel;
            }

            if (inner is ProxyCombatCard card && card.Card != null)
            {
                Message title = Message.FromText(card.Card.GetTitle());
                Message remainder = LabelRemainder(innerLabel, title);
                return remainder != null
                    ? Message.Join(", ", title, merchantStatus, remainder)
                    : Message.Join(", ", title, merchantStatus);
            }

            if (inner is ProxyRelicInfo relic)
            {
                Message title = relic.State != null
                    ? Message.FromText(relic.State.GetName())
                    : ProxyRelicInfo.Label(relic.Relic);
                Message remainder = LabelRemainder(innerLabel, title);
                return remainder != null
                    ? Message.Join(", ", title, merchantStatus, remainder)
                    : Message.Join(", ", title, merchantStatus);
            }

            return Message.Join(", ", innerLabel, merchantStatus);
        }

        private static Message LabelRemainder(Message label, Message title)
        {
            string labelText = label?.Resolve();
            string titleText = title?.Resolve();
            if (string.IsNullOrWhiteSpace(labelText) || string.IsNullOrWhiteSpace(titleText))
            {
                return null;
            }

            if (!labelText.StartsWith(titleText, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            string remainder = labelText.Substring(titleText.Length).TrimStart();
            if (remainder.StartsWith(",", StringComparison.Ordinal))
            {
                remainder = remainder.Substring(1).TrimStart();
            }

            return Message.FromText(remainder);
        }

        private static bool IsMerchantGoodVisible(MerchantGoodUIBase good)
        {
            if (good == null || !good.gameObject.activeInHierarchy || good.Claimed)
            {
                return false;
            }

            return good.SelectableUI != null && good.SelectableUI.CanBeSelected();
        }

        private static List<Message> MerchantBufferMessages(MerchantGoodUIBase good, MerchantData merchantData)
        {
            List<Message> messages = new List<Message>();
            MessageList.Add(messages, Status(good, merchantData));
            return messages;
        }

        private static Message Cost(MerchantGoodUIBase good, MerchantData merchantData)
        {
            if (good?.GoodState == null)
            {
                return null;
            }

            int cost = good.GoodState.GetCost(merchantData);
            return Message.Localized("ui", "MERCHANT.COST", new { cost });
        }

        private static Message Status(MerchantGoodUIBase good, MerchantData merchantData)
        {
            if (good == null)
            {
                return null;
            }

            List<Message> parts = new List<Message>();
            MessageList.Add(parts, Cost(good, merchantData));
            if (good.Claimed)
            {
                MessageList.Add(parts, Message.Localized("ui", "STATES.SOLD"));
            }
            else if (!good.Purchasable)
            {
                MessageList.Add(parts, Message.Localized("ui", "STATES.UNAVAILABLE"));
            }
            else if (!good.Affordable || good.ForceDisableAffordable)
            {
                MessageList.Add(parts, Message.Localized("ui", "STATES.UNAFFORDABLE"));
            }

            if (good.GoodState != null && !good.GoodState.Unlocked)
            {
                MessageList.Add(parts, Message.Localized("ui", "STATES.LOCKED"));
            }

            return parts.Count > 0 ? Message.Join(", ", parts) : null;
        }
    }
}
