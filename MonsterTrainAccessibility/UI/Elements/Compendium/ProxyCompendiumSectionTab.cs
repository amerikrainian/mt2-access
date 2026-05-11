using MonsterTrainAccessibility.UI.Screens;
using System;
using MonsterTrainAccessibility.Localization;
using MonsterTrainAccessibility.UI.Elements;

namespace MonsterTrainAccessibility.UI.Elements.Compendium
{
    internal sealed class ProxyCompendiumSectionTab : ProxyElement, IActivatableElement, INavigationTargetElement
    {
        private readonly global::CompendiumScreen _screen;
        private readonly global::CompendiumScreen.Section _section;
        private readonly Func<global::CompendiumScreen.Section> _currentSection;
        private readonly global::CompendiumTab _tab;
        private readonly global::ShinyShoe.GameUISelectableButton _button;

        public ProxyCompendiumSectionTab(
            global::CompendiumScreen screen,
            global::CompendiumScreen.Section section,
            Func<global::CompendiumScreen.Section> currentSection,
            global::CompendiumTab tab,
            global::ShinyShoe.GameUISelectableButton button)
            : base(tab != null ? tab.gameObject : button != null ? button.gameObject : null)
        {
            _screen = screen;
            _section = section;
            _currentSection = currentSection;
            _tab = tab;
            _button = button;
        }

        public override bool IsVisible => _tab == null || _tab.gameObject.activeInHierarchy;

        public override string GetTypeKey() => "button";

        public override Message GetLabel()
        {
            return Screens.CompendiumScreen.SectionName(_section);
        }

        public override Message GetStatusString()
        {
            return _currentSection != null && _section == _currentSection()
                ? Message.Localized("messages", "state.selected")
                : null;
        }

        public bool Activate()
        {
            _screen?.SetSection(_section);
            return _screen != null;
        }

        public void SelectForNavigation()
        {
            Screens.CompendiumScreen.SelectGameComponent(_button);
        }
    }
}
