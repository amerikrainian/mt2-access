using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using MonsterTrainAccessibility.Localization;
using MonsterTrainAccessibility.UI.Elements;
using ShinyShoe;
using TMPro;
using UnityEngine;

namespace MonsterTrainAccessibility.UI.Screens
{
    internal sealed class RewardScreen : GridNavigationGameScreen
    {
        private static readonly FieldInfo RewardDetailsUIsField = AccessTools.Field(typeof(global::RewardScreen), "rewardDetailsUIs")!;
        private static readonly FieldInfo CollectButtonField = AccessTools.Field(typeof(global::RewardScreen), "collectButton")!;
        private static readonly FieldInfo SkipButtonField = AccessTools.Field(typeof(global::RewardScreen), "skipButton")!;
        private static readonly FieldInfo CollectButtonLabelField = AccessTools.Field(typeof(global::RewardScreen), "collectButtonLabel")!;
        private static readonly FieldInfo SkipButtonLabelField = AccessTools.Field(typeof(global::RewardScreen), "skipButtonLabel")!;
        private static readonly FieldInfo InputAllowedField = AccessTools.Field(typeof(global::RewardScreen), "inputAllowed")!;
        private static readonly FieldInfo RewardCardUIField = AccessTools.Field(typeof(global::RewardDetailsUI), "cardUI")!;
        private static readonly FieldInfo RewardRelicUIField = AccessTools.Field(typeof(global::RewardDetailsUI), "relicUI")!;
        private static readonly FieldInfo RewardUpgradeUIField = AccessTools.Field(typeof(global::RewardDetailsUI), "upgradeUI")!;
        private static readonly FieldInfo RewardSinRelicUIField = AccessTools.Field(typeof(global::RewardDetailsUI), "sinRelicUI")!;
        private static readonly FieldInfo RewardMutatorUIField = AccessTools.Field(typeof(global::RewardDetailsUI), "mutatorUI")!;
        private static readonly FieldInfo RewardGenericUIField = AccessTools.Field(typeof(global::RewardDetailsUI), "genericRewardUI")!;

        private readonly global::RewardScreen _screen;
        private bool _initialRewardFocusAligned;

        public RewardScreen(global::RewardScreen screen)
        {
            _screen = screen;
            Grid.AnnouncePosition = false;
        }

        public override void OnPush()
        {
            base.OnPush();
            _initialRewardFocusAligned = false;
        }

        public override void OnUpdate()
        {
            base.OnUpdate();
            AlignInitialRewardFocus();
        }

        public override bool ShouldAnnounceFocus(UIElement element)
        {
            if (!IsInputAllowed())
            {
                return false;
            }

            if (!_initialRewardFocusAligned)
            {
                return false;
            }

            return base.ShouldAnnounceFocus(element);
        }

        public override bool ShouldRestoreNavigationFocus() => false;

        protected override bool ShouldFocusFirstOnPush() => false;

        protected override void PopulateGrid()
        {
            int columns = 3;
            int count = 0;

            List<global::RewardDetailsUI> rewards = Get<List<global::RewardDetailsUI>>(_screen, RewardDetailsUIsField);
            if (rewards != null)
            {
                for (int i = 0; i < rewards.Count; i++)
                {
                    if (AddRewardDetails(rewards[i], count % columns, count / columns))
                    {
                        count++;
                    }
                }
            }

            int buttonRow = count == 0 ? 0 : (count + columns - 1) / columns;
            AddButton(new ProxyRewardCollectButton(
                _screen,
                Get<GameUISelectableButton>(_screen, CollectButtonField),
                Get<TMP_Text>(_screen, CollectButtonLabelField)),
                0,
                buttonRow);
            AddButton(new ProxyRewardSkipButton(
                _screen,
                Get<GameUISelectableButton>(_screen, SkipButtonField),
                Get<TMP_Text>(_screen, SkipButtonLabelField)),
                1,
                buttonRow);
        }

        protected override string BuildSignature()
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            List<global::RewardDetailsUI> rewards = Get<List<global::RewardDetailsUI>>(_screen, RewardDetailsUIsField);
            if (rewards != null)
            {
                for (int i = 0; i < rewards.Count; i++)
                {
                    global::RewardDetailsUI reward = rewards[i];
                    sb.Append(reward != null && reward.gameObject.activeInHierarchy);
                    sb.Append(':').Append(reward?.RewardData?.RewardTitle).Append('|');
                }
            }

            GameUISelectableButton collect = Get<GameUISelectableButton>(_screen, CollectButtonField);
            GameUISelectableButton skip = Get<GameUISelectableButton>(_screen, SkipButtonField);
            sb.Append(";collect:").Append(collect != null && collect.gameObject.activeInHierarchy);
            sb.Append(";skip:").Append(skip != null && skip.gameObject.activeInHierarchy);
            return sb.ToString();
        }

        private bool AddRewardDetails(global::RewardDetailsUI details, int x, int y)
        {
            if (details == null || !details.gameObject.activeInHierarchy)
            {
                return false;
            }

            global::CardUI cardUI = Get<global::CardUI>(details, RewardCardUIField);
            if (cardUI != null && cardUI.gameObject.activeInHierarchy && cardUI.SelectableUI != null)
            {
                ProxyCombatCard card = new ProxyCombatCard(cardUI, cardUI.SelectableUI);
                Grid.Add(card, x, y);
                RegisterElement(card, details.gameObject, cardUI.gameObject, cardUI.SelectableUI.component != null ? cardUI.SelectableUI.component.gameObject : null);
                return true;
            }

            RelicInfoUI relic = FirstVisibleRelic(details);
            if (relic != null && relic.SelectableUI != null && relic.SelectableUI.component != null)
            {
                ProxyRelicInfo element = new ProxyRelicInfo(relic, typeKey: null);
                Grid.Add(element, x, y);
                RegisterElement(element, details.gameObject, relic.gameObject, relic.SelectableUI.component.gameObject);
                return true;
            }

            global::RewardIconUI generic = Get<global::RewardIconUI>(details, RewardGenericUIField);
            if (generic != null && generic.gameObject.activeInHierarchy)
            {
                ProxyRewardData element = new ProxyRewardData(
                    generic.gameObject,
                    details.RewardData);
                Grid.Add(element, x, y);
                RegisterElement(element, details.gameObject, generic.gameObject);
                return true;
            }

            ProxyRewardFallback fallback = new ProxyRewardFallback(details);
            Grid.Add(fallback, x, y);
            RegisterElement(fallback, details.gameObject);
            return true;
        }

        private RelicInfoUI FirstVisibleRelic(global::RewardDetailsUI details)
        {
            RelicInfoUI[] relics =
            {
                Get<RelicInfoUI>(details, RewardRelicUIField),
                Get<RelicInfoUI>(details, RewardUpgradeUIField),
                Get<RelicInfoUI>(details, RewardSinRelicUIField),
                Get<RelicInfoUI>(details, RewardMutatorUIField)
            };

            for (int i = 0; i < relics.Length; i++)
            {
                if (relics[i] != null && relics[i].gameObject.activeInHierarchy)
                {
                    return relics[i];
                }
            }

            return null;
        }

        private void AddButton(GameObjectElement element, int x, int y)
        {
            if (element == null)
            {
                return;
            }

            Grid.Add(element, x, y);
            if (element is ProxyRewardCollectButton)
            {
                GameUISelectableButton button = Get<GameUISelectableButton>(_screen, CollectButtonField);
                RegisterElement(element, button != null ? button.gameObject : null);
                return;
            }

            GameUISelectableButton skip = Get<GameUISelectableButton>(_screen, SkipButtonField);
            RegisterElement(element, skip != null ? skip.gameObject : null);
        }

        private void AlignInitialRewardFocus()
        {
            if (_initialRewardFocusAligned || !IsInputAllowed())
            {
                return;
            }

            int rewardIndex = FindFirstRewardElementIndex();
            if (rewardIndex < 0)
            {
                return;
            }

            Grid.SetFocusIndex(rewardIndex);
            _initialRewardFocusAligned = true;
        }

        private int FindFirstRewardElementIndex()
        {
            for (int i = 0; i < Grid.Children.Count; i++)
            {
                UIElement child = Grid.Children[i];
                if (child != null && child.IsVisible && !(child is GameButtonElement))
                {
                    return i;
                }
            }

            return -1;
        }

        private bool IsInputAllowed()
        {
            return _screen != null && (bool)InputAllowedField.GetValue(_screen);
        }

        private static T Get<T>(object owner, FieldInfo field) where T : class
        {
            return owner != null ? field.GetValue(owner) as T : null;
        }
    }
}
