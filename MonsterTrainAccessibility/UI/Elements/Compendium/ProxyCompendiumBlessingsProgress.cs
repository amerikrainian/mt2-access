using MonsterTrainAccessibility.UI.Screens;
using MonsterTrainAccessibility.Localization;
using MonsterTrainAccessibility.UI.Elements;
using TMPro;

namespace MonsterTrainAccessibility.UI.Elements.Compendium
{
    internal sealed class ProxyCompendiumBlessingsProgress : ProxyElement, INavigationTargetElement
    {
        private readonly TMP_Text _count;

        public ProxyCompendiumBlessingsProgress(TMP_Text count)
            : base(count != null ? count.gameObject : null)
        {
            _count = count;
        }

        public override bool IsVisible => _count != null && _count.gameObject.activeInHierarchy;

        public override Message GetLabel()
        {
            return Message.Localized("ui", "COMPENDIUM.BLESSINGS.PROGRESS");
        }

        public override Message GetStatusString()
        {
            return Screens.CompendiumScreen.TextOrNull(_count);
        }

        public void SelectForNavigation()
        {
            Screens.CompendiumScreen.ClearGameSelection();
        }
    }
}
