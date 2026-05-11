using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using MonsterTrainAccessibility.Localization;
using MonsterTrainAccessibility.UI.Screens;
using ShinyShoe;
using TMPro;

namespace MonsterTrainAccessibility.UI.Elements
{
    internal sealed class ProxyRewardCollectButton : GameObjectElement
    {
        private static readonly FieldInfo CurrentRewardsField = AccessTools.Field(typeof(global::RewardScreen), "currentRewards")!;

        private readonly global::RewardScreen _screen;
        private readonly GameUISelectableButton _button;
        private readonly TMP_Text _label;

        public ProxyRewardCollectButton(global::RewardScreen screen, GameUISelectableButton button, TMP_Text label)
            : base(
                button != null ? button.gameObject : null,
                typeKey: "button",
                label: null)
        {
            _screen = screen;
            _button = button;
            _label = label;
        }

        public override bool IsVisible => _button != null && _button.gameObject.activeInHierarchy;

        public override Message GetLabel()
        {
            IRewardDisplayable reward = CurrentReward();
            if (reward != null)
            {
                if (!string.IsNullOrWhiteSpace(reward.CollectButtonLabelOverride))
                {
                    return Message.FromText(reward.CollectButtonLabelOverride);
                }

                if (!string.IsNullOrWhiteSpace(reward.CollectButtonLabelKey))
                {
                    return Message.FromText(AccessibilityText.LocalizeTerm(reward.CollectButtonLabelKey));
                }
            }

            return Message.FromText(Message.JoinText(
                AccessibilityText.ReadLocalizedText(_label),
                AccessibleScreenText.ReadButtonLabel(_button)));
        }

        private IRewardDisplayable CurrentReward()
        {
            List<IRewardDisplayable> rewards = Get<List<IRewardDisplayable>>(_screen, CurrentRewardsField);
            return rewards != null && rewards.Count > 0 ? rewards[0] : null;
        }

        private static T Get<T>(object owner, FieldInfo field) where T : class
        {
            return owner != null ? field.GetValue(owner) as T : null;
        }
    }
}
