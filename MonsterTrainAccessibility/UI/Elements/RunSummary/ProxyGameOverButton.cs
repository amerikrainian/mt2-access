using MonsterTrainAccessibility.Localization;
using MonsterTrainAccessibility.UI.Screens;
using ShinyShoe;
using TMPro;

namespace MonsterTrainAccessibility.UI.Elements
{
    internal sealed class ProxyGameOverButton : GameObjectElement
    {
        private readonly GameUISelectableButton _button;
        private readonly TMP_Text _labelText;
        private readonly string _fallbackKey;

        public ProxyGameOverButton(GameUISelectableButton button, string fallbackKey = null, TMP_Text labelText = null)
            : base(
                button != null ? button.gameObject : null,
                typeKey: "button",
                label: null)
        {
            _button = button;
            _fallbackKey = fallbackKey;
            _labelText = labelText;
        }

        public GameUISelectableButton Button => _button;
        public override bool IsVisible => _button != null && _button.gameObject.activeInHierarchy;
        public override Message GetLabel()
        {
            Message label = AccessibleScreenText.Text(_labelText);
            if (label != null)
            {
                return label;
            }

            string buttonLabel = AccessibleScreenText.ReadButtonLabel(_button);
            if (!string.IsNullOrWhiteSpace(buttonLabel))
            {
                return Message.Raw(buttonLabel);
            }

            return !string.IsNullOrWhiteSpace(_fallbackKey)
                ? Message.Localized("ui", _fallbackKey)
                : null;
        }

        public override Message GetStatusString() => GameButtonElement.StateMessage(_button);

        public static void AppendSignature(System.Text.StringBuilder sb, GameUISelectableButton button)
        {
            sb.Append(button != null && button.gameObject.activeInHierarchy)
                .Append(':')
                .Append(button != null && button.interactable)
                .Append('|');
        }
    }
}
