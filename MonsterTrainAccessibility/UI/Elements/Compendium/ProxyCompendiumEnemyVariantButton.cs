using MonsterTrainAccessibility.UI.Screens;
using MonsterTrainAccessibility.Localization;

namespace MonsterTrainAccessibility.UI.Elements.Compendium
{
    internal sealed class ProxyCompendiumEnemyVariantButton : ProxyCompendiumGameButton
    {
        private readonly global::ShinyShoe.GameUISelectableButton _button;
        private readonly int _index;

        public ProxyCompendiumEnemyVariantButton(global::ShinyShoe.GameUISelectableButton button, int index)
            : base(button)
        {
            _button = button;
            _index = index;
        }

        public override bool IsVisible => _button != null && _button.gameObject.activeInHierarchy;

        public override Message GetLabel()
        {
            return Message.Localized("ui", "COMPENDIUM.ENEMIES.VARIANT", new { index = _index });
        }
    }
}
