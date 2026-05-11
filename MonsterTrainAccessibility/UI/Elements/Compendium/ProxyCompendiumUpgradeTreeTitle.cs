using MonsterTrainAccessibility.UI.Screens;
using MonsterTrainAccessibility.Localization;
using MonsterTrainAccessibility.UI.Elements;
using TMPro;

namespace MonsterTrainAccessibility.UI.Elements.Compendium
{
    internal sealed class ProxyCompendiumUpgradeTreeTitle : ProxyElement, INavigationTargetElement
    {
        private readonly global::UpgradeTreeUI _tree;
        private readonly TMP_Text _title;

        public ProxyCompendiumUpgradeTreeTitle(global::UpgradeTreeUI tree, TMP_Text title)
            : base(tree != null ? tree.gameObject : null)
        {
            _tree = tree;
            _title = title;
        }

        public override bool IsVisible => _tree != null && _tree.gameObject.activeInHierarchy;

        public override Message GetLabel()
        {
            return Screens.CompendiumScreen.TextOrNull(_title);
        }

        public void SelectForNavigation()
        {
            Screens.CompendiumScreen.ClearGameSelection();
        }
    }
}
