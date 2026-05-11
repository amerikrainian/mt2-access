using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using MonsterTrainAccessibility.Core;
using MonsterTrainAccessibility.Localization;
using MonsterTrainAccessibility.UI.Elements;
using ShinyShoe;

using MonsterTrainAccessibility.UI.Elements.Compendium;

namespace MonsterTrainAccessibility.UI.Screens
{
    internal static class CompendiumFilters
    {
        private static readonly FieldInfo FilterToolbarFilterUIsField = AccessTools.Field(typeof(global::FilterToolbar), "filterUIs")!;
        private static readonly FieldInfo OptionsFilterButtonsField = AccessTools.Field(typeof(global::OptionsFilterUI), "optionButtons")!;
        private static readonly FieldInfo DropdownFilterDropdownField = AccessTools.Field(typeof(global::DropdownFilterUI), "dropdown")!;
        private static readonly FieldInfo SearchFilterClearButtonField = AccessTools.Field(typeof(global::SearchFilterUI), "clearButton")!;
        private static readonly FieldInfo SearchFilterInputField = AccessTools.Field(typeof(global::SearchFilterUI), "inputField")!;

        public static void AddToolbar(CompendiumScreen screen, global::FilterToolbar toolbar)
        {
            if (toolbar == null)
            {
                return;
            }

            List<global::FilterUI> filters = global::MonsterTrainAccessibility.Core.ReflectionUtil.Get<List<global::FilterUI>>(toolbar, FilterToolbarFilterUIsField);
            if (filters == null)
            {
                return;
            }

            for (int i = 0; i < filters.Count; i++)
            {
                AddFilter(screen, filters[i]);
            }
        }

        public static void AddFilter(CompendiumScreen screen, global::FilterUI filter)
        {
            if (filter == null)
            {
                return;
            }

            if (filter is global::OptionsFilterUI options)
            {
                AddOptionsFilter(screen, options);
            }
            else if (filter is global::DropdownFilterUI dropdown)
            {
                AddDropdownFilter(screen, dropdown);
            }
            else if (filter is global::SearchFilterUI search)
            {
                AddSearchFilter(screen, search, Message.Localized("ui", "COMPENDIUM.SEARCH"));
            }
        }

        public static void AddDropdownFilter(CompendiumScreen screen, global::DropdownFilterUI filter)
        {
            if (filter == null)
            {
                return;
            }

            IGameUIComponent dropdown = global::MonsterTrainAccessibility.Core.ReflectionUtil.Get<IGameUIComponent>(filter, DropdownFilterDropdownField);
            if (dropdown == null)
            {
                return;
            }

            screen.AddAccessibleElement(new ProxyCompendiumDropdownFilter(filter, dropdown),
                filter.gameObject,
                dropdown.component != null ? dropdown.component.gameObject : null);
        }

        public static void AddSearchFilter(CompendiumScreen screen, global::SearchFilterUI filter, Message label)
        {
            if (filter == null)
            {
                return;
            }

            global::InputFieldContainer input = global::MonsterTrainAccessibility.Core.ReflectionUtil.Get<global::InputFieldContainer>(filter, SearchFilterInputField);
            if (input?.button != null)
            {
                screen.AddAccessibleElement(new ProxyCompendiumSearchFilter(filter, input, label, input.button, () => screen.Trigger(input.button)),
                    filter.gameObject,
                    input.button.gameObject);
            }

            GameUISelectableButton clear = global::MonsterTrainAccessibility.Core.ReflectionUtil.Get<GameUISelectableButton>(filter, SearchFilterClearButtonField);
            if (clear != null)
            {
                screen.AddAccessibleElement(new LabeledButton(clear, "COMPENDIUM.SEARCH.CLEAR"),
                    clear.gameObject);
            }
        }

        private static void AddOptionsFilter(CompendiumScreen screen, global::OptionsFilterUI options)
        {
            List<global::FilterOptionButton> buttons = global::MonsterTrainAccessibility.Core.ReflectionUtil.Get<List<global::FilterOptionButton>>(options, OptionsFilterButtonsField);
            if (buttons == null)
            {
                return;
            }

            screen.AddAccessibleElement(new ProxyCompendiumOptionsFilter(options, buttons),
                options.gameObject,
                buttons.Count > 0 && buttons[0]?.Button != null ? buttons[0].Button.gameObject : null);
        }
    }
}
