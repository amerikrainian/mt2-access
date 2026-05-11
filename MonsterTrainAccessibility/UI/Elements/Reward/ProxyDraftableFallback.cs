using MonsterTrainAccessibility.Localization;
using MonsterTrainAccessibility.UI.Screens;
using ShinyShoe;

namespace MonsterTrainAccessibility.UI.Elements
{
    internal sealed class ProxyDraftableFallback : GameObjectElement
    {
        private readonly IDraftableUI _item;
        private readonly IGameUIComponent _selectable;

        public ProxyDraftableFallback(IDraftableUI item, IGameUIComponent selectable)
            : base(
                selectable,
                typeKey: "button",
                label: null)
        {
            _item = item;
            _selectable = selectable;
        }

        public override bool IsVisible => _selectable?.component != null && _selectable.component.gameObject.activeInHierarchy;
        public override Message GetLabel() => AccessibleScreenText.ReadDraftableLabel(_item);
        public override Message GetTooltip() => AccessibleScreenText.Tooltip(_selectable?.component);
    }
}
