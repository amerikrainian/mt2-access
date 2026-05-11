using MonsterTrainAccessibility.UI.Screens;
using MonsterTrainAccessibility.Localization;
using MonsterTrainAccessibility.UI.Elements;

namespace MonsterTrainAccessibility.UI.Elements.Compendium
{
    internal sealed class ProxyCompendiumRelicCollection : ProxyElement, INavigationTargetElement
    {
        private readonly global::CompendiumRelicCollection _collection;
        private readonly global::TooltipProviderComponent _provider;

        public ProxyCompendiumRelicCollection(global::CompendiumRelicCollection collection, global::TooltipProviderComponent provider)
            : base(collection != null ? collection.gameObject : null)
        {
            _collection = collection;
            _provider = provider;
        }

        public override bool IsVisible => _collection != null && _collection.gameObject.activeInHierarchy;

        public override Message GetLabel()
        {
            return AccessibleScreenText.Tooltip(_provider);
        }

        public override Message GetStatusString()
        {
            return _collection != null
                ? Message.Localized("ui", "COMPENDIUM.BLESSINGS.COUNT", new { discovered = _collection.NumRelicsDiscovered, total = _collection.NumRelicsTotal })
                : null;
        }

        public void SelectForNavigation()
        {
            Screens.CompendiumScreen.ClearGameSelection();
        }
    }
}
