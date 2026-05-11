using System.Collections.Generic;
using MonsterTrainAccessibility.UI.Screens;
using System.Reflection;
using HarmonyLib;
using MonsterTrainAccessibility.Core;
using MonsterTrainAccessibility.Input;
using MonsterTrainAccessibility.Localization;
using MonsterTrainAccessibility.UI.Elements;
using ShinyShoe;
using TMPro;

namespace MonsterTrainAccessibility.UI.Elements.Compendium
{
    internal sealed class ProxyCompendiumDropdownFilter : ProxyCompendiumGameButton, ICompendiumExpandableFilter
    {
        private static readonly FieldInfo FilterLabelField = AccessTools.Field(typeof(global::DropdownFilterUI), "label")!;
        private static readonly FieldInfo FilterActiveLabelField = AccessTools.Field(typeof(global::DropdownFilterUI), "labelActive")!;
        private static readonly FieldInfo DropdownValueLabelField = AccessTools.Field(typeof(global::ShinyShoe.GameUISelectableDropdown), "valueLabel")!;
        private static readonly FieldInfo DropdownValueActiveLabelField = AccessTools.Field(typeof(global::ShinyShoe.GameUISelectableDropdown), "valueActiveLabel")!;
        private static readonly FieldInfo DropdownEntriesField = AccessTools.Field(typeof(global::ShinyShoe.GameUISelectableDropdown), "entries")!;

        private readonly global::DropdownFilterUI _filter;
        private readonly global::ShinyShoe.GameUISelectableDropdown _dropdown;
        private ListContainer _choices;
        private bool _expanded;

        public ProxyCompendiumDropdownFilter(global::DropdownFilterUI filter, IGameUIComponent dropdown)
            : base(dropdown)
        {
            _filter = filter;
            _dropdown = dropdown as global::ShinyShoe.GameUISelectableDropdown;
        }

        public override bool IsVisible => _filter != null && _filter.gameObject.activeInHierarchy;

        public override string GetTypeKey() => "dropdown";
        public bool IsExpanded => _expanded;

        public override Message GetLabel()
        {
            Message semanticLabel = FilterLabel();
            if (semanticLabel != null)
            {
                return semanticLabel;
            }

            return Screens.CompendiumScreen.TextOrNull(global::MonsterTrainAccessibility.Core.ReflectionUtil.Get<TMP_Text>(_filter, FilterActiveLabelField))
                ?? Screens.CompendiumScreen.TextOrNull(global::MonsterTrainAccessibility.Core.ReflectionUtil.Get<TMP_Text>(_filter, FilterLabelField));
        }

        public override Message GetStatusString()
        {
            return Screens.CompendiumScreen.TextOrNull(global::MonsterTrainAccessibility.Core.ReflectionUtil.Get<TMP_Text>(_dropdown, DropdownValueActiveLabelField))
                ?? Screens.CompendiumScreen.TextOrNull(global::MonsterTrainAccessibility.Core.ReflectionUtil.Get<TMP_Text>(_dropdown, DropdownValueLabelField));
        }

        public override bool Activate()
        {
            SelectTarget();
            if (_expanded)
            {
                Collapse(announce: true);
            }
            else
            {
                Expand();
            }
            return true;
        }

        public bool HandleExpandedAction(InputAction action)
        {
            switch (action?.Key)
            {
                case "ui_up":
                    return MoveChoice(-1);
                case "ui_down":
                    return MoveChoice(1);
                case "ui_accept":
                case "ui_select":
                    return SelectFocusedChoice();
                case "ui_cancel":
                    Collapse(announce: true);
                    return true;
                default:
                    return false;
            }
        }

        public void Collapse(bool announce)
        {
            _expanded = false;
            _choices = null;
            SelectTarget();
            UIManager.SetFocusedElement(this);
            UIManager.RefreshBuffersFor(this);
            if (announce)
            {
                UIManager.ForceReannounceCurrentFocus();
            }
        }

        private bool Expand()
        {
            if (_dropdown == null)
            {
                return false;
            }

            List<global::SettableLabel> entries = global::MonsterTrainAccessibility.Core.ReflectionUtil.Get<List<global::SettableLabel>>(_dropdown, DropdownEntriesField);
            if (entries == null || entries.Count == 0)
            {
                return false;
            }

            OptionsFilter options = _filter?.Filter as OptionsFilter;
            int current = options != null ? options.OptionIndex : -1;
            _choices = new ListContainer
            {
                AnnounceName = false,
                AnnouncePosition = true,
                NavigationAxis = NavigationAxis.Vertical
            };

            for (int i = 0; i < entries.Count; i++)
            {
                global::SettableLabel entry = entries[i];
                if (entry != null)
                {
                    _choices.Add(new DropdownChoice(entry, i));
                }
            }

            if (_choices.Children.Count == 0)
            {
                _choices = null;
                return false;
            }

            _expanded = true;
            if (current >= 0 && current < _choices.Children.Count)
            {
                _choices.SetFocusIndex(current, selectForNavigation: false);
            }
            else
            {
                _choices.FocusFirst();
            }

            ReannounceFocusedChoice();
            return true;
        }

        private bool MoveChoice(int direction)
        {
            if (_choices == null || direction == 0)
            {
                return false;
            }

            int oldIndex = _choices.FocusIndex;
            bool handled = _choices.MoveRelative(direction);
            if (handled && _choices.FocusIndex != oldIndex)
            {
                ReannounceFocusedChoice();
            }

            return handled;
        }

        private bool SelectFocusedChoice()
        {
            DropdownChoice choice = _choices?.FocusedChild as DropdownChoice;
            if (choice == null)
            {
                return false;
            }

            return ApplyChoice(choice);
        }

        private bool ApplyChoice(DropdownChoice choice)
        {
            if (_dropdown == null || choice?.Entry == null)
            {
                return false;
            }

            _dropdown.SetValue(choice.Entry.content);
            _dropdown.optionChosenSignal.Dispatch(choice.Index, choice.Entry.content.text);
            _dropdown.Close();
            Collapse(announce: true);
            return true;
        }

        private void ReannounceFocusedChoice()
        {
            UIElement focused = _choices?.FocusedChild;
            if (focused == null)
            {
                return;
            }

            UIManager.SetFocusedElement(focused);
            UIManager.RefreshBuffersFor(focused);
            UIManager.ForceReannounceCurrentFocus();
        }

        private Message FilterLabel()
        {
            string typeName = _filter?.Filter?.GetType().Name;
            switch (typeName)
            {
                case "TypeFilter":
                case "MasteryFilter":
                case "CovenantRankFilter":
                case "DifficultyTierFilter":
                    return Message.Localized("ui", "COMPENDIUM.FILTER." + typeName.ToUpperInvariant());
                default:
                    return null;
            }
        }

        private sealed class DropdownChoice : UIElement
        {
            public DropdownChoice(global::SettableLabel entry, int index)
            {
                Entry = entry;
                Index = index;
            }

            public global::SettableLabel Entry { get; }
            public int Index { get; }

            public override Message GetLabel()
            {
                string label = AuthoredLabelReader.Read(Entry);
                if (!string.IsNullOrWhiteSpace(label))
                {
                    return Message.RawCleaned(label);
                }

                return Message.RawCleaned(ProxySettingsDropdownEntry.ResolveDropdownText(Entry?.content.text));
            }
        }
    }
}
