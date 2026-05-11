using MonsterTrainAccessibility.Localization;
using MonsterTrainAccessibility.UI;
using ShinyShoe;

namespace MonsterTrainAccessibility.UI.Elements
{
    internal sealed class ProxyUnlockButton : GameObjectElement
    {
        private readonly GameUISelectableButton _button;

        public ProxyUnlockButton(GameUISelectableButton button)
            : base(
                button != null ? button.gameObject : null,
                typeKey: "button",
                label: null)
        {
            _button = button;
        }

        public override bool IsVisible => _button != null && _button.gameObject.activeInHierarchy;
        public override Message GetLabel()
        {
            string label = Message.Clean(GameUIButtonSupport.ResolveLabel(_button));
            return !string.IsNullOrWhiteSpace(label)
                ? Message.RawCleaned(label)
                : Message.Localized("ui", "UNLOCK.OK");
        }

        public override Message GetStatusString() => GameButtonElement.StateMessage(_button);
    }
}
