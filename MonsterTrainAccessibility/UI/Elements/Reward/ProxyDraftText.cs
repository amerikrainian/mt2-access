using MonsterTrainAccessibility.Localization;
using MonsterTrainAccessibility.UI.Screens;
using TMPro;

namespace MonsterTrainAccessibility.UI.Elements
{
    internal sealed class ProxyDraftText : GameObjectElement
    {
        private readonly TMP_Text _text;

        public ProxyDraftText(TMP_Text text)
            : base(
                text != null ? text.gameObject : null,
                label: null)
        {
            _text = text;
        }

        public override bool IsVisible => _text != null && _text.gameObject.activeInHierarchy;
        public override Message GetLabel() => AccessibleScreenText.Text(_text);
    }
}
