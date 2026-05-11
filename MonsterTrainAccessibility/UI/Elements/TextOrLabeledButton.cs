using MonsterTrainAccessibility.Localization;
using ShinyShoe;
using TMPro;

namespace MonsterTrainAccessibility.UI.Elements
{
    internal sealed class TextOrLabeledButton : ButtonProxy
    {
        private readonly GameUISelectableButton _button;
        private readonly TMP_Text _text;
        private readonly string _fallbackKey;

        public TextOrLabeledButton(GameUISelectableButton button, TMP_Text text, string fallbackKey)
            : base(button)
        {
            _button = button;
            _text = text;
            _fallbackKey = fallbackKey;
        }

        public override bool IsVisible => _button != null && _button.gameObject.activeInHierarchy;

        public override Message GetLabel()
        {
            return Message.FromText(AccessibilityText.ReadLocalizedText(_text)) ?? Message.Localized("ui", _fallbackKey);
        }

        public override Message GetStatusString()
        {
            return ButtonState(_button);
        }
    }
}
