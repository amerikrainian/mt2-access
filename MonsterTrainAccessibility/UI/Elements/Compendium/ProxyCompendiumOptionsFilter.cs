using System.Collections.Generic;
using MonsterTrainAccessibility.Core;
using MonsterTrainAccessibility.Input;
using MonsterTrainAccessibility.Localization;
using ShinyShoe;

namespace MonsterTrainAccessibility.UI.Elements.Compendium
{
    internal sealed class ProxyCompendiumOptionsFilter : ProxyCompendiumGameButton, ICompendiumExpandableFilter
    {
        private readonly global::OptionsFilterUI _filter;
        private readonly List<global::FilterOptionButton> _options;
        private ListContainer _choices;
        private bool _expanded;

        public ProxyCompendiumOptionsFilter(
            global::OptionsFilterUI filter,
            List<global::FilterOptionButton> options)
            : base(filter?.GetDefaultGameUISelectable())
        {
            _filter = filter;
            _options = options;
        }

        public override bool IsVisible => _filter != null && _filter.gameObject.activeInHierarchy;
        public override string GetTypeKey() => "dropdown";
        public bool IsExpanded => _expanded;

        public override Message GetLabel()
        {
            string typeName = _filter?.Filter?.GetType().Name;
            if (string.IsNullOrEmpty(typeName))
            {
                return Message.Localized("ui", "COMPENDIUM.FILTER");
            }

            return Message.Localized("ui", "COMPENDIUM.FILTER." + typeName.ToUpperInvariant());
        }

        public override Message GetStatusString()
        {
            if (HasExplicitAllChoice() && _filter?.IsActive != true)
            {
                return Message.Localized("ui", "COMPENDIUM.FILTER.ALL");
            }

            return _filter?.IsActive == true ? Message.FromText(_filter.Filter?.ToString()) : null;
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
            if (_filter == null || _options == null || _options.Count == 0)
            {
                return false;
            }

            OptionsFilter optionsFilter = _filter.Filter as OptionsFilter;
            int current = optionsFilter != null ? optionsFilter.OptionIndex : -1;
            _choices = new ListContainer
            {
                AnnounceName = false,
                AnnouncePosition = true,
                NavigationAxis = NavigationAxis.Vertical
            };

            if (HasExplicitAllChoice())
            {
                _choices.Add(OptionChoice.All());
            }

            for (int i = 0; i < _options.Count; i++)
            {
                global::FilterOptionButton option = _options[i];
                if (option != null && option.Button != null && option.gameObject.activeInHierarchy && !option.isLockedWithoutDlc)
                {
                    _choices.Add(new OptionChoice(option, i));
                }
            }

            if (_choices.Children.Count == 0)
            {
                _choices = null;
                return false;
            }

            _expanded = true;
            UIElement currentChoice = FindChoice(current);
            if (currentChoice != null)
            {
                _choices.SetFocusTo(currentChoice, selectForNavigation: false);
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
            OptionChoice choice = _choices?.FocusedChild as OptionChoice;
            if (choice == null)
            {
                return false;
            }

            return ApplyChoice(choice);
        }

        private bool ApplyChoice(OptionChoice choice)
        {
            if (_filter == null || choice == null)
            {
                return false;
            }

            if (choice.IsAll)
            {
                _filter.Clear();
                Collapse(announce: true);
                return true;
            }

            if (choice.Option?.Button == null)
            {
                return false;
            }

            SelectTarget();
            CoreInputControlMapping mapping = new CoreInputControlMapping(global::InputManager.Controls.Submit, InputType.None, fake: true);
            bool handled = _filter.ApplyScreenInput(mapping, choice.Option.Button, global::InputManager.Controls.Submit);
            if (handled)
            {
                Collapse(announce: true);
            }

            return handled;
        }

        private UIElement FindChoice(int optionIndex)
        {
            if (_choices == null)
            {
                return null;
            }

            for (int i = 0; i < _choices.Children.Count; i++)
            {
                OptionChoice choice = _choices.Children[i] as OptionChoice;
                if (choice != null && choice.Index == optionIndex)
                {
                    return choice;
                }
            }

            return null;
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

        private bool HasExplicitAllChoice()
        {
            return _filter?.Filter is ClassFilter ||
                _filter?.Filter is RarityFilter ||
                _filter?.Filter is CostFilter;
        }

        private sealed class OptionChoice : UIElement
        {
            private readonly ProxyCompendiumFilterOptionButton _labelSource;

            private OptionChoice()
            {
                Index = -1;
                IsAll = true;
            }

            public OptionChoice(global::FilterOptionButton option, int index)
            {
                Option = option;
                Index = index;
                _labelSource = new ProxyCompendiumFilterOptionButton(option);
            }

            public global::FilterOptionButton Option { get; }
            public int Index { get; }
            public bool IsAll { get; }

            public override Message GetLabel() => IsAll ? Message.Localized("ui", "COMPENDIUM.FILTER.ALL") : _labelSource.GetLabel();
            public override Message GetStatusString() => IsAll ? null : _labelSource.GetStatusString();
            public override Message GetTooltip() => IsAll ? null : _labelSource.GetTooltip();

            public static OptionChoice All()
            {
                return new OptionChoice();
            }
        }
    }
}
