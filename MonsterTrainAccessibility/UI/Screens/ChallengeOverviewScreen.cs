using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using HarmonyLib;
using MonsterTrainAccessibility.Input;
using MonsterTrainAccessibility.Localization;
using MonsterTrainAccessibility.UI.Elements;
using ShinyShoe;
using TMPro;
using UnityEngine;

namespace MonsterTrainAccessibility.UI.Screens
{
    internal sealed class ChallengeOverviewScreen : ListNavigationGameScreen
    {
        private static readonly FieldInfo FeaturedChallengesButtonField = AccessTools.Field(typeof(global::ChallengeOverviewScreen), "featuredChallengesButton")!;
        private static readonly FieldInfo RecentlyViewedButtonField = AccessTools.Field(typeof(global::ChallengeOverviewScreen), "recentlyViewedButton")!;
        private static readonly FieldInfo FeaturedChallengesSectionField = AccessTools.Field(typeof(global::ChallengeOverviewScreen), "featuredChallengesSection")!;
        private static readonly FieldInfo RecentChallengesSectionField = AccessTools.Field(typeof(global::ChallengeOverviewScreen), "recentChallengesSection")!;
        private static readonly FieldInfo BackButtonField = AccessTools.Field(typeof(global::ChallengeOverviewScreen), "backButton")!;
        private static readonly FieldInfo CreateChallengeButtonField = AccessTools.Field(typeof(global::ChallengeOverviewScreen), "createChallengeButton")!;
        private static readonly FieldInfo JoinChallengeButtonField = AccessTools.Field(typeof(global::ChallengeOverviewScreen), "joinChallengeButton")!;
        private static readonly FieldInfo EnterSharecodeDialogField = AccessTools.Field(typeof(global::ChallengeOverviewScreen), "enterSharecodeDialog")!;
        private static readonly FieldInfo SaveManagerField = AccessTools.Field(typeof(global::ChallengeOverviewScreen), "saveManager")!;

        private static readonly FieldInfo ChallengesSectionEntriesField = AccessTools.Field(typeof(global::ChallengesSection), "challengeEntries")!;
        private static readonly FieldInfo ChallengesSectionSpinnerField = AccessTools.Field(typeof(global::ChallengesSection), "spinner")!;
        private static readonly FieldInfo ChallengesSectionNoChallengeLabelField = AccessTools.Field(typeof(global::ChallengesSection), "noChallengeLabel")!;

        private static readonly FieldInfo EnterSharecodeSubmitButtonField = AccessTools.Field(typeof(global::EnterSharecodeDialog), "submitSharecodeButton")!;
        private static readonly FieldInfo EnterSharecodeEntryField = AccessTools.Field(typeof(global::EnterSharecodeDialog), "sharecodeEntryField")!;

        private readonly global::ChallengeOverviewScreen _screen;
        private readonly Dictionary<UIElement, string> _focusKeys = new Dictionary<UIElement, string>();
        private string _lastSignature;

        public ChallengeOverviewScreen(global::ChallengeOverviewScreen screen)
        {
            _screen = screen;
        }

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
            if (string.Equals(nextSignature, _lastSignature, StringComparison.Ordinal))
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
            global::EnterSharecodeDialog dialog = Get<global::EnterSharecodeDialog>(_screen, EnterSharecodeDialogField);
            if (dialog != null && dialog.gameObject.activeSelf)
            {
                AddSharecodeDialog(dialog);
                return;
            }

            AddTabs();
            AddActiveSection();
            AddActionButton(Get<GameUISelectableButton>(_screen, JoinChallengeButtonField), Message.Localized("ui", "CHALLENGE.JOIN"), "button:join");
            AddActionButton(Get<GameUISelectableButton>(_screen, CreateChallengeButtonField), Message.Localized("ui", "CHALLENGE.CREATE"), "button:create");
            AddActionButton(Get<GameUISelectableButton>(_screen, BackButtonField), Message.Localized("ui", "CHALLENGE.BACK"), "button:back");
        }

        protected override string BuildSignature()
        {
            StringBuilder sb = new StringBuilder();
            global::EnterSharecodeDialog dialog = Get<global::EnterSharecodeDialog>(_screen, EnterSharecodeDialogField);
            sb.Append("dialog:").Append(dialog != null && dialog.gameObject.activeSelf ? '1' : '0').Append('|');
            if (dialog != null && dialog.gameObject.activeSelf)
            {
                global::InputFieldContainer input = Get<global::InputFieldContainer>(dialog, EnterSharecodeEntryField);
                sb.Append("sharecode:").Append(input?.text).Append('|');
                AppendButtonSignature(sb, Get<global::ProcessingSpinnerButton>(dialog, EnterSharecodeSubmitButtonField)?.Button);
                return sb.ToString();
            }

            global::ChallengesSection featured = Get<global::ChallengesSection>(_screen, FeaturedChallengesSectionField);
            global::ChallengesSection recent = Get<global::ChallengesSection>(_screen, RecentChallengesSectionField);
            sb.Append("tab:").Append(featured != null && featured.gameObject.activeSelf ? "featured" : "recent").Append('|');
            AppendButtonSignature(sb, Get<GameUISelectableButton>(_screen, FeaturedChallengesButtonField));
            AppendButtonSignature(sb, Get<GameUISelectableButton>(_screen, RecentlyViewedButtonField));
            AppendSectionSignature(sb, featured);
            AppendSectionSignature(sb, recent);
            AppendButtonSignature(sb, Get<GameUISelectableButton>(_screen, JoinChallengeButtonField));
            AppendButtonSignature(sb, Get<GameUISelectableButton>(_screen, CreateChallengeButtonField));
            AppendButtonSignature(sb, Get<GameUISelectableButton>(_screen, BackButtonField));
            return sb.ToString();
        }

        private void AddTabs()
        {
            global::ChallengesSection featured = Get<global::ChallengesSection>(_screen, FeaturedChallengesSectionField);
            global::ChallengesSection recent = Get<global::ChallengesSection>(_screen, RecentChallengesSectionField);
            AddActionButton(
                Get<GameUISelectableButton>(_screen, FeaturedChallengesButtonField),
                Message.Localized("ui", "CHALLENGE.FEATURED"),
                "tab:featured",
                () => featured != null && featured.gameObject.activeSelf ? Message.Localized("messages", "state.selected") : null);
            AddActionButton(
                Get<GameUISelectableButton>(_screen, RecentlyViewedButtonField),
                Message.Localized("ui", "CHALLENGE.RECENT"),
                "tab:recent",
                () => recent != null && recent.gameObject.activeSelf ? Message.Localized("messages", "state.selected") : null);
        }

        private void AddActiveSection()
        {
            global::ChallengesSection featured = Get<global::ChallengesSection>(_screen, FeaturedChallengesSectionField);
            global::ChallengesSection recent = Get<global::ChallengesSection>(_screen, RecentChallengesSectionField);
            global::ChallengesSection active = featured != null && featured.gameObject.activeSelf ? featured : recent;
            if (active == null)
            {
                return;
            }

            Spinner spinner = Get<Spinner>(active, ChallengesSectionSpinnerField);
            if (spinner != null && spinner.gameObject.activeInHierarchy)
            {
                AddElement(new StaticTextElement(Message.Localized("ui", "CHALLENGE.LOADING")));
                return;
            }

            TMP_Text noChallenge = Get<TMP_Text>(active, ChallengesSectionNoChallengeLabelField);
            if (noChallenge != null && noChallenge.gameObject.activeInHierarchy)
            {
                AddElement(new StaticTextElement(Message.FromText(AccessibilityText.ReadLocalizedText(noChallenge))));
                return;
            }

            List<global::ChallengeEntryUI> entries = Get<List<global::ChallengeEntryUI>>(active, ChallengesSectionEntriesField);
            if (entries == null)
            {
                return;
            }

            global::SaveManager saveManager = Get<global::SaveManager>(_screen, SaveManagerField) ?? AllGameManagers.Instance.OrNull()?.GetSaveManager();
            global::AllGameData allGameData = AllGameManagers.Instance.OrNull()?.GetAllGameData();
            for (int i = 0; i < entries.Count; i++)
            {
                global::ChallengeEntryUI entry = entries[i];
                if (entry == null || entry.ChallengeData == null)
                {
                    continue;
                }

                ProxyChallengeEntry element = new ProxyChallengeEntry(entry, saveManager, allGameData, () => Trigger(entry.Button));
                AddElement(element, ChallengeKey(entry.ChallengeData, i), entry.gameObject, entry.Button?.gameObject);
            }
        }

        private void AddSharecodeDialog(global::EnterSharecodeDialog dialog)
        {
            global::InputFieldContainer input = Get<global::InputFieldContainer>(dialog, EnterSharecodeEntryField);
            global::ProcessingSpinnerButton submit = Get<global::ProcessingSpinnerButton>(dialog, EnterSharecodeSubmitButtonField);
            if (input != null)
            {
                ProxyChallengeSharecodeInput element = new ProxyChallengeSharecodeInput(input, () => Trigger(input.button));
                AddElement(element, "sharecode:input", input.gameObject, input.button?.gameObject);
            }

            AddActionButton(submit?.Button, Message.Localized("ui", "CHALLENGE.SUBMIT_SHARECODE"), "sharecode:submit");
            AddElement(new DialogCancelElement(dialog), "sharecode:cancel", dialog.gameObject);
        }

        private ProxyChallengeActionButton AddActionButton(GameUISelectableButton button, Message fallback, string focusKey, Func<Message> status = null)
        {
            if (button == null)
            {
                return null;
            }

            ProxyChallengeActionButton element = new ProxyChallengeActionButton(button, fallback, () => Trigger(button), status);
            AddElement(element, focusKey, button.gameObject);
            return element;
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

        private bool Trigger(IGameUIComponent component)
        {
            if (component == null)
            {
                return false;
            }

            if (InputManager.Inst != null)
            {
                InputManager.Inst.SelectGameUIComponent(component, allowClearingSelection: false);
            }

            CoreInputControlMapping mapping = new CoreInputControlMapping(global::InputManager.Controls.Submit, InputType.None, fake: true);
            return _screen.ApplyScreenInput(mapping, component, global::InputManager.Controls.Submit);
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

        private static string ChallengeKey(global::ChallengeData challenge, int index)
        {
            string id = challenge?.GetSharecodeOrID();
            return !string.IsNullOrEmpty(id)
                ? "challenge:" + id
                : "challenge:index:" + index.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        private static void AppendSectionSignature(StringBuilder sb, global::ChallengesSection section)
        {
            sb.Append("section:");
            if (section == null)
            {
                sb.Append("null|");
                return;
            }

            sb.Append(section.gameObject.activeInHierarchy ? '1' : '0').Append(':');
            Spinner spinner = Get<Spinner>(section, ChallengesSectionSpinnerField);
            sb.Append(spinner != null && spinner.gameObject.activeInHierarchy ? '1' : '0').Append(':');
            TMP_Text noChallenge = Get<TMP_Text>(section, ChallengesSectionNoChallengeLabelField);
            sb.Append(noChallenge != null && noChallenge.gameObject.activeInHierarchy ? AccessibilityText.ReadLocalizedText(noChallenge) : string.Empty).Append(':');
            List<global::ChallengeEntryUI> entries = Get<List<global::ChallengeEntryUI>>(section, ChallengesSectionEntriesField);
            if (entries != null)
            {
                for (int i = 0; i < entries.Count; i++)
                {
                    global::ChallengeData challenge = entries[i]?.ChallengeData;
                    if (challenge == null)
                    {
                        continue;
                    }

                    sb.Append(challenge.GetSharecodeOrID())
                        .Append(',')
                        .Append(challenge.GetLeaderboardEntryCount())
                        .Append(',')
                        .Append(challenge.GetTargetPlayerRank())
                        .Append(';');
                }
            }
            sb.Append('|');
        }

        private static void AppendButtonSignature(StringBuilder sb, GameUISelectableButton button)
        {
            sb.Append(button != null && button.gameObject.activeInHierarchy ? '1' : '0')
                .Append(':')
                .Append(button != null && button.interactable ? '1' : '0')
                .Append(':')
                .Append(GameUIButtonSupport.ResolveLabel(button))
                .Append('|');
        }

        private static T Get<T>(object owner, FieldInfo field) where T : class
        {
            return owner != null ? field.GetValue(owner) as T : null;
        }

        private sealed class StaticTextElement : UIElement
        {
            private readonly Message _label;

            public StaticTextElement(Message label)
            {
                _label = label;
            }

            public override Message GetLabel() => _label;
        }

        private sealed class DialogCancelElement : UIElement, IActivatableElement
        {
            private readonly global::EnterSharecodeDialog _dialog;

            public DialogCancelElement(global::EnterSharecodeDialog dialog)
            {
                _dialog = dialog;
            }

            public override Message GetLabel() => Message.Localized("ui", "CHALLENGE.CANCEL");
            public override string GetTypeKey() => "button";
            public bool Activate()
            {
                _dialog?.Close();
                return true;
            }
        }
    }
}
