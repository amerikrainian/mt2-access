using MonsterTrainAccessibility.UI.Screens;
using MonsterTrainAccessibility.Localization;
using MonsterTrainAccessibility.UI.Elements;

namespace MonsterTrainAccessibility.UI.Elements.Compendium
{
    internal sealed class ProxyCompendiumFilterToolbar : ProxyElement, INavigationTargetElement
    {
        private readonly global::FilterToolbar _toolbar;

        public ProxyCompendiumFilterToolbar(global::FilterToolbar toolbar)
            : base(toolbar != null ? toolbar.gameObject : null)
        {
            _toolbar = toolbar;
        }

        public override bool IsVisible => _toolbar != null && _toolbar.gameObject.activeInHierarchy;

        public override Message GetLabel()
        {
            return Message.Localized("ui", "COMPENDIUM.FILTERS");
        }

        public override Message GetStatusString()
        {
            return Message.FromText(_toolbar?.GetFilterString()) ?? Message.Localized("ui", "COMPENDIUM.FILTERS.NONE");
        }

        public void SelectForNavigation()
        {
            Screens.CompendiumScreen.SelectGameComponent(_toolbar?.GetDefaultGameUISelectable());
        }
    }
}
