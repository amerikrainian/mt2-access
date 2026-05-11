using MonsterTrainAccessibility.UI.Screens;
using MonsterTrainAccessibility.Localization;
using MonsterTrainAccessibility.UI.Elements;

namespace MonsterTrainAccessibility.UI.Elements.Compendium
{
    internal sealed class ProxyCompendiumChecklistPage : ProxyElement, INavigationTargetElement
    {
        private readonly global::ChecklistPage _page;

        public ProxyCompendiumChecklistPage(global::ChecklistPage page)
            : base(page != null ? page.gameObject : null)
        {
            _page = page;
        }

        public override bool IsVisible => _page != null && _page.gameObject.activeInHierarchy;

        public override Message GetLabel()
        {
            if (_page is global::RailforgedChecklistPage)
            {
                return Message.Localized("ui", "COMPENDIUM.CHECKLIST.RAILFORGED");
            }

            return Message.Localized("ui", "COMPENDIUM.CHECKLIST.STANDARD");
        }

        public void SelectForNavigation()
        {
            Screens.CompendiumScreen.ClearGameSelection();
        }
    }
}
