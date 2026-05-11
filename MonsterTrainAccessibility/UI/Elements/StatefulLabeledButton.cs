using System;
using MonsterTrainAccessibility.Localization;
using ShinyShoe;

namespace MonsterTrainAccessibility.UI.Elements
{
    internal sealed class StatefulLabeledButton : ButtonProxy
    {
        private readonly GameUISelectableButton _button;
        private readonly string _labelKey;
        private readonly Func<Message> _status;

        public StatefulLabeledButton(GameUISelectableButton button, string labelKey, Func<Message> status)
            : base(button)
        {
            _button = button;
            _labelKey = labelKey;
            _status = status;
        }

        public override bool IsVisible => _button != null && _button.gameObject.activeInHierarchy;

        public override Message GetLabel()
        {
            return Message.Localized("ui", _labelKey);
        }

        public override Message GetStatusString()
        {
            return _status != null ? _status() : ButtonState(_button);
        }
    }
}
