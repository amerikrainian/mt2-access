using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using MonsterTrainAccessibility.Localization;
using MonsterTrainAccessibility.UI.Screens;
using ShinyShoe;
using TMPro;

namespace MonsterTrainAccessibility.UI.Elements
{
    internal sealed class ProxyRewardSkipButton : GameObjectElement
    {
        private static readonly FieldInfo CurrentRewardsField = AccessTools.Field(typeof(global::RewardScreen), "currentRewards")!;
        private static readonly FieldInfo SkipButtonKeyField = AccessTools.Field(typeof(global::RewardScreen), "skipButtonKey")!;
        private static readonly FieldInfo GoldForSkipFormatKeyField = AccessTools.Field(typeof(global::RewardScreen), "goldForSkipFormatKey")!;
        private static readonly FieldInfo SaveManagerField = AccessTools.Field(typeof(global::RewardScreen), "saveManager")!;

        private readonly global::RewardScreen _screen;
        private readonly GameUISelectableButton _button;
        private readonly TMP_Text _label;

        public ProxyRewardSkipButton(global::RewardScreen screen, GameUISelectableButton button, TMP_Text label)
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
                string key = !string.IsNullOrWhiteSpace(reward.SkipButtonLabelKeyOverride)
                    ? reward.SkipButtonLabelKeyOverride
                    : Get<string>(_screen, SkipButtonKeyField);
                string text = AccessibilityText.LocalizeTerm(key);

                SaveManager saveManager = Get<SaveManager>(_screen, SaveManagerField);
                if (saveManager != null)
                {
                    int skipGold = CurrentSkipGold(saveManager);
                    if (skipGold > 0)
                    {
                        string bonusFormat = Get<string>(_screen, GoldForSkipFormatKeyField);
                        if (!string.IsNullOrWhiteSpace(bonusFormat))
                        {
                            text += string.Format(AccessibilityText.LocalizeTerm(bonusFormat), skipGold);
                        }
                    }
                }

                Message computed = Message.FromText(text);
                if (computed != null)
                {
                    return computed;
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

        private int CurrentSkipGold(SaveManager saveManager)
        {
            List<IRewardDisplayable> rewards = Get<List<IRewardDisplayable>>(_screen, CurrentRewardsField);
            if (rewards == null)
            {
                return 0;
            }

            int total = 0;
            for (int i = 0; i < rewards.Count; i++)
            {
                IRewardDisplayable reward = rewards[i];
                if (reward == null)
                {
                    continue;
                }

                total += saveManager.GetAdjustedGoldAmount(
                    reward.GetGoldForSkipping(saveManager.GetBalanceData()),
                    isReward: true);
            }

            return total;
        }

        private static T Get<T>(object owner, FieldInfo field) where T : class
        {
            return owner != null ? field.GetValue(owner) as T : null;
        }
    }
}
