using MonsterTrainAccessibility.Localization;
using MonsterTrainAccessibility.UI.Screens;
using TMPro;

namespace MonsterTrainAccessibility.UI.Elements
{
    internal sealed class ProxyVictoryText : GameObjectElement
    {
        private readonly TMP_Text _label;
        private readonly TMP_Text _amount;

        public ProxyVictoryText(TMP_Text label, TMP_Text amount = null)
            : base(
                target: label != null ? label.gameObject : null,
                label: null)
        {
            _label = label;
            _amount = amount;
        }

        public override bool IsVisible => _label != null && _label.gameObject.activeInHierarchy;
        public override Message GetLabel()
        {
            return _amount != null
                ? Message.Join(", ", AccessibleScreenText.Text(_label), AccessibleScreenText.Text(_amount))
                : AccessibleScreenText.Text(_label);
        }

        public TMP_Text Label => _label;
        public TMP_Text Amount => _amount;
    }
}
