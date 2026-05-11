using System;
using MonsterTrainAccessibility.Localization;
using ShinyShoe;
using UnityEngine;

namespace MonsterTrainAccessibility.UI.Elements
{
    internal sealed class LabeledButton : ButtonProxy
    {
        private readonly GameUISelectableButton _button;
        private readonly string _labelKey;
        private readonly Message _labelMessage;
        private readonly Func<Message> _label;
        private readonly Func<Message> _tooltip;
        private readonly Func<bool> _visibility;
        private readonly string _typeKey;

        public LabeledButton(GameUISelectableButton button, string labelKey)
            : base(button)
        {
            _button = button;
            _labelKey = labelKey;
            _typeKey = "button";
        }

        public LabeledButton(GameUISelectableButton button, Message label)
            : base(button)
        {
            _button = button;
            _labelMessage = label;
            _typeKey = "button";
        }

        public LabeledButton(GameUISelectableButton button, Func<Message> label, Func<bool> visibility = null, Func<Message> tooltip = null)
            : base(button)
        {
            _button = button;
            _label = label;
            _visibility = visibility;
            _tooltip = tooltip;
            _typeKey = "button";
        }

        public LabeledButton(GameObject target, Func<Message> label, Func<bool> visibility = null, Func<Message> tooltip = null, string typeKey = "button")
            : base(target)
        {
            _label = label;
            _visibility = visibility;
            _tooltip = tooltip;
            _typeKey = typeKey;
        }

        public override bool IsVisible
        {
            get
            {
                if (_visibility != null)
                {
                    return _visibility();
                }

                return _button != null ? _button.gameObject.activeInHierarchy : base.IsVisible;
            }
        }

        public override Message GetLabel()
        {
            if (_label != null)
            {
                return _label();
            }

            if (_labelMessage != null)
            {
                return _labelMessage;
            }

            return _labelKey != null ? Message.Localized("ui", _labelKey) : null;
        }

        public override string GetTypeKey() => _typeKey;

        public override Message GetTooltip() => _tooltip != null ? _tooltip() : null;

        public override Message GetStatusString()
        {
            return _button != null ? ButtonState(_button) : null;
        }
    }
}
