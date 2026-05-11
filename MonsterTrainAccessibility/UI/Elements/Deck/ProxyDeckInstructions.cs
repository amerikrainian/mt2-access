using MonsterTrainAccessibility.Localization;
using MonsterTrainAccessibility.UI.Screens;
using TMPro;
using UnityEngine;

namespace MonsterTrainAccessibility.UI.Elements
{
    internal sealed class ProxyDeckInstructions : GameObjectElement
    {
        private readonly TMP_Text _title;
        private readonly TMP_Text _description;

        public ProxyDeckInstructions(TMP_Text title, TMP_Text description, GameObject fallbackTarget)
            : base(
                title != null ? title.gameObject : fallbackTarget,
                label: null)
        {
            _title = title;
            _description = description;
        }

        public override bool IsVisible => _title != null && _title.gameObject.activeInHierarchy;

        public override Message GetLabel()
        {
            return AccessibleScreenText.Text(_title);
        }

        public override Message GetTooltip()
        {
            return AccessibleScreenText.Text(_description);
        }
    }
}
