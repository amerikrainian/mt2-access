using System;
using MonsterTrainAccessibility.Localization;

namespace MonsterTrainAccessibility.UI.Elements
{
    internal sealed class ActionElement : CustomElement, IActivatableElement, INavigationTargetElement
    {
        private readonly Func<bool> _activate;
        private readonly Action _selectForNavigation;

        public ActionElement(
            Func<Message> label,
            Func<Message> status = null,
            Func<Message> tooltip = null,
            Func<Message> extras = null,
            Func<bool> visibility = null,
            string typeKey = null,
            Func<bool> activate = null,
            Action selectForNavigation = null)
            : base(
                label: label,
                status: status,
                tooltip: tooltip,
                extras: extras,
                visibility: visibility,
                typeKey: typeKey)
        {
            _activate = activate;
            _selectForNavigation = selectForNavigation;
        }

        public bool Activate()
        {
            return _activate != null && _activate();
        }

        public void SelectForNavigation()
        {
            _selectForNavigation?.Invoke();
        }
    }
}
