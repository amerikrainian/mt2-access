using System;
using MonsterTrainAccessibility.Localization;

namespace MonsterTrainAccessibility.UI.Elements
{
    public class CustomElement : UIElement
    {
        private readonly Func<Message> _label;
        private readonly Func<Message> _status;
        private readonly Func<Message> _tooltip;
        private readonly Func<Message> _extras;
        private readonly Func<bool> _visibility;
        private readonly string _typeKey;

        public CustomElement(
            Func<Message> label,
            Func<Message> status = null,
            Func<Message> tooltip = null,
            Func<Message> extras = null,
            Func<bool> visibility = null,
            string typeKey = null)
        {
            _label = label;
            _status = status;
            _tooltip = tooltip;
            _extras = extras;
            _visibility = visibility;
            _typeKey = typeKey;
        }

        public override bool IsVisible => _visibility == null || _visibility();

        public override Message GetLabel() => _label != null ? _label() : null;
        public override Message GetStatusString() => _status != null ? _status() : null;
        public override Message GetTooltip() => _tooltip != null ? _tooltip() : null;
        public override Message GetExtrasString() => _extras != null ? _extras() : null;
        public override string GetTypeKey() => _typeKey;
    }
}
