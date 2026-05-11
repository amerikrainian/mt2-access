using MonsterTrainAccessibility.Localization;
using MonsterTrainAccessibility.UI.Screens;
using TMPro;

namespace MonsterTrainAccessibility.UI.Elements
{
    internal sealed class ProxyDeckEmptyMessage : GameObjectElement
    {
        private readonly TMP_Text _message;

        public ProxyDeckEmptyMessage(TMP_Text message)
            : base(
                message != null ? message.gameObject : null,
                label: null)
        {
            _message = message;
        }

        public override bool IsVisible => _message != null && _message.gameObject.activeInHierarchy;

        public override Message GetLabel()
        {
            return AccessibleScreenText.Text(_message);
        }
    }
}
