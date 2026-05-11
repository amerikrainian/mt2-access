using MonsterTrainAccessibility.Localization;
using MonsterTrainAccessibility.UI;
using TMPro;

namespace MonsterTrainAccessibility.UI.Elements
{
    internal sealed class ProxyUnlockText : GameObjectElement
    {
        private readonly TMP_Text _text;

        public ProxyUnlockText(TMP_Text text)
            : base(text != null ? text.gameObject : null, label: null)
        {
            _text = text;
        }

        public override bool IsVisible => _text != null &&
            _text.gameObject.activeInHierarchy &&
            Message.ShouldAdd(Message.Clean(AccessibilityText.ReadLocalizedText(_text)));

        public override Message GetLabel() => Message.RawCleaned(AccessibilityText.ReadLocalizedText(_text));
    }
}
