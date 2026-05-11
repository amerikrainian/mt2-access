using System;
using MonsterTrainAccessibility.Input;
using MonsterTrainAccessibility.Localization;
using ShinyShoe;

namespace MonsterTrainAccessibility.UI.Elements
{
    internal sealed class GameDropdownCycleElement : CustomElement, INavigationActionHandler, IActivatableElement, INavigationTargetElement
    {
        private readonly GameUISelectableDropdown _dropdown;
        private readonly Func<int> _currentIndex;
        private readonly Func<int> _count;
        private readonly Func<int, Message> _optionLabel;
        private readonly Action<int, string> _choose;
        private readonly Action _selectForNavigation;

        public GameDropdownCycleElement(
            GameUISelectableDropdown dropdown,
            Func<Message> label,
            Func<int> currentIndex,
            Func<int> count,
            Func<int, Message> optionLabel,
            Action<int, string> choose,
            Action selectForNavigation = null)
            : base(
                label: label,
                status: () => Status(dropdown, currentIndex, optionLabel),
                visibility: () => dropdown != null && dropdown.gameObject.activeInHierarchy,
                typeKey: "dropdown")
        {
            _dropdown = dropdown;
            _currentIndex = currentIndex;
            _count = count;
            _optionLabel = optionLabel;
            _choose = choose;
            _selectForNavigation = selectForNavigation;
        }

        public bool Activate()
        {
            return true;
        }

        public void SelectForNavigation()
        {
            _selectForNavigation?.Invoke();
        }

        public bool HandleAction(InputAction action)
        {
            switch (action?.Key)
            {
                case "ui_left":
                    return Move(-1);
                case "ui_right":
                    return Move(1);
                default:
                    return false;
            }
        }

        private bool Move(int delta)
        {
            int count = _count != null ? _count() : 0;
            if (_dropdown == null || count <= 0)
            {
                return false;
            }

            int current = _currentIndex != null ? _currentIndex() : 0;
            int next = ((current + delta) % count + count) % count;
            Message label = _optionLabel != null ? _optionLabel(next) : null;
            string value = label != null ? label.Resolve() : string.Empty;

            _dropdown.SetIndex(next);
            _choose?.Invoke(next, value);
            return true;
        }

        private static Message Status(
            GameUISelectableDropdown dropdown,
            Func<int> currentIndex,
            Func<int, Message> optionLabel)
        {
            Message option = null;
            int index = currentIndex != null ? currentIndex() : -1;
            if (optionLabel != null && index >= 0)
            {
                option = optionLabel(index);
            }

            Message dropdownStatus = dropdown != null ? Message.RawCleaned(ProxyDropdown.ResolveStatus(dropdown.gameObject)) : null;
            return option ?? dropdownStatus;
        }
    }
}
