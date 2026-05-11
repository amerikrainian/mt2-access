using MonsterTrainAccessibility.Localization;
using ShinyShoe;

namespace MonsterTrainAccessibility.UI.Elements
{
    internal sealed class ProxyRunSummaryButton : GameObjectElement
    {
        private readonly GameUISelectableButton _button;
        private readonly string _fallbackLabelKey;

        public ProxyRunSummaryButton(GameUISelectableButton button, string fallbackLabelKey)
            : base(
                button != null ? button.gameObject : null,
                typeKey: "button",
                label: null)
        {
            _button = button;
            _fallbackLabelKey = fallbackLabelKey;
        }

        public override bool IsVisible => _button != null && _button.gameObject.activeInHierarchy;
        public override Message GetLabel()
        {
            Message label = Message.RawCleaned(GameUIButtonSupport.ResolveLabel(_button));
            return label ?? Message.Localized("ui", _fallbackLabelKey);
        }

        public override Message GetStatusString() => GameButtonElement.StateMessage(_button);
    }
}
