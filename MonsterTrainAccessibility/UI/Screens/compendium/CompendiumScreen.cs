using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using MonsterTrainAccessibility.Core;
using MonsterTrainAccessibility.Input;
using MonsterTrainAccessibility.Localization;
using MonsterTrainAccessibility.UI.Elements;
using ShinyShoe;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

using MonsterTrainAccessibility.UI.Elements.Compendium;

namespace MonsterTrainAccessibility.UI.Screens
{
    internal sealed class CompendiumScreen : ListNavigationGameScreen
    {
        private static readonly FieldInfo SectionOrderField = AccessTools.Field(typeof(global::CompendiumScreen), "sectionOrder")!;
        private static readonly FieldInfo TabsBySectionField = AccessTools.Field(typeof(global::CompendiumScreen), "tabsBySection")!;
        private static readonly FieldInfo PagesBySectionField = AccessTools.Field(typeof(global::CompendiumScreen), "pagesBySection")!;
        private static readonly FieldInfo CurrentSectionField = AccessTools.Field(typeof(global::CompendiumScreen), "currentSection")!;
        private static readonly FieldInfo BackButtonField = AccessTools.Field(typeof(global::CompendiumScreen), "backButton")!;
        private static readonly FieldInfo PageTurnZonesField = AccessTools.Field(typeof(global::CompendiumScreen), "pageTurnZones")!;
        private static readonly FieldInfo SearchFilterInputField = AccessTools.Field(typeof(global::SearchFilterUI), "inputField")!;
        private static readonly FieldInfo InputContainerInputField = AccessTools.Field(typeof(global::InputFieldContainer), "inputField")!;

        private readonly global::CompendiumScreen _screen;
        private string _lastComputedSignature = string.Empty;
        private bool _hasComputedSignature;

        public CompendiumScreen(global::CompendiumScreen screen)
        {
            _screen = screen;
            RootList.AnnouncePosition = false;
            ClaimAction("ui_cancel");
        }

        public override bool ShouldRestoreNavigationFocus() => CompendiumChecklistChangeStore.HasChanges(_screen);

        public override bool ShouldAcceptGameSelection()
        {
            return !CompendiumChecklistChangeStore.HasChanges(_screen) && !IsFocusedExpandableFilter();
        }

        public override bool BlocksGameInput(InputAction action)
        {
            if (FocusedExpandedFilter() != null && IsExpandedFilterAction(action?.Key))
            {
                return true;
            }

            if ((action?.Key == "ui_accept" || action?.Key == "ui_select") && IsFocusedExpandableFilter())
            {
                return true;
            }

            return base.BlocksGameInput(action);
        }

        internal void AddAccessibleElement(UIElement element, params GameObject[] targets)
        {
            AddElement(element, targets);
        }

        internal bool Trigger(IGameUIComponent component)
        {
            if (component == null)
            {
                return false;
            }

            if (global::InputManager.Inst != null)
            {
                global::InputManager.Inst.SelectGameUIComponent(component, allowClearingSelection: false);
            }

            CoreInputControlMapping mapping = new CoreInputControlMapping(global::InputManager.Controls.Submit, InputType.None, fake: true);
            return _screen.ApplyScreenInput(mapping, component, global::InputManager.Controls.Submit);
        }

        public override bool OnActionJustPressed(InputAction action)
        {
            ICompendiumExpandableFilter expanded = FocusedExpandedFilter();
            if (expanded != null && IsExpandedFilterAction(action?.Key))
            {
                return expanded.HandleExpandedAction(action);
            }

            return base.OnActionJustPressed(action);
        }

        internal static void RefreshEnemyDetailsBuffers(global::CompendiumEnemyDetailsUI details)
        {
            if (details == null)
            {
                return;
            }

            UIElement element = ScreenManager.ResolveElement(details.gameObject);
            UIManager.RefreshBuffersFor(element);
        }

        private bool IsFocusedExpandableFilter()
        {
            return RootList.FocusedChild is ICompendiumExpandableFilter;
        }

        private ICompendiumExpandableFilter FocusedExpandedFilter()
        {
            ICompendiumExpandableFilter filter = RootList.FocusedChild as ICompendiumExpandableFilter;
            return filter != null && filter.IsExpanded ? filter : null;
        }

        private static bool IsExpandedFilterAction(string key)
        {
            switch (key)
            {
                case "ui_up":
                case "ui_down":
                case "ui_accept":
                case "ui_select":
                case "ui_cancel":
                    return true;
                default:
                    return false;
            }
        }

        protected override void PopulateList()
        {
            AddChecklistChanges();
            AddSectionTabs();

            global::CompendiumSection section = GetCurrentSectionUI();
            AddPaginator(section);
            CompendiumSectionRegistry.Populate(this, section);

            GameUISelectableButton backButton = global::MonsterTrainAccessibility.Core.ReflectionUtil.Get<GameUISelectableButton>(_screen, BackButtonField);
            AddElement(new LabeledButton(backButton, "COMPENDIUM.BACK"),
                backButton != null ? backButton.gameObject : null);
        }

        protected override string BuildSignature()
        {
            if (IsCurrentSectionTextInputActive() && _hasComputedSignature)
            {
                return _lastComputedSignature;
            }

            global::CompendiumSection section = GetCurrentSectionUI();
            string signature = ChecklistChangeSignature() + ":" + CurrentSection() + ":" + CompendiumPaginatorSource.CurrentPageOf(section) + ":" + CompendiumSectionRegistry.Signature(section);
            _lastComputedSignature = signature;
            _hasComputedSignature = true;
            return signature;
        }

        private bool IsCurrentSectionTextInputActive()
        {
            if (global::InputManager.Inst?.GetSelectedGameUIComponent() is GameUISelectableInputField)
            {
                return true;
            }

            GameObject selected = EventSystem.current?.currentSelectedGameObject;
            if (selected == null)
            {
                return false;
            }

            TMP_InputField input = selected.GetComponent<TMP_InputField>();
            if (input != null && input.isFocused)
            {
                return true;
            }

            foreach (global::SearchFilterUI search in CurrentSectionSearchFilters())
            {
                global::InputFieldContainer container = global::MonsterTrainAccessibility.Core.ReflectionUtil.Get<global::InputFieldContainer>(search, SearchFilterInputField);
                GameUISelectableInputField field = global::MonsterTrainAccessibility.Core.ReflectionUtil.Get<GameUISelectableInputField>(container, InputContainerInputField);
                if (field != null && (field.isFocused || global::InputManager.Inst?.IsGameUIComponentSelected(field) == true))
                {
                    return true;
                }
            }

            return false;
        }

        private IEnumerable<global::SearchFilterUI> CurrentSectionSearchFilters()
        {
            global::CompendiumSection section = GetCurrentSectionUI();
            return section != null
                ? section.GetComponentsInChildren<global::SearchFilterUI>(includeInactive: true)
                : System.Array.Empty<global::SearchFilterUI>();
        }

        private void AddChecklistChanges()
        {
            IReadOnlyList<Message> changes = CompendiumChecklistChangeStore.Messages(_screen);
            for (int i = 0; i < changes.Count; i++)
            {
                AddElement(new ProxyCompendiumChecklistChange(changes[i]));
            }
        }

        private string ChecklistChangeSignature()
        {
            IReadOnlyList<Message> changes = CompendiumChecklistChangeStore.Messages(_screen);
            if (changes.Count == 0)
            {
                return string.Empty;
            }

            System.Text.StringBuilder builder = new System.Text.StringBuilder();
            for (int i = 0; i < changes.Count; i++)
            {
                if (builder.Length > 0)
                {
                    builder.Append('|');
                }
                builder.Append(changes[i]?.Resolve());
            }

            return builder.ToString();
        }

        private void AddSectionTabs()
        {
            List<global::CompendiumScreen.Section> sections = global::MonsterTrainAccessibility.Core.ReflectionUtil.Get<List<global::CompendiumScreen.Section>>(_screen, SectionOrderField);
            Dictionary<global::CompendiumScreen.Section, global::CompendiumTab> tabs =
                global::MonsterTrainAccessibility.Core.ReflectionUtil.Get<Dictionary<global::CompendiumScreen.Section, global::CompendiumTab>>(_screen, TabsBySectionField);
            if (sections == null)
            {
                return;
            }

            global::CompendiumScreen.Section current = CurrentSection();
            for (int i = 0; i < sections.Count; i++)
            {
                global::CompendiumScreen.Section section = sections[i];
                if (section == global::CompendiumScreen.Section.NONE)
                {
                    continue;
                }

                global::CompendiumTab tab = null;
                tabs?.TryGetValue(section, out tab);
                GameUISelectableButton button = tab?.Button;
                AddElement(new ProxyCompendiumSectionTab(
                    _screen,
                    section,
                    CurrentSection,
                    tab,
                    button),
                    tab != null ? tab.gameObject : null,
                    button != null ? button.gameObject : null);
            }
        }

        private void AddPaginator(global::CompendiumSection section)
        {
            if (section == null || (!section.CanTurnPage(PageTurnZone.TurnDir.Left) && !section.CanTurnPage(PageTurnZone.TurnDir.Right)))
            {
                return;
            }

            AddElement(new PaginatorElement(new CompendiumPaginatorSource(
                section,
                global::MonsterTrainAccessibility.Core.ReflectionUtil.Get<List<PageTurnZone>>(_screen, PageTurnZonesField))));
        }

        private global::CompendiumScreen.Section CurrentSection()
        {
            object value = CurrentSectionField.GetValue(_screen);
            return value is global::CompendiumScreen.Section section ? section : global::CompendiumScreen.Section.NONE;
        }

        private global::CompendiumSection GetCurrentSectionUI()
        {
            Dictionary<global::CompendiumScreen.Section, global::CompendiumSection> pages =
                global::MonsterTrainAccessibility.Core.ReflectionUtil.Get<Dictionary<global::CompendiumScreen.Section, global::CompendiumSection>>(_screen, PagesBySectionField);
            if (pages == null)
            {
                return null;
            }

            pages.TryGetValue(CurrentSection(), out global::CompendiumSection section);
            return section;
        }

        internal static Message SectionName(global::CompendiumScreen.Section section)
        {
            return Message.Localized("ui", "COMPENDIUM.SECTION." + section.ToString().ToUpperInvariant());
        }

        internal static Message TextOrNull(TMP_Text label)
        {
            return Message.FromText(AccessibilityText.ReadLocalizedText(label));
        }

        internal static void SelectGameComponent(IGameUIComponent component)
        {
            if (component != null && global::InputManager.Inst != null)
            {
                global::InputManager.Inst.SelectGameUIComponent(component, allowClearingSelection: false);
            }
        }

        internal static void ClearGameSelection()
        {
            global::InputManager.Inst?.SelectGameUIComponent(null, allowClearingSelection: true);
            EventSystem.current?.SetSelectedGameObject(null);
        }

        internal static bool ShouldSuppressSearchEditingSelection(IGameUIComponent nextSelection)
        {
            CompendiumScreen screen = ScreenManager.CurrentScreen as CompendiumScreen;
            return screen != null && screen.ShouldSuppressSearchEditingSelectionInstance(nextSelection);
        }

        internal static bool IsCurrentSearchInput(global::InputFieldContainer input)
        {
            CompendiumScreen screen = ScreenManager.CurrentScreen as CompendiumScreen;
            return screen != null && screen.IsCurrentSearchInputInstance(input);
        }

        internal static bool FocusSearchTrigger(GameUISelectableButton button)
        {
            CompendiumScreen screen = ScreenManager.CurrentScreen as CompendiumScreen;
            return screen != null && screen.FocusSearchTriggerInstance(button);
        }

        private bool ShouldSuppressSearchEditingSelectionInstance(IGameUIComponent nextSelection)
        {
            if (!IsCurrentSectionTextInputActive())
            {
                return false;
            }

            if (nextSelection == null || nextSelection is GameUISelectableInputField)
            {
                return false;
            }

            foreach (global::SearchFilterUI search in CurrentSectionSearchFilters())
            {
                global::InputFieldContainer container = global::MonsterTrainAccessibility.Core.ReflectionUtil.Get<global::InputFieldContainer>(search, SearchFilterInputField);
                if (container?.button != null && container.button.IsGameUIComponent(nextSelection))
                {
                    return false;
                }
            }

            return true;
        }

        private bool IsCurrentSearchInputInstance(global::InputFieldContainer input)
        {
            if (input == null)
            {
                return false;
            }

            foreach (global::SearchFilterUI search in CurrentSectionSearchFilters())
            {
                global::InputFieldContainer container = global::MonsterTrainAccessibility.Core.ReflectionUtil.Get<global::InputFieldContainer>(search, SearchFilterInputField);
                if (ReferenceEquals(container, input))
                {
                    return true;
                }
            }

            return false;
        }

        private bool FocusSearchTriggerInstance(GameUISelectableButton button)
        {
            if (button == null)
            {
                return false;
            }

            for (int i = 0; i < RootList.Children.Count; i++)
            {
                if (RootList.Children[i] is ProxyCompendiumSearchFilter search && search.IsForButton(button))
                {
                    RootList.SetFocusIndex(i);
                    return true;
                }
            }

            return false;
        }

    }
}
