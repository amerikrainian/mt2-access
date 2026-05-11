using MonsterTrainAccessibility.Localization;
using MonsterTrainAccessibility.UI.Screens;
using TMPro;

namespace MonsterTrainAccessibility.UI.Elements
{
    internal sealed class ProxyGameOverText : UIElement, INavigationTargetElement
    {
        private readonly TMP_Text _text;
        private readonly Message _prefix;

        public ProxyGameOverText(TMP_Text text, Message prefix = null)
        {
            _text = text;
            _prefix = prefix;
        }

        public override bool IsVisible => _text != null && _text.gameObject.activeInHierarchy && HasMessage(TextMessage());
        public override Message GetLabel()
        {
            Message text = TextMessage();
            return _prefix != null && text != null
                ? Message.Join(", ", _prefix, text)
                : text;
        }

        public void SelectForNavigation()
        {
            global::InputManager.Inst?.SelectGameUIComponent(null);
        }

        public static void AppendSignature(System.Text.StringBuilder sb, TMP_Text text)
        {
            sb.Append(text != null && text.gameObject.activeInHierarchy).Append('|');
        }

        internal static bool HasMessage(Message message)
        {
            return !string.IsNullOrWhiteSpace(message?.Resolve());
        }

        private Message TextMessage()
        {
            return AccessibleScreenText.Text(_text);
        }
    }
}
