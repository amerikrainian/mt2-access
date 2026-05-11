using System;
using MonsterTrainAccessibility.Localization;
using ShinyShoe;

namespace MonsterTrainAccessibility.UI.Elements
{
    internal sealed class ProxyRunSetupButton : GameObjectElement
    {
        private readonly GameUISelectableButton _button;
        private readonly Func<Message> _label;

        public ProxyRunSetupButton(GameUISelectableButton button, Func<Message> label)
            : base(
                button != null ? button.gameObject : null,
                typeKey: "button",
                label: null)
        {
            _button = button;
            _label = label;
        }

        public override bool IsVisible => _button != null && _button.gameObject.activeInHierarchy;
        public override Message GetLabel() => _label != null ? _label() : null;
        public override Message GetStatusString() => GameButtonElement.StateMessage(_button);
    }
}
