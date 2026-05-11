using System;
using System.Collections.Generic;
using MonsterTrainAccessibility.Localization;
using MonsterTrainAccessibility.UI;
using MonsterTrainAccessibility.UI.Elements;
using ShinyShoe;
using TMPro;
using UnityEngine;

namespace MonsterTrainAccessibility.UI.Screens
{
    internal static class AccessibleScreenText
    {
        public static Message Text(TMP_Text text)
        {
            return Message.RawCleaned(AccessibilityText.ReadLocalizedText(text));
        }

        public static Message Tooltip(Component component)
        {
            return TooltipText.ForComponent(component);
        }

        public static Message Tooltip(GameObject go)
        {
            return go != null ? TooltipText.ForComponent(go.transform) : null;
        }

        public static string ReadButtonLabel(GameUISelectableButton button)
        {
            return Message.Clean(GameUIButtonSupport.ResolveLabel(button));
        }

        public static Message ReadDraftableLabel(IDraftableUI item)
        {
            if (item is CardChoiceItem cardChoice)
            {
                CardState cardState = cardChoice.cardState;
                if (cardState != null)
                {
                    return Message.RawCleaned(cardState.GetTitle());
                }

                CardData cardData = cardChoice.cardData;
                if (cardData != null)
                {
                    return Message.RawCleaned(cardData.GetName());
                }
            }

            if (item is RelicInfoUI relicInfo)
            {
                return new ProxyRelicInfo(relicInfo).GetLabel();
            }

            if (item is EndlessMutatorPairUI endlessMutatorPair)
            {
                return ProxyEndlessMutatorPair.Label(endlessMutatorPair.MutatorPair);
            }

            return null;
        }

        public static Message ReadRewardItem(RewardItemUI item)
        {
            if (item == null)
            {
                return null;
            }

            RewardState state = item.rewardState;
            return state?.RewardData != null ? ProxyRewardItem.Tooltip(state.RewardData) : null;
        }

        public static Message RewardDataTooltip(RewardDetailsUI rewardDetails)
        {
            GrantableRewardData data = rewardDetails?.RewardData;
            if (data == null)
            {
                return rewardDetails != null ? Tooltip(rewardDetails.transform) : null;
            }

            return ProxyRewardItem.Description(data);
        }

    }
}
