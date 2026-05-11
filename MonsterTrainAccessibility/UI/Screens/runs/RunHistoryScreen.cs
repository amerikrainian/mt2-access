using System.Collections.Generic;
using System.Reflection;
using System.Text;
using HarmonyLib;
using MonsterTrainAccessibility.Core;
using MonsterTrainAccessibility.Input;
using MonsterTrainAccessibility.Localization;
using MonsterTrainAccessibility.Speech;
using MonsterTrainAccessibility.UI.Elements;
using ShinyShoe;
using UnityEngine;

namespace MonsterTrainAccessibility.UI.Screens
{
    internal sealed class RunHistoryScreen : ListNavigationGameScreen
    {
        private static readonly FieldInfo RunHistoryUIField = AccessTools.Field(typeof(global::RunHistoryScreen), "runHistoryUI")!;
        private static readonly FieldInfo BackButtonField = AccessTools.Field(typeof(global::RunHistoryScreen), "backButton")!;
        private static readonly MethodInfo ReturnToMainMenuMethod = AccessTools.Method(typeof(global::RunHistoryScreen), "ReturnToMainMenu")!;

        private static readonly FieldInfo SortDropdownField = AccessTools.Field(typeof(global::RunHistoryUI), "sortDropdown")!;
        private static readonly FieldInfo PaginationControlsField = AccessTools.Field(typeof(global::RunHistoryUI), "paginationControls")!;
        private static readonly FieldInfo RowsField = AccessTools.Field(typeof(global::RunHistoryUI), "currentRows")!;
        private static readonly FieldInfo SortOptionsField = AccessTools.Field(typeof(global::RunHistoryUI), "sortOptions")!;
        private static readonly FieldInfo CurrentSortFieldNameField = AccessTools.Field(typeof(global::RunHistoryUI), "currentSortFieldName")!;
        private static readonly FieldInfo CurrentSortDirectionField = AccessTools.Field(typeof(global::RunHistoryUI), "currentSortDirection")!;
        private static readonly FieldInfo FetchingGameRunsField = AccessTools.Field(typeof(global::RunHistoryUI), "fetchingGameRuns")!;
        private static readonly FieldInfo SaveManagerField = AccessTools.Field(typeof(global::RunHistoryUI), "saveManager")!;

        private readonly global::RunHistoryScreen _screen;
        private UIElement _loadingElement;
        private UIElement _sortElement;
        private bool _suppressSortFocusAnnouncement;

        public RunHistoryScreen(global::RunHistoryScreen screen)
        {
            _screen = screen;
            RootList.AnnouncePosition = false;
        }

        public override bool ShouldRestoreNavigationFocus() => false;
        public override bool ShouldAcceptGameSelection() => false;

        public override bool ShouldAnnounceFocus(UIElement element)
        {
            if (_suppressSortFocusAnnouncement && ReferenceEquals(element, _sortElement))
            {
                return false;
            }

            if (!ReferenceEquals(element, _sortElement))
            {
                _suppressSortFocusAnnouncement = false;
            }

            return true;
        }

        public override bool BlocksGameInput(InputAction action)
        {
            if (action?.Key == "ui_accept" || action?.Key == "ui_select")
            {
                return true;
            }

            return base.BlocksGameInput(action);
        }

        protected override void PopulateList()
        {
            global::RunHistoryUI runHistory = RunHistoryUI;
            if (runHistory == null)
            {
                return;
            }

            AddLoading(runHistory);
            AddSortDropdown(runHistory);
            AddPaginator(runHistory);
            AddRows(runHistory);
            AddBackButton();
        }

        public override UIElement GetElement(GameObject go)
        {
            UIElement element = base.GetElement(go);
            if (element != null)
            {
                return element;
            }

            global::RunHistoryUI runHistory = RunHistoryUI;
            if (runHistory != null && IsFetchingGameRuns(runHistory) && _loadingElement != null)
            {
                return _loadingElement;
            }

            return null;
        }

        protected override string BuildSignature()
        {
            global::RunHistoryUI runHistory = RunHistoryUI;
            if (runHistory == null)
            {
                return string.Empty;
            }

            StringBuilder sb = new StringBuilder();
            sb.Append(IsFetchingGameRuns(runHistory)).Append('|');
            sb.Append(CurrentSortFieldNameField.GetValue(runHistory)).Append(':');
            sb.Append(CurrentSortDirectionField.GetValue(runHistory)).Append('|');

            global::PaginationControls pagination = Get<global::PaginationControls>(runHistory, PaginationControlsField);
            if (pagination != null)
            {
                sb.Append(pagination.CurrentPage).Append('/').Append(pagination.LastPage);
            }

            List<global::RunHistoryEntryUI> rows = CurrentRows(runHistory);
            if (rows != null)
            {
                for (int i = 0; i < rows.Count; i++)
                {
                    global::RunAggregateData data = rows[i]?.GetRunData();
                    if (data == null)
                    {
                        continue;
                    }

                    sb.Append('|').Append(data.GetID());
                }
            }

            return sb.ToString();
        }

        private void AddLoading(global::RunHistoryUI runHistory)
        {
            _loadingElement = new ProxyRunHistoryLoading(runHistory);
            AddElement(_loadingElement);
        }

        private void AddSortDropdown(global::RunHistoryUI runHistory)
        {
            GameUISelectableDropdown dropdown = Get<GameUISelectableDropdown>(runHistory, SortDropdownField);
            List<object> sortOptions = SortOptions(runHistory);
            if (dropdown == null || sortOptions == null)
            {
                return;
            }

            _sortElement = new GameDropdownCycleElement(
                dropdown,
                label: () => Message.Localized("ui", "RUN_HISTORY.SORT"),
                currentIndex: () => CurrentSortIndex(runHistory, sortOptions),
                count: () => sortOptions.Count,
                optionLabel: index => SortOptionLabel(sortOptions, index),
                choose: (index, label) => ChooseSortOption(dropdown, index, label),
                selectForNavigation: ClearGameSelection);
            AddElement(_sortElement, dropdown.gameObject);
        }

        private void AddPaginator(global::RunHistoryUI runHistory)
        {
            global::PaginationControls pagination = Get<global::PaginationControls>(runHistory, PaginationControlsField);
            if (pagination == null || !pagination.gameObject.activeInHierarchy || pagination.LastPage <= 1)
            {
                return;
            }

            AddElement(new PaginatorElement(new RunHistoryPaginatorSource(pagination)));
        }

        private void AddRows(global::RunHistoryUI runHistory)
        {
            SaveManager saveManager = Get<SaveManager>(runHistory, SaveManagerField) ?? GameManagers.GetSaveManager();
            List<global::RunHistoryEntryUI> rows = CurrentRows(runHistory);
            if (rows == null)
            {
                return;
            }

            for (int i = 0; i < rows.Count; i++)
            {
                global::RunHistoryEntryUI row = rows[i];
                if (row == null || row.GetRunData() == null)
                {
                    continue;
                }

                ListContainer rowContainer = new ListContainer
                {
                    AnnounceName = false,
                    AnnouncePosition = false,
                    NavigationAxis = NavigationAxis.Horizontal
                };

                RunHistoryEntryElement entry = new RunHistoryEntryElement(runHistory, row, saveManager);
                RunHistoryStarElement star = new RunHistoryStarElement(row, saveManager);
                rowContainer.Add(entry);
                rowContainer.Add(star);

                AddElement(rowContainer, row.gameObject, row.Button != null ? row.Button.gameObject : null);
            }
        }

        private void AddBackButton()
        {
            GameUISelectableButton backButton = Get<GameUISelectableButton>(_screen, BackButtonField);
            if (backButton == null)
            {
                return;
            }

            AddElement(new ProxyRunHistoryBackButton(backButton, _screen),
                backButton.gameObject);
        }

        internal static bool ReturnToMainMenu(global::RunHistoryScreen screen)
        {
            if (screen == null)
            {
                return false;
            }

            ReturnToMainMenuMethod.Invoke(screen, null);
            return true;
        }

        private global::RunHistoryUI RunHistoryUI => Get<global::RunHistoryUI>(_screen, RunHistoryUIField);

        internal static bool IsFetchingGameRuns(global::RunHistoryUI runHistory)
        {
            object value = FetchingGameRunsField.GetValue(runHistory);
            return value is bool fetching && fetching;
        }

        private static List<global::RunHistoryEntryUI> CurrentRows(global::RunHistoryUI runHistory)
        {
            return Get<List<global::RunHistoryEntryUI>>(runHistory, RowsField);
        }

        private static List<object> SortOptions(global::RunHistoryUI runHistory)
        {
            System.Collections.IEnumerable enumerable = SortOptionsField.GetValue(runHistory) as System.Collections.IEnumerable;
            if (enumerable == null)
            {
                return null;
            }

            List<object> options = new List<object>();
            foreach (object option in enumerable)
            {
                options.Add(option);
            }

            return options;
        }

        private static int CurrentSortIndex(global::RunHistoryUI runHistory, List<object> sortOptions)
        {
            if (runHistory == null || sortOptions == null)
            {
                return 0;
            }

            object currentFieldName = CurrentSortFieldNameField.GetValue(runHistory);
            object currentDirection = CurrentSortDirectionField.GetValue(runHistory);
            for (int i = 0; i < sortOptions.Count; i++)
            {
                object option = sortOptions[i];
                if (option == null)
                {
                    continue;
                }

                if (Equals(GetSortOption<object>(option, "fieldName"), currentFieldName) &&
                    Equals(GetSortOption<object>(option, "direction"), currentDirection))
                {
                    return i;
                }
            }

            return 0;
        }

        private static Message SortOptionLabel(List<object> sortOptions, int index)
        {
            if (sortOptions == null || index < 0 || index >= sortOptions.Count)
            {
                return null;
            }

            string key = GetSortOption<string>(sortOptions[index], "labelKey");
            return !string.IsNullOrWhiteSpace(key)
                ? Message.RawCleaned(AccessibilityLocalizationScope.Run(() => key.Localize()))
                : null;
        }

        private void ChooseSortOption(GameUISelectableDropdown dropdown, int index, string label)
        {
            if (dropdown == null)
            {
                return;
            }

            _suppressSortFocusAnnouncement = true;
            SpeechManager.Output(SortAnnouncement(Message.RawCleaned(label)));
            dropdown.optionChosenSignal.Dispatch(index, label);
        }

        private static Message SortAnnouncement(Message option)
        {
            return Message.Join(
                ", ",
                Message.Localized("ui", "RUN_HISTORY.SORT"),
                Message.Join(" ", Message.Localized("ui", "TYPES.DROPDOWN"), option));
        }

        private static T GetSortOption<T>(object owner, string fieldName)
        {
            if (owner == null)
            {
                return default(T);
            }

            FieldInfo field = AccessTools.Field(owner.GetType(), fieldName)!;
            object value = field.GetValue(owner);
            return value is T typed ? typed : default(T);
        }

        internal static Message BackButtonLabel(GameUISelectableButton backButton)
        {
            Message label = Message.RawCleaned(GameUIButtonSupport.ResolveLabel(backButton));
            if (label != null)
            {
                return label;
            }

            string gameLabel = AccessibilityLocalizationScope.Run(() => "Back".Localize());
            return Message.RawCleaned(gameLabel) ?? Message.Localized("ui", "COMPENDIUM.BACK");
        }

        private static void ClearGameSelection()
        {
            global::InputManager.Inst?.SelectGameUIComponent(null, allowClearingSelection: true);
            UnityEngine.EventSystems.EventSystem.current?.SetSelectedGameObject(null);
        }

        private static T Get<T>(object owner, FieldInfo field) where T : class
        {
            return owner != null ? field.GetValue(owner) as T : null;
        }
    }
}
