using MonsterTrainAccessibility.UI.Screens;
using MonsterTrainAccessibility.Localization;
using MonsterTrainAccessibility.UI.Elements;

namespace MonsterTrainAccessibility.UI.Elements.Compendium
{
    internal sealed class ProxyCompendiumSectionHeader : ProxyElement, INavigationTargetElement
    {
        private readonly global::CompendiumSection _section;

        public ProxyCompendiumSectionHeader(global::CompendiumSection section)
            : base(section != null ? section.gameObject : null)
        {
            _section = section;
        }

        public override bool IsVisible => _section != null && _section.gameObject.activeInHierarchy;

        public override Message GetLabel()
        {
            return _section != null ? Screens.CompendiumScreen.SectionName(_section.Section) : null;
        }

        public void SelectForNavigation()
        {
            Screens.CompendiumScreen.ClearGameSelection();
        }
    }
}
