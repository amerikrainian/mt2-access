using System;
using MonsterTrainAccessibility.Localization;
using ShinyShoe;

namespace MonsterTrainAccessibility.UI.Elements
{
    internal sealed class ProxyChallengeActionButton : GameObjectElement
    {
        private readonly GameUISelectableButton _button;
        private readonly Message _fallback;
        private readonly Func<bool> _activate;
        private readonly Func<Message> _status;

        public ProxyChallengeActionButton(GameUISelectableButton button, Message fallback, Func<bool> activate, Func<Message> status = null)
            : base(button != null ? button.gameObject : null, typeKey: "button", label: null)
        {
            _button = button;
            _fallback = fallback;
            _activate = activate;
            _status = status;
        }

        public override bool IsVisible => _button != null && _button.gameObject.activeInHierarchy;

        public override Message GetLabel()
        {
            string label = GameUIButtonSupport.ResolveLabel(_button);
            return !string.IsNullOrWhiteSpace(label) ? Message.RawCleaned(label) : _fallback;
        }

        public override Message GetStatusString() => _status != null ? _status() : GameButtonElement.StateMessage(_button);

        public override bool Activate() => _activate != null && _activate();
    }
}
