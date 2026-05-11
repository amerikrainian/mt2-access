using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using HarmonyLib;
using MonsterTrainAccessibility.Core;
using MonsterTrainAccessibility.Input;
using MonsterTrainAccessibility.Localization;
using MonsterTrainAccessibility.UI;
using MonsterTrainAccessibility.UI.Elements;
using ShinyShoe;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MonsterTrainAccessibility.UI.Screens
{
    internal sealed class ChallengeDetailsScreen : ListNavigationGameScreen
    {
        private static readonly FieldInfo TitleLabelField = AccessTools.Field(typeof(global::ChallengeDetailsScreen), "titleLabel")!;
        private static readonly FieldInfo DetailsLabelField = AccessTools.Field(typeof(global::ChallengeDetailsScreen), "detailsLabel")!;
        private static readonly FieldInfo DetailsUIField = AccessTools.Field(typeof(global::ChallengeDetailsScreen), "detailsUI")!;
        private static readonly FieldInfo LeaderboardField = AccessTools.Field(typeof(global::ChallengeDetailsScreen), "leaderboard")!;
        private static readonly FieldInfo SubmitButtonField = AccessTools.Field(typeof(global::ChallengeDetailsScreen), "submitButton")!;
        private static readonly FieldInfo BackButtonField = AccessTools.Field(typeof(global::ChallengeDetailsScreen), "backButton")!;
        private static readonly FieldInfo YesterdaysChallengeButtonField = AccessTools.Field(typeof(global::ChallengeDetailsScreen), "yesterdaysChallengeButton")!;
        private static readonly FieldInfo CleanRunOnlyButtonField = AccessTools.Field(typeof(global::ChallengeDetailsScreen), "cleanRunOnlyButton")!;
        private static readonly FieldInfo RandomizeDetailsButtonField = AccessTools.Field(typeof(global::ChallengeDetailsScreen), "randomizeDetailsButton")!;
        private static readonly FieldInfo SwapChampionButtonField = AccessTools.Field(typeof(global::ChallengeDetailsScreen), "swapChampionButton")!;
        private static readonly FieldInfo LoadingSpinnerField = AccessTools.Field(typeof(global::ChallengeDetailsScreen), "loadingSpinner")!;
        private static readonly FieldInfo ChallengeDataField = AccessTools.Field(typeof(global::ChallengeDetailsScreen), "challengeData")!;
        private static readonly FieldInfo EditableField = AccessTools.Field(typeof(global::ChallengeDetailsScreen), "editable")!;
        private static readonly FieldInfo ViewingCleanRunsOnlyField = AccessTools.Field(typeof(global::ChallengeDetailsScreen), "viewingCleanRunsOnly")!;
        private static readonly FieldInfo MainClassInfoField = AccessTools.Field(typeof(global::ChallengeDetailsUI), "mainClassInfo")!;
        private static readonly FieldInfo SubClassInfoField = AccessTools.Field(typeof(global::ChallengeDetailsUI), "subClassInfo")!;
        private static readonly FieldInfo PyreHeartInfoField = AccessTools.Field(typeof(global::ChallengeDetailsUI), "pyreHeartInfo")!;
        private static readonly FieldInfo MutatorRootField = AccessTools.Field(typeof(global::ChallengeDetailsUI), "mutatorRoot")!;
        private static readonly FieldInfo CovenantSelectionField = AccessTools.Field(typeof(global::ChallengeDetailsUI), "covenantSelection")!;
        private static readonly FieldInfo ClassDropdownField = AccessTools.Field(typeof(global::ChallengeDetailsUI), "classDropdown")!;
        private static readonly FieldInfo PyreHeartDropdownField = AccessTools.Field(typeof(global::ChallengeDetailsUI), "pyreHeartDropdown")!;
        private static readonly FieldInfo ClassDropdownButtonsField = AccessTools.Field(typeof(global::ClassSelectionDropdown), "classButtons")!;
        private static readonly FieldInfo PyreHeartDropdownButtonsField = AccessTools.Field(typeof(global::PyreHeartSelectionDropdown), "pyreHeartButtons")!;

        private static readonly FieldInfo SubmitNewRunButtonField = AccessTools.Field(typeof(global::ChallengeSubmitButton), "newRunButton")!;
        private static readonly FieldInfo SubmitContinueRunButtonField = AccessTools.Field(typeof(global::ChallengeSubmitButton), "continueRunButton")!;
        private static readonly FieldInfo SubmitNewRunButtonLabelField = AccessTools.Field(typeof(global::ChallengeSubmitButton), "newRunButtonLabel")!;
        private static readonly FieldInfo SubmitContinueRunButtonLabelField = AccessTools.Field(typeof(global::ChallengeSubmitButton), "continueRunButtonLabel")!;

        private static readonly FieldInfo MutatorTitleLabelField = AccessTools.Field(typeof(global::MutatorSelectionUI), "titleLabel")!;
        private static readonly FieldInfo MutatorCurrentUIsField = AccessTools.Field(typeof(global::MutatorSelectionUI), "currentMutatorUIs")!;
        private static readonly FieldInfo MutatorAddButtonField = AccessTools.Field(typeof(global::MutatorSelectionUI), "addMutatorButton")!;
        private static readonly FieldInfo MutatorDropdownRootField = AccessTools.Field(typeof(global::MutatorSelectionUI), "mutatorDropdownRoot")!;
        private static readonly FieldInfo MutatorDropdownUIsField = AccessTools.Field(typeof(global::MutatorSelectionUI), "dropdownMutatorUIs")!;
        private static readonly FieldInfo MutatorClearButtonField = AccessTools.Field(typeof(global::MutatorSelectionUI), "clearMutatorButton")!;

        private static readonly FieldInfo LeaderboardAllEntriesButtonField = AccessTools.Field(typeof(global::ChallengeDetailsLeaderboardUI), "allEntriesButton")!;
        private static readonly FieldInfo LeaderboardFriendsOnlyButtonField = AccessTools.Field(typeof(global::ChallengeDetailsLeaderboardUI), "friendsOnlyButton")!;
        private static readonly FieldInfo LeaderboardPaginationControlsField = AccessTools.Field(typeof(global::ChallengeDetailsLeaderboardUI), "paginationControls")!;
        private static readonly FieldInfo LeaderboardCurrentRowsField = AccessTools.Field(typeof(global::ChallengeDetailsLeaderboardUI), "currentRows")!;
        private static readonly FieldInfo LeaderboardLoadingSpinnerField = AccessTools.Field(typeof(global::ChallengeDetailsLeaderboardUI), "loadingSpinner")!;

        private static readonly FieldInfo PaginationFirstButtonField = AccessTools.Field(typeof(global::PaginationControls), "firstButton")!;
        private static readonly FieldInfo PaginationPrevButtonField = AccessTools.Field(typeof(global::PaginationControls), "prevButton")!;
        private static readonly FieldInfo PaginationNextButtonField = AccessTools.Field(typeof(global::PaginationControls), "nextButton")!;
        private static readonly FieldInfo PaginationLastButtonField = AccessTools.Field(typeof(global::PaginationControls), "lastButton")!;
        private static readonly FieldInfo PaginationFocusPlayerButtonField = AccessTools.Field(typeof(global::PaginationControls), "focusPlayerButton")!;
        private static readonly FieldInfo PaginationPageLabelField = AccessTools.Field(typeof(global::PaginationControls), "pageLabel")!;

        private static readonly FieldInfo RowRunSummaryButtonField = AccessTools.Field(typeof(global::LeaderboardRow), "leaderboardRunSummaryButton")!;

        private readonly global::ChallengeDetailsScreen _screen;
        private readonly Dictionary<string, UIElement> _restoreTargets = new Dictionary<string, UIElement>(StringComparer.Ordinal);
        private string _restoreFocusKey;
        private global::ClassLevelInfoUI _lastChampionSwapTarget;
        private bool _waitingForMutatorDropdown;
        private bool _mutatorDropdownOpening;
        private UIElement _mutatorDropdownInitialFocus;

        public ChallengeDetailsScreen(global::ChallengeDetailsScreen screen)
        {
            _screen = screen;
        }

        public override bool ShouldAcceptGameSelection() => false;

        protected override bool ShouldSuppressUnchangedRebuildAnnouncement(
            UIElement oldFocused,
            UIElement newFocused,
            string oldAnnouncement,
            string newAnnouncement)
        {
            return !IsEditableMode();
        }

        public override void OnUpdate()
        {
            bool wasWaiting = _waitingForMutatorDropdown;
            base.OnUpdate();

            global::MutatorSelectionUI mutators = CurrentMutators();
            bool open = IsMutatorDropdownOpen(mutators);
            if (_mutatorDropdownOpening && !open)
            {
                _mutatorDropdownOpening = false;
            }

            bool waiting = open && !IsMutatorDropdownReady(mutators);
            bool openingComplete = _mutatorDropdownOpening && open && IsMutatorDropdownReady(mutators);
            if ((wasWaiting && !waiting && open) || openingComplete)
            {
                _mutatorDropdownOpening = false;
                FocusFirstMutatorChoice(selectForNavigation: true);
                UIManager.ForceReannounceCurrentFocus();
            }

            _waitingForMutatorDropdown = waiting;
        }

        public override bool ShouldAnnounceFocus(UIElement element)
        {
            return !IsMutatorInputBlocked() && base.ShouldAnnounceFocus(element);
        }

        public override bool BlocksGameInput(InputAction action)
        {
            if (action?.Key == "ui_accept" || action?.Key == "ui_select")
            {
                return true;
            }

            return base.BlocksGameInput(action);
        }

        public override bool OnActionJustPressed(InputAction action)
        {
            if (IsContainerNavigationAction(action?.Key) && !IsReadyForNavigation())
            {
                return true;
            }

            if (IsContainerNavigationAction(action?.Key) && IsMutatorInputBlocked())
            {
                return true;
            }

            return base.OnActionJustPressed(action);
        }

        protected override void PopulateList()
        {
            _restoreTargets.Clear();
            _mutatorDropdownInitialFocus = null;

            if (!IsReadyForNavigation())
            {
                RootList.AnnouncePosition = false;
                AddElement(new StaticTextElement(Message.Localized("ui", "CHALLENGE.LOADING")));
                return;
            }

            RootList.AnnouncePosition = false;
            global::ChallengeDetailsUI details = Get<global::ChallengeDetailsUI>(_screen, DetailsUIField);
            if (AddActiveEditDropdown(details))
            {
                return;
            }

            AddHeader();
            AddRunDetails(details);
            AddActions();
            AddLeaderboard(Get<global::ChallengeDetailsLeaderboardUI>(_screen, LeaderboardField));
        }

        protected override void RestoreFocusAfterRebuild(int oldIndex, UIElement oldFocused)
        {
            if (IsMutatorDropdownOpen(CurrentMutators()) &&
                _mutatorDropdownInitialFocus != null &&
                _mutatorDropdownInitialFocus.IsVisible)
            {
                RootList.SetFocusTo(_mutatorDropdownInitialFocus, selectForNavigation: !IsMutatorInputBlocked());
                return;
            }

            if (!IsLoading() && !string.IsNullOrEmpty(_restoreFocusKey))
            {
                if (_restoreTargets.TryGetValue(_restoreFocusKey, out UIElement target) && target != null && target.IsVisible)
                {
                    _restoreFocusKey = null;
                    RootList.SetFocusTo(target);
                    return;
                }

                _restoreFocusKey = null;
            }

            base.RestoreFocusAfterRebuild(oldIndex, oldFocused);
        }

        protected override string BuildSignature()
        {
            StringBuilder sb = new StringBuilder();
            AppendText(sb, _screen, TitleLabelField);
            AppendText(sb, _screen, DetailsLabelField);
            sb.Append("loading:").Append(IsLoading() ? '1' : '0').Append('|');

            global::ChallengeDetailsUI details = Get<global::ChallengeDetailsUI>(_screen, DetailsUIField);
            AppendClassSignature(sb, Get<global::ClassLevelInfoUI>(details, MainClassInfoField));
            AppendClassSignature(sb, Get<global::ClassLevelInfoUI>(details, SubClassInfoField));
            AppendPyreSignature(sb, Get<global::PyreHeartInfoUI>(details, PyreHeartInfoField));
            global::MutatorSelectionUI mutators = Get<global::MutatorSelectionUI>(details, MutatorRootField);
            AppendMutatorSignature(sb, mutators);
            AppendClassDropdownSignature(sb, Get<global::ClassSelectionDropdown>(details, ClassDropdownField));
            AppendPyreHeartDropdownSignature(sb, Get<global::PyreHeartSelectionDropdown>(details, PyreHeartDropdownField));
            AppendMutatorDropdownSignature(sb, mutators);
            AppendButtonSignature(sb, Get<GameUISelectableButton>(_screen, BackButtonField));
            AppendButtonSignature(sb, Get<GameUISelectableButton>(_screen, YesterdaysChallengeButtonField));
            AppendButtonSignature(sb, Get<GameUISelectableButton>(_screen, CleanRunOnlyButtonField));
            AppendButtonSignature(sb, Get<GameUISelectableButton>(_screen, RandomizeDetailsButtonField));
            AppendButtonSignature(sb, Get<GameUISelectableButton>(_screen, SwapChampionButtonField));

            global::ChallengeSubmitButton submit = Get<global::ChallengeSubmitButton>(_screen, SubmitButtonField);
            AppendButtonSignature(sb, Get<GameUISelectableButton>(submit, SubmitNewRunButtonField));
            AppendButtonSignature(sb, Get<GameUISelectableButton>(submit, SubmitContinueRunButtonField));
            AppendText(sb, submit, SubmitNewRunButtonLabelField);
            AppendText(sb, submit, SubmitContinueRunButtonLabelField);

            AppendLeaderboardSignature(sb, Get<global::ChallengeDetailsLeaderboardUI>(_screen, LeaderboardField));
            return sb.ToString();
        }

        internal static void HandleNativeLoadingChanged(global::ChallengeDetailsScreen nativeScreen, bool loading)
        {
            ChallengeDetailsScreen screen = ScreenManager.CurrentScreen as ChallengeDetailsScreen;
            if (screen == null || !ReferenceEquals(screen._screen, nativeScreen))
            {
                return;
            }

            screen.HandleNativeLoadingChanged(loading);
        }

        internal static void HandleNativeChallengeDataReady(global::ChallengeDetailsScreen nativeScreen)
        {
            ChallengeDetailsScreen screen = ScreenManager.CurrentScreen as ChallengeDetailsScreen;
            if (screen == null || !ReferenceEquals(screen._screen, nativeScreen))
            {
                return;
            }

            screen.HandleNativeChallengeDataReady();
        }

        private void HandleNativeLoadingChanged(bool loading)
        {
            if (!loading)
            {
                return;
            }

            RebuildFromNativeState(focusFirst: true);
        }

        private void HandleNativeChallengeDataReady()
        {
            if (!IsReadyForNavigation())
            {
                return;
            }

            RebuildFromNativeState(focusFirst: false);
        }

        private void RebuildFromNativeState(bool focusFirst)
        {
            int oldIndex = RootList.FocusIndex;
            UIElement oldFocused = RootList.FocusedChild;
            BuildRegistry();
            if (focusFirst)
            {
                RootList.FocusFirst();
                return;
            }

            RestoreFocusAfterRebuild(oldIndex, oldFocused);
        }

        private void AddHeader()
        {
            Message title = Message.FromText(AccessibilityText.ReadLocalizedText(Get<TMP_Text>(_screen, TitleLabelField)));
            Message details = Message.FromText(AccessibilityText.ReadLocalizedText(Get<TMP_Text>(_screen, DetailsLabelField)));
            if (title == null && details == null)
            {
                return;
            }

            AddElement(new StaticTextElement(Message.Join(": ", title, details), Message.Localized("ui", "CHALLENGE.DETAILS")));
        }

        private void AddRunDetails(global::ChallengeDetailsUI details)
        {
            if (details == null || !details.gameObject.activeInHierarchy)
            {
                return;
            }

            AddClassInfo(Get<global::ClassLevelInfoUI>(details, MainClassInfoField), "CHALLENGE.MAIN_CLAN");
            AddClassInfo(Get<global::ClassLevelInfoUI>(details, SubClassInfoField), "CHALLENGE.ALLIED_CLAN");
            AddPyreHeart(Get<global::PyreHeartInfoUI>(details, PyreHeartInfoField));
            AddCovenant(Get<global::CovenantSelectionUI>(details, CovenantSelectionField));
            AddMutators(Get<global::MutatorSelectionUI>(details, MutatorRootField));
        }

        private bool AddActiveEditDropdown(global::ChallengeDetailsUI details)
        {
            if (details == null)
            {
                return false;
            }

            if (AddClassDropdown(Get<global::ClassSelectionDropdown>(details, ClassDropdownField)))
            {
                return true;
            }

            if (AddPyreHeartDropdown(Get<global::PyreHeartSelectionDropdown>(details, PyreHeartDropdownField)))
            {
                return true;
            }

            return AddMutatorDropdown(Get<global::MutatorSelectionUI>(details, MutatorRootField));
        }

        private bool AddClassDropdown(global::ClassSelectionDropdown dropdown)
        {
            if (dropdown == null || !dropdown.gameObject.activeInHierarchy)
            {
                return false;
            }

            List<global::ClassInfoButtonUI> buttons = Get<List<global::ClassInfoButtonUI>>(dropdown, ClassDropdownButtonsField);
            if (buttons == null)
            {
                return true;
            }

            for (int i = 0; i < buttons.Count; i++)
            {
                global::ClassInfoButtonUI option = buttons[i];
                if (option == null || option.classData == null || !option.gameObject.activeInHierarchy)
                {
                    continue;
                }

                GameUISelectableButton button = option.GetComponent<GameUISelectableButton>();
                AddElement(new ChallengeClassChoiceElement(option, button, () => Trigger(button)), option.gameObject, button != null ? button.gameObject : null);
            }

            AddElement(new StaticTextElement(Message.Localized("ui", "CHALLENGE.CANCEL"), null)
            {
                ActivateHandler = () =>
                {
                    dropdown.Close();
                    return true;
                }
            });
            return true;
        }

        private bool AddPyreHeartDropdown(global::PyreHeartSelectionDropdown dropdown)
        {
            if (dropdown == null || !dropdown.gameObject.activeInHierarchy)
            {
                return false;
            }

            List<global::PyreHeartInfoButtonUI> buttons = Get<List<global::PyreHeartInfoButtonUI>>(dropdown, PyreHeartDropdownButtonsField);
            if (buttons == null)
            {
                return true;
            }

            for (int i = 0; i < buttons.Count; i++)
            {
                global::PyreHeartInfoButtonUI option = buttons[i];
                if (option == null || option.PyreHeartCharacterData == null || !option.gameObject.activeInHierarchy)
                {
                    continue;
                }

                GameUISelectableButton button = option.GetComponent<GameUISelectableButton>();
                AddElement(new ChallengePyreHeartChoiceElement(option, button, () => Trigger(button)), option.gameObject, button != null ? button.gameObject : null);
            }

            AddElement(new StaticTextElement(Message.Localized("ui", "CHALLENGE.CANCEL"), null)
            {
                ActivateHandler = () =>
                {
                    dropdown.Close();
                    return true;
                }
            });
            return true;
        }

        private bool AddMutatorDropdown(global::MutatorSelectionUI mutators)
        {
            GameUISelectable dropdownRoot = Get<GameUISelectable>(mutators, MutatorDropdownRootField);
            if (mutators == null || dropdownRoot == null || !dropdownRoot.gameObject.activeInHierarchy)
            {
                return false;
            }

            if (!IsMutatorDropdownReady(mutators))
            {
                AddElement(new StaticTextElement(Message.Localized("ui", "CHALLENGE.MUTATORS_LOADING")));
                return true;
            }

            GameUISelectableButton clear = Get<GameUISelectableButton>(mutators, MutatorClearButtonField);
            if (clear != null && clear.gameObject.activeInHierarchy)
            {
                AddActionButton(
                    clear,
                    Message.Localized("ui", "CHALLENGE.CLEAR_MUTATOR"),
                    () => TriggerMutatorClear(clear));
            }

            List<MutatorButtonUI> choices = Get<List<MutatorButtonUI>>(mutators, MutatorDropdownUIsField);
            if (choices != null)
            {
                for (int i = 0; i < choices.Count; i++)
                {
                    MutatorButtonUI mutator = choices[i];
                    if (mutator == null || mutator.MutatorState == null || !mutator.gameObject.activeInHierarchy)
                    {
                        continue;
                    }

                    ProxyMutatorButton element = new ProxyMutatorButton(
                        mutator,
                        () => Trigger(mutator.Button),
                        buttonRoleRequiresInteractable: true);
                    AddElement(element, mutator.gameObject, mutator.Button != null ? mutator.Button.gameObject : null);
                    if (_mutatorDropdownInitialFocus == null)
                    {
                        _mutatorDropdownInitialFocus = element;
                    }
                }
            }

            return true;
        }

        private void AddClassInfo(global::ClassLevelInfoUI info, string labelKey)
        {
            if (info == null)
            {
                return;
            }

            IGameUIComponent component = info.GetDefaultGameUISelectable();
            bool editable = component is GameUISelectableButton;
            ProxyChallengeClassInfo element = new ProxyChallengeClassInfo(
                info,
                component,
                labelKey,
                () => Trigger(component),
                editable ? () => _lastChampionSwapTarget = info : (Action)null,
                editable ? () => SwapChampion(info) : (Func<bool>)null);
            AddElement(element, info.gameObject, ComponentTarget(component));
        }

        private void AddPyreHeart(global::PyreHeartInfoUI info)
        {
            if (info == null)
            {
                return;
            }

            IGameUIComponent component = info.GetDefaultGameUISelectable();
            ProxyChallengePyreHeart element = new ProxyChallengePyreHeart(info, component, () => Trigger(component));
            AddElement(element, info.gameObject, ComponentTarget(component));
        }

        private void AddCovenant(global::CovenantSelectionUI covenant)
        {
            if (covenant == null || !covenant.gameObject.activeInHierarchy)
            {
                return;
            }

            ProxyChallengeCovenant element = new ProxyChallengeCovenant(covenant);
            AddElement(element, covenant.gameObject);
        }

        private void AddMutators(global::MutatorSelectionUI mutators)
        {
            if (mutators == null || !mutators.gameObject.activeInHierarchy)
            {
                return;
            }

            List<MutatorButtonUI> current = Get<List<MutatorButtonUI>>(mutators, MutatorCurrentUIsField);
            string groupLabel = Message.Clean(AccessibilityText.ReadLocalizedText(Get<TMP_Text>(mutators, MutatorTitleLabelField)));
            if (!Message.ShouldAdd(groupLabel))
            {
                groupLabel = Message.Localized("ui", "HUD.MUTATORS").Resolve();
            }

            ListContainer group = new ListContainer(groupLabel, NavigationAxis.Horizontal);
            if (current != null)
            {
                for (int i = 0; i < current.Count; i++)
                {
                    MutatorButtonUI mutator = current[i];
                    if (mutator == null || mutator.MutatorState == null)
                    {
                        continue;
                    }

                    ProxyMutatorButton element = new ProxyMutatorButton(
                        mutator,
                        () => TriggerCurrentMutator(mutator.Button),
                        buttonRoleRequiresInteractable: true);
                    group.Add(element);
                    Register(element, mutator.gameObject, mutator.Button != null ? mutator.Button.gameObject : null);
                }
            }

            GameUISelectableButton addButton = Get<GameUISelectableButton>(mutators, MutatorAddButtonField);
            if (addButton != null && addButton.gameObject.activeInHierarchy)
            {
                ProxyChallengeActionButton addElement = new ProxyChallengeActionButton(
                    addButton,
                    Message.Localized("ui", "CHALLENGE.ADD_MUTATOR"),
                    () => TriggerAddMutator(addButton));
                group.Add(addElement);
                Register(addElement, addButton.gameObject);
            }

            if (group.Children.Count > 0)
            {
                RootList.Add(group);
            }
            else
            {
                AddElement(new StaticTextElement(Message.Localized("ui", "CHALLENGE.MUTATORS_NONE")));
            }
        }

        private void AddActions()
        {
            AddElement(new ProxyChallengeSubmitButton(
                Get<global::ChallengeSubmitButton>(_screen, SubmitButtonField),
                () => Trigger(ActiveSubmitButton())),
                SubmitTargets());
            AddActionButton(Get<GameUISelectableButton>(_screen, YesterdaysChallengeButtonField), Message.Localized("ui", "CHALLENGE.YESTERDAY_TODAY"), "yesterday");
            AddActionButton(Get<GameUISelectableButton>(_screen, CleanRunOnlyButtonField), Message.Localized("ui", "CHALLENGE.CLEAN_RUNS_TOGGLE"), "cleanRuns", CleanRunsStatus);
            AddActionButton(Get<GameUISelectableButton>(_screen, RandomizeDetailsButtonField), Message.Localized("ui", "CHALLENGE.RANDOMIZE"));
            AddActionButton(Get<GameUISelectableButton>(_screen, SwapChampionButtonField), Message.Localized("ui", "CHALLENGE.SWAP_CHAMPION"), () => SwapChampion(_lastChampionSwapTarget));
            AddActionButton(Get<GameUISelectableButton>(_screen, BackButtonField), Message.Localized("ui", "CHALLENGE.BACK"));
        }

        private void AddLeaderboard(global::ChallengeDetailsLeaderboardUI leaderboard)
        {
            if (leaderboard == null || !leaderboard.gameObject.activeInHierarchy)
            {
                return;
            }

            AddElement(new StaticTextElement(LeaderboardHeader(leaderboard)));
            if (IsLeaderboardLoading(leaderboard))
            {
                AddElement(new StaticTextElement(Message.Localized("ui", "CHALLENGE.LEADERBOARD_LOADING")));
            }

            AddActionButton(Get<GameUISelectableButton>(leaderboard, LeaderboardAllEntriesButtonField), Message.Localized("ui", "CHALLENGE.LEADERBOARD_ALL"), "leaderboardAll");
            AddActionButton(Get<GameUISelectableButton>(leaderboard, LeaderboardFriendsOnlyButtonField), Message.Localized("ui", "CHALLENGE.LEADERBOARD_FRIENDS"), "leaderboardFriends");
            AddPagination(leaderboard.GetPaginationControls());

            List<global::LeaderboardRow> rows = Get<List<global::LeaderboardRow>>(leaderboard, LeaderboardCurrentRowsField);
            if (rows == null || rows.Count == 0)
            {
                return;
            }

            ListContainer group = new ListContainer(Message.Localized("ui", "CHALLENGE.LEADERBOARD_ROWS").Resolve(), NavigationAxis.Horizontal);
            for (int i = 0; i < rows.Count; i++)
            {
                global::LeaderboardRow row = rows[i];
                if (row == null || row.GetEntryData() == null)
                {
                    continue;
                }

                GameUISelectableButton infoButton = RowInfoButton(row);
                ProxyChallengeLeaderboardRow element = new ProxyChallengeLeaderboardRow(row, infoButton, () => Trigger(infoButton));
                group.Add(element);
                Register(element, row.gameObject, infoButton?.gameObject);
            }

            if (group.Children.Count > 0)
            {
                RootList.Add(group);
            }
        }

        private void AddPagination(global::PaginationControls pagination)
        {
            if (pagination == null || !pagination.gameObject.activeInHierarchy)
            {
                return;
            }

            AddActionButton(Get<GameUISelectableButton>(pagination, PaginationFirstButtonField), Message.Localized("ui", "PAGINATOR.FIRST_PAGE"), "pageFirst");
            AddActionButton(Get<GameUISelectableButton>(pagination, PaginationPrevButtonField), Message.Localized("ui", "PAGINATOR.PREVIOUS_PAGE"), "pagePrev");
            AddActionButton(Get<GameUISelectableButton>(pagination, PaginationFocusPlayerButtonField), Message.Localized("ui", "PAGINATOR.PLAYER_PAGE"), "pagePlayer");
            AddActionButton(Get<GameUISelectableButton>(pagination, PaginationNextButtonField), Message.Localized("ui", "PAGINATOR.NEXT_PAGE"), "pageNext");
            AddActionButton(Get<GameUISelectableButton>(pagination, PaginationLastButtonField), Message.Localized("ui", "PAGINATOR.LAST_PAGE"), "pageLast");
        }

        private ProxyChallengeActionButton AddActionButton(GameUISelectableButton button, Message fallback)
        {
            return AddActionButton(button, fallback, () => Trigger(button));
        }

        private ProxyChallengeActionButton AddActionButton(GameUISelectableButton button, Message fallback, string restoreKey, Func<Message> status = null)
        {
            return AddActionButton(button, fallback, () => TriggerWithRestore(button, restoreKey), restoreKey, status);
        }

        private ProxyChallengeActionButton AddActionButton(GameUISelectableButton button, Message fallback, Func<bool> activate)
        {
            return AddActionButton(button, fallback, activate, null, null);
        }

        private ProxyChallengeActionButton AddActionButton(GameUISelectableButton button, Message fallback, Func<bool> activate, string restoreKey, Func<Message> status)
        {
            if (button == null)
            {
                return null;
            }

            ProxyChallengeActionButton element = new ProxyChallengeActionButton(button, fallback, activate, status);
            AddElement(element, button.gameObject);
            if (!string.IsNullOrEmpty(restoreKey))
            {
                _restoreTargets[restoreKey] = element;
            }
            return element;
        }

        private bool IsReadyForNavigation()
        {
            return !IsLoading() && Get<global::ChallengeData>(_screen, ChallengeDataField) != null;
        }

        private Message CleanRunsStatus()
        {
            return GetBool(_screen, ViewingCleanRunsOnlyField) ? Message.Localized("messages", "state.selected") : null;
        }

        private global::MutatorSelectionUI CurrentMutators()
        {
            global::ChallengeDetailsUI details = Get<global::ChallengeDetailsUI>(_screen, DetailsUIField);
            return Get<global::MutatorSelectionUI>(details, MutatorRootField);
        }

        private bool IsMutatorDropdownWaiting()
        {
            global::MutatorSelectionUI mutators = CurrentMutators();
            return IsMutatorDropdownOpen(mutators) && !IsMutatorDropdownReady(mutators);
        }

        private bool IsMutatorDropdownReadyForInput()
        {
            global::MutatorSelectionUI mutators = CurrentMutators();
            return IsMutatorDropdownOpen(mutators) && IsMutatorDropdownReady(mutators);
        }

        private bool IsMutatorInputBlocked()
        {
            return _mutatorDropdownOpening || IsMutatorDropdownWaiting();
        }

        private static bool IsMutatorDropdownOpen(global::MutatorSelectionUI mutators)
        {
            GameUISelectable dropdownRoot = Get<GameUISelectable>(mutators, MutatorDropdownRootField);
            return dropdownRoot != null && dropdownRoot.gameObject.activeInHierarchy;
        }

        private static bool IsMutatorDropdownReady(global::MutatorSelectionUI mutators)
        {
            List<MutatorButtonUI> choices = Get<List<MutatorButtonUI>>(mutators, MutatorDropdownUIsField);
            if (choices == null || choices.Count == 0)
            {
                return false;
            }

            for (int i = 0; i < choices.Count; i++)
            {
                MutatorButtonUI choice = choices[i];
                if (choice != null && choice.MutatorState != null && choice.gameObject.activeInHierarchy)
                {
                    return true;
                }
            }

            return false;
        }

        private void FocusFirstMutatorChoice(bool selectForNavigation)
        {
            if (_mutatorDropdownInitialFocus != null && _mutatorDropdownInitialFocus.IsVisible)
            {
                RootList.SetFocusTo(_mutatorDropdownInitialFocus, selectForNavigation);
                return;
            }

            IReadOnlyList<UIElement> children = RootList.Children;
            for (int i = 0; i < children.Count; i++)
            {
                if (children[i] is ProxyMutatorButton && children[i].IsVisible)
                {
                    RootList.SetFocusIndex(i, selectForNavigation);
                    return;
                }
            }
        }

        private bool SwapChampion(global::ClassLevelInfoUI info)
        {
            if (info == null || info.ClassData == null)
            {
                return true;
            }

            int next = info.ChampionIndex == 0 ? 1 : 0;
            if (info.ClassData.GetChampionData(next)?.championCardData == null)
            {
                return true;
            }

            info.SetChampionIndex(next, refreshChampionSelector: true);
            UIManager.ForceReannounceCurrentFocus();
            return true;
        }

        private bool TriggerWithRestore(GameUISelectableButton button, string restoreFocusKey)
        {
            _restoreFocusKey = restoreFocusKey;
            return Trigger(button);
        }

        private bool TriggerCurrentMutator(GameUISelectableButton button)
        {
            _mutatorDropdownOpening = true;
            bool handled = Trigger(button);
            if (!handled)
            {
                _mutatorDropdownOpening = false;
            }

            return handled;
        }

        private bool TriggerAddMutator(GameUISelectableButton button)
        {
            _mutatorDropdownOpening = true;
            bool handled = Trigger(button);
            if (!handled)
            {
                _mutatorDropdownOpening = false;
            }

            return handled;
        }

        private bool TriggerMutatorClear(GameUISelectableButton button)
        {
            if (IsMutatorInputBlocked())
            {
                return true;
            }

            return Trigger(button);
        }

        internal static bool ShouldBlockNativeMutatorClear(
            global::MutatorSelectionUI mutators,
            CoreInputControlMapping mapping,
            IGameUIComponent triggeredUI,
            global::InputManager.Controls triggeredMappingID)
        {
            if (mapping?.fake == true)
            {
                return false;
            }

            ChallengeDetailsScreen screen = ScreenManager.CurrentScreen as ChallengeDetailsScreen;
            if (screen == null || !ReferenceEquals(screen.CurrentMutators(), mutators))
            {
                return false;
            }

            if (!IsMutatorDropdownOpen(mutators))
            {
                return false;
            }

            GameUISelectableButton clear = Get<GameUISelectableButton>(mutators, MutatorClearButtonField);
            bool targetsClear = triggeredUI != null && triggeredUI.IsGameUIComponent(clear);
            bool accessHudClear = triggeredMappingID == global::InputManager.Controls.AccessHud &&
                (triggeredUI == null || targetsClear);
            if (!targetsClear && !accessHudClear)
            {
                return false;
            }

            Log.Info("[AccessibilityMod] Challenge mutator native clear blocked; clear remains available through accessible focus.");
            return true;
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

        private bool IsLoading()
        {
            Spinner spinner = Get<Spinner>(_screen, LoadingSpinnerField);
            return spinner != null && spinner.gameObject.activeInHierarchy;
        }

        private bool IsEditableMode()
        {
            return GetBool(_screen, EditableField);
        }

        private static bool IsLeaderboardLoading(global::ChallengeDetailsLeaderboardUI leaderboard)
        {
            Spinner spinner = Get<Spinner>(leaderboard, LeaderboardLoadingSpinnerField);
            return spinner != null && spinner.gameObject.activeInHierarchy;
        }

        private GameUISelectableButton ActiveSubmitButton()
        {
            global::ChallengeSubmitButton submit = Get<global::ChallengeSubmitButton>(_screen, SubmitButtonField);
            GameUISelectableButton continueButton = Get<GameUISelectableButton>(submit, SubmitContinueRunButtonField);
            if (continueButton != null && continueButton.gameObject.activeInHierarchy)
            {
                return continueButton;
            }

            return Get<GameUISelectableButton>(submit, SubmitNewRunButtonField);
        }

        private GameObject[] SubmitTargets()
        {
            global::ChallengeSubmitButton submit = Get<global::ChallengeSubmitButton>(_screen, SubmitButtonField);
            GameUISelectableButton newRun = Get<GameUISelectableButton>(submit, SubmitNewRunButtonField);
            GameUISelectableButton cont = Get<GameUISelectableButton>(submit, SubmitContinueRunButtonField);
            return new[]
            {
                submit != null ? submit.gameObject : null,
                newRun != null ? newRun.gameObject : null,
                cont != null ? cont.gameObject : null
            };
        }

        private static GameUISelectableButton RowInfoButton(global::LeaderboardRow row)
        {
            global::LeaderboardRunSummaryButton summary = Get<global::LeaderboardRunSummaryButton>(row, RowRunSummaryButtonField);
            return summary != null ? summary.InfoButton : null;
        }

        private static Message LeaderboardHeader(global::ChallengeDetailsLeaderboardUI leaderboard)
        {
            global::PaginationControls pagination = leaderboard?.GetPaginationControls();
            TMP_Text label = Get<TMP_Text>(pagination, PaginationPageLabelField);
            Message page = Message.FromText(AccessibilityText.ReadLocalizedText(label));
            return page != null
                ? Message.Localized("ui", "CHALLENGE.LEADERBOARD_WITH_PAGE", new { page = page.Resolve() })
                : Message.Localized("ui", "CHALLENGE.LEADERBOARD");
        }

        private static void AppendLeaderboardSignature(StringBuilder sb, global::ChallengeDetailsLeaderboardUI leaderboard)
        {
            if (leaderboard == null)
            {
                sb.Append("leaderboard:null|");
                return;
            }

            sb.Append("leaderboard:").Append(leaderboard.gameObject.activeInHierarchy ? '1' : '0').Append(':');
            sb.Append(IsLeaderboardLoading(leaderboard) ? '1' : '0').Append(':');
            AppendButtonSignature(sb, Get<GameUISelectableButton>(leaderboard, LeaderboardAllEntriesButtonField));
            AppendButtonSignature(sb, Get<GameUISelectableButton>(leaderboard, LeaderboardFriendsOnlyButtonField));
            global::PaginationControls pagination = leaderboard.GetPaginationControls();
            if (pagination != null)
            {
                sb.Append("page:").Append(pagination.CurrentPage).Append('/').Append(pagination.LastPage).Append(':');
            }

            List<global::LeaderboardRow> rows = Get<List<global::LeaderboardRow>>(leaderboard, LeaderboardCurrentRowsField);
            if (rows != null)
            {
                for (int i = 0; i < rows.Count; i++)
                {
                    global::ChallengeLeaderboardData.LeaderEntry entry = rows[i]?.GetEntryData();
                    if (entry == null)
                    {
                        continue;
                    }

                    sb.Append(entry.GetRank()).Append(':')
                        .Append(entry.GetPlayerId()).Append(':')
                        .Append(entry.GetScore()).Append(':')
                        .Append(entry.GetVictory() ? '1' : '0')
                        .Append(';');
                }
            }
        }

        private static void AppendClassSignature(StringBuilder sb, global::ClassLevelInfoUI info)
        {
            if (info == null)
            {
                sb.Append("class:null|");
                return;
            }

            sb.Append("class:")
                .Append(info.gameObject.activeInHierarchy ? '1' : '0')
                .Append(':')
                .Append(info.ClassData != null ? info.ClassData.GetID() : "random")
                .Append(':')
                .Append(info.ChampionIndex)
                .Append('|');
        }

        private static void AppendPyreSignature(StringBuilder sb, global::PyreHeartInfoUI pyre)
        {
            if (pyre == null)
            {
                sb.Append("pyre:null|");
                return;
            }

            sb.Append("pyre:")
                .Append(pyre.gameObject.activeInHierarchy ? '1' : '0')
                .Append(':')
                .Append(pyre.PyreHeartCharacterData != null ? pyre.PyreHeartCharacterData.GetID() : "random")
                .Append('|');
        }

        private static void AppendMutatorSignature(StringBuilder sb, global::MutatorSelectionUI mutators)
        {
            sb.Append("mutators:");
            if (mutators == null)
            {
                sb.Append("null|");
                return;
            }

            sb.Append(mutators.gameObject.activeInHierarchy ? '1' : '0').Append(':');
            List<MutatorState> states = mutators.GetMutators();
            if (states != null)
            {
                for (int i = 0; i < states.Count; i++)
                {
                    sb.Append(states[i]?.GetRelicDataID()).Append(';');
                }
            }
            sb.Append('|');
        }

        private static void AppendClassDropdownSignature(StringBuilder sb, global::ClassSelectionDropdown dropdown)
        {
            sb.Append("classDropdown:");
            if (dropdown == null)
            {
                sb.Append("null|");
                return;
            }

            sb.Append(dropdown.gameObject.activeInHierarchy ? '1' : '0').Append(':');
            List<global::ClassInfoButtonUI> buttons = Get<List<global::ClassInfoButtonUI>>(dropdown, ClassDropdownButtonsField);
            if (buttons != null)
            {
                for (int i = 0; i < buttons.Count; i++)
                {
                    global::ClassInfoButtonUI button = buttons[i];
                    sb.Append(button != null && button.gameObject.activeInHierarchy ? '1' : '0')
                        .Append(',')
                        .Append(button?.classData != null ? button.classData.GetID() : string.Empty)
                        .Append(';');
                }
            }
            sb.Append('|');
        }

        private static void AppendPyreHeartDropdownSignature(StringBuilder sb, global::PyreHeartSelectionDropdown dropdown)
        {
            sb.Append("pyreDropdown:");
            if (dropdown == null)
            {
                sb.Append("null|");
                return;
            }

            sb.Append(dropdown.gameObject.activeInHierarchy ? '1' : '0').Append(':');
            List<global::PyreHeartInfoButtonUI> buttons = Get<List<global::PyreHeartInfoButtonUI>>(dropdown, PyreHeartDropdownButtonsField);
            if (buttons != null)
            {
                for (int i = 0; i < buttons.Count; i++)
                {
                    global::PyreHeartInfoButtonUI button = buttons[i];
                    sb.Append(button != null && button.gameObject.activeInHierarchy ? '1' : '0')
                        .Append(',')
                        .Append(button?.PyreHeartCharacterData != null ? button.PyreHeartCharacterData.GetID() : string.Empty)
                        .Append(';');
                }
            }
            sb.Append('|');
        }

        private static void AppendMutatorDropdownSignature(StringBuilder sb, global::MutatorSelectionUI mutators)
        {
            GameUISelectable dropdownRoot = Get<GameUISelectable>(mutators, MutatorDropdownRootField);
            sb.Append("mutatorDropdown:");
            if (dropdownRoot == null)
            {
                sb.Append("null|");
                return;
            }

            sb.Append(dropdownRoot.gameObject.activeInHierarchy ? '1' : '0').Append(':');
            List<MutatorButtonUI> choices = Get<List<MutatorButtonUI>>(mutators, MutatorDropdownUIsField);
            if (choices != null)
            {
                for (int i = 0; i < choices.Count; i++)
                {
                    MutatorButtonUI choice = choices[i];
                    sb.Append(choice != null && choice.gameObject.activeInHierarchy ? '1' : '0')
                        .Append(',')
                        .Append(choice?.MutatorState != null ? choice.MutatorState.GetRelicDataID() : string.Empty)
                        .Append(',')
                        .Append(choice != null && choice.Chosen ? '1' : '0')
                        .Append(';');
                }
            }
            sb.Append('|');
        }

        private static void AppendButtonSignature(StringBuilder sb, GameUISelectableButton button)
        {
            if (button == null)
            {
                sb.Append("button:null|");
                return;
            }

            sb.Append("button:")
                .Append(button.gameObject.activeInHierarchy ? '1' : '0')
                .Append(':')
                .Append(button.interactable ? '1' : '0')
                .Append(':')
                .Append(button.state)
                .Append(':')
                .Append(GameUIButtonSupport.ResolveLabel(button))
                .Append('|');
        }

        private static void AppendText(StringBuilder sb, object owner, FieldInfo field)
        {
            TMP_Text text = Get<TMP_Text>(owner, field);
            sb.Append(AccessibilityText.ReadLocalizedText(text)).Append('|');
        }

        private static GameObject ComponentTarget(IGameUIComponent component)
        {
            return component?.component != null ? component.component.gameObject : null;
        }

        private static T Get<T>(object owner, FieldInfo field) where T : class
        {
            return owner != null ? field.GetValue(owner) as T : null;
        }

        private static bool GetBool(object owner, FieldInfo field)
        {
            return owner != null && field.GetValue(owner) is bool value && value;
        }

        private sealed class ChallengeClassChoiceElement : UIElement, IActivatableElement, INavigationTargetElement
        {
            private readonly global::ClassInfoButtonUI _option;
            private readonly GameUISelectableButton _button;
            private readonly Func<bool> _activate;

            public ChallengeClassChoiceElement(global::ClassInfoButtonUI option, GameUISelectableButton button, Func<bool> activate)
            {
                _option = option;
                _button = button;
                _activate = activate;
            }

            public override bool IsVisible => _option != null && _option.gameObject.activeInHierarchy;
            public override string GetTypeKey() => "button";
            public override Message GetLabel() => Message.FromText(_option?.classData?.GetTitle());
            public override Message GetStatusString() => GameButtonElement.StateMessage(_button);
            public override Message GetTooltip() => ChallengePresentation.ClassDescription(_option?.classData, _option != null && _option.isSubClass);

            public void SelectForNavigation()
            {
                if (_button != null && InputManager.Inst != null)
                {
                    InputManager.Inst.SelectGameUIComponent(_button, allowClearingSelection: false);
                }
            }

            public bool Activate() => _activate != null && _activate();
        }

        private sealed class ChallengePyreHeartChoiceElement : UIElement, IActivatableElement, INavigationTargetElement
        {
            private readonly global::PyreHeartInfoButtonUI _option;
            private readonly GameUISelectableButton _button;
            private readonly Func<bool> _activate;

            public ChallengePyreHeartChoiceElement(global::PyreHeartInfoButtonUI option, GameUISelectableButton button, Func<bool> activate)
            {
                _option = option;
                _button = button;
                _activate = activate;
            }

            public override bool IsVisible => _option != null && _option.gameObject.activeInHierarchy;
            public override string GetTypeKey() => "button";
            public override Message GetLabel() => Message.FromText(_option?.PyreHeartCharacterData?.GetName());
            public override Message GetStatusString() => GameButtonElement.StateMessage(_button);
            public override Message GetTooltip() => ChallengePresentation.PyreHeartTooltip(_option?.PyreHeartCharacterData);

            public void SelectForNavigation()
            {
                if (_button != null && InputManager.Inst != null)
                {
                    InputManager.Inst.SelectGameUIComponent(_button, allowClearingSelection: false);
                }
            }

            public bool Activate() => _activate != null && _activate();
        }

        private sealed class StaticTextElement : UIElement, IActivatableElement
        {
            private readonly Message _label;
            private readonly Message _tooltip;

            public StaticTextElement(Message label, Message tooltip = null)
            {
                _label = label;
                _tooltip = tooltip;
            }

            public Func<bool> ActivateHandler { get; set; }

            public override string GetTypeKey() => ActivateHandler != null ? "button" : null;
            public override Message GetLabel() => _label;
            public override Message GetTooltip() => _tooltip;
            public bool Activate() => ActivateHandler != null && ActivateHandler();
        }


    }
}
