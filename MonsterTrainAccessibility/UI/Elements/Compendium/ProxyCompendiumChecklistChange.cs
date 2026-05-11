using MonsterTrainAccessibility.UI.Screens;
using MonsterTrainAccessibility.Localization;
using MonsterTrainAccessibility.UI.Elements;

namespace MonsterTrainAccessibility.UI.Elements.Compendium
{
    internal sealed class ProxyCompendiumChecklistChange : ProxyElement, INavigationTargetElement
    {
        private readonly Message _message;

        public ProxyCompendiumChecklistChange(Message message)
        {
            _message = message;
        }

        public override bool IsVisible => _message != null;

        public override Message GetLabel()
        {
            return _message;
        }

        public void SelectForNavigation()
        {
            Screens.CompendiumScreen.ClearGameSelection();
        }
    }
}
