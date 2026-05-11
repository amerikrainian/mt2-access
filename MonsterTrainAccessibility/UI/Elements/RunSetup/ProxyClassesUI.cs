using MonsterTrainAccessibility.Localization;
using MonsterTrainAccessibility.UI;

namespace MonsterTrainAccessibility.UI.Elements
{
    internal sealed class ProxyClassesUI : GameObjectElement
    {
        private readonly global::ChosenClassesUI _classes;

        public ProxyClassesUI(global::ChosenClassesUI classes)
            : base(
                component: classes != null ? classes.SelectableUI : null,
                typeKey: null,
                label: null)
        {
            _classes = classes;
        }

        public override bool IsVisible => _classes != null && _classes.gameObject.activeInHierarchy;
        public override Message GetLabel() => Message.Localized("ui", "HUD.CLASSES");
        public override Message GetTooltip() => _classes != null ? TooltipText.ForComponent(_classes) : null;
    }
}
