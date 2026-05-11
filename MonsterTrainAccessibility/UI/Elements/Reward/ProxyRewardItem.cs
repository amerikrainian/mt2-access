using System.Collections.Generic;
using System.Reflection;
using System.Text;
using HarmonyLib;
using MonsterTrainAccessibility.Buffers;
using MonsterTrainAccessibility.Localization;
using MonsterTrainAccessibility.Presentation;
using MonsterTrainAccessibility.Presentation.Rewards;
using MonsterTrainAccessibility.Util;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace MonsterTrainAccessibility.UI.Elements
{
    internal sealed class ProxyRewardItem : UIElement, INavigationTargetElement
    {
        private static readonly FieldInfo VictoryRewardUIsField = AccessTools.Field(typeof(global::VictoryUI), "rewardUIs")!;
        private static readonly FieldInfo RewardTitleLabelField = AccessTools.Field(typeof(global::RewardItemUI), "rewardTitleLabel")!;
        private static readonly FieldInfo RewardQuantityLabelField = AccessTools.Field(typeof(global::RewardItemUI), "rewardQuantityLabel")!;

        private readonly RewardItemUI _item;
        private readonly GrantableRewardData _reward;
        private readonly List<TooltipContent> _tooltips;

        public ProxyRewardItem(RewardItemUI item)
        {
            _item = item;
        }

        private ProxyRewardItem(GrantableRewardData reward, List<TooltipContent> tooltips)
        {
            _reward = reward;
            _tooltips = tooltips;
        }

        public static ProxyRewardItem FromData(GrantableRewardData reward, List<TooltipContent> tooltips = null)
        {
            return reward != null ? new ProxyRewardItem(reward, tooltips) : null;
        }

        public RewardItemUI Item => _item;
        private GrantableRewardData Reward => _item?.rewardState?.RewardData ?? _reward;
        private List<TooltipContent> Tooltips => _item?.Tooltips ?? _tooltips;
        public override bool IsVisible => _reward != null || (_item != null && _item.gameObject.activeInHierarchy);
        public override Message GetLabel() => Label(Reward);
        public override Message GetExtrasString() => Detail(Reward);
        public override Message GetStatusString() => Status(_item?.rewardState != null && _item.rewardState.Claimed);
        public override Message GetTooltip() => Tooltip(Reward, Tooltips);
        public GameObject TitleTarget => TitleLabel != null ? TitleLabel.gameObject : null;
        public GameObject QuantityTarget => QuantityLabel != null ? QuantityLabel.gameObject : null;

        private TMP_Text TitleLabel => _item != null ? RewardTitleLabelField.GetValue(_item) as TMP_Text : null;
        private TMP_Text QuantityLabel => _item != null ? RewardQuantityLabelField.GetValue(_item) as TMP_Text : null;

        public void SelectForNavigation()
        {
            if (_item == null || !_item.gameObject.activeInHierarchy)
            {
                return;
            }

            EventSystem eventSystem = EventSystem.current;
            if (eventSystem != null)
            {
                eventSystem.SetSelectedGameObject(_item.gameObject);
            }
        }

        internal override string HandleBuffers(BufferManager buffers)
        {
            return HandleBuffers(buffers, null);
        }

        internal string HandleBuffers(BufferManager buffers, List<Message> beforeLabel)
        {
            base.HandleBuffers(buffers);
            GrantableRewardData data = Reward;
            if (data == null)
            {
                return "ui";
            }

            if (data is CardRewardData cardReward && BindCardPresentationBuffer(buffers, cardReward, beforeLabel))
            {
                return "card";
            }

            PresentationBuffer<RewardPresentationSource> buffer = buffers?.GetBuffer("reward") as PresentationBuffer<RewardPresentationSource>;
            if (buffer == null)
            {
                return "ui";
            }

            buffer.Bind(new RewardPresentationSource(data, Tooltips, beforeLabel));
            buffers.EnableBuffer("reward", true);
            return "reward";
        }

        public static List<RewardItemUI> VictoryRewards(global::VictoryUI victory)
        {
            return victory != null ? VictoryRewardUIsField.GetValue(victory) as List<RewardItemUI> : null;
        }

        public static bool HasVisibleVictoryRewards(global::VictoryUI victory)
        {
            List<RewardItemUI> rewards = VictoryRewards(victory);
            if (rewards == null)
            {
                return false;
            }

            for (int i = 0; i < rewards.Count; i++)
            {
                RewardItemUI reward = rewards[i];
                if (reward != null && reward.gameObject.activeInHierarchy)
                {
                    return true;
                }
            }

            return false;
        }

        public static void AppendVictorySignature(StringBuilder sb, global::VictoryUI victory)
        {
            List<RewardItemUI> rewards = VictoryRewards(victory);
            if (sb == null || rewards == null)
            {
                return;
            }

            for (int i = 0; i < rewards.Count; i++)
            {
                RewardItemUI reward = rewards[i];
                sb.Append(reward != null && reward.gameObject.activeInHierarchy)
                    .Append(':')
                    .Append(reward?.rewardState?.RewardData?.RewardTitle)
                    .Append(':')
                    .Append(reward?.rewardState != null && reward.rewardState.Claimed)
                    .Append('|');
            }
        }

        private static bool BindCardPresentationBuffer(BufferManager buffers, CardRewardData reward, List<Message> beforeLabel)
        {
            PresentationBuffer<CardState> buffer = buffers?.GetBuffer("card") as PresentationBuffer<CardState>;
            CardData cardData = reward?.GetCardData();
            if (buffer == null || cardData == null)
            {
                return false;
            }

            buffer.Bind(new CardState(cardData, reward.saveManager), beforeLabel);
            buffers.EnableBuffer("card", true);
            return true;
        }

        public static Message Label(GrantableRewardData reward)
        {
            if (reward == null)
            {
                return null;
            }

            if (reward is GoldRewardData)
            {
                return Message.Localized("ui", "REWARD.GOLD", new { amount = reward.RewardValue });
            }

            if (reward is HealthRewardData)
            {
                return reward.RewardValue > 0
                    ? Message.Localized("ui", "REWARD.PYRE_HEALTH_PERCENT", new { amount = reward.RewardValue })
                    : Message.Localized("ui", "REWARD.PYRE_HEALTH_FULL");
            }

            if (reward is DragonsHoardCollectionRewardData)
            {
                return Message.Localized("ui", "HUD.DRAGONS_HOARD");
            }

            if (reward is TitanTrialRewardData titanTrialReward)
            {
                Message title = Message.FromText(titanTrialReward.GetTfbUnlockRelicData()?.GetName());
                if (title != null)
                {
                    return title;
                }
            }

            return Message.FromText(reward.RewardTitle);
        }

        public static Message Detail(GrantableRewardData reward)
        {
            if (reward == null || reward is GoldRewardData || reward is HealthRewardData)
            {
                return null;
            }

            if (reward is DragonsHoardCollectionRewardData)
            {
                return Message.Localized("ui", "DRAGONS_HOARD.REDEEM");
            }

            return Message.FromText(reward.RewardDetail);
        }

        public static Message Status(bool claimed)
        {
            return claimed ? Message.Localized("ui", "STATES.CLAIMED") : null;
        }

        public static Message Description(GrantableRewardData reward)
        {
            if (reward == null)
            {
                return null;
            }

            List<Message> parts = new List<Message>();
            if (reward is TitanTrialRewardData titanTrialReward)
            {
                AddPart(parts, Message.FromText(titanTrialReward.GetTfbUnlockRelicData()?.GetDescription()));
            }
            else
            {
                AddPart(parts, Message.FromText(reward.RewardDescription));
            }

            AddPart(parts, Message.FromText(reward.NoEffectDescription));

            return parts.Count > 0 ? Message.JoinLines(parts) : null;
        }

        public static Message Tooltip(GrantableRewardData reward, List<TooltipContent> tooltips = null)
        {
            List<Message> parts = new List<Message>();
            AddPart(parts, Description(reward));

            if (tooltips != null)
            {
                for (int i = 0; i < tooltips.Count; i++)
                {
                    AddPart(parts, MessageList.Tooltip(tooltips[i]));
                }
            }

            return parts.Count > 0 ? Message.JoinLines(parts) : null;
        }

        private static void AddPart(List<Message> parts, Message message)
        {
            MessageList.Add(parts, message);
        }

    }
}
