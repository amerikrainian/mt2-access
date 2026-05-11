using System.Collections.Generic;
using System.Reflection;
using System.Text;
using HarmonyLib;
using MonsterTrainAccessibility.Input;
using MonsterTrainAccessibility.Localization;
using MonsterTrainAccessibility.UI;
using MonsterTrainAccessibility.UI.Elements;
using ShinyShoe;
using TMPro;
using UnityEngine;

namespace MonsterTrainAccessibility.UI.Screens
{
    internal sealed class ChallengeProgressScreen : ListNavigationGameScreen
    {
        private static readonly FieldInfo BackButtonField = AccessTools.Field(typeof(global::ChallengeProgressScreen), "backButton")!;
        private static readonly FieldInfo ChallengeDetailsButtonField = AccessTools.Field(typeof(global::ChallengeProgressScreen), "challengeDetailsButton")!;
        private static readonly FieldInfo ChallengeSetTitleLabelField = AccessTools.Field(typeof(global::ChallengeProgressScreen), "challengeSetTitleLabel")!;
        private static readonly FieldInfo NextButtonField = AccessTools.Field(typeof(global::ChallengeProgressScreen), "nextButton")!;
        private static readonly FieldInfo PreviousButtonField = AccessTools.Field(typeof(global::ChallengeProgressScreen), "previousButton")!;
        private static readonly FieldInfo ChallengeSelectionUIField = AccessTools.Field(typeof(global::ChallengeProgressScreen), "challengeSelectionUI")!;
        private static readonly FieldInfo ChallengeSelectionUIEchoesField = AccessTools.Field(typeof(global::ChallengeProgressScreen), "challengeSelectionUIEchoes")!;
        private static readonly FieldInfo ChallengeDetailsAndLaunchUIField = AccessTools.Field(typeof(global::ChallengeProgressScreen), "challengeDetailsAndLaunchUI")!;
        private static readonly FieldInfo CurrentChallengeSetField = AccessTools.Field(typeof(global::ChallengeProgressScreen), "currentChallengeSet")!;
        private static readonly FieldInfo ChallengeSetOrderField = AccessTools.Field(typeof(global::ChallengeProgressScreen), "challengeSetOrder")!;
        private static readonly FieldInfo SidebarNavLayerField = AccessTools.Field(typeof(global::ChallengeProgressScreen), "sidebarNavLayer")!;
        private static readonly FieldInfo UIScreenScreenTransitionField = AccessTools.Field(typeof(global::UIScreen), "screenTransition")!;
        private static readonly FieldInfo UIScreenScreenManagerField = AccessTools.Field(typeof(global::UIScreen), "screenManager")!;
        private static readonly FieldInfo UIScreenSoundManagerField = AccessTools.Field(typeof(global::UIScreen), "soundManager")!;
        private static readonly FieldInfo ShowRewardProgressButtonField = AccessTools.Field(typeof(global::ChallengeDetailsAndLaunchUI), "showRewardProgressButton")!;
        private static readonly FieldInfo StartButtonField = AccessTools.Field(typeof(global::ChallengeDetailsAndLaunchUI), "startButton")!;
        private static readonly FieldInfo ResumeButtonField = AccessTools.Field(typeof(global::ChallengeDetailsAndLaunchUI), "resumeButton")!;
        private static readonly FieldInfo ChallengeRewardsCloseButtonField = AccessTools.Field(typeof(global::ChallengeRewardsUI), "closeButton")!;
        private static readonly FieldInfo ChallengeRewardItemsField = AccessTools.Field(typeof(global::ChallengeRewardsUI), "challengeRewardItems")!;
        private static readonly FieldInfo ChallengeMasteryRewardItemField = AccessTools.Field(typeof(global::ChallengeRewardsUI), "masteryRewardItemUI")!;
        private static readonly MethodInfo ShowChallengeRewardsUIMethod = AccessTools.Method(typeof(global::ChallengeDetailsAndLaunchUI), "ShowChallengeRewardsUI")!;
        private static readonly MethodInfo RefreshChallengeSetVisibilityMethod = AccessTools.Method(typeof(global::ChallengeProgressScreen), "RefreshChallengeSetVisibility", new[] { typeof(bool) })!;

        private readonly global::ChallengeProgressScreen _screen;
        private readonly Dictionary<UIElement, string> _focusKeys = new Dictionary<UIElement, string>();
        private string _lastSignature;

        public ChallengeProgressScreen(global::ChallengeProgressScreen screen)
        {
            _screen = screen;
        }

        public override bool ShouldRestoreNavigationFocus() => false;

        public override bool ShouldAcceptGameSelection() => false;

        public override bool BlocksGameInput(InputAction action)
        {
            if (action?.Key == "ui_accept" || action?.Key == "ui_select")
            {
                return true;
            }

            return base.BlocksGameInput(action);
        }

        public override void OnUpdate()
        {
            string nextSignature = BuildSignature();
            if (string.Equals(nextSignature, _lastSignature, System.StringComparison.Ordinal))
            {
                return;
            }

            string focusKey = CurrentFocusKey();
            int oldIndex = RootList.FocusIndex;
            BuildRegistry();
            if (!RestoreFocus(focusKey))
            {
                RootList.SetFocusIndex(oldIndex);
            }
        }

        protected override void BuildRegistry()
        {
            _focusKeys.Clear();
            base.BuildRegistry();
            _lastSignature = BuildSignature();
        }

        protected override void PopulateList()
        {
            if (AddRewardsOverlay())
            {
                return;
            }

            global::ChallengeSelectionUI selection = CurrentSelection();
            AddChallengeNodes(selection);
            AddDetailsButtons();
            AddButton(Get<GameUISelectableButton>(_screen, PreviousButtonField), Message.Localized("ui", "CHALLENGE.PREVIOUS_SET"), "button:previous", () => ChangeChallengeSet(isNextButton: false));
            AddButton(Get<GameUISelectableButton>(_screen, NextButtonField), Message.Localized("ui", "CHALLENGE.NEXT_SET"), "button:next", () => ChangeChallengeSet(isNextButton: true));
            AddButton(Get<GameUISelectableButton>(_screen, ChallengeDetailsButtonField), Message.Localized("ui", "CHALLENGE.DETAILS"), "button:details", ShowDetails);
            AddButton(Get<GameUISelectableButton>(_screen, BackButtonField), Message.Localized("ui", "CHALLENGE.BACK"), "button:back", ReturnToMainMenu);
        }

        protected override string BuildSignature()
        {
            StringBuilder sb = new StringBuilder();
            TMP_Text title = Get<TMP_Text>(_screen, ChallengeSetTitleLabelField);
            sb.Append("title:").Append(AccessibilityText.ReadText(title)).Append('|');
            sb.Append("set:").Append(CurrentSet()).Append('|');
            AppendSelectionSignature(sb, Get<global::ChallengeSelectionUI>(_screen, ChallengeSelectionUIField));
            AppendSelectionSignature(sb, Get<global::ChallengeSelectionUI>(_screen, ChallengeSelectionUIEchoesField));
            global::ChallengeDetailsAndLaunchUI details = Get<global::ChallengeDetailsAndLaunchUI>(_screen, ChallengeDetailsAndLaunchUIField);
            global::ChallengeRewardsUI rewards = details?.GetChallengeRewardsUI();
            sb.Append("rewardsOpen:").Append(rewards != null && rewards.IsOpen ? '1' : '0').Append('|');
            AppendRewardsSignature(sb, rewards);
            AppendButtonSignature(sb, Get<GameUISelectableButton>(details, ShowRewardProgressButtonField));
            AppendButtonSignature(sb, Get<GameUISelectableButton>(details, StartButtonField));
            AppendButtonSignature(sb, Get<GameUISelectableButton>(details, ResumeButtonField));
            AppendButtonSignature(sb, Get<GameUISelectableButton>(_screen, PreviousButtonField));
            AppendButtonSignature(sb, Get<GameUISelectableButton>(_screen, NextButtonField));
            AppendButtonSignature(sb, Get<GameUISelectableButton>(_screen, ChallengeDetailsButtonField));
            AppendButtonSignature(sb, Get<GameUISelectableButton>(_screen, BackButtonField));
            return sb.ToString();
        }

        private void AddChallengeNodes(global::ChallengeSelectionUI selection)
        {
            IReadOnlyList<global::ChallengeNode> nodes = selection?.AvailableChallenges;
            if (nodes == null)
            {
                return;
            }

            for (int i = 0; i < nodes.Count; i++)
            {
                global::ChallengeNode node = nodes[i];
                if (node?.Challenge == null)
                {
                    continue;
                }

                ProxyChallengeNode element = new ProxyChallengeNode(node, selection);
                AddElement(element, ChallengeKey(node.Challenge, i), node.gameObject, node.Button?.gameObject);
            }
        }

        private void AddButton(GameUISelectableButton button, Message fallbackLabel, string focusKey, System.Func<bool> activate)
        {
            if (button == null)
            {
                return;
            }

            UIElement element = new ProxyChallengeProgressButton(button, fallbackLabel, activate);
            AddElement(element, focusKey, button.gameObject);
        }

        private void AddDetailsButtons()
        {
            global::ChallengeDetailsAndLaunchUI details = Get<global::ChallengeDetailsAndLaunchUI>(_screen, ChallengeDetailsAndLaunchUIField);
            if (details == null)
            {
                return;
            }

            AddButton(Get<GameUISelectableButton>(details, ShowRewardProgressButtonField), Message.Localized("ui", "CHALLENGE.REWARDS"), "button:rewards", ShowRewards);
            AddButton(Get<GameUISelectableButton>(details, StartButtonField), Message.Localized("ui", "CHALLENGE.START"), "button:start", StartChallenge);
            AddButton(Get<GameUISelectableButton>(details, ResumeButtonField), Message.Localized("ui", "CHALLENGE.RESUME"), "button:resume", ResumeChallenge);
        }

        private bool AddRewardsOverlay()
        {
            global::ChallengeRewardsUI rewards = Get<global::ChallengeDetailsAndLaunchUI>(_screen, ChallengeDetailsAndLaunchUIField)?.GetChallengeRewardsUI();
            if (rewards == null || !rewards.IsOpen)
            {
                return false;
            }

            AddButton(Get<GameUISelectableButton>(rewards, ChallengeRewardsCloseButtonField), Message.Localized("ui", "CHALLENGE.CLOSE_REWARDS"), "button:closeRewards", () => CloseRewards(rewards));

            List<global::ChallengeRewardsItemUI> items = Get<List<global::ChallengeRewardsItemUI>>(rewards, ChallengeRewardItemsField);
            if (items != null)
            {
                for (int i = 0; i < items.Count; i++)
                {
                    global::ChallengeRewardsItemUI item = items[i];
                    if (item == null)
                    {
                        continue;
                    }

                    ProxyChallengeRewardItem element = new ProxyChallengeRewardItem(item);
                    AddElement(element, "reward:item:" + i, item.gameObject, ComponentTarget(item.RewardSelectable));
                }
            }

            global::ChallengeCardMasteryRewardItemUI mastery = Get<global::ChallengeCardMasteryRewardItemUI>(rewards, ChallengeMasteryRewardItemField);
            if (mastery != null)
            {
                ProxyChallengeMasteryRewardItem element = new ProxyChallengeMasteryRewardItem(mastery);
                AddElement(element, "reward:mastery", mastery.gameObject, ComponentTarget(mastery.RewardSelectable));
            }

            return true;
        }

        private bool ShowRewards()
        {
            global::ChallengeDetailsAndLaunchUI details = Get<global::ChallengeDetailsAndLaunchUI>(_screen, ChallengeDetailsAndLaunchUIField);
            if (details == null)
            {
                return false;
            }

            ShowChallengeRewardsUIMethod.Invoke(details, null);
            return true;
        }

        private bool CloseRewards(global::ChallengeRewardsUI rewards)
        {
            if (rewards == null || !rewards.IsOpen)
            {
                return false;
            }

            rewards.Close();
            return true;
        }

        private bool StartChallenge()
        {
            global::ChallengeDetailsAndLaunchUI details = Get<global::ChallengeDetailsAndLaunchUI>(_screen, ChallengeDetailsAndLaunchUIField);
            global::ChallengeNode node = CurrentSelection()?.CurrentChallenge;
            if (details == null || node == null || node.IsLocked || node.Challenge == null)
            {
                return true;
            }

            details.ChallengeStartedSignal.Dispatch(node.Challenge);
            return true;
        }

        private bool ResumeChallenge()
        {
            global::ChallengeDetailsAndLaunchUI details = Get<global::ChallengeDetailsAndLaunchUI>(_screen, ChallengeDetailsAndLaunchUIField);
            global::ChallengeNode node = CurrentSelection()?.CurrentChallenge;
            if (details == null || node == null || node.IsLocked || node.Challenge == null)
            {
                return true;
            }

            details.ChallengeResumedSignal.Dispatch(node.Challenge);
            return true;
        }

        private bool ChangeChallengeSet(bool isNextButton)
        {
            List<global::ChallengeProgressScreen.ChallengeSet> challengeSetOrder = Get<List<global::ChallengeProgressScreen.ChallengeSet>>(_screen, ChallengeSetOrderField);
            if (challengeSetOrder == null || challengeSetOrder.Count == 0)
            {
                return false;
            }

            int currentIndex = challengeSetOrder.IndexOf(CurrentSet());
            if (currentIndex < 0)
            {
                currentIndex = 0;
            }

            int nextIndex = isNextButton
                ? (currentIndex + 1) % challengeSetOrder.Count
                : (currentIndex - 1 + challengeSetOrder.Count) % challengeSetOrder.Count;
            CurrentChallengeSetField.SetValue(_screen, challengeSetOrder[nextIndex]);
            RefreshChallengeSetVisibilityMethod.Invoke(_screen, new object[] { isNextButton });
            return true;
        }

        private bool ShowDetails()
        {
            global::FocusableNavigationLayer sidebarNavLayer = Get<global::FocusableNavigationLayer>(_screen, SidebarNavLayerField);
            if (sidebarNavLayer == null)
            {
                return false;
            }

            sidebarNavLayer.SetFocused(setFocused: true);
            return true;
        }

        private bool ReturnToMainMenu()
        {
            global::ScreenTransition screenTransition = Get<global::ScreenTransition>(_screen, UIScreenScreenTransitionField);
            screenTransition?.SetActive(setActive: false);

            if (global::ShinyShoe.AppManager.PlatformServices.SupportsFeature(global::ShinyShoe.PlatformFeature.MuteMusicOnTransitions))
            {
                Get<global::SoundManager>(_screen, UIScreenSoundManagerField)?.TransitionToSnapshot("Mute");
            }

            global::ScreenManager screenManager = Get<global::ScreenManager>(_screen, UIScreenScreenManagerField);
            screenManager?.ReturnToMainMenu();
            return true;
        }

        internal static bool ActivateButton(GameUISelectableButton button, System.Func<bool> activate)
        {
            if (button != null && !button.CanBeActivated())
            {
                return true;
            }

            return activate != null && activate();
        }

        internal static Message ButtonLabel(GameUISelectableButton button, Message fallbackLabel)
        {
            if (button != null)
            {
                string label = Message.Clean(GameUIButtonSupport.ResolveLabel(button));
                if (!string.IsNullOrWhiteSpace(label))
                {
                    return Message.RawCleaned(label);
                }
            }

            return fallbackLabel;
        }

        internal static void ClearNativeSelection()
        {
            global::InputManager.Inst?.SelectGameUIComponent(null, allowClearingSelection: true);
            UnityEngine.EventSystems.EventSystem.current?.SetSelectedGameObject(null);
        }

        private void AddElement(UIElement element, string focusKey, params GameObject[] targets)
        {
            if (element == null)
            {
                return;
            }

            if (!string.IsNullOrEmpty(focusKey))
            {
                _focusKeys[element] = focusKey;
            }

            AddElement(element, targets);
        }

        private string CurrentFocusKey()
        {
            UIElement focused = RootList.FocusedChild;
            return focused != null && _focusKeys.TryGetValue(focused, out string key) ? key : null;
        }

        private bool RestoreFocus(string focusKey)
        {
            if (string.IsNullOrEmpty(focusKey))
            {
                return false;
            }

            IReadOnlyList<UIElement> children = RootList.Children;
            for (int i = 0; i < children.Count; i++)
            {
                UIElement child = children[i];
                if (child.IsVisible && _focusKeys.TryGetValue(child, out string key) && key == focusKey)
                {
                    RootList.SetFocusTo(child, selectForNavigation: false);
                    return true;
                }
            }

            return false;
        }

        private static string ChallengeKey(global::SpChallengeData challenge, int index)
        {
            string id = challenge?.GetID();
            return !string.IsNullOrEmpty(id)
                ? "challenge:" + id
                : "challenge:index:" + index.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        private global::ChallengeSelectionUI CurrentSelection()
        {
            return CurrentSet() == global::ChallengeProgressScreen.ChallengeSet.Echoes
                ? Get<global::ChallengeSelectionUI>(_screen, ChallengeSelectionUIEchoesField)
                : Get<global::ChallengeSelectionUI>(_screen, ChallengeSelectionUIField);
        }

        private global::ChallengeProgressScreen.ChallengeSet CurrentSet()
        {
            object value = CurrentChallengeSetField.GetValue(_screen);
            return value is global::ChallengeProgressScreen.ChallengeSet set
                ? set
                : global::ChallengeProgressScreen.ChallengeSet.Standard;
        }

        private static void AppendSelectionSignature(StringBuilder sb, global::ChallengeSelectionUI selection)
        {
            sb.Append("selection:");
            if (selection == null)
            {
                sb.Append("null|");
                return;
            }

            sb.Append(selection.CurrentChallenge?.Challenge?.GetID()).Append(':');
            IReadOnlyList<global::ChallengeNode> nodes = selection.AvailableChallenges;
            for (int i = 0; i < nodes.Count; i++)
            {
                global::ChallengeNode node = nodes[i];
                if (node?.Challenge == null)
                {
                    continue;
                }

                sb.Append(node.Challenge.GetID())
                    .Append(',')
                    .Append(node.IsLocked ? '1' : '0')
                    .Append(',')
                    .Append(node.IsCompleted ? '1' : '0')
                    .Append(';');
            }

            sb.Append('|');
        }

        private static void AppendButtonSignature(StringBuilder sb, GameUISelectableButton button)
        {
            sb.Append(button != null && button.gameObject.activeInHierarchy ? '1' : '0')
                .Append(':')
                .Append(button != null && button.interactable ? '1' : '0')
                .Append('|');
        }

        private static void AppendRewardsSignature(StringBuilder sb, global::ChallengeRewardsUI rewards)
        {
            if (rewards == null)
            {
                return;
            }

            List<global::ChallengeRewardsItemUI> items = Get<List<global::ChallengeRewardsItemUI>>(rewards, ChallengeRewardItemsField);
            sb.Append("rewardItems:");
            if (items != null)
            {
                for (int i = 0; i < items.Count; i++)
                {
                    global::ChallengeRewardsItemUI item = items[i];
                    sb.Append(item != null && item.gameObject.activeInHierarchy ? '1' : '0').Append(',');
                }
            }

            global::ChallengeCardMasteryRewardItemUI mastery = Get<global::ChallengeCardMasteryRewardItemUI>(rewards, ChallengeMasteryRewardItemField);
            sb.Append("mastery:").Append(mastery != null && mastery.gameObject.activeInHierarchy ? '1' : '0').Append('|');
            AppendButtonSignature(sb, Get<GameUISelectableButton>(rewards, ChallengeRewardsCloseButtonField));
        }

        private static GameObject ComponentTarget(IGameUIComponent component)
        {
            return component?.component != null ? component.component.gameObject : null;
        }

        private static T Get<T>(object owner, FieldInfo field) where T : class
        {
            return owner != null ? field.GetValue(owner) as T : null;
        }
    }
}
