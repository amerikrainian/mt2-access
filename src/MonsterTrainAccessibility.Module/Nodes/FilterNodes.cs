using MonsterTrainAccessibility.Core.Graph;
using MonsterTrainAccessibility.Core.Strings;
using MonsterTrainAccessibility.Module.Interop;
using MonsterTrainAccessibility.Module.UI;
using ShinyShoe;
using TMPro;
using UnityEngine;

namespace MonsterTrainAccessibility.Module.Nodes
{
    /// <summary>
    /// The game's list filters (<see cref="FilterUI"/>: the Logbook's card and enemy filters, and
    /// wherever else the game puts a <see cref="FilterToolbar"/>):
    /// <list type="bullet">
    /// <item>an options filter (clan, rarity, cost) is its caption as the context, each option a button
    /// with "selected", named by its tooltip title where the option is an icon; Enter toggles it
    /// through the game;</item>
    /// <item>a dropdown filter is the shared combo box with its picker;</item>
    /// <item>a search filter is a <see cref="SearchField"/> on the game's own text field, then the
    /// game's clear button while there is text to clear.</item>
    /// </list>
    /// The game filters as each changes, as for the mouse.
    /// </summary>
    internal static class FilterNodes
    {
        public static void Options(GraphBuilder b, string key, OptionsFilterUI filter)
        {
            if (filter == null || !filter.gameObject.activeInHierarchy) return;
            string label = Label(filter);
            b.PushContext(label, label != null ? Strings.RoleList : null); // an unlabeled group says nothing of itself
            for (int i = 0; i < filter.optionButtons.Count; i++)
            {
                var option = filter.optionButtons[i];
                var button = option != null ? option.GetComponent<GameUISelectableButton>() : null;
                if (button == null || !option.gameObject.activeInHierarchy) continue;
                int index = i;
                b.AddItem(ControlId.Structural(key + ":" + i), GameWidgets.Button(button,
                    () => OptionName(option), () => OptionDescription(option),
                    GameNodes.SelectedPart(() => filter.SelectedOption == index)));
            }
            b.PopContext();
        }

        public static void Dropdown(GraphBuilder b, string key, DropdownFilterUI filter)
        {
            if (filter == null || filter.dropdown == null || !filter.gameObject.activeInHierarchy) return;
            b.AddItem(ControlId.Structural(key), GameWidgets.Dropdown(filter.dropdown, () => Label(filter)));
        }

        /// <summary>The search field, then its clear button while it has text.</summary>
        public static void Search(GraphBuilder b, SearchField search, SearchFilterUI filter)
        {
            var field = Field(filter);
            if (field == null || !filter.gameObject.activeInHierarchy) return;
            b.AddItem(search.Id, search.Node(field, () => SearchLabel(filter, field)));
            var clear = filter.clearButton;
            if (clear != null && clear.gameObject.activeInHierarchy && !string.IsNullOrEmpty(field.text))
                b.AddItem(ControlId.Structural(search.Key + ":clear"), GameWidgets.Button(clear, () => Strings.SearchClear));
        }

        /// <summary>The game's text field behind a search filter.</summary>
        public static TMP_InputField Field(SearchFilterUI filter)
            => filter != null && filter.inputField != null ? filter.inputField.inputField : null;

        // The field's prompt as drawn ("Search"), else the title the game gives the on-screen keyboard.
        private static string SearchLabel(SearchFilterUI filter, TMP_InputField field)
        {
            var placeholder = field.placeholder as TMP_Text;
            if (placeholder != null && !string.IsNullOrWhiteSpace(placeholder.text)) return placeholder.text;
            string key = filter.inputField.fieldTitleLocalizationKey;
            return !string.IsNullOrEmpty(key) ? GameText.Term(key) : Label(filter);
        }

        /// <summary>A filter's caption ("Clan"): its first shown text that is not inside an option, the
        /// dropdown, the field or a key hint (it sits in the filter's header).</summary>
        public static string Label(Component filter)
        {
            foreach (var text in filter.GetComponentsInChildren<TMP_Text>(false))
            {
                if (string.IsNullOrWhiteSpace(text.text)) continue;
                if (text.GetComponentInParent<FilterOptionButton>() != null || text.GetComponentInParent<GameUISelectableDropdown>() != null
                    || text.GetComponentInParent<TMP_InputField>() != null || text.GetComponentInParent<GameUIControlMapping>() != null) continue;
                return text.text;
            }
            return null;
        }

        // An option by its tooltip title (the clan icons, "Common" for "C"), else its drawn texts
        // ("Herzal Cup, 10").
        private static string OptionName(FilterOptionButton option)
        {
            var tips = option.Tooltips;
            if (tips != null && tips.Count > 0 && !string.IsNullOrWhiteSpace(tips[0].title)) return CardTextHelper.RemoveBold(tips[0].title);
            var parts = new System.Collections.Generic.List<string>();
            foreach (var text in option.GetComponentsInChildren<TMP_Text>(false))
                if (text.enabled && !string.IsNullOrWhiteSpace(text.text) && text.GetComponentInParent<GameUIControlMapping>() == null) parts.Add(text.text.Trim());
            return parts.Count > 0 ? string.Join(", ", parts) : null;
        }

        private static string OptionDescription(FilterOptionButton option)
        {
            var tips = option.Tooltips;
            return tips != null && tips.Count > 0 && !string.IsNullOrWhiteSpace(tips[0].body) ? tips[0].body : null;
        }
    }
}
