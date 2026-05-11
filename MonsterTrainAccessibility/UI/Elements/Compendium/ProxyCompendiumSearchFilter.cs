using MonsterTrainAccessibility.UI.Screens;
using MonsterTrainAccessibility.Localization;
using System;

namespace MonsterTrainAccessibility.UI.Elements.Compendium
{
    internal sealed class ProxyCompendiumSearchFilter : ProxyCompendiumGameButton
    {
        private readonly global::SearchFilterUI _filter;
        private readonly global::InputFieldContainer _input;
        private readonly Message _label;
        private readonly Func<bool> _activate;

        public ProxyCompendiumSearchFilter(
            global::SearchFilterUI filter,
            global::InputFieldContainer input,
            Message label,
            global::ShinyShoe.GameUISelectableButton button,
            Func<bool> activate)
            : base(button)
        {
            _filter = filter;
            _input = input;
            _label = label;
            _activate = activate;
        }

        public override bool IsVisible => _filter != null && _filter.gameObject.activeInHierarchy;

        public override Message GetLabel()
        {
            return _label;
        }

        public override Message GetStatusString()
        {
            return Message.FromText(_input?.text);
        }

        internal bool IsForButton(global::ShinyShoe.GameUISelectableButton button)
        {
            return button != null && _input?.button != null && _input.button.IsGameUIComponent(button);
        }

        public override bool Activate() => _activate != null && _activate();
    }
}
