using MonsterTrainAccessibility.UI.Screens;
using MonsterTrainAccessibility.Localization;
using MonsterTrainAccessibility.UI.Elements;

namespace MonsterTrainAccessibility.UI.Elements.Compendium
{
    internal sealed class ProxyCompendiumSubclanVictory : ProxyElement, INavigationTargetElement
    {
        private readonly global::SubclanVictoryItem _victory;

        public ProxyCompendiumSubclanVictory(global::SubclanVictoryItem victory)
            : base(victory != null ? victory.gameObject : null)
        {
            _victory = victory;
        }

        public override bool IsVisible => _victory != null && _victory.gameObject.activeInHierarchy;

        public override Message GetLabel()
        {
            if (_victory?.data == null)
            {
                return null;
            }

            return Message.Localized("ui", "COMPENDIUM.CHECKLIST.VICTORY", new
            {
                main = _victory.data.mainClassData?.GetTitle(),
                allied = _victory.data.subClassData?.GetTitle()
            });
        }

        public override Message GetTooltip()
        {
            return AccessibleScreenText.Tooltip(_victory);
        }

        public void SelectForNavigation()
        {
            Screens.CompendiumScreen.ClearGameSelection();
        }
    }
}
